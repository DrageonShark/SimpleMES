using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public partial class DeviceAlarmViewModel : DialogViewModelBase, IDisposable
    {
        private const string AllSeverityFilter = "全部";
        private readonly IDeviceWorkspaceActions _actions;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _severityFilter = AllSeverityFilter;

        public DeviceAlarmViewModel(DeviceWorkspaceState state, IDeviceWorkspaceActions actions)
        {
            State = state;
            _actions = actions;
            PageTitle = "设备告警";

            SeverityFilterOptions = new[]
            {
                AllSeverityFilter,
                "严重",
                "告警",
                "提醒"
            };

            State.PendingAlarms.CollectionChanged += OnPendingAlarmsChanged;
            _actions.PropertyChanged += OnActionsPropertyChanged;
            RefreshVisibleAlarms();
        }

        public DeviceWorkspaceState State { get; }
        public IDeviceWorkspaceActions Actions => _actions;
        public IReadOnlyList<string> SeverityFilterOptions { get; }
        public ObservableCollection<DeviceAlarmItemViewModel> VisibleAlarms { get; } = new();

        public bool HasActiveFilters => !string.IsNullOrWhiteSpace(SearchKeyword) || SeverityFilter != AllSeverityFilter;
        public bool HasVisibleAlarms => VisibleAlarms.Count > 0;
        public int CriticalCount => State.PendingAlarms.Count(alarm => ResolveSeverity(alarm) == DeviceAlarmSeverity.Critical);
        public int WarningCount => State.PendingAlarms.Count(alarm => ResolveSeverity(alarm) == DeviceAlarmSeverity.Warning);
        public int NoticeCount => State.PendingAlarms.Count(alarm => ResolveSeverity(alarm) == DeviceAlarmSeverity.Notice);

        public string FilteredAlarmSummary =>
            HasActiveFilters
                ? $"筛选后 {VisibleAlarms.Count} 条未确认告警"
                : State.AlarmSummary;

        public string EmptyStateTitle =>
            State.HasPendingAlarms
                ? "当前筛选下暂无告警"
                : "当前没有未确认告警";

        public string EmptyStateDescription =>
            State.HasPendingAlarms
                ? "调整关键字或级别筛选后再试。"
                : "设备告警会集中显示在这里，便于统一确认、跟进和后续扩展历史告警。";

        public string HistoryEntryText => "历史告警入口待后续接入";

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshVisibleAlarms();
        }

        partial void OnSeverityFilterChanged(string value)
        {
            RefreshVisibleAlarms();
        }

        [RelayCommand]
        private Task RefreshAlarms()
        {
            return _actions.RefreshAlarmsAsync();
        }

        [RelayCommand]
        private void ResetFilters()
        {
            SearchKeyword = string.Empty;
            SeverityFilter = AllSeverityFilter;
            RefreshVisibleAlarms();
        }

        [RelayCommand]
        private Task AckAlarm(DeviceAlarmItemViewModel? item)
        {
            return _actions.AckAlarmAsync(item?.Alarm);
        }

        [RelayCommand(CanExecute = nameof(CanAckVisibleAlarms))]
        private async Task AckVisibleAlarms()
        {
            var alarms = VisibleAlarms.Select(item => item.Alarm).ToList();
            foreach (var alarm in alarms)
            {
                await _actions.AckAlarmAsync(alarm);
            }
        }

        public void Dispose()
        {
            State.PendingAlarms.CollectionChanged -= OnPendingAlarmsChanged;
            _actions.PropertyChanged -= OnActionsPropertyChanged;
        }

        private bool CanAckVisibleAlarms()
        {
            return _actions.CanAckAlarmPermission && VisibleAlarms.Count > 0;
        }

        private void OnPendingAlarmsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibleAlarms();
        }

        private void OnActionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDeviceWorkspaceActions.CanAckAlarmPermission))
            {
                AckVisibleAlarmsCommand.NotifyCanExecuteChanged();
            }
        }

        private void RefreshVisibleAlarms()
        {
            var filteredAlarms = State.PendingAlarms
                .Where(AlarmMatchesFilter)
                .OrderByDescending(alarm => alarm.AlarmTime)
                .Select(alarm => new DeviceAlarmItemViewModel(alarm, ResolveSeverity(alarm)))
                .ToList();

            VisibleAlarms.Clear();
            foreach (var alarm in filteredAlarms)
            {
                VisibleAlarms.Add(alarm);
            }

            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(HasVisibleAlarms));
            OnPropertyChanged(nameof(CriticalCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(NoticeCount));
            OnPropertyChanged(nameof(FilteredAlarmSummary));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateDescription));
            AckVisibleAlarmsCommand.NotifyCanExecuteChanged();
        }

        private bool AlarmMatchesFilter(AlarmRecordModel alarm)
        {
            var keyword = SearchKeyword.Trim();
            var matchesKeyword = string.IsNullOrWhiteSpace(keyword) ||
                (!string.IsNullOrWhiteSpace(alarm.AlarmMessage) &&
                 alarm.AlarmMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                alarm.DeviceId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase);

            if (!matchesKeyword)
            {
                return false;
            }

            return SeverityFilter switch
            {
                "严重" => ResolveSeverity(alarm) == DeviceAlarmSeverity.Critical,
                "告警" => ResolveSeverity(alarm) == DeviceAlarmSeverity.Warning,
                "提醒" => ResolveSeverity(alarm) == DeviceAlarmSeverity.Notice,
                _ => true
            };
        }

        private static DeviceAlarmSeverity ResolveSeverity(AlarmRecordModel alarm)
        {
            var message = alarm.AlarmMessage ?? string.Empty;

            if (ContainsAny(message, "紧急", "急停", "故障", "停机", "危险", "严重"))
            {
                return DeviceAlarmSeverity.Critical;
            }

            if (ContainsAny(message, "报警", "告警", "异常", "超限", "断连", "失败"))
            {
                return DeviceAlarmSeverity.Warning;
            }

            return DeviceAlarmSeverity.Notice;
        }

        private static bool ContainsAny(string message, params string[] tokens)
        {
            return tokens.Any(token => message.Contains(token, StringComparison.OrdinalIgnoreCase));
        }
    }
}

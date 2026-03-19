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
        private const string AllSeverityFilter = "\u5168\u90e8";
        private readonly IDeviceWorkspaceActions _actions;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _severityFilter = AllSeverityFilter;

        public DeviceAlarmViewModel(DeviceWorkspaceState state, IDeviceWorkspaceActions actions)
        {
            State = state;
            _actions = actions;
            PageTitle = "\u8bbe\u5907\u544a\u8b66";

            SeverityFilterOptions = new[]
            {
                AllSeverityFilter,
                "\u4e25\u91cd",
                "\u544a\u8b66",
                "\u63d0\u9192"
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
                ? $"\u7b5b\u9009\u540e {VisibleAlarms.Count} \u6761\u672a\u786e\u8ba4\u544a\u8b66"
                : State.AlarmSummary;

        public string EmptyStateTitle =>
            State.HasPendingAlarms
                ? "\u5f53\u524d\u7b5b\u9009\u4e0b\u6682\u65e0\u544a\u8b66"
                : "\u5f53\u524d\u6ca1\u6709\u672a\u786e\u8ba4\u544a\u8b66";

        public string EmptyStateDescription =>
            State.HasPendingAlarms
                ? "\u8c03\u6574\u5173\u952e\u5b57\u6216\u7ea7\u522b\u7b5b\u9009\u540e\u518d\u8bd5\u3002"
                : "\u8bbe\u5907\u544a\u8b66\u4f1a\u96c6\u4e2d\u663e\u793a\u5728\u8fd9\u91cc\uff0c\u4fbf\u4e8e\u7edf\u4e00\u786e\u8ba4\u3001\u8ddf\u8fdb\u548c\u540e\u7eed\u6269\u5c55\u5386\u53f2\u544a\u8b66\u3002";

        public string HistoryEntryText => "\u5386\u53f2\u544a\u8b66\u5165\u53e3\u5f85\u540e\u7eed\u63a5\u5165";

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
                "\u4e25\u91cd" => ResolveSeverity(alarm) == DeviceAlarmSeverity.Critical,
                "\u544a\u8b66" => ResolveSeverity(alarm) == DeviceAlarmSeverity.Warning,
                "\u63d0\u9192" => ResolveSeverity(alarm) == DeviceAlarmSeverity.Notice,
                _ => true
            };
        }

        private static DeviceAlarmSeverity ResolveSeverity(AlarmRecordModel alarm)
        {
            var message = alarm.AlarmMessage ?? string.Empty;

            if (ContainsAny(message, "\u7d27\u6025", "\u6025\u505c", "\u6545\u969c", "\u505c\u673a", "\u5371\u9669", "\u4e25\u91cd"))
            {
                return DeviceAlarmSeverity.Critical;
            }

            if (ContainsAny(message, "\u62a5\u8b66", "\u544a\u8b66", "\u5f02\u5e38", "\u8d85\u9650", "\u65ad\u8fde", "\u5931\u8d25"))
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

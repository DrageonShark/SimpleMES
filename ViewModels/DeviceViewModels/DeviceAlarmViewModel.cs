using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public partial class DeviceAlarmViewModel : DialogViewModelBase, IDisposable
    {
        private const string AlarmSection = "未确认告警";
        private const string EventSection = "事件历史";
        private const string AllSeverityFilter = "全部";
        private const string AllEventStatusFilter = "全部状态";
        private const string AllDeviceFilter = "全部设备";
        private const string AllEventTypeFilter = "全部类型";
        private const string AllTimeRangeFilter = "全部时间";
        private const int EventPageSize = 40;

        private static readonly (string EventType, string DisplayName)[] EventTypeMappings =
        {
            ("DeviceAdded", "设备接入"),
            ("ConfigUpdated", "配置更新"),
            ("DeviceEnabled", "启用设备"),
            ("DeviceDisabled", "停用设备"),
            ("FaultRaised", "故障触发"),
            ("FaultRecovered", "故障恢复"),
            ("CommunicationRestored", "通信恢复"),
            ("AlarmAcknowledged", "告警确认"),
            ("EventConfirmed", "事件确认")
        };

        private readonly IDeviceWorkspaceActions _actions;
        private bool _suppressEventReload;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _severityFilter = AllSeverityFilter;

        [ObservableProperty]
        private string _eventSeverityFilter = AllSeverityFilter;

        [ObservableProperty]
        private string _eventStatusFilter = AllEventStatusFilter;

        [ObservableProperty]
        private string _selectedSection = AlarmSection;

        [ObservableProperty]
        private string _selectedDeviceFilter = AllDeviceFilter;

        [ObservableProperty]
        private string _selectedEventTypeFilter = AllEventTypeFilter;

        [ObservableProperty]
        private string _selectedTimeRangeFilter = AllTimeRangeFilter;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmSelectedEventCommand))]
        private DeviceEventItemViewModel? _selectedEvent;

        [ObservableProperty]
        private string _eventResolutionNote = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadMoreEventsCommand))]
        private bool _isLoadingEvents;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadMoreEventsCommand))]
        private int _eventTotalCount;

        public DeviceAlarmViewModel(DeviceWorkspaceState state, IDeviceWorkspaceActions actions)
        {
            State = state;
            _actions = actions;
            PageTitle = "设备事件中心";

            SectionOptions = new[] { AlarmSection, EventSection };
            SeverityFilterOptions = new[] { AllSeverityFilter, "严重", "告警", "提醒" };
            EventSeverityFilterOptions = new[] { AllSeverityFilter, "严重", "告警", "信息" };
            EventStatusFilterOptions = new[] { AllEventStatusFilter, "待处理", "已恢复待确认", "已确认", "已记录" };
            TimeRangeFilterOptions = new[] { AllTimeRangeFilter, "今天", "近24小时", "近3天", "近7天", "近30天" };

            EventDeviceFilterOptions.Add(AllDeviceFilter);
            EventTypeFilterOptions.Add(AllEventTypeFilter);

            State.PendingAlarms.CollectionChanged += OnPendingAlarmsChanged;
            State.RecentEvents.CollectionChanged += OnRecentEventsChanged;
            State.ListDeviceDto.CollectionChanged += OnDeviceListChanged;
            _actions.PropertyChanged += OnActionsPropertyChanged;

            RefreshVisibleAlarms();
            RebuildEventFilterOptions();
            _ = _actions.RefreshRecentEventsAsync();
        }

        public DeviceWorkspaceState State { get; }
        public IDeviceWorkspaceActions Actions => _actions;
        public IReadOnlyList<string> SectionOptions { get; }
        public IReadOnlyList<string> SeverityFilterOptions { get; }
        public IReadOnlyList<string> EventSeverityFilterOptions { get; }
        public IReadOnlyList<string> EventStatusFilterOptions { get; }
        public IReadOnlyList<string> TimeRangeFilterOptions { get; }
        public ObservableCollection<string> EventDeviceFilterOptions { get; } = new();
        public ObservableCollection<string> EventTypeFilterOptions { get; } = new();
        public ObservableCollection<DeviceAlarmItemViewModel> VisibleAlarms { get; } = new();
        public ObservableCollection<DeviceEventItemViewModel> VisibleEvents { get; } = new();

        public bool IsAlarmSection => SelectedSection == AlarmSection;
        public bool IsEventSection => SelectedSection == EventSection;
        public bool HasVisibleAlarms => VisibleAlarms.Count > 0;
        public bool HasVisibleEvents => VisibleEvents.Count > 0;
        public bool HasSelectedEvent => SelectedEvent is not null;
        public bool HasMoreEvents => VisibleEvents.Count < EventTotalCount;
        public int EventLoadedCount => VisibleEvents.Count;

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchKeyword)
            || SeverityFilter != AllSeverityFilter
            || EventSeverityFilter != AllSeverityFilter
            || EventStatusFilter != AllEventStatusFilter
            || SelectedDeviceFilter != AllDeviceFilter
            || SelectedEventTypeFilter != AllEventTypeFilter
            || SelectedTimeRangeFilter != AllTimeRangeFilter;

        public int CriticalCount => State.PendingAlarms.Count(alarm => ResolveAlarmSeverity(alarm) == DeviceAlarmSeverity.Critical);
        public int WarningCount => State.PendingAlarms.Count(alarm => ResolveAlarmSeverity(alarm) == DeviceAlarmSeverity.Warning);
        public int NoticeCount => State.PendingAlarms.Count(alarm => ResolveAlarmSeverity(alarm) == DeviceAlarmSeverity.Notice);

        public int EventCriticalCount => VisibleEvents.Count(item => item.Severity == DeviceEventSeverity.Critical);
        public int EventWarningCount => VisibleEvents.Count(item => item.Severity == DeviceEventSeverity.Warning);
        public int EventPendingCount => VisibleEvents.Count(item => item.RequiresManualConfirmation && !item.IsConfirmed);

        public string FilteredSummary => IsAlarmSection
            ? (HasActiveFilters ? $"筛选后 {VisibleAlarms.Count} 条未确认告警" : State.AlarmSummary)
            : $"已加载 {EventLoadedCount} / {EventTotalCount} 条事件";

        public string EmptyStateTitle => IsAlarmSection
            ? (State.HasPendingAlarms ? "当前筛选下暂无告警" : "当前没有未确认告警")
            : "当前筛选下暂无事件";

        public string EmptyStateDescription => IsAlarmSection
            ? (State.HasPendingAlarms
                ? "调整关键字或告警等级后再试。"
                : "设备告警会集中显示在这里，便于统一确认和跟进。")
            : "事件历史已经改为仓储查询。调整设备、事件类型、时间范围或处理状态后再试。";

        public string SearchHint => IsAlarmSection ? "搜索告警内容 / 设备编号" : "搜索事件、设备、区域或关联告警";
        public string RefreshButtonText => IsAlarmSection ? "刷新告警" : "刷新事件";
        public string EventDetailTitle => SelectedEvent?.EventTypeText ?? "选择一条事件查看详情";
        public string EventDetailSubtitle => SelectedEvent?.DeviceName ?? "右侧详情区用于人工确认闭环。";
        public string LoadMoreButtonText => IsLoadingEvents ? "加载中..." : "加载更多";

        partial void OnSearchKeywordChanged(string value)
        {
            if (IsAlarmSection)
            {
                RefreshVisibleAlarms();
                return;
            }

            TriggerEventQueryReset();
        }

        partial void OnSeverityFilterChanged(string value)
        {
            RefreshVisibleAlarms();
        }

        partial void OnEventSeverityFilterChanged(string value)
        {
            TriggerEventQueryReset();
        }

        partial void OnEventStatusFilterChanged(string value)
        {
            TriggerEventQueryReset();
        }

        partial void OnSelectedDeviceFilterChanged(string value)
        {
            TriggerEventQueryReset();
        }

        partial void OnSelectedEventTypeFilterChanged(string value)
        {
            TriggerEventQueryReset();
        }

        partial void OnSelectedTimeRangeFilterChanged(string value)
        {
            TriggerEventQueryReset();
        }

        partial void OnSelectedSectionChanged(string value)
        {
            OnPropertyChanged(nameof(IsAlarmSection));
            OnPropertyChanged(nameof(IsEventSection));
            OnPropertyChanged(nameof(FilteredSummary));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateDescription));
            OnPropertyChanged(nameof(SearchHint));
            OnPropertyChanged(nameof(RefreshButtonText));

            if (IsEventSection)
            {
                _ = ReloadEventHistoryAsync();
            }
        }

        partial void OnSelectedEventChanged(DeviceEventItemViewModel? value)
        {
            EventResolutionNote = value?.ResolutionNote ?? string.Empty;
            OnPropertyChanged(nameof(HasSelectedEvent));
            OnPropertyChanged(nameof(EventDetailTitle));
            OnPropertyChanged(nameof(EventDetailSubtitle));
        }

        partial void OnIsLoadingEventsChanged(bool value)
        {
            LoadMoreEventsCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(LoadMoreButtonText));
        }

        partial void OnEventTotalCountChanged(int value)
        {
            OnPropertyChanged(nameof(HasMoreEvents));
            OnPropertyChanged(nameof(FilteredSummary));
            LoadMoreEventsCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void ShowAlarmSection()
        {
            SelectedSection = AlarmSection;
        }

        [RelayCommand]
        private void ShowEventSection()
        {
            SelectedSection = EventSection;
        }

        [RelayCommand]
        private async Task RefreshCurrentView()
        {
            if (IsAlarmSection)
            {
                await _actions.RefreshAlarmsAsync();
                return;
            }

            await ReloadEventHistoryAsync();
        }

        [RelayCommand]
        private void ResetFilters()
        {
            _suppressEventReload = true;
            SearchKeyword = string.Empty;
            SeverityFilter = AllSeverityFilter;
            EventSeverityFilter = AllSeverityFilter;
            EventStatusFilter = AllEventStatusFilter;
            SelectedDeviceFilter = AllDeviceFilter;
            SelectedEventTypeFilter = AllEventTypeFilter;
            SelectedTimeRangeFilter = AllTimeRangeFilter;
            _suppressEventReload = false;

            RefreshVisibleAlarms();
            if (IsEventSection)
            {
                _ = ReloadEventHistoryAsync();
            }
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

        [RelayCommand(CanExecute = nameof(CanConfirmSelectedEvent))]
        private async Task ConfirmSelectedEvent()
        {
            if (SelectedEvent is null)
            {
                return;
            }

            var currentEventId = SelectedEvent.EventId;
            await _actions.ConfirmDeviceEventAsync(SelectedEvent.DeviceEvent, EventResolutionNote);
            await _actions.RefreshAlarmsAsync();
            await ReloadEventHistoryAsync(Math.Max(EventPageSize, VisibleEvents.Count), currentEventId);
        }

        [RelayCommand(CanExecute = nameof(CanLoadMoreEvents))]
        private async Task LoadMoreEvents()
        {
            await QueryEventHistoryAsync(VisibleEvents.Count, EventPageSize, append: true, selectedEventId: SelectedEvent?.EventId);
        }

        public void Dispose()
        {
            State.PendingAlarms.CollectionChanged -= OnPendingAlarmsChanged;
            State.RecentEvents.CollectionChanged -= OnRecentEventsChanged;
            State.ListDeviceDto.CollectionChanged -= OnDeviceListChanged;
            _actions.PropertyChanged -= OnActionsPropertyChanged;
        }

        private bool CanAckVisibleAlarms()
        {
            return _actions.CanAckAlarmPermission && VisibleAlarms.Count > 0;
        }

        private bool CanConfirmSelectedEvent()
        {
            return _actions.CanAckAlarmPermission && SelectedEvent?.CanConfirm == true;
        }

        private bool CanLoadMoreEvents()
        {
            return IsEventSection && !IsLoadingEvents && HasMoreEvents;
        }

        private void OnPendingAlarmsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibleAlarms();
        }

        private void OnRecentEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsEventSection && !_suppressEventReload && !IsLoadingEvents)
            {
                _ = ReloadEventHistoryAsync(Math.Max(EventPageSize, VisibleEvents.Count), SelectedEvent?.EventId);
            }
        }

        private void OnDeviceListChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildEventFilterOptions();
        }

        private void OnActionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IDeviceWorkspaceActions.CanAckAlarmPermission))
            {
                AckVisibleAlarmsCommand.NotifyCanExecuteChanged();
                ConfirmSelectedEventCommand.NotifyCanExecuteChanged();
            }
        }

        private void RefreshVisibleAlarms()
        {
            var filteredAlarms = State.PendingAlarms
                .Where(AlarmMatchesFilter)
                .OrderByDescending(alarm => alarm.AlarmTime)
                .Select(alarm => new DeviceAlarmItemViewModel(alarm, ResolveAlarmSeverity(alarm)))
                .ToList();

            SyncCollection(VisibleAlarms, filteredAlarms);

            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(HasVisibleAlarms));
            OnPropertyChanged(nameof(CriticalCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(NoticeCount));
            OnPropertyChanged(nameof(FilteredSummary));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateDescription));
            AckVisibleAlarmsCommand.NotifyCanExecuteChanged();
        }

        private async Task ReloadEventHistoryAsync(int? take = null, long? selectedEventId = null)
        {
            await QueryEventHistoryAsync(0, take ?? EventPageSize, append: false, selectedEventId: selectedEventId);
        }

        private async Task QueryEventHistoryAsync(int skip, int take, bool append, long? selectedEventId)
        {
            if (!IsEventSection && !append)
            {
                return;
            }

            IsLoadingEvents = true;
            try
            {
                var result = await _actions.QueryDeviceEventsAsync(BuildEventCriteria(skip, take));
                EventTotalCount = result.TotalCount;

                var mappedItems = result.Items
                    .Select(item => new DeviceEventItemViewModel(item, ResolveEventSeverity(item)))
                    .ToList();

                if (append)
                {
                    foreach (var item in mappedItems)
                    {
                        VisibleEvents.Add(item);
                    }
                }
                else
                {
                    SyncCollection(VisibleEvents, mappedItems);
                }

                if (VisibleEvents.Count == 0)
                {
                    SelectedEvent = null;
                }
                else if (selectedEventId.HasValue)
                {
                    SelectedEvent = VisibleEvents.FirstOrDefault(item => item.EventId == selectedEventId.Value) ?? VisibleEvents[0];
                }
                else if (SelectedEvent is null || !VisibleEvents.Any(item => item.EventId == SelectedEvent.EventId))
                {
                    SelectedEvent = VisibleEvents[0];
                }

                OnPropertyChanged(nameof(HasActiveFilters));
                OnPropertyChanged(nameof(HasVisibleEvents));
                OnPropertyChanged(nameof(EventLoadedCount));
                OnPropertyChanged(nameof(EventCriticalCount));
                OnPropertyChanged(nameof(EventWarningCount));
                OnPropertyChanged(nameof(EventPendingCount));
                OnPropertyChanged(nameof(FilteredSummary));
                OnPropertyChanged(nameof(EmptyStateTitle));
                OnPropertyChanged(nameof(EmptyStateDescription));
                OnPropertyChanged(nameof(HasMoreEvents));
                ConfirmSelectedEventCommand.NotifyCanExecuteChanged();
                LoadMoreEventsCommand.NotifyCanExecuteChanged();
            }
            finally
            {
                IsLoadingEvents = false;
            }
        }

        private DeviceEventQueryCriteria BuildEventCriteria(int skip, int take)
        {
            return new DeviceEventQueryCriteria
            {
                Keyword = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim(),
                EventLevel = MapSeverityFilter(EventSeverityFilter),
                ProcessingStatus = MapProcessingStatusFilter(EventStatusFilter),
                DeviceId = TryParseDeviceFilter(SelectedDeviceFilter),
                EventType = MapEventTypeDisplayToRaw(SelectedEventTypeFilter),
                OccurredFrom = MapOccurredFrom(SelectedTimeRangeFilter),
                Skip = skip,
                Take = take
            };
        }

        private void RebuildEventFilterOptions()
        {
            var deviceOptions = State.ListDeviceDto
                .OrderBy(device => device.DeviceId)
                .Select(device => FormatDeviceFilter(device.DeviceId, device.DeviceName))
                .ToList();

            ResetFilterOptions(EventDeviceFilterOptions, AllDeviceFilter, deviceOptions);
            ResetFilterOptions(EventTypeFilterOptions, AllEventTypeFilter, EventTypeMappings.Select(item => item.DisplayName));

            if (!EventDeviceFilterOptions.Contains(SelectedDeviceFilter))
            {
                SelectedDeviceFilter = AllDeviceFilter;
            }

            if (!EventTypeFilterOptions.Contains(SelectedEventTypeFilter))
            {
                SelectedEventTypeFilter = AllEventTypeFilter;
            }
        }

        private void TriggerEventQueryReset()
        {
            if (_suppressEventReload || !IsEventSection)
            {
                return;
            }

            _ = ReloadEventHistoryAsync();
        }

        private bool AlarmMatchesFilter(AlarmRecordModel alarm)
        {
            var keyword = SearchKeyword.Trim();
            var matchesKeyword = string.IsNullOrWhiteSpace(keyword)
                || (!string.IsNullOrWhiteSpace(alarm.AlarmMessage) && alarm.AlarmMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || alarm.DeviceId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase);

            if (!matchesKeyword)
            {
                return false;
            }

            return SeverityFilter switch
            {
                "严重" => ResolveAlarmSeverity(alarm) == DeviceAlarmSeverity.Critical,
                "告警" => ResolveAlarmSeverity(alarm) == DeviceAlarmSeverity.Warning,
                "提醒" => ResolveAlarmSeverity(alarm) == DeviceAlarmSeverity.Notice,
                _ => true
            };
        }

        private static string? MapSeverityFilter(string severityFilter)
        {
            return severityFilter switch
            {
                "严重" => "Critical",
                "告警" => "Warning",
                "信息" => "Info",
                _ => null
            };
        }

        private static string? MapProcessingStatusFilter(string processingStatus)
        {
            return processingStatus switch
            {
                "待处理" => "Pending",
                "已恢复待确认" => "AwaitingConfirmation",
                "已确认" => "Confirmed",
                "已记录" => "Recorded",
                _ => null
            };
        }

        private static int? TryParseDeviceFilter(string selectedDeviceFilter)
        {
            if (string.IsNullOrWhiteSpace(selectedDeviceFilter) || selectedDeviceFilter == AllDeviceFilter || !selectedDeviceFilter.StartsWith("#", StringComparison.Ordinal))
            {
                return null;
            }

            var splitIndex = selectedDeviceFilter.IndexOf(' ');
            var idText = splitIndex > 1
                ? selectedDeviceFilter[1..splitIndex]
                : selectedDeviceFilter[1..];

            return int.TryParse(idText, out var deviceId) ? deviceId : null;
        }

        private static string? MapEventTypeDisplayToRaw(string selectedEventType)
        {
            if (string.IsNullOrWhiteSpace(selectedEventType) || selectedEventType == AllEventTypeFilter)
            {
                return null;
            }

            return EventTypeMappings.FirstOrDefault(item => item.DisplayName == selectedEventType).EventType;
        }

        private static DateTime? MapOccurredFrom(string selectedTimeRange)
        {
            var now = DateTime.Now;
            return selectedTimeRange switch
            {
                "今天" => now.Date,
                "近24小时" => now.AddHours(-24),
                "近3天" => now.AddDays(-3),
                "近7天" => now.AddDays(-7),
                "近30天" => now.AddDays(-30),
                _ => null
            };
        }

        private static string FormatDeviceFilter(int deviceId, string deviceName)
        {
            return $"#{deviceId} {deviceName}";
        }

        private static void ResetFilterOptions(ObservableCollection<string> target, string first, IEnumerable<string> items)
        {
            target.Clear();
            target.Add(first);
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private static void SyncCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

        private static DeviceAlarmSeverity ResolveAlarmSeverity(AlarmRecordModel alarm)
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

        private static DeviceEventSeverity ResolveEventSeverity(DeviceEventDto deviceEvent)
        {
            return deviceEvent.EventLevel?.Trim().ToLowerInvariant() switch
            {
                "critical" => DeviceEventSeverity.Critical,
                "warning" => DeviceEventSeverity.Warning,
                _ => DeviceEventSeverity.Info
            };
        }

        private static bool ContainsAny(string message, params string[] tokens)
        {
            return tokens.Any(token => message.Contains(token, StringComparison.OrdinalIgnoreCase));
        }
    }
}

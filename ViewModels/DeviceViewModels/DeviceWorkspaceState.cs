using CommunityToolkit.Mvvm.ComponentModel;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.State;
using System.Collections.ObjectModel;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public partial class DeviceWorkspaceState : ObservableObject
    {
        private const string AllFilter = "全部";
        private const string RunningFilter = "运行";
        private const string DisconnectedFilter = "断连";
        private const string FaultFilter = "故障";
        private const string DisabledFilter = "停用";
        private const int AttentionLimit = 5;
        private const int SpotlightLimit = 3;
        private const int BoardRecentLimit = 6;
        private const int EventHistoryLimit = 120;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _stateFilter = AllFilter;

        [ObservableProperty]
        private int _runningCount;

        [ObservableProperty]
        private int _disconnectedCount;

        [ObservableProperty]
        private int _faultCount;

        [ObservableProperty]
        private int _disabledCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AlarmPanelToggleText))]
        [NotifyPropertyChangedFor(nameof(AlarmPanelToggleContent))]
        private bool _isAlarmPanelCollapsed;

        public DeviceWorkspaceState()
        {
            StateFilterOptions = new[] { AllFilter, RunningFilter, DisconnectedFilter, FaultFilter, DisabledFilter };
        }

        public ObservableCollection<DeviceDto> ListDeviceDto { get; } = new();
        public ObservableCollection<DeviceDto> FilteredDeviceDto { get; } = new();
        public ObservableCollection<AlarmRecordModel> PendingAlarms { get; } = new();
        public ObservableCollection<DeviceDto> AttentionDevices { get; } = new();
        public ObservableCollection<DeviceEventDto> RecentEvents { get; } = new();

        public IReadOnlyList<string> StateFilterOptions { get; }

        public int TotalDeviceCount => ListDeviceDto.Count;
        public int AttentionDeviceCount => FaultCount + DisconnectedCount;
        public int OnlineDeviceCount => RunningCount;
        public int EnabledDeviceCount => ListDeviceDto.Count(device => device.IsEnabled);

        public bool HasDevices => ListDeviceDto.Count > 0;
        public bool HasFilteredDevices => FilteredDeviceDto.Count > 0;
        public bool HasActiveFilters => !string.IsNullOrWhiteSpace(SearchKeyword) || StateFilter != AllFilter;
        public bool HasPendingAlarms => PendingAlarms.Count > 0;
        public bool HasAttentionDevices => AttentionDevices.Count > 0;
        public bool HasRecentEvents => RecentEvents.Count > 0;
        public IReadOnlyList<DeviceDto> SpotlightDevices => AttentionDevices.Take(SpotlightLimit).ToList();
        public IReadOnlyList<DeviceEventDto> BoardRecentEvents => RecentEvents.Take(BoardRecentLimit).ToList();
        public bool HasSpotlightOverflow => AttentionDevices.Count > SpotlightLimit;
        public int SpotlightOverflowCount => Math.Max(0, AttentionDevices.Count - SpotlightLimit);

        public string AlarmPanelToggleContent => IsAlarmPanelCollapsed ? "<" : ">";
        public string AlarmPanelToggleText => IsAlarmPanelCollapsed ? "展开告警侧栏" : "收起告警侧栏";

        public string DeviceOverviewSummary =>
            HasActiveFilters
                ? $"当前设备 {ListDeviceDto.Count} 台，筛选结果 {FilteredDeviceDto.Count} 台。"
                : $"当前共 {ListDeviceDto.Count} 台设备，已启用 {EnabledDeviceCount} 台。";

        public string ManagementEmptyTitle =>
            HasActiveFilters ? "没有匹配的设备" : "暂无设备数据";

        public string ManagementEmptyDescription =>
            HasActiveFilters
                ? "调整搜索词或状态筛选后再试。"
                : "设备接入后，这里会展示实时状态和运维入口。";

        public string AlarmSummary =>
            HasPendingAlarms ? $"当前有 {PendingAlarms.Count} 条未确认告警" : "当前没有未确认告警";

        public string LatestAlarmPreview =>
            PendingAlarms.Count > 0 ? PendingAlarms[0].AlarmMessage : "暂无未确认告警";

        public string BoardHeadline =>
            FaultCount > 0
                ? $"有 {FaultCount} 台设备故障，建议优先派修"
                : DisconnectedCount > 0
                    ? $"有 {DisconnectedCount} 台设备断连，建议检查通信"
                    : HasDevices
                        ? "设备运行整体稳定"
                        : "等待设备接入";

        public string BoardDescription =>
            HasDevices
                ? $"运行 {RunningCount} 台，异常 {AttentionDeviceCount} 台，停用 {DisabledCount} 台。异常卡片已按持续时长排序。"
                : "接入设备后，这里会展示实时状态、异常摘要和快捷操作。";

        public string AttentionSummary =>
            HasAttentionDevices
                ? $"共有 {AttentionDevices.Count} 台重点设备需要处理"
                : "当前没有需要优先处理的设备";

        public string SpotlightOverflowSummary =>
            HasSpotlightOverflow
                ? $"另有 {SpotlightOverflowCount} 台异常设备待跟进"
                : "当前重点设备已全部展开";

        public string RecentEventSummary =>
            HasRecentEvents
                ? "按照真实设备事件流展示最近状态变化"
                : "暂无可展示的设备事件";

        partial void OnSearchKeywordChanged(string value)
        {
            RefreshDeviceFilter();
        }

        partial void OnStateFilterChanged(string value)
        {
            RefreshDeviceFilter();
        }

        public void ApplyLatestDeviceSnapshot(IEnumerable<DeviceDto> latestDevices)
        {
            var latestList = latestDevices.ToList();
            if (ListDeviceDto.Count == 0)
            {
                foreach (var device in latestList.OrderBy(d => d.DeviceId))
                {
                    ListDeviceDto.Add(device);
                }
            }
            else
            {
                foreach (var device in latestList)
                {
                    var existing = ListDeviceDto.FirstOrDefault(d => d.DeviceId == device.DeviceId);
                    if (existing is null)
                    {
                        ListDeviceDto.Add(device);
                        continue;
                    }

                    existing.DeviceName = device.DeviceName;
                    existing.DeviceCode = device.DeviceCode;
                    existing.DeviceType = device.DeviceType;
                    existing.IpAddress = device.IpAddress;
                    existing.Port = device.Port;
                    existing.SerialPort = device.SerialPort;
                    existing.SlaveId = device.SlaveId;
                    existing.WorkshopName = device.WorkshopName;
                    existing.LineName = device.LineName;
                    existing.StationName = device.StationName;
                    existing.IsEnabled = device.IsEnabled;
                    existing.Criticality = device.Criticality;
                    existing.Temperature = device.Temperature;
                    existing.Pressure = device.Pressure;
                    existing.Speed = device.Speed;
                    existing.DeviceState = device.DeviceState;
                    existing.LastUpdateTime = device.LastUpdateTime;
                    existing.LastHeartbeatTime = device.LastHeartbeatTime;
                    existing.LastStateChangeTime = device.LastStateChangeTime;
                    existing.CurrentOrderNo = device.CurrentOrderNo;
                }
            }

            RefreshDeviceFilter();
        }

        public void ApplyRecentEvents(IEnumerable<DeviceEventDto> recentEvents)
        {
            SyncCollection(RecentEvents, recentEvents.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.EventId).Take(EventHistoryLimit));
            OnPropertyChanged(nameof(HasRecentEvents));
            OnPropertyChanged(nameof(BoardRecentEvents));
            OnPropertyChanged(nameof(RecentEventSummary));
        }

        public void RefreshDeviceFilter()
        {
            IEnumerable<DeviceDto> query = ListDeviceDto;

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var keyword = SearchKeyword.Trim();
                query = query.Where(device =>
                    (!string.IsNullOrWhiteSpace(device.DeviceName) && device.DeviceName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.DeviceCode) && device.DeviceCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.DeviceType) && device.DeviceType.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.WorkshopName) && device.WorkshopName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.LineName) && device.LineName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.StationName) && device.StationName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.IpAddress) && device.IpAddress.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(device.SerialPort) && device.SerialPort.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            query = StateFilter switch
            {
                RunningFilter => query.Where(device => device.DeviceState == DeviceState.Running),
                DisconnectedFilter => query.Where(device => device.DeviceState == DeviceState.Disconnected),
                FaultFilter => query.Where(device => device.DeviceState == DeviceState.Fault),
                DisabledFilter => query.Where(device => device.DeviceState == DeviceState.Disabled),
                _ => query
            };

            SyncCollection(FilteredDeviceDto, query.OrderBy(device => device.DeviceId));

            RunningCount = ListDeviceDto.Count(device => device.DeviceState == DeviceState.Running);
            DisconnectedCount = ListDeviceDto.Count(device => device.DeviceState == DeviceState.Disconnected);
            FaultCount = ListDeviceDto.Count(device => device.DeviceState == DeviceState.Fault);
            DisabledCount = ListDeviceDto.Count(device => device.DeviceState == DeviceState.Disabled);

            RefreshBoardCollections();
            RaiseDeviceStateChanged();
        }

        public void ResetFilters()
        {
            SearchKeyword = string.Empty;
            StateFilter = AllFilter;
            RefreshDeviceFilter();
        }

        public void ReplacePendingAlarms(IEnumerable<AlarmRecordModel> alarms)
        {
            SyncCollection(PendingAlarms, alarms.OrderByDescending(alarm => alarm.AlarmTime));
            RaiseAlarmStateChanged();
        }

        public void RemovePendingAlarm(AlarmRecordModel alarm)
        {
            PendingAlarms.Remove(alarm);
            RaiseAlarmStateChanged();
        }

        public void ToggleAlarmPanel()
        {
            IsAlarmPanelCollapsed = !IsAlarmPanelCollapsed;
        }

        public void NotifyDeviceMetadataChanged()
        {
            RefreshDeviceFilter();
        }

        private void RefreshBoardCollections()
        {
            var attentionDevices = ListDeviceDto
                .Where(device => device.DeviceState is DeviceState.Fault or DeviceState.Disconnected)
                .OrderBy(device => device.DeviceState == DeviceState.Fault ? 0 : 1)
                .ThenBy(device => device.LastStateChangeTime ?? device.LastUpdateTime)
                .ThenByDescending(device => device.Criticality)
                .ThenBy(device => device.DeviceId)
                .Take(AttentionLimit);
            SyncCollection(AttentionDevices, attentionDevices);
        }

        private void RaiseDeviceStateChanged()
        {
            OnPropertyChanged(nameof(TotalDeviceCount));
            OnPropertyChanged(nameof(AttentionDeviceCount));
            OnPropertyChanged(nameof(OnlineDeviceCount));
            OnPropertyChanged(nameof(EnabledDeviceCount));
            OnPropertyChanged(nameof(HasDevices));
            OnPropertyChanged(nameof(HasFilteredDevices));
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(HasAttentionDevices));
            OnPropertyChanged(nameof(HasRecentEvents));
            OnPropertyChanged(nameof(BoardRecentEvents));
            OnPropertyChanged(nameof(SpotlightDevices));
            OnPropertyChanged(nameof(HasSpotlightOverflow));
            OnPropertyChanged(nameof(SpotlightOverflowCount));
            OnPropertyChanged(nameof(DeviceOverviewSummary));
            OnPropertyChanged(nameof(ManagementEmptyTitle));
            OnPropertyChanged(nameof(ManagementEmptyDescription));
            OnPropertyChanged(nameof(BoardHeadline));
            OnPropertyChanged(nameof(BoardDescription));
            OnPropertyChanged(nameof(AttentionSummary));
            OnPropertyChanged(nameof(SpotlightOverflowSummary));
            OnPropertyChanged(nameof(RecentEventSummary));
        }

        private void RaiseAlarmStateChanged()
        {
            OnPropertyChanged(nameof(HasPendingAlarms));
            OnPropertyChanged(nameof(AlarmSummary));
            OnPropertyChanged(nameof(LatestAlarmPreview));
        }

        private static void SyncCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }
    }
}

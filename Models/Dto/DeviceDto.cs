using CommunityToolkit.Mvvm.ComponentModel;
using SimpleMES.Services.State;

namespace SimpleMES.Models.Dto
{
    public partial class DeviceDto : ObservableObject
    {
        [ObservableProperty] private int _deviceId;
        [ObservableProperty] private string _deviceName = string.Empty;
        [ObservableProperty] private string? _deviceCode;
        [ObservableProperty] private string? _deviceType;

        [NotifyPropertyChangedFor(nameof(DeviceAddress))]
        [ObservableProperty] private string _ipAddress = string.Empty;

        [ObservableProperty] private int? _port;

        [NotifyPropertyChangedFor(nameof(DeviceAddress))]
        [ObservableProperty] private string _serialPort = string.Empty;

        [ObservableProperty] private byte? _slaveId;

        [NotifyPropertyChangedFor(nameof(LocationSummary))]
        [ObservableProperty] private string? _workshopName;

        [NotifyPropertyChangedFor(nameof(LocationSummary))]
        [ObservableProperty] private string? _lineName;

        [NotifyPropertyChangedFor(nameof(LocationSummary))]
        [ObservableProperty] private string? _stationName;

        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private byte _criticality;
        [ObservableProperty] private decimal? _temperature;
        [ObservableProperty] private decimal? _pressure;
        [ObservableProperty] private int _speed;

        [NotifyPropertyChangedFor(nameof(StateAgeSummary))]
        [NotifyPropertyChangedFor(nameof(HeartbeatSummary))]
        [ObservableProperty] private DeviceState _deviceState;

        [NotifyPropertyChangedFor(nameof(StateAgeSummary))]
        [NotifyPropertyChangedFor(nameof(HeartbeatSummary))]
        [ObservableProperty] private DateTime _lastUpdateTime;

        [NotifyPropertyChangedFor(nameof(StateAgeSummary))]
        [NotifyPropertyChangedFor(nameof(HeartbeatSummary))]
        [ObservableProperty] private DateTime? _lastHeartbeatTime;

        [NotifyPropertyChangedFor(nameof(StateAgeSummary))]
        [ObservableProperty] private DateTime? _lastStateChangeTime;

        [NotifyPropertyChangedFor(nameof(CurrentOrderSummary))]
        [ObservableProperty] private string? _currentOrderNo;

        public string DeviceAddress =>
            !string.IsNullOrWhiteSpace(IpAddress)
                ? IpAddress
                : !string.IsNullOrWhiteSpace(SerialPort)
                    ? SerialPort
                    : "未配置地址";

        public string LocationSummary
        {
            get
            {
                var parts = new[] { WorkshopName, LineName, StationName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
                return parts.Length > 0 ? string.Join(" / ", parts) : "未配置区域";
            }
        }

        public string HeartbeatSummary
        {
            get
            {
                if (DeviceState == DeviceState.Disabled)
                {
                    return "设备已停用";
                }

                return LastHeartbeatTime.HasValue
                    ? $"最后通信时间 {LastHeartbeatTime:MM-dd HH:mm:ss}"
                    : $"最后更新 {LastUpdateTime:MM-dd HH:mm:ss}";
            }
        }

        public string StateAgeSummary
        {
            get
            {
                var anchor = LastStateChangeTime ?? LastHeartbeatTime ?? LastUpdateTime;
                var durationText = FormatDuration(DateTime.Now - anchor);

                return DeviceState switch
                {
                    DeviceState.Fault => $"故障持续 {durationText}",
                    DeviceState.Disconnected => $"断连持续 {durationText}",
                    DeviceState.Disabled => LastStateChangeTime.HasValue
                        ? $"停用于 {LastStateChangeTime:MM-dd HH:mm}"
                        : "设备已停用",
                    _ => LastHeartbeatTime.HasValue
                        ? $"在线 {durationText}"
                        : $"最近更新 {LastUpdateTime:MM-dd HH:mm:ss}"
                };
            }
        }

        public string CurrentOrderSummary =>
            string.IsNullOrWhiteSpace(CurrentOrderNo) ? "未绑定工单" : $"工单 {CurrentOrderNo}";

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays}天";
            }

            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}小时{duration.Minutes}分钟";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{Math.Max(1, (int)duration.TotalMinutes)}分钟";
            }

            return $"{Math.Max(1, (int)duration.TotalSeconds)}秒";
        }
    }
}

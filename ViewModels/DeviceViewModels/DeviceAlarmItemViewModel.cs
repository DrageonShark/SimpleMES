using SimpleMES.Models;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public enum DeviceAlarmSeverity
    {
        Critical,
        Warning,
        Notice
    }

    public sealed class DeviceAlarmItemViewModel
    {
        public DeviceAlarmItemViewModel(AlarmRecordModel alarm, DeviceAlarmSeverity severity)
        {
            Alarm = alarm;
            Severity = severity;
        }

        public AlarmRecordModel Alarm { get; }
        public int AlarmId => Alarm.AlarmId;
        public int DeviceId => Alarm.DeviceId;
        public string AlarmMessage => Alarm.AlarmMessage;
        public DateTime AlarmTime => Alarm.AlarmTime;
        public DeviceAlarmSeverity Severity { get; }
        public string SeverityKey => Severity.ToString();

        public string SeverityText => Severity switch
        {
            DeviceAlarmSeverity.Critical => "\u4e25\u91cd",
            DeviceAlarmSeverity.Warning => "\u544a\u8b66",
            _ => "\u63d0\u9192"
        };
    }
}

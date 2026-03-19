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
            DeviceAlarmSeverity.Critical => "严重",
            DeviceAlarmSeverity.Warning => "告警",
            _ => "提醒"
        };
    }
}

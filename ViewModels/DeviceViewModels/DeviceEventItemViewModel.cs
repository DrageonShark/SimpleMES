using SimpleMES.Models.Dto;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public enum DeviceEventSeverity
    {
        Critical,
        Warning,
        Info
    }

    public sealed class DeviceEventItemViewModel
    {
        public DeviceEventItemViewModel(DeviceEventDto deviceEvent, DeviceEventSeverity severity)
        {
            DeviceEvent = deviceEvent;
            Severity = severity;
        }

        public DeviceEventDto DeviceEvent { get; }
        public long EventId => DeviceEvent.EventId;
        public int DeviceId => DeviceEvent.DeviceId;
        public string DeviceName => DeviceEvent.DeviceName;
        public string EventType => DeviceEvent.EventType;
        public string EventTypeText => DeviceEvent.EventTypeText;
        public string EventMessage => DeviceEvent.EventMessage;
        public string LocationSummary => DeviceEvent.LocationSummary;
        public string SnapshotStateText => DeviceEvent.SnapshotStateText;
        public string OccurredAtSummary => DeviceEvent.OccurredAtSummary;
        public string ResolutionSummary => DeviceEvent.ResolutionSummary;
        public string ProcessingStatusText => DeviceEvent.ProcessingStatusText;
        public string ProcessingStatusKey => DeviceEvent.ProcessingStatusKey;
        public bool IsResolved => DeviceEvent.IsResolved;
        public bool IsConfirmed => DeviceEvent.IsConfirmed;
        public bool RequiresManualConfirmation => DeviceEvent.RequiresManualConfirmation;
        public bool CanConfirm => RequiresManualConfirmation && !IsConfirmed;
        public string ConfirmationSummary => DeviceEvent.ConfirmationSummary;
        public string RelatedAlarmSummary => DeviceEvent.RelatedAlarmSummary;
        public string? ResolutionNote => DeviceEvent.ResolutionNote;
        public DeviceEventSeverity Severity { get; }
        public string SeverityKey => Severity.ToString();

        public string SeverityText => Severity switch
        {
            DeviceEventSeverity.Critical => "严重",
            DeviceEventSeverity.Warning => "告警",
            _ => "信息"
        };
    }
}

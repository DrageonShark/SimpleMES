namespace SimpleMES.Models.Dto
{
    public sealed class DeviceEventDto
    {
        public long EventId { get; set; }
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string? DeviceCode { get; set; }
        public string? WorkshopName { get; set; }
        public string? LineName { get; set; }
        public string? StationName { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventLevel { get; set; } = "Info";
        public string EventMessage { get; set; } = string.Empty;
        public string? SnapshotState { get; set; }
        public DateTime OccurredAt { get; set; }
        public int? RelatedAlarmId { get; set; }
        public string? RelatedAlarmCode { get; set; }
        public string? RelatedAlarmLevel { get; set; }
        public string? RelatedAlarmMessage { get; set; }
        public DateTime? RelatedAlarmTime { get; set; }
        public bool? RelatedAlarmIsAck { get; set; }
        public DateTime? RelatedAlarmAckTime { get; set; }
        public DateTime? RelatedAlarmRecoverTime { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ConfirmedByUserId { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? ResolutionNote { get; set; }

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

        public string EventTypeText => EventType.Trim() switch
        {
            "DeviceAdded" => "设备接入",
            "ConfigUpdated" => "配置更新",
            "DeviceEnabled" => "启用设备",
            "DeviceDisabled" => "停用设备",
            "FaultRaised" => "故障触发",
            "FaultRecovered" => "故障恢复",
            "CommunicationRestored" => "通信恢复",
            "AlarmAcknowledged" => "告警确认",
            "EventConfirmed" => "事件确认",
            _ => EventType
        };

        public string EventLevelText => EventLevel.Trim().ToLowerInvariant() switch
        {
            "critical" => "严重",
            "warning" => "告警",
            "notice" => "提醒",
            _ => "信息"
        };

        public string SnapshotStateText => SnapshotState?.Trim().ToLowerInvariant() switch
        {
            "running" => "运行中",
            "disconnected" => "断连",
            "fault" => "故障",
            "disabled" => "停用",
            _ => "未知状态"
        };

        public bool RequiresManualConfirmation =>
            RelatedAlarmId.HasValue || string.Equals(EventType, "FaultRaised", StringComparison.OrdinalIgnoreCase);

        public bool IsConfirmed => ConfirmedAt.HasValue;

        public string ProcessingStatusKey
        {
            get
            {
                if (!RequiresManualConfirmation)
                {
                    return IsResolved ? "Recorded" : "Pending";
                }

                if (IsConfirmed && IsResolved)
                {
                    return "Confirmed";
                }

                if (IsConfirmed)
                {
                    return "ConfirmedPendingRecovery";
                }

                return IsResolved ? "AwaitingConfirmation" : "Pending";
            }
        }

        public string ProcessingStatusText => ProcessingStatusKey switch
        {
            "Confirmed" => "已确认",
            "ConfirmedPendingRecovery" => "已确认待恢复",
            "AwaitingConfirmation" => "已恢复待确认",
            "Recorded" => "已记录",
            _ => RequiresManualConfirmation ? "待处理" : "待记录"
        };

        public string OccurredAtSummary => $"发生时间 {OccurredAt:MM-dd HH:mm:ss}";

        public string ResolutionSummary
        {
            get
            {
                if (IsConfirmed)
                {
                    var confirmer = string.IsNullOrWhiteSpace(ConfirmedByUserName) ? "当前用户" : ConfirmedByUserName;
                    return $"人工确认 {ConfirmedAt:MM-dd HH:mm:ss} / {confirmer}";
                }

                if (RequiresManualConfirmation && IsResolved)
                {
                    return $"系统已恢复 {ResolvedAt.GetValueOrDefault(OccurredAt):MM-dd HH:mm:ss}，待人工确认";
                }

                if (IsResolved)
                {
                    return $"系统已记录 {ResolvedAt.GetValueOrDefault(OccurredAt):MM-dd HH:mm:ss}";
                }

                return RequiresManualConfirmation ? "待处理，需人工确认原因" : "待处理";
            }
        }

        public string RelatedAlarmSummary
        {
            get
            {
                if (!RelatedAlarmId.HasValue)
                {
                    return "无关联告警";
                }

                var code = string.IsNullOrWhiteSpace(RelatedAlarmCode) ? $"告警 #{RelatedAlarmId}" : $"{RelatedAlarmCode} / 告警 #{RelatedAlarmId}";
                if (string.IsNullOrWhiteSpace(RelatedAlarmMessage))
                {
                    return code;
                }

                return $"{code} - {RelatedAlarmMessage}";
            }
        }

        public string ConfirmationSummary
        {
            get
            {
                if (IsConfirmed)
                {
                    var confirmer = string.IsNullOrWhiteSpace(ConfirmedByUserName) ? "当前用户" : ConfirmedByUserName;
                    return $"确认人 {confirmer} / {ConfirmedAt:yyyy-MM-dd HH:mm:ss}";
                }

                return RequiresManualConfirmation ? "待人工确认" : "无需人工确认";
            }
        }
    }
}

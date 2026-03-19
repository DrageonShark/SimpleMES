using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class RunningState : IDeviceState
    {
        public string Name => "Running";

        public async Task<IDeviceState> HandleAsync(MonitoredDeviceModel device, DevicePollResult result, IDataRepository repository,
            CancellationToken token = default)
        {
            if (!result.IsSuccess)
            {
                // 新增：状态从“运行”掉到“故障”时，写一条未确认告警
                var alarmId = await repository.InsertAlarmRecordAsync(new AlarmRecordModel
                {
                    DeviceId = device.DeviceId,
                    AlarmCode = "COMM_FAIL",
                    AlarmLevel = "Critical",
                    AlarmSource = "Communication",
                    AlarmMessage = $"设备通信失败：{result.Exception?.Message ?? "未知异常"}",
                    AlarmTime = result.OccurredAt,
                    IsAck = false
                });

                var next = new FaultState(alarmId, result.Exception?.Message);
                await repository.InsertDeviceEventAsync(new DeviceEventModel
                {
                    DeviceId = device.DeviceId,
                    EventType = "FaultRaised",
                    EventLevel = "Critical",
                    EventMessage = $"设备通信失败：{result.Exception?.Message ?? "未知异常"}",
                    SnapshotState = next.Name,
                    OccurredAt = result.OccurredAt,
                    RelatedAlarmId = alarmId,
                    IsResolved = false
                });
                await repository.UpdateDeviceStateAsync(device.DeviceId, next.Name, result.OccurredAt);
                device.Runtime.DeviceState = next.Name;
                device.Runtime.LastUpdateTime = result.OccurredAt;
                device.Runtime.LastStateChangeTime = result.OccurredAt;
                return next;
            }


            device.Runtime.DeviceState = Name;
            device.Runtime.LastUpdateTime = result.OccurredAt;
            device.Runtime.LastHeartbeatTime = result.OccurredAt;
            await repository.UpdateDeviceStateAsync(device.DeviceId, Name, result.OccurredAt);
            return this;
        }
    }
}

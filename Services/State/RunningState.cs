using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class RunningState : IDeviceState
    {
        public string Name => "Running";

        public async Task<IDeviceState> HandleAsync(DeviceModel device, DevicePollResult result, IDataRepository repository,
            CancellationToken token = default)
        {
            if (!result.IsSuccess)
            {
                // 新增：状态从“运行”掉到“故障”时，写一条未确认告警
                await repository.InsertAlarmRecordAsync(new AlarmRecordModel
                {
                    DeviceId = device.DeviceId,
                    AlarmMessage = $"设备通信失败：{result.Exception?.Message ?? "未知异常"}",
                    AlarmTime = result.OccurredAt,
                    IsAck = false
                });

                var next = new FaultState(result.Exception?.Message);
                await repository.UpdateDeviceStateAsync(device.DeviceId, next.Name, result.OccurredAt);
                device.DeviceState = next.Name;
                device.LastUpdateTime = result.OccurredAt;
                return next;
            }


            device.DeviceState = Name;
            device.LastUpdateTime = result.OccurredAt;
            await repository.UpdateDeviceStateAsync(device.DeviceId, Name, result.OccurredAt);
            return this;
        }
    }
}

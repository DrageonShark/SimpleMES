using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class DisconnectedState : IDeviceState
    {
        public string Name => "Disconnected";

        public async Task<IDeviceState> HandleAsync(DeviceModel device, DevicePollResult result, IDataRepository repository,
            CancellationToken token = default)
        {
            if (!result.IsSuccess)
            {
                // 仍然连接失败：保持断连，记故障日志
                device.DeviceState = nameof(DeviceState.Disconnected);
                await repository.UpdateDeviceStateAsync(device.DeviceId, Name, result.OccurredAt);
                return this;
            }

            device.DeviceState = nameof(DeviceState.Running);
            device.LastUpdateTime = result.OccurredAt;
            await repository.UpdateDeviceStateAsync(device.DeviceId, device.DeviceState, result.OccurredAt);
            // 成功读到数据 -> 迁移到 Running
            return new RunningState();
        }
    }
}

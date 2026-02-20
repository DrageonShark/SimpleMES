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
                // 读失败 -> 故障
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

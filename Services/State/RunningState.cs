using Serilog;
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
                Log.Error("设备故障，设备名：{DeviceName}，设备ID：{DeviceId}，错误信息：{Message}", device.DeviceName, device.DeviceId, result?.Exception?.Message);
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

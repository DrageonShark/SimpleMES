using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class FaultState : IDeviceState
    {
        private readonly string? _lastError;
        public FaultState(string? lastError = null) => _lastError = lastError;

        public string Name => "Fault";

        public async Task<IDeviceState> HandleAsync(MonitoredDeviceModel device, DevicePollResult result, IDataRepository repository,
            CancellationToken token = default)
        {
            // 若再次成功读到数据，则恢复运行；否则保持故障并记录最新错误
            if (result.IsSuccess)
            {
                Log.Information("设备状态变化，设备名：{DeviceName}，设备ID：{DeviceId}， 状态：故障 -> 连接", device.DeviceName, device.DeviceId);
                device.Runtime.DeviceState = nameof(DeviceState.Running);
                device.Runtime.LastUpdateTime = result.OccurredAt;
                await repository.UpdateDeviceStateAsync(device.DeviceId, nameof(DeviceState.Running), result.OccurredAt);
                return new RunningState();
            }
            Log.Error("设备故障，设备名：{DeviceName}，设备ID：{DeviceId}，错误信息：{Message}", device.DeviceName, device.DeviceId, result?.Exception?.Message);
            device.Runtime.DeviceState = Name;
            device.Runtime.LastUpdateTime = result.OccurredAt;
            await repository.UpdateDeviceStateAsync(device.DeviceId, Name, result.OccurredAt);
            return this;
        }
    }
}

using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class FaultState : IDeviceState
    {
        private readonly string? _lastError;
        public FaultState(string? lastError = null) => _lastError = lastError;

        public string Name => "Fault";

        public async Task<IDeviceState> HandleAsync(DeviceModel device, DevicePollResult result, IDataRepository repository,
            CancellationToken token = default)
        {
            // 若再次成功读到数据，则恢复运行；否则保持故障并记录最新错误
            if (result.IsSuccess)
            {
                device.DeviceState = nameof(DeviceState.Running);
                device.LastUpdateTime = result.OccurredAt;
                await repository.UpdateDeviceStateAsync(device.DeviceId, nameof(DeviceState.Running), result.OccurredAt);
                return new RunningState();
            }

            device.DeviceState = Name;
            device.LastUpdateTime = result.OccurredAt;
            await repository.UpdateDeviceStateAsync(device.DeviceId, Name, result.OccurredAt);
            return this;
        }
    }
}

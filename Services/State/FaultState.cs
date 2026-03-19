using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class FaultState : IDeviceState
    {
        private readonly int? _relatedAlarmId;
        private readonly string? _lastError;
        public FaultState(int? relatedAlarmId = null, string? lastError = null)
        {
            _relatedAlarmId = relatedAlarmId;
            _lastError = lastError;
        }

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
                device.Runtime.LastHeartbeatTime = result.OccurredAt;
                device.Runtime.LastStateChangeTime = result.OccurredAt;
                await repository.UpdateDeviceStateAsync(device.DeviceId, nameof(DeviceState.Running), result.OccurredAt);
                if (_relatedAlarmId.HasValue)
                {
                    await repository.MarkAlarmRecoveredAsync(_relatedAlarmId.Value, result.OccurredAt);
                    await repository.ResolveDeviceEventsByAlarmAsync(_relatedAlarmId.Value, result.OccurredAt);
                }
                await repository.InsertDeviceEventAsync(new DeviceEventModel
                {
                    DeviceId = device.DeviceId,
                    EventType = "FaultRecovered",
                    EventLevel = "Info",
                    EventMessage = "设备故障恢复，通信重新正常",
                    SnapshotState = nameof(DeviceState.Running),
                    OccurredAt = result.OccurredAt,
                    IsResolved = true,
                    ResolvedAt = result.OccurredAt
                });
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

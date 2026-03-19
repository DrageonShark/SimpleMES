using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public class DisconnectedState : IDeviceState
    {
        public string Name => "Disconnected";

        public async Task<IDeviceState> HandleAsync(MonitoredDeviceModel device, DevicePollResult result, IDataRepository repository,
            CancellationToken token = default)
        {
            if (!result.IsSuccess)
            {
                // 仍然连接失败：保持断连，记故障日志
                Log.Error("设备连接失败，设备名：{DeviceName}，设备ID：{DeviceId}，错误信息：{Message}", device.DeviceName, device.DeviceId, result?.Exception?.Message);
                device.Runtime.DeviceState = nameof(DeviceState.Disconnected);
                device.Runtime.LastUpdateTime = result.OccurredAt;
                await repository.UpdateDeviceStateAsync(device.DeviceId, Name, result.OccurredAt);
                return this;
            }
            Log.Information("设备状态变化，设备名：{DeviceName}，设备ID：{DeviceId}， 状态：未连接 -> 连接", device.DeviceName, device.DeviceId);
            device.Runtime.DeviceState = nameof(DeviceState.Running);
            device.Runtime.LastUpdateTime = result.OccurredAt;
            device.Runtime.LastHeartbeatTime = result.OccurredAt;
            device.Runtime.LastStateChangeTime = result.OccurredAt;
            await repository.UpdateDeviceStateAsync(device.DeviceId, device.Runtime.DeviceState, result.OccurredAt);
            await repository.InsertDeviceEventAsync(new DeviceEventModel
            {
                DeviceId = device.DeviceId,
                EventType = "CommunicationRestored",
                EventLevel = "Info",
                EventMessage = "设备恢复通信并重新上线",
                SnapshotState = nameof(DeviceState.Running),
                OccurredAt = result.OccurredAt,
                IsResolved = true,
                ResolvedAt = result.OccurredAt
            });
            // 成功读到数据 -> 迁移到 Running
            return new RunningState();
        }
    }
}

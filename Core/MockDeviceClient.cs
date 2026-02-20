using SimpleMES.Models;

namespace SimpleMES.Core
{
    public class MockDeviceClient : IDeviceClient
    {
        private readonly DeviceModel _device;

        public MockDeviceClient(DeviceModel device) => _device = device;

        public Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, CancellationToken token = default)
        {
            // 简单模拟：固定或随机数据，便于测试
            ushort[] data = { 250, 120, 45 };
            return Task.FromResult(data);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

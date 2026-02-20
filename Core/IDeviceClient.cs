namespace SimpleMES.Core
{
    public interface IDeviceClient : IAsyncDisposable
    {
        Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints,
            CancellationToken token = default);
    }
}

using SimpleMES.Models;

namespace SimpleMES.Core
{
    internal class DevicePollingStrategyResolver : IDevicePollingStrategyResolver
    {
        private readonly IReadOnlyDictionary<string, IDevicePollingStrategy> _strategies;
        private readonly IDevicePollingStrategy _fallback;

        public DevicePollingStrategyResolver(IEnumerable<IDevicePollingStrategy> strategies)
        {
            var dict = strategies.ToDictionary(s => s.Key,
                StringComparer.OrdinalIgnoreCase);
            _strategies = dict;
            _fallback = dict.Values.First();
        }
        public IDevicePollingStrategy Resolve(DeviceModel device)
        {
            if (!string.IsNullOrWhiteSpace(device.SerialPort) && _strategies.TryGetValue("rtu", out var rtu))
                return rtu;
            if (_strategies.TryGetValue("default", out var dft))
                return dft;
            return _fallback;
        }
    }
}

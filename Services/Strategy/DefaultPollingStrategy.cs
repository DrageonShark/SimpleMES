using SimpleMES.Core;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.State;

namespace SimpleMES.Services.Strategy
{
    /// <summary>
    /// 默认寄存器布局（温/压/速各 1 个寄存器）的轮询与解析策略。
    /// </summary>
    public class DefaultPollingStrategy : IDevicePollingStrategy
    {
        public string Key => "default";
        public async Task<PollingResult> PollAsync(IDeviceClient client, MonitoredDeviceModel device, CancellationToken token = default)
        {
            try
            {
                var data = await client.ReadHoldingRegistersAsync(0, 3, token);
                var occurredAt = DateTime.Now;
                var pollResult = new DevicePollResult(true, data, null, occurredAt);
                decimal temp = Math.Round(data[0] / 10.0m, 3);
                decimal press = Math.Round(data[1] / 12.0m, 3);
                int speed = data[2] / 15;
                var snapshot = new DeviceDto
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    IpAddress = device.IpAddress,
                    SerialPort = device.SerialPort,
                    Temperature = temp,
                    Pressure = press,
                    Speed = speed,
                    DeviceState = Enum.Parse<DeviceState>(device.Runtime.DeviceState, true),
                    LastUpdateTime = occurredAt
                };
                PersistCallback persist = async (IDataRepository repository, CancellationToken ct) =>
                {
                    await repository.InsertProductionRecordAsync(new ProductionRecordModel
                    {
                        DeviceId = device.DeviceId,
                        Pressure = press,
                        Speed = speed,
                        Temperature = temp,
                        RecordTime = occurredAt
                    });
                };
                return new PollingResult(pollResult, snapshot, persist);
            }
            catch (Exception ex)
            {
                var pollResult = new DevicePollResult(false, null, ex, DateTime.Now);
                return new PollingResult(pollResult, null, null);
            }
        }
    }
}

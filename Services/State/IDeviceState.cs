using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public interface IDeviceState
    {
        string Name { get; }

        Task<IDeviceState> HandleAsync(DeviceModel device, DevicePollResult result,
            IDataRepository repository, CancellationToken token = default);
    }

    /// <summary>
    /// 设备运行状态枚举
    /// </summary>
    public enum DeviceState
    {
        /// <summary>
        /// 设备正常运行中
        /// </summary>
        Running,

        /// <summary>
        /// 设备已断开连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 设备故障
        /// </summary>
        Fault
    }
}

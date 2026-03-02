using SimpleMES.Models;

namespace SimpleMES.Services.Observer
{
    /// <summary>
    /// 定义设备配置变更的通知契约，供观察者订阅和发布配置更新事件。
    /// </summary>
    public interface IDeviceConfigNotifier
    {
        /// <summary>
        /// 当设备配置发生变更时触发的事件。
        /// </summary>
        event EventHandler<DeviceConfigChangeEventArgs>? ConfigChanged;

        /// <summary>
        /// 通知订阅者指定设备的配置发生了变化，并指明变更类型。
        /// </summary>
        /// <param name="updateDevice">已更新配置的设备实例。</param>
        /// <param name="changeType">配置变更类型，用于描述本次变更的性质。</param>
        void NotifyConfigChanged(DeviceModel updateDevice, ConfigChangeType changeType);
    }
}
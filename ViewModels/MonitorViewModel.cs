using SimpleMES.Core;
using SimpleMES.Models.Dto;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace SimpleMES.ViewModels
{
    public partial class MonitorViewModel : ViewModelBase
    {
        private readonly Dispatcher _dispatcher;
        // 界面绑定的设备列表
        public ObservableCollection<DeviceDto> ListDeviceDto { get; set; } = new ObservableCollection<DeviceDto>();

        public MonitorViewModel(DeviceCommunicationService service)
        {
            _dispatcher = GetCurrentDispatcher();
            // 订阅 Service 的事件
            service.OnDeviceStatusChanged += Service_OnDeviceStatusChanged;
        }

        public void Service_OnDeviceStatusChanged(List<DeviceDto> listLatestDeviceDto)
        {
            // 关键点：回到主线程更新 UI
            _dispatcher.Invoke(() =>
            {
                // 如果列表是空的（第一次），就全部添加
                if (ListDeviceDto.Count == 0)
                {
                    foreach (var deviceDto in listLatestDeviceDto.ToList())
                    {
                        ListDeviceDto.Add(deviceDto);
                    }
                }
                else
                {
                    // 如果已经有数据，就只更新属性，不要 Clear 再 Add（否则界面会闪烁）
                    foreach (var newDeviceDto in listLatestDeviceDto)
                    {
                        var oldDeviceDto =
                            ListDeviceDto.FirstOrDefault(d => d.DeviceId == newDeviceDto.DeviceId);
                        if (oldDeviceDto != null)
                        {
                            oldDeviceDto.Temperature = newDeviceDto.Temperature;
                            oldDeviceDto.Pressure = newDeviceDto.Pressure;
                            oldDeviceDto.Speed = newDeviceDto.Speed;
                            oldDeviceDto.DeviceState = newDeviceDto.DeviceState;
                            oldDeviceDto.LastUpdateTime = newDeviceDto.LastUpdateTime;
                        }
                    }
                }
            });
        }

        private static Dispatcher GetCurrentDispatcher()
        {
            //尝试多种方式获取UI线程Dispatcher
            var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher != null && dispatcher.Thread == Thread.CurrentThread)
                return dispatcher;
            if (Application.Current != null)
                return Application.Current.Dispatcher;
            return Dispatcher.CurrentDispatcher;
        }

    }
}

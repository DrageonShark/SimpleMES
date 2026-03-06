using MaterialDesignThemes.Wpf;
using SimpleMES.Models.Dto;
using SimpleMES.Services.Observer;
using SimpleMES.Services.State;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace SimpleMES.ViewModels
{
    public partial class MonitorViewModel : ViewModelBase, IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly IDeviceStatusNotifier _notifier;
        private bool _disposed;
        // 界面绑定的设备列表
        public ObservableCollection<DeviceDto> ListDeviceDto { get; set; } = new ObservableCollection<DeviceDto>();
        // Snackbar 消息队列（自动关闭，含进度条动画）
        public SnackbarMessageQueue SnackbarQueue { get; }

        public MonitorViewModel(IDeviceStatusNotifier notifier)
        {
            _dispatcher = GetCurrentDispatcher();
            _notifier = notifier;
            // Snackbar 队列绑定到 UI 线程 Dispatcher，确保线程安全
            SnackbarQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3), _dispatcher);
            // 订阅 Service 的事件
            _notifier.DeviceStatusChanged += OnDeviceStatusChanged;
        }

        public void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            // 关键点：回到主线程更新 UI
            _dispatcher.Invoke(() =>
            {
                var listLatestDeviceDto = e.LatestDevices;
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
                            // 状态发生变化时弹出 Snackbar 提示
                            if (oldDeviceDto.DeviceState != newDeviceDto.DeviceState)
                            {
                                if (newDeviceDto.DeviceState == DeviceState.Fault)
                                    SnackbarQueue.Enqueue($"⚠️ 设备 [{newDeviceDto.DeviceName}] 发生故障！");
                                else if (newDeviceDto.DeviceState == DeviceState.Disconnected)
                                    SnackbarQueue.Enqueue($"🔌 设备 [{newDeviceDto.DeviceName}] 已断开连接！");
                                else if (newDeviceDto.DeviceState == DeviceState.Running)
                                    SnackbarQueue.Enqueue($"✅ 设备 [{newDeviceDto.DeviceName}] 已恢复运行！");
                            }
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

        public void Dispose()
        {
            if (_disposed) return;
            _notifier.DeviceStatusChanged -= OnDeviceStatusChanged;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Observer;
using SimpleMES.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace SimpleMES.ViewModels
{
    public partial class MonitorViewModel : ViewModelBase, IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly IDeviceStatusNotifier _notifier;
        private readonly IDataRepository _repository;
        private bool _disposed;
        // 界面绑定的设备列表
        public ObservableCollection<DeviceDto> ListDeviceDto { get; set; } = new ObservableCollection<DeviceDto>();

        public MonitorViewModel(IDeviceStatusNotifier notifier, IDataRepository repository)
        {
            _dispatcher = GetCurrentDispatcher();
            _notifier = notifier;
            _repository = repository;
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
                    // 同时处理新增设备
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
                        else
                        {
                            // 新增设备不在列表中，直接添加
                            ListDeviceDto.Add(newDeviceDto);
                        }
                    }
                }
            });
        }

        [RelayCommand]
        private async Task AddDevice()
        {
            var dto = new DeviceDto();
            var window = new DeviceAddWindow(dto);
            if (window.ShowDialog() != true)
                return;

            var device = new DeviceModel
            {
                DeviceName = dto.DeviceName,
                IpAddress = dto.IpAddress,
                Port = dto.Port,
                SerialPort = dto.SerialPort,
                SlaveId = 0,
                DeviceState = "Disconnected",
                LastUpdateTime = DateTime.Now
            };

            try
            {
                var newId = await _repository.AddDeviceAsync(device).ConfigureAwait(false);
                dto.DeviceId = newId;
                _dispatcher.Invoke(() => ListDeviceDto.Add(dto));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加设备失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task EditDevice(DeviceDto? device)
        {
            if (device is null) return;

            var window = new DeviceEditWindow(device);
            if (window.ShowDialog() != true)
                return;

            var model = new DeviceModel
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                IpAddress = device.IpAddress,
                Port = device.Port,
                SerialPort = device.SerialPort,
                SlaveId = 0
            };

            try
            {
                await _repository.UpdateDeviceAsync(model).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新设备失败: {ex.Message}");
            }
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

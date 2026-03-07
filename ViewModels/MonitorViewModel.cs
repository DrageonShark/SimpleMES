using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
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
        //编辑页面属性绑定
        [ObservableProperty] private int _deviceId;
        [ObservableProperty] private string? _deviceName;
        [ObservableProperty] private string? _ipAddress;
        [ObservableProperty] private int? _port;
        [ObservableProperty] private string? _serialPort;
        [ObservableProperty] private byte _slaveId = 1;
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
                    //处理新增设备
                    var newItems = listLatestDeviceDto.Where(n => ListDeviceDto.All(o => o.DeviceId != n.DeviceId));
                    foreach (var item in newItems) ListDeviceDto.Add(item);
                }
            });
        }

        [RelayCommand]
        private async Task EditDeviceConfig(DeviceDto? device)
        {
            if (device is null) return;
            Log.Information("修改设备配置，设备Id：{DeviceId},设备名：{DeviceName}", device.DeviceId, device.DeviceName);
            // 复制一份，给弹窗编辑，避免取消污染原对象
            var editing = new DeviceDto
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                IpAddress = device.IpAddress,
                Port = device.Port,
                SerialPort = device.SerialPort,
                SlaveId = device.SlaveId
            };

            async Task<bool> SaveAsync(DeviceDto dto)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.IpAddress) && string.IsNullOrWhiteSpace(dto.SerialPort))
                    {
                        throw new Exception("设备IP地址或串口至少一个不为空和空格");
                    }
                    if (string.IsNullOrWhiteSpace(dto.DeviceName.Trim()))
                        throw new Exception("设备名不能为空和空格");
                    var newDevice = new DeviceModel
                    {
                        DeviceId = dto.DeviceId,
                        DeviceName = dto.DeviceName.Trim(),
                        IpAddress = dto.IpAddress?.Trim(),
                        Port = dto.Port,
                        SerialPort = dto.SerialPort?.Trim(),
                        SlaveId = dto.SlaveId ?? 0,
                    };
                    await _repository.UpdateDeviceAsync(newDevice);
                    // 回写到原始对象，刷新 UI
                    device.DeviceName = dto.DeviceName;
                    device.IpAddress = dto.IpAddress;
                    device.Port = dto.Port;
                    device.SerialPort = dto.SerialPort;
                    device.SlaveId = dto.SlaveId;
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error("设备配置修改失败，设备Id：{DeviceId},设备名：{DeviceName}，错误内容：{Message}", device.DeviceId, device.DeviceName, ex.Message);
                    //ToastWindow.Error($"保存失败，错误：{ex.Message}", 5);
                    MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            var dialog = new DeviceEditWindow(SaveAsync)
            {
                Owner = Application.Current.MainWindow,
                DataContext = editing
            };
            var result = dialog.ShowDialog();// 成功时窗口会在 SaveAsync 返回 true 后关闭
            if (result == true)
            {
                Log.Information("设备配置修改成功，设备Id：{DeviceId},设备名：{DeviceName}", device.DeviceId, device.DeviceName);
                ToastWindow.Success("设备配置更新成功");
            }
        }

        [RelayCommand]
        private async Task AddDevice()
        {
            Log.Information("添加新设备");
            DeviceModel newDevice = new DeviceModel();

            async Task<bool> OnSure(DeviceModel device)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(device.DeviceName))
                    {
                        if (!string.IsNullOrWhiteSpace(device.IpAddress))
                        {
                            newDevice = new DeviceModel
                            {
                                DeviceName = device.DeviceName.Trim(),
                                IpAddress = device.IpAddress.Trim(),
                                Port = device.Port,
                                SlaveId = device.SlaveId ?? 0
                            };
                            await _repository.InsertDeviceAsync(newDevice);
                            return true;
                        }
                        if (!string.IsNullOrWhiteSpace(device.SerialPort))
                        {
                            newDevice = new DeviceModel
                            {
                                DeviceName = device.DeviceName.Trim(),
                                SerialPort = device.SerialPort.Trim(),
                                SlaveId = device.SlaveId
                            };
                            await _repository.InsertDeviceAsync(newDevice);
                            return true;
                        }
                        throw new Exception("IP地址或串口至少一个不为空");
                    }
                    throw new Exception("设备名为空或空格");
                }
                catch (Exception ex)
                {
                    Log.Error("新增设备失败，错误：{Message}", ex.Message);
                    //ToastWindow.Error($"新增设备失败，错误：{ex.Message}");
                    MessageBox.Show($"新增设备失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            var dialog = new DeviceAddWindow(OnSure)
            {
                Owner = Application.Current.MainWindow,
                DataContext = newDevice
            };
            var result = dialog.ShowDialog();
            if (result == true)
            {
                Log.Information("设备添加成功，设备名：{DeviceName}", newDevice.DeviceName);
                ToastWindow.Success("设备添加成功");
                //MessageBox.Show("设备添加成功");
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

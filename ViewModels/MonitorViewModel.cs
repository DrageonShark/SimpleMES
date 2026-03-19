using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleMES.Core;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Dialog;
using SimpleMES.Services.Observer;
using SimpleMES.Services.Security;
using SimpleMES.Services.State;
using SimpleMES.Services.Toast;
using SimpleMES.Services.UI;
using SimpleMES.ViewModels.DeviceViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleMES.ViewModels
{
    public partial class MonitorViewModel : DialogViewModelBase, IDisposable, IDeviceWorkspaceActions
    {
        private readonly IDeviceClientFactory _deviceClientFactory;
        private readonly IDeviceConfigNotifier _configNotifier;
        private readonly IDeviceStatusNotifier _statusNotifier;
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private readonly IDeviceDialogService _deviceDialogService;
        private readonly IUiDispatcher _uiDispatcher;
        private readonly DeviceWorkspaceState _workspaceState;
        private bool _disposed;
        private readonly UserSession _session = UserSession.Current;


        //编辑页面属性绑定
        [ObservableProperty] private int _deviceId;
        [ObservableProperty] private string? _deviceName;
        [ObservableProperty] private string? _ipAddress;
        [ObservableProperty] private int? _port;
        [ObservableProperty] private string? _serialPort;
        [ObservableProperty] private byte _slaveId = 1;

        public DeviceWorkspaceState WorkspaceState => _workspaceState;

        public string SearchKeyword
        {
            get => _workspaceState.SearchKeyword;
            set
            {
                if (_workspaceState.SearchKeyword == value) return;
                _workspaceState.SearchKeyword = value;
                OnPropertyChanged();
            }
        }

        public string StateFilter
        {
            get => _workspaceState.StateFilter;
            set
            {
                if (_workspaceState.StateFilter == value) return;
                _workspaceState.StateFilter = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<string> StateFilterOptions => _workspaceState.StateFilterOptions;
        public int RunningCount => _workspaceState.RunningCount;
        public int DisconnectedCount => _workspaceState.DisconnectedCount;
        public int FaultCount => _workspaceState.FaultCount;
        public int DisabledCount => _workspaceState.DisabledCount;
        public bool IsAlarmPanelCollapsed => _workspaceState.IsAlarmPanelCollapsed;
        public string AlarmPanelToggleContent => _workspaceState.AlarmPanelToggleContent;
        public string AlarmPanelToggleText => _workspaceState.AlarmPanelToggleText;
        public ObservableCollection<DeviceDto> ListDeviceDto => _workspaceState.ListDeviceDto;
        public ObservableCollection<DeviceDto> FilteredDeviceDto => _workspaceState.FilteredDeviceDto;
        public ObservableCollection<AlarmRecordModel> PendingAlarms => _workspaceState.PendingAlarms;
        public ObservableCollection<DeviceDto> AttentionDevices => _workspaceState.AttentionDevices;
        public ObservableCollection<DeviceDto> RecentDevices => _workspaceState.RecentDevices;
        public int TotalDeviceCount => _workspaceState.TotalDeviceCount;
        public int AttentionDeviceCount => _workspaceState.AttentionDeviceCount;
        public bool HasDevices => _workspaceState.HasDevices;
        public bool HasFilteredDevices => _workspaceState.HasFilteredDevices;
        public bool HasActiveFilters => _workspaceState.HasActiveFilters;
        public bool HasPendingAlarms => _workspaceState.HasPendingAlarms;
        public bool HasAttentionDevices => _workspaceState.HasAttentionDevices;
        public bool HasRecentDevices => _workspaceState.HasRecentDevices;
        public string DeviceOverviewSummary => _workspaceState.DeviceOverviewSummary;
        public string ManagementEmptyTitle => _workspaceState.ManagementEmptyTitle;
        public string ManagementEmptyDescription => _workspaceState.ManagementEmptyDescription;
        public string AlarmSummary => _workspaceState.AlarmSummary;
        public string LatestAlarmPreview => _workspaceState.LatestAlarmPreview;
        public string BoardHeadline => _workspaceState.BoardHeadline;
        public string BoardDescription => _workspaceState.BoardDescription;
        public string AttentionSummary => _workspaceState.AttentionSummary;
        public string RecentDeviceSummary => _workspaceState.RecentDeviceSummary;
        //权限属性
        public bool CanAddDevicePermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.AddDevice);
        public bool CanEditDevicePermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.EditDevice);
        public bool CanToggleDevicePermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.ToggleDevice);
        public bool CanAckAlarmPermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.AckAlarm);

        public MonitorViewModel(
            IDeviceStatusNotifier notifier, IDataRepository repository,
            IToastService toast, IDeviceConfigNotifier configNotifier,
            IDeviceClientFactory deviceClientFactory,
            DeviceWorkspaceState workspaceState,
            IDeviceDialogService? deviceDialogService = null,
            IUiDispatcher? uiDispatcher = null)
        {
            _statusNotifier = notifier;
            _repository = repository;
            _toast = toast;
            _configNotifier = configNotifier;
            _deviceClientFactory = deviceClientFactory;
            _workspaceState = workspaceState;
            _deviceDialogService = deviceDialogService ?? new DeviceDialogService();
            _uiDispatcher = uiDispatcher ?? WpfUiDispatcher.CreateDefault();

            _statusNotifier.DeviceStatusChanged += OnDeviceStatusChanged;
            _workspaceState.PropertyChanged += OnWorkspaceStatePropertyChanged;
            _ = RefreshAlarms();
            _session.PropertyChanged += OnSessionPropertyChanged;
        }
        //设备面板数据源处理
        public void OnDeviceStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            //回到主线程更新 UI
            _uiDispatcher.Invoke(() =>
            {
                _workspaceState.ApplyLatestDeviceSnapshot(e.LatestDevices);
            });
        }
        //设备配置修改逻辑
        [RelayCommand(CanExecute = nameof(CanEditDeviceConfig))]
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
                    if (string.IsNullOrWhiteSpace(dto.DeviceName.Trim()))
                        throw new Exception("设备名不能为空和空格");
                    if (string.IsNullOrWhiteSpace(dto.IpAddress) && string.IsNullOrWhiteSpace(dto.SerialPort))
                    {
                        throw new Exception("设备IP地址或串口至少一个不为空和空格");
                    }

                    var newDevice = new DeviceModel
                    {
                        DeviceId = dto.DeviceId,
                        DeviceName = dto.DeviceName.Trim(),
                        IpAddress = dto.IpAddress?.Trim() ?? string.Empty,
                        Port = dto.Port,
                        SerialPort = dto.SerialPort?.Trim() ?? string.Empty,
                        SlaveId = dto.SlaveId is null or 0 ? (byte)1 : dto.SlaveId,
                    };
                    await _repository.UpdateDeviceAsync(newDevice);
                    // 回写到原始对象，刷新 UI
                    device.DeviceName = dto.DeviceName;
                    device.IpAddress = dto.IpAddress ?? string.Empty;
                    device.Port = dto.Port;
                    device.SerialPort = dto.SerialPort ?? string.Empty;
                    device.SlaveId = dto.SlaveId;
                    _workspaceState.NotifyDeviceMetadataChanged();
                    // 告诉通信层：这个设备配置变了，需要重启该设备采集任务
                    _configNotifier.NotifyConfigChanged(newDevice, ConfigChangeType.Updated);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error("设备配置修改失败，设备Id：{DeviceId},设备名：{DeviceName}，错误内容：{Message}", device.DeviceId, device.DeviceName, ex.Message);
                    _toast.Error($"保存失败，错误：{ex.Message}", null, 3.5);
                    return false;
                }
            }
            async Task<(bool IsSuccess, string Message)> TestAsync(DeviceDto dto)
            {
                var model = new DeviceModel
                {
                    DeviceId = dto.DeviceId,
                    DeviceName = dto.DeviceName,
                    IpAddress = dto.IpAddress,
                    Port = dto.Port,
                    SerialPort = dto.SerialPort,
                    SlaveId = dto.SlaveId
                };
                return await TestConnectionAsync(model);
            }
            var confirmed = await _deviceDialogService.ShowEditDeviceDialogAsync(editing, SaveAsync, TestAsync);
            if (!confirmed) return;

            Log.Information("设备配置修改成功，设备Id：{DeviceId},设备名：{DeviceName}", device.DeviceId, device.DeviceName);
            _toast.Success("设备配置更新成功", null, 3.5);
        }

        //设备添加逻辑
        [RelayCommand(CanExecute = nameof(CanAddDevice))]
        private async Task AddDevice()
        {
            Log.Information("添加新设备");
            var draft = new DeviceModel { SlaveId = 1 };
            DeviceModel? savedDevice = null;
            var newId = 0;
            async Task<bool> SaveAsync(DeviceModel device)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(device.DeviceName))
                    {
                        throw new Exception("设备名为空或空格");
                    }

                    if (string.IsNullOrWhiteSpace(device.IpAddress) && string.IsNullOrWhiteSpace(device.SerialPort))
                    {
                        throw new Exception("IP地址或串口至少一个不为空");
                    }

                    savedDevice = new DeviceModel
                    {
                        DeviceName = device.DeviceName.Trim(),
                        IpAddress = device.IpAddress?.Trim() ?? string.Empty,
                        Port = device.Port,
                        SerialPort = device.SerialPort?.Trim() ?? string.Empty,
                        SlaveId = device.SlaveId is null or 0 ? (byte)1 : device.SlaveId
                    };

                    newId = await _repository.InsertDeviceAsync(savedDevice);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error("新增设备失败，错误：{Message}", ex.Message);
                    _toast.Error($"新增设备失败，错误：{ex.Message}", null, 3.5);
                    return false;
                }
            }
            async Task<(bool IsSuccess, string Message)> TestAsync(DeviceModel draft)
            {
                return await TestConnectionAsync(draft);
            }
            var confirmed = await _deviceDialogService.ShowAddDeviceDialogAsync(draft, SaveAsync, TestAsync);
            if (!confirmed || savedDevice is null) return;

            savedDevice.DeviceId = newId;
            _configNotifier.NotifyConfigChanged(savedDevice, ConfigChangeType.Added);
            Log.Information("设备添加成功，设备名：{DeviceName}", savedDevice.DeviceName);
            _toast.Success($"设备添加成功，设备名：{savedDevice.DeviceName}", null, 3.5);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _statusNotifier.DeviceStatusChanged -= OnDeviceStatusChanged;
            _workspaceState.PropertyChanged -= OnWorkspaceStatePropertyChanged;
            _session.PropertyChanged -= OnSessionPropertyChanged;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        //UI界面设备停用和启用逻辑
        [RelayCommand(CanExecute = nameof(CanToggleDeviceEnabled))]
        private async Task ToggleDeviceEnabled(DeviceDto? device)
        {
            if (device is null) return;
            //前是停用 -> 启用；否则停用
            bool toEnable = device.DeviceState == DeviceState.Disabled;
            try
            {
                var updateTime = DateTime.Now;
                await _repository.SetDeviceEnabledAsync(device.DeviceId, toEnable, updateTime);
                // 更新前端状态，保证 UI 立即反馈
                device.DeviceState = toEnable ? DeviceState.Disconnected : DeviceState.Disabled;
                device.LastUpdateTime = updateTime;
                if (!toEnable)
                {
                    // 停用后清零实时值，避免显示旧数据
                    device.Temperature = 0;
                    device.Pressure = 0;
                    device.Speed = 0;
                }
                // 通知通信服务停止/恢复采集线程
                var changed = new DeviceModel
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    IpAddress = device.IpAddress,
                    Port = device.Port,
                    SerialPort = device.SerialPort,
                    SlaveId = device.SlaveId,
                    DeviceState = toEnable ? nameof(DeviceState.Disconnected) : nameof(DeviceState.Disabled),
                    LastUpdateTime = device.LastUpdateTime
                };
                _configNotifier.NotifyConfigChanged(changed, toEnable ? ConfigChangeType.Enabled : ConfigChangeType.Disabled);
                _workspaceState.RefreshDeviceFilter();
                _toast.Success(toEnable ? $"已启用：{device.DeviceName}" : $"已停用：{device.DeviceName}");
            }
            catch (Exception ex)
            {
                _toast.Error($"设备状态切换失败：{ex.Message}");
            }
        }
        //新增设备界面的测试设备连接按钮
        private async Task<(bool IsSuccess, string Message)> TestConnectionAsync(DeviceModel raw)
        {
            var device = new DeviceModel
            {
                DeviceId = raw.DeviceId,
                DeviceName = string.IsNullOrWhiteSpace(raw.DeviceName) ? $"设备{raw.DeviceId}" : raw.DeviceName.Trim(),
                IpAddress = raw.IpAddress?.Trim() ?? string.Empty,
                Port = raw.Port is > 0 ? raw.Port : 502,
                SerialPort = raw.SerialPort?.Trim() ?? string.Empty,
                SlaveId = raw.SlaveId is null or 0 ? (byte)1 : raw.SlaveId
            };
            if (string.IsNullOrWhiteSpace(device.IpAddress) && string.IsNullOrWhiteSpace(device.SerialPort))
            {
                return (false, "IP地址和串口不能同时为空");
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await using var client = _deviceClientFactory.Create(device);
                // 读取 1 个寄存器作为连接性验证
                var data = await client.ReadHoldingRegistersAsync(0, 1, cts.Token);
                if (data is { Length: > 0 })
                    return (true, $"连接成功，寄存器值：{data[0]}");

                return (false, "连接成功，但未读取到寄存器数据");
            }
            catch (TimeoutException)
            {
                return (false, "连接超时，请检查设备地址、端口或串口");
            }
            catch (Exception ex)
            {
                return (false, $"连接失败：{ex.Message}");
            }
        }
        //告警面板数据源实现逻辑
        [RelayCommand]
        private async Task RefreshAlarms()
        {
            try
            {
                var alarms = await _repository.GetUnAckAlarmsAsync(30);
                _workspaceState.ReplacePendingAlarms(alarms);
            }
            catch (Exception ex)
            {
                _toast.Error($"加载警告失败：{ex.Message}", null, 3);
            }
        }
        //确认警告
        [RelayCommand(CanExecute = nameof(CanAckAlarmEnabled))]
        private async Task AckAlarm(AlarmRecordModel? alarm)
        {
            if (alarm is null) return;
            try
            {
                var rows = await _repository.AckAlarmAsync(alarm.AlarmId);
                if (rows <= 0) return;

                _workspaceState.RemovePendingAlarm(alarm);
                _toast.Success($"已确认警告 #{alarm.AlarmId}", null, 3);
            }
            catch (Exception ex)
            {
                _toast.Error($"确认告警失败：{ex.Message}", null, 3);
            }
        }
        /// <summary>
        /// 警告板块侧边栏按钮
        /// </summary>
        [RelayCommand]
        private void ToggleAlarmPanel()
        {
            _workspaceState.ToggleAlarmPanel();
        }
        //根据当前用户权限判断操作是否可用
        private bool CanAddDevice() => CanAddDevicePermission;
        private bool CanEditDeviceConfig(DeviceDto? device) => CanEditDevicePermission && device is not null;
        private bool CanToggleDeviceEnabled(DeviceDto? device) => CanToggleDevicePermission && device is not null;
        private bool CanAckAlarmEnabled(AlarmRecordModel? alarm) => CanAckAlarmPermission && alarm is not null;
        /// <summary>
        /// 用户切换时自动刷新权限
        /// </summary>
        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            OnPropertyChanged(nameof(CanAddDevicePermission));
            OnPropertyChanged(nameof(CanEditDevicePermission));
            OnPropertyChanged(nameof(CanToggleDevicePermission));
            OnPropertyChanged(nameof(CanAckAlarmPermission));

            AddDeviceCommand.NotifyCanExecuteChanged();
            EditDeviceConfigCommand.NotifyCanExecuteChanged();
            ToggleDeviceEnabledCommand.NotifyCanExecuteChanged();
            AckAlarmCommand.NotifyCanExecuteChanged();
        }

        private void OnWorkspaceStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
            OnPropertyChanged(e.PropertyName);
        }

        public Task AddDeviceAsync()
        {
            return AddDevice();
        }

        public Task EditDeviceConfigAsync(DeviceDto? device)
        {
            return EditDeviceConfig(device);
        }

        public Task ToggleDeviceEnabledAsync(DeviceDto? device)
        {
            return ToggleDeviceEnabled(device);
        }

        public Task RefreshAlarmsAsync()
        {
            return RefreshAlarms();
        }

        public Task AckAlarmAsync(AlarmRecordModel? alarm)
        {
            return AckAlarm(alarm);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using SimpleMES.Core;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Dialog;
using SimpleMES.Services.Observer;
using SimpleMES.Services.Security;
using SimpleMES.Services.Toast;
using System.ComponentModel;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public partial class DeviceWorkspaceActionService : ObservableObject, IDeviceWorkspaceActions, IDisposable
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private readonly IDeviceConfigNotifier _configNotifier;
        private readonly IDeviceClientFactory _deviceClientFactory;
        private readonly IDeviceDialogService _deviceDialogService;
        private readonly DeviceWorkspaceState _workspaceState;
        private readonly UserSession _session = UserSession.Current;

        public DeviceWorkspaceActionService(
            IDataRepository repository,
            IToastService toast,
            IDeviceConfigNotifier configNotifier,
            IDeviceClientFactory deviceClientFactory,
            DeviceWorkspaceState workspaceState,
            IDeviceDialogService? deviceDialogService = null)
        {
            _repository = repository;
            _toast = toast;
            _configNotifier = configNotifier;
            _deviceClientFactory = deviceClientFactory;
            _workspaceState = workspaceState;
            _deviceDialogService = deviceDialogService ?? new DeviceDialogService();

            _session.PropertyChanged += OnSessionPropertyChanged;
        }

        public bool CanAddDevicePermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.AddDevice);
        public bool CanEditDevicePermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.EditDevice);
        public bool CanToggleDevicePermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.ToggleDevice);
        public bool CanAckAlarmPermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.AckAlarm);

        public async Task AddDeviceAsync()
        {
            Log.Information("添加新设备");
            var draft = new DeviceModel { SlaveId = 1, IsEnabled = true };
            DeviceModel? savedDevice = null;
            var newId = 0;

            async Task<bool> SaveAsync(DeviceModel device)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(device.DeviceName))
                    {
                        throw new Exception("设备名不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(device.IpAddress) && string.IsNullOrWhiteSpace(device.SerialPort))
                    {
                        throw new Exception("IP 地址或串口至少填写一项");
                    }

                    savedDevice = new DeviceModel
                    {
                        DeviceName = device.DeviceName.Trim(),
                        IpAddress = device.IpAddress?.Trim() ?? string.Empty,
                        Port = device.Port,
                        SerialPort = device.SerialPort?.Trim() ?? string.Empty,
                        SlaveId = device.SlaveId is null or 0 ? (byte)1 : device.SlaveId,
                        IsEnabled = true
                    };

                    newId = await _repository.InsertDeviceAsync(savedDevice);
                    await _repository.InsertDeviceEventAsync(new DeviceEventModel
                    {
                        DeviceId = newId,
                        EventType = "DeviceAdded",
                        EventLevel = "Info",
                        EventMessage = "设备已接入系统，等待建立通信",
                        SnapshotState = nameof(Services.State.DeviceState.Disconnected),
                        OccurredAt = DateTime.Now,
                        IsResolved = true,
                        ResolvedAt = DateTime.Now
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error("新增设备失败：{Message}", ex.Message);
                    _toast.Error($"新增设备失败：{ex.Message}", null, 3.5);
                    return false;
                }
            }

            async Task<(bool IsSuccess, string Message)> TestAsync(DeviceModel raw)
            {
                return await TestConnectionAsync(raw);
            }

            var confirmed = await _deviceDialogService.ShowAddDeviceDialogAsync(draft, SaveAsync, TestAsync);
            if (!confirmed || savedDevice is null)
            {
                return;
            }

            savedDevice.DeviceId = newId;
            _configNotifier.NotifyConfigChanged(savedDevice, ConfigChangeType.Added);
            Log.Information("设备添加成功，设备名：{DeviceName}", savedDevice.DeviceName);
            _toast.Success($"设备添加成功：{savedDevice.DeviceName}", null, 3.5);
        }

        public async Task EditDeviceConfigAsync(DeviceDto? device)
        {
            if (device is null)
            {
                return;
            }

            Log.Information("修改设备配置，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
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
                    if (string.IsNullOrWhiteSpace(dto.DeviceName?.Trim()))
                    {
                        throw new Exception("设备名不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(dto.IpAddress) && string.IsNullOrWhiteSpace(dto.SerialPort))
                    {
                        throw new Exception("设备 IP 地址或串口至少填写一项");
                    }

                    var newDevice = new DeviceModel
                    {
                        DeviceId = dto.DeviceId,
                        DeviceName = dto.DeviceName.Trim(),
                        IpAddress = dto.IpAddress?.Trim() ?? string.Empty,
                        Port = dto.Port,
                        SerialPort = dto.SerialPort?.Trim() ?? string.Empty,
                        SlaveId = dto.SlaveId is null or 0 ? (byte)1 : dto.SlaveId,
                        IsEnabled = device.DeviceState != Services.State.DeviceState.Disabled
                    };

                    await _repository.UpdateDeviceAsync(newDevice);
                    await _repository.InsertDeviceEventAsync(new DeviceEventModel
                    {
                        DeviceId = newDevice.DeviceId,
                        EventType = "ConfigUpdated",
                        EventLevel = "Info",
                        EventMessage = "设备配置已更新",
                        SnapshotState = device.DeviceState.ToString(),
                        OccurredAt = DateTime.Now,
                        IsResolved = true,
                        ResolvedAt = DateTime.Now
                    });

                    device.DeviceName = dto.DeviceName;
                    device.IpAddress = dto.IpAddress ?? string.Empty;
                    device.Port = dto.Port;
                    device.SerialPort = dto.SerialPort ?? string.Empty;
                    device.SlaveId = dto.SlaveId;
                    _workspaceState.NotifyDeviceMetadataChanged();
                    _configNotifier.NotifyConfigChanged(newDevice, ConfigChangeType.Updated);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error("设备配置修改失败，设备Id：{DeviceId}，设备名：{DeviceName}，错误：{Message}", device.DeviceId, device.DeviceName, ex.Message);
                    _toast.Error($"保存失败：{ex.Message}", null, 3.5);
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
            if (!confirmed)
            {
                return;
            }

            Log.Information("设备配置修改成功，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
            _toast.Success("设备配置更新成功", null, 3.5);
        }

        public async Task ToggleDeviceEnabledAsync(DeviceDto? device)
        {
            if (device is null)
            {
                return;
            }

            var toEnable = device.DeviceState == Services.State.DeviceState.Disabled;
            try
            {
                var updateTime = DateTime.Now;
                await _repository.SetDeviceEnabledAsync(device.DeviceId, toEnable, updateTime);
                await _repository.InsertDeviceEventAsync(new DeviceEventModel
                {
                    DeviceId = device.DeviceId,
                    EventType = toEnable ? "DeviceEnabled" : "DeviceDisabled",
                    EventLevel = "Info",
                    EventMessage = toEnable ? "设备已启用，等待重新建立通信" : "设备已停用，停止参与实时监控",
                    SnapshotState = toEnable ? nameof(Services.State.DeviceState.Disconnected) : nameof(Services.State.DeviceState.Disabled),
                    OccurredAt = updateTime,
                    IsResolved = true,
                    ResolvedAt = updateTime
                });

                device.DeviceState = toEnable ? Services.State.DeviceState.Disconnected : Services.State.DeviceState.Disabled;
                device.LastUpdateTime = updateTime;
                if (!toEnable)
                {
                    device.Temperature = 0;
                    device.Pressure = 0;
                    device.Speed = 0;
                }

                var changed = new DeviceModel
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    IpAddress = device.IpAddress,
                    Port = device.Port,
                    SerialPort = device.SerialPort,
                    SlaveId = device.SlaveId,
                    IsEnabled = toEnable
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

        public async Task RefreshAlarmsAsync()
        {
            try
            {
                var alarms = await _repository.GetUnAckAlarmsAsync(30);
                _workspaceState.ReplacePendingAlarms(alarms);
            }
            catch (Exception ex)
            {
                _toast.Error($"加载告警失败：{ex.Message}", null, 3);
            }
        }

        public async Task AckAlarmAsync(AlarmRecordModel? alarm)
        {
            if (alarm is null)
            {
                return;
            }

            try
            {
                await ConfirmAlarmAsync(alarm.DeviceId, alarm.AlarmId, alarm.AlarmMessage, null, null);
            }
            catch (Exception ex)
            {
                _toast.Error($"确认告警失败：{ex.Message}", null, 3);
            }
        }

        public async Task ConfirmDeviceEventAsync(DeviceEventDto? deviceEvent, string? resolutionNote)
        {
            if (deviceEvent is null || !deviceEvent.RequiresManualConfirmation || deviceEvent.IsConfirmed)
            {
                return;
            }

            try
            {
                if (deviceEvent.RelatedAlarmId.HasValue)
                {
                    var alarmMessage = string.IsNullOrWhiteSpace(deviceEvent.RelatedAlarmMessage)
                        ? deviceEvent.EventMessage
                        : deviceEvent.RelatedAlarmMessage;
                    await ConfirmAlarmAsync(deviceEvent.DeviceId, deviceEvent.RelatedAlarmId.Value, alarmMessage, resolutionNote, deviceEvent);
                    return;
                }

                var currentUser = _session.CurrentUser;
                if (currentUser is null)
                {
                    _toast.Error("当前未登录用户，无法确认事件", null, 3);
                    return;
                }

                var confirmedAt = DateTime.Now;
                var normalizedNote = NormalizeResolutionNote(resolutionNote);
                await _repository.ConfirmDeviceEventAsync(deviceEvent.EventId, currentUser.UserId, confirmedAt, normalizedNote);
                await _repository.InsertDeviceEventAsync(new DeviceEventModel
                {
                    DeviceId = deviceEvent.DeviceId,
                    EventType = "EventConfirmed",
                    EventLevel = "Info",
                    EventMessage = $"事件已确认：{deviceEvent.EventMessage}",
                    SnapshotState = ResolveSnapshotState(deviceEvent.DeviceId, deviceEvent.SnapshotState),
                    OccurredAt = confirmedAt,
                    RelatedAlarmId = deviceEvent.RelatedAlarmId,
                    IsResolved = true,
                    ResolvedAt = confirmedAt,
                    ConfirmedByUserId = currentUser.UserId,
                    ConfirmedAt = confirmedAt,
                    ResolutionNote = normalizedNote
                });

                await RefreshRecentEventsAsync();
                _toast.Success($"事件 #{deviceEvent.EventId} 已确认", null, 3);
            }
            catch (Exception ex)
            {
                _toast.Error($"确认事件失败：{ex.Message}", null, 3);
            }
        }

        public async Task RefreshRecentEventsAsync(int top = 120)
        {
            var recentEvents = await _repository.GetRecentDeviceEventsAsync(top);
            _workspaceState.ApplyRecentEvents(recentEvents);
        }

        public Task<DeviceEventQueryResult> QueryDeviceEventsAsync(DeviceEventQueryCriteria criteria)
        {
            return _repository.GetDeviceEventsPageAsync(criteria);
        }

        public void Dispose()
        {
            _session.PropertyChanged -= OnSessionPropertyChanged;
        }

        private async Task ConfirmAlarmAsync(int deviceId, int alarmId, string alarmMessage, string? resolutionNote, DeviceEventDto? deviceEvent)
        {
            var currentUser = _session.CurrentUser;
            if (currentUser is null)
            {
                _toast.Error("当前未登录用户，无法确认告警", null, 3);
                return;
            }

            var confirmedAt = DateTime.Now;
            var normalizedNote = NormalizeResolutionNote(resolutionNote);
            var rows = await _repository.AckAlarmAsync(alarmId, currentUser.UserId, confirmedAt);
            if (rows <= 0)
            {
                return;
            }

            await _repository.ConfirmDeviceEventsByAlarmAsync(alarmId, currentUser.UserId, confirmedAt, normalizedNote);
            await _repository.InsertDeviceEventAsync(new DeviceEventModel
            {
                DeviceId = deviceId,
                EventType = "AlarmAcknowledged",
                EventLevel = "Info",
                EventMessage = string.IsNullOrWhiteSpace(alarmMessage) ? $"告警 #{alarmId} 已确认" : $"告警已确认：{alarmMessage}",
                SnapshotState = ResolveSnapshotState(deviceId, deviceEvent?.SnapshotState),
                OccurredAt = confirmedAt,
                RelatedAlarmId = alarmId,
                IsResolved = true,
                ResolvedAt = confirmedAt,
                ConfirmedByUserId = currentUser.UserId,
                ConfirmedAt = confirmedAt,
                ResolutionNote = normalizedNote
            });

            var pendingAlarm = _workspaceState.PendingAlarms.FirstOrDefault(item => item.AlarmId == alarmId);
            if (pendingAlarm is not null)
            {
                _workspaceState.RemovePendingAlarm(pendingAlarm);
            }

            await RefreshRecentEventsAsync();
            _toast.Success($"已确认告警 #{alarmId}", null, 3);
        }

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
                return (false, "IP 地址和串口不能同时为空");
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await using var client = _deviceClientFactory.Create(device);
                var data = await client.ReadHoldingRegistersAsync(0, 1, cts.Token);
                if (data is { Length: > 0 })
                {
                    return (true, $"连接成功，寄存器值：{data[0]}");
                }

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

        private string? ResolveSnapshotState(int deviceId, string? fallbackSnapshotState)
        {
            var relatedDevice = _workspaceState.ListDeviceDto.FirstOrDefault(device => device.DeviceId == deviceId);
            return relatedDevice?.DeviceState.ToString() ?? fallbackSnapshotState;
        }

        private static string? NormalizeResolutionNote(string? resolutionNote)
        {
            if (string.IsNullOrWhiteSpace(resolutionNote))
            {
                return null;
            }

            return resolutionNote.Trim();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser))
            {
                return;
            }

            OnPropertyChanged(nameof(CanAddDevicePermission));
            OnPropertyChanged(nameof(CanEditDevicePermission));
            OnPropertyChanged(nameof(CanToggleDevicePermission));
            OnPropertyChanged(nameof(CanAckAlarmPermission));
        }
    }
}

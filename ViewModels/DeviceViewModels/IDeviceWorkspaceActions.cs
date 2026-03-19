using SimpleMES.Models;
using SimpleMES.Models.Dto;
using System.ComponentModel;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public interface IDeviceWorkspaceActions : INotifyPropertyChanged
    {
        bool CanAddDevicePermission { get; }
        bool CanEditDevicePermission { get; }
        bool CanToggleDevicePermission { get; }
        bool CanAckAlarmPermission { get; }

        Task AddDeviceAsync();
        Task EditDeviceConfigAsync(DeviceDto? device);
        Task ToggleDeviceEnabledAsync(DeviceDto? device);
        Task RefreshAlarmsAsync();
        Task AckAlarmAsync(AlarmRecordModel? alarm);
    }
}

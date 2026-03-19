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
        Task RefreshRecentEventsAsync(int top = 120);
        Task<DeviceEventQueryResult> QueryDeviceEventsAsync(DeviceEventQueryCriteria criteria);
        Task AckAlarmAsync(AlarmRecordModel? alarm);
        Task ConfirmDeviceEventAsync(DeviceEventDto? deviceEvent, string? resolutionNote);
    }
}

using SimpleMES.Models;
using SimpleMES.Models.Dto;

namespace SimpleMES.Services.Dialog
{
    public interface IDeviceDialogService
    {
        Task<bool> ShowAddDeviceDialogAsync(
            DeviceModel draft,
            Func<DeviceModel, Task<bool>> saveAsync,
            Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? testAsync = null);

        Task<bool> ShowEditDeviceDialogAsync(
            DeviceDto draft,
            Func<DeviceDto, Task<bool>> saveAsync,
            Func<DeviceDto, Task<(bool IsSuccess, string Message)>>? testAsync = null);
    }
}

using SimpleMES.Models;

namespace SimpleMES.Services.Dialog
{
    public interface IOrderDialogService
    {
        Task<bool> ShowOrderDetailDialogAsync(OrderModel order);
    }
}

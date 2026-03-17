using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.Orders
{
    public interface IOrderState
    {
        string Name { get; }

        Task<IOrderState> HandleAsync(
            OrderModel order,
            OrderPollResult result,
            IDataRepository repository,
            CancellationToken token);
    }
}

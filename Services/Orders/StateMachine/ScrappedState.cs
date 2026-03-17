using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Orders;

namespace SimpleMES.Services.Orders.StateMachine
{
    public class ScrappedState : IOrderState
    {
        public string Name { get; }
        public async Task<IOrderState> HandleAsync(OrderModel order, OrderPollResult result, IDataRepository repository, CancellationToken token)
        {
            return await Task.FromResult<IOrderState>(this);
        }
    }
}

using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
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

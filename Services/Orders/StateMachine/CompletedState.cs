using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.Orders.StateMachine
{
    public class CompletedState : IOrderState
    {
        public string Name { get; }
        public async Task<IOrderState> HandleAsync(OrderModel order, OrderPollResult result, IDataRepository repository, CancellationToken token)
        {
            // 已完工，不管什么操作，都保持完成状态（拒绝任何变更）
            return await Task.FromResult<IOrderState>(this);
        }
    }
}

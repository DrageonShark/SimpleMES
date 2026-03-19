using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.Orders.StateMachine
{
    internal class OtherState : IOrderState
    {
        public string Name { get; }
        public async Task<IOrderState> HandleAsync(OrderModel order, OrderPollResult result, IDataRepository repository, CancellationToken token)
        {
            if (!result.IsSuccess) return this;
            switch (result.Operation)
            {
                case OrderStatus.Pending:
                    order.OrderStatus = nameof(OrderStatus.Pending);
                    order.StartTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new PendingState();
                case OrderStatus.Scrapped:
                    order.OrderStatus = nameof(OrderStatus.Scrapped);
                    order.EndTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new ScrappedState();
                default:
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return this;
            }
        }
    }
}

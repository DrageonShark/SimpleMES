using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Orders;

namespace SimpleMES.Services.Orders.StateMachine
{
    public class ProducingState : IOrderState
    {
        public string Name { get; }
        public async Task<IOrderState> HandleAsync(OrderModel order, OrderPollResult result, IDataRepository repository, CancellationToken token)
        {
            if (!result.IsSuccess) return this;
            if (result.CompletedQtyDelta is int delta && delta > 0) order.CompletedQty += delta;
            switch (result.Operation)
            {
                case OrderState.Paused:
                    order.OrderStatus = nameof(OrderState.Paused);
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new PausedState();
                case OrderState.Completed:
                    order.OrderStatus = nameof(OrderState.Completed);
                    order.EndTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new CompletedState();
                case OrderState.Scrapped:
                    order.OrderStatus = nameof(OrderState.Scrapped);
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

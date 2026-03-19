using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.Orders.StateMachine
{
    public class PendingState : IOrderState
    {
        public string Name => "Pending";
        public async Task<IOrderState> HandleAsync(OrderModel order, OrderPollResult result, IDataRepository repository, CancellationToken token)
        {
            if (!result.IsSuccess) return this;
            switch (result.Operation)
            {
                case OrderStatus.Producing:
                    order.OrderStatus = nameof(OrderStatus.Producing);
                    order.StartTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new ProducingState();
                case OrderStatus.Scrapped:
                    order.OrderStatus = nameof(OrderStatus.Scrapped);
                    order.EndTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new ScrappedState();
                case OrderStatus.Pending:
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new ScrappedState();
                default:
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new OtherState();
            }
        }
    }
}

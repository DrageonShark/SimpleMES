using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Orders;
using SimpleMES.Services.State;

namespace SimpleMES.Services.Orders.StateMachine
{
    public class PausedState : IOrderState
    {
        public string Name { get; }
        public async Task<IOrderState> HandleAsync(OrderModel order, OrderPollResult result, IDataRepository repository, CancellationToken token)
        {
            if (!result.IsSuccess) return this;
            switch (result.Operation)
            {
                case OrderState.Producing:
                    order.OrderStatus = nameof(OrderState.Producing);
                    order.StartTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new ProducingState();
                case OrderState.Scrapped:
                    order.OrderStatus = nameof(OrderState.Scrapped);
                    order.EndTime = result.OccurredAt;
                    order.LastOperationTime = result.OccurredAt;
                    await repository.UpdateOrderAsync(order);
                    return new ScrappedState();
                default:
                    return this;
            }
        }
    }
}

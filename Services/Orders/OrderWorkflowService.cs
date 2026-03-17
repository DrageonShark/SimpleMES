using SimpleMES.Models;

namespace SimpleMES.Services.Orders
{
    public class OrderWorkflowService : IOrderWorkflowService
    {
        public bool CanTransit(OrderModel? order, OrderWorkflowAction action)
        {
            if (order is null) return false;

            var status = (order.OrderStatus ?? string.Empty).Trim();

            return action switch
            {
                OrderWorkflowAction.Start =>
                    status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Paused", StringComparison.OrdinalIgnoreCase),

                OrderWorkflowAction.Pause =>
                    status.Equals("Producing", StringComparison.OrdinalIgnoreCase),

                OrderWorkflowAction.Complete =>
                    status.Equals("Producing", StringComparison.OrdinalIgnoreCase),

                _ => false
            };
        }

        public OrderModel Transit(OrderModel order, OrderWorkflowAction action, DateTime? now = null)
        {
            if (!CanTransit(order, action))
            {
                throw new InvalidOperationException($"订单 {order.OrderNo} 当前状态不允许执行 {action}");
            }

            var at = now ?? DateTime.Now;

            return new OrderModel
            {
                OrderNo = order.OrderNo,
                ProductCode = order.ProductCode,
                PlanQty = order.PlanQty,
                CompletedQty = order.CompletedQty,
                CreateTime = order.CreateTime,
                LastOperationTime = at,
                StartTime = action == OrderWorkflowAction.Start ? order.StartTime ?? at : order.StartTime,
                EndTime = action == OrderWorkflowAction.Complete ? at : null,
                OrderStatus = action switch
                {
                    OrderWorkflowAction.Start => "Producing",
                    OrderWorkflowAction.Pause => "Paused",
                    OrderWorkflowAction.Complete => "Completed",
                    _ => order.OrderStatus
                }
            };
        }
    }
}

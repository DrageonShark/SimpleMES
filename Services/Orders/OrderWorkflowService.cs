using Serilog;
using SimpleMES.Models;

namespace SimpleMES.Services.Orders
{
    public class OrderWorkflowService : IOrderWorkflowService
    {
        public bool CanTransit(OrderModel? order, OrderWorkflowAction action)
        {
            if (order is null) return false;

            var status = order.OrderStatus;

            return action switch
            {
                OrderWorkflowAction.Start =>
                    status.Equals(nameof(OrderState.Pending), StringComparison.OrdinalIgnoreCase)
                    || status.Equals(nameof(OrderState.Paused), StringComparison.OrdinalIgnoreCase),

                OrderWorkflowAction.Pause =>
                    status.Equals(nameof(OrderState.Producing), StringComparison.OrdinalIgnoreCase),

                OrderWorkflowAction.Complete =>
                    order.PlanQty == order.CompletedQty,

                _ => false
            };
        }

        public OrderModel Transit(OrderModel order, OrderWorkflowAction action, DateTime? now = null)
        {
            if (!CanTransit(order, action))
            {
                Log.Error("订单 {order.OrderNo} 当前状态不允许执行 {action}", order.OrderNo, action);
            }

            var at = now ?? DateTime.Now;

            var nextState = action switch
            {
                OrderWorkflowAction.Start => OrderState.Producing,
                OrderWorkflowAction.Pause => OrderState.Paused,
                OrderWorkflowAction.Complete => OrderState.Completed,
                _ => throw new InvalidOperationException($"不支持的流转动作: {action}")
            };
            return new OrderModel
            {
                OrderNo = order.OrderNo,
                ProductCode = order.ProductCode,
                PlanQty = order.PlanQty,
                CompletedQty = order.CompletedQty,
                CreateTime = order.CreateTime,
                LastOperationTime = at,
                StartTime = action == OrderWorkflowAction.Start
                    ? order.StartTime ?? at
                    : order.StartTime,
                EndTime = action == OrderWorkflowAction.Complete
                    ? at
                    : null,
                OrderStatus = nextState.ToCode()
            };
        }
    }
}

using SimpleMES.Models;

namespace SimpleMES.Services.Orders
{
    /// <summary>
    /// 状态流转服务，判断订单状态能否改变
    /// </summary>
    public interface IOrderWorkflowService
    {
        bool CanTransit(OrderModel? order, OrderWorkflowAction action);
        OrderModel Transit(OrderModel order, OrderWorkflowAction action, DateTime? now = null);
    }
    public enum OrderWorkflowAction
    {
        Start,
        Pause,
        Complete
    }
}

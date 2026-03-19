using Serilog;
using SimpleMES.Models;

namespace SimpleMES.Services.Orders
{
    public static class OrderStateExtensions
    {
        public static OrderStatus ToOrderState(this string? value)
        {
            if (Enum.TryParse<OrderStatus>(value, true, out var state))
            {
                return state;
            }
            Log.Error("未知订单状态:{state}", state);
            return OrderStatus.Other;
        }

        public static string ToCode(this OrderStatus state) => state.ToString();

        public static OrderStatus GetState(this OrderModel order)
        {
            if (order is null)
            {
                Log.Error("订单不能为null");
                return OrderStatus.Other;
            }
            return order.OrderStatus.ToOrderState();
        }
    }
}

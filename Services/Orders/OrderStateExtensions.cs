using Serilog;
using SimpleMES.Models;

namespace SimpleMES.Services.Orders
{
    public static class OrderStateExtensions
    {
        public static OrderState ToOrderState(this string? value)
        {
            if (Enum.TryParse<OrderState>(value, true, out var state))
            {
                return state;
            }
            Log.Error("未知订单状态:{state}", state);
            return OrderState.Other;
        }

        public static string ToCode(this OrderState state) => state.ToString();

        public static OrderState GetState(this OrderModel order)
        {
            if (order is null)
            {
                Log.Error("订单不能为null");
                return OrderState.Other;
            }
            return order.OrderStatus.ToOrderState();
        }
    }
}

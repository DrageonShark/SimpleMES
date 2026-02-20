using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Services.State
{
    public interface IOrderState
    {
        string Name { get; }

        Task<IOrderState> HandleAsync(
            OrderModel order,
            OrderPollResult result,
            IDataRepository repository,
            CancellationToken token);
    }

    /// <summary>
    /// 订单状态枚举
    /// </summary>
    public enum OrderState
    {
        /// <summary>
        /// 待产
        /// </summary>
        Pending,
        /// <summary>
        /// 生产中
        /// </summary>
        Producing,
        /// <summary>
        /// 暂停
        /// </summary>
        Paused,
        /// <summary>
        /// 完工
        /// </summary>
        Completed,
        /// <summary>
        /// 废弃
        /// </summary>
        Scrapped
    }

}

namespace SimpleMES.Services.Orders
{
    /// <summary>
    /// 订单状态枚举
    /// </summary>
    public enum OrderStatus
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
        Scrapped,
        /// <summary>
        /// 其他状态
        /// </summary>
        Other
    }

}

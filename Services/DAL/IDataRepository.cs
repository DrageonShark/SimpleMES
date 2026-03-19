using SimpleMES.Models;

namespace SimpleMES.Services.DAL
{
    /// <summary>
    /// 数据仓储：封装对 SQL Server 的写入操作。
    /// </summary>
    public interface IDataRepository
    {
        /// <summary>
        /// 产品数据的更新或插入
        /// </summary>
        Task<int> UpsertProductAsync(ProductModel product);
        /// <summary>
        /// 新增生产工单
        /// </summary>
        Task<int> CreateOrderAsync(OrderModel order);
        /// <summary>
        /// 获取所有订单
        /// </summary>
        Task<IEnumerable<OrderModel>> GetAllOrdersAsync();
        /// <summary>
        /// 按条件查询订单
        /// </summary>
        Task<IEnumerable<OrderModel>> GetOrdersAsync(string? keyword = null, string? status = null, int? take = null);
        /// <summary>
        /// 更新生产工单
        /// </summary>
        Task<int> UpdateOrderAsync(OrderModel order);
        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="orderNo"></param>
        /// <returns></returns>
        Task<int> DeleteOrderAsync(string orderNo);
        /// <summary>
        /// 获取所有设备
        /// </summary>
        Task<IEnumerable<MonitoredDeviceModel>> GetAllDevicesAsync();
        /// <summary>
        /// 更新设备状态
        /// </summary>
        Task<int> UpdateDeviceStateAsync(int deviceId, string status, DateTime? lastUpDateTime = null);
        /// <summary>
        /// 修改设备配置
        /// </summary>
        Task<int> UpdateDeviceAsync(DeviceModel device);
        /// <summary>
        /// 启用或停用设备
        /// </summary>
        Task<int> SetDeviceEnabledAsync(int deviceId, bool isEnabled, DateTime? changedAt = null);
        /// <summary>
        /// 新增设备
        /// </summary>
        Task<int> InsertDeviceAsync(DeviceModel device);
        /// <summary>
        ///  更新生产记录
        /// </summary>
        Task<int> InsertProductionRecordAsync(ProductionRecordModel productionRecord);
        /// <summary>
        /// 获取最近1小时的记录 (用于画图表)
        /// </summary>
        Task<ProductionRecordModel?> GetRecentRecordsAsync(int deviceId);
        /// <summary>
        ///  更新报警信息
        /// </summary>
        Task<int> InsertAlarmRecordAsync(AlarmRecordModel alarmRecord);
        /// <summary>
        /// 写入设备事件
        /// </summary>
        Task<long> InsertDeviceEventAsync(DeviceEventModel deviceEvent);
        /// <summary>
        /// 获取最近设备事件
        /// </summary>
        Task<IEnumerable<Models.Dto.DeviceEventDto>> GetRecentDeviceEventsAsync(int top = 20);
        /// <summary>
        /// 按条件分页查询设备事件
        /// </summary>
        Task<Models.Dto.DeviceEventQueryResult> GetDeviceEventsPageAsync(Models.Dto.DeviceEventQueryCriteria criteria);
        /// <summary>
        ///  获取最新未确认的20条报警记录
        /// </summary>
        Task<IEnumerable<AlarmRecordModel>> GetUnAckAlarmsAsync(int top = 20);
        /// <summary>
        ///  确认ID为alarmId的报警记录
        /// </summary>
        Task<int> AckAlarmAsync(int alarmId, int? ackUserId = null, DateTime? ackTime = null);
        /// <summary>
        /// 标记报警已恢复
        /// </summary>
        Task<int> MarkAlarmRecoveredAsync(int alarmId, DateTime recoveredAt);
        /// <summary>
        /// 标记关联报警的设备事件已恢复
        /// </summary>
        Task<int> ResolveDeviceEventsByAlarmAsync(int alarmId, DateTime resolvedAt);
        /// <summary>
        /// 确认单条设备事件
        /// </summary>
        Task<int> ConfirmDeviceEventAsync(long eventId, int confirmedByUserId, DateTime confirmedAt, string? resolutionNote);
        /// <summary>
        /// 确认某条报警关联的设备事件
        /// </summary>
        Task<int> ConfirmDeviceEventsByAlarmAsync(int alarmId, int confirmedByUserId, DateTime confirmedAt, string? resolutionNote);
        /// <summary>
        /// 获取所有产品信息
        /// </summary>
        Task<IEnumerable<ProductModel>> GetAllProductsAsync();
        /// <summary>
        /// 获取所有员工信息
        /// </summary>
        Task<IEnumerable<UserModel>> GetAllUserAsync();
        /// <summary>
        ///  验证用户身份并检索相关的用户信息。
        /// </summary>
        Task<UserModel?> LoginAsync(string account);
        /// <summary>
        /// 将新用户插入到数据存储中。
        /// </summary>
        Task<int> InsertUserAsync(UserModel newUser);
        /// <summary>
        /// 更新数据存储中指定的用户。
        /// </summary>
        Task<int> UpdateUserAsync(UserModel oldUser);
        /// <summary>
        /// 删除指定ID的用户
        /// </summary>
        Task<int> DeleteUserAsync(int userId);
    }
}

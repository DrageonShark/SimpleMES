using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        Task<IEnumerable<OrderModel>> GetAllOrdersAsync();
        /// <summary>
        /// 更新生产工单
        /// </summary>
        Task<int> UpdateOrderAsync(OrderModel order);
        /// <summary>
        /// 获取所有设备
        /// </summary>
        Task<IEnumerable<DeviceModel>> GetAllDevicesAsync();
        /// <summary>
        /// 更新设备状态
        /// </summary>
        Task<int> UpdateDeviceStatusAsync(int deviceId, string status, DateTime? lastUpDateTime = null);
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

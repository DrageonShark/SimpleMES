using SimpleMES.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SimpleMES.Services.DAL
{
    /// <summary>
    /// 基于 IDbService 的仓储实现，负责写入 SQL Server。
    /// </summary>
    public class DataRepository : IDataRepository
    {
        private readonly IDbService _db;
        public DataRepository(IDbService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }
        public async Task<int> UpsertProductAsync(ProductModel product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            const string sql = @"IF EXISTS (SELECT 1 FROM T_Products WHERE ProductCode = @ProductCode) 
                                 UPDATE T_Products 
                                 SET ProductName = @ProductName, 
                                 SetTemperature = @SetTemperature,
                                 SetPressure = @SetPressure, 
                                 Description = @Description 
                                 WHERE ProductCode = @ProductCode 
                                 ELSE 
                                 INSERT INTO T_Products(ProductCode, ProductName, SetTemperature, SetPressure, Description) 
                                 VALUES (@ProductCode, @ProductName, @SetTemperature, @SetPressure, @Description);";
            return await _db.ExecuteAsync(sql, product);
        }

        public async Task<int> CreateOrderAsync(OrderModel order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            const string sql = @"INSERT INTO T_ProductionOrders 
                                 (OrderNo, ProductCode, PlanQty, CompletedQty, OrderStatus, StartTime, EndTime, CreateTime)
                                 VALUES (@OrderNo, @ProductCode, @PlanQty, @CompletedQty, @OrderStatus, @StartTime, @EndTime, @CreateTime);";
            return await _db.ExecuteAsync(sql, order);
        }

        public async Task<IEnumerable<OrderModel>> GetAllOrdersAsync()
        {
            const string sql = @"SELECT * FROM T_ProductionOrders ORDER BY CreateTime DESC";
            return await _db.QueryAsync<OrderModel>(sql);
        }

        public async Task<int> UpdateOrderAsync(OrderModel order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            const string sql = @"UPDATE T_ProductionOrders
                                 SET ProductCode = @ProductCode,
                                 PlanQty = @PlanQty,
                                 CompletedQty = @CompletedQty,
                                 OrderStatus = @OrderStatus,
                                 StartTime = @StartTime,
                                 EndTime = @EndTime
                                 WHERE OrderNo = @OrderNo;";
            return await _db.ExecuteAsync(sql, order);
        }

        public async Task<IEnumerable<DeviceModel>> GetAllDevicesAsync()
        {
            const string sql = @"SELECT * FROM T_Devices";
            return await _db.QueryAsync<DeviceModel>(sql);
        }

        public async Task<int> UpdateDeviceStatusAsync(int deviceId, string status, DateTime? lastUpDateTime = null)
        {
            const string sql = @"UPDATE T_Devices
                                 SET Status = @Status,
                                 LastUpdateTime = ISNULL(@LastUpdateTime, LastUpdateTime)
                                 WHERE DeviceId = @DeviceId;";
            return await _db.ExecuteAsync(sql, new { DeviceId = deviceId, Status = status, LastUpdateTime = lastUpDateTime });
        }

        public async Task<int> InsertProductionRecordAsync(ProductionRecordModel productionRecord)
        {
            if (productionRecord == null)
            {
                throw new ArgumentNullException(nameof(productionRecord));
            }
            const string sql = @"INSERT INTO T_ProductionRecords 
                                 (DeviceId, Temperature, Pressure, Speed, RecordTime)
                                 VALUES (@DeviceId, @Temperature, @Pressure, @Speed, @RecordTime);";
            return await _db.ExecuteAsync(sql, productionRecord);
        }

        public async Task<ProductionRecordModel?> GetRecentRecordsAsync(int deviceId)
        {
            const string sql = @"SELECT  * FROM T_ProductionRecords 
                                 WHERE DeviceId = @DeviceId 
                                 ORDER BY RecordTime DESC";
            return (await _db.QueryFirstOrDefault<ProductionRecordModel>(sql, new { DeviceId = deviceId }));
        }

        public async Task<int> InsertAlarmRecordAsync(AlarmRecordModel alarmRecord)
        {
            if (alarmRecord == null)
            {
                throw new ArgumentNullException(nameof(alarmRecord));
            }
            const string sql = @"INSERT INTO T_AlarmRecord 
                                 (DeviceId, AlarmMessage, AlarmTime, IsAck)
                                 VALUES (@DeviceId, @AlarmMessage, @AlarmTime, @IsAck);";
            return await _db.ExecuteAsync(sql, alarmRecord);
        }

        public async Task<IEnumerable<ProductModel>> GetAllProductsAsync()
        {
            const string sql = @"SELECT * FROM T_Products";
            return await _db.QueryAsync<ProductModel>(sql);
        }

        public async Task<IEnumerable<UserModel>> GetAllUserAsync()
        {
            const string sql = @"SELECT * FROM T_User";
            return await _db.QueryAsync<UserModel>(sql);
        }

        public async Task<UserModel?> LoginAsync(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                throw new ArgumentException($"账号不能为空，{nameof(account)}");
            }
            const string sql = @"SELECT TOP 1 UserId, Role, Account, PasswordHash, Salt, IsActive, Email, UserName
                                 FROM T_User 
                                 WHERE Account = @account AND IsActive = 1";
            return await _db.QueryFirstOrDefault<UserModel>(sql, new { Account = account });
        }

        public async Task<int> InsertUserAsync(UserModel newUser)
        {
            const string sql = @"INSERT INTO T_User (UserName, Account, PasswordHash, Salt)
                                 VALUES(UserName = @UserName, Account = @Account, PasswordHash = @PasswordHash, Salt = @Salt)";
            return await _db.ExecuteAsync(sql, newUser);
        }

        public async Task<int> UpdateUserAsync(UserModel oldUser)
        {
            const string sql = @"UPDATE T_User
                                 SET UserName = @UserName,
                                 Role = @Role,
                                 Account = @Account,
                                 PasswordHash = @PasswordHash,
                                 Salt = @Salt,
                                 Email = @Email,
                                 IsActive = ISNULL(@IsActive, IsActive)
                                 WHERE UserId = @UserId";
            return await _db.ExecuteAsync(sql, oldUser);
        }

        public async Task<int> DeleteUserAsync(int userId)
        {
            const string sql = @"DELETE T_User WHERE UserId = @UserId";
            return await _db.ExecuteAsync(sql, new { UserId = userId });
        }
    }
}

using SimpleMES.Models;

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
            return await GetOrdersAsync();
        }

        public async Task<IEnumerable<OrderModel>> GetOrdersAsync(string? keyword = null, string? status = null, int? take = null)
        {
            var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            var topClause = take.HasValue ? "TOP (@Take) " : string.Empty;
            var sql = $@"SELECT {topClause}*
                         FROM T_ProductionOrders
                         WHERE (@Keyword IS NULL
                                OR OrderNo LIKE '%' + @Keyword + '%'
                                OR ProductCode LIKE '%' + @Keyword + '%')
                           AND (@Status IS NULL OR OrderStatus = @Status)
                         ORDER BY CreateTime DESC";

            return await _db.QueryAsync<OrderModel>(sql, new
            {
                Keyword = normalizedKeyword,
                Status = normalizedStatus,
                Take = take
            });
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
                         EndTime = @EndTime,
                         LastOperationTime = @LastOperationTime
                     WHERE OrderNo = @OrderNo;";

            return await _db.ExecuteAsync(sql, order);
        }

        public async Task<int> DeleteOrderAsync(string orderNo)
        {
            if (string.IsNullOrWhiteSpace(orderNo))
            {
                throw new ArgumentException("订单号不能为空", nameof(orderNo));
            }

            const string sql = @"DELETE FROM T_ProductionOrders WHERE OrderNo = @OrderNo;";
            return await _db.ExecuteAsync(sql, new { OrderNo = orderNo });
        }


        public async Task<IEnumerable<MonitoredDeviceModel>> GetAllDevicesAsync()
        {
            const string sql = @"SELECT
                                     m.DeviceId,
                                     m.DeviceName,
                                     m.DeviceCode,
                                     m.DeviceType,
                                     m.WorkshopName,
                                     m.LineName,
                                     m.StationName,
                                     m.IpAddress,
                                     m.Port,
                                     m.SerialPort,
                                     m.SlaveId,
                                     COALESCE(r.DeviceState, 'Disconnected') AS RuntimeDeviceState,
                                     m.IsEnabled,
                                     m.Criticality,
                                     m.SortOrder,
                                     COALESCE(r.LastUpdateTime, GETDATE()) AS RuntimeLastUpdateTime,
                                     r.LastHeartbeatTime AS RuntimeLastHeartbeatTime,
                                     r.LastStateChangeTime AS RuntimeLastStateChangeTime,
                                     r.CurrentOrderNo AS RuntimeCurrentOrderNo,
                                     m.Remark,
                                     m.CreatedAt,
                                     m.UpdatedAt,
                                     COALESCE(r.UpdatedAt, m.UpdatedAt) AS RuntimeUpdatedAt
                                 FROM T_DeviceMaster m
                                 LEFT JOIN T_DeviceRuntime r ON r.DeviceId = m.DeviceId
                                 ORDER BY m.SortOrder, m.DeviceId";

            var rows = await _db.QueryAsync<MonitoredDeviceRow>(sql);
            return rows.Select(row => new MonitoredDeviceModel
            {
                Device = new DeviceModel
                {
                    DeviceId = row.DeviceId,
                    DeviceName = row.DeviceName ?? string.Empty,
                    DeviceCode = row.DeviceCode,
                    DeviceType = row.DeviceType,
                    WorkshopName = row.WorkshopName,
                    LineName = row.LineName,
                    StationName = row.StationName,
                    IpAddress = row.IpAddress ?? string.Empty,
                    Port = row.Port,
                    SerialPort = row.SerialPort ?? string.Empty,
                    SlaveId = row.SlaveId,
                    IsEnabled = row.IsEnabled,
                    Criticality = row.Criticality,
                    SortOrder = row.SortOrder,
                    Remark = row.Remark,
                    CreatedAt = row.CreatedAt,
                    UpdatedAt = row.UpdatedAt
                },
                Runtime = new DeviceRuntimeModel
                {
                    DeviceId = row.DeviceId,
                    DeviceState = row.RuntimeDeviceState ?? "Disconnected",
                    LastUpdateTime = row.RuntimeLastUpdateTime,
                    LastHeartbeatTime = row.RuntimeLastHeartbeatTime,
                    LastStateChangeTime = row.RuntimeLastStateChangeTime,
                    CurrentOrderNo = row.RuntimeCurrentOrderNo,
                    UpdatedAt = row.RuntimeUpdatedAt
                }
            });
        }

        public async Task<int> UpdateDeviceStateAsync(int deviceId, string deviceState, DateTime? lastUpDateTime = null)
        {
            const string sql = @"UPDATE T_DeviceRuntime
                                 SET DeviceState = @DeviceState,
                                     LastUpdateTime = COALESCE(@LastUpdateTime, LastUpdateTime, GETDATE()),
                                     LastHeartbeatTime = CASE
                                                             WHEN @DeviceState = 'Running'
                                                                 THEN COALESCE(@LastUpdateTime, GETDATE())
                                                             ELSE LastHeartbeatTime
                                                         END,
                                     LastStateChangeTime = CASE
                                                               WHEN DeviceState <> @DeviceState OR LastStateChangeTime IS NULL
                                                                   THEN COALESCE(@LastUpdateTime, GETDATE())
                                                               ELSE LastStateChangeTime
                                                           END,
                                     UpdatedAt = SYSDATETIME()
                                 WHERE DeviceId = @DeviceId;";
            return await _db.ExecuteAsync(sql, new { DeviceId = deviceId, DeviceState = deviceState, LastUpdateTime = lastUpDateTime });
        }

        public async Task<int> UpdateDeviceAsync(DeviceModel device)
        {
            const string sql = @"UPDATE T_DeviceMaster
                         SET DeviceName   = @DeviceName,
                             DeviceCode   = COALESCE(NULLIF(@DeviceCode, ''), DeviceCode),
                             DeviceType   = COALESCE(NULLIF(@DeviceType, ''), DeviceType),
                             WorkshopName = COALESCE(NULLIF(@WorkshopName, ''), WorkshopName),
                             LineName     = COALESCE(NULLIF(@LineName, ''), LineName),
                             StationName  = COALESCE(NULLIF(@StationName, ''), StationName),
                             IpAddress    = @IpAddress,
                             Port         = ISNULL(@Port, Port),
                             SerialPort   = @SerialPort,
                             SlaveId      = ISNULL(@SlaveId, SlaveId),
                             Remark       = COALESCE(NULLIF(@Remark, ''), Remark),
                             UpdatedAt    = SYSDATETIME()
                          WHERE DeviceId   = @DeviceId;";
            return await _db.ExecuteAsync(sql, device);
        }

        public async Task<int> SetDeviceEnabledAsync(int deviceId, bool isEnabled, DateTime? changedAt = null)
        {
            const string sql = @"UPDATE T_DeviceMaster
                                 SET IsEnabled = @IsEnabled,
                                     UpdatedAt = SYSDATETIME()
                                 WHERE DeviceId = @DeviceId;

                                 UPDATE T_DeviceRuntime
                                 SET DeviceState = CASE WHEN @IsEnabled = 1 THEN 'Disconnected' ELSE 'Disabled' END,
                                     LastUpdateTime = ISNULL(@ChangedAt, GETDATE()),
                                     LastStateChangeTime = ISNULL(@ChangedAt, GETDATE()),
                                     UpdatedAt = SYSDATETIME()
                                 WHERE DeviceId = @DeviceId;";

            return await _db.ExecuteAsync(sql, new
            {
                DeviceId = deviceId,
                IsEnabled = isEnabled,
                ChangedAt = changedAt
            });
        }

        public async Task<int> InsertDeviceAsync(DeviceModel device)
        {
            const string sql = @"DECLARE @NewDeviceId int;

                                 INSERT INTO T_DeviceMaster
                                 (
                                     DeviceName,
                                     DeviceCode,
                                     DeviceType,
                                     WorkshopName,
                                     LineName,
                                     StationName,
                                     IpAddress,
                                     Port,
                                     SerialPort,
                                     SlaveId,
                                     IsEnabled,
                                     Criticality,
                                     SortOrder,
                                     Remark
                                 )
                                 VALUES
                                 (
                                     @DeviceName,
                                     @DeviceCode,
                                     @DeviceType,
                                     @WorkshopName,
                                     @LineName,
                                     @StationName,
                                     @IpAddress,
                                     @Port,
                                     @SerialPort,
                                     @SlaveId,
                                     1,
                                     CASE WHEN @Criticality = 0 THEN 2 ELSE @Criticality END,
                                     @SortOrder,
                                     @Remark
                                 );

                                 SET @NewDeviceId = CAST(SCOPE_IDENTITY() AS int);

                                 INSERT INTO T_DeviceRuntime
                                 (
                                     DeviceId,
                                     DeviceState,
                                     CurrentOrderNo,
                                     LastUpdateTime,
                                     LastHeartbeatTime,
                                     LastStateChangeTime
                                 )
                                 VALUES
                                 (
                                     @NewDeviceId,
                                     'Disconnected',
                                     NULL,
                                     GETDATE(),
                                     NULL,
                                     GETDATE()
                                 );

                                 SELECT @NewDeviceId;";
            return await _db.ExecuteScalarAsync<int>(sql, device);
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
                                 (DeviceId, AlarmCode, AlarmLevel, AlarmSource, AlarmMessage, AlarmTime, IsAck, AckUserId, AckTime, RecoverTime)
                                 VALUES (
                                     @DeviceId,
                                     @AlarmCode,
                                     COALESCE(NULLIF(@AlarmLevel, ''), 'Warning'),
                                     COALESCE(NULLIF(@AlarmSource, ''), 'System'),
                                     @AlarmMessage,
                                     @AlarmTime,
                                     @IsAck,
                                     @AckUserId,
                                     @AckTime,
                                     @RecoverTime
                                 );";
            return await _db.ExecuteAsync(sql, alarmRecord);
        }

        public async Task<IEnumerable<AlarmRecordModel>> GetUnAckAlarmsAsync(int top = 20)
        {
            const string sql = @"SELECT TOP (@Top) AlarmId, DeviceId, AlarmCode, AlarmLevel, AlarmSource, AlarmMessage, AlarmTime, IsAck, AckUserId, AckTime, RecoverTime, CreatedAt
                                 FROM T_AlarmRecord
                                 WHERE IsAck = 0
                                 ORDER BY AlarmTime DESC;";
            return await _db.QueryAsync<AlarmRecordModel>(sql, new { Top = top });
        }

        public async Task<int> AckAlarmAsync(int alarmId)
        {
            const string sql = @"UPDATE T_AlarmRecord
                                 SET IsAck = 1,
                                     AckTime = COALESCE(AckTime, GETDATE())
                                 WHERE AlarmId = @AlarmId;";
            return await _db.ExecuteAsync(sql, new { AlarmId = alarmId });
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
            const string sql = @"INSERT INTO T_User (UserName, Account, PasswordHash, Salt, Email)
                                 VALUES(@UserName, @Account, @PasswordHash, @Salt, @Email)";
            return await _db.ExecuteAsync(sql, new
            {
                UserName = newUser.UserName,
                Account = newUser.Account,
                PasswordHash = newUser.PasswordHash,
                Salt = newUser.Salt,
                Email = newUser.Email
            });
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

        private sealed class MonitoredDeviceRow
        {
            public int DeviceId { get; set; }
            public string? DeviceName { get; set; }
            public string? DeviceCode { get; set; }
            public string? DeviceType { get; set; }
            public string? WorkshopName { get; set; }
            public string? LineName { get; set; }
            public string? StationName { get; set; }
            public string? IpAddress { get; set; }
            public int? Port { get; set; }
            public string? SerialPort { get; set; }
            public byte? SlaveId { get; set; }
            public bool IsEnabled { get; set; }
            public byte Criticality { get; set; }
            public int SortOrder { get; set; }
            public string? Remark { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string? RuntimeDeviceState { get; set; }
            public DateTime RuntimeLastUpdateTime { get; set; }
            public DateTime? RuntimeLastHeartbeatTime { get; set; }
            public DateTime? RuntimeLastStateChangeTime { get; set; }
            public string? RuntimeCurrentOrderNo { get; set; }
            public DateTime RuntimeUpdatedAt { get; set; }
        }
    }
}

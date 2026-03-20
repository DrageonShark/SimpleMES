using SimpleMES.Models;

using SimpleMES.Models.Dto;

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
                             DeviceCode   = @DeviceCode,
                             DeviceType   = @DeviceType,
                             WorkshopName = @WorkshopName,
                             LineName     = @LineName,
                             StationName  = @StationName,
                             IpAddress    = @IpAddress,
                             Port         = @Port,
                             SerialPort   = @SerialPort,
                             SlaveId      = @SlaveId,
                             Criticality  = CASE WHEN @Criticality = 0 THEN 2 ELSE @Criticality END,
                             SortOrder    = @SortOrder,
                             Remark       = @Remark,
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
                                     @IsEnabled,
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
                                     CASE WHEN @IsEnabled = 1 THEN 'Disconnected' ELSE 'Disabled' END,
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
                                 );

                                 SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await _db.ExecuteScalarAsync<int>(sql, alarmRecord);
        }

        public async Task<long> InsertDeviceEventAsync(DeviceEventModel deviceEvent)
        {
            if (deviceEvent == null)
            {
                throw new ArgumentNullException(nameof(deviceEvent));
            }

            const string sql = @"INSERT INTO T_DeviceEvent
                                 (
                                     DeviceId,
                                     EventType,
                                     EventLevel,
                                     EventMessage,
                                     SnapshotState,
                                     OccurredAt,
                                     RelatedAlarmId,
                                     IsResolved,
                                     ResolvedAt,
                                     ConfirmedByUserId,
                                     ConfirmedAt,
                                     ResolutionNote
                                 )
                                 VALUES
                                 (
                                     @DeviceId,
                                     @EventType,
                                     COALESCE(NULLIF(@EventLevel, ''), 'Info'),
                                     @EventMessage,
                                     @SnapshotState,
                                     @OccurredAt,
                                     @RelatedAlarmId,
                                     @IsResolved,
                                     @ResolvedAt,
                                     @ConfirmedByUserId,
                                     @ConfirmedAt,
                                     @ResolutionNote
                                 );

                                 SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            return await _db.ExecuteScalarAsync<long>(sql, deviceEvent);
        }

        public async Task<IEnumerable<DeviceEventDto>> GetRecentDeviceEventsAsync(int top = 20)
        {
            const string sql = @"SELECT TOP (@Top)
                                     e.EventId,
                                     e.DeviceId,
                                     m.DeviceName,
                                     m.DeviceCode,
                                     m.WorkshopName,
                                     m.LineName,
                                     m.StationName,
                                     e.EventType,
                                     e.EventLevel,
                                     e.EventMessage,
                                     e.SnapshotState,
                                     e.OccurredAt,
                                     e.RelatedAlarmId,
                                     a.AlarmCode AS RelatedAlarmCode,
                                     a.AlarmLevel AS RelatedAlarmLevel,
                                     a.AlarmMessage AS RelatedAlarmMessage,
                                     a.AlarmTime AS RelatedAlarmTime,
                                     a.IsAck AS RelatedAlarmIsAck,
                                     a.AckTime AS RelatedAlarmAckTime,
                                     a.RecoverTime AS RelatedAlarmRecoverTime,
                                     e.IsResolved,
                                     e.ResolvedAt,
                                     e.ConfirmedByUserId,
                                     u.UserName AS ConfirmedByUserName,
                                     e.ConfirmedAt,
                                     e.ResolutionNote
                                 FROM T_DeviceEvent e
                                 INNER JOIN T_DeviceMaster m ON m.DeviceId = e.DeviceId
                                 LEFT JOIN T_AlarmRecord a ON a.AlarmId = e.RelatedAlarmId
                                 LEFT JOIN T_User u ON u.UserId = e.ConfirmedByUserId
                                 ORDER BY e.OccurredAt DESC, e.EventId DESC;";
            return await _db.QueryAsync<DeviceEventDto>(sql, new { Top = top });
        }

        public async Task<DeviceEventQueryResult> GetDeviceEventsPageAsync(DeviceEventQueryCriteria criteria)
        {
            if (criteria == null)
            {
                throw new ArgumentNullException(nameof(criteria));
            }

            var normalizedKeyword = string.IsNullOrWhiteSpace(criteria.Keyword) ? null : criteria.Keyword.Trim();
            var normalizedLevel = string.IsNullOrWhiteSpace(criteria.EventLevel) ? null : criteria.EventLevel.Trim();
            var normalizedStatus = string.IsNullOrWhiteSpace(criteria.ProcessingStatus) ? null : criteria.ProcessingStatus.Trim();
            var normalizedType = string.IsNullOrWhiteSpace(criteria.EventType) ? null : criteria.EventType.Trim();
            var skip = Math.Max(0, criteria.Skip);
            var take = Math.Clamp(criteria.Take, 1, 200);

            const string sql = @"SELECT
                                     e.EventId,
                                     e.DeviceId,
                                     m.DeviceName,
                                     m.DeviceCode,
                                     m.WorkshopName,
                                     m.LineName,
                                     m.StationName,
                                     e.EventType,
                                     e.EventLevel,
                                     e.EventMessage,
                                     e.SnapshotState,
                                     e.OccurredAt,
                                     e.RelatedAlarmId,
                                     a.AlarmCode AS RelatedAlarmCode,
                                     a.AlarmLevel AS RelatedAlarmLevel,
                                     a.AlarmMessage AS RelatedAlarmMessage,
                                     a.AlarmTime AS RelatedAlarmTime,
                                     a.IsAck AS RelatedAlarmIsAck,
                                     a.AckTime AS RelatedAlarmAckTime,
                                     a.RecoverTime AS RelatedAlarmRecoverTime,
                                     e.IsResolved,
                                     e.ResolvedAt,
                                     e.ConfirmedByUserId,
                                     u.UserName AS ConfirmedByUserName,
                                     e.ConfirmedAt,
                                     e.ResolutionNote
                                 INTO #Filtered
                                 FROM T_DeviceEvent e
                                 INNER JOIN T_DeviceMaster m ON m.DeviceId = e.DeviceId
                                 LEFT JOIN T_AlarmRecord a ON a.AlarmId = e.RelatedAlarmId
                                 LEFT JOIN T_User u ON u.UserId = e.ConfirmedByUserId
                                 WHERE (@Keyword IS NULL
                                        OR e.EventMessage LIKE '%' + @Keyword + '%'
                                        OR m.DeviceName LIKE '%' + @Keyword + '%'
                                        OR m.DeviceCode LIKE '%' + @Keyword + '%'
                                        OR m.WorkshopName LIKE '%' + @Keyword + '%'
                                        OR m.LineName LIKE '%' + @Keyword + '%'
                                        OR m.StationName LIKE '%' + @Keyword + '%'
                                        OR a.AlarmMessage LIKE '%' + @Keyword + '%')
                                   AND (
                                       @EventLevel IS NULL
                                       OR (@EventLevel = 'Critical' AND e.EventLevel = 'Critical')
                                       OR (@EventLevel = 'Warning' AND e.EventLevel = 'Warning')
                                       OR (@EventLevel = 'Info' AND ISNULL(e.EventLevel, 'Info') NOT IN ('Critical', 'Warning'))
                                   )
                                   AND (@DeviceId IS NULL OR e.DeviceId = @DeviceId)
                                   AND (@EventType IS NULL OR e.EventType = @EventType)
                                   AND (@OccurredFrom IS NULL OR e.OccurredAt >= @OccurredFrom)
                                   AND (
                                       @ProcessingStatus IS NULL
                                       OR (
                                           @ProcessingStatus = 'Pending'
                                           AND (e.RelatedAlarmId IS NOT NULL OR e.EventType = 'FaultRaised')
                                           AND e.ConfirmedAt IS NULL
                                           AND e.IsResolved = 0
                                       )
                                       OR (
                                           @ProcessingStatus = 'AwaitingConfirmation'
                                           AND (e.RelatedAlarmId IS NOT NULL OR e.EventType = 'FaultRaised')
                                           AND e.ConfirmedAt IS NULL
                                           AND e.IsResolved = 1
                                       )
                                       OR (
                                           @ProcessingStatus = 'Confirmed'
                                           AND e.ConfirmedAt IS NOT NULL
                                       )
                                       OR (
                                           @ProcessingStatus = 'Recorded'
                                           AND e.ConfirmedAt IS NULL
                                           AND e.RelatedAlarmId IS NULL
                                           AND e.EventType <> 'FaultRaised'
                                       )
                                   );

                                 SELECT COUNT(1) AS TotalCount FROM #Filtered;

                                 SELECT *
                                 FROM #Filtered
                                 ORDER BY OccurredAt DESC, EventId DESC
                                 OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                                 DROP TABLE #Filtered;";

            var (countRows, items) = await _db.QueryMultipleAsync<CountRow, DeviceEventDto>(sql, new
            {
                Keyword = normalizedKeyword,
                EventLevel = normalizedLevel,
                ProcessingStatus = normalizedStatus,
                DeviceId = criteria.DeviceId,
                EventType = normalizedType,
                OccurredFrom = criteria.OccurredFrom,
                Skip = skip,
                Take = take
            });

            return new DeviceEventQueryResult
            {
                TotalCount = countRows.FirstOrDefault()?.TotalCount ?? 0,
                Items = items.ToList().AsReadOnly()
            };
        }

        public async Task<IEnumerable<AlarmRecordModel>> GetUnAckAlarmsAsync(int top = 20)
        {
            const string sql = @"SELECT TOP (@Top) AlarmId, DeviceId, AlarmCode, AlarmLevel, AlarmSource, AlarmMessage, AlarmTime, IsAck, AckUserId, AckTime, RecoverTime, CreatedAt
                                 FROM T_AlarmRecord
                                 WHERE IsAck = 0
                                 ORDER BY AlarmTime DESC;";
            return await _db.QueryAsync<AlarmRecordModel>(sql, new { Top = top });
        }

        public async Task<int> AckAlarmAsync(int alarmId, int? ackUserId = null, DateTime? ackTime = null)
        {
            const string sql = @"UPDATE T_AlarmRecord
                                 SET IsAck = 1,
                                     AckUserId = COALESCE(@AckUserId, AckUserId),
                                     AckTime = COALESCE(@AckTime, AckTime, GETDATE())
                                 WHERE AlarmId = @AlarmId;";
            return await _db.ExecuteAsync(sql, new { AlarmId = alarmId, AckUserId = ackUserId, AckTime = ackTime });
        }

        public async Task<int> MarkAlarmRecoveredAsync(int alarmId, DateTime recoveredAt)
        {
            const string sql = @"UPDATE T_AlarmRecord
                                 SET RecoverTime = COALESCE(RecoverTime, @RecoveredAt)
                                 WHERE AlarmId = @AlarmId;";
            return await _db.ExecuteAsync(sql, new { AlarmId = alarmId, RecoveredAt = recoveredAt });
        }

        public async Task<int> ResolveDeviceEventsByAlarmAsync(int alarmId, DateTime resolvedAt)
        {
            const string sql = @"UPDATE T_DeviceEvent
                                 SET IsResolved = 1,
                                     ResolvedAt = COALESCE(ResolvedAt, @ResolvedAt)
                                 WHERE RelatedAlarmId = @AlarmId
                                   AND IsResolved = 0;";
            return await _db.ExecuteAsync(sql, new { AlarmId = alarmId, ResolvedAt = resolvedAt });
        }

        public async Task<int> ConfirmDeviceEventAsync(long eventId, int confirmedByUserId, DateTime confirmedAt, string? resolutionNote)
        {
            const string sql = @"UPDATE T_DeviceEvent
                                 SET ConfirmedByUserId = COALESCE(@ConfirmedByUserId, ConfirmedByUserId),
                                     ConfirmedAt = COALESCE(ConfirmedAt, @ConfirmedAt),
                                     ResolutionNote = CASE
                                                          WHEN NULLIF(@ResolutionNote, '') IS NULL
                                                              THEN ResolutionNote
                                                          ELSE @ResolutionNote
                                                      END
                                 WHERE EventId = @EventId;";
            return await _db.ExecuteAsync(sql, new
            {
                EventId = eventId,
                ConfirmedByUserId = confirmedByUserId,
                ConfirmedAt = confirmedAt,
                ResolutionNote = resolutionNote
            });
        }

        public async Task<int> ConfirmDeviceEventsByAlarmAsync(int alarmId, int confirmedByUserId, DateTime confirmedAt, string? resolutionNote)
        {
            const string sql = @"UPDATE T_DeviceEvent
                                 SET ConfirmedByUserId = COALESCE(@ConfirmedByUserId, ConfirmedByUserId),
                                     ConfirmedAt = COALESCE(ConfirmedAt, @ConfirmedAt),
                                     ResolutionNote = CASE
                                                          WHEN NULLIF(@ResolutionNote, '') IS NULL
                                                              THEN ResolutionNote
                                                          ELSE @ResolutionNote
                                                      END
                                 WHERE RelatedAlarmId = @AlarmId
                                   AND ConfirmedAt IS NULL;";
            return await _db.ExecuteAsync(sql, new
            {
                AlarmId = alarmId,
                ConfirmedByUserId = confirmedByUserId,
                ConfirmedAt = confirmedAt,
                ResolutionNote = resolutionNote
            });
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

        private sealed class CountRow
        {
            public int TotalCount { get; set; }
        }
    }
}

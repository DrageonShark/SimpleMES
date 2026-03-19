-- SimpleMES 数据库初始化脚本
-- 对于分立式设备架构，运行此脚本前建议先手动删除并重建 SimpleMES_DB

-- 1. 如果数据库不存在，则创建数据库
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'SimpleMES_DB')
BEGIN
    CREATE DATABASE SimpleMES_DB;
END
GO

USE SimpleMES_DB;
GO

/* ------------------------------------------------------------
   设备主表 (T_DeviceMaster)
   用于存储工厂中所有设备的基础配置信息
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_DeviceMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_DeviceMaster
    (
        DeviceId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_T_DeviceMaster PRIMARY KEY, -- 设备内部ID (自增)
        DeviceName nvarchar(50) NOT NULL,    -- 设备名称
        DeviceCode nvarchar(50) NULL,        -- 设备编号 (唯一识别码)
        DeviceType nvarchar(50) NULL,        -- 设备类型 (如：注塑机、冲压机)
        WorkshopName nvarchar(50) NULL,      -- 车间名称
        LineName nvarchar(50) NULL,          -- 产线名称
        StationName nvarchar(50) NULL,       -- 工位名称
        IpAddress nvarchar(50) NULL,         -- IP地址 (用于网络通讯)
        Port int NULL CONSTRAINT DF_T_DeviceMaster_Port DEFAULT (502), -- 通讯端口 (默认Modbus 502)
        SerialPort nvarchar(50) NULL,        -- 串口名称 (如：COM1)
        SlaveId tinyint NULL CONSTRAINT DF_T_DeviceMaster_SlaveId DEFAULT (1), -- 从站ID
        IsEnabled bit NOT NULL CONSTRAINT DF_T_DeviceMaster_IsEnabled DEFAULT (1), -- 是否启用 (1-启用, 0-禁用)
        Criticality tinyint NOT NULL CONSTRAINT DF_T_DeviceMaster_Criticality DEFAULT (2), -- 关键程度 (1-低, 2-中, 3-高)
        SortOrder int NOT NULL CONSTRAINT DF_T_DeviceMaster_SortOrder DEFAULT (0), -- 排序优先级
        Remark nvarchar(200) NULL,           -- 备注信息
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_T_DeviceMaster_CreatedAt DEFAULT (SYSDATETIME()), -- 创建时间
        UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_T_DeviceMaster_UpdatedAt DEFAULT (SYSDATETIME())  -- 更新时间
    );
END
GO

-- 为设备编号创建唯一索引
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_T_DeviceMaster_DeviceCode' AND object_id = OBJECT_ID(N'dbo.T_DeviceMaster'))
BEGIN
    CREATE UNIQUE INDEX IX_T_DeviceMaster_DeviceCode ON dbo.T_DeviceMaster(DeviceCode) WHERE DeviceCode IS NOT NULL;
END
GO

-- 为位置信息创建索引，优化按车间/产线查询的速度
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_T_DeviceMaster_Location' AND object_id = OBJECT_ID(N'dbo.T_DeviceMaster'))
BEGIN
    CREATE INDEX IX_T_DeviceMaster_Location ON dbo.T_DeviceMaster(WorkshopName, LineName, StationName);
END
GO

/* ------------------------------------------------------------
   设备运行时表 (T_DeviceRuntime)
   存储设备的实时状态，通常由采集程序频繁更新
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_DeviceRuntime', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_DeviceRuntime
    (
        DeviceId int NOT NULL CONSTRAINT PK_T_DeviceRuntime PRIMARY KEY, -- 设备ID (外键)
        DeviceState nvarchar(20) NOT NULL CONSTRAINT DF_T_DeviceRuntime_DeviceState DEFAULT (N'Disconnected'), -- 设备状态 (运行/断开/故障/禁用)
        CurrentOrderNo nvarchar(50) NULL,    -- 当前正在生产的订单号
        LastUpdateTime datetime NOT NULL CONSTRAINT DF_T_DeviceRuntime_LastUpdateTime DEFAULT (GETDATE()), -- 最后数据更新时间
        Last CmmunicationTime datetime NULL,     -- 最后一次通信通讯时间
        LastStateChangeTime datetime NULL,   -- 最后一次状态切换时间
        UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_T_DeviceRuntime_UpdatedAt DEFAULT (SYSDATETIME()), -- 记录修改时间
        CONSTRAINT FK_T_DeviceRuntime_Device FOREIGN KEY (DeviceId) REFERENCES dbo.T_DeviceMaster(DeviceId) -- 关联主表
    );
END
GO

-- 状态约束：只允许预定义的四种状态
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_T_DeviceRuntime_DeviceState' AND parent_object_id = OBJECT_ID(N'dbo.T_DeviceRuntime'))
BEGIN
    ALTER TABLE dbo.T_DeviceRuntime ADD CONSTRAINT CK_T_DeviceRuntime_DeviceState CHECK (DeviceState IN (N'Running', N'Disconnected', N'Fault', N'Disabled'));
END
GO

/* ------------------------------------------------------------
   产品定义表 (T_Products)
   存储生产工艺参数标准
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_Products
    (
        ProductCode nvarchar(50) NOT NULL CONSTRAINT PK_T_Products PRIMARY KEY, -- 产品代码
        ProductName nvarchar(100) NOT NULL,  -- 产品名称
        SetTemperature float NOT NULL,       -- 工艺设定温度
        SetPressure float NOT NULL CONSTRAINT DF_T_Products_SetPressure DEFAULT (0), -- 工艺设定压力
        Description nvarchar(200) NULL       -- 产品描述
    );
END
GO

-- 插入一些演示产品数据
IF NOT EXISTS (SELECT 1 FROM dbo.T_Products WHERE ProductCode = N'PROD_A')
BEGIN
    INSERT INTO dbo.T_Products (ProductCode, ProductName, SetTemperature, SetPressure, Description)
    VALUES (N'PROD_A', N'Phone Case', 60.5, 100, N'手机壳示例产品');
END
GO

/* ------------------------------------------------------------
   生产订单表 (T_ProductionOrders)
   管理生产任务的执行进度
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_ProductionOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_ProductionOrders
    (
        OrderNo nvarchar(50) NOT NULL CONSTRAINT PK_T_ProductionOrders PRIMARY KEY, -- 订单号
        ProductCode nvarchar(50) NOT NULL,    -- 产品代码 (关联产品表)
        PlanQty int NOT NULL,                 -- 计划生产数量
        CompletedQty int NOT NULL CONSTRAINT DF_T_ProductionOrders_CompletedQty DEFAULT (0), -- 已完成数量
        OrderStatus nvarchar(20) NOT NULL CONSTRAINT DF_T_ProductionOrders_OrderStatus DEFAULT (N'Pending'), -- 订单状态 (待生产/运行中等)
        StartTime datetime NULL,              -- 实际开始时间
        EndTime datetime NULL,                -- 实际完成时间
        LastOperationTime datetime NULL,      -- 最后操作时间
        CreateTime datetime NOT NULL CONSTRAINT DF_T_ProductionOrders_CreateTime DEFAULT (GETDATE()), -- 创建时间
        CONSTRAINT FK_T_ProductionOrders_Product FOREIGN KEY (ProductCode) REFERENCES dbo.T_Products(ProductCode)
    );
END
GO

/* ------------------------------------------------------------
   生产记录/参数历史表 (T_ProductionRecords)
   存储设备采集到的实时工艺参数历史
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_ProductionRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_ProductionRecords
    (
        RecordId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_T_ProductionRecords PRIMARY KEY, -- 记录ID
        DeviceId int NOT NULL,                -- 设备ID
        Temperature decimal(10,2) NULL,       -- 采集到的温度
        Pressure decimal(10,2) NULL,          -- 采集到的压力
        Speed int NULL,                       -- 运行速度
        RecordTime datetime NOT NULL CONSTRAINT DF_T_ProductionRecords_RecordTime DEFAULT (GETDATE()), -- 记录采集时间
        CONSTRAINT FK_T_ProductionRecords_Device FOREIGN KEY (DeviceId) REFERENCES dbo.T_DeviceMaster(DeviceId)
    );
END
GO

-- 优化索引：加速按设备和时间查询历史趋势
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_T_ProductionRecords_Device_RecordTime' AND object_id = OBJECT_ID(N'dbo.T_ProductionRecords'))
BEGIN
    CREATE INDEX IX_T_ProductionRecords_Device_RecordTime ON dbo.T_ProductionRecords(DeviceId, RecordTime DESC);
END
GO

/* ------------------------------------------------------------
   报警记录表 (T_AlarmRecord)
   存储设备发生的各种报警及确认情况
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_AlarmRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_AlarmRecord
    (
        AlarmId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_T_AlarmRecord PRIMARY KEY, -- 报警ID
        DeviceId int NOT NULL,                -- 报警设备ID
        AlarmCode nvarchar(50) NULL,          -- 报警代码
        AlarmLevel nvarchar(20) NOT NULL CONSTRAINT DF_T_AlarmRecord_AlarmLevel DEFAULT (N'Warning'), -- 报警级别 (Info/Warning/Critical)
        AlarmSource nvarchar(20) NOT NULL CONSTRAINT DF_T_AlarmRecord_AlarmSource DEFAULT (N'System'), -- 报警来源 (设备/系统)
        AlarmMessage nvarchar(200) NOT NULL,  -- 报警详细内容
        AlarmTime datetime NOT NULL CONSTRAINT DF_T_AlarmRecord_AlarmTime DEFAULT (GETDATE()), -- 报警发生时间
        IsAck bit NOT NULL CONSTRAINT DF_T_AlarmRecord_IsAck DEFAULT (0),      -- 是否确认 (1-已确认, 0-未确认)
        AckUserId int NULL,                   -- 确认人ID
        AckTime datetime NULL,                 -- 确认时间
        RecoverTime datetime NULL,             -- 报警恢复时间
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_T_AlarmRecord_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_T_AlarmRecord_Device FOREIGN KEY (DeviceId) REFERENCES dbo.T_DeviceMaster(DeviceId)
    );
END
GO

/* ------------------------------------------------------------
   用户权限表 (T_User)
   管理系统登录用户
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_User', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_User
    (
        UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_T_User PRIMARY KEY, -- 用户ID
        UserName nvarchar(60) NOT NULL,       -- 用户真实姓名
        Role int NOT NULL CONSTRAINT DF_T_User_Role DEFAULT (3), -- 角色级别 (1-管理员, 2-班长, 3-操作工)
        Account nvarchar(50) NOT NULL UNIQUE, -- 登录账号 (唯一)
        PasswordHash nvarchar(255) NOT NULL,  -- 加密后的密码
        Salt nvarchar(128) NULL,              -- 密码盐值
        Email nvarchar(100) NULL UNIQUE,      -- 电子邮箱
        IsActive bit NOT NULL CONSTRAINT DF_T_User_IsActive DEFAULT (1) -- 账号是否激活
    );
END
GO

-- 在创建完用户表后，建立报警确认人与用户表的关联
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_T_AlarmRecord_AckUser')
BEGIN
    ALTER TABLE dbo.T_AlarmRecord ADD CONSTRAINT FK_T_AlarmRecord_AckUser FOREIGN KEY (AckUserId) REFERENCES dbo.T_User(UserId);
END
GO

/* ------------------------------------------------------------
   设备事件表 (T_DeviceEvent)
   记录设备的全生命周期重要事件（非生产数据，如开关机、状态变化）
   ------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.T_DeviceEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.T_DeviceEvent
    (
        EventId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_T_DeviceEvent PRIMARY KEY,
        DeviceId int NOT NULL,                -- 设备ID
        EventType nvarchar(30) NOT NULL,      -- 事件类型
        EventLevel nvarchar(20) NOT NULL CONSTRAINT DF_T_DeviceEvent_EventLevel DEFAULT (N'Info'), -- 事件等级
        EventMessage nvarchar(200) NOT NULL,  -- 事件内容描述
        SnapshotState nvarchar(20) NULL,      -- 事件发生时的设备状态快照
        OccurredAt datetime NOT NULL CONSTRAINT DF_T_DeviceEvent_OccurredAt DEFAULT (GETDATE()), -- 发生时间
        RelatedAlarmId int NULL,              -- 关联的报警ID (如果有)
        IsResolved bit NOT NULL CONSTRAINT DF_T_DeviceEvent_IsResolved DEFAULT (0), -- 系统是否已恢复/解除
        ResolvedAt datetime NULL,             -- 系统恢复时间
        ConfirmedByUserId int NULL,           -- 人工确认人ID
        ConfirmedAt datetime NULL,            -- 人工确认时间
        ResolutionNote nvarchar(500) NULL,    -- 人工处理说明 / 原因备注
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_T_DeviceEvent_CreatedAt DEFAULT (SYSDATETIME()), -- 创建时间
        CONSTRAINT FK_T_DeviceEvent_Device FOREIGN KEY (DeviceId) REFERENCES dbo.T_DeviceMaster(DeviceId),
        CONSTRAINT FK_T_DeviceEvent_Alarm FOREIGN KEY (RelatedAlarmId) REFERENCES dbo.T_AlarmRecord(AlarmId),
        CONSTRAINT FK_T_DeviceEvent_ConfirmedUser FOREIGN KEY (ConfirmedByUserId) REFERENCES dbo.T_User(UserId)
    );
END
GO

/* ------------------------------------------------------------
   预设演示数据：种子设备
   ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM dbo.T_DeviceMaster)
BEGIN
    -- 插入三个示例设备：注塑机、冲压机、包装机
    INSERT INTO dbo.T_DeviceMaster (DeviceName, DeviceCode, DeviceType, WorkshopName, LineName, StationName, IpAddress, Port, SerialPort, IsEnabled, Criticality, SortOrder, Remark)
    VALUES
    (N'注塑机-A01', N'DEV-A01', N'Injection', N'1号车间', N'注塑线', N'S01', N'127.0.0.1', 501, NULL, 1, 3, 10, N'核心生产设备'),
    (N'冲压机-B02', N'DEV-B02', N'Stamping', N'1号车间', N'冲压线', N'S02', N'127.0.0.1', 502, NULL, 1, 2, 20, N'二号压力设备'),
    (N'包装机-C03', N'DEV-C03', N'Packing', N'2号车间', N'包装线', N'S03', NULL, NULL, N'COM1', 1, 1, 30, N'末端包装站');

    -- 同步为每个设备初始化运行时状态
    INSERT INTO dbo.T_DeviceRuntime (DeviceId, DeviceState, LastUpdateTime, LastHeartbeatTime, LastStateChangeTime)
    SELECT DeviceId, N'Disconnected', GETDATE(), NULL, GETDATE() FROM dbo.T_DeviceMaster;
END
GO

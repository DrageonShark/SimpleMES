--创建数据库 (如果不存在)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SimpleMES_DB')
BEGIN
    CREATE DATABASE SimpleMES_DB;
END
GO

USE SimpleMES_DB;
GO

--创建设备表 (Devices)
-- 存储生产线上的设备基础信息及当前状态
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID (N'[dbo].[T_Devices]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[T_Devices](
		DeviceId int IDENTITY(1,1) NOT NULL PRIMARY KEY, --设备ID
		DeviceName nvarchar(50) NOT NULL, --设备名
		IpAddress nvarchar(20) NULL, --设备IP (Modbus TCP)
		Port int DEFAULT 502 NULL, --端口
		SerialPort nvarchar(50) NULL, --串口名
        SlaveId tinyint NOT NULL DEFAULT 1, --从站ID
		Status nvarchar(20) DEFAULT 'Stopped', --状态: Running/Stopped/Fault
		LastUpdateTime datetime DEFAULT GETDATE() --最后通信时间
		);
		
		--插入模拟数据(3台设备)
		INSERT INTO T_Devices (DeviceName, IpAddress, Statu) VALUES
		('注塑机-A01', '127.0.0.1', 'Stopped'),
		('冲压机-B02', '127.0.0.1', 'Stopped');
		INSERT INTO T_Devices (DeviceName, SerialPort, Statu) VALUES
		('包装机-C03', 'COM1', 'Stopped');
END;
GO


--创建产品表
--产品温度，压力
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.T_Products') AND type in (N'U'))
BEGIN
	CREATE TABLE dbo.T_Products (
		ProductCode nvarchar(50) NOT NULL PRIMARY KEY, --产品编号
		ProductName nvarchar(100) NOT NULL, --产品名
		SetTemperature FLOAT NOT NULL,        -- 设定温度 (配方参数1)
		SetPressure FLOAT DEFAULT 0,          -- 设定压力 (配方参数2)
		Description NVARCHAR(200)             -- 备注
	);
	--插入两种产品配方
	INSERT INTO T_Products (ProductCode, ProductName, SetTemperature, SetPressure) 
	VALUES ('PROD_A', '苹果手机壳', 60.5, 100);

	INSERT INTO T_Products (ProductCode, ProductName, SetTemperature, SetPressure)
	VALUES ('PROD_B', '特斯拉配件', 120.0, 250);
END
GO


--创建生产订单表
--管理生产人物，包含计划数和完成数
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.T_ProductionOrders') AND type in (N'U'))
BEGIN
	CREATE TABLE dbo.T_ProductionOrders (
		OrderNo nvarchar(50) NOT NULL PRIMARY KEY, --订单号
		ProductCode nvarchar(50) FOREIGN KEY REFERENCES T_Products (ProductCode), --产品编号
		PlanQty int NOT NULL, --计划数量
		CompletedQty int DEFAULT 0, --已完成数量
		OrderStatus int	DEFAULT 0, --状态：0=待产, 1=生产中, 2=暂停, 3=完工
		StartTime datetime NULL,  --开始时间
		EndTime datetime NULL, --结束时间
		CreateTime datetime DEFAULT GETDATE()
	);

	-- 插入一条测试订单
    --INSERT INTO T_ProductionOrders (OrderNo, ProductCode, PlanQty, OrderStatus) 
    --VALUES ('MO-20231027001', 'Widget-X', 1000, 0);

END
GO

--创建生产过程记录表
--用于生成历史趋势图，记录核心参数
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.T_ProductionRecords') AND type in (N'U'))
BEGIN
	CREATE TABLE T_ProductionRecords (
		RecordId bigint IDENTITY(1,1) NOT NULL  PRIMARY KEY, 
		DeviceId int FOREIGN KEY REFERENCES T_Devices(DeviceId) NOT NULL, --关联设备ID
		Temperature decimal(10,2) NULL, --温度
		Pressure decimal(10,2) NULL, --压力
		Speed int NULL, --转速
		RecordTime datetime DEFAULT GETDATE() --记录时间
		);
END
GO

--创建报警记录表
--记录设备故障历史
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.T_AlarmRecord') AND type in (N'U'))
BEGIN
	CREATE TABLE T_AlarmRecord (
	AlarmId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
	DeviceId int FOREIGN KEY REFERENCES T_Devices(DeviceId) NOT NULL, --关联设备ID
	AlarmMessage nvarchar(200) NOT NULL, --报警内容
	AlarmTime datetime DEFAULT GETDATE(), --报警时间
	IsAck bit DEFAULT 0 --是否确认 (扩展功能)
	);
END
GO




# SimpleMES 数据库说明

当前统一初始化脚本只有一个文件：[SQLQuery1.sql](./SQLQuery1.sql)。

它已经合并了设备事件中心重构所需的完整结构，包括：

- `T_DeviceMaster` 主数据表
- `T_DeviceRuntime` 运行态表
- `T_AlarmRecord` 告警表
- `T_DeviceEvent` 真实事件流表

其中 `T_DeviceEvent` 已内置以下人工闭环字段：

- `ConfirmedByUserId`
- `ConfirmedAt`
- `ResolutionNote`

这表示脚本同时支持：

- 系统恢复轨迹：`IsResolved`、`ResolvedAt`
- 人工确认轨迹：`ConfirmedByUserId`、`ConfirmedAt`、`ResolutionNote`

语义上仍保持分离：

- 恢复 = 系统事件，例如 `FaultRecovered`、`CommunicationRestored`
- 确认 = 人工动作，例如 `AlarmAcknowledged`

## 使用方式

如果是新库初始化：

1. 在 SSMS 中打开 [SQLQuery1.sql](./SQLQuery1.sql)
2. 直接执行整份脚本

如果后面还有表结构调整，建议继续优先维护这份统一初始化脚本，避免新建库时还要额外拼接升级脚本。

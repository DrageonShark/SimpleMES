# SimpleMES schema notes

The executable bootstrap script is [SQLQuery1.sql](./SQLQuery1.sql). This note explains the device-side schema changes that support a better board design.

## Device master table: `T_DeviceMaster`

`T_DeviceMaster` only stores low-frequency device profile data:

- Identity: `DeviceId`, `DeviceName`, `DeviceCode`, `DeviceType`
- Location: `WorkshopName`, `LineName`, `StationName`
- Communication: `IpAddress`, `Port`, `SerialPort`, `SlaveId`
- Enable/config: `IsEnabled`, `Criticality`, `SortOrder`, `Remark`
- Audit: `CreatedAt`, `UpdatedAt`

This is the table for:

- device management
- line/station grouping
- sort priority
- device configuration

## Device runtime table: `T_DeviceRuntime`

`T_DeviceRuntime` stores high-frequency board state:

- `DeviceState`
- `CurrentOrderNo`
- `LastUpdateTime`
- `LastHeartbeatTime`
- `LastStateChangeTime`
- `UpdatedAt`

Why these fields matter:

- `LastHeartbeatTime` supports "offline for how long" instead of only "latest update time".
- `LastStateChangeTime` supports "fault duration" and board prioritization.
- `CurrentOrderNo` lets the board connect device state with production context.

## Alarm table: `T_AlarmRecord`

The alarm table now includes:

- Alarm identity: `AlarmCode`
- Severity: `AlarmLevel`
- Source: `AlarmSource`
- Ack trail: `AckUserId`, `AckTime`
- Recovery time: `RecoverTime`
- Audit: `CreatedAt`

This is the minimum structure needed for:

- unacknowledged alarm counters
- critical-first sorting
- ack tracing
- alarm duration metrics

## Device event table: `T_DeviceEvent`

`T_DeviceEvent` is added for real timeline data. The current board UI only has device snapshots, but a true timeline should come from events such as:

- state changed
- communication lost
- communication recovered
- fault raised
- fault acknowledged

Suggested UI mapping:

- board timeline -> `T_DeviceEvent`
- exception focus -> `T_DeviceMaster` + `T_DeviceRuntime` + latest active `T_AlarmRecord`
- management filters -> `T_DeviceMaster`

## Compatibility

This script is intended for a clean rebuild after the split-table change.

Recommended execution:

1. Back up the current `SimpleMES_DB` if you need the data.
2. Drop `SimpleMES_DB`.
3. Run [SQLQuery1.sql](./SQLQuery1.sql) once in SSMS.

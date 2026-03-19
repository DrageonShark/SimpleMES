namespace SimpleMES.Services.Security
{
    [Flags]
    public enum UserPermission
    {
        None = 0,
        AddDevice = 1 << 0,
        EditDevice = 1 << 1,
        ToggleDevice = 1 << 2,
        CreateOrder = 1 << 3,
        EditOrder = 1 << 4,
        DeleteOrder = 1 << 5,
        ExecuteOrder = 1 << 6,
        PauseOrder = 1 << 7,
        AckAlarm = 1 << 8,
        All = AddDevice | EditDevice | ToggleDevice | CreateOrder | EditOrder | DeleteOrder | ExecuteOrder | PauseOrder | AckAlarm
    }
}

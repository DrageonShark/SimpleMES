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
        ExecuteOrder = 1 << 4,
        PauseOrder = 1 << 5,
        AckAlarm = 1 << 6,
        All = AddDevice | EditDevice | ToggleDevice | CreateOrder | ExecuteOrder | PauseOrder | AckAlarm
    }
}

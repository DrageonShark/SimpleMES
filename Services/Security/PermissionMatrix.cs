using SimpleMES.Models;

namespace SimpleMES.Services.Security
{
    public static class PermissionMatrix
    {
        /// <summary>
        /// 根据角色分配权限
        /// </summary>
        public static UserPermission GetPermissions(UserModel? user)
        {
            if (user is null || user.IsActive == 0) return UserPermission.None;

            return user.Role switch
            {
                1 => UserPermission.All,
                2 => UserPermission.ToggleDevice
                     | UserPermission.CreateOrder
                     | UserPermission.EditOrder
                     | UserPermission.DeleteOrder
                     | UserPermission.ExecuteOrder
                     | UserPermission.PauseOrder
                     | UserPermission.AckAlarm,
                3 => UserPermission.None,
                _ => UserPermission.None
            };
        }

        public static bool HasPermission(UserModel? user, UserPermission permission)
            => (GetPermissions(user) & permission) == permission;
    }
}

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
                1 => UserPermission.All, // 管理员：全部
                2 => UserPermission.ToggleDevice
                     | UserPermission.CreateOrder
                     | UserPermission.ExecuteOrder
                     | UserPermission.PauseOrder
                     | UserPermission.AckAlarm, // 组长
                3 => UserPermission.None, // 员工
                _ => UserPermission.None //未登录
            };
        }
        /// <summary>
        /// 检查用户是否拥有某个权限
        /// </summary>
        public static bool HasPermission(UserModel? user, UserPermission permission)
            => (GetPermissions(user) & permission) == permission;
    }
}

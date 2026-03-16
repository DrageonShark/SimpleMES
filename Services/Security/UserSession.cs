using CommunityToolkit.Mvvm.ComponentModel;
using SimpleMES.Models;

namespace SimpleMES.Services.Security
{
    public sealed partial class UserSession : ObservableObject
    {
        private static readonly Lazy<UserSession> Lazy = new Lazy<UserSession>(() => new UserSession());
        public static UserSession Current => Lazy.Value;

        private UserSession() { }
        [ObservableProperty]
        private UserModel? _currentUser;
        public void SignIn(UserModel user)
        {
            // 仅保留必要信息，避免在内存中保存密码相关字段
            CurrentUser = new UserModel()
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role,
                Account = user.Account,
                Email = user.Email,
                IsActive = user.IsActive
            };
        }
        public void SignOut()
        {
            CurrentUser = null;
        }
    }
}

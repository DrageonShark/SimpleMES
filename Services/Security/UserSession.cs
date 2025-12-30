using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleMES.Models;

namespace SimpleMES.Services.Security
{
    public sealed class UserSession
    {
        private static readonly Lazy<UserSession> Lazy = new Lazy<UserSession>(() => new UserSession());
        public static UserSession Current => Lazy.Value;

        private UserSession() {}
        public UserModel? CurrentUser { get; private set; }
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

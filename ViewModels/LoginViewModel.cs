using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Security;
using SimpleMES.Services.Toast;

namespace SimpleMES.ViewModels
{
    public partial class LoginViewModel : DialogViewModelBase 
    {
        public event Action? LoginSucceeded;
        private readonly IDataRepository _repository;
        private readonly UserSession _session = UserSession.Current;
        private IToastService _toast;
        [ObservableProperty] private string _account;
        [ObservableProperty] private string _password;
        [ObservableProperty] private string _userName;
        [ObservableProperty] private string _email;

        [ObservableProperty]
        private int _slideIndex;

        [RelayCommand]
        private void GoLogin() => SlideIndex = 0;

        [RelayCommand]
        private void GoRegister() => SlideIndex = 1;
        private string _roleString;

        public LoginViewModel(IDbService repository, IToastService toast)
        {
            _toast = toast;
            _repository = new DataRepository(repository);
        }

        [RelayCommand]
        private async Task Login()
        {
            Log.Information("用户登录");
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
            {
                _toast.Error("账号或密码不能为空或空格", null, 1.5);
                return;
            }
            Log.Information("校验用户登录信息，账号：{Account}", Account);
            var user = await _repository.LoginAsync(Account);
            if (user == null)
            {
                _toast.Error("用户不存在，请注册", null, 2);
                Log.Information("账号[{Account}]不存在", Account);
                return;
            }

            if (!PasswordHasher.VerifyPassword(Password, user.PasswordHash, user.Salt))
            {
                _toast.Error("用户名或密码错误", null, 2);
                Log.Information("账号[{Account}]密码错误", Account);
                return;
            }

            if (user.IsActive == 0)
            {
                _toast.Warning("账号已被禁用，请联系管理员！");
                Log.Information("账号[{Account}]已被禁用", Account);
                return;
            }

            // 同步 ViewModel 的用户名，供后续显示/绑定
            UserName = user.UserName;

            Log.Information("用户[{UserName}]登录成功，用户Id：{UserId}", user.UserName, user.UserId);
            _session.SignIn(user);

            _roleString = user.Role switch
            {
                1 => "管理员",
                2 => "组长",
                3 => "员工",
                _ => "游客"
            };

            _toast.Success($"欢迎{_roleString}{user.UserName}", null, 2);
            LoginSucceeded?.Invoke();
        }
        [RelayCommand]
        private async Task Register()
        {
            Log.Information("用户注册");
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
            {
                _toast.Error("账号或密码不能为空或空格", null, 1.5);
                return;
            }
            var user = await _repository.LoginAsync(Account);
            if (user?.Account != null)
            {
                _toast.Info("账号已注册，请登录！", null, 2);
                return;
            }
            var saltAndHash = PasswordHasher.HashPassword(Password);
            var newUser = new UserModel()
            {
                UserName = UserName,
                Account = Account,
                Salt = saltAndHash.Salt,
                PasswordHash = saltAndHash.Hash,
                Email = Email
            };
            if (await _repository.InsertUserAsync(newUser) == 0)
            {
                _toast.Error("注册失败，请联系管理员！", null, 3);
                return;
            }
            Log.Information("用户注册成功，用户Id：{UserName}", UserName);
            _toast.Success("注册成功请登录", null, 2);
        }

        [RelayCommand]
        private void UpdatePassword()
        {
            _toast.Warning("暂不支持修改，请联系管理员！", null, 2.5);
        }
    }
}

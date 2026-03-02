using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Security;
using System.Windows;

namespace SimpleMES.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        public event Action? LoginSucceeded;
        private readonly IDataRepository _repository;
        private readonly UserSession _session = UserSession.Current;
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

        public LoginViewModel(IDbService repository)
        {
            _repository = new DataRepository(repository);
        }

        [RelayCommand]
        private async Task Login()
        {
            Log.Information("用户登录");
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("账号或密码不能为空或空格");
                return;
            }
            Log.Information("校验用户登录信息，用户名：{UserName}", UserName);
            var user = await _repository.LoginAsync(Account);
            if (user == null)
            {
                MessageBox.Show("用户不存在，请注册");
                Log.Information("用户[{UserName}]不存在", UserName);
                return;
            }

            if (!PasswordHasher.VerifyPassword(Password, user.PasswordHash, user.Salt))
            {
                MessageBox.Show("用户名或密码错误");
                Log.Information("用户[{UserName}]用户名或密码错误", UserName);
                return;
            }

            if (user.IsActive == 0)
            {
                MessageBox.Show("账号已被禁用，请联系管理员！");
                Log.Information("用户[{UserName}]账号已被禁用", UserName);
                return;
            }
            Log.Information("用户[{UserName}]登录成功，用户Id：{UserId}", UserName, user.UserId);
            _session.SignIn(user);

            _roleString = user.Role switch
            {
                1 => "管理员",
                2 => "组长",
                3 => "员工",
                _ => "游客"
            };

            MessageBox.Show($"欢迎{_roleString}{user.UserName}");
            LoginSucceeded?.Invoke();
        }
        [RelayCommand]
        private async Task Register()
        {
            Log.Information("用户注册");
            if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("账号或密码不能为空或空格");
                return;
            }
            var user = await _repository.LoginAsync(Account);
            if (user?.Account != null)
            {
                MessageBox.Show("账号已注册，请登录！");
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
                MessageBox.Show("注册失败，请联系管理员！");
                return;
            }
            Log.Information("用户注册成功，用户Id：{UserName}", UserName);
            MessageBox.Show("注册成功请登录");
        }

        [RelayCommand]
        private void UpdatePassword()
        {
            MessageBox.Show("暂不支持修改，请联系管理员！");
        }
    }
}

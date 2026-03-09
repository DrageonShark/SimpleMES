using SimpleMES;
using SimpleMES.Core;
using SimpleMES.Helpers.Log;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Observer;
using SimpleMES.Services.Toast;
using SimpleMES.ViewModels;
using SimpleMES.Views;
using System.Windows;

namespace MESDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 保持服务的引用，防止被回收
        private DeviceCommunicationService _deviceCommunication;
        protected override void OnStartup(StartupEventArgs e)
        {
            Logging.Initialize();
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            //1.创建各种服务
            var dbService = new SqlDbService();
            var repo = new DataRepository(dbService);
            var clientFactory = new DeviceClientFactory();
            var strategyResolver = new DevicePollingStrategyResolver(new IDevicePollingStrategy[]
            {
                new DefaultPollingStrategy()
            });
            var configNotifier = new DeviceConfigNotifier();
            var toast = new ToastService();
            //2.创建并启动通信服务 (MES 的心脏)
            _deviceCommunication = new DeviceCommunicationService(repo, clientFactory, strategyResolver, configNotifier);
            _deviceCommunication.StartAsync();
            //3.登录验证
            var loginVm = new LoginViewModel(dbService, toast);
            var loginWindow = new LoginWindow(loginVm);
            var loginOk = loginWindow.ShowDialog();
            if (loginOk != true)
            {
                Shutdown();
                //默认 ShutdownMode 为 OnLastWindowClose，当登录窗口关闭后应用被自动关停，随后主窗口还没机会保持进程存活。
                //需要需要设置窗口关闭逻辑
                return;
            }
            //4.创建主界面 ViewModel
            var monitorVM = new MonitorViewModel(_deviceCommunication, repo, toast, configNotifier);// 注入 Service
            var orderVM = new OrderViewModel(dbService, toast);
            var reportVM = new ReportViewModel(dbService, Dispatcher, _deviceCommunication);

            var mainViewModel = new MainViewModel(monitorVM, orderVM, reportVM);     // 注入 MonitorVM
            // 5. 创建主窗口，并赋值 DataContext
            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainViewModel;
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 程序退出时停止通信
            _deviceCommunication?.Stop();
            Logging.Shutdown();
            base.OnExit(e);
        }
    }

}

using System.Configuration;
using System.Data;
using System.Windows;
using SimpleMES;
using SimpleMES.Core;
using SimpleMES.Services.DAL;
using SimpleMES.ViewModels;

namespace MESDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 保持服务的引用，防止被回收
        private DeviceCommunicationService _deviceCommunication;
        private IDbService _db = new SqlDbService();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //1.创建数据库服务
            var dbService = new SqlDbService();
            var repo = new DataRepository(dbService);
            //2.创建并启动通信服务 (MES 的心脏)
            _deviceCommunication = new DeviceCommunicationService(repo);
            _deviceCommunication.Start();
            //3.创建主界面 ViewModel
            var monitorVM = new MonitorViewModel(_deviceCommunication);// 注入 Service
            var orderVM = new OrderViewModel(_db);
            var reportVM = new ReportViewModel(_db, _deviceCommunication);
            var mainViewModel = new MainViewModel(monitorVM, orderVM, reportVM);     // 注入 MonitorVM
            // 4. 创建主窗口，并赋值 DataContext
            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainViewModel;

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 程序退出时停止通信
            _deviceCommunication?.Stop();
            base.OnExit(e);
        }
    }

}

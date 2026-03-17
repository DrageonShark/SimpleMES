using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Security;
using SimpleMES.Services.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace SimpleMES.ViewModels
{
    public partial class OrderViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly UserSession _session = UserSession.Current;

        // 表格绑定的数据源
        public ObservableCollection<OrderModel> Orders { get; set; } = new ObservableCollection<OrderModel>();

        public bool CanCreateOrderPermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.CreateOrder);
        public bool CanExecuteOrderPermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.ExecuteOrder);
        public bool CanPauseOrderPermission => PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.PauseOrder);


        private IToastService _toast;
        // === 新增订单的表单字段 ===
        [ObservableProperty] private string _newOrderNo = DateTime.Now.ToString("yyyyMMddHHmmss");
        [ObservableProperty] private string _newProductCode;
        [ObservableProperty] private int _newPlanQty = 100;

        //核心业务数据
        //下拉框用的产品列表
        public ObservableCollection<ProductModel> Products { get; } = new ObservableCollection<ProductModel>();
        [ObservableProperty] private ProductModel _productOrder;
        public OrderViewModel(IToastService toast)
        {
            _toast = toast;
            _repository = new DataRepository(new SqlDbService());
            _ = LoadOrders();
            RefreshOrderCommandStates();
            _session.PropertyChanged += OnSessionPropertyChanged;
        }

        public OrderViewModel(IDbService dbService, IToastService toast)
        {
            _toast = toast;
            _repository = new DataRepository(dbService);
            _ = LoadOrders();
            RefreshOrderCommandStates();
            _session.PropertyChanged += OnSessionPropertyChanged;
        }

        /// <summary>
        /// 加载订单
        /// </summary>
        [RelayCommand]
        private async Task LoadOrders()
        {
            try
            {
                var list = await _repository.GetAllOrdersAsync().ConfigureAwait(false);
                var products = await _repository.GetAllProductsAsync().ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Orders.Clear();
                    foreach (var order in list.ToList())
                    {
                        Orders.Add(order);
                    }
                    Products.Clear();
                    foreach (var product in products.ToList())
                    {
                        Products.Add(product);
                    }
                });
                RefreshOrderCommandStates();
            }
            catch (Exception ex)
            {
                Log.Error("订单列表刷新失败{ex.Message}", ex.Message);
                _toast.Error($"加载失败: {ex.Message}", null, 3);
            }
        }
        [RelayCommand(CanExecute = nameof(CanCreateOrder))]
        private async Task CreateOrder()
        {
            //1.简单的校验
            if (string.IsNullOrWhiteSpace(NewOrderNo) || string.IsNullOrWhiteSpace(NewProductCode))
            {
                _toast.Warning("请填写完整订单信息！", null, 2);
                return;
            }

            try
            {
                // 2. 构建模型
                var order = new OrderModel()
                {
                    OrderNo = NewOrderNo,
                    ProductCode = NewProductCode,
                    PlanQty = NewPlanQty,
                    OrderStatus = "Pending",
                    CreateTime = DateTime.Now
                };
                // 3. 写入数据库
                await _repository.CreateOrderAsync(order).ConfigureAwait(false);
                // 4. 刷新列表 & 清空输入框
                await LoadOrders().ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NewOrderNo = DateTime.Now.ToString("yyyyMMddHHmmss");
                    _toast.Success("订单创建成功！", null, 2);
                });
            }
            catch (Exception ex)
            {
                _toast.Error($"创建失败: {ex.Message}", null, 2);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartOrder))]
        private async Task StartOrder(OrderModel? order)
        {
            if (order is null) return;

            var updating = new OrderModel
            {
                OrderNo = order.OrderNo,
                ProductCode = order.ProductCode,
                PlanQty = order.PlanQty,
                CompletedQty = order.CompletedQty,
                OrderStatus = "Producing",
                StartTime = order.StartTime ?? DateTime.Now,
                EndTime = order.EndTime,
                LastOperationTime = DateTime.Now,
                CreateTime = order.CreateTime
            };

            await _repository.UpdateOrderAsync(updating);
            await LoadOrders();
            _toast.Success($"订单 {updating.OrderNo} 已执行", null, 2);
        }

        [RelayCommand(CanExecute = nameof(CanPauseOrder))]
        private async Task PauseOrder(OrderModel? order)
        {
            if (order is null) return;

            var updating = new OrderModel
            {
                OrderNo = order.OrderNo,
                ProductCode = order.ProductCode,
                PlanQty = order.PlanQty,
                CompletedQty = order.CompletedQty,
                OrderStatus = "Paused",
                StartTime = order.StartTime,
                EndTime = order.EndTime,
                LastOperationTime = DateTime.Now,
                CreateTime = order.CreateTime
            };

            await _repository.UpdateOrderAsync(updating);
            await LoadOrders();
            _toast.Info($"订单 {updating.OrderNo} 已暂停", null, 2);
        }


        private bool CanCreateOrder() => CanCreateOrderPermission;
        private bool CanStartOrder(OrderModel? order)
        {
            if (!CanExecuteOrderPermission || order is null) return false;
            var status = order.OrderStatus?.Trim() ?? string.Empty;
            return status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                   || status.Equals("Paused", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanPauseOrder(OrderModel? order)
        {
            if (!CanPauseOrderPermission || order is null) return false;
            var status = order.OrderStatus?.Trim() ?? string.Empty;
            return status.Equals("Producing", StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshOrderCommandStates()
        {
            CreateOrderCommand.NotifyCanExecuteChanged();
            StartOrderCommand.NotifyCanExecuteChanged();
            PauseOrderCommand.NotifyCanExecuteChanged();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            OnPropertyChanged(nameof(CanCreateOrderPermission));
            OnPropertyChanged(nameof(CanExecuteOrderPermission));
            OnPropertyChanged(nameof(CanPauseOrderPermission));

            RefreshOrderCommandStates();
        }

    }
}

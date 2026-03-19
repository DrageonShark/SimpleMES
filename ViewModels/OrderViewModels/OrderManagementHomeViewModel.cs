using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Dialog;
using SimpleMES.Services.Orders;
using SimpleMES.Services.Security;
using SimpleMES.Services.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleMES.ViewModels.OrderViewModels
{
    public partial class OrderManagementHomeViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private readonly UserSession _session = UserSession.Current;
        private readonly IOrderDialogService _orderDialogService;

        public ObservableCollection<ProductModel> Products { get; } = new();
        public ObservableCollection<OrderModel> RecentOrders { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateOrderCommand))]
        private string _newOrderNo = DateTime.Now.ToString("yyyyMMddHms");

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateOrderCommand))]
        private ProductModel? _selectedProduct;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateOrderCommand))]
        private int _newPlanQty = 100;

        public bool CanCreateOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.CreateOrder);

        public OrderManagementHomeViewModel(
            IDataRepository repository,
            IToastService toast,
            IOrderDialogService orderDialogService)
        {
            _repository = repository;
            _toast = toast;
            _orderDialogService = orderDialogService;

            PageTitle = "订单维护";
            _session.PropertyChanged += OnSessionPropertyChanged;
            _ = LoadInitialData();
        }

        [RelayCommand]
        private async Task LoadInitialData()
        {
            try
            {
                var products = (await _repository.GetAllProductsAsync()).ToList();
                var orders = (await _repository.GetAllOrdersAsync()).Take(20).ToList();

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                RecentOrders.Clear();
                foreach (var order in orders)
                {
                    RecentOrders.Add(order);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "订单维护页加载失败");
                _toast.Error($"加载失败: {ex.Message}", null, 2.5);
            }
        }

        [RelayCommand(CanExecute = nameof(CanCreateOrder))]
        private async Task CreateOrder()
        {
            if (SelectedProduct is null)
            {
                _toast.Warning("请选择产品", null, 2);
                return;
            }

            try
            {
                var order = new OrderModel
                {
                    OrderNo = NewOrderNo.Trim(),
                    ProductCode = SelectedProduct.ProductCode,
                    PlanQty = NewPlanQty,
                    CompletedQty = 0,
                    OrderStatus = OrderStatus.Pending.ToCode(),
                    CreateTime = DateTime.Now,
                    StartTime = null,
                    EndTime = null,
                    LastOperationTime = DateTime.Now
                };

                await _repository.CreateOrderAsync(order);
                _toast.Success($"订单 {order.OrderNo} 创建成功", null, 2);

                ResetForm();
                await LoadInitialData();
            }
            catch (Exception ex)
            {
                _toast.Error($"创建失败: {ex.Message}", null, 2.5);
            }
        }

        [RelayCommand]
        private async Task OpenOrderDetail(OrderModel? order)
        {
            if (order is null) return;

            var changed = await _orderDialogService.ShowOrderDetailDialogAsync(order);
            if (changed)
            {
                await LoadInitialData();
            }
        }


        private bool CanCreateOrder()
        {
            return CanCreateOrderPermission
                   && !string.IsNullOrWhiteSpace(NewOrderNo)
                   && SelectedProduct is not null
                   && NewPlanQty > 0;
        }

        private void ResetForm()
        {
            NewOrderNo = DateTime.Now.ToString("yyyyMMddHHmmss");
            SelectedProduct = null;
            NewPlanQty = 100;
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            OnPropertyChanged(nameof(CanCreateOrderPermission));
            CreateOrderCommand.NotifyCanExecuteChanged();
        }
    }
}

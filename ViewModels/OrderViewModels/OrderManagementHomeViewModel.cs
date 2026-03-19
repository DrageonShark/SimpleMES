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
        private bool _suspendAutoReload;

        public ObservableCollection<ProductModel> Products { get; } = new();
        public ObservableCollection<OrderModel> RecentOrders { get; } = new();
        public ObservableCollection<KeyValuePair<string, string>> StatusFilterOptions { get; } = new();
        public ObservableCollection<int> RecentOrderLimitOptions { get; } = new() { 10, 20, 50, 100 };

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateOrderCommand))]
        private string _newOrderNo = DateTime.Now.ToString("yyyyMMddHHmmss");

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateOrderCommand))]
        private ProductModel? _selectedProduct;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateOrderCommand))]
        private int _newPlanQty = 100;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = string.Empty;

        [ObservableProperty]
        private int _recentOrderLimit = 20;

        public bool CanCreateOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.CreateOrder);

        public bool HasRecentOrders => RecentOrders.Count > 0;
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchKeyword) || !string.IsNullOrWhiteSpace(SelectedStatusFilter);

        public string OrdersSummary =>
            HasActiveFilters ? $"筛选结果 {RecentOrders.Count} 条" : $"最近订单 {RecentOrders.Count} 条";

        public string EmptyResultTitle =>
            HasActiveFilters ? "没有匹配的订单" : "还没有订单";

        public string EmptyResultDescription =>
            HasActiveFilters
                ? "调整搜索词、状态筛选或显示条数后再试。"
                : "先在左侧创建订单，右侧会展示最近订单。";

        public OrderManagementHomeViewModel(
            IDataRepository repository,
            IToastService toast,
            IOrderDialogService orderDialogService)
        {
            _repository = repository;
            _toast = toast;
            _orderDialogService = orderDialogService;

            StatusFilterOptions.Add(new(string.Empty, "全部状态"));
            StatusFilterOptions.Add(new(OrderStatus.Pending.ToCode(), "待产"));
            StatusFilterOptions.Add(new(OrderStatus.Producing.ToCode(), "生产中"));
            StatusFilterOptions.Add(new(OrderStatus.Paused.ToCode(), "已暂停"));
            StatusFilterOptions.Add(new(OrderStatus.Completed.ToCode(), "已完工"));
            StatusFilterOptions.Add(new(OrderStatus.Scrapped.ToCode(), "已废弃"));

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

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                await LoadRecentOrdersAsync();
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
                await LoadRecentOrdersAsync();
            }
            catch (Exception ex)
            {
                _toast.Error($"创建失败: {ex.Message}", null, 2.5);
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            _suspendAutoReload = true;
            SearchKeyword = string.Empty;
            SelectedStatusFilter = string.Empty;
            RecentOrderLimit = 20;
            _suspendAutoReload = false;

            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(EmptyResultTitle));
            OnPropertyChanged(nameof(EmptyResultDescription));

            await LoadRecentOrdersAsync();
        }

        [RelayCommand]
        private async Task OpenOrderDetail(OrderModel? order)
        {
            if (order is null) return;

            var changed = await _orderDialogService.ShowOrderDetailDialogAsync(order);
            if (changed)
            {
                await LoadRecentOrdersAsync();
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

        partial void OnSearchKeywordChanged(string value)
        {
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(EmptyResultTitle));
            OnPropertyChanged(nameof(EmptyResultDescription));

            if (_suspendAutoReload)
            {
                return;
            }

            _ = LoadRecentOrdersAsync();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(EmptyResultTitle));
            OnPropertyChanged(nameof(EmptyResultDescription));

            if (_suspendAutoReload)
            {
                return;
            }

            _ = LoadRecentOrdersAsync();
        }

        partial void OnRecentOrderLimitChanged(int value)
        {
            if (_suspendAutoReload)
            {
                return;
            }

            _ = LoadRecentOrdersAsync();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            OnPropertyChanged(nameof(CanCreateOrderPermission));
            CreateOrderCommand.NotifyCanExecuteChanged();
        }

        private async Task LoadRecentOrdersAsync()
        {
            try
            {
                var limit = RecentOrderLimit > 0 ? RecentOrderLimit : 20;
                var orders = (await _repository.GetOrdersAsync(SearchKeyword, SelectedStatusFilter, limit)).ToList();

                RecentOrders.Clear();
                foreach (var order in orders)
                {
                    RecentOrders.Add(order);
                }

                OnPropertyChanged(nameof(HasRecentOrders));
                OnPropertyChanged(nameof(OrdersSummary));
                OnPropertyChanged(nameof(EmptyResultTitle));
                OnPropertyChanged(nameof(EmptyResultDescription));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "订单维护页订单查询失败");
                _toast.Error($"订单查询失败: {ex.Message}", null, 2.5);
            }
        }
    }
}

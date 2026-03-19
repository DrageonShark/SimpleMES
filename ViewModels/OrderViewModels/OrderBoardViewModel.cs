using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Orders;
using SimpleMES.Services.Toast;
using System.Collections.ObjectModel;
using System.Windows;

namespace SimpleMES.ViewModels.OrderViewModels
{
    public partial class OrderBoardViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private bool _suspendAutoReload;

        public ObservableCollection<OrderModel> Orders { get; } = new();
        public ObservableCollection<KeyValuePair<string, string>> StatusFilterOptions { get; } = new();
        public ObservableCollection<int> OrderLimitOptions { get; } = new() { 20, 50, 100, 200 };

        [ObservableProperty]
        private OrderModel? _selectedOrder;

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = string.Empty;

        [ObservableProperty]
        private int _orderLimit = 50;

        [ObservableProperty]
        private int _pendingCount;

        [ObservableProperty]
        private int _producingCount;

        [ObservableProperty]
        private int _pausedCount;

        [ObservableProperty]
        private int _completedCount;

        public bool HasOrders => Orders.Count > 0;

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchKeyword) || !string.IsNullOrWhiteSpace(SelectedStatusFilter);

        public string OrdersSummary =>
            HasActiveFilters ? $"筛选结果 {Orders.Count} 条" : $"看板当前展示 {Orders.Count} 条订单";

        public string EmptyStateTitle =>
            HasActiveFilters ? "没有匹配的订单" : "暂无订单数据";

        public string EmptyStateDescription =>
            HasActiveFilters
                ? "调整搜索词、状态筛选或显示条数后再试。"
                : "订单创建后会在这里按状态展示。";

        public OrderBoardViewModel(IDataRepository repository, IToastService toast)
        {
            _toast = toast;
            _repository = repository;
            PageTitle = "订单看板";

            StatusFilterOptions.Add(new(string.Empty, "全部状态"));
            StatusFilterOptions.Add(new(OrderStatus.Pending.ToCode(), "待产"));
            StatusFilterOptions.Add(new(OrderStatus.Producing.ToCode(), "生产中"));
            StatusFilterOptions.Add(new(OrderStatus.Paused.ToCode(), "已暂停"));
            StatusFilterOptions.Add(new(OrderStatus.Completed.ToCode(), "已完工"));

            _ = LoadOrders();
        }

        [RelayCommand]
        private async Task LoadOrders()
        {
            try
            {
                var filteredOrdersTask = _repository.GetOrdersAsync(SearchKeyword, SelectedStatusFilter, OrderLimit);
                var allOrdersTask = _repository.GetOrdersAsync();
                await Task.WhenAll(filteredOrdersTask, allOrdersTask).ConfigureAwait(false);

                var filteredOrders = filteredOrdersTask.Result.ToList();
                var allOrders = allOrdersTask.Result.ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Orders.Clear();
                    foreach (var order in filteredOrders)
                    {
                        Orders.Add(order);
                    }

                    PendingCount = allOrders.Count(order => order.GetState() == OrderStatus.Pending);
                    ProducingCount = allOrders.Count(order => order.GetState() == OrderStatus.Producing);
                    PausedCount = allOrders.Count(order => order.GetState() == OrderStatus.Paused);
                    CompletedCount = allOrders.Count(order => order.GetState() == OrderStatus.Completed);

                    OnPropertyChanged(nameof(HasOrders));
                    OnPropertyChanged(nameof(HasActiveFilters));
                    OnPropertyChanged(nameof(OrdersSummary));
                    OnPropertyChanged(nameof(EmptyStateTitle));
                    OnPropertyChanged(nameof(EmptyStateDescription));
                });
            }
            catch (Exception ex)
            {
                _toast.Error($"订单加载失败: {ex.Message}", null, 2.5);
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            _suspendAutoReload = true;
            SearchKeyword = string.Empty;
            SelectedStatusFilter = string.Empty;
            OrderLimit = 50;
            _suspendAutoReload = false;

            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(OrdersSummary));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateDescription));

            await LoadOrders();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(OrdersSummary));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateDescription));

            if (_suspendAutoReload)
            {
                return;
            }

            _ = LoadOrders();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(OrdersSummary));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateDescription));

            if (_suspendAutoReload)
            {
                return;
            }

            _ = LoadOrders();
        }

        partial void OnOrderLimitChanged(int value)
        {
            if (_suspendAutoReload)
            {
                return;
            }

            _ = LoadOrders();
        }
    }
}

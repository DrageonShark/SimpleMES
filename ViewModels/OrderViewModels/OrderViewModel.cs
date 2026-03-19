using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Orders;
using SimpleMES.Services.Security;
using SimpleMES.Services.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleMES.ViewModels.OrderViewModels
{
    public partial class OrderViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private readonly IOrderWorkflowService _workflowService;
        private readonly UserSession _session = UserSession.Current;

        public ObservableCollection<OrderModel> Orders { get; } = new();

        [ObservableProperty] private OrderModel? _selectedOrder;

        public bool CanExecuteOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.ExecuteOrder);

        public bool CanPauseOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.PauseOrder);

        public bool CanCompleteOrderPermission => CanExecuteOrderPermission;
        public bool HasSelectedOrder => SelectedOrder is not null;
        public bool HasOrders => Orders.Count > 0;

        public string EmptyStateTitle =>
            HasOrders ? "请选择要调度的订单" : "暂无可调度订单";

        public string EmptyStateDescription =>
            HasOrders
                ? "从左侧订单列表中选择一条工单，右侧将显示调度详情和可执行状态流转。"
                : "当前还没有可调度订单，请先到订单维护页创建工单后再进行调度。";

        public bool CanDispatchSelectedOrder =>
            CanRunAction(UserPermission.ExecuteOrder, OrderWorkflowAction.Start);

        public bool CanPauseSelectedOrderByRule =>
            CanRunAction(UserPermission.PauseOrder, OrderWorkflowAction.Pause);

        public bool CanCompleteSelectedOrderByRule =>
            CanRunAction(UserPermission.ExecuteOrder, OrderWorkflowAction.Complete);

        private bool HasPermission(UserPermission permission) =>
            PermissionMatrix.HasPermission(_session.CurrentUser, permission);

        private bool CanRunAction(UserPermission permission, OrderWorkflowAction action) =>
            HasPermission(permission) && _workflowService.CanTransit(SelectedOrder, action);

        public bool ShowStartButton => HasPermission(UserPermission.ExecuteOrder);
        public bool ShowPauseButton => HasPermission(UserPermission.PauseOrder);
        public bool ShowCompleteButton => HasPermission(UserPermission.ExecuteOrder);

        public OrderViewModel(
            IDataRepository repository,
            IToastService toast,
            IOrderWorkflowService workflowService)
        {
            _repository = repository;
            _toast = toast;
            _workflowService = workflowService;

            PageTitle = "订单调度";
            _session.PropertyChanged += OnSessionPropertyChanged;
            _ = LoadOrders();
        }

        partial void OnSelectedOrderChanged(OrderModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedOrder));
            RefreshCommandStates();
        }

        [RelayCommand]
        private async Task LoadOrders()
        {
            var selectedOrderNo = SelectedOrder?.OrderNo;

            try
            {
                var list = (await _repository.GetAllOrdersAsync()).ToList();

                Orders.Clear();
                foreach (var order in list)
                {
                    Orders.Add(order);
                }

                SelectedOrder = Orders.FirstOrDefault(x => x.OrderNo == selectedOrderNo);
                OnPropertyChanged(nameof(HasOrders));
                OnPropertyChanged(nameof(EmptyStateTitle));
                OnPropertyChanged(nameof(EmptyStateDescription));
                RefreshCommandStates();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "订单列表刷新失败");
                _toast.Error($"加载失败: {ex.Message}", null, 3);
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartSelectedOrder))]
        private Task StartSelectedOrder() =>
            ApplyWorkflowAsync(OrderWorkflowAction.Start, "已执行");

        [RelayCommand(CanExecute = nameof(CanPauseSelectedOrder))]
        private Task PauseSelectedOrder() =>
            ApplyWorkflowAsync(OrderWorkflowAction.Pause, "已暂停");

        [RelayCommand(CanExecute = nameof(CanCompleteSelectedOrder))]
        private Task CompleteSelectedOrder() =>
            ApplyWorkflowAsync(OrderWorkflowAction.Complete, "已完工");

        private bool CanStartSelectedOrder() => CanDispatchSelectedOrder;
        private bool CanPauseSelectedOrder() => CanPauseSelectedOrderByRule;
        private bool CanCompleteSelectedOrder() => CanCompleteSelectedOrderByRule;

        private async Task ApplyWorkflowAsync(OrderWorkflowAction action, string successText)
        {
            if (SelectedOrder is null) return;

            try
            {
                var updated = _workflowService.Transit(SelectedOrder, action);
                await _repository.UpdateOrderAsync(updated);
                await LoadOrders();
                _toast.Success($"订单 {updated.OrderNo} {successText}", null, 2);
            }
            catch (Exception ex)
            {
                _toast.Error($"操作失败: {ex.Message}", null, 2.5);
            }
        }

        private void RefreshCommandStates()
        {
            StartSelectedOrderCommand.NotifyCanExecuteChanged();
            PauseSelectedOrderCommand.NotifyCanExecuteChanged();
            CompleteSelectedOrderCommand.NotifyCanExecuteChanged();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            OnPropertyChanged(nameof(ShowStartButton));
            OnPropertyChanged(nameof(ShowPauseButton));
            OnPropertyChanged(nameof(ShowCompleteButton));
            OnPropertyChanged(nameof(CanDispatchSelectedOrder));
            OnPropertyChanged(nameof(CanPauseSelectedOrderByRule));
            OnPropertyChanged(nameof(CanCompleteSelectedOrderByRule));
            RefreshCommandStates();
        }
    }
}

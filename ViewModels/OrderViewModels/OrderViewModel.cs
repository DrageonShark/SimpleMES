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
    /// <summary>
    /// 订单调度页面的视图模型。
    /// 负责订单列表加载、选中订单状态维护、权限控制以及工单状态流转（开始/暂停/完工）。
    /// </summary>
    public partial class OrderViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private readonly IOrderWorkflowService _workflowService;
        private readonly UserSession _session = UserSession.Current;

        /// <summary>
        /// 当前可调度订单集合（绑定到左侧订单列表）。
        /// </summary>
        public ObservableCollection<OrderModel> Orders { get; } = new();

        /// <summary>
        /// 当前选中的订单。
        /// 由 <see cref="ObservablePropertyAttribute"/> 生成 <c>SelectedOrder</c> 属性及变更通知。
        /// </summary>
        [ObservableProperty] private OrderModel? _selectedOrder;

        /// <summary>
        /// 当前用户是否具备“执行订单”权限。
        /// </summary>
        public bool CanExecuteOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.ExecuteOrder);

        /// <summary>
        /// 当前用户是否具备“暂停订单”权限。
        /// </summary>
        public bool CanPauseOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.PauseOrder);

        /// <summary>
        /// 当前用户是否具备“完工订单”权限（复用执行权限）。
        /// </summary>
        public bool CanCompleteOrderPermission => CanExecuteOrderPermission;

        /// <summary>
        /// 是否存在已选中订单。
        /// </summary>
        public bool HasSelectedOrder => SelectedOrder is not null;

        /// <summary>
        /// 当前订单集合是否为空。
        /// </summary>
        public bool HasOrders => Orders.Count > 0;

        /// <summary>
        /// 空状态区域标题。
        /// </summary>
        public string EmptyStateTitle =>
            HasOrders ? "请选择要调度的订单" : "暂无可调度订单";

        /// <summary>
        /// 空状态区域描述文案。
        /// </summary>
        public string EmptyStateDescription =>
            HasOrders
                ? "从左侧订单列表中选择一条工单，右侧将显示调度详情和可执行状态流转。"
                : "当前还没有可调度订单，请先到订单维护页创建工单后再进行调度。";

        /// <summary>
        /// 当前选中订单是否可执行“开始”动作（权限 + 状态机规则）。
        /// </summary>
        public bool CanDispatchSelectedOrder =>
            CanRunAction(UserPermission.ExecuteOrder, OrderWorkflowAction.Start);

        /// <summary>
        /// 当前选中订单是否可执行“暂停”动作（权限 + 状态机规则）。
        /// </summary>
        public bool CanPauseSelectedOrderByRule =>
            CanRunAction(UserPermission.PauseOrder, OrderWorkflowAction.Pause);

        /// <summary>
        /// 当前选中订单是否可执行“完工”动作（权限 + 状态机规则）。
        /// </summary>
        public bool CanCompleteSelectedOrderByRule =>
            CanRunAction(UserPermission.ExecuteOrder, OrderWorkflowAction.Complete);

        /// <summary>
        /// 判断当前用户是否拥有指定权限。
        /// </summary>
        /// <param name="permission">要校验的权限。</param>
        /// <returns>有权限返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        private bool HasPermission(UserPermission permission) =>
            PermissionMatrix.HasPermission(_session.CurrentUser, permission);

        /// <summary>
        /// 判断当前选中订单是否允许执行指定状态流转动作。
        /// </summary>
        /// <param name="permission">执行动作所需权限。</param>
        /// <param name="action">目标工作流动作。</param>
        /// <returns>同时满足权限与状态机规则时返回 <see langword="true"/>。</returns>
        private bool CanRunAction(UserPermission permission, OrderWorkflowAction action) =>
            HasPermission(permission) && _workflowService.CanTransit(SelectedOrder, action);

        /// <summary>
        /// 是否显示“开始”按钮（仅权限控制显示）。
        /// </summary>
        public bool ShowStartButton => HasPermission(UserPermission.ExecuteOrder);

        /// <summary>
        /// 是否显示“暂停”按钮（仅权限控制显示）。
        /// </summary>
        public bool ShowPauseButton => HasPermission(UserPermission.PauseOrder);

        /// <summary>
        /// 是否显示“完工”按钮（仅权限控制显示）。
        /// </summary>
        public bool ShowCompleteButton => HasPermission(UserPermission.ExecuteOrder);

        /// <summary>
        /// 初始化 <see cref="OrderViewModel"/> 实例。
        /// </summary>
        /// <param name="repository">订单数据访问仓储。</param>
        /// <param name="toast">消息提示服务。</param>
        /// <param name="workflowService">订单状态流转服务。</param>
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

        /// <summary>
        /// 选中订单变更后的回调。
        /// 用于刷新依赖选中项的属性与命令可执行状态。
        /// </summary>
        /// <param name="value">最新选中的订单。</param>
        partial void OnSelectedOrderChanged(OrderModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedOrder));
            RefreshCommandStates();
        }

        /// <summary>
        /// 加载订单列表并尽可能保留原选中项。
        /// </summary>
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

        /// <summary>
        /// 执行“开始订单”命令。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartSelectedOrder))]
        private Task StartSelectedOrder() =>
            ApplyWorkflowAsync(OrderWorkflowAction.Start, "已执行");

        /// <summary>
        /// 执行“暂停订单”命令。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPauseSelectedOrder))]
        private Task PauseSelectedOrder() =>
            ApplyWorkflowAsync(OrderWorkflowAction.Pause, "已暂停");

        /// <summary>
        /// 执行“完工订单”命令。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCompleteSelectedOrder))]
        private Task CompleteSelectedOrder() =>
            ApplyWorkflowAsync(OrderWorkflowAction.Complete, "已完工");

        /// <summary>
        /// “开始订单”命令可执行条件。
        /// </summary>
        private bool CanStartSelectedOrder() => CanDispatchSelectedOrder;

        /// <summary>
        /// “暂停订单”命令可执行条件。
        /// </summary>
        private bool CanPauseSelectedOrder() => CanPauseSelectedOrderByRule;

        /// <summary>
        /// “完工订单”命令可执行条件。
        /// </summary>
        private bool CanCompleteSelectedOrder() => CanCompleteSelectedOrderByRule;

        /// <summary>
        /// 应用订单工作流动作，落库后刷新列表并提示结果。
        /// </summary>
        /// <param name="action">要执行的工作流动作。</param>
        /// <param name="successText">成功提示文案后缀。</param>
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

        /// <summary>
        /// 通知开始/暂停/完工命令重新计算可执行状态。
        /// </summary>
        private void RefreshCommandStates()
        {
            StartSelectedOrderCommand.NotifyCanExecuteChanged();
            PauseSelectedOrderCommand.NotifyCanExecuteChanged();
            CompleteSelectedOrderCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 用户会话变更处理。
        /// 当当前用户变化时，刷新按钮显示与命令可执行状态。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="e">属性变更参数。</param>
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

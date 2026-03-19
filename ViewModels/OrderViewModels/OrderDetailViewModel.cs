using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Orders;
using SimpleMES.Services.Security;
using SimpleMES.Services.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleMES.ViewModels.OrderViewModels
{
    public partial class OrderDetailViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;
        private readonly UserSession _session = UserSession.Current;

        public ObservableCollection<ProductModel> Products { get; } = new();

        [ObservableProperty] private OrderModel _order = null!;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _editProductCode = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _editPlanQtyText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProductValidationMessage))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _productValidationMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPlanQtyValidationMessage))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _planQtyValidationMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowEditRestriction))]
        private string _editRestrictionMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDeleteRestriction))]
        private string _deleteRestrictionMessage = string.Empty;

        public bool HasProductValidationMessage => !string.IsNullOrWhiteSpace(ProductValidationMessage);
        public bool HasPlanQtyValidationMessage => !string.IsNullOrWhiteSpace(PlanQtyValidationMessage);
        public bool ShowEditRestriction => !string.IsNullOrWhiteSpace(EditRestrictionMessage);
        public bool ShowDeleteRestriction => !string.IsNullOrWhiteSpace(DeleteRestrictionMessage);

        public bool HasEditOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.EditOrder);

        public bool HasDeleteOrderPermission =>
            PermissionMatrix.HasPermission(_session.CurrentUser, UserPermission.DeleteOrder);

        public bool CanEditOrder => HasEditOrderPermission && IsEditableByState();
        public bool CanDeleteOrder => HasDeleteOrderPermission && IsDeletableByState();

        public OrderDetailViewModel(OrderModel order, IDataRepository repository, IToastService toast)
        {
            _repository = repository;
            _toast = toast;
            _session.PropertyChanged += OnSessionPropertyChanged;

            Order = Clone(order);
            EditProductCode = order.ProductCode;
            EditPlanQtyText = order.PlanQty.ToString();
            PageTitle = $"订单详情 - {order.OrderNo}";

            RefreshRuleState();
            ValidateInputs();

            _ = LoadProducts();
        }

        [RelayCommand]
        private async Task LoadProducts()
        {
            var products = await _repository.GetAllProductsAsync();
            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            if (!CanEditOrder)
            {
                _toast.Warning(EditRestrictionMessage, null, 2.5);
                return;
            }

            ValidateInputs();
            if (HasProductValidationMessage || HasPlanQtyValidationMessage)
            {
                _toast.Warning(GetFirstValidationMessage(), null, 2.5);
                return;
            }

            var updated = new OrderModel
            {
                OrderNo = Order.OrderNo,
                ProductCode = EditProductCode.Trim(),
                PlanQty = int.Parse(EditPlanQtyText.Trim()),
                CompletedQty = Order.CompletedQty,
                OrderStatus = Order.OrderStatus,
                StartTime = Order.StartTime,
                EndTime = Order.EndTime,
                CreateTime = Order.CreateTime,
                LastOperationTime = DateTime.Now
            };

            await _repository.UpdateOrderAsync(updated);
            _toast.Success($"订单 {updated.OrderNo} 已更新", null, 2);
            Close(true);
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task Delete()
        {
            if (!CanDeleteOrder)
            {
                _toast.Warning(DeleteRestrictionMessage, null, 2.5);
                return;
            }

            var confirmed = Confirm(
                "删除订单",
                $"确认删除订单 {Order.OrderNo} 吗？该操作不可撤销。");
            if (!confirmed) return;

            await _repository.DeleteOrderAsync(Order.OrderNo);
            _toast.Warning($"订单 {Order.OrderNo} 已删除", null, 2);
            Close(true);
        }

        [RelayCommand]
        private void CloseWindow()
        {
            Close(false);
        }

        private bool CanSave() =>
            CanEditOrder &&
            !HasProductValidationMessage &&
            !HasPlanQtyValidationMessage;

        private bool CanDelete() => CanDeleteOrder;

        partial void OnEditProductCodeChanged(string value)
        {
            ValidateInputs();
        }

        partial void OnEditPlanQtyTextChanged(string value)
        {
            ValidateInputs();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            RefreshRuleState();
            ValidateInputs();

            OnPropertyChanged(nameof(HasEditOrderPermission));
            OnPropertyChanged(nameof(HasDeleteOrderPermission));
            OnPropertyChanged(nameof(CanEditOrder));
            OnPropertyChanged(nameof(CanDeleteOrder));
            SaveCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
        }

        private void RefreshRuleState()
        {
            EditRestrictionMessage = !HasEditOrderPermission
                ? "当前账号没有订单修改权限。"
                : CanEditOrder
                    ? string.Empty
                    : "订单进入生产后不允许再修改产品和计划数量，仅待产且未开工订单可维护。";

            DeleteRestrictionMessage = !HasDeleteOrderPermission
                ? "当前账号没有订单删除权限。"
                : CanDeleteOrder
                    ? string.Empty
                    : "仅待产且未开工的订单允许删除，生产中、暂停中、已完工订单需保留追溯记录。";
        }

        private void ValidateInputs()
        {
            ProductValidationMessage = string.IsNullOrWhiteSpace(EditProductCode)
                ? "请选择产品。"
                : string.Empty;

            if (string.IsNullOrWhiteSpace(EditPlanQtyText))
            {
                PlanQtyValidationMessage = "计划数量不能为空。";
                return;
            }

            if (!int.TryParse(EditPlanQtyText.Trim(), out var planQty))
            {
                PlanQtyValidationMessage = "计划数量必须是整数。";
                return;
            }

            PlanQtyValidationMessage = planQty <= 0
                ? "计划数量必须大于 0。"
                : string.Empty;
        }

        private string GetFirstValidationMessage()
        {
            if (HasProductValidationMessage)
            {
                return ProductValidationMessage;
            }

            if (HasPlanQtyValidationMessage)
            {
                return PlanQtyValidationMessage;
            }

            return "订单信息校验失败。";
        }

        private bool IsEditableByState() =>
            Order.GetState() == OrderStatus.Pending && Order.CompletedQty == 0;

        private bool IsDeletableByState() =>
            Order.GetState() == OrderStatus.Pending && Order.CompletedQty == 0;

        private static OrderModel Clone(OrderModel source)
        {
            return new OrderModel
            {
                OrderNo = source.OrderNo,
                ProductCode = source.ProductCode,
                PlanQty = source.PlanQty,
                CompletedQty = source.CompletedQty,
                OrderStatus = source.OrderStatus,
                StartTime = source.StartTime,
                EndTime = source.EndTime,
                LastOperationTime = source.LastOperationTime,
                CreateTime = source.CreateTime
            };
        }
    }
}

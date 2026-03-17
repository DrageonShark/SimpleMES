using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleMES.ViewModels.OrderViewModels
{
    /// <summary>
    /// 管理订单子视图模型的显示和导航。
    /// </summary>
    public partial class OrderShellViewModel : DialogViewModelBase
    {
        public enum OrderModulePage
        {
            Board,
            Management,
            Dispatch
        }
        private readonly OrderBoardViewModel _boardViewModel;
        private readonly OrderManagementHomeViewModel _managementViewModel;
        private readonly OrderViewModel _dispatchViewModel;

        [ObservableProperty]
        private DialogViewModelBase _currentChild = null!;

        public OrderShellViewModel(
            OrderBoardViewModel boardViewModel,
            OrderManagementHomeViewModel managementViewModel,
            OrderViewModel dispatchViewModel)
        {
            _boardViewModel = boardViewModel;
            _managementViewModel = managementViewModel;
            _dispatchViewModel = dispatchViewModel;

            NavigateTo(OrderModulePage.Board);
        }
        public void NavigateTo(OrderModulePage page)
        {
            switch (page)
            {
                case OrderModulePage.Board:
                    CurrentChild = _boardViewModel;
                    PageTitle = "订单看板";
                    break;
                case OrderModulePage.Management:
                    CurrentChild = _managementViewModel;
                    PageTitle = "订单维护";
                    break;
                case OrderModulePage.Dispatch:
                    CurrentChild = _dispatchViewModel;
                    PageTitle = "订单调度";
                    break;
            }
        }
        [RelayCommand]
        private void ShowBoard() => NavigateTo(OrderModulePage.Board);

        [RelayCommand]
        private void ShowManagement() => NavigateTo(OrderModulePage.Management);

        [RelayCommand]
        private void ShowDispatch() => NavigateTo(OrderModulePage.Dispatch);
    }
}
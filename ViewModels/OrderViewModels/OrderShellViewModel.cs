using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleMES.ViewModels.OrderViewModels
{
    public enum OrderModulePage
    {
        Board,
        Management,
        Dispatch
    }

    public partial class OrderShellViewModel : DialogViewModelBase
    {
        private readonly OrderBoardViewModel _boardViewModel;
        private readonly OrderManagementHomeViewModel _managementViewModel;
        private readonly OrderViewModels.OrderViewModel _dispatchViewModel;

        [ObservableProperty]
        private DialogViewModelBase _currentChild = null!;

        public OrderShellViewModel(
            OrderBoardViewModel boardViewModel,
            OrderManagementHomeViewModel managementViewModel,
            OrderViewModels.OrderViewModel dispatchViewModel)
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
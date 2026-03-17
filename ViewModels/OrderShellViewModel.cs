using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleMES.ViewModels
{
    /// <summary>
    /// 管理订单子视图模型的显示和导航。
    /// </summary>
    public partial class OrderShellViewModel : DialogViewModelBase
    {
        private readonly OrderBoardViewModel _boardViewModel;

        [ObservableProperty]
        private DialogViewModelBase _currentChild = null!;

        public OrderShellViewModel(OrderBoardViewModel boardViewModel)
        {
            _boardViewModel = boardViewModel;
            ShowBoard();
        }

        [RelayCommand]
        private void ShowBoard()
        {
            CurrentChild = _boardViewModel;
            PageTitle = "订单看板";
        }
    }
}
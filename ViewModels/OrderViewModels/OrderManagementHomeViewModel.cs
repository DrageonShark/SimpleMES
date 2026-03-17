using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleMES.ViewModels.OrderViewModels
{
    public partial class OrderManagementHomeViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        private string _description =
            "这里先作为订单维护主页。后面把修改、删除、审核等操作逐步迁进来。";

        public OrderManagementHomeViewModel()
        {
            PageTitle = "订单维护";
        }
    }
}

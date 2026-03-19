using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Toast;
using System.Collections.ObjectModel;
using System.Windows;

namespace SimpleMES.ViewModels.OrderViewModels
{
    /// <summary>
    /// 订单看板页面
    /// </summary>
    public partial class OrderBoardViewModel : DialogViewModelBase
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;

        public ObservableCollection<OrderModel> Orders { get; } = new();

        [ObservableProperty]
        private OrderModel? _selectedOrder;

        public OrderBoardViewModel(IDataRepository repository, IToastService toast)
        {
            _toast = toast;
            _repository = repository;
            _ = LoadOrders();
        }

        [RelayCommand]
        private async Task LoadOrders()
        {
            try
            {
                var list = await _repository.GetAllOrdersAsync().ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Orders.Clear();
                    foreach (var order in list)
                    {
                        Orders.Add(order);
                    }
                });
            }
            catch (Exception ex)
            {
                _toast.Error($"订单加载失败: {ex.Message}", null, 2.5);
            }
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleMES.ViewModels
{
    public partial class OrderViewModel:ViewModelBase
    {
        private readonly IDataRepository _repository;
        // 表格绑定的数据源
        public ObservableCollection<OrderModel> Orders { get; set; } = new ObservableCollection<OrderModel>();
        // === 新增订单的表单字段 ===
        [ObservableProperty] private string _newOrderNo;
        [ObservableProperty] private string _newProductCode;
        [ObservableProperty] private int _newPlanQty = 100;

        //核心业务数据
        //下拉框用的产品列表
        public ObservableCollection<ProductModel> Products { get; } = new ObservableCollection<ProductModel>();
        [ObservableProperty] private ProductModel _productOrder;
        public OrderViewModel()
        {
            _repository = new DataRepository(new SqlDbService());
            _ = LoadOrders();
        }

        public OrderViewModel(IDbService dbService)
        {
            _repository = new DataRepository(dbService);
            _ = LoadOrders();
        }

        [RelayCommand]
        private async Task LoadOrders()
        {
            try
            {
                var list = await _repository.GetAllOrdersAsync().ConfigureAwait(false);
                var products = await _repository.GetAllProductsAsync().ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Orders.Clear();
                    foreach (var order in list.ToList())
                    {
                        Orders.Add(order);
                    }
                    Products.Clear();
                    foreach (var product in products.ToList())
                    {
                        Products.Add(product);
                    }
                });
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败: {ex.Message}");
            }
        }
        [RelayCommand]
        private async Task CreateOrder()
        {
            //1.简单的校验
            if (string.IsNullOrWhiteSpace(NewOrderNo) || string.IsNullOrWhiteSpace(NewProductCode))
            {
                MessageBox.Show("请填写完整订单信息！");
                return;
            }

            try
            {
                // 2. 构建模型
                var order = new OrderModel()
                {
                    OrderNo = NewOrderNo,
                    ProductCode = NewProductCode,
                    PlanQty = NewPlanQty,
                    OrderStatus = 0,
                    CreateTime = DateTime.Now
                };
                // 3. 写入数据库
                await _repository.CreateOrderAsync(order).ConfigureAwait(false);
                // 4. 刷新列表 & 清空输入框
                await LoadOrders().ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NewOrderNo = "";
                    MessageBox.Show("订单创建成功！");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建失败: {ex.Message}");
            }
        }
       
    }
}

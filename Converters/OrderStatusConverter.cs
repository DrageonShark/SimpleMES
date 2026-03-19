using SimpleMES.Services.Orders; // OrderStatus 命名空间按你项目实际调整
using System.Globalization;
using System.Windows.Data;

namespace SimpleMES.Converters
{
    public class OrderStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "未知";

            // 兼容枚举输入
            if (value is OrderStatus s)
            {
                return s switch
                {
                    OrderStatus.Pending => "待生产",
                    OrderStatus.Producing => "生产中",
                    OrderStatus.Paused => "暂停中",
                    OrderStatus.Completed => "已完工",
                    OrderStatus.Scrapped => "已报废",
                    _ => "其他"
                };
            }

            // 兼容英文字符串输入
            var text = value.ToString()?.Trim();
            return text switch
            {
                "Pending" => "待生产",
                "Producing" => "生产中",
                "Paused" => "暂停中",
                "Completed" => "已完工",
                "Scrapped" => "已报废",
                "Other" => "其他",
                _ => "其他"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim();
            return text switch
            {
                "待生产" => OrderStatus.Pending,
                "生产中" => OrderStatus.Producing,
                "暂停中" => OrderStatus.Paused,
                "已完工" => OrderStatus.Completed,
                "已报废" => OrderStatus.Scrapped,
                "其他" => OrderStatus.Other,
                _ => OrderStatus.Other
            };
        }
    }
}
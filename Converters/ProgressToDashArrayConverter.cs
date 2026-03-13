using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimpleMES.Converters
{
    /// <summary>
    /// WPF 值转换器，
    /// 把 VM 的进度值（double）转换为 Path.StrokeDashArray（DoubleCollection）
    /// </summary>
    public class ProgressToDashArrayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 把传入的value转成double类型，转失败就设为0；同时保证数值≥0（避免负数）
            var units = value is double d ? Math.Max(0, d) : 0;
            // 返回DoubleCollection，第一个值是圆环显示的长度，第二个值是“空白部分”的长度
            return new DoubleCollection { units, 999d };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}

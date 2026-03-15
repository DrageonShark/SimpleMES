using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleMES.Converters
{
    /// <summary>
    /// 将 bool 转换为 GridLength
    /// true  -> 0 (折叠)
    /// false -> 指定宽度 (默认200)
    /// </summary>
    public class BoolToGridLengthConverter : IValueConverter
    {
        /// <summary>
        /// 数据源 -> UI 时调用
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {

            // 1 类型检查（防止绑定到错误类型）
            if (targetType != typeof(GridLength))
                throw new InvalidOperationException($"BoolToGridLengthConverter转换器只能与GridLength一起使用。目标类型为 {targetType}。");
            // 2 null 检查
            if (value == null)
            {
                return new GridLength(0);
            }
            // 3 类型转换
            if (!(value is bool collapsed))
            {
                throw new InvalidOperationException(
                    $"{value}不是布尔类型"
                );
            }// 4 默认展开宽度
            double expandedWidth = 200;
            // 5 解析 ConverterParameter
            if (parameter != null)
            {
                if (!double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out expandedWidth))
                {
                    expandedWidth = 200;
                }
            }
            // 6 返回 GridLength
            return collapsed
                ? new GridLength(0, GridUnitType.Pixel)
                : new GridLength(expandedWidth, GridUnitType.Pixel);
        }
        /// <summary>
        /// UI -> 数据源 时调用
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
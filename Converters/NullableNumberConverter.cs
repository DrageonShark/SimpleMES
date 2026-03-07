using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleMES.Converters
{
    /// <summary>
    /// 可空数值类型的双向值转换器，用于 WPF 数据绑定。
    /// <para>
    /// 将可空数值（如 <see cref="int?"/>、<see cref="double?"/> 等）转换为字符串以供 UI 显示，
    /// 并将用户输入的字符串转换回对应的可空数值类型。
    /// </para>
    /// </summary>
    /// <remarks>
    /// 在 XAML 中使用示例：
    /// <code>
    /// &lt;TextBox Text="{Binding NullableValue, Converter={StaticResource NullableNumberConverter}}"/&gt;
    /// </code>
    /// </remarks>
    public class NullableNumberConverter : IValueConverter
    {
        /// <summary>
        /// 将可空数值转换为字符串，用于 UI 显示。
        /// </summary>
        /// <param name="value">要转换的源值，通常为可空数值类型（如 <see cref="int?"/>、<see cref="double?"/>）。</param>
        /// <param name="targetType">绑定目标属性的类型，此方法中未使用。</param>
        /// <param name="parameter">传入的转换参数，此方法中未使用。</param>
        /// <param name="culture">转换时使用的区域信息，此方法中未使用。</param>
        /// <returns>
        /// 若 <paramref name="value"/> 不为 <see langword="null"/>，返回其 <see cref="object.ToString()"/> 结果；
        /// 否则返回 <see cref="string.Empty"/>。
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 将用户输入的字符串转换回目标可空数值类型，用于数据回写。
        /// </summary>
        /// <param name="value">用户输入的值，通常为 <see cref="string"/>。</param>
        /// <param name="targetType">
        /// 绑定目标属性的类型，支持可空类型（如 <see cref="int?"/>）和非可空类型（如 <see cref="int"/>）。
        /// </param>
        /// <param name="parameter">传入的转换参数，此方法中未使用。</param>
        /// <param name="culture">转换时使用的区域信息，传递给 <see cref="System.Convert.ChangeType(object, Type, IFormatProvider)"/>。</param>
        /// <returns>
        /// <list type="bullet">
        ///   <item>
        ///     <description>若输入为空白字符串，返回 <see langword="null"/>（适用于可空类型）。</description>
        ///   </item>
        ///   <item>
        ///     <description>若转换成功，返回转换后的目标类型值。</description>
        ///   </item>
        ///   <item>
        ///     <description>若转换失败（如格式不合法），返回 <see cref="DependencyProperty.UnsetValue"/> 以阻止非法值写入绑定源。</description>
        ///   </item>
        /// </list>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && string.IsNullOrWhiteSpace(str))
                return null;

            try
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return System.Convert.ChangeType(value, underlyingType, culture);
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
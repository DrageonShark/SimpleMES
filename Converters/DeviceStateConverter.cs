using SimpleMES.Services.State;
// 枚举转中文的转换器
using System.Globalization;
using System.Windows.Data;

namespace SimpleMES.Converters
{
    public class DeviceStateConverter : IValueConverter
    {
        // 枚举 → 显示文本
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DeviceState state) return "故障";

            return state switch
            {
                DeviceState.Running => "运行中",
                DeviceState.Disconnected => "未连接",
                _ => "故障"
            };
        }

        // 显示文本 → 枚举（如果需要双向绑定才实现）
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            return text switch
            {
                "运行中" => DeviceState.Running,
                "已停止" => DeviceState.Disconnected,
                _ => DeviceState.Fault
            };
        }
    }
}

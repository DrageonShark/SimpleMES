using System.Globalization;
using System.Windows.Data;

namespace SimpleMES.Converters
{
    internal class UserRoleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not int role) return -1;
            return role switch
            {
                1 => "管理员",
                2 => "组长",
                3 => "员工",
                _ => "其他账号"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value as string;
            return text switch
            {
                "管理员" => 1,
                "组长" => 2,
                "员工" => 3,
                _ => -1
            };
        }
    }
}

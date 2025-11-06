using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null!;
            return value.ToString()!;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null!;
            return Enum.Parse(targetType, (string)value);
        }
    }
}
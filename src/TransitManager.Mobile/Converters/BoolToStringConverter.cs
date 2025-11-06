using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b) return string.Empty;
            var parameters = parameter as string;
            if (string.IsNullOrEmpty(parameters)) return b.ToString();

            var parts = parameters.Split('|');
            return b ? parts[0] : (parts.Length > 1 ? parts[1] : string.Empty);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
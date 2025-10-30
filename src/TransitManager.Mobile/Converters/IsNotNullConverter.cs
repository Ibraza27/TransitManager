using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // La conversion retour n'est pas nécessaire ici
            throw new NotImplementedException();
        }
    }
}
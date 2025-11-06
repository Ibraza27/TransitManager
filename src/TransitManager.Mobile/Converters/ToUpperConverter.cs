using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class ToUpperConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string str ? str.ToUpper() : value!;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // La conversion retour n'est généralement pas nécessaire pour l'affichage
            throw new NotImplementedException();
        }
    }
}
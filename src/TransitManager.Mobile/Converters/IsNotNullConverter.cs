using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        // --- DÉBUT DE LA CORRECTION ---
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked)
            {
                // Si la case est cochée, on veut une date.
                // Si elle n'est pas cochée, on veut null.
                return isChecked ? DateTime.Now : null;
            }

            // En cas de valeur inattendue, on ne fait rien.
            return null;
        }
        // --- FIN DE LA CORRECTION ---
    }
}
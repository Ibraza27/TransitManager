using System;
using System.Globalization;
using System.Windows.Data;

namespace TransitManager.WPF.Converters
{
    public class ToUpperConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.ToUpper();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Le retour n'est pas nécessaire pour un affichage simple
            throw new NotImplementedException();
        }
    }
}
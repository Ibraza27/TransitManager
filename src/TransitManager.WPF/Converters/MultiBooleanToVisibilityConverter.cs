// src/TransitManager.WPF/Converters/MultiBooleanToVisibilityConverter.cs

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TransitManager.WPF.Converters
{
    public class MultiBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // La logique est : "Rendre visible si TOUTES les conditions sont vraies"
            bool allTrue = values.OfType<bool>().All(b => b);

            return allTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
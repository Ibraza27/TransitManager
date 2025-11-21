// src/TransitManager.WPF/Converters/NullToVisibilityConverter.cs

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TransitManager.WPF.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            bool invert = parameter?.ToString() == "Inverse";

            // Si on n'inverse pas : null -> Collapsed, non-null -> Visible
            // Si on inverse :      null -> Visible,   non-null -> Collapsed
            return (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
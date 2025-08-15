using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace TransitManager.WPF.Helpers
{
    public class BoolToInkCanvasEditingModeConverter : IValueConverter
    {
        public InkCanvasEditingMode Ink { get; set; }
        public InkCanvasEditingMode Erase { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Ink : Erase;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
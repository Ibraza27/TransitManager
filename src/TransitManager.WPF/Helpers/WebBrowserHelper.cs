using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;

namespace TransitManager.WPF.Helpers
{
    public static class WebBrowserHelper
    {
        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached("BindableSource", typeof(Uri), typeof(WebBrowserHelper), new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static Uri GetBindableSource(DependencyObject obj)
        {
            return (Uri)obj.GetValue(BindableSourceProperty);
        }

        public static void SetBindableSource(DependencyObject obj, Uri value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }

        public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is System.Windows.Controls.WebBrowser browser && e.NewValue is Uri uri)
            {
                browser.Source = uri;
            }
        }
    }
}
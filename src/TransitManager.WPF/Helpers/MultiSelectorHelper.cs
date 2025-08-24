using System.Collections;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace TransitManager.WPF.Helpers
{
    public class MultiSelectorHelper
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(MultiSelectorHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

        public static IList GetSelectedItems(DependencyObject obj) => (IList)obj.GetValue(SelectedItemsProperty);
        public static void SetSelectedItems(DependencyObject obj, IList value) => obj.SetValue(SelectedItemsProperty, value);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MultiSelector multiSelector) return;
            multiSelector.SelectionChanged -= OnSelectionChanged;
            if (e.NewValue != null) multiSelector.SelectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is not MultiSelector multiSelector) return;
            var selectedItems = GetSelectedItems(multiSelector);
            selectedItems.Clear();
            foreach (var item in multiSelector.SelectedItems) selectedItems.Add(item);
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TransitManager.WPF.Controls
{
    public partial class SearchControl : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(SearchControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SearchHintProperty =
            DependencyProperty.Register(nameof(SearchHint), typeof(string), typeof(SearchControl),
                new PropertyMetadata("Rechercher..."));

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(SearchControl));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public string SearchHint
        {
            get => (string)GetValue(SearchHintProperty);
            set => SetValue(SearchHintProperty, value);
        }

        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public SearchControl()
        {
            InitializeComponent();
        }
    }
}
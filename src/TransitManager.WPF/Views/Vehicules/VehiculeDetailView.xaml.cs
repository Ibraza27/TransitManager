using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Vehicules
{
    public partial class VehiculeDetailView : System.Windows.Controls.UserControl
    {
        private bool _isDrawingOrErasing = false;
        private System.Windows.Point _startPoint;

        public VehiculeDetailView()
        {
            InitializeComponent();
        }

        private void InkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(InkCanvasEtatDesLieux);
            _isDrawingOrErasing = true;
            InkCanvasEtatDesLieux.CaptureMouse();
        }

        private void InkCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingOrErasing && InkCanvasEtatDesLieux.EditingMode == InkCanvasEditingMode.Ink)
            {
                System.Windows.Point endPoint = e.GetPosition(InkCanvasEtatDesLieux);
                var distance = System.Windows.Point.Subtract(_startPoint, endPoint).Length;

                if (distance < 5.0)
                {
                    if (InkCanvasEtatDesLieux.Strokes.Count > 0)
                    {
                        InkCanvasEtatDesLieux.Strokes.RemoveAt(InkCanvasEtatDesLieux.Strokes.Count - 1);
                    }

                    if (DataContext is VehiculeDetailViewModel viewModel && viewModel.AddDamagePointCommand.CanExecute(_startPoint))
                    {
                        viewModel.AddDamagePointCommand.Execute(_startPoint);
                    }
                }
            }
            
            InkCanvasEtatDesLieux.ReleaseMouseCapture();
            _isDrawingOrErasing = false;
        }
    }
}
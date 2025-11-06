using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class EditEtatDesLieuxPage : ContentPage
{
    private EditEtatDesLieuxViewModel? _viewModel;

    public EditEtatDesLieuxPage(EditEtatDesLieuxViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        DamageGraphicsView.Drawable = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DamageGraphicsView.StartInteraction += OnStartInteraction;
        DamageGraphicsView.DragInteraction += OnDragInteraction;
        DamageGraphicsView.EndInteraction += OnEndInteraction;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DamageGraphicsView.StartInteraction -= OnStartInteraction;
        DamageGraphicsView.DragInteraction -= OnDragInteraction;
        DamageGraphicsView.EndInteraction -= OnEndInteraction;
    }

    private List<PointF>? _currentStroke;

    // --- NOUVELLE FONCTION D'AIDE ---
    private PointF NormalizePoint(PointF absolutePoint)
    {
        float canvasWidth = (float)DamageGraphicsView.Width;
        float canvasHeight = (float)DamageGraphicsView.Height;
        float imageSize = Math.Min(canvasWidth, canvasHeight);
        float offsetX = (canvasWidth - imageSize) / 2f;
        float offsetY = (canvasHeight - imageSize) / 2f;

        // Convertir le point absolu en coordonnées relatives à l'image
        float relativeX = absolutePoint.X - offsetX;
        float relativeY = absolutePoint.Y - offsetY;

        // Normaliser en pourcentage
        float normalizedX = relativeX / imageSize;
        float normalizedY = relativeY / imageSize;

        // S'assurer que les coordonnées restent entre 0 et 1
        return new PointF(
            Math.Clamp(normalizedX, 0, 1),
            Math.Clamp(normalizedY, 0, 1)
        );
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (_viewModel == null) return;
        
        PointF normalizedPoint = NormalizePoint(e.Touches[0]);

        if (_viewModel.SelectedTool == DrawingTool.Pen)
        {
            _currentStroke = new List<PointF> { normalizedPoint };
            _viewModel.RayuresStrokes.Add(_currentStroke);
        }
        else if (_viewModel.SelectedTool == DrawingTool.Eraser)
        {
            EraseAtPoint(normalizedPoint);
        }
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_viewModel == null) return;
        PointF normalizedPoint = NormalizePoint(e.Touches[0]);

        if (_viewModel.SelectedTool == DrawingTool.Pen)
        {
            _currentStroke?.Add(normalizedPoint);
        }
        else if (_viewModel.SelectedTool == DrawingTool.Eraser)
        {
            EraseAtPoint(normalizedPoint);
        }

        DamageGraphicsView.Invalidate();
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (_viewModel == null || _currentStroke == null || !_currentStroke.Any()) return;

        if (_viewModel.SelectedTool == DrawingTool.Pen)
        {
            var start = _currentStroke.First();
            var end = _currentStroke.Last();
            
            // On calcule la distance en coordonnées normalisées, pas en pixels.
            // On peut donc utiliser un seuil plus petit.
            var distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));

            if (distance < 0.02) // Seuil pour un clic (2% de la taille de l'image)
            {
                _viewModel.RayuresStrokes.Remove(_currentStroke);
                _viewModel.DamagePoints.Add(start);
            }
        }
        
        _currentStroke = null;
        DamageGraphicsView.Invalidate();
    }


    private void EraseAtPoint(PointF normalizedTouchPoint)
    {
        if (_viewModel == null) return;

        // Fonction locale pour calculer la distance entre deux points normalisés
        double CalculateNormalizedDistance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        // Seuil d'effacement (ex: 5% de la taille de l'image)
        double eraserRadius = 0.05;

        // Effacer les points d'impact
        for (int i = _viewModel.DamagePoints.Count - 1; i >= 0; i--)
        {
            var point = _viewModel.DamagePoints[i];
            if (CalculateNormalizedDistance(point, normalizedTouchPoint) < eraserRadius)
            {
                _viewModel.DamagePoints.RemoveAt(i);
            }
        }

        // Effacer les rayures
        for (int i = _viewModel.RayuresStrokes.Count - 1; i >= 0; i--)
        {
            var stroke = _viewModel.RayuresStrokes[i];
            // Si N'IMPORTE QUEL point de la rayure est proche du point touché, on supprime TOUTE la rayure.
            if (stroke.Any(p => CalculateNormalizedDistance(p, normalizedTouchPoint) < eraserRadius))
            {
                _viewModel.RayuresStrokes.RemoveAt(i);
            }
        }
    }
}
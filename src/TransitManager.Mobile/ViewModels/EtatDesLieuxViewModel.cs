using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using TransitManager.Core.Entities;
using TransitManager.Core;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(VehiculeJson), "vehiculeJson")]
    public partial class EtatDesLieuxViewModel : ObservableObject, IDrawable
    {
        [ObservableProperty]
        private string _vehiculeJson = string.Empty;

        [ObservableProperty]
        private string _planImagePath = "vehicule_plan.png";

        public ObservableCollection<PointF> DamagePoints { get; } = new();
        public ObservableCollection<List<PointF>> RayuresStrokes { get; } = new();

        async partial void OnVehiculeJsonChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            
            var serializerOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
            var vehicule = JsonSerializer.Deserialize<Vehicule>(Uri.UnescapeDataString(value), serializerOptions);
            if (vehicule == null) return;
            
            PlanImagePath = GetImagePathForType(vehicule.Type);
            
            // --- CORRECTION : CHARGEMENT DES POINTS D'IMPACT ---
            DamagePoints.Clear();
            if (!string.IsNullOrEmpty(vehicule.EtatDesLieux))
            {
                try
                {
                    var points = JsonSerializer.Deserialize<List<SerializablePoint>>(vehicule.EtatDesLieux);
                    if (points != null) 
                    {
                        foreach(var p in points) DamagePoints.Add(new PointF((float)p.X, (float)p.Y));
                    }
                }
                catch { /* Ignorer */ }
            }

            // CHARGEMENT DES RAYURES (déjà correct)
            RayuresStrokes.Clear();
            if (!string.IsNullOrEmpty(vehicule.EtatDesLieuxRayures))
            {
                try
                {
                    var strokes = JsonSerializer.Deserialize<List<List<SerializablePoint>>>(vehicule.EtatDesLieuxRayures);
                    if (strokes != null)
                    {
                        foreach (var stroke in strokes)
                        {
                            var pointsList = stroke.Select(p => new PointF((float)p.X, (float)p.Y)).ToList();
                            RayuresStrokes.Add(pointsList);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur chargement rayures (JSON): {ex.Message}");
                }
            }
        }
        
        private string GetImagePathForType(Core.Enums.TypeVehicule type)
        {
            return type switch
            {
                Core.Enums.TypeVehicule.Voiture => "voiture_plan.png",
                Core.Enums.TypeVehicule.Moto => "moto_plan.png",
                Core.Enums.TypeVehicule.Scooter => "moto_plan.png",
                Core.Enums.TypeVehicule.Camion => "camion_plan.png",
                Core.Enums.TypeVehicule.Bus => "camion_plan.png",
                Core.Enums.TypeVehicule.Van => "van_plan.png",
                _ => "vehicule_plan.png"
            };
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // --- CORRECTION : LOGIQUE DE DESSIN AVEC NORMALISATION ---
            // On utilise la taille réelle du canvas (dirtyRect) pour dessiner
            float canvasWidth = dirtyRect.Width;
            float canvasHeight = dirtyRect.Height;

            // Dessin des points d'impact (on multiplie les pourcentages par la taille du canvas)
            canvas.FillColor = Colors.Red.WithAlpha(0.5f);
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;
            foreach (var point in DamagePoints)
            {
                float x = point.X * canvasWidth;
                float y = point.Y * canvasHeight;
                canvas.FillCircle(x, y, 10);
                canvas.DrawCircle(x, y, 10);
            }

            // Dessin des rayures
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 3; 
            foreach (var stroke in RayuresStrokes)
            {
                if (stroke.Count > 1)
                {
                    PathF path = new PathF();
                    path.MoveTo(stroke[0].X * canvasWidth, stroke[0].Y * canvasHeight);
                    for (int i = 1; i < stroke.Count; i++)
                    {
                        path.LineTo(stroke[i].X * canvasWidth, stroke[i].Y * canvasHeight);
                    }
                    canvas.DrawPath(path);
                }
            }
        }
    }
}
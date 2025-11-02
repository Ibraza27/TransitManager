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
            // dirtyRect nous donne la taille réelle du canvas sur l'écran.
            float canvasWidth = dirtyRect.Width;
            float canvasHeight = dirtyRect.Height;

            // L'image a un ratio de 1:1 (1024x1024). Le canvas a une taille rectangulaire.
            // A cause de AspectFit, l'image va soit toucher les bords gauche/droite, soit haut/bas.
            // On doit trouver la taille réelle de l'image DANS le canvas.
            float imageSize = Math.Min(canvasWidth, canvasHeight);
            
            // Calculer le décalage pour centrer le dessin sur l'image
            float offsetX = (canvasWidth - imageSize) / 2f;
            float offsetY = (canvasHeight - imageSize) / 2f;

            // --- DESSIN DES POINTS D'IMPACT ---
            canvas.FillColor = Colors.Red.WithAlpha(0.6f); // Un peu plus opaque
            canvas.StrokeColor = Colors.DarkRed;
            canvas.StrokeSize = 1;

            foreach (var point in DamagePoints)
            {
                // On applique le pourcentage à la taille réelle de l'image, puis on ajoute le décalage
                float x = (point.X * imageSize) + offsetX;
                float y = (point.Y * imageSize) + offsetY;
                
                // Dessiner un cercle de 15 pixels de rayon
                canvas.FillCircle(x, y, 5);
                canvas.DrawCircle(x, y, 5);
            }

            // --- DESSIN DES RAYURES ---
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 4; // Un peu plus épais

            foreach (var stroke in RayuresStrokes)
            {
                if (stroke.Count > 1)
                {
                    PathF path = new PathF();
                    // Point de départ
                    path.MoveTo(stroke[0].X * imageSize + offsetX, stroke[0].Y * imageSize + offsetY);
                    
                    // Lignes suivantes
                    for (int i = 1; i < stroke.Count; i++)
                    {
                        path.LineTo(stroke[i].X * imageSize + offsetX, stroke[i].Y * imageSize + offsetY);
                    }
                    canvas.DrawPath(path);
                }
            }
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;
using TransitManager.Core;

namespace TransitManager.Mobile.ViewModels
{
    public enum DrawingTool { Pen, Eraser }

    [QueryProperty(nameof(VehiculeJson), "vehiculeJson")]
    public partial class EditEtatDesLieuxViewModel : ObservableObject, IDrawable
    {
        private readonly ITransitApi _transitApi; // <-- Assurez-vous que c'est bien ici

        [ObservableProperty]
        private string _vehiculeJson = string.Empty;

        [ObservableProperty]
        private string _planImagePath = "vehicule_plan.png";

        [ObservableProperty]
        private Vehicule? _vehicule;

        public ObservableCollection<PointF> DamagePoints { get; } = new();
        public ObservableCollection<List<PointF>> RayuresStrokes { get; } = new();
        
        [ObservableProperty]
        private DrawingTool _selectedTool = DrawingTool.Pen;

        // Le constructeur doit injecter ITransitApi
        public EditEtatDesLieuxViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        partial void OnVehiculeJsonChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            
            var serializerOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
            Vehicule = JsonSerializer.Deserialize<Vehicule>(Uri.UnescapeDataString(value), serializerOptions);
            
            if (Vehicule == null) return;
            
            PlanImagePath = GetImagePathForType(Vehicule.Type);
            
            DamagePoints.Clear();
            if (!string.IsNullOrEmpty(Vehicule.EtatDesLieux))
            {
                try
                {
                    var points = JsonSerializer.Deserialize<List<SerializablePoint>>(Vehicule.EtatDesLieux);
                    if (points != null) foreach(var p in points) DamagePoints.Add(new PointF((float)p.X, (float)p.Y));
                }
                catch {}
            }

            RayuresStrokes.Clear();
            if (!string.IsNullOrEmpty(Vehicule.EtatDesLieuxRayures))
            {
                try
                {
                    var strokes = JsonSerializer.Deserialize<List<List<SerializablePoint>>>(Vehicule.EtatDesLieuxRayures);
                    if (strokes != null)
                    {
                        foreach (var stroke in strokes)
                        {
                            var pointsList = stroke.Select(p => new PointF((float)p.X, (float)p.Y)).ToList();
                            RayuresStrokes.Add(pointsList);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Erreur chargement rayures (JSON): {ex.Message}"); }
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
        
        [RelayCommand]
        void ClearAll()
        {
            DamagePoints.Clear();
            RayuresStrokes.Clear();
        }

		[RelayCommand]
		async Task SaveAsync()
		{
			if (Vehicule == null) return;

			// Les collections contiennent DÉJÀ des coordonnées normalisées (en %)
			var pointsToSave = DamagePoints.Select(p => new SerializablePoint { X = p.X, Y = p.Y }).ToList();
			Vehicule.EtatDesLieux = JsonSerializer.Serialize(pointsToSave);

			var strokesToSave = RayuresStrokes.Select(stroke => 
				stroke.Select(p => new SerializablePoint { X = p.X, Y = p.Y }).ToList()
			).ToList();
			Vehicule.EtatDesLieuxRayures = JsonSerializer.Serialize(strokesToSave);

			try
			{
				await _transitApi.UpdateVehiculeAsync(Vehicule.Id, Vehicule);
				await Shell.Current.DisplayAlert("Succès", "État des lieux enregistré.", "OK");
				await Shell.Current.GoToAsync("..");
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Erreur", $"Impossible d'enregistrer : {ex.Message}", "OK");
			}
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
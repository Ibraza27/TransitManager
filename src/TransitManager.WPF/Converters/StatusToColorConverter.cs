using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TransitManager.Core.Enums;

namespace TransitManager.WPF.Converters
{
    /// <summary>
    /// Convertit un statut en couleur pour l'affichage
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Couleurs par défaut
            var defaultColor = new SolidColorBrush(System.Windows.Media.Colors.Gray);
            
            if (value == null) return defaultColor;

            // Conversion selon le type de statut
            switch (value)
            {
                case StatutColis statutColis:
                    return GetColisStatusColor(statutColis);
                    
                case StatutConteneur statutConteneur:
                    return GetConteneurStatusColor(statutConteneur);
                    
                case StatutPaiement statutPaiement:
                    return GetPaiementStatusColor(statutPaiement);
                    
                case TypeNotification typeNotification:
                    return GetNotificationTypeColor(typeNotification);
                    
                default:
                    return defaultColor;
            }
        }

        private System.Windows.Media.Brush GetColisStatusColor(StatutColis statut)
        {
            return statut switch
            {
                StatutColis.EnAttente => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)), // Orange
                StatutColis.Affecte => new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)), // Blue
                StatutColis.EnTransit => new SolidColorBrush(System.Windows.Media.Color.FromRgb(3, 169, 244)), // Light Blue
                StatutColis.Arrive => new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)), // Light Green
                StatutColis.Livre => new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)), // Green
                StatutColis.Probleme => new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)), // Red
                StatutColis.Perdu => new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)), // Gray
                StatutColis.Retourne => new SolidColorBrush(System.Windows.Media.Color.FromRgb(121, 85, 72)), // Brown
                _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }

        private System.Windows.Media.Brush GetConteneurStatusColor(StatutConteneur statut)
        {
            return statut switch
            {
                StatutConteneur.Ouvert => new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)), // Green
                StatutConteneur.EnPreparation => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)), // Amber
                StatutConteneur.EnTransit => new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)), // Blue
                StatutConteneur.Arrive => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 188, 212)), // Cyan
                StatutConteneur.EnDedouanement => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)), // Orange
                StatutConteneur.Livre => new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 195, 74)), // Light Green
                StatutConteneur.Cloture => new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)), // Gray
                StatutConteneur.Annule => new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)), // Red
                _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }

        private System.Windows.Media.Brush GetPaiementStatusColor(StatutPaiement statut)
        {
            return statut switch
            {
                StatutPaiement.EnAttente => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)), // Amber
                StatutPaiement.Valide => new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)), // Blue
                StatutPaiement.Paye => new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)), // Green
                StatutPaiement.Annule => new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)), // Gray
                StatutPaiement.Rembourse => new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 39, 176)), // Purple
                StatutPaiement.Echoue => new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)), // Red
                _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }

        private System.Windows.Media.Brush GetNotificationTypeColor(TypeNotification type)
        {
            return type switch
            {
                TypeNotification.Information => new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)), // Blue
                TypeNotification.Succes => new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)), // Green
                TypeNotification.Avertissement => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)), // Orange
                TypeNotification.Erreur => new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)), // Red
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)) // Blue par défaut
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
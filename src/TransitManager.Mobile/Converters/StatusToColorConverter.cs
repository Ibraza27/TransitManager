using System.Globalization;
using TransitManager.Core.Enums;

namespace TransitManager.Mobile.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is StatutVehicule statut)
            {
                return statut switch
                {
                    StatutVehicule.EnAttente => Colors.Orange,
                    StatutVehicule.Affecte => Colors.CornflowerBlue,
                    StatutVehicule.EnTransit => Colors.DodgerBlue,
                    StatutVehicule.Arrive => Colors.LightGreen,
                    StatutVehicule.EnDedouanement => Colors.DarkOrange,
                    StatutVehicule.Livre => Colors.Green,
                    StatutVehicule.Probleme => Colors.Red,
                    StatutVehicule.Retourne => Colors.Brown,
                    _ => Colors.Gray
                };
            }
			
            if (value is StatutColis statutColis)
            {
                return statutColis switch
                {
                    StatutColis.EnAttente => Colors.Orange,
                    StatutColis.Affecte => Colors.CornflowerBlue,
                    StatutColis.EnTransit => Colors.DodgerBlue,
                    StatutColis.Arrive => Colors.LightGreen,
                    StatutColis.EnDedouanement => Colors.DarkOrange,
                    StatutColis.Livre => Colors.Green,
                    StatutColis.Probleme => Colors.Red,
                    StatutColis.Perdu => Colors.DarkGray,
                    StatutColis.Retourne => Colors.Brown,
                    _ => Colors.Gray
                };
            }
			
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
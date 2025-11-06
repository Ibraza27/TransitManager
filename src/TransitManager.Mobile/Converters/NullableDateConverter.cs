using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class NullableDateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Si la date nullable a une valeur, on la retourne. Sinon, on retourne la date du jour
            // pour que le DatePicker ne plante pas.
            return (value as DateTime?) ?? DateTime.Now;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                // On pourrait ajouter une logique ici si on voulait qu'une date spécifique (ex: la date minimale) représente null,
                // mais pour le moment, le binding unidirectionnel suffit si la case à cocher gère le null.
                return date;
            }
            return null;
        }
    }
}
using System.Globalization;

namespace TransitManager.Mobile.Converters
{
    public class IsDateOverdueConverter : IValueConverter
    {
        // On définit le nombre de jours comme une constante
        private const int OverdueDays = 5;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                // Retourne true si la date est plus vieille que le nombre de jours définis
                return (DateTime.UtcNow - date).TotalDays > OverdueDays;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
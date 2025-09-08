using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TransitManager.Core.Entities
{
    // On conserve INotifyPropertyChanged
    public class InventaireItem : INotifyPropertyChanged 
    {
        private string _designation = string.Empty;
        private int _quantite = 1;
        private decimal _valeur;
		private DateTime _date = DateTime.Today; // AJOUTÉ

        public string Designation
        {
            get => _designation;
            set => SetProperty(ref _designation, value);
        }

        public int Quantite
        {
            get => _quantite;
            set => SetProperty(ref _quantite, value);
        }

        public decimal Valeur
        {
            get => _valeur;
            set => SetProperty(ref _valeur, value);
        }

		public DateTime Date
		{
			get => _date;
			set => SetProperty(ref _date, value);
		}


        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
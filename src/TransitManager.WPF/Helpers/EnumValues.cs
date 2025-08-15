// Fichier : src/TransitManager.WPF/Helpers/EnumValues.cs
using System;
using System.Linq;
using TransitManager.Core.Enums;

// VÉRIFIEZ BIEN QUE LE NAMESPACE EST CELUI DES HELPERS
namespace TransitManager.WPF.Helpers 
{
    public static class EnumValues
    {
        public static object[] TypeColisValues => Enum.GetValues(typeof(TypeColis)).Cast<object>().ToArray();
		public static object[] TypeVehiculeValues => Enum.GetValues(typeof(TypeVehicule)).Cast<object>().ToArray(); 
        public static object[] EtatColisValues => Enum.GetValues(typeof(EtatColis)).Cast<object>().ToArray();
		public static object[] TypeEnvoiValues => Enum.GetValues(typeof(TypeEnvoi)).Cast<object>().ToArray();
    }
}
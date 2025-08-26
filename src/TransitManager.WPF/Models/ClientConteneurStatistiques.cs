using TransitManager.Core.Entities;

namespace TransitManager.WPF.Models
{
    public class ClientConteneurStatistiques
    {
        public Client Client { get; }

        public decimal PrixTotalConteneur { get; set; }
        public decimal TotalPayeConteneur { get; set; }
        public decimal TotalRestantConteneur => PrixTotalConteneur - TotalPayeConteneur;

        public ClientConteneurStatistiques(Client client)
        {
            Client = client;
        }
    }
}
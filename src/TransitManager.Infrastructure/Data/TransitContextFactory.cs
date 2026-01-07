using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TransitManager.Infrastructure.Data
{
    public class TransitContextFactory : IDesignTimeDbContextFactory<TransitContext>
    {
        public TransitContext CreateDbContext(string[] args)
        {
            // Construire la configuration pour lire la chaîne de connexion
            // On pointe vers le dossier du projet Web pour trouver appsettings.json si nécessaire,
            // ou on hardcode pour le dev si plus simple, mais le mieux est de lire la config.
            
            // Note: En mode design, on est souvent dans le répertoire du projet Infrastructure ou Root.
            // On va essayer de trouver le appsettings du projet Web.
            
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../TransitManager.Web");
            if (!Directory.Exists(basePath))
            {
                basePath = Directory.GetCurrentDirectory(); // Fallback
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<TransitContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Fallback si la configuration ne charge pas (ex: chemin incorrect)
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Server=localhost;Database=TransitManagerDb;User Id=postgres;Password=password;";
            }

            builder.UseNpgsql(connectionString);

            return new TransitContext(builder.Options);
        }
    }
}
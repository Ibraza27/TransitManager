using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration; // <-- Using ajouté
using System.IO;                       // <-- Using ajouté

namespace TransitManager.Infrastructure.Data
{
    public class TransitContextFactory : IDesignTimeDbContextFactory<TransitContext>
    {
        public TransitContext CreateDbContext(string[] args)
        {
            // Trouve le chemin du projet WPF où se trouve appsettings.json
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\TransitManager.WPF"));
            
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<TransitContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(connectionString);
            
            return new TransitContext(optionsBuilder.Options);
        }
    }
}
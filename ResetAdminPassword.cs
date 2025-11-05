using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BCryptNet = BCrypt.Net.BCrypt;

class ResetAdminPassword
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Reset Admin Password ===");
        Console.WriteLine();

        string connectionString = "Host=localhost;Database=TransitManager;Username=postgres;Password=postgres";
        string newPassword = "Admin@123";
        string adminEmail = "admin@transitmanager.com";

        try
        {
            // Générer le nouveau hash BCrypt
            string newHash = BCryptNet.HashPassword(newPassword);
            Console.WriteLine($"Nouveau hash généré pour le mot de passe '{newPassword}'");
            Console.WriteLine($"Hash: {newHash}");
            Console.WriteLine();

            // Se connecter à la base de données et mettre à jour le mot de passe
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Vérifier si l'utilisateur existe
            await using var checkCmd = new NpgsqlCommand(
                @"SELECT ""Id"", ""Email"", ""NomUtilisateur"", ""Actif""
                  FROM ""Utilisateurs""
                  WHERE ""Email"" = @email",
                connection);
            checkCmd.Parameters.AddWithValue("@email", adminEmail);

            await using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                Console.WriteLine($"ERREUR: Aucun utilisateur trouvé avec l'email {adminEmail}");
                return;
            }

            var userId = reader.GetGuid(0);
            var email = reader.GetString(1);
            var username = reader.GetString(2);
            var actif = reader.GetBoolean(3);

            await reader.CloseAsync();

            Console.WriteLine($"Utilisateur trouvé:");
            Console.WriteLine($"  ID: {userId}");
            Console.WriteLine($"  Email: {email}");
            Console.WriteLine($"  Nom d'utilisateur: {username}");
            Console.WriteLine($"  Actif: {actif}");
            Console.WriteLine();

            if (!actif)
            {
                Console.WriteLine("ATTENTION: L'utilisateur n'est pas actif!");
            }

            // Mettre à jour le mot de passe
            await using var updateCmd = new NpgsqlCommand(
                @"UPDATE ""Utilisateurs""
                  SET ""MotDePasseHash"" = @hash,
                      ""TentativesConnexionEchouees"" = 0,
                      ""DateVerrouillage"" = NULL,
                      ""DoitChangerMotDePasse"" = false
                  WHERE ""Email"" = @email",
                connection);
            updateCmd.Parameters.AddWithValue("@hash", newHash);
            updateCmd.Parameters.AddWithValue("@email", adminEmail);

            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                Console.WriteLine("✓ Mot de passe réinitialisé avec succès!");
                Console.WriteLine($"✓ Email: {adminEmail}");
                Console.WriteLine($"✓ Nouveau mot de passe: {newPassword}");
                Console.WriteLine();

                // Vérifier que le nouveau hash fonctionne
                bool isValid = BCryptNet.Verify(newPassword, newHash);
                Console.WriteLine($"✓ Vérification du hash: {(isValid ? "OK" : "ÉCHEC")}");
            }
            else
            {
                Console.WriteLine("ERREUR: Aucune ligne mise à jour");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

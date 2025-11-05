// Programme simple pour générer un hash BCrypt
// Compilez et exécutez ce programme pour obtenir le hash correct
// dotnet run --project GeneratePasswordHash.csproj

using BCryptNet = BCrypt.Net.BCrypt;

class GeneratePasswordHash
{
    static void Main()
    {
        string password = "Admin@123";
        string hash = BCryptNet.HashPassword(password);

        Console.WriteLine("=== Générateur de Hash BCrypt ===");
        Console.WriteLine();
        Console.WriteLine($"Mot de passe: {password}");
        Console.WriteLine($"Hash BCrypt: {hash}");
        Console.WriteLine();
        Console.WriteLine("Vérification du hash:");
        bool isValid = BCryptNet.Verify(password, hash);
        Console.WriteLine($"  Résultat: {(isValid ? "✓ OK" : "✗ ÉCHEC")}");
        Console.WriteLine();
        Console.WriteLine("Requête SQL pour mettre à jour la base de données:");
        Console.WriteLine();
        Console.WriteLine($@"UPDATE ""Utilisateurs""
SET ""MotDePasseHash"" = '{hash}',
    ""TentativesConnexionEchouees"" = 0,
    ""DateVerrouillage"" = NULL,
    ""DoitChangerMotDePasse"" = false,
    ""Actif"" = true
WHERE ""Email"" = 'admin@transitmanager.com';");
    }
}

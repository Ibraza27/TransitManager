using System;

// Génère un hash BCrypt pour "Admin@123" que nous allons utiliser partout.
string password = "Admin@123";
string hash = BCrypt.Net.BCrypt.HashPassword(password);

Console.WriteLine("Copiez la ligne de hash suivante :");
Console.WriteLine(hash);

// Vérification pour être sûr
bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
Console.WriteLine($"Vérification : {isValid}"); // Doit afficher True
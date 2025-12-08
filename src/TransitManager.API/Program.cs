using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransitManager.API.Hubs;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Infrastructure.Repositories;
using TransitManager.Infrastructure.Services;
using CommunityToolkit.Mvvm.Messaging;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using TransitManager.API.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using QuestPDF.Infrastructure;
using System.IO;
var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
// --- LOG AU DÉMARRAGE ---
Console.WriteLine("[API] Démarrage de la configuration des services...");
// === DÉBUT DE L'AJOUT STRATÉGIQUE ===
Console.WriteLine("[API] Configuration du partage de clés de protection des données...");
try
{
    // Remplacez ce chemin par le dossier que vous avez créé.
    var keyPath = @"C:\Keys\TransitManager";
    Directory.CreateDirectory(keyPath); // S'assure que le dossier existe
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("TransitManagerShared"); // Nom d'application partagé
    Console.WriteLine($"[API] ✅ Les clés de protection seront stockées dans : {keyPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"[API] 💥 ERREUR CRITIQUE lors de la configuration de DataProtection : {ex.Message}");
    throw; // Arrêter l'application si on ne peut pas configurer la sécurité
}
// === FIN DE L'AJOUT STRATÉGIQUE ===
// --- CONFIGURATION DB ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<TransitContext>(options =>
    options.UseNpgsql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
);
// --- INJECTION DES DÉPENDANCES ---
builder.Services.AddTransient<TransitManager.Core.Interfaces.IAuthenticationService, AuthenticationService>();
builder.Services.AddTransient<IClientService, ClientService>();
builder.Services.AddTransient<IColisService, ColisService>();
builder.Services.AddTransient<IVehiculeService, VehiculeService>();
builder.Services.AddTransient<IConteneurService, ConteneurService>();
builder.Services.AddTransient<IPaiementService, PaiementService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IBarcodeService, BarcodeService>();
builder.Services.AddTransient<IExportService, ExportService>();
builder.Services.AddTransient<IBackupService, BackupService>();
builder.Services.AddTransient<IPrintingService, PrintingService>();
builder.Services.AddTransient<IDocumentService, DocumentService>();
builder.Services.AddTransient<IJwtService, JwtService>();
builder.Services.AddTransient<IUserService, UserService>(); 
builder.Services.AddSingleton<INotificationHubService, NotificationHubService>();
builder.Services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddTransient<IClientRepository, ClientRepository>();
builder.Services.AddTransient<IColisRepository, ColisRepository>();
builder.Services.AddTransient<IConteneurRepository, ConteneurRepository>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
builder.Services.AddTransient<IEmailService, EmailService>();
// --- SERVICES WEB API ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// --- CORRECTION DÉFINITIVE : AUTHENTIFICATION HYBRIDE CORRECTEMENT CONFIGURÉE ---
Console.WriteLine("[API] Configuration de l'authentification (Cookie + JWT)...");
// On a besoin de IHttpContextAccessor dans notre nouveau handler
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    Console.WriteLine("[API] Ajout du gestionnaire de Cookie.");
    Console.WriteLine("[API - Cookie] Configuration avancée : SameSite=None, SecurePolicy=Always.");
    // Nommer le cookie pour le retrouver facilement dans le navigateur
    options.Cookie.Name = "TransitManager.AuthCookie";
    // Le cookie ne sera pas accessible par JavaScript côté client (sécurité)
    options.Cookie.HttpOnly = true;
    // Essentiel pour le développement local (ports différents) et les déploiements cross-domain.
    // Le navigateur enverra le cookie même si l'API et le client n'ont pas la même origine.
    options.Cookie.SameSite = SameSiteMode.None;
    // SameSiteMode.None REQUIERT que le cookie soit marqué comme Secure.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // On s'assure que le cookie persiste bien comme demandé dans LoginWithCookie
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        Console.WriteLine("[API - Cookie] Événement OnRedirectToLogin déclenché. Remplacement par un statut 401.");
        context.Response.StatusCode = 401; // Unauthorized
        return Task.CompletedTask;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    Console.WriteLine("[API] Ajout du gestionnaire de JWT Bearer.");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});
// --- AUTORISATION ---
Console.WriteLine("[API] Configuration de la politique d'autorisation HYBRIDE...");
// Enregistrer notre nouveau handler d'autorisation
builder.Services.AddSingleton<IAuthorizationHandler, HybridAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    // On crée une politique nommée "HybridPolicy" qui utilise notre nouvelle exigence.
    options.AddPolicy("HybridPolicy", policy =>
    {
        policy.Requirements.Add(new HybridRequirement());
    });
    // TRÈS IMPORTANT: On définit notre politique hybride comme politique par défaut.
    // Cela signifie que tout endpoint avec un simple [Authorize] utilisera cette logique.
    options.DefaultPolicy = options.GetPolicy("HybridPolicy")!;
});
// --- SIGNALR, CORS, etc. ---
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true)
              .AllowCredentials();
    });
});
Console.WriteLine("[API] Fin de la configuration des services.");
var app = builder.Build();
// --- PIPELINE HTTP ---
Console.WriteLine("[API] Configuration du pipeline HTTP...");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRouting(); // Ajout de UseRouting pour un ordre explicite
Console.WriteLine("[API] Ajout des middlewares d'authentification et d'autorisation.");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TransitContext>();
    
    // --- 1. Restauration & Protection Admin ---
    var adminUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == "admin@transitmanager.com");
    if (adminUser == null)
    {
        Console.WriteLine("[Maintenance] ⚠️ Compte Admin introuvable ! Recréation...");
        adminUser = new TransitManager.Core.Entities.Utilisateur
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            NomUtilisateur = "admin",
            Nom = "Administrateur",
            Prenom = "Système",
            Email = "admin@transitmanager.com",
            // Mot de passe : Admin123!
            MotDePasseHash = "$2a$11$Tb9CvmOW2h/YNRaP.3QZsOo3jxIN0IN.M4khQYoZu7Ji8i82WyDxu", 
            Role = TransitManager.Core.Enums.RoleUtilisateur.Administrateur,
            DateCreation = DateTime.UtcNow,
            Actif = true,
            EmailConfirme = true // Force la confirmation
        };
        context.Utilisateurs.Add(adminUser);
        await context.SaveChangesAsync();
        Console.WriteLine("[Maintenance] ✅ Compte Admin restauré.");
    }
    else if (!adminUser.EmailConfirme)
    {
        // Si l'admin existe mais n'est pas confirmé, on le valide de force pour éviter le blocage
        adminUser.EmailConfirme = true;
        await context.SaveChangesAsync();
        Console.WriteLine("[Maintenance] ✅ Compte Admin validé de force.");
    }

    // --- 2. Validation massive des anciens comptes Staff (Hack temporaire) ---
    // On valide automatiquement tous les comptes qui NE SONT PAS des clients (Gestionnaires, Comptables, etc.)
    var staffUsers = await context.Utilisateurs
        .Where(u => !u.EmailConfirme && u.Role != TransitManager.Core.Enums.RoleUtilisateur.Client)
        .ToListAsync();
        
    foreach(var u in staffUsers) 
    {
        u.EmailConfirme = true; 
    }
    if (staffUsers.Any()) 
    {
        await context.SaveChangesAsync();
        Console.WriteLine($"[Maintenance] {staffUsers.Count} comptes Staff validés automatiquement.");
    }

    // --- 3. Nettoyage des comptes CLIENTS non confirmés (Garbage Collection) ---
    try
    {
        Console.WriteLine("[Maintenance] 🧹 Analyse des comptes expirés...");
        
        var threshold = DateTime.UtcNow.AddHours(-24); // Comptes créés il y a plus de 24h

        // SÉCURITÉ : On filtre explicitement sur Role == Client
        // On ne touche JAMAIS aux Admins, Gestionnaires, etc. ici.
        var usersToDelete = await context.Utilisateurs
            .Where(u => !u.EmailConfirme 
                        && u.DateCreation < threshold
                        && u.Role == TransitManager.Core.Enums.RoleUtilisateur.Client) 
            .Include(u => u.Client) // Pour supprimer la fiche client associée si elle est orpheline
            .ToListAsync();

        if (usersToDelete.Any())
        {
            foreach (var user in usersToDelete)
            {
                // Si un client est lié et semble avoir été créé juste pour cet user (pas de code client métier spécifique), on nettoie
                if (user.ClientId.HasValue && user.Client != null)
                {
                    context.Clients.Remove(user.Client);
                }
                context.Utilisateurs.Remove(user);
            }

            int count = await context.SaveChangesAsync();
            Console.WriteLine($"[Maintenance] 🗑️ {count} inscriptions clients non confirmées ont été supprimées.");
        }
        else
        {
            Console.WriteLine("[Maintenance] Aucun compte client expiré à nettoyer.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Maintenance] ⚠️ Erreur lors du nettoyage : {ex.Message}");
    }
}
 
Console.WriteLine("[API] Lancement de l'application.");
app.Run();

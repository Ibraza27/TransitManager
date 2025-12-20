using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Infrastructure.Repositories;
using TransitManager.Infrastructure.Services;
using TransitManager.Infrastructure.Data.Uow;
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
using TransitManager.Infrastructure.Hubs;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

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

// --- OPTIMISATIONS (PHASE 1 & 3) ---
Console.WriteLine("[API] Configuration des services d'optimisation (Cache, RateLimiting, HealthChecks)...");
builder.Services.AddMemoryCache(); // Pour le cache des stats interne
builder.Services.AddHealthChecks(); // Monitoring de base
builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 1000, 
                QueueLimit = 2,
                Window = TimeSpan.FromMinutes(1)
            }));
});
// -----------------------------------

// --- CONFIGURATION DB ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<TransitContext>(options =>
    options.UseNpgsql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
);

// --- CACHE & SIGNALR (REDIS OPTIONNEL) ---
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
bool useRedis = !string.IsNullOrWhiteSpace(redisConnectionString) && redisConnectionString != "localhost:6379"; 

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    Console.WriteLine($"[API] ⚡ Configuration de Redis (Cache & SignalR) sur : {redisConnectionString}");
    
    // 1. Cache Distribué Redis
    try 
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "TransitManager_";
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] ⚠️ Erreur configuration Cache Redis : {ex.Message}. Fallback sur Mémoire.");
        builder.Services.AddDistributedMemoryCache();
        useRedis = false;
    }
}
else
{
    Console.WriteLine("[API] ℹ️ Redis non configuré. Utilisation du Cache en Mémoire (DistributedMemoryCache).");
    builder.Services.AddDistributedMemoryCache(); // Fallback vital
    useRedis = false;
}

// --- INJECTION DES DÉPENDANCES ---
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

// On remplace toutes les injections de XRepository ou de IDbContextFactory par IUnitOfWork
builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
builder.Services.AddTransient<IClientService, ClientService>();
builder.Services.AddTransient<IColisService, ColisService>();
builder.Services.AddTransient<IVehiculeService, VehiculeService>();
builder.Services.AddTransient<IConteneurService, ConteneurService>();
builder.Services.AddTransient<IPaiementService, PaiementService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IFinanceService, FinanceService>();
builder.Services.AddTransient<IBarcodeService, BarcodeService>();
builder.Services.AddTransient<IExportService, ExportService>();
builder.Services.AddTransient<IBackupService, BackupService>();
builder.Services.AddTransient<IPrintingService, PrintingService>();
builder.Services.AddTransient<IDocumentService, DocumentService>();
builder.Services.AddTransient<IJwtService, JwtService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddSingleton<INotificationHubService, NotificationHubService>();
builder.Services.AddHostedService<MaintenanceService>(); // AJOUT SERVICE MAINTENANCE (Background)
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IMessageService, MessageService>();
builder.Services.AddTransient<ITimelineService, TimelineService>();
// --- SERVICES WEB API ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; 
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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

    options.Cookie.Name = "TransitManager.AuthCookie";
    options.Cookie.HttpOnly = true;

    // MODIFICATION CRITIQUE ICI
    // "None" permet l'envoi du cookie même si l'appel vient d'un autre port (Web vers API)
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Obligatoire si SameSite=None

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // ... (reste inchangé)
    options.Events.OnRedirectToLogin = context =>
    {
        // Pour SignalR, on veut une erreur 401, pas une redirection HTML
        if (context.Request.Path.StartsWithSegments("/notificationHub"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // Si la requête est pour le Hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/notificationHub") || path.StartsWithSegments("/appHub")))
            {
                // ... on lit le token depuis l'URL
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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
// --- SIGNALR ---
// Augmenter les limites pour éviter les timeouts en dev
var signalRBuilder = builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

if (useRedis && !string.IsNullOrWhiteSpace(redisConnectionString))
{
    Console.WriteLine("[API] ⚡ SignalR configuré avec Backplane Redis.");
    try 
    {
        signalRBuilder.AddStackExchangeRedis(redisConnectionString, options => {
            options.Configuration.ChannelPrefix = "TransitManagerSignalR";
        });
    }
    catch(Exception ex)
    {
         Console.WriteLine($"[API] ⚠️ Impossible de configurer SignalR avec Redis : {ex.Message}. Mode autonome activé.");
    }
}
else
{
    Console.WriteLine("[API] ℹ️ SignalR configuré en mode Autonome (pas de Redis).");
}


// === AJOUTER CETTE LIGNE ICI ===
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, TransitManager.API.Hubs.CustomUserIdProvider>();
// ===============================

builder.Services.AddCors(options =>
// ... la suite reste inchangée
{
    options.AddPolicy("AllowAll", policy =>
    {
        // REMPLACE "SetIsOriginAllowed(origin => true)" PAR CECI SI POSSIBLE :
        policy.WithOrigins("https://localhost:7207", "https://100.91.147.96:7207", "http://localhost:5129") // Liste tes URLs Web
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // OBLIGATOIRE pour les cookies/auth SignalR
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
// Utilisation explicite de UseEndpoints pour s'assurer que les Hubs sont bien mappés après l'auth
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<AppHub>("/appHub");

Console.WriteLine("[API] Lancement de l'application.");
app.Run();

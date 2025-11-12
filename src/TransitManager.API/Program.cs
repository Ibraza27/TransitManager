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

var builder = WebApplication.CreateBuilder(args);

// --- LOG AU DÉMARRAGE ---
Console.WriteLine("[API] Démarrage de la configuration des services...");

// --- CONFIGURATION DB ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TransitContext>(options =>
    options.UseNpgsql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
);

// --- INJECTION DES DÉPENDANCES ---
builder.Services.AddScoped<TransitManager.Core.Interfaces.IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IColisService, ColisService>();
builder.Services.AddScoped<IVehiculeService, VehiculeService>();
builder.Services.AddScoped<IConteneurService, ConteneurService>();
builder.Services.AddScoped<IPaiementService, PaiementService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IPrintingService, PrintingService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<INotificationHubService, NotificationHubService>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IColisRepository, ColisRepository>();
builder.Services.AddScoped<IConteneurRepository, ConteneurRepository>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

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
builder.Services.AddAuthentication(options =>
{
    // Le schéma Cookie est utilisé pour les actions interactives du navigateur (login, logout)
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Si un endpoint [Authorize] est accédé sans authentification, le challenge se fait via Cookie
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    Console.WriteLine("[API] Ajout du gestionnaire de Cookie.");
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
Console.WriteLine("[API] Configuration de l'autorisation...");
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme)
        .Build();

    // CORRECTION : Simplification de la politique pour accepter JWT ou Cookie
    options.AddPolicy("AllowInternalOrAuthenticated", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
    });
});

// NOTE : Nous pouvons commenter ou supprimer le handler personnalisé s'il n'est plus nécessaire
// builder.Services.AddSingleton<IAuthorizationHandler, InternalOrJwtAuthorizationHandler>();

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

Console.WriteLine("[API] Lancement de l'application.");
app.Run();
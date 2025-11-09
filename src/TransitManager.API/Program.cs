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

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURATION DE LA CONNEXION À LA BASE DE DONNÉES ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TransitContext>(options =>
    options.UseNpgsql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
);

// --- INJECTION DES DÉPENDANCES (SERVICES ET REPOSITORIES) ---
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
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

// Notification Hub Service
builder.Services.AddSingleton<INotificationHubService, NotificationHubService>();

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IColisRepository, ColisRepository>();
builder.Services.AddScoped<IConteneurRepository, ConteneurRepository>();

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Messenger
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

// --- CONFIGURATION DES SERVICES WEB API ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORRECTION : CONFIGURATION D'AUTHENTIFICATION CORRECTE ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
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

// --- CONFIGURATION D'AUTORISATION ---
builder.Services.AddAuthorization(options =>
{
    // Politique par défaut (requiert un token JWT)
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build();

    // Politique "hybride" pour l'interne ou JWT
    options.AddPolicy("AllowInternalOrAuthenticated", policy =>
    {
        policy.Requirements.Add(new InternalOrJwtRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, InternalOrJwtAuthorizationHandler>();

// --- CONFIGURATION DE SIGNALR ---
builder.Services.AddSignalR();

// --- CONFIGURATION CORS ---
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

var app = builder.Build();

// --- CONFIGURATION DU PIPELINE HTTP ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT: L'ordre des middlewares est crucial
app.UseCors("AllowAll");
app.UseAuthentication(); // Doit venir avant UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
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

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURATION DE LA CONNEXION À LA BASE DE DONNÉES ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<TransitContext>(options =>
    options.UseNpgsql(connectionString));

// --- INJECTION DES DÉPENDANCES (SERVICES ET REPOSITORIES) ---
// Services de l'application
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

// Notification Hub Service (pour les notifications entre services et le Hub)
builder.Services.AddSingleton<INotificationHubService, NotificationHubService>();

// Repositories génériques et spécifiques
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IColisRepository, ColisRepository>();
builder.Services.AddScoped<IConteneurRepository, ConteneurRepository>();

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Messenger (si nécessaire pour la communication inter-services)
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);


// --- CONFIGURATION DES SERVICES WEB API ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Gère les boucles de référence (Client -> Colis -> Client)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        
        // Ignore les propriétés nulles lors de la sérialisation pour un JSON plus propre
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// --- CONFIGURATION DE SIGNALR ---
builder.Services.AddSignalR();

// --- CONFIGURATION CORS (Très important pour autoriser l'app mobile à se connecter) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true) // Autorise toutes les origines pour le développement
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Appliquer la politique CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mapper le Hub SignalR à une URL
app.MapHub<NotificationHub>("/notificationHub");


app.Run();
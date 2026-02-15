using Microsoft.AspNetCore.Components.Authorization;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;
using TransitManager.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using TransitManager.Web.Auth;
using TransitManager.Infrastructure.Services;
using TransitManager.Infrastructure.Data;
using TransitManager.Core.Interfaces; // Assuming IVehiculeService is here, let's check
using Microsoft.EntityFrameworkCore; // Might be needed for other things

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(); // Enable running as a Windows Service
// Directory.SetCurrentDirectory(AppContext.BaseDirectory); // Fix for Windows Service pathing to find appsettings.json
// === DÉBUT DE L'AJOUT STRATÉGIQUE ===
Console.WriteLine("[WEB] Configuration du partage de clés de protection des données...");
try
{
    var keyPath = @"C:\Keys\TransitManager";
    Directory.CreateDirectory(keyPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("TransitManagerShared");
    Console.WriteLine($"[WEB] ✅ Les clés de protection seront lues depuis : {keyPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"[WEB] 💥 ERREUR CRITIQUE lors de la configuration de DataProtection : {ex.Message}");
    throw;
}
// === DÉBUT DE LA MODIFICATION ===
// On remplace AddControllers() par AddControllersWithViews() pour enregistrer tous les services nécessaires, y compris les filtres.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// On ajoute explicitement le service Antiforgery. C'est ce service qui manquait.
builder.Services.AddAntiforgery();
// === FIN DE LA MODIFICATION ===
// --- SERVICES ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => 
    {
        // Increase circuit retention to 15 mins (default 3 mins) to allow mobile users to return from background
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);
    });

// Configure SignalR Hub Options for Blazor Server
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options => 
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(15);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

// === DÉBUT DE LA MODIFICATION ===
Console.WriteLine("[WEB] Configuration de l'authentification Cookie...");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TransitManager.AuthCookie";
        options.Cookie.HttpOnly = true;
        
        // === MODIFICATION ICI : Strict -> None ===
        options.Cookie.SameSite = SameSiteMode.None; 
        // =========================================
        
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        Console.WriteLine("[WEB] ✅ Le service d'authentification par cookie est configuré pour lire les cookies de l'API.");
    });
// === FIN DE LA MODIFICATION ===
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor(); // Indispensable pour le CookieHandler
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());

// Register DbContext for SettingsService (and generic usage)
builder.Services.AddDbContextFactory<TransitContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Settings Service (Direct DB Access)
builder.Services.AddScoped<ISettingsService, SettingsService>();
// === DÉBUT DE LA MODIFICATION HTTPCLIENT ===
// 1. Enregistrer notre handler personnalisé
builder.Services.AddTransient<CookieHandler>();
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}
// 2. Configurer le HttpClient pour le AccountController ET le DocumentProxyController ("API")
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<CookieHandler>() // <--- C'EST LA LIGNE MANQUANTE QUI CORRIGE LE 401
// REVERT: SSL Strict Mode disabled to fix "SSL connection could not be established" in Dev.
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});
// 3. Configurer le HttpClient pour ApiService en lui ajoutant notre handler
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
     client.BaseAddress = new Uri(apiBaseUrl);
     // Timeout global augmenté pour permettre l'upload de gros fichiers (vidéos)
     client.Timeout = TimeSpan.FromMinutes(10);
})
.AddHttpMessageHandler<CookieHandler>()
// AJOUT : On ignore les erreurs de certificat SSL ici aussi
// REVERT: SSL Strict Mode disabled to fix "SSL connection could not be established" in Dev.
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});
// === FIN DE LA MODIFICATION HTTPCLIENT ===
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<ContactEmailService>();
var app = builder.Build();
// --- PIPELINE ---

app.Use(async (context, next) =>
{
    // Récupérer l'IP du visiteur
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();
    
    // Récupérer la liste blanche depuis appsettings.json
    var allowedIps = builder.Configuration.GetSection("AllowedTailscaleIPs").Get<string[]>();

    // Si la liste existe et que l'IP n'est pas dedans (et que ce n'est pas localhost ipv6 ::1)
    if (allowedIps != null && remoteIp != null && remoteIp != "::1" && remoteIp != "127.0.0.1" && !allowedIps.Contains(remoteIp))
    {
        Console.WriteLine($"[SECURITÉ] Accès refusé pour l'IP : {remoteIp}");
        context.Response.StatusCode = 403; // Forbidden
        await context.Response.WriteAsync($"Accès refusé. Votre IP ({remoteIp}) n'est pas autorisée.");
        return;
    }
    
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();

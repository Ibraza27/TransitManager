using Microsoft.AspNetCore.Components.Authorization;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;
using TransitManager.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using TransitManager.Web.Auth;

var builder = WebApplication.CreateBuilder(args);
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
    .AddInteractiveServerComponents();
// === DÉBUT DE LA MODIFICATION ===
Console.WriteLine("[WEB] Configuration de l'authentification Cookie...");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TransitManager.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
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
// === DÉBUT DE LA MODIFICATION HTTPCLIENT ===
// 1. Enregistrer notre handler personnalisé
builder.Services.AddTransient<CookieHandler>();
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}
// 2. Configurer le HttpClient pour le AccountController ("API")
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
// 3. Configurer le HttpClient pour ApiService en lui ajoutant notre handler
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
     client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<CookieHandler>(); // <-- C'EST LA LIGNE MAGIQUE
// === FIN DE LA MODIFICATION HTTPCLIENT ===
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
var app = builder.Build();
// --- PIPELINE ---
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

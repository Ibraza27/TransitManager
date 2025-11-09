using Microsoft.AspNetCore.Components.Authorization;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;
using TransitManager.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies; // AJOUT

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- SOLUTION : CONFIGURATION AUTHENTIFICATION COOKIE ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ENREGISTREZ LES DEUX
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

// Services personnalisés
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// --- CONFIGURATION HTTPCLIENT SIMPLE ---
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}

// On enregistre HttpClient et ApiService séparément
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<IApiService, ApiService>();

// --- FIN DE LA CONFIGURATION ---

var app = builder.Build();

// --- PIPELINE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// MIDDLEWARES IMPORTANTS - ORDRE CRUCIAL
app.UseAuthentication();    // DOIT ÊTRE AVANT UseAuthorization
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
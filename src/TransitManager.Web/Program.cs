using Microsoft.AspNetCore.Components.Authorization;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;
using TransitManager.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// On enregistre les services d'authentification pour ASP.NET Core (corrige l'erreur 500 de l'API)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// --- CORRECTION DÉFINITIVE DE L'INJECTION ---
// On enregistre CustomAuthenticationStateProvider une seule fois, en tant que service Scoped.
// Blazor saura l'utiliser à la fois comme AuthenticationStateProvider et comme lui-même.
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
// On dit à Blazor que lorsque quelqu'un demande l'interface 'AuthenticationStateProvider',
// il doit utiliser l'instance de 'CustomAuthenticationStateProvider' déjà créée.
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<IApiService, ApiService>();


// --- SERVICES PERSONNALISÉS ---
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });


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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
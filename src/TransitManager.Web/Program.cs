using Microsoft.AspNetCore.Components.Authorization;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;
using TransitManager.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---

// Ajouter les services Razor Components et définir le mode interactif Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Services d'authentification
builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextAccessor>();

// Services personnalisés
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddTransient<AuthHeaderHandler>();

// Configuration HttpClient pour l'API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthHeaderHandler>();


// --- APPLICATION (MIDDLEWARE PIPELINE) ---

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ORDRE IMPORTANT CI-DESSOUS
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery(); // <-- VÉRIFIEZ BIEN QUE CETTE LIGNE EST PRÉSENTE ET À CET ENDROIT

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
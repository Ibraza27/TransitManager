// Créez un dossier "Controllers" à la racine de TransitManager.Web si il n'existe pas
// puis ajoutez ce fichier.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TransitManager.Core.DTOs;

namespace TransitManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

		[HttpPost("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] LoginRequestDto model)
        {
            Console.WriteLine("🛂 [AccountController] === DÉBUT Login POST ===");
            
            if (!ModelState.IsValid)
            {
                return Redirect("/login?error=invalid_input");
            }

            try
            {
                var apiClient = _httpClientFactory.CreateClient("API");
                var response = await apiClient.PostAsJsonAsync("api/auth/login-with-cookie", model);
                
                // Transfert des cookies (Auth ou autres)
                if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
                {
                    Response.Headers["Set-Cookie"] = setCookieHeaders.ToArray();
                }

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("🛂 [AccountController] ✅ Succès. Redirection Accueil.");
                    return Redirect("/");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🛂 [AccountController] ⚠️ API Erreur: {response.StatusCode}");

                    // URL par défaut
                    string redirectUrl = "/login?error=auth_failed";

                    // Gestion 403 : Email non confirmé
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) 
                    {
                        // CORRECTION : Utilisation de Uri.EscapeDataString pour éviter les caractères invalides
                        var safeEmail = Uri.EscapeDataString(model.Email ?? "");
                        redirectUrl = $"/login?error=unconfirmed&email={safeEmail}";
                    }
                    else
                    {
                        // Gestion Lockout (Json Parsing sécurisé)
                        try 
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                            if (doc.RootElement.TryGetProperty("lockoutEnd", out var lockElem) && lockElem.GetString() != null)
                            {
                                var safeLockout = Uri.EscapeDataString(lockElem.GetString()!);
                                redirectUrl = $"/login?error=locked&until={safeLockout}";
                            }
                        }
                        catch { /* Ignorer les erreurs de parsing JSON */ }
                    }

                    Console.WriteLine($"🛂 [AccountController] ↪️ Redirection vers : {redirectUrl}");
                    return Redirect(redirectUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🛂 [AccountController] 💥 Exception : {ex.Message}");
                return Redirect("/login?error=server_error");
            }
            finally
            {
                 Console.WriteLine("🛂 [AccountController] === FIN Login POST ===");
            }
        }


		[HttpPost("/account/resend-confirmation")]
		[HttpPost("/account/resend-confirmation")]
        [ValidateAntiForgeryToken] // <--- SÉCURITÉ RÉACTIVÉE (Fix V6)
        public async Task<IActionResult> ResendConfirmation([FromForm] string email)
        {
            Console.WriteLine($"🌐 [WEB] CLIC REÇU : Demande de renvoi pour '{email}'");

            if (string.IsNullOrWhiteSpace(email)) 
            {
                Console.WriteLine("⚠️ [WEB] L'email est vide. Redirection.");
                return Redirect("/login?error=invalid_input");
            }

            try 
            {
                var apiClient = _httpClientFactory.CreateClient("API");
                var dto = new ResendConfirmationDto { Email = email };
                
                Console.WriteLine($"🌐 [WEB] Envoi à l'API...");
                var response = await apiClient.PostAsJsonAsync("api/auth/resend-confirmation", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ [WEB] ERREUR API ({response.StatusCode}) : {content}");
                    return Redirect("/login?error=server_error");
                }

                Console.WriteLine("✅ [WEB] Succès ! Redirection.");
                return Redirect("/login?resend=success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 [WEB] CRASH : {ex.Message}");
                return Redirect("/login?error=server_error");
            }
        }

        [HttpPost("/account/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Console.WriteLine("🛂 [AccountController] === DÉBUT Logout POST ===");
            
            // Appelle l'API pour invalider la session côté API (si nécessaire à l'avenir)
            // Pour l'instant, le plus important est de supprimer le cookie du navigateur.
            
            // Supprime le cookie d'authentification du navigateur.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            Console.WriteLine("🛂 [AccountController] ✅ Cookie de déconnexion envoyé au navigateur. Redirection vers /login.");
            
            return Redirect("/login");
        }		

		
    }
}
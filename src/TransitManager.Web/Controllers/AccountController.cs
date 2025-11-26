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
                Console.WriteLine("🛂 [AccountController] ❌ ModelState invalide.");
                // Idéalement, retourner à la page de login avec un message d'erreur.
                // Pour l'instant, une redirection simple suffit.
                return Redirect("/login?error=invalid_input");
            }

            try
            {
                // Utiliser HttpClientFactory pour obtenir un client configuré
                var apiClient = _httpClientFactory.CreateClient("API");

                Console.WriteLine($"🛂 [AccountController] ➡️ Envoi de la requête de login à l'API pour {model.Email}...");
                var response = await apiClient.PostAsJsonAsync("api/auth/login-with-cookie", model);
                Console.WriteLine($"🛂 [AccountController] ⬅️ Réponse de l'API reçue : {response.StatusCode}");

                // TRÈS IMPORTANT : Transférer l'en-tête "Set-Cookie"
                if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
                {
                    Console.WriteLine("🛂 [AccountController] ✅ En-tête 'Set-Cookie' trouvé ! Transfert à la réponse du navigateur...");
                    // On attache le cookie à la réponse que CE contrôleur envoie au navigateur de l'utilisateur.
                    Response.Headers["Set-Cookie"] = setCookieHeaders.ToArray();
                    Console.WriteLine("🛂 [AccountController] ✅ Cookie transféré avec succès.");
                }
                else
                {
                    Console.WriteLine("🛂 [AccountController] ⚠️ Aucun en-tête 'Set-Cookie' trouvé dans la réponse de l'API.");
                }

                if (response.IsSuccessStatusCode)
                {
                    // La connexion a réussi, on redirige l'utilisateur vers la page d'accueil.
                    // Le navigateur va recevoir cette redirection ET le cookie à sauvegarder.
                    Console.WriteLine("🛂 [AccountController] ✅ Connexion API réussie. Redirection vers l'accueil...");
                    return Redirect("/");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🛂 [AccountController] ❌ Échec : {errorContent}");
                    
                    string redirectUrl = "/login?error=auth_failed";

                    // Tentative de lecture du JSON pour voir s'il y a un lockout
                    try 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                        if (doc.RootElement.TryGetProperty("lockoutEnd", out var lockElem) && lockElem.GetString() != null)
                        {
                            var lockoutTime = lockElem.GetString();
                            redirectUrl = $"/login?error=locked&until={System.Net.WebUtility.UrlEncode(lockoutTime)}";
                        }
                        else if(doc.RootElement.TryGetProperty("message", out var msgElem))
                        {
                             // On pourrait passer le message custom, mais auth_failed suffit souvent
                        }
                    }
                    catch {}

                    return Redirect(redirectUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🛂 [AccountController] 💥 Erreur fatale lors du login : {ex.Message}");
                return Redirect("/login?error=server_error");
            }
            finally
            {
                 Console.WriteLine("🛂 [AccountController] === FIN Login POST ===");
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
using System;
using System.Web.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System.Configuration;

namespace WebAppKeyVault.Controllers
{
    public class HomeController : Controller
    {
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var redisUri = ConfigurationManager.AppSettings["redisPrimaryKeySecretUri"];

            try
            {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var secret = await keyVaultClient.GetSecretAsync(redisUri).ConfigureAwait(false);
                ViewBag.Secret = $"Secret: {secret.Value}";                
            }
            catch (Exception exp)
            {
                ViewBag.Error = $"Something went wrong: {exp.Message}";
            }

            ViewBag.Principal = azureServiceTokenProvider.PrincipalUsed != null ? $"Principal Used: {azureServiceTokenProvider.PrincipalUsed}" : string.Empty;

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
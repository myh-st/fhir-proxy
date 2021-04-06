using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web;

namespace FHIRProxy
{
    public static class SMARTProxyAuthorize
    {
        [FunctionName("SMARTProxyAuthorize")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "fhir/AadSmartOnFhirProxy/authorize")] HttpRequest req,
            ILogger log)
        {
            string aadname=Utils.GetEnvironmentVariable("FP-LOGIN-AUTHORITY","login.microsoftonline.com");
            string aadpolicy = Utils.GetEnvironmentVariable("FP-LOGIN-POLICY", "");
            string tenant = Utils.GetEnvironmentVariable("FP-LOGIN-TENANT");
            if (tenant==null)
            {
                return new ContentResult() { Content = "Login Tenant not Configured...Cannot proxy AD Authorize Request", StatusCode = 500 , ContentType = "text/plain" };
            }
            
            string response_type = req.Query["response_type"];
            string client_id = req.Query["client_id"];
            string redirect_uri = req.Query["redirect_uri"];
            string launch = req.Query["launch"];
            string scope = req.Query["scope"];
            string state = req.Query["state"];
            string aud = req.Query["aud"];
            if (string.IsNullOrEmpty(aud)) aud = $"https://{tenant}/{client_id}";

            string newQueryString = $"response_type={response_type}&redirect_uri={redirect_uri}&client_id={client_id}";
            
            if (!string.IsNullOrEmpty(launch))
            {
                //TODO: Implement appropriate behavior
            }

            if (!string.IsNullOrEmpty(state))
            {
                newQueryString += $"&state={HttpUtility.UrlEncode(state)}";
            }

            if (!string.IsNullOrEmpty(scope))
            {
                string[] scopes = scope.Split(' ');
                var scopeString = "";
                foreach (var s in scopes)
                {
                    if (!string.IsNullOrEmpty(scopeString)) scopeString += " ";
                    if (s.StartsWith("patient", System.StringComparison.InvariantCultureIgnoreCase) || s.StartsWith("user", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        var newScope = s.Replace("/", ".");
                        scopeString += $"{aud}/{newScope}";
                    } else
                    {
                        scopeString += s;
                    }
                }
                newQueryString += $"&scope={HttpUtility.UrlEncode(scopeString)}";
            }
            string redirect = $"https://{aadname}/{tenant}";
            if (!string.IsNullOrEmpty(aadpolicy)) redirect += $"/{aadpolicy}";
            redirect += $"/oauth2/v2.0/authorize?{newQueryString}";
            return new RedirectResult(redirect, false);
           
        }
    }
}

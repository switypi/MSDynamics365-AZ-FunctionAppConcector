using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Principal;
using System.Text.Json;
using System.Net;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Collections.Generic;
using System.Linq;

namespace CrmDataverseConect
{
    public static class CrmConnectFunc
    {
        [FunctionName("CrmConnectFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Accounts/{name?}")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            DataResponse result;

            try
            {

                string _clientId = "5ee7c545-54a6-4228-9c28-5fa41db04cea";
                string _clientSecret = "rU-8Q~NDA4xtinqswslUNHDxa_md4boun6x_RdfA";
                string _environment = "org85191994";
                string _tenantId = "91d41eca-c9e8-4c16-9cc4-0131cc930c69";
                string apiVersion = "9.2";


                var scope = new[] { $"https://{_environment}.api.crm.dynamics.com/.default" };
                var webAPI = $"https://{_environment}.api.crm.dynamics.com/api/data/v{apiVersion}/";
                var authority = "https://login.microsoftonline.com/" + _tenantId + "/oauth2/v2.0/token";

                var clientApp = ConfidentialClientApplicationBuilder.Create(_clientId)
                     .WithClientSecret(_clientSecret)
                     .WithAuthority(new Uri(authority))
                     .Build();

                var authResult = await clientApp.AcquireTokenForClient(scope).ExecuteAsync();

                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(webAPI),
                    Timeout = new TimeSpan(0, 0, 10)
                };

                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                var response = await httpClient.GetAsync("accounts ?$top=10&$select=name");// "accounts ?$top=10&$select=name");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                if (response.IsSuccessStatusCode)
                {
                    var accountsResponse = await response.Content.ReadAsStringAsync();

                    var resp = JsonSerializer.Deserialize<Response<Account>>(accountsResponse, options).Value.ToList();
                    result = new DataResponse();
                    result.Accounts = resp;
                    result.IsSuccess = true;
                    
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    result = new DataResponse();
                    result.IsSuccess = false;
                    result.ErrorCode = errorResponse;
                   // var error = JsonSerializer.Deserialize<ErrorResponse>(errorResponse, options);

                   
                }

            }
            catch (Exception ex)
            {
                return new OkObjectResult(ex.Message);
                throw new(ex.Message);
            }
            return new OkObjectResult(result);


        }
    }

    class Response<T>
    {
        public T[] Value { get; set; }
    }

    class Account
    {
        public string Name { get; set; }
        public Guid AccountId { get; set; }
    }

    class DataResponse
    {
        public string ErrorCode { get; set; }
        public bool IsSuccess { get; set; }

        public List<Account> Accounts { get; set;}
    }
}

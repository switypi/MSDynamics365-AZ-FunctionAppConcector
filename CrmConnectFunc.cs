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

            string searchname = req.Query["name"];
            DataResponse result;

            try
            {

                string _clientId = "";
                string _clientSecret = "";
                string _environment = "";
                string _tenantId = "";
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

                HttpResponseMessage response;
                if (string.IsNullOrEmpty(searchname))
                  response=  await httpClient.GetAsync("accounts");
                else
                  response=  await httpClient.GetAsync("accounts ?$filter=startswith(name,'(searchname)')");

                // "accounts ?$top=10&select=searchname");

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

        public List<Account> Accounts { get; set; }
    }
}

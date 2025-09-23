using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMSBilling.Models;

namespace TMSBilling.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;

        public ApiService(HttpClient httpClient, ApiSettings apiSettings)
        {
            _httpClient = httpClient;
            _apiSettings = apiSettings;

            _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _apiSettings.Token.Replace("Bearer ", "")
                );
        }

        public async Task<(bool ok, JsonElement json)> SendRequestAsync(
            HttpMethod method,
            string url,
            object payload = null)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);

                if (payload != null)
                {
                    string jsonString = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // 🔍 Debug payload
                    Console.WriteLine("=== DEBUG PAYLOAD ===");
                    Console.WriteLine(jsonString);
                }

                // 🔍 Debug request
                Console.WriteLine("=== DEBUG REQUEST ===");
                Console.WriteLine($"{method} {_httpClient.BaseAddress}{url}");
                Console.WriteLine($"Authorization: {_httpClient.DefaultRequestHeaders.Authorization}");
                Console.WriteLine($"Content-Type: application/json");
                Console.WriteLine("=====================");

                using var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                // 🔍 Debug response
                Console.WriteLine("=== DEBUG RESPONSE ===");
                Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(responseContent);
                Console.WriteLine("=====================");

                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    return (response.IsSuccessStatusCode, jsonDoc.RootElement.Clone());
                }
                catch
                {
                    return (response.IsSuccessStatusCode,
                        JsonDocument.Parse($"{{\"raw\":\"{responseContent}\"}}").RootElement.Clone());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== DEBUG ERROR ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("===================");

                return (false,
                    JsonDocument.Parse($"{{\"error\":\"{ex.Message}\"}}").RootElement.Clone());
            }
        }


        public async Task<(bool ok, JsonElement json)> ExecuteGraphQLAsync(
        string query,
        object variables = null,
        string operationName = null)
        {
            try
            {
                var gqlRequest = new
                {
                    query,
                    variables,
                    operationName
                };

                string jsonString = JsonSerializer.Serialize(gqlRequest, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, _apiSettings.BaseUrlGraphql);
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // 🔍 Debug
                Console.WriteLine("=== DEBUG GRAPHQL REQUEST ===");
                Console.WriteLine(jsonString);
                Console.WriteLine("=============================");

                using var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                // 🔍 Debug
                Console.WriteLine("=== DEBUG GRAPHQL RESPONSE ===");
                Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(responseContent);
                Console.WriteLine("==============================");

                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    return (response.IsSuccessStatusCode, jsonDoc.RootElement.Clone());
                }
                catch
                {
                    return (response.IsSuccessStatusCode,
                        JsonDocument.Parse($"{{\"raw\":\"{responseContent}\"}}").RootElement.Clone());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== DEBUG GRAPHQL ERROR ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("===========================");

                return (false,
                    JsonDocument.Parse($"{{\"error\":\"{ex.Message}\"}}").RootElement.Clone());
            }
        }

    }
}

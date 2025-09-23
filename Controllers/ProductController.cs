using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Models;
using TMSBilling.Models.McEasyApiModel;

public class ProductController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    public ProductController(HttpClient httpClient, IOptions<ApiSettings> apiSettings)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
    }

    [ActionName("Index")]
    public async Task<IActionResult> Index(string search = "", int page = 1, int limit = 10)
    {
        // tambahin search ke query string
        string url = $"{_apiSettings.BaseUrl}/order/api/web/v1/product?limit={limit}&page={page}&search={search}";

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = $"API Error: {response.StatusCode}";
            return View(new List<Product>());
        }

        var json = await response.Content.ReadAsStringAsync();

        // Cek status code
        Console.WriteLine($"[DEBUG] Status Code: {response.StatusCode}");

        // Cek isi raw JSON
        Console.WriteLine("[DEBUG] Raw JSON Response:");
        Console.WriteLine(json);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var productResponse = JsonSerializer.Deserialize<ProductResponse>(json, options);
        var products = productResponse?.data?.paginated_result ?? new List<Product>();

        // biar View bisa tahu metadata + keyword search yang lagi dipakai
        ViewBag.Metadata = productResponse?.metadata;
        ViewBag.Search = search;

        return View(products);
    }

    public IActionResult Form(int? id)
    {
        // TODO: bisa bikin form add/edit product di sini
        return PartialView("_Form", new ProductStore());
    }

    [HttpPost]
    public async Task<IActionResult> Form(ProductStore model)
    {
        // serialize body sesuai JSON API
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(model),
            Encoding.UTF8,
            "application/json"
        );

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

        HttpResponseMessage response;

        if (model.Id == Guid.Empty) // create
        {
            response = await _httpClient.PostAsync($"{_apiSettings.BaseUrl}/order/api/web/v1/product", jsonContent);
        }
        else // update
        {
            response = await _httpClient.PutAsync($"{_apiSettings.BaseUrl}/order/api/web/v1/product/{model.Id}", jsonContent);
        }

        var apiResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return Json(new { success = false, message = $"API Error: {response.StatusCode}", detail = apiResponse });
        }

        return Json(new { success = true, data = apiResponse });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));
        var response = await _httpClient.DeleteAsync($"{_apiSettings.BaseUrl}/order/api/web/v1/product/{id}");
        var apiResponse = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return Json(new { success = false, message = $"API Error: {response.StatusCode}", detail = apiResponse });
        }
        return Json(new { success = true, data = apiResponse });
    }
}

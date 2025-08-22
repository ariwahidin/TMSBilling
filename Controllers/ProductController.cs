using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using TMSBilling.Models;

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
        string url = $"{_apiSettings.BaseUrl}/product?limit={limit}&page={page}&search={search}";

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = $"API Error: {response.StatusCode}";
            return View(new List<Product>());
        }

        var json = await response.Content.ReadAsStringAsync();
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
        return PartialView("_Form", new Product());
    }

}

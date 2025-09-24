using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Models;
using TMSBilling.Models.McEasyApiModel;
using TMSBilling.Services;

public class ProductController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ApiService _apiService;

    public ProductController(HttpClient httpClient, IOptions<ApiSettings> apiSettings, ApiService apiService)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        _apiService = apiService;
    }

    [ActionName("Index")]
    public async Task<IActionResult> Index(string search = "", int page = 1, int limit = 10)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product?limit={limit}&page={page}&search={search}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API get product",
                detail = json
            });
        }

        var products = json
        .GetProperty("data")
        .GetProperty("paginated_result")
        .Deserialize<List<Product>>() ?? new List<Product>();

        return View(products);
    }

    public async Task<IActionResult>  Form(Guid? id)
    {
        var model = new ProductStore();
        bool ok;
        JsonElement json = default;


        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-type?limit={100}&page={1}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API Get Type",
                detail = json
            });
        }

        var types = json.GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<ProductType>>() ?? new List<ProductType>();

        ViewBag.TypeList = types.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = c.id.ToString(),
            Text = c.name
        }).ToList();
        return PartialView("_Form", model);
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


    // PRODUCT CATEGORY
    [ActionName("Category")]
    public async Task<IActionResult> Category(string search = "", int page = 1, int limit = 10)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-category?limit={limit}&page={page}&search={search}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API get product category",
                detail = json
            });
        }

        var categorys = json
        .GetProperty("data")
        .GetProperty("paginated_result")
        .Deserialize<List<ProductCategory>>() ?? new List<ProductCategory>();

        return View("Category",categorys);
    }

    public async Task<IActionResult> FormCategory(Guid? id)
    {
        var model = new ProductCategoryStore();
        bool ok;
        JsonElement json = default;

        if (id != null)
        {
   
            (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Get,
                $"order/api/web/v1/product-category/{id}",
                new { }
            );

            if (!ok)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Gagal kirim ke API show category",
                    detail = json
                });
            }

            model = json.GetProperty("data").Deserialize<ProductCategoryStore>() ?? new ProductCategoryStore();
        }

        return PartialView("_FormCategory", model);
    }

    [HttpPost]
    public async Task<IActionResult> FormCategory(ProductCategoryStore model)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Post,
            $"order/api/web/v1/product-category",
            model
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API store product category",
                detail = json
            });
        }

        return Json(new { success = true, data = json });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(ProductCategoryStore model)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Patch,
            $"order/api/web/v1/product-category/{model.Id}",
            model
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API edit product category",
                detail = json
            });
        }

        return Json(new { success = true, data = json });
    }

    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Delete,
            $"order/api/web/v1/product-category/{id}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API delete category",
                detail = json
            });
        }

        return Json(new { success = true, data = json });
    }



    // PRODUCT TYPE
    [ActionName("Type")]
    public async Task<IActionResult> Type(string search = "", int page = 1, int limit = 10)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-type?limit={limit}&page={page}&search={search}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API get product type",
                detail = json
            });
        }

        var types = json
        .GetProperty("data")
        .GetProperty("paginated_result")
        .Deserialize<List<ProductType>>() ?? new List<ProductType>();

        return View("Type", types);
    }

    public async Task<IActionResult> FormType(Guid? id)
    {
        var model = new ProductTypeStore();
        bool ok;
        JsonElement json = default;


        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-category?limit={100}&page={1}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API Get Category",
                detail = json
            });
        }

        var categorys = json.GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<ProductCategory>>() ?? new List<ProductCategory>();

        ViewBag.CategoryList = categorys.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = c.id,
            Text = c.name
        }).ToList();


        if (id != null)
        {

            (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Get,
                $"order/api/web/v1/product-type/{id}",
                new { }
            );

            if (!ok)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Gagal kirim ke API show type",
                    detail = json
                });
            }

            var data = json.GetProperty("data").Deserialize<ProductType>() ?? new ProductType();
            model.id = data?.id;
            model.name = data?.name;
            model.product_category_id = data?.product_category?.id;
        }

        return PartialView("_FormType", model);
    }

    [HttpPost]
    public async Task<IActionResult> FormType(ProductTypeStore model)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Post,
            $"order/api/web/v1/product-type",
            model
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API store product type",
                detail = json
            });
        }

        return Json(new { success = true, data = json });
    }

    [HttpPost]
    public async Task<IActionResult> EditType(ProductTypeStore model)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Patch,
            $"order/api/web/v1/product-type/{model.id}",
            model
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API edit product type",
                detail = json
            });
        }

        return Json(new { success = true, data = json });
    }


    public async Task<IActionResult> DeleteType(Guid id)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Delete,
            $"order/api/web/v1/product-type/{id}",
            new { }
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "Gagal kirim ke API delete type",
                detail = json
            });
        }

        return Json(new { success = true, data = json });
    }

}

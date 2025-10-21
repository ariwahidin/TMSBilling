using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Models;
using TMSBilling.Models.McEasyApiModel;
using TMSBilling.Services;

public class ProductController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ApiService _apiService;
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context, HttpClient httpClient, IOptions<ApiSettings> apiSettings, ApiService apiService)
    {
        _context = context;
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        _apiService = apiService;
    }

    //[ActionName("Index")]
    //public async Task<IActionResult> Index(string search = "", int page = 1, int limit = 1000)
    //{
    //    bool ok;
    //    JsonElement json = default;

    //    (ok, json) = await _apiService.SendRequestAsync(
    //        HttpMethod.Get,
    //        $"order/api/web/v1/product?limit={limit}&page={page}"
    //    );

    //    if (!ok)
    //    {
    //        return BadRequest(new
    //        {
    //            success = false,
    //            message = "Gagal kirim ke API get product",
    //            detail = json
    //        });
    //    }

    //    var products = json
    //    .GetProperty("data")
    //    .GetProperty("paginated_result")
    //    .Deserialize<List<Product>>() ?? new List<Product>();

    //    //Console.WriteLine("Product", products);

    //    foreach (var p in products)
    //    {
    //        var existProduct = _context.Products.FirstOrDefault(o => o.ProductID == p.id);

    //        if (existProduct == null) {
    //            var newProduct = new ProductTable();
    //            newProduct.ProductID = p.id;
    //            newProduct.ProductTypeID = p.product_type?.id.ToString();
    //            newProduct.ProductTypeName = p.product_type?.name;
    //            newProduct.ProductCategoryID = p.product_type?.product_category?.id?.ToString();
    //            newProduct.ProductCategoryName = p.product_type?.product_category?.name?.ToString();
    //            newProduct.Name = p.name?.ToString();
    //            newProduct.Sku = p.sku?.ToString();
    //            newProduct.Description = p.description?.ToString();
    //            newProduct.Uom = p.uom?.ToString();
    //            newProduct.Weight = p.weight;
    //            newProduct.Volume = p.volume;
    //            newProduct.Price = p.price;
    //            newProduct.CreatedAt = DateTime.Now;

    //            _context.Products.Add(newProduct);
    //            _context.SaveChanges();
    //        }
    //    }


    //    return View(products);
    //}

    private async Task<List<Product>> FetchProductsFromApi(int page = 1, int limit = 1000)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product?limit={limit}&page={page}"
        );

        if (!ok)
            throw new Exception("Gagal kirim ke API get product");

        var products = json
            .GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<Product>>() ?? new List<Product>();

        return products;
    }
    private void SyncProductsToDatabase(List<Product> products)
    {
        var existingIds = _context.Products
            .Select(p => p.ProductID)
            .ToHashSet();

        var newProducts = new List<ProductTable>();

        foreach (var p in products)
        {
            if (!existingIds.Contains(p.id))
            {
                var newProduct = new ProductTable
                {
                    ProductID = p.id,
                    ProductTypeID = p.product_type?.id?.ToString(),
                    ProductTypeName = p.product_type?.name,
                    ProductCategoryID = p.product_type?.product_category?.id?.ToString(),
                    ProductCategoryName = p.product_type?.product_category?.name,
                    Name = p.name,
                    Sku = p.sku,
                    Description = p.description,
                    Uom = p.uom,
                    Weight = p.weight,
                    Volume = p.volume,
                    Price = p.price,
                    CreatedAt = DateTime.Now
                };

                newProducts.Add(newProduct);
            }
        }

        if (newProducts.Any())
        {
            _context.Products.AddRange(newProducts);
            _context.SaveChanges();
        }
    }
    public async Task<IActionResult> Index(string search = "", int page = 1, int limit = 1000)
    {
        try
        {
            var productsFromApi = await FetchProductsFromApi(page, limit);
            SyncProductsToDatabase(productsFromApi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sync data: {ex.Message}");
        }
        var products = await FetchProductsFromApi(page, limit);

        return View(products);
    }

    public async Task<IActionResult> Form(Guid? id)
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

        return View("Category", categorys);
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


    // PRODUCT UOM
    [ActionName("Uom")]
    public async Task<IActionResult> Uom(string search = "", int page = 1, int limit = 10)
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

}

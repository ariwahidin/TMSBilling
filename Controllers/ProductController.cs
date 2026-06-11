using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System.CodeDom;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.McEasyApiModel;
using TMSBilling.Services;


[SessionAuthorize]
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

    // =====================
    // HELPERS
    // =====================

    private bool IsApiCustomer(string subCode)
    {
        var group = _context.CustomerGroups
            .FirstOrDefault(g => g.SUB_CODE == subCode);
        return group?.API_FLAG == 1;
    }

    private List<SelectListItem> GetCustomerGroupList()
    {
        return _context.CustomerGroups
            .Select(g => new SelectListItem
            {
                Value = g.SUB_CODE,
                Text = g.SUB_CODE
            })
            .ToList();
    }

    private async Task<List<Product>> FetchProductsFromApi(int page = 1, int limit = 1000, string search = "")
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product?limit={limit}&page={page}&search={search}"
        );

        if (!ok)
            throw new Exception("Gagal kirim ke API get product");

        var products = json
            .GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<Product>>() ?? new List<Product>();

        return products;
    }

    private async Task<List<ProductCategory>> FetchProductCategoryFromApi(int page = 1, int limit = 1000)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-category?limit={limit}&page={page}"
        );

        if (!ok)
            throw new Exception("Failed to get product category");

        var categories = json
            .GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<ProductCategory>>() ?? new List<ProductCategory>();

        return categories;
    }

    private async Task<List<ProductType>> FetchProductTypeFromApi(int page = 1, int limit = 1000)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-type?limit={limit}&page={page}"
        );

        if (!ok) throw new Exception("Failed to get product type");

        var types = json
            .GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<ProductType>>() ?? new List<ProductType>();

        return types;
    }

    private async Task<ProductType> FetchProductTypeByID(Guid? id)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-type/{id}",
            new { }
        );

        if (!ok) throw new Exception("Failed to get product type by id");

        var data = json.GetProperty("data")
                       .Deserialize<ProductType>();

        return data;
    }

    private void SyncProductsToDatabase(List<Product> products, string subCode)
    {
        var existingIds = _context.Products
            .Where(p => p.SubCode == subCode)
            .Select(p => p.ProductID)
            .ToHashSet();

        var newProducts = new List<ProductTable>();

        foreach (var p in products)
        {
            if (!existingIds.Contains(p.id))
            {
                newProducts.Add(new ProductTable
                {
                    ProductID = p.id,
                    SubCode = subCode, // <--
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
                });
            }
        }

        if (newProducts.Any())
        {
            _context.Products.AddRange(newProducts);
            _context.SaveChanges();
        }
    }

    // =====================
    // PRODUCT
    // =====================

    [ActionName("Index")]
    public async Task<IActionResult> Index(string subCode = "", string search = "", int page = 1, int limit = 1000)
    {
        ViewBag.CustomerGroups = GetCustomerGroupList();
        ViewBag.SelectedCust = subCode;

        if (string.IsNullOrEmpty(subCode))
            return View(new List<Product>());

        bool isApi = IsApiCustomer(subCode);
        ViewBag.IsApi = isApi;

        if (isApi)
        {
            try
            {
                var fromApi = await FetchProductsFromApi(page, limit, search);
                SyncProductsToDatabase(fromApi, subCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sync: {ex.Message}");
            }

            var products = await FetchProductsFromApi(page, limit, search);
            return View(products);
        }
        else
        {
            var products = _context.Products
                .Where(p => p.SubCode == subCode &&
                            (string.IsNullOrEmpty(search) || p.Name.Contains(search)))
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new Product
                {
                    id = p.ProductID,
                    name = p.Name,
                    sku = p.Sku,
                    description = p.Description,
                    uom = p.Uom,
                    weight = p.Weight,
                    volume = p.Volume,
                    price = p.Price,
                    width = p.Width,
                    length = p.Length,
                    height = p.Height,
                    cbm = p.Cbm
                })
                .ToList();

            return View(products);
        }
    }

    public async Task<IActionResult> Form(Guid? id, string subCode = "")
    {
        bool isApi = IsApiCustomer(subCode);

        var model = new ProductStore
        {
            SubCode = subCode,
            IsApi = isApi
        };

        if (isApi)
        {
            var types = await FetchProductTypeFromApi();
            ViewBag.TypeList = types.Select(t => new SelectListItem
            {
                Value = t.id.ToString(),
                Text = t.name
            }).ToList();

            if (id != null && id != Guid.Empty)
            {
                bool ok;
                JsonElement json = default;

                (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Get,
                    $"order/api/web/v1/product/{id}"
                );

                if (ok)
                {
                    var product = json.GetProperty("data").Deserialize<ProductStore>();
                    if (product != null)
                    {
                        product.SubCode = subCode;
                        product.IsApi = isApi;
                        return PartialView("_Form", product);
                    }
                }
            }
        }
        else
        {
            if (id != null && id != Guid.Empty)
            {
                var existing = _context.Products
                    .FirstOrDefault(p => p.ProductID == id.ToString() && p.SubCode == subCode);

                if (existing != null)
                {
                    model.Id = Guid.Parse(existing.ProductID ?? Guid.Empty.ToString());
                    model.Name = existing.Name;
                    model.Sku = existing.Sku;
                    model.Description = existing.Description;
                    model.Uom = existing.Uom;
                    model.Weight = existing.Weight;
                    model.Volume = existing.Volume ?? 0;
                    model.Price = existing.Price ?? 0;
                    model.Width = existing.Width ?? 0;
                    model.Length = existing.Length ?? 0;
                    model.Height = existing.Height ?? 0;
                    model.Cbm = existing.Cbm ?? 0;
                }
            }
        }

        return PartialView("_Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Form(ProductStore model)
    {
        if (model.IsApi)
        {
            bool ok;
            JsonElement json = default;

            if (model.Id == Guid.Empty)
                (ok, json) = await _apiService.SendRequestAsync(HttpMethod.Post, "/order/api/web/v1/product", model);
            else
                (ok, json) = await _apiService.SendRequestAsync(HttpMethod.Patch, $"/order/api/web/v1/product/{model.Id}", model);

            if (!ok)
                return BadRequest(new { success = false, message = "Gagal kirim ke API CREATE/UPDATE product", detail = json });

            return Json(new { success = true, data = json });
        }
        else
        {
            if (model.Id == Guid.Empty)
            {
                _context.Products.Add(new ProductTable
                {
                    ProductID = Guid.NewGuid().ToString(),
                    SubCode = model.SubCode,
                    Name = model.Name,
                    Sku = model.Sku,
                    Description = model.Description,
                    Uom = model.Uom,
                    Weight = model.Weight,
                    Volume = model.Volume,
                    Price = model.Price,
                    Width = model.Width,
                    Length = model.Length,
                    Height = model.Height,
                    Cbm = model.Cbm,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                var existing = _context.Products
                    .FirstOrDefault(p => p.ProductID == model.Id.ToString() && p.SubCode == model.SubCode);

                if (existing == null)
                    return NotFound(new { success = false, message = "Product tidak ditemukan" });

                existing.Name = model.Name;
                existing.Sku = existing.Sku;
                existing.Description = model.Description;
                existing.Uom = model.Uom;
                existing.Weight = model.Weight;
                existing.Volume = model.Volume;
                existing.Price = model.Price;
                existing.Width = model.Width;
                existing.Length = model.Length;
                existing.Height = model.Height;
                existing.Cbm = model.Cbm;
                existing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id, string subCode = "")
    {
        bool isApi = IsApiCustomer(subCode);

        if (isApi)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

            var response = await _httpClient.DeleteAsync($"{_apiSettings.BaseUrl}/order/api/web/v1/product/{id}");
            var apiResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false, message = $"API Error: {response.StatusCode}", detail = apiResponse });

            return Json(new { success = true, data = apiResponse });
        }
        else
        {
            var product = _context.Products
                .FirstOrDefault(p => p.ProductID == id.ToString() && p.SubCode == subCode);

            if (product == null)
                return NotFound(new { success = false, message = "Product tidak ditemukan" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // =====================
    // PRODUCT CATEGORY
    // =====================

    [ActionName("Category")]
    public async Task<IActionResult> Category(string search = "", int page = 1, int limit = 1000)
    {
        var categories = await FetchProductCategoryFromApi(page, limit);
        return View("Category", categories);
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
                return BadRequest(new { success = false, message = "Gagal kirim ke API show category", detail = json });

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
            "order/api/web/v1/product-category",
            model
        );

        if (!ok)
            return BadRequest(new { success = false, message = "Gagal kirim ke API store product category", detail = json });

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
            return BadRequest(new { success = false, message = "Gagal kirim ke API edit product category", detail = json });

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
            return BadRequest(new { success = false, message = "Gagal kirim ke API delete category", detail = json });

        return Json(new { success = true, data = json });
    }

    // =====================
    // PRODUCT TYPE
    // =====================

    [ActionName("Type")]
    public async Task<IActionResult> Type(string search = "", int page = 1, int limit = 1000)
    {
        var types = await FetchProductTypeFromApi(page, limit);
        return View("Type", types);
    }

    public async Task<IActionResult> FormType(Guid? id)
    {
        var model = new ProductTypeStore();

        var categories = await FetchProductCategoryFromApi(1, 1000);
        ViewBag.CategoryList = categories.Select(c => new SelectListItem
        {
            Value = c.id,
            Text = c.name
        }).ToList();

        if (id != null)
        {
            var data = await FetchProductTypeByID(id);

            if (data == null)
                return BadRequest(new { success = false, message = "Data kosong dari API" });

            model.id = data.id;
            model.name = data.name;
            model.product_category_id = data.product_category?.id;
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
            "order/api/web/v1/product-type",
            model
        );

        if (!ok)
            return BadRequest(new { success = false, message = "Gagal kirim ke API store product type", detail = json });

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
            return BadRequest(new { success = false, message = "Gagal kirim ke API edit product type", detail = json });

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
            return BadRequest(new { success = false, message = "Gagal kirim ke API delete type", detail = json });

        return Json(new { success = true, data = json });
    }

    // =====================
    // UOM (existing, tidak diubah)
    // =====================

    [ActionName("Uom")]
    public async Task<IActionResult> Uom(string search = "", int page = 1, int limit = 10)
    {
        bool ok;
        JsonElement json = default;

        (ok, json) = await _apiService.SendRequestAsync(
            HttpMethod.Get,
            $"order/api/web/v1/product-type?limit={limit}&page={page}",
            new { }
        );

        if (!ok)
            return BadRequest(new { success = false, message = "Gagal kirim ke API get product type", detail = json });

        var types = json
            .GetProperty("data")
            .GetProperty("paginated_result")
            .Deserialize<List<ProductType>>() ?? new List<ProductType>();

        return View("Type", types);
    }
}

//using DocumentFormat.OpenXml.Math;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.Extensions.Options;
//using System.CodeDom;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using TMSBilling.Data;
//using TMSBilling.Filters;
//using TMSBilling.Models;
//using TMSBilling.Models.McEasyApiModel;
//using TMSBilling.Services;


//[SessionAuthorize]
//public class ProductController : Controller
//{
//    private readonly HttpClient _httpClient;
//    private readonly ApiSettings _apiSettings;
//    private readonly ApiService _apiService;
//    private readonly AppDbContext _context;

//    public ProductController(AppDbContext context, HttpClient httpClient, IOptions<ApiSettings> apiSettings, ApiService apiService)
//    {
//        _context = context;
//        _httpClient = httpClient;
//        _apiSettings = apiSettings.Value;
//        _apiService = apiService;
//    }
//    private async Task<List<Product>> FetchProductsFromApi(int page = 1, int limit = 1000, string search = "")
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Get,
//            $"order/api/web/v1/product?limit={limit}&page={page}&search={search}"
//        );

//        if (!ok)
//            throw new Exception("Gagal kirim ke API get product");

//        var products = json
//            .GetProperty("data")
//            .GetProperty("paginated_result")
//            .Deserialize<List<Product>>() ?? new List<Product>();

//        return products;
//    }
//    private async Task<List<ProductCategory>> FetchProductCategoryFromApi(int page = 1, int limit = 1000) { 
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//                HttpMethod.Get,
//                $"order/api/web/v1/product-category?limit={limit}&page={page}"
//        );

//        if (!ok)
//            throw new Exception("Failed to get product category");

//        var categories = json
//            .GetProperty("data")
//            .GetProperty("paginated_result")
//            .Deserialize<List<ProductCategory>>() ?? new List<ProductCategory>();

//        return categories;
//    }
//    private async Task<List<ProductType>> FetchProductTypeFromApi(int page = 1, int limit = 1000)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Get,
//            $"order/api/web/v1/product-type?limit={limit}&page={page}"
//        );

//        if (!ok) throw new Exception("Failed to get product type");

//        var types = json
//            .GetProperty ("data")
//            .GetProperty ("paginated_result")
//            .Deserialize<List<ProductType>>() ?? new List<ProductType>();

//        return types;

//    }

//    private async Task<ProductType> FecthProductTypeByID(Guid? id)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//                    HttpMethod.Get,
//                    $"order/api/web/v1/product-type/{id}",
//                    new { }
//                );

//        if (!ok) throw new Exception("Failed to get product type by id");


//        var data = json.GetProperty("data")
//                       .Deserialize<ProductType>();

//        return data;
//    }
//    //private void SyncProductsToDatabase(List<Product> products)
//    //{
//    //    var existingIds = _context.Products
//    //        .Select(p => p.ProductID)
//    //        .ToHashSet();

//    //    var newProducts = new List<ProductTable>();

//    //    foreach (var p in products)
//    //    {
//    //        if (!existingIds.Contains(p.id))
//    //        {
//    //            var newProduct = new ProductTable
//    //            {
//    //                ProductID = p.id,
//    //                ProductTypeID = p.product_type?.id?.ToString(),
//    //                ProductTypeName = p.product_type?.name,
//    //                ProductCategoryID = p.product_type?.product_category?.id?.ToString(),
//    //                ProductCategoryName = p.product_type?.product_category?.name,
//    //                Name = p.name,
//    //                Sku = p.sku,
//    //                Description = p.description,
//    //                Uom = p.uom,
//    //                Weight = p.weight,
//    //                Volume = p.volume,
//    //                Price = p.price,
//    //                CreatedAt = DateTime.Now
//    //            };

//    //            newProducts.Add(newProduct);
//    //        }
//    //    }

//    //    if (newProducts.Any())
//    //    {
//    //        _context.Products.AddRange(newProducts);
//    //        _context.SaveChanges();
//    //    }
//    //}

//    // Sync dari API — sekarang terima custCode
//    private void SyncProductsToDatabase(List<Product> products, string custCode)
//    {
//        var existingIds = _context.Products
//            .Where(p => p.CustCode == custCode)
//            .Select(p => p.ProductID)
//            .ToHashSet();

//        var newProducts = new List<ProductTable>();

//        foreach (var p in products)
//        {
//            if (!existingIds.Contains(p.id))
//            {
//                newProducts.Add(new ProductTable
//                {
//                    ProductID = p.id,
//                    CustCode = custCode, // <-- tambah ini
//                    ProductTypeID = p.product_type?.id?.ToString(),
//                    ProductTypeName = p.product_type?.name,
//                    ProductCategoryID = p.product_type?.product_category?.id?.ToString(),
//                    ProductCategoryName = p.product_type?.product_category?.name,
//                    Name = p.name,
//                    Sku = p.sku,
//                    Description = p.description,
//                    Uom = p.uom,
//                    Weight = p.weight,
//                    Volume = p.volume,
//                    Price = p.price,
//                    CreatedAt = DateTime.Now
//                });
//            }
//        }

//        if (newProducts.Any())
//        {
//            _context.Products.AddRange(newProducts);
//            _context.SaveChanges();
//        }
//    }

//    //[ActionName("Index")]
//    //public async Task<IActionResult> Index(string search = "", int page = 1, int limit = 1000)
//    //{
//    //    try
//    //    {
//    //        var productsFromApi = await FetchProductsFromApi(page, limit, search);
//    //        SyncProductsToDatabase(productsFromApi);
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        Console.WriteLine($"Error sync data: {ex.Message}");
//    //    }
//    //    var products = await FetchProductsFromApi(page, limit, search);

//    //    return View(products);
//    //}

//    [ActionName("Index")]
//    public async Task<IActionResult> Index(string custCode = "", string search = "", int page = 1, int limit = 1000)
//    {
//        ViewBag.CustomerGroups = GetCustomerGroupList();
//        ViewBag.SelectedCust = custCode;

//        if (string.IsNullOrEmpty(custCode))
//            return View(new List<Product>());

//        bool isApi = IsApiCustomer(custCode);
//        ViewBag.IsApi = isApi;

//        if (isApi)
//        {
//            try
//            {
//                var fromApi = await FetchProductsFromApi(page, limit, search);
//                SyncProductsToDatabase(fromApi, custCode); // <-- pass custCode
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error sync: {ex.Message}");
//            }

//            var products = await FetchProductsFromApi(page, limit, search);
//            return View(products);
//        }
//        else
//        {
//            var products = _context.Products
//                .Where(p => p.CustCode == custCode &&
//                            (string.IsNullOrEmpty(search) || p.Name.Contains(search)))
//                .Skip((page - 1) * limit)
//                .Take(limit)
//                .Select(p => new Product
//                {
//                    id = p.ProductID,
//                    name = p.Name,
//                    sku = p.Sku,
//                    description = p.Description,
//                    uom = p.Uom,
//                    weight = p.Weight,
//                    volume = p.Volume,
//                    price = p.Price
//                })
//                .ToList();

//            return View(products);
//        }
//    }

//    public async Task<IActionResult> Form(Guid? id)
//    {
//        var model = new ProductStore();
//        bool ok;
//        JsonElement json = default;


//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Get,
//            $"order/api/web/v1/product-type?limit={100}&page={1}",
//            new { }
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API Get Type",
//                detail = json
//            });
//        }

//        var types = json.GetProperty("data")
//            .GetProperty("paginated_result")
//            .Deserialize<List<ProductType>>() ?? new List<ProductType>();

//        ViewBag.TypeList = types.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
//        {
//            Value = c.id.ToString(),
//            Text = c.name
//        }).ToList();
//        return PartialView("_Form", model);
//    }

//    [HttpPost]
//    public async Task<IActionResult> Form(ProductStore model)
//    {
//        var jsonContent = new StringContent(
//            JsonSerializer.Serialize(model),
//            Encoding.UTF8,
//            "application/json"
//        );

//        bool ok;
//        JsonElement json = default;

//        _httpClient.DefaultRequestHeaders.Authorization =
//            new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

//        if (model.Id == Guid.Empty)
//        {
//            (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Post,
//            $"/order/api/web/v1/product",
//            model
//        );

//        }
//        else
//        {
//            (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Patch,
//            $"/order/api/web/v1/product/{model.Id}",
//            model
//            );
//        }

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API CREATE product",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }

//    [HttpDelete]
//    public async Task<IActionResult> Delete(Guid id)
//    {
//        _httpClient.DefaultRequestHeaders.Authorization =
//            new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));
//        var response = await _httpClient.DeleteAsync($"{_apiSettings.BaseUrl}/order/api/web/v1/product/{id}");
//        var apiResponse = await response.Content.ReadAsStringAsync();
//        if (!response.IsSuccessStatusCode)
//        {
//            return Json(new { success = false, message = $"API Error: {response.StatusCode}", detail = apiResponse });
//        }
//        return Json(new { success = true, data = apiResponse });
//    }

//    [ActionName("Category")]
//    public async Task<IActionResult> Category(string search = "", int page = 1, int limit = 1000)
//    {
//        var categories = await FetchProductCategoryFromApi(page, limit);
//        return View("Category", categories);
//    }

//    public async Task<IActionResult> FormCategory(Guid? id)
//    {
//        var model = new ProductCategoryStore();
//        bool ok;
//        JsonElement json = default;

//        if (id != null)
//        {

//            (ok, json) = await _apiService.SendRequestAsync(
//                HttpMethod.Get,
//                $"order/api/web/v1/product-category/{id}",
//                new { }
//            );

//            if (!ok)
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "Gagal kirim ke API show category",
//                    detail = json
//                });
//            }

//            model = json.GetProperty("data").Deserialize<ProductCategoryStore>() ?? new ProductCategoryStore();
//        }

//        return PartialView("_FormCategory", model);
//    }

//    [HttpPost]
//    public async Task<IActionResult> FormCategory(ProductCategoryStore model)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Post,
//            $"order/api/web/v1/product-category",
//            model
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API store product category",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }

//    [HttpPost]
//    public async Task<IActionResult> EditCategory(ProductCategoryStore model)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Patch,
//            $"order/api/web/v1/product-category/{model.Id}",
//            model
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API edit product category",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }

//    public async Task<IActionResult> DeleteCategory(Guid id)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Delete,
//            $"order/api/web/v1/product-category/{id}",
//            new { }
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API delete category",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }

//    [ActionName("Type")]
//    public async Task<IActionResult> Type(string search = "", int page = 1, int limit = 1000)
//    {
//        var types = await FetchProductTypeFromApi(page, limit);
//        return View("Type", types);
//    }

//    public async Task<IActionResult> FormType(Guid? id)
//    {
//        var model = new ProductTypeStore();
//        bool ok;
//        JsonElement json = default;


//        var categorys = await FetchProductCategoryFromApi(1, 1000);

//        ViewBag.CategoryList = categorys.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
//        {
//            Value = c.id,
//            Text = c.name
//        }).ToList();


//        if (id != null)
//        {
//            var data = await FecthProductTypeByID(id);

//            if (data == null)
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "Data kosong dari API"
//                });
//            }

//            model.id = data.id;
//            model.name = data.name;
//            model.product_category_id = data.product_category?.id;
//        }

//        return PartialView("_FormType", model);
//    }

//    [HttpPost]
//    public async Task<IActionResult> FormType(ProductTypeStore model)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Post,
//            $"order/api/web/v1/product-type",
//            model
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API store product type",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }

//    [HttpPost]
//    public async Task<IActionResult> EditType(ProductTypeStore model)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Patch,
//            $"order/api/web/v1/product-type/{model.id}",
//            model
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API edit product type",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }


//    public async Task<IActionResult> DeleteType(Guid id)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Delete,
//            $"order/api/web/v1/product-type/{id}",
//            new { }
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API delete type",
//                detail = json
//            });
//        }

//        return Json(new { success = true, data = json });
//    }


//    [ActionName("Uom")]
//    public async Task<IActionResult> Uom(string search = "", int page = 1, int limit = 10)
//    {
//        bool ok;
//        JsonElement json = default;

//        (ok, json) = await _apiService.SendRequestAsync(
//            HttpMethod.Get,
//            $"order/api/web/v1/product-type?limit={limit}&page={page}",
//            new { }
//        );

//        if (!ok)
//        {
//            return BadRequest(new
//            {
//                success = false,
//                message = "Gagal kirim ke API get product type",
//                detail = json
//            });
//        }

//        var types = json
//        .GetProperty("data")
//        .GetProperty("paginated_result")
//        .Deserialize<List<ProductType>>() ?? new List<ProductType>();

//        return View("Type", types);
//    }


//    private bool IsApiCustomer(string custCode)
//    {
//        var group = _context.CustomerGroups
//            .FirstOrDefault(g => g.CUST_CODE == custCode);
//        return group?.API_FLAG == 1;
//    }

//    // Helper: ambil list CustomerGroup untuk dropdown
//    private List<SelectListItem> GetCustomerGroupList()
//    {
//        return _context.CustomerGroups
//            .Select(g => new SelectListItem
//            {
//                Value = g.CUST_CODE,
//                Text = g.CUST_CODE
//            })
//            .ToList();
//    }
//}

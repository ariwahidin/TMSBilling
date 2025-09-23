using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;

[SessionAuthorize]
public class CustomerGroupController : Controller
{
    private readonly AppDbContext _context;
    private readonly ApiService _apiService;

    public CustomerGroupController(AppDbContext context, ApiService apiService)
    {
        _context = context;
        _apiService = apiService;
    }

    public IActionResult Index()
    {
        var list = _context.CustomerGroups.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        var customerList = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CUST_CODE,
                Text = c.CUST_CODE,
            }).ToList();

        ViewBag.CustomerList = customerList;

        if (id == null)
            return PartialView("_Form", new CustomerGroup());

        var data = _context.CustomerGroups.Find(id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public async Task<IActionResult> Form(CustomerGroup model)
    {
        if (!ModelState.IsValid) return BadRequest();

        if (model.ID == 0)
        {
            bool exists = _context.CustomerGroups.Any(v => v.SUB_CODE == model.SUB_CODE);
            if (exists)
            {
                return BadRequest(new
                {
                    message = "WMS code already exists"
                });
            }

            var customer = _context.Customers.FirstOrDefault(v => v.CUST_CODE == model.CUST_CODE);
            if (customer?.API_FLAG == 1) {
                var payload = new
                {
                    name = model.SUB_CODE,
                };
                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Post,
                    "order/api/web/v1/customer",
                    payload
                );
                if (!ok)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Customer",
                        detail = json
                    });
                }
                var id = json.GetProperty("data").GetProperty("id").GetString();
                model.MCEASY_CUST_ID = id;
            }

            
            model.ENTRY_USER = HttpContext.Session.GetString("username") ?? "System";
            model.ENTRY_DATE = DateTime.Now;
            _context.CustomerGroups.Add(model);
        }
        else
        {
            bool duplicate = _context.CustomerGroups.Any(v => v.SUB_CODE == model.SUB_CODE && v.ID != model.ID);
            if (duplicate)
            {
                return BadRequest(new
                {
                    message = "WMS code already exists on another record"
                });
            }

            var data = _context.CustomerGroups.Find(model.ID);
            if (data == null) return NotFound();

            data.SUB_CODE = model.SUB_CODE;
            data.CUST_CODE = model.CUST_CODE;
            data.UPDATE_USER = HttpContext.Session.GetString("username") ?? "System";
            data.UPDATE_DATE = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.CustomerGroups.Find(id);
        if (data == null) return NotFound();

        _context.CustomerGroups.Remove(data);
        _context.SaveChanges();

        return Ok();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

[SessionAuthorize]
public class CustomerGroupController : Controller
{
    private readonly AppDbContext _context;

    public CustomerGroupController(AppDbContext context)
    {
        _context = context;
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
    public IActionResult Form(CustomerGroup model)
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

        _context.SaveChanges();
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;


[SessionAuthorize]
public class ConsigneeController : Controller
{
    private readonly AppDbContext _context;

    public ConsigneeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var list = _context.Consignees.ToList();
        return View(list);
    }

    public IActionResult Form(int? id)
    {
        var subCodeList = _context.CustomerGroups
            .Select(g => new SelectListItem
            {
                Value = g.SUB_CODE,
                Text = g.SUB_CODE
            }).Distinct().ToList();

        ViewBag.SubCodeList = subCodeList;

        if (id == null)
        {             
            //return PartialView("_Form", new Consignee());
            return PartialView("_Form", new Consignee
            {
                CNEE_CODE = string.Empty,
                SUB_CODE = string.Empty
            });
        }

        var data = _context.Consignees.FirstOrDefault(c => c.ID == id);
        return PartialView("_Form", data);
    }

    [HttpPost]
    public IActionResult Form(Consignee model)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var existing = _context.Consignees.FirstOrDefault(c => c.ID == model.ID);

        if (existing == null)
        {
            model.ENTRY_DATE = DateTime.Now;
            _context.Consignees.Add(model);
        }
        else
        {
            // Hanya update properti yang boleh diubah
            existing.CNEE_CODE = model.CNEE_CODE;
            existing.CNEE_NAME = model.CNEE_NAME;
            existing.CNEE_ADDR1 = model.CNEE_ADDR1;
            existing.CNEE_ADDR2 = model.CNEE_ADDR2;
            existing.CNEE_ADDR3 = model.CNEE_ADDR3;
            existing.CNEE_ADDR4 = model.CNEE_ADDR4;
            existing.CNEE_TEL = model.CNEE_TEL;
            existing.CNEE_FAX = model.CNEE_FAX;
            existing.CNEE_PIC = model.CNEE_PIC;
            existing.TAX_REG_NO = model.TAX_REG_NO;
            existing.ACTIVE_FLAG = model.ACTIVE_FLAG;
            existing.SUB_CODE = model.SUB_CODE;
            existing.UPDATE_USER = model.UPDATE_USER;
            existing.UPDATE_DATE = DateTime.Now;

            // Tidak menyentuh existing.ID (identity)
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var data = _context.Consignees.FirstOrDefault(c => c.ID == id);
        if (data == null) return NotFound();

        _context.Consignees.Remove(data);
        _context.SaveChanges();

        return Ok();
    }
}

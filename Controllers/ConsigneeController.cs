using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using TMSBilling.Controllers;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Services;


[SessionAuthorize]
public class ConsigneeController : Controller
{
    private readonly AppDbContext _context;
    private readonly ApiService _apiService;


    public ConsigneeController(ApiService apiService, AppDbContext context)
    {
        _apiService = apiService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var list = _context.Consignees
            .OrderByDescending(o => o.ID)
            .ToList();


        var graphqlVariables = new
        {
            filter = new {
                ownership = "Customer"
            }
        };


        var (ok, result) = await _apiService.ExecuteGraphQLAsync(
            @"query Geofences($filter: GetGeofencesFilter) {
                  geofences(filter: $filter) {
                    geofences {
                      data {
                        geofenceId
                        companyId
                        customerId
                        fenceName
                        type
                        polyData
                        circData
                        address
                        addressDetail
                        province
                        city
                        postalCode
                        category
                        contactName
                        phoneNo
                        isGarage
                        isServiceLoc
                        isBillingAddr
                        isDepot
                        isAlert
                        serviceStart
                        serviceEnd
                        breakStart
                        breakEnd
                        serviceLocType
                        customerName
                        createdOn
                        hasRelation
                        contacts {
                          contactId
                          name
                          phoneNo
                          sendWhatsapp
                        }
                      }
                      pagination {
                        total
                        page
                        show
                        startCursor
                        endCursor
                        actualTotal
                      }
                    }
                  }
                }",
            graphqlVariables,
            "Geofences"
        );

        if (!ok)
        {
            return BadRequest(new
            {
                success = false,
                message = "GraphQL get geofences request failed",
                detail = result.ToString()
            });
        }

        var data = result.GetProperty("data")
                         .GetProperty("geofences")
                         .GetProperty("geofences")
                         .GetProperty("data")
                         .Deserialize<List<Geofence>>() ?? new List<Geofence>();
        foreach (var p in data)
        {
            var existGeofence = _context.Geofences.FirstOrDefault(o => o.GeofenceId == p.geofenceId);
            if (existGeofence == null) {

                var ng = new GeofenceTable();

                if (p.circData != null) { 
                    var match = Regex.Match(p.circData, @"<\(\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*\)\s*,\s*(\d+)\s*>");

                    if (match.Success)
                    {
                        ng.Lat = $"{match.Groups[1].Value}";
                        ng.Long = $"{match.Groups[2].Value}";
                        ng.Cordinates = $"({ng.Lat},{ng.Long})";
                        ng.Radius = $"{match.Groups[3].Value}";
                    }
                }
                
                ng.GeofenceId = p.geofenceId;
                ng.CompanyId = p.companyId;
                ng.CustomerId = p.customerId;
                ng.FenceName = p.fenceName;
                ng.Type = p.type;
                ng.PolyData = p.polyData;
                ng.CircData = p.circData;
                ng.Address = p.address;
                ng.AddressDetail = p.addressDetail;
                ng.Province = p.province;
                ng.City = p.city;
                ng.PostalCode = p.postalCode;
                ng.Category = p.category;
                ng.ContactName = p.contactName;
                ng.PhoneNo = p.phoneNo;
                ng.IsGarage = p.isGarage;
                ng.IsServiceLoc = p.isServiceLoc;
                ng.IsBillingAddr = p.isBillingAddr;
                ng.IsDepot = p.isDepot;
                ng.IsAlert = p.isAlert;
                ng.ServiceStart = p.serviceStart;
                ng.ServiceEnd = p.serviceEnd;
                ng.BreakStart = p.breakStart;
                ng.BreakEnd = p.breakEnd;
                ng.ServiceLocType = p.serviceLocType;
                ng.CustomerName = p.customerName;
                ng.HasRelation = p.hasRelation;
                ng.CreatedAt = DateTime.Now;
                ng.UpdatedAt = DateTime.Now;
                

                _context.Geofences.Add(ng);
                _context.SaveChanges();
            }

            

        }

        return View(list);
    }

    public IActionResult OptionCustomerGroup() {
        var list = _context.CustomerGroups.ToList();
        return PartialView("_OptionCustomer", list);
    }

    [HttpGet]
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
            // Cek apakah CNEE_CODE sudah digunakan oleh record lain
            bool codeExists = _context.Consignees.Any(c => c.CNEE_CODE == model.CNEE_CODE);
            if (codeExists)
            {
                return BadRequest(new { message = "Cnee Code is already in use. Please choose a different one." });
            }

            model.ENTRY_DATE = DateTime.Now;
            model.ENTRY_USER = HttpContext.Session.GetString("username");
            _context.Consignees.Add(model);
        }
        else
        {
            // Update existing record
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
            existing.UPDATE_USER = HttpContext.Session.GetString("username");
            existing.UPDATE_DATE = DateTime.Now;
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

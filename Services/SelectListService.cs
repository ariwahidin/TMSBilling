// Services/SelectListService.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using TMSBilling.Models; // namespace model kamu
using TMSBilling.Data;   // atau dimana _context didefinisikan

public class SelectListService
{
    private readonly AppDbContext _context;

    public SelectListService(AppDbContext context)
    {
        _context = context;
    }

    public List<SelectListItem> getCustomerMains()
    {
        return _context.CustomerMains
            .Select(c => new SelectListItem
            {
                Value = c.MAIN_CUST,
                Text = c.MAIN_CUST,
            }).ToList();
    }

    public List<SelectListItem> getCustomers()
    {
        return _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CUST_CODE,
                Text = c.CUST_CODE,
            }).ToList();
    }

    public List<SelectListItem> getCustomerGroup()
    {
        return _context.CustomerGroups
            .Select(c => new SelectListItem
            {
                Value = c.SUB_CODE,
                Text = c.SUB_CODE,
            }).ToList();
    }

    public List<SelectListItem> GetVendors()
    {
        return _context.Vendors
            .Select(v => new SelectListItem
            {
                Value = v.SUP_CODE,
                Text = v.SUP_CODE
            }).ToList();
    }

    public List<SelectListItem> GetCurrency()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "IDR", Text = "IDR" },
            new SelectListItem { Value = "USD", Text = "USD" }
        };
    }

    public List<SelectListItem> GetOrigins()
    {
        return _context.Origins
            .Select(x => new SelectListItem { Value = x.origin_code, Text = x.origin_code })
            .ToList();
    }

    public List<SelectListItem> GetDestinations()
    {
        return _context.Destinations
            .Select(x => new SelectListItem { Value = x.destination_code, Text = x.destination_code })
            .ToList();
    }

    public List<SelectListItem> GetServiceTypes()
    {
        return _context.ServiceTypes
            .Select(x => new SelectListItem { Value = x.serv_name, Text = x.serv_name })
            .ToList();
    }

    public List<SelectListItem> GetServiceModas()
    {
        return _context.ServiceModas
            .Select(x => new SelectListItem { Value = x.moda_name, Text = x.moda_name })
            .ToList();
    }

    public List<SelectListItem> GetTruckSizes()
    {
        return _context.TruckSizes
            .Select(x => new SelectListItem { Value = x.trucksize_code, Text = x.trucksize_code })
            .ToList();
    }

    public List<SelectListItem> GetChargeUoms()
    {
        return _context.ChargeUoms
            .Select(x => new SelectListItem { Value = x.charge_name, Text = x.charge_name })
            .ToList();
    }

    public List<SelectListItem> GetWarehouse()
    {
        return _context.Warehouses
            .Select(w => new SelectListItem { Value = w.wh_code, Text = w.wh_code })
            .ToList();
    }

    public List<SelectListItem> GetConsignee()
    {
        return _context.Consignees
            .Select(c => new SelectListItem { Value = c.CNEE_CODE, Text = c.CNEE_CODE })
            .ToList();
    }

    public List<SelectListItem> GetYesNo()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "0", Text = "No" },
            new SelectListItem { Value = "1", Text = "Yes" }
        };
    }

    public List<SelectListItem> GeofenceCategory()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "Consignee", Text = "Consignee" },
            new SelectListItem { Value = "Origin", Text = "Origin" },
            new SelectListItem { Value = "Destination", Text = "Destination" }
        };
    }

    public List<SelectListItem> PackingTypeOption()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "semi wooden", Text = "Semi Wooden" },
            new SelectListItem { Value = "none", Text = "None" },
        };
    }

    public List<SelectListItem> getCustomerGroupGeofence()
    {
        return _context.CustomerGroups
            .Where(c => c.MCEASY_CUST_ID != null)
            .Select(c => new SelectListItem
            {
                Value = c.MCEASY_CUST_ID,
                Text = c.SUB_CODE,
            }).ToList();
    }

    public List<SelectListItem> GetAreaGroup()
    {
        return _context.AreaGroups
            .Select(v => new SelectListItem
            {
                Value = v.area_name,
                Text = v.area_name
            }).ToList();
    }

    public List<SelectListItem> GetArea()
    {
        return _context.Destinations
            .Select(v => v.destination_code)        // ambil kolom destination_code
            .Distinct()                            // bikin unik
            .Select(area => new SelectListItem
            {
                Value = area,
                Text = area
            })
            .ToList();
    }


    public List<SelectListItem> GetStartingPoint()
    {
        return _context.Geofences
            .Where(c =>  c.IsGarage == true)
            .Select(c => new SelectListItem
            {
                Value = c.GeofenceId.ToString(),
                Text = c.FenceName,
            }).ToList();
    }


    public List<SelectListItem> GetConsigneeByCustomer(string customer)
    {
        return _context.Consignees
            .Select(c => new SelectListItem { Value = c.CNEE_CODE, Text = c.CNEE_CODE })
            .ToList();
    }

}

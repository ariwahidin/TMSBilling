﻿using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class ServiceTypeController : Controller
    {
        private readonly AppDbContext _context;

        public ServiceTypeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.ServiceTypes.ToList();
            return View(data);
        }

        public IActionResult Form(int? id)
        {
            ServiceType model = id == null || id == 0
                ? new ServiceType { serv_name  = string.Empty}
                : _context.ServiceTypes.FirstOrDefault(x => x.id_seq == id) ?? new ServiceType { serv_name = string.Empty };

            return PartialView("_Form", model);
        }

        [HttpPost]
        public IActionResult Save(ServiceType model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Invalid input data"
                });

            if (model.id_seq == 0)
            {

                bool exists = _context.ServiceTypes.Any(v => v.serv_name == model.serv_name);
                if (exists)
                {
                    return BadRequest(new
                    {
                        message = "Service type already exists"
                    });
                }

                model.entry_date = DateTime.Now;
                model.entry_user = HttpContext.Session.GetString("username") ?? "System";
                _context.ServiceTypes.Add(model);
            }
            else
            {
                bool duplicate = _context.ServiceTypes.Any(v => v.serv_name == model.serv_name && v.id_seq != model.id_seq);
                if (duplicate)
                {

                    return BadRequest(new
                    {
                        message = "Service type already exists on another record"
                    });
                }

                var existing = _context.ServiceTypes.FirstOrDefault(x => x.id_seq == model.id_seq);
                if (existing == null) return NotFound();

                existing.serv_name = model.serv_name;
                existing.entry_date = DateTime.Now;
                existing.entry_user = HttpContext.Session.GetString("username") ?? "System";
            }

            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _context.ServiceTypes.Find(id);
            if (item == null) return NotFound();

            _context.ServiceTypes.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}

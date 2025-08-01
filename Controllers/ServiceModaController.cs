﻿using Microsoft.AspNetCore.Mvc;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class ServiceModaController : Controller
    {
        private readonly AppDbContext _context;

        public ServiceModaController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.ServiceModas.ToList();
            return View(data);
        }

        // Load Partial View (_Form.cshtml)
        public IActionResult Form(int? id)
        {
            ServiceModa model;

            if (id == null || id == 0)
                model = new ServiceModa {
                    moda_name = string.Empty,
                };
            else
                model = _context.ServiceModas.FirstOrDefault(m => m.id_seq == id) ?? new ServiceModa { 
                    moda_name = string.Empty,
                };

            return PartialView("_Form", model);
        }

        [HttpPost]
        public IActionResult Save(ServiceModa model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Invalid input data"
                });

            if (model.id_seq == 0)
            {
                bool exists = _context.ServiceModas.Any(v => v.moda_name == model.moda_name);
                if (exists)
                {
                    return BadRequest(new
                    {
                        message = "Moda already exists"
                    });
                }

                model.entry_date = DateTime.Now;
                model.entry_user = User.Identity?.Name ?? "system";
                _context.ServiceModas.Add(model);
            }
            else
            {
                bool duplicate = _context.ServiceModas.Any(v => v.moda_name == model.moda_name && v.id_seq != model.id_seq);
                if (duplicate)
                {

                    return BadRequest(new
                    {
                        message = "Moda already exists on another record"
                    });
                }

                var existing = _context.ServiceModas.FirstOrDefault(x => x.id_seq == model.id_seq);
                if (existing == null) return NotFound();

                existing.moda_name = model.moda_name;
                existing.entry_date = DateTime.Now;
                existing.entry_user = HttpContext.Session.GetString("username") ?? "System";
            }

            _context.SaveChanges();
            return Ok();
            //return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = _context.ServiceModas.Find(id);
            if (item == null) return NotFound();

            _context.ServiceModas.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}

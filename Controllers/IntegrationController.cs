//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;
//using TMSBilling.Filters;
//using TMSBilling.Models;
//using TMSBilling.Repositories;
//using TMSBilling.Services;
//using TMSBilling.Services.Integration;

//namespace TMSBilling.Controllers
//{
//    //[Authorize]
//    [SessionAuthorize]
//    public class IntegrationController : Controller
//    {
//        private readonly IIntegrationRepository _repo;
//        private readonly IIntegrationDispatcher _dispatcher;
//        private readonly IIntegrationScheduler _scheduler;
//        private readonly IQueryValidator _queryValidator;
//        private readonly IQueryExecutor _queryExecutor;
//        private readonly ILogger<IntegrationController> _logger;
//        private readonly IEncryptionService _encryptionService;

//        public IntegrationController(
//            IIntegrationRepository repo,
//            IIntegrationDispatcher dispatcher,
//            IIntegrationScheduler scheduler,
//            IQueryValidator queryValidator,
//            IQueryExecutor queryExecutor,
//            ILogger<IntegrationController> logger,
//            IEncryptionService encryption)
//        {
//            _repo = repo;
//            _dispatcher = dispatcher;
//            _scheduler = scheduler;
//            _queryValidator = queryValidator;
//            _queryExecutor = queryExecutor;
//            _logger = logger;
//            _encryptionService = encryption;

//        }

//        // ── LIST ──────────────────────────────────────────────────────────────

//        public async Task<IActionResult> Index()
//        {
//            var integrations = await _repo.GetAllAsync();
//            return View(integrations);
//        }

//        // ── CREATE ────────────────────────────────────────────────────────────

//        public IActionResult Create()
//        {
//            return View("CreateEdit", new IntegrationFormViewModel());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(IntegrationFormViewModel vm)
//        {
//            if (!ModelState.IsValid) return View(vm);

//            // Validate query if source_type = query
//            if (vm.Integration.SourceType == "query" && !string.IsNullOrWhiteSpace(vm.Integration.Query))
//            {
//                var (isValid, error) = _queryValidator.Validate(vm.Integration.Query);
//                if (!isValid)
//                {
//                    ModelState.AddModelError("Integration.Query", error!);
//                    return View(vm);
//                }
//            }

//            var integration = await _repo.CreateAsync(vm.Integration);

//            if (vm.Connection != null)
//            {
//                vm.Connection.IntegrationId = integration.Id;
//                if (vm.Connection.Password != null)
//                {
//                    vm.Connection.Password = _encryptionService.Encrypt(vm.Connection.Password);
//                }
//                await _repo.SaveConnectionAsync(vm.Connection);
//            }

//            if (vm.Recipients != null)
//                await _repo.SaveRecipientsAsync(integration.Id, vm.Recipients);

//            // Reload scheduler if scheduled
//            if (integration.Timing == "scheduled")
//                await _scheduler.ReloadIntegrationAsync(integration.Id);

//            TempData["Success"] = "Integrasi berhasil dibuat.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── EDIT ──────────────────────────────────────────────────────────────

//        public async Task<IActionResult> Edit(int id)
//        {
//            var integration = await _repo.GetByIdFullAsync(id);
//            if (integration == null) return NotFound();

//            var vm = new IntegrationFormViewModel
//            {
//                Integration = integration,
//                Connection = integration.Connection ?? new IntegrationConnection { IntegrationId = id },
//                Recipients = integration.Recipients.ToList()
//            };

//            return View("CreateEdit", vm);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, IntegrationFormViewModel vm)
//        {
//            if (id != vm.Integration.Id) return BadRequest();
//            if (!ModelState.IsValid) return View(vm);

//            if (vm.Integration.SourceType == "query" && !string.IsNullOrWhiteSpace(vm.Integration.Query))
//            {
//                var (isValid, error) = _queryValidator.Validate(vm.Integration.Query);
//                if (!isValid)
//                {
//                    ModelState.AddModelError("Integration.Query", error!);
//                    return View(vm);
//                }
//            }

//            await _repo.UpdateAsync(vm.Integration);

//            if (vm.Connection != null)
//            {
//                vm.Connection.IntegrationId = id;

//                // Ambil connection lama dari DB sekali saja
//                var existingConn = await _repo.GetConnectionAsync(id);

//                // Password — kalau dikosongkan di form, pertahankan yang lama
//                if (string.IsNullOrWhiteSpace(vm.Connection.Password))
//                    vm.Connection.Password = existingConn?.Password;
//                else
//                    //vm.Connection.Password = vm.Connection.Password;
//                vm.Connection.Password = _encryptionService.Encrypt(vm.Connection.Password);

//                // ApiToken — sama
//                //if (string.IsNullOrWhiteSpace(vm.Connection.ApiToken))
//                //    vm.Connection.ApiToken = existingConn?.ApiToken; // sudah encrypted
//                //else
//                //    vm.Connection.ApiToken = _encryption.Encrypt(vm.Connection.ApiToken);

//                await _repo.SaveConnectionAsync(vm.Connection);

//                //await _repo.SaveConnectionAsync(vm.Connection);
//            }

//            if (vm.Recipients != null)
//                await _repo.SaveRecipientsAsync(id, vm.Recipients);

//            await _scheduler.ReloadIntegrationAsync(id);

//            TempData["Success"] = "Integrasi berhasil diupdate.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── DELETE ────────────────────────────────────────────────────────────

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int id)
//        {
//            await _scheduler.RemoveIntegrationAsync(id);
//            await _repo.DeleteAsync(id);
//            TempData["Success"] = "Integrasi berhasil dihapus.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── TOGGLE ACTIVE ─────────────────────────────────────────────────────

//        [HttpPost]
//        public async Task<IActionResult> ToggleActive(int id)
//        {
//            var integration = await _repo.GetByIdAsync(id);
//            if (integration == null) return NotFound();

//            integration.IsActive = !integration.IsActive;
//            await _repo.UpdateAsync(integration);

//            if (!integration.IsActive)
//                await _scheduler.RemoveIntegrationAsync(id);
//            else if (integration.Timing == "scheduled")
//                await _scheduler.ReloadIntegrationAsync(id);

//            return Ok(new { isActive = integration.IsActive });
//        }

//        // ── TRIGGER MANUAL ────────────────────────────────────────────────────

//        [HttpPost]
//        public async Task<IActionResult> TriggerNow(int id)
//        {
//            try
//            {
//                await _scheduler.TriggerNowAsync(id);
//                TempData["Success"] = "Integrasi berhasil di-trigger manual.";
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = $"Gagal trigger: {ex.Message}";
//            }
//            return RedirectToAction(nameof(Index));
//        }

//        // ── HISTORY ───────────────────────────────────────────────────────────

//        public async Task<IActionResult> History(int id)
//        {
//            var integration = await _repo.GetByIdAsync(id);
//            if (integration == null) return NotFound();

//            var histories = await _repo.GetHistoriesAsync(id, take: 100);
//            ViewBag.Integration = integration;
//            return View(histories);
//        }

//        public async Task<IActionResult> RecentHistory()
//        {
//            var histories = await _repo.GetRecentHistoriesAsync(100);
//            return View(histories);
//        }

//        // ── QUERY PREVIEW (AJAX) ──────────────────────────────────────────────

//        [HttpPost]
//        public async Task<IActionResult> PreviewQuery([FromBody] PreviewQueryRequest req)
//        {
//            if (string.IsNullOrWhiteSpace(req.Query))
//                return BadRequest(new { error = "Query kosong." });

//            var (rows, error) = await _queryExecutor.PreviewAsync(req.Query);

//            if (error != null)
//                return Ok(new { success = false, error });

//            return Ok(new
//            {
//                success = true,
//                rowCount = rows.Count,
//                columns = rows.Count > 0 ? rows[0].Keys.ToList() : new List<string>(),
//                rows = rows.Take(20) // UI hanya tampil 20 baris
//            });
//        }

//        [HttpPost]
//        public IActionResult ValidateQuery([FromBody] PreviewQueryRequest req)
//        {
//            var (isValid, error) = _queryValidator.Validate(req.Query ?? "");
//            return Ok(new { isValid, error });
//        }
//    }

//    // ── ViewModels ────────────────────────────────────────────────────────────

//    public class IntegrationFormViewModel
//    {
//        public Integration Integration { get; set; } = new();
//        public IntegrationConnection? Connection { get; set; }
//        public List<IntegrationRecipient>? Recipients { get; set; } = new();
//    }

//    public class PreviewQueryRequest
//    {
//        public string? Query { get; set; }
//    }
//}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Repositories;
using TMSBilling.Services;
using TMSBilling.Services.Integration;

namespace TMSBilling.Controllers
{
    //[Authorize]
    [SessionAuthorize]
    public class IntegrationController : Controller
    {
        private readonly IIntegrationRepository _repo;
        private readonly IIntegrationDispatcher _dispatcher;
        private readonly IIntegrationScheduler _scheduler;
        private readonly IQueryValidator _queryValidator;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IIntegrationEmailNotifier _emailNotifier;
        private readonly ILogger<IntegrationController> _logger;
        private readonly IEncryptionService _encryptionService;

        public IntegrationController(
            IIntegrationRepository repo,
            IIntegrationDispatcher dispatcher,
            IIntegrationScheduler scheduler,
            IQueryValidator queryValidator,
            IQueryExecutor queryExecutor,
            IIntegrationEmailNotifier emailNotifier,
            ILogger<IntegrationController> logger,
            IEncryptionService encryptionService
            )
        {
            _repo = repo;
            _dispatcher = dispatcher;
            _scheduler = scheduler;
            _queryValidator = queryValidator;
            _queryExecutor = queryExecutor;
            _emailNotifier = emailNotifier;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        // ── LIST ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var integrations = await _repo.GetAllAsync();
            return View(integrations);
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public IActionResult Create()
        {
            return View("CreateEdit", new IntegrationFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IntegrationFormViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (vm.Integration.SourceType == "query" && !string.IsNullOrWhiteSpace(vm.Integration.Query))
            {
                var (isValid, error) = _queryValidator.Validate(vm.Integration.Query);
                if (!isValid) { ModelState.AddModelError("Integration.Query", error!); return View(vm); }
            }

            var integration = await _repo.CreateAsync(vm.Integration);

            if (vm.Connection != null)
            {
                vm.Connection.IntegrationId = integration.Id;
                await _repo.SaveConnectionAsync(vm.Connection);
            }

            if (vm.Recipients != null)
                await _repo.SaveRecipientsAsync(integration.Id, vm.Recipients);

            if (vm.Attachments != null && vm.Attachments.Any())
                await _repo.SaveAttachmentsAsync(integration.Id, vm.Attachments);

            if (integration.Timing == "scheduled")
                await _scheduler.ReloadIntegrationAsync(integration.Id);

            TempData["Success"] = "Integrasi berhasil dibuat.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int id)
        {
            var integration = await _repo.GetByIdFullAsync(id);
            if (integration == null) return NotFound();

            var vm = new IntegrationFormViewModel
            {
                Integration = integration,
                Connection = integration.Connection ?? new IntegrationConnection { IntegrationId = id },
                Recipients = integration.Recipients.ToList(),
                Attachments = integration.Attachments.ToList()
            };

            return View("CreateEdit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IntegrationFormViewModel vm)
        {
            if (id != vm.Integration.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            if (vm.Integration.SourceType == "query" && !string.IsNullOrWhiteSpace(vm.Integration.Query))
            {
                var (isValid, error) = _queryValidator.Validate(vm.Integration.Query);
                if (!isValid) { ModelState.AddModelError("Integration.Query", error!); return View(vm); }
            }

            await _repo.UpdateAsync(vm.Integration);

            //if (vm.Connection != null)
            //{
            //    vm.Connection.IntegrationId = id;
            //    await _repo.SaveConnectionAsync(vm.Connection);
            //}

            if (vm.Connection != null)
            {
                vm.Connection.IntegrationId = id;

                // Ambil connection lama dari DB sekali saja
                var existingConn = await _repo.GetConnectionAsync(id);

                // Password — kalau dikosongkan di form, pertahankan yang lama
                if (string.IsNullOrWhiteSpace(vm.Connection.Password))
                    vm.Connection.Password = existingConn?.Password;
                else
                    //vm.Connection.Password = vm.Connection.Password;
                    vm.Connection.Password = _encryptionService.Encrypt(vm.Connection.Password);

                // ApiToken — sama
                //if (string.IsNullOrWhiteSpace(vm.Connection.ApiToken))
                //    vm.Connection.ApiToken = existingConn?.ApiToken; // sudah encrypted
                //else
                //    vm.Connection.ApiToken = _encryption.Encrypt(vm.Connection.ApiToken);

                await _repo.SaveConnectionAsync(vm.Connection);

                //await _repo.SaveConnectionAsync(vm.Connection);
            }

            if (vm.Recipients != null)
                await _repo.SaveRecipientsAsync(id, vm.Recipients);

            if (vm.Attachments != null)
                await _repo.SaveAttachmentsAsync(id, vm.Attachments);

            await _scheduler.ReloadIntegrationAsync(id);

            TempData["Success"] = "Integrasi berhasil diupdate.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _scheduler.RemoveIntegrationAsync(id);
            await _repo.DeleteAsync(id);
            TempData["Success"] = "Integrasi berhasil dihapus.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var integration = await _repo.GetByIdAsync(id);
            if (integration == null) return NotFound();

            integration.IsActive = !integration.IsActive;
            await _repo.UpdateAsync(integration);

            if (!integration.IsActive)
                await _scheduler.RemoveIntegrationAsync(id);
            else if (integration.Timing == "scheduled")
                await _scheduler.ReloadIntegrationAsync(id);

            return Ok(new { isActive = integration.IsActive });
        }

        // ── TRIGGER MANUAL ────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> TriggerNow(int id)
        {
            try
            {
                await _scheduler.TriggerNowAsync(id);
                TempData["Success"] = "Integrasi berhasil di-trigger manual.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal trigger: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── HISTORY ───────────────────────────────────────────────────────────

        public async Task<IActionResult> History(int id)
        {
            var integration = await _repo.GetByIdAsync(id);
            if (integration == null) return NotFound();

            var histories = await _repo.GetHistoriesAsync(id, take: 100);
            ViewBag.Integration = integration;
            return View(histories);
        }

        public async Task<IActionResult> RecentHistory()
        {
            var histories = await _repo.GetRecentHistoriesAsync(100);
            return View(histories);
        }

        // ── QUERY PREVIEW (AJAX) ──────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> PreviewQuery([FromBody] PreviewQueryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Query))
                return BadRequest(new { error = "Query kosong." });

            var (rows, error) = await _queryExecutor.PreviewAsync(req.Query);

            if (error != null)
                return Ok(new { success = false, error });

            return Ok(new
            {
                success = true,
                rowCount = rows.Count,
                columns = rows.Count > 0 ? rows[0].Keys.ToList() : new List<string>(),
                rows = rows.Take(20) // UI hanya tampil 20 baris
            });
        }

        [HttpPost]
        public IActionResult ValidateQuery([FromBody] PreviewQueryRequest req)
        {
            var (isValid, error) = _queryValidator.Validate(req.Query ?? "");
            return Ok(new { isValid, error });
        }

        // ── EMAIL PREVIEW (AJAX) ──────────────────────────────────────────────

        /// <summary>
        /// Render preview HTML email berdasarkan template + sample data.
        /// Dipanggil dari tab Email di form UI.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PreviewEmail([FromBody] PreviewEmailRequest req)
        {
            // Build integration object dari request (tidak harus sudah tersimpan)
            var integration = new TMSBilling.Models.Integration
            {
                Id = req.IntegrationId,
                Name = req.IntegrationName ?? "Sample Integration",
                EventKey = req.EventKey ?? "order.loaded",
                ChannelType = req.ChannelType ?? "google_sheets",
                EmailSubjectTemplate = req.SubjectTemplate,
                EmailBodyTemplate = req.BodyTemplate,
                EmailFooterTemplate = req.FooterTemplate,
                EmailIncludeDataTable = req.IncludeDataTable,
            };

            // Build sample data dari query jika ada
            List<Dictionary<string, object?>> sampleRows = new();
            Dictionary<string, object?> sampleEventData = new()
            {
                ["order_no"] = "SPK-2026-001",
                ["status"] = "loaded",
                ["driver_name"] = "Budi Santoso",
                ["destination"] = "Surabaya",
                ["updated_by"] = "admin",
                ["updated_at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Jika ada query, jalankan untuk ambil sample rows
            if (!string.IsNullOrWhiteSpace(req.Query))
            {
                try
                {
                    var (rows, error) = await _queryExecutor.PreviewAsync(req.Query);
                    if (error == null && rows.Any())
                    {
                        sampleRows = rows.Take(5).ToList(); // limit 5 rows untuk preview
                        // Update event data dari kolom query
                        foreach (var (k, v) in rows[0])
                            sampleEventData[k] = v;
                    }
                }
                catch { /* ignore query errors in preview */ }
            }

            // Merge dengan user-provided sample data
            if (req.SampleData != null)
                foreach (var (k, v) in req.SampleData)
                    sampleEventData[k] = v;

            var isSuccess = req.PreviewAs != "failed";
            var result = new SenderResult
            {
                Success = isSuccess,
                Message = isSuccess ? $"{sampleRows.Count} baris berhasil dikirim ke {integration.ChannelType}" : "Koneksi ke server gagal: Connection timed out",
                ErrorDetail = isSuccess ? null : "System.Net.Sockets.SocketException: Connection timed out\n   at ...",
                RowCount = sampleRows.Count > 0 ? sampleRows.Count : 3,
                DurationMs = 342
            };

            var html = _emailNotifier.PreviewHtml(integration, result, sampleEventData, sampleRows);
            return Content(html, "text/html");
        }

        /// <summary>
        /// Kembalikan daftar placeholder yang tersedia berdasarkan integration + query.
        /// Dipanggil dari tab Email di form UI untuk tampilkan variable panel.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetEmailPlaceholders([FromBody] PreviewEmailRequest req)
        {
            var integration = new TMSBilling.Models.Integration
            {
                Id = req.IntegrationId,
                Name = req.IntegrationName ?? "Sample",
                EventKey = req.EventKey ?? "order.loaded",
                ChannelType = req.ChannelType ?? "google_sheets",
            };

            List<Dictionary<string, object?>>? sampleRows = null;
            if (!string.IsNullOrWhiteSpace(req.Query))
            {
                try
                {
                    var (rows, _) = await _queryExecutor.PreviewAsync(req.Query);
                    if (rows.Any()) sampleRows = rows.Take(1).ToList();
                }
                catch { }
            }

            var placeholders = _emailNotifier.GetPlaceholders(integration, req.SampleData, sampleRows);
            return Ok(placeholders.Select(p => new
            {
                p.Key,
                p.Description,
                p.Example,
                p.Category
            }));
        }
    }

    // ── ViewModels ────────────────────────────────────────────────────────────

    public class IntegrationFormViewModel
    {
        public Integration Integration { get; set; } = new();
        public IntegrationConnection? Connection { get; set; }
        public List<IntegrationRecipient>? Recipients { get; set; } = new();
        public List<IntegrationAttachment>? Attachments { get; set; } = new();
    }

    public class PreviewQueryRequest
    {
        public string? Query { get; set; }
    }

    public class PreviewEmailRequest
    {
        public int IntegrationId { get; set; }
        public string? IntegrationName { get; set; }
        public string? EventKey { get; set; }
        public string? ChannelType { get; set; }
        public string? SubjectTemplate { get; set; }
        public string? BodyTemplate { get; set; }
        public string? FooterTemplate { get; set; }
        public bool IncludeDataTable { get; set; } = true;
        public string? AttachmentMode { get; set; } = "inline";
        public string? Query { get; set; }
        public string? PreviewAs { get; set; } = "success";
        public Dictionary<string, object?>? SampleData { get; set; }
    }
}

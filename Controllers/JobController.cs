
using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;
using TMSBilling.Services;
using TMSBilling.Services.Integration;
using static TMSBilling.Models.ViewModels.JobViewModel;

namespace TMSBilling.Controllers
{

    [SessionAuthorize]
    public class JobController : Controller
    {

        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly ApiService _apiService;
        private readonly IConfiguration _configuration;
        private readonly SyncronizeWithMcEasy _sync;
        private readonly IEmailService _emailService;
        private readonly IIntegrationDispatcher _integrationDispatcher;
        private readonly IMailReportService _mailer;

        public JobController(AppDbContext context, 
            SelectListService selectList, 
            ApiService apiService, 
            IConfiguration configuration,
            SyncronizeWithMcEasy sync,
            IEmailService emailService,
            IIntegrationDispatcher integrationDispatcher,
            IMailReportService mailer

            )
        {
            _context = context;
            _selectList = selectList;
            _apiService = apiService;
            _configuration = configuration;
            _sync = sync;
            _emailService = emailService;
            _integrationDispatcher = integrationDispatcher;
            _mailer = mailer;

        }

        [HttpPost]
        public async Task<IActionResult> SyncronizeFO()
        {
            try
            {
                var ordersFromApi = await _sync.FetchFO();
                var result = await _sync.SyncFOToDatabase(ordersFromApi);
                return Json(new
                {
                    success = true,
                    message = $"Synchronization successful. {result} order records have been updated.",
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Terjadi kesalahan saat sinkronisasi: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            // Default: 7 hari terakhir
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddDays(-7).Date;

            if (!endDate.HasValue)
                endDate = DateTime.Now.Date;

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            var data = await GetJobSummaryQuery(startDate.Value, endDate.Value).ToListAsync();
            return View(data);
        }

        private IQueryable<JobSummaryViewModel> GetJobSummaryQuery(DateTime startDate, DateTime endDate)
        {
            var username = HttpContext.Session.GetString("username") ?? "System";
            var sql = @"
                WITH tj AS (
                    SELECT 
                        jobid,
                        COUNT(inv_no) AS total_do
                    FROM TRC_JOB
                    GROUP BY jobid
                )
                SELECT 
                    a.id_seq AS IdSeq,
                    a.jobid AS JobId,
                    c.MAIN_CUST AS CustomerMain,
                    a.truck_no AS TruckNo,
                    a.deliv_date AS DelivDate,
                    a.origin AS Origin,
                    a.dest AS Dest,
                    a.truck_size AS TruckSize,
                    CASE 
				        WHEN mc_fo.[status] IS NOT NULL THEN mc_fo.[status]
				        ELSE 
					        CASE 
						        WHEN a.status_job = 'DRAFT' THEN 'Draf'
						        WHEN a.status_job = 'STARTED' THEN 'Perjalanan'
                                WHEN a.status_job = 'CLOSED' THEN 'Closed'
                                WHEN a.status_job = 'CANCELLED' THEN 'Cancel'
						        ELSE NULL
					        END
			        END AS MCStatus,
                    a.vendor_plan AS VendorPlan,
                    COALESCE(tj.total_do, 0) AS TotalDo,
                    a.driver_name as DriverName,
                    a.serv_type as ServiceType,
                    a.is_integration as IsIntegration
                FROM TRC_JOB_H a
                LEFT JOIN tj ON a.jobid = tj.jobid
                LEFT JOIN mc_fo ON mc_fo.id = a.mceasy_job_id
                INNER JOIN TRC_CUST_GROUP c ON a.cust_group = c.SUB_CODE
                INNER JOIN UserXCustomers d ON d.CustomerMain = c.MAIN_CUST
                WHERE d.Username = {0}
                    AND CAST(a.deliv_date AS date) BETWEEN {1} AND {2}
                ORDER BY a.id_seq DESC
            ";
            return _context.JobSummaryView.FromSqlRaw(sql, username, startDate, endDate);
        }

        [Route("Job/Form/{jobid?}")]
        public IActionResult Form(string? jobid)
        {
            var vm = new JobViewModel();

            // ViewBag dropdown


            var username = HttpContext.Session.GetString("username") ?? "System";
            var customerGroups = _context.UserXCustomers
                .Where(x => x.UserName == username)
                .Select(x => x.CustomerMain)
                .Distinct()
                .Join(_context.CustomerGroups,
                      custMain => custMain,
                      cg => cg.MAIN_CUST,
                      (custMain, cg) => cg)
                .ToList();

            var customerGroupAllowed = customerGroups
                .Select(x => x.SUB_CODE)
                .Distinct()
                .ToList();
            ViewBag.ListCustomerGroup = _context.CustomerGroups
            .Where(o => customerGroupAllowed.Contains(o.SUB_CODE))
            .Select(c => new SelectListItem
            {
                Value = c.SUB_CODE,
                Text = c.SUB_CODE,
            }).ToList();

            ViewBag.ListWarehouse = _selectList.GetWarehouse();
            ViewBag.ListConsignee = _selectList.GetConsignee();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            //ViewBag.ListDestination = _selectList.GetDestinations();

            var accessibleCustomers = _context.UserXCustomers
            .Where(x => x.UserName == username)
            .Select(x => x.CustomerMain)
            .Distinct()
            .ToList();

            ViewBag.ListDestination = _context.Destinations
                .Where(d => accessibleCustomers.Contains(d.MAIN_CUST))
                .Select(d => d.destination_code)
                .Distinct()
                .ToList()
                .Select(code => new SelectListItem { Value = code, Text = code })
                .ToList();

            ViewBag.ListUoM = _selectList.GetChargeUoms();
            ViewBag.ListModa = _selectList.GetServiceModas();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();
            ViewBag.ListPickupCargo = _selectList.GetYesNo();
            ViewBag.ListMultiDrop = _selectList.GetYesNo();
            ViewBag.ListMultitrip = _selectList.GetYesNo();
            ViewBag.ListVendor = _selectList.GetVendors();
            ViewBag.ListStartingPoint = _selectList.GetStartingPoint();
            ViewBag.ListJobType = _selectList.JobTypeOption();

            // ✅ Default selalu diset dulu
            vm.Header.job_type = "NORMAL";
            vm.Header.status_job = "DRAFT";


            if (!string.IsNullOrEmpty(jobid))
            {

                var jobHeader = _context.JobHeaders.FirstOrDefault(or => or.jobid == jobid);
                if (jobHeader == null) {
                    return View(vm);
                }

                var geofence = _context.Geofences.FirstOrDefault(f => f.GeofenceId == jobHeader.starting_point);

                if (geofence == null) { return View(vm); }

                jobHeader.starting_point = geofence.Id;

                vm.Header = jobHeader;
            }

            return View(vm);
        }

        [HttpGet]
        public IActionResult GetOrdersByDate(string date, string jobid)
        {
            if (string.IsNullOrEmpty(date))
                return BadRequest(new { success = false, message = "Tanggal tidak valid" });

            try
            {
                DateTime parsedDate = DateTime.ParseExact(date, "yyyy-MM-dd", null);

                Console.WriteLine("deliv_date "+ date);
                Console.WriteLine("jobId " + jobid);

                var query = _context.Orders.AsQueryable();

                if (!string.IsNullOrEmpty(jobid))
                {

                    query = query.Where(o =>
                        (EF.Functions.DateDiffDay(o.delivery_date, parsedDate) == 0 && o.order_status == 0)
                        || o.jobid == jobid
                    );
                }
                else
                {

                    query = query.Where(o =>
                        EF.Functions.DateDiffDay(o.delivery_date, parsedDate) == 0 && o.order_status == 0
                    );
                }

                var orders = query
                    .Select(o => new
                    {
                        o.id_seq,
                        o.wh_code,
                        o.sub_custid,
                        o.inv_no,
                        o.delivery_date,
                        o.origin_id,
                        o.dest_area,
                        o.tot_pkgs,
                        o.truck_size,
                        o.order_status,
                        o.remark
                    })
                .Distinct()
                .ToList();

                return Ok(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Job/Save/{jobid?}")]
        public async Task<IActionResult> Save([FromBody] JobViewModel model, string? jobid) 
        {

            if (model == null || model.FormJobHeader == null || model.FormJobDetails == null)
            {
                return BadRequest(new { success = false, message = "Incomplete data!" });
            }

            Console.WriteLine("JOB ID " + jobid);
            Console.WriteLine("JOB FORM HEADER " + System.Text.Json.JsonSerializer.Serialize(model.FormJobHeader));
            Console.WriteLine("JOB FORM DETAIL " + System.Text.Json.JsonSerializer.Serialize(model.FormJobDetails));

            var Header = model.FormJobHeader;
            var Details = model.FormJobDetails;

            var customerGroup = await _context.CustomerGroups.FirstOrDefaultAsync( c => c.SUB_CODE == Header.cust_group);
            if (customerGroup == null) {
                return Json(new { success = true, message = "Customer group not found!" });
            }

            var customerApi = await _context.CustomerGroups.AnyAsync(cs => cs.SUB_CODE == Header.cust_group && cs.API_FLAG == 1);
            var GeofenceStartingPoint = await _context.Geofences.FirstOrDefaultAsync(f => f.Id == Header.starting_point);

            if (GeofenceStartingPoint != null)
            {
                Header.starting_point = GeofenceStartingPoint.GeofenceId;
            }

            //// check order is b2c
            //var isB2C = false;
            //foreach (var ord in Details) {
            //    var order = _context.Orders.First(o => o.inv_no == ord.inv_no);
            //    if (order == null) { continue; }
            //    if (order.is_b2c == "1") {
            //        isB2C = true;
            //        continue;
            //    }
            //}

            var invNos = Details.Select(d => d.inv_no).ToList();

            var isB2C = _context.Orders
                .Where(o => invNos.Contains(o.inv_no) && o.is_b2c == "1")
                .Any();


            if (customerApi && !isB2C)
            {
                var run = await RunSaveWithApi(Header, Details, jobid);
                if (!run.ok) return BadRequest(new { success = false, message = run.message });
            }
            else
            {
                var run = await RunSaveWithOutApi(Header, Details, jobid);
                if (!run.ok) return BadRequest(new { success = false, message = run.message });
            }


            var userId = HttpContext.Session.GetString("username") ?? "System";


            // Panggil setelah update status order
            //await _integrationDispatcher.DispatchAsync("job.created", new Dictionary<string, object?>
            //{
            //    { "order_no", jobid },
            //    { "status", "created" },
            //    { "updated_by", userId },
            //    { "updated_at", DateTime.UtcNow },
            //});


            //if (jobid != null)
            //{
            //    var emailVendors = await _context.VendorTruckEmails
            //      .Where(vte => vte.sup_code == Header.vendor_act && vte.is_active == 1)
            //      .ToListAsync();

            //    var emailTo = string.Join(",", emailVendors
            //        .Where(e => e.email_type == "TO")
            //        .Select(e => e.email_address));

            //    var emailCc = string.Join(",", emailVendors
            //        .Where(e => e.email_type == "CC")
            //        .Select(e => e.email_address));

            //    _mailer.TriggerEvent("spk.created", new Dictionary<string, string>
            //    {
            //        ["nomor_spk"] = jobid,
            //        ["vendor_name"] = model.FormJobHeader.vendor_act,
            //        ["tanggal"] = DateTime.UtcNow.ToString("dd MMMM yyyy"),
            //        ["order_id"] = jobid,
            //        ["email_to"] = emailTo,
            //        ["email_cc"] = emailCc
            //    }, HttpContext.Session.GetString("username") ?? "System");
            //}
           

            return Json(new { success = true, message = "Job saved successfully" });
        }

        private async Task<(bool ok, string message)> RunSaveWithApi(
            HeaderFormJob Header,
            List<OrderForJobForm> Details,
            string? jobid
        )
        {

            if (Details == null || Details.Count == 0)
            {
                return (false, "Order detail not found.");
            }

            var CostRate = _context.PriceBuys.FirstOrDefault(hm =>
            hm.sup_code == Header.vendor_id
            && hm.origin == Header.origin_id
            && hm.dest == Header.dest_area
            && hm.serv_moda == Header.serv_moda
            && hm.truck_size == Header.truck_size);

            if (CostRate == null)
            {
                return (false, "Header, price buy not found");
            }

            var OriginIsMatch = false;
            var DestinationMatch = false;

            // check origin order min 1 is match
            foreach (var oro in Details)
            {
                if (CostRate.origin == oro.origin_id)
                {
                    OriginIsMatch = true;
                    break;
                }
            }

            if (!OriginIsMatch)
            {
                return (false, $"Origin not found in order. Required origin: {CostRate.origin}");
            }

            // check destination order min 1 is match
            foreach (var ordes in Details)
            {
                if (CostRate.dest == ordes.dest_area)
                {
                    DestinationMatch = true;
                    break;
                }
            }

            if (!DestinationMatch)
            {
                return (false, $"Destination not found in order. Required destination : {CostRate.dest}");
            }


            foreach (var ord in Details)
            {
                var orderExisting = _context.Orders.FirstOrDefault(or => or.inv_no == ord.inv_no);
                if (orderExisting == null)
                {
                    return (false, "Order not found");
                }

                var customerGroup = _context.CustomerGroups.FirstOrDefault(g => g.SUB_CODE == orderExisting.sub_custid);

                if (customerGroup == null)
                {
                    return (false, "Customer group not found");
                }

                var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

                if (customer == null)
                {
                    return (false, "Customer not found");
                }


                var SellRateCheck = _context.PriceSells.FirstOrDefault(sr =>
                                    sr.cust_code == customer.MAIN_CUST
                                    && sr.origin == orderExisting.origin_id
                                    && sr.dest == orderExisting.dest_area
                                    && sr.truck_size == orderExisting.truck_size
                                    && sr.serv_type == orderExisting.serv_req
                                    && sr.serv_moda == orderExisting.moda_req
                                    && sr.charge_uom == orderExisting.uom
                                    );
                if (SellRateCheck == null)
                {
                    return (false, "Sell rate not found for INV " + orderExisting.inv_no);

                }
            }

            var jobPrefix = _context.Configs
                .Where(x => x.key == "job-prefix")
                .Select(x => x.value)
                .FirstOrDefault();
            string monthPrefix = jobPrefix + DateTime.Now.ToString("yyMM");

            // Hitung jumlah jobid yang sudah ada untuk bulan ini
            int existingCount = _context.JobHeaders
                .Where(j => j.jobid != null && j.jobid.StartsWith(monthPrefix))
                .Count();

            // Buat jobid baru

            string newJobId = jobid ?? GenerateJobId(existingCount + 1);

            //return Json(new { success = true, message = "BOLEH LANJUT, JOB ID : "+jobid });

            // awal kosong
            var deliveryOrderIds = new List<string>();
            var mceasy_job_id = string.Empty;


            // BAGIAN INSERT NEW JOB ADA DISINI
            var result = InsertOrderToJob(Details, newJobId, Header, CostRate);

            if (!result.ok)
                return (false, result.message);

            deliveryOrderIds = result.deliveryOrderIds;

            if (deliveryOrderIds.Count > 0 && string.IsNullOrEmpty(jobid))
            {
                var payload = new
                {
                    delivery_order_ids = deliveryOrderIds,
                };

                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Post,
                    "fleet-planning/api/web/v1/fleet-task",
                    payload
                );
                if (!ok)
                {
                    return (false, "Gagal kirim ke API Store Fleet Task");
                }

                mceasy_job_id = json.GetProperty("data").GetProperty("id").GetString();


                var payload2 = new
                {
                    expected_departure_on = Header.dvdate.HasValue ? Header.dvdate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null,
                    shipment_reference = newJobId,
                    start_address_id = Header.starting_point,
                    end_address_id = Header.starting_point,
                };

                var (ok2, json2) = await _apiService.SendRequestAsync(
                    HttpMethod.Patch,
                    $"fleet-planning/api/web/v1/fleet-task/{mceasy_job_id}",
                    payload2
                );
                if (!ok2)
                {
                    return (false, "Gagal kirim ke API Patch Fleet Task");
                }

                var payload3 = new
                {
                    status = "DRAFT"
                };

                var (ok3, json3) = await _apiService.SendRequestAsync(
                    HttpMethod.Post,
                    $"fleet-planning/api/web/v1/fleet-task/{mceasy_job_id}/transition",
                    payload3
                );

                if (!ok3)
                {
                    return (false, "Gagal kirim ke API Do Transition Fleet Task!");
                }  

            }

            if (!string.IsNullOrEmpty(jobid))
            {

                var jobHeader = _context.JobHeaders.FirstOrDefault(jo => jo.jobid == jobid);
                if (jobHeader != null)
                {
                    if (!string.IsNullOrEmpty(jobHeader.mceasy_job_id))
                    {
                        foreach (var idDel in deliveryOrderIds)
                        {

                            var (okOpt, jsonOpt) = await _apiService.SendRequestAsync(
                                HttpMethod.Options,
                                $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}/delivery-order/{idDel}"
                            );

                            var (okDel, jsonDel) = await _apiService.SendRequestAsync(
                                HttpMethod.Delete,
                                $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}/delivery-order/{idDel}"
                            );
                            //if (!okDel)
                            //{
                            //    return BadRequest(new
                            //    {
                            //        success = false,
                            //        message = "Failed delete order from fleet task",
                            //        detail = jsonDel
                            //    });
                            //}

                        }

                        var payload = new
                        {
                            expected_departure_on = Header.dvdate.HasValue
                            ? Header.dvdate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            : null,
                            shipment_reference = jobid,
                            start_address_id = Header.starting_point,
                            end_address_id = Header.starting_point,
                        };

                        var (ok, json) = await _apiService.SendRequestAsync(
                            HttpMethod.Patch,
                            $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}",
                            payload
                        );
                        if (!ok)
                        {
                            //return BadRequest(new
                            //{
                            //    success = false,
                            //    message = "Gagal kirim ke API Patch Fleet Task [Edit Job]",
                            //    detail = json
                            //});
                        }
                        else
                        {

                            jobHeader.starting_point = Header.starting_point;

                        }

                        var newOrderIDs = new List<string>();

                        foreach (var item in Details)
                        {
                            Console.WriteLine("NEW ORDER ID : {0}", item.mceasy_order_id);
                            var orderExisting = _context.Orders.FirstOrDefault(or => or.inv_no == item.inv_no);
                            if (orderExisting != null && orderExisting.mceasy_order_id != null)
                            {
                                newOrderIDs.Add(orderExisting.mceasy_order_id);
                            }
                        }

                        var payloadNewOrder = new
                        {
                            delivery_order_ids = newOrderIDs,
                        };

                        var (okNew, jsonNew) = await _apiService.SendRequestAsync(
                            HttpMethod.Put,
                            $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}/delivery-order",
                            payloadNewOrder
                        );
                        if (!okNew)
                        {
                            //return BadRequest(new
                            //{
                            //    success = false,
                            //    message = "Gagal kirim ke API Store Fleet Task [PUT]",
                            //    detail = jsonNew
                            //});

                            var (inter1, jsonInter1) = await _apiService.SendRequestAsync(
                                HttpMethod.Options,
                                $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}/intervention"
                            );

                            var (inter2, jsonInter2) = await _apiService.SendRequestAsync(
                                HttpMethod.Post,
                                $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}/intervention",
                                payloadNewOrder
                            );

                        }


                        jobHeader.cust_group = Header.cust_group;
                        jobHeader.vendor_plan = Header.vendor_id;
                        jobHeader.vendor_act = Header.vendor_act;
                        jobHeader.is_vendor = Header.vendor_id == Header.vendor_act ? true : false;
                        jobHeader.pickup_date = Header.pickup_date;
                        jobHeader.deliv_date = Header.dvdate;
                        jobHeader.charge_uom = Header.charge_uom;
                        jobHeader.dest = Header.dest_area;
                        jobHeader.origin = Header.origin_id;
                        jobHeader.driver_name = Header.driver_name;
                        jobHeader.serv_moda = Header.serv_moda;
                        jobHeader.serv_type = Header.serv_type;
                        jobHeader.truck_size = Header.truck_size;
                        jobHeader.truck_no = Header.truck_id;
                        jobHeader.multidrop = Header.multidrop;
                        jobHeader.multitrip = Header.multitrip;
                        jobHeader.ritase_seq = Header.ritase_seq;
                        jobHeader.job_type = Header.job_type;
                        jobHeader.update_user = HttpContext.Session.GetString("username") ?? "System";
                        jobHeader.update_date = DateTime.Now;

                        if (!string.IsNullOrEmpty(mceasy_job_id))
                        {
                            jobHeader.mceasy_job_id = mceasy_job_id;
                        }
                        _context.JobHeaders.Update(jobHeader);

                    }


                    // delete existing job first
                    var jobExistingList = _context.Jobs.Where(j => j.jobid == jobid).ToList();

                    if (jobExistingList.Any())
                    {
                        Console.WriteLine("DELETE JOB : {0}", jobid);
                        var rows = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_JOB WHERE jobid = {0}", jobid);
                        Console.WriteLine("Rows delete affected: " + rows);
                    }

                    var rowUpd = _context.Database.ExecuteSqlRaw("UPDATE TRC_ORDER SET order_status = 0, jobid = NULL WHERE jobid = {0}", jobid);
                    Console.WriteLine("Rows update affected: " + rowUpd);
                }
                else
                {
                    return (false, "Data job not found!");
                }
            }
            else
            {
                var (ok4, json4) = await _apiService.SendRequestAsync(
                    HttpMethod.Get,
                    $"fleet-planning/api/web/v1/fleet-task/{mceasy_job_id}"
                );

                if (!ok4)
                    return (false, "Gagal kirim ke API Show Fleet Task!");

                var fo = json4.GetProperty("data")
                             .Deserialize<FleetOrderMcEasy>()
                          ?? new FleetOrderMcEasy();

                var mcFO = new MCFleetOrder();
                mcFO.id = mceasy_job_id;
                mcFO.number = fo.number;
                mcFO.shipment_reference = newJobId;
                mcFO.status = fo.status?.name;
                mcFO.status_raw_type = fo.status?.raw_type;
                mcFO.entry_date = DateTime.Now;
                _context.MCFleetOrders.Add(mcFO);

                var jobHeader = new JobHeader();
                jobHeader.jobid = newJobId;
                jobHeader.cust_group = Header.cust_group;
                jobHeader.vendor_plan = Header.vendor_id;
                jobHeader.vendor_act = Header.vendor_act;
                jobHeader.is_vendor = Header.vendor_id == Header.vendor_act ? true : false;
                jobHeader.pickup_date = Header.pickup_date;
                jobHeader.deliv_date = Header.dvdate;
                jobHeader.charge_uom = Header.charge_uom;
                jobHeader.dest = Header.dest_area;
                jobHeader.origin = Header.origin_id;
                jobHeader.driver_name = Header.driver_name;
                jobHeader.serv_moda = Header.serv_moda;
                jobHeader.serv_type = Header.serv_type;
                jobHeader.truck_size = Header.truck_size;
                jobHeader.truck_no = Header.truck_id;
                jobHeader.multidrop = Header.multidrop;
                jobHeader.multitrip = Header.multitrip;
                jobHeader.ritase_seq = Header.ritase_seq;
                jobHeader.job_type = Header.job_type;
                jobHeader.entry_user = HttpContext.Session.GetString("username") ?? "System";
                jobHeader.entry_date = DateTime.Now;
                jobHeader.mceasy_job_id = mceasy_job_id;
                jobHeader.starting_point = Header.starting_point;
                jobHeader.status_job = "DRAFT";

                _context.JobHeaders.Add(jobHeader);
            }


            // Update field custom Order Type [udf2] dan Bisnis unit [udf4] 
            var payloadFieldCustome = new
            {
                udf2 = Header.serv_type,
                udf4 = Header.vendor_act,
            };

            foreach (var idUpd in deliveryOrderIds)
            {
                var (oks1, jsons1) = await _apiService.SendRequestAsync(
                    HttpMethod.Patch,
                    $"order/api/web/v1/delivery-order/{idUpd}",
                    payloadFieldCustome
                );
                //if (!oks1)
                //{
                //    return (false, "GAGAL UPDATE CUSTOM FIELD");
                //}
            }

            await _context.SaveChangesAsync();

            return (true, "OK");
        }


        private async Task<(bool ok, string message)> RunSaveWithOutApi(
            HeaderFormJob Header,
            List<OrderForJobForm> Details,
            string? jobid
        )
        {

            if (Details == null || Details.Count == 0)
            {
                return (false, "Order detail not found.");
            }

            var CostRate = _context.PriceBuys.FirstOrDefault(hm =>
            hm.sup_code == Header.vendor_id
            && hm.origin == Header.origin_id
            && hm.dest == Header.dest_area
            && hm.serv_moda == Header.serv_moda
            && hm.truck_size == Header.truck_size);

            if (CostRate == null)
            {
                return (false, "Header, price buy not found");
            }

            var OriginIsMatch = false;
            var DestinationMatch = false;

            // check origin order min 1 is match
            foreach (var oro in Details)
            {
                if (CostRate.origin == oro.origin_id)
                {
                    OriginIsMatch = true;
                    break;
                }
            }

            if (!OriginIsMatch)
            {
                return (false, $"Origin not found in order. Required origin: {CostRate.origin}");
            }

            // check destination order min 1 is match
            foreach (var ordes in Details)
            {
                if (CostRate.dest == ordes.dest_area)
                {
                    DestinationMatch = true;
                    break;
                }
            }

            if (!DestinationMatch)
            {
                return (false, $"Destination not found in order. Required destination : {CostRate.dest}");
            }


            foreach (var ord in Details)
            {
                var orderExisting = _context.Orders.FirstOrDefault(or => or.inv_no == ord.inv_no);
                if (orderExisting == null)
                {
                    return (false, "Order not found");
                }

                var customerGroup = _context.CustomerGroups.FirstOrDefault(g => g.SUB_CODE == orderExisting.sub_custid);

                if (customerGroup == null)
                {
                    return (false, "Customer group not found");
                }

                var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

                if (customer == null)
                {
                    return (false, "Customer not found");
                }


                var SellRateCheck = _context.PriceSells.FirstOrDefault(sr =>
                                    sr.cust_code == customer.MAIN_CUST
                                    && sr.origin == orderExisting.origin_id
                                    && sr.dest == orderExisting.dest_area
                                    && sr.truck_size == orderExisting.truck_size
                                    && sr.serv_type == orderExisting.serv_req
                                    && sr.serv_moda == orderExisting.moda_req
                                    && sr.charge_uom == orderExisting.uom
                                    );
                if (SellRateCheck == null)
                {
                    return (false, "Sell rate not found for INV " + orderExisting.inv_no);

                }
            }

            var jobPrefix = _context.Configs
                .Where(x => x.key == "job-prefix")
                .Select(x => x.value)
                .FirstOrDefault();
            string monthPrefix = jobPrefix + DateTime.Now.ToString("yyMM");

            // Hitung jumlah jobid yang sudah ada untuk bulan ini
            int existingCount = _context.JobHeaders
                .Where(j => j.jobid != null && j.jobid.StartsWith(monthPrefix))
                .Count();

            // Buat jobid baru

            string newJobId = jobid ?? GenerateJobId(existingCount + 1);

            //return Json(new { success = true, message = "BOLEH LANJUT, JOB ID : "+jobid });

            // awal kosong
            var deliveryOrderIds = new List<string>();
            var mceasy_job_id = string.Empty;


            // BAGIAN INSERT NEW JOB ADA DISINI
            var result = InsertOrderToJob(Details, newJobId, Header, CostRate);

            if (!result.ok)
                return (false, result.message);

            deliveryOrderIds = result.deliveryOrderIds;



            if (!string.IsNullOrEmpty(jobid))
            {

                var jobHeader = _context.JobHeaders.FirstOrDefault(jo => jo.jobid == jobid);
                if (jobHeader != null)
                {
                    jobHeader.cust_group = Header.cust_group;
                    jobHeader.vendor_plan = Header.vendor_id;
                    jobHeader.vendor_act = Header.vendor_act;
                    jobHeader.is_vendor = Header.vendor_id == Header.vendor_act ? true : false;
                    jobHeader.pickup_date = Header.pickup_date;
                    jobHeader.deliv_date = Header.dvdate;
                    jobHeader.charge_uom = Header.charge_uom;
                    jobHeader.dest = Header.dest_area;
                    jobHeader.origin = Header.origin_id;
                    jobHeader.driver_name = Header.driver_name;
                    jobHeader.serv_moda = Header.serv_moda;
                    jobHeader.serv_type = Header.serv_type;
                    jobHeader.truck_size = Header.truck_size;
                    jobHeader.truck_no = Header.truck_id;
                    jobHeader.multidrop = Header.multidrop;
                    jobHeader.multitrip = Header.multitrip;
                    jobHeader.ritase_seq = Header.ritase_seq;
                    jobHeader.job_type = Header.job_type;
                    jobHeader.update_user = HttpContext.Session.GetString("username") ?? "System";
                    jobHeader.update_date = DateTime.Now;

                    
                    _context.JobHeaders.Update(jobHeader);


                    // delete existing job first
                    var jobExistingList = _context.Jobs.Where(j => j.jobid == jobid).ToList();

                    if (jobExistingList.Any())
                    {
                        Console.WriteLine("DELETE JOB : {0}", jobid);
                        var rows = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_JOB WHERE jobid = {0}", jobid);
                        Console.WriteLine("Rows delete affected: " + rows);
                    }

                    var rowUpd = _context.Database.ExecuteSqlRaw("UPDATE TRC_ORDER SET order_status = 0, jobid = NULL WHERE jobid = {0}", jobid);
                    Console.WriteLine("Rows update affected: " + rowUpd);
                }
                else
                {
                    return (false, "Data job not found!");
                }
            }
            else
            {
                var jobHeader = new JobHeader();
                jobHeader.jobid = newJobId;
                jobHeader.cust_group = Header.cust_group;
                jobHeader.vendor_plan = Header.vendor_id;
                jobHeader.vendor_act = Header.vendor_act;
                jobHeader.is_vendor = Header.vendor_id == Header.vendor_act ? true : false;
                jobHeader.pickup_date = Header.pickup_date;
                jobHeader.deliv_date = Header.dvdate;
                jobHeader.charge_uom = Header.charge_uom;
                jobHeader.dest = Header.dest_area;
                jobHeader.origin = Header.origin_id;
                jobHeader.driver_name = Header.driver_name;
                jobHeader.serv_moda = Header.serv_moda;
                jobHeader.serv_type = Header.serv_type;
                jobHeader.truck_size = Header.truck_size;
                jobHeader.truck_no = Header.truck_id;
                jobHeader.multidrop = Header.multidrop;
                jobHeader.multitrip = Header.multitrip;
                jobHeader.ritase_seq = Header.ritase_seq;
                jobHeader.job_type = Header.job_type;
                jobHeader.entry_user = HttpContext.Session.GetString("username") ?? "System";
                jobHeader.entry_date = DateTime.Now;
                //jobHeader.starting_point = Header.starting_point;
                jobHeader.is_integration = 0;
                jobHeader.status_job = "DRAFT";

                _context.JobHeaders.Add(jobHeader);
            }

            await _context.SaveChangesAsync();

            return (true, "OK");
        }


        private (bool ok, string message, List<string> deliveryOrderIds) InsertOrderToJob(
            IEnumerable<OrderForJobForm> Details,
            String newJobId,
            HeaderFormJob Header,
            PriceBuy CostRate)
            {
                var deliveryOrderIds = new List<string>();

                foreach (var ordx in Details)
                {
                    var order = _context.Orders.FirstOrDefault(j => j.inv_no == ordx.inv_no);
                    if (order == null)
                        return (false, $"Order not found for INV {ordx.inv_no}", deliveryOrderIds);

                    if (order.mceasy_status == "Dikonfirmasi")
                    {
                        order.mceasy_status = "Dijadwalkan";
                    }

                    order.order_status = 1;
                    order.mceasy_is_upload = true;
                    order.jobid = newJobId;
                    order.update_user = HttpContext.Session.GetString("username") ?? "System";
                    order.update_date = DateTime.Now;
                    _context.Orders.Update(order);

                    var customerGroup = _context.CustomerGroups
                        .FirstOrDefault(g => g.SUB_CODE == order.sub_custid);

                    if (customerGroup == null)
                        return (false, "Customer group not found", deliveryOrderIds);

                    var customer = _context.Customers
                        .FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

                    if (customer == null)
                        return (false, "Customer not found", deliveryOrderIds);

                    if (customer.API_FLAG == 1 && !string.IsNullOrEmpty(order.mceasy_order_id))
                        deliveryOrderIds.Add(order.mceasy_order_id);

                    var SellRate = _context.PriceSells.FirstOrDefault(sr =>
                                        sr.cust_code == customer.MAIN_CUST
                                        && sr.origin == order.origin_id
                                        && sr.dest == order.dest_area
                                        && sr.truck_size == order.truck_size
                                        && sr.serv_type == order.serv_req
                                        && sr.serv_moda == order.moda_req
                                        && sr.charge_uom == order.uom);

                    if (SellRate == null)
                        return (false, $"Sell rate not found for INV {order.inv_no}", deliveryOrderIds);

                    var newJob = new Job
                    {
                        jobid = newJobId,
                        vendorid = CostRate.sup_code,
                        truckid = Header.truck_id,
                        drivername = Header.driver_name,
                        moda_req = order.moda_req,
                        serv_req = order.serv_req,
                        truck_size = order.truck_size,
                        flag_ep = ordx.flag_ep,
                        flag_rc = ordx.flag_rc,
                        flag_ov = ordx.flag_ov,
                        flag_cc = ordx.flag_cc,
                        flag_diffa = ordx.flag_diffa,
                        charge_uom_v = CostRate.charge_uom,
                        charge_uom_c = order.uom,
                        drop_seq = ordx.drop_seq,
                        multidrop = (byte)(Header.multidrop == true && ordx.drop_seq == 1 ? 1 : 0),
                        flag_charge = (byte)(Header.multidrop == true && ordx.drop_seq == 1 ? 1 : 0),
                        multitrip = (byte)(Header.multitrip == true ? 1 : 0),
                        ritase_seq = Header.ritase_seq,
                        job_type = Header.job_type,

                        charge_uom = CostRate.charge_uom,
                        inv_no = order.inv_no,
                        origin_id = order.origin_id,
                        dest_id = order.dest_area,
                        dvdate = Header.dvdate,

                        buy1 = CostRate.buy1,
                        buy2 = CostRate.buy2,
                        buy3 = CostRate.buy3,
                        buy_ov = CostRate.buy_ovnight,
                        buy_cc = CostRate.buy_cancel,
                        buy_rc = CostRate.buy_ret_cargo,
                        buy_ep = CostRate.buy_ret_empt,
                        buy_diffa = CostRate.buy_diff_area,
                        buy_trip2 = CostRate.buytrip2,
                        buy_trip3 = CostRate.buytrip3,

                        cust_ori = order.sub_custid,

                        sell1 = SellRate.sell1,
                        sell2 = SellRate.sell2,
                        sell3 = SellRate.sell3,
                        sell_trip2 = SellRate.selltrip2,
                        sell_trip3 = SellRate.selltrip3,
                        sell_diffa = SellRate.sell_diff_area,
                        sell_ep = SellRate.sell_ret_empty,
                        sell_rc = SellRate.sell_ret_cargo,
                        sell_ov = SellRate.sell_ovnight,
                        sell_cc = SellRate.sell_cancel,

                        entry_user = HttpContext.Session.GetString("username") ?? "System",
                        entry_date = DateTime.Now,
                        update_user = HttpContext.Session.GetString("username") ?? "System",
                        update_date = DateTime.Now
                    };

                    _context.Jobs.Add(newJob);
                }

                return (true, "OK", deliveryOrderIds);
        }


        private string GenerateJobId(int sequence)
        {
            //string prefix = _configuration["Tms:JobPrefix"];
            var prefix = _context.Configs
                .Where(x => x.key == "job-prefix")
                .Select(x => x.value)
                .FirstOrDefault();
            string year = DateTime.Now.ToString("yy");  // contoh: 25
            string month = DateTime.Now.ToString("MM"); // contoh: 07
            string sequencePart = sequence.ToString("D4"); // 0001, 0002, dst

            return $"{prefix}{year}{month}{sequencePart}";
        }

        [HttpGet]
        public IActionResult GetVendors(string originId, string destId, string truckSize, string servModa, string chargeUom)
        {
            var vendors = _context.PriceBuys
                .Where(v =>
                    v.active_flag == 1 &&
                    (string.IsNullOrEmpty(originId) || v.origin == originId) &&
                    (string.IsNullOrEmpty(destId) || v.dest == destId) &&
                    (string.IsNullOrEmpty(truckSize) || v.truck_size == truckSize) &&
                    (string.IsNullOrEmpty(servModa) || v.serv_moda == servModa) &&
                    (string.IsNullOrEmpty(chargeUom) || v.charge_uom == chargeUom)
                )
                .OrderBy(v => v.buy1)
                .Select(v => new {
                    vendor_code = v.sup_code,
                    vendor_name = v.sup_code,
                    origin = v.origin,
                    destination = v.dest,
                    service = v.serv_type,
                    moda = v.serv_moda,
                    truck_size = v.truck_size,
                    uom = v.charge_uom
                })
                .Distinct()
                .ToList();


            if (vendors.Any())
            {
                return Ok(new { success = true, data = vendors });
            } 

            return BadRequest(new { success = false, data = new List<object>(), message = "Vendor not found!" });
        }

        [HttpGet]
        public IActionResult GetDriversByVendor(string supCode)
        {
            var drivers = _context.VendorTrucks
                .Where(v =>
                    v.vehicle_active == 1 &&
                    v.sup_code == supCode
                )
                //.OrderBy(v => v.buy1)
                .Select(v => new {
                    DriverName = v.vehicle_driver,
                    TruckId = v.vehicle_no
                })
                .Distinct()
                .ToList();

            if (drivers.Any())
            {
                return Json(new { success = true, drivers });
            }

            return Json(new { success = false, drivers = new List<object>() });
        }

        [HttpGet]
        public IActionResult GetJobDetails(string jobid)
        {
            string sql = @"
            select 
                a.*,
                b.flag_cc,
                b.flag_charge,
                b.flag_diffa,
                b.flag_ep,
                b.flag_ov,
                b.flag_pu,
                b.flag_rc
            from trc_order a
            left join (
                select top 1 *
                from trc_job 
                where jobid = @jobid
                order by jobid
            ) b on a.jobid = b.jobid
            where a.jobid = @jobid";

            var details = _context.OrderForJob
                .FromSqlRaw(sql, new SqlParameter("@jobid", jobid))
                .ToList();

            return Json(new { success = true, data = details });
        }

        public async Task<IActionResult> GetOrders(string originId, string destArea, DateTime pickupDate, DateTime deliveryDate, bool multidrop)
        {

            var username = HttpContext.Session.GetString("username") ?? "System";

            var customerGroups = _context.UserXCustomers
                .Where(x => x.UserName == username)
                .Select(x => x.CustomerMain)
                .Distinct()
                .Join(_context.CustomerGroups,
                      custMain => custMain,
                      cg => cg.MAIN_CUST,
                      (custMain, cg) => cg)
                .ToList();

            var customerGroupAllowed = customerGroups
                .Select(x => x.SUB_CODE)
                .Distinct()
                .ToList();

            var allowedStatus = new[] { "Dikonfirmasi", "Dijadwalkan" };

            var query = _context.Orders
                .Where(o =>
                    o.origin_id == originId &&
                    EF.Functions.DateDiffDay(o.pickup_date, pickupDate) == 0 &&
                    allowedStatus.Contains(o.mceasy_status) &&
                    o.jobid == null &&
                    customerGroupAllowed.Contains(o.sub_custid)
                );

            if (!multidrop)
            {
                query = query.Where(o => o.dest_area == destArea);
            }

            var result = await query.ToListAsync();

            return Ok(new { success = true, data = result });
        }

        public async Task<IActionResult> GetVendor(string? originId, string? destArea, DateTime? deliveryDate)
        {

            var result = await _context.Vendors
            .Select(v => new VendorViewModel
            {
                VendorCode = v.SUP_CODE,
                VendorName = v.SUP_NAME
            })
            .ToListAsync();
            return Ok(new { success = true, data = result });
        }

        public async Task<IActionResult> SetStarted(string id)
        {
            var username = HttpContext.Session.GetString("username") ?? "System";
            try
            {
                var job = _context.JobHeaders.FirstOrDefault(j => j.jobid == id);
                if (job == null)
                    return Json(new { success = false, message = "Job not found." });

                job.status_job = "STARTED";
                job.update_user = username;
                job.update_date = DateTime.Now;
                _context.SaveChanges();

                // ─── Trigger Kirim Email ───────────────────────────────────────
                try
                {
                    // Ambil data Customer
                    var customerGroup = _context.CustomerGroups
                        .FirstOrDefault(c => c.SUB_CODE == job.cust_group);

                    // Ambil data Vendor
                    var vendor = _context.Vendors
                        .FirstOrDefault(v => v.SUP_CODE == job.vendor_plan);

                    //  List<DeliveryOrderItem> GetDeliveryOrdersByJobId

                    var deliveryDetail = GetDeliveryOrdersByJobId(job?.jobid);

                    var details = deliveryDetail.Select((item, index) => new SuratPerintahKirimDetailViewModel
                    {
                        No = index + 1,
                        ShipToParty = item.ShipToName ?? item.ShipTo ?? "-",
                        City = item.City ?? "-",
                        DateUnloading = job.deliv_date,
                        DeliveryNo = item.DO ?? "-",
                        TotalBox = item.TotalBoxKoli,
                        TotalQty = item.TotalQtyPcs,
                        Volume = item.TotalVolume
                    }).ToList();

                    // Susun ViewModel
                    var emailModel = new SuratPerintahKirimViewModel
                    {
                        NomorOrder = job.jobid ?? "-",
                        Transporter = vendor?.SUP_NAME ?? job.vendor_plan ?? "-",
                        JenisTruck = job.truck_size ?? "-",
                        NomorPolisi = job.truck_no ?? "-",
                        DriverName = job.driver_name ?? "-",
                        DriverPhone = job.driver_phone ?? "-",
                        TanggalOrder = job.entry_date,
                        TanggalMuat = job.pickup_date,
                        JamMulai = null,   // dummy, belum ada di JobHeader
                        JamSelesai = null,   // dummy, belum ada di JobHeader
                        Remarks = null,
                        Details = details
                    };

                    // ← Simpan nilai MAIN_CUST ke variable dulu sebelum query CustomerMain
                    var mainCustCode = customerGroup?.MAIN_CUST;

                    // Baru query CustomerMain pakai variable biasa
                    var customerMain = mainCustCode != null
                        ? _context.CustomerMains.FirstOrDefault(cm => cm.MAIN_CUST == mainCustCode)
                        : null;

                    var toEmails = new List<string>();
                    var ccEmails = new List<string>();

                    // To → dari TO_EMAIL di CustomerMain
                    if (!string.IsNullOrWhiteSpace(customerMain?.TO_EMAIL))
                    {
                        var emails = customerMain.TO_EMAIL
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .Where(e => !string.IsNullOrWhiteSpace(e));
                        toEmails.AddRange(emails);
                    }

                    // CC → dari CC_EMAIL di CustomerMain
                    if (!string.IsNullOrWhiteSpace(customerMain?.CC_EMAIL))
                    {
                        var emails = customerMain.CC_EMAIL
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .Where(e => !string.IsNullOrWhiteSpace(e));
                        ccEmails.AddRange(emails);
                    }

                    // CC → email user yang login
                    var currentUser = _context.Users
                        .FirstOrDefault(u => u.Username == username);
                    if (!string.IsNullOrWhiteSpace(currentUser?.Email))
                        ccEmails.Add(currentUser.Email);

                    // Skip jika tidak ada To email
                    if (!toEmails.Any())
                    {
                        Console.WriteLine($"[EMAIL SKIP] Job {id} - CustomerMain tidak memiliki TO_EMAIL.");
                    }
                    else
                    {
                        // DiNonaktifkan
                        //await _emailService.SendSuratPerintahKirimAsync(
                        //    model: emailModel,
                        //    toEmails: toEmails,
                        //    ccEmails: ccEmails.Any() ? ccEmails : null,
                        //    sentByUserId: currentUser?.Id
                        //);
                    }
                }
                catch (Exception emailEx)
                {
                    // Log sudah tersimpan di EmailLogs oleh EmailService
                    Console.WriteLine($"[EMAIL ERROR] {emailEx.Message}");
                }
                // ──────────────────────────────────────────────────────────────

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public async Task<IActionResult> SetClosed(string id)
        {
            var username = HttpContext.Session.GetString("username") ?? "System";
            try
            {
                var job = _context.JobHeaders.FirstOrDefault(j => j.jobid == id);
                if (job == null)
                    return Json(new { success = false, message = "Job not found." });

                job.status_job = "CLOSED";
                job.update_user = username;
                job.update_date = DateTime.Now;
                _context.SaveChanges();

                // ─── Trigger Kirim Email ───────────────────────────────────────
                try
                {
                    // Ambil data Customer
                    var customerGroup = _context.CustomerGroups
                        .FirstOrDefault(c => c.SUB_CODE == job.cust_group);

                    // Ambil data Vendor
                    var vendor = _context.Vendors
                        .FirstOrDefault(v => v.SUP_CODE == job.vendor_plan);

                    //  List<DeliveryOrderItem> GetDeliveryOrdersByJobId

                    var deliveryDetail = GetDeliveryOrdersByJobId(job?.jobid);

                    var details = deliveryDetail.Select((item, index) => new SuratPerintahKirimDetailViewModel
                    {
                        No = index + 1,
                        ShipToParty = item.ShipToName ?? item.ShipTo ?? "-",
                        City = item.City ?? "-",
                        DateUnloading = job.deliv_date,
                        DeliveryNo = item.DO ?? "-",
                        TotalBox = item.TotalBoxKoli,
                        TotalQty = item.TotalQtyPcs,
                        Volume = item.TotalVolume
                    }).ToList();

                    // Susun ViewModel
                    var emailModel = new SuratPerintahKirimViewModel
                    {
                        NomorOrder = job.jobid ?? "-",
                        Transporter = vendor?.SUP_NAME ?? job.vendor_plan ?? "-",
                        JenisTruck = job.truck_size ?? "-",
                        NomorPolisi = job.truck_no ?? "-",
                        DriverName = job.driver_name ?? "-",
                        DriverPhone = job.driver_phone ?? "-",
                        TanggalOrder = job.entry_date,
                        TanggalMuat = job.pickup_date,
                        JamMulai = null,   // dummy, belum ada di JobHeader
                        JamSelesai = null,   // dummy, belum ada di JobHeader
                        Remarks = null,
                        Details = details
                    };

                    // ← Simpan nilai MAIN_CUST ke variable dulu sebelum query CustomerMain
                    var mainCustCode = customerGroup?.MAIN_CUST;

                    // Baru query CustomerMain pakai variable biasa
                    var customerMain = mainCustCode != null
                        ? _context.CustomerMains.FirstOrDefault(cm => cm.MAIN_CUST == mainCustCode)
                        : null;

                    var toEmails = new List<string>();
                    var ccEmails = new List<string>();

                    // To → dari TO_EMAIL di CustomerMain
                    if (!string.IsNullOrWhiteSpace(customerMain?.TO_EMAIL))
                    {
                        var emails = customerMain.TO_EMAIL
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .Where(e => !string.IsNullOrWhiteSpace(e));
                        toEmails.AddRange(emails);
                    }

                    // CC → dari CC_EMAIL di CustomerMain
                    if (!string.IsNullOrWhiteSpace(customerMain?.CC_EMAIL))
                    {
                        var emails = customerMain.CC_EMAIL
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .Where(e => !string.IsNullOrWhiteSpace(e));
                        ccEmails.AddRange(emails);
                    }

                    // CC → email user yang login
                    var currentUser = _context.Users
                        .FirstOrDefault(u => u.Username == username);
                    if (!string.IsNullOrWhiteSpace(currentUser?.Email))
                        ccEmails.Add(currentUser.Email);

                    // Skip jika tidak ada To email
                    if (!toEmails.Any())
                    {
                        Console.WriteLine($"[EMAIL SKIP] Job {id} - CustomerMain tidak memiliki TO_EMAIL.");
                    }
                    else
                    {
                        //Fungsi Email DiNonaktifkan
                        //await _emailService.SendSuratPerintahKirimAsync(
                        //    model: emailModel,
                        //    toEmails: toEmails,
                        //    ccEmails: ccEmails.Any() ? ccEmails : null,
                        //    sentByUserId: currentUser?.Id
                        //);
                    }
                }
                catch (Exception emailEx)
                {
                    // Log sudah tersimpan di EmailLogs oleh EmailService
                    Console.WriteLine($"[EMAIL ERROR] {emailEx.Message}");
                }
                // ──────────────────────────────────────────────────────────────

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("Job/BulkJobPod")]
        public IActionResult BulkJobPod([FromBody] List<int> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                return Json(new { success = false, message = "No data received" });
            }

            // Simpan ke TempData / Session untuk halaman edit
            HttpContext.Session.SetString(
                "BulkOrderIds",
                string.Join(",", orderIds)
            );

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("JobPod", "Job")
            });
        }

        public IActionResult JobPod()
        {
            var ids = HttpContext.Session.GetString("BulkOrderIds");

            if (string.IsNullOrEmpty(ids))
                return Content("SESSION KOSONG");

            var orderIdList = ids.Split(',').Select(int.Parse).ToList();

            // ===== HEADER =====
            var headers = _context.JobHeaders
                .Where(h => orderIdList.Contains(h.id_seq))
                .Select(h => new {
                    h.jobid,
                    h.deliv_date,
                    h.origin,
                    h.dest,
                    h.vendor_act,
                    h.status_job
                })
                .ToList();

            var jobIds = headers.Select(x => x.jobid).ToList();

            // ===== DETAIL =====
            var details =
            (from j in _context.Jobs
             join p in _context.JobPODs on j.inv_no equals p.inv_no into podGroup
             from pod in podGroup.DefaultIfEmpty()
             join o in _context.Orders on j.inv_no equals o.inv_no into orderGroup
             from order in orderGroup.DefaultIfEmpty()
             where jobIds.Contains(j.jobid)
             select new
             {
                 j.jobid,
                 j.inv_no,
                 order.cnee_code,
                 outorigin_date = pod.outorigin_date == null
                     ? null
                     : pod.outorigin_date.Value.ToString("yyyy-MM-dd"),

                 arriv_date = pod.arriv_date == null
                     ? null
                     : pod.arriv_date.Value.ToString("yyyy-MM-dd"),

                 pod_ret_date = pod.pod_ret_date == null
                     ? null
                     : pod.pod_ret_date.Value.ToString("yyyy-MM-dd"),

                 pod_send_date = pod.pod_send_date == null
                     ? null
                     : pod.pod_send_date.Value.ToString("yyyy-MM-dd"),

                 pod.outorigin_time,
                 pod.arriv_time,
                 pod.arriv_pic,
                 pod.pod_ret_time,
                 pod.pod_ret_pic,
                 pod.pod_send_time,
                 pod.pod_send_pic,
                 pod.pod_status,
                 pod.spd_no,
                 pod.pod_remark
             }).ToList();


            ViewBag.Headers = headers;
            ViewBag.Details = details;

            return View("JobPod");
        }


        public IActionResult BulkJobPodByCnee([FromBody] List<int> orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                return Json(new { success = false, message = "No data received" });
            }

            // Simpan ke TempData / Session untuk halaman edit
            HttpContext.Session.SetString(
                "BulkOrderIds",
                string.Join(",", orderIds)
            );

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("JobPodByCnee", "Job")
            });
        }

        public IActionResult JobPodByCnee()
        {
            var ids = HttpContext.Session.GetString("BulkOrderIds");

            if (string.IsNullOrEmpty(ids))
                return Content("SESSION KOSONG");

            var orderIdList = ids.Split(',').Select(int.Parse).ToList();

            // ===== HEADER =====
            var headers = _context.JobHeaders
                .Where(h => orderIdList.Contains(h.id_seq))
                .Select(h => new {
                    h.jobid,
                    h.deliv_date,
                    h.origin,
                    h.dest,
                    h.vendor_act,
                    h.status_job
                })
                .ToList();

            var jobIds = headers.Select(x => x.jobid).ToList();

            // ===== DETAIL =====
            var details =
            (from j in _context.Jobs
             join p in _context.JobPODs on j.inv_no equals p.inv_no into podGroup
             from pod in podGroup.DefaultIfEmpty()
             join o in _context.Orders on j.inv_no equals o.inv_no into orderGroup
             from order in orderGroup.DefaultIfEmpty()
             where jobIds.Contains(j.jobid)
             select new
             {
                 j.jobid,
                 j.inv_no,
                 order.cnee_code,
                 outorigin_date = pod.outorigin_date == null
                     ? null
                     : pod.outorigin_date.Value.ToString("yyyy-MM-dd"),

                 arriv_date = pod.arriv_date == null
                     ? null
                     : pod.arriv_date.Value.ToString("yyyy-MM-dd"),

                 pod_ret_date = pod.pod_ret_date == null
                     ? null
                     : pod.pod_ret_date.Value.ToString("yyyy-MM-dd"),

                 pod_send_date = pod.pod_send_date == null
                     ? null
                     : pod.pod_send_date.Value.ToString("yyyy-MM-dd"),

                 pod.outorigin_time,
                 pod.arriv_time,
                 pod.arriv_pic,
                 pod.pod_ret_time,
                 pod.pod_ret_pic,
                 pod.pod_send_time,
                 pod.pod_send_pic,
                 pod.pod_status,
                 pod.spd_no,
                 pod.pod_remark
             }).ToList()
             .GroupBy(x => new { x.cnee_code, x.outorigin_date })
             .Select(y => new {
                 jobid = y.First().jobid,
                 cnee_code = y.First().cnee_code,
                 outorigin_date = y.First().outorigin_date,
                 outorigin_time = y.First().outorigin_time,
                 arriv_date = y.First().arriv_date,
                 arriv_time = y.First().arriv_time,
                 arriv_pic = y.First().arriv_pic,
                 pod_ret_date = y.First().pod_ret_date,
                 pod_ret_time = y.First().pod_ret_time,
                 pod_ret_pic = y.First().pod_ret_pic,
                 pod_send_date = y.First().pod_send_date,
                 pod_send_time = y.First().pod_send_time,
                 pod_send_pic = y.First().pod_send_pic,
                 pod_status = y.First().pod_status,
                 spd_no = y.First().spd_no,
                 pod_remark = y.First().pod_remark,
             }).ToList();


            ViewBag.Headers = headers;
            ViewBag.Details = details;

            return View("JobPodByCnee");
        }

        [HttpPost]
        public IActionResult SavePod([FromBody] List<JobPOD> data)
        {
            if (data == null || data.Count == 0)
                return Json(new { success = false, message = "Data kosong" });

            var username = HttpContext.Session.GetString("username") ?? "System";

            foreach (var item in data)
            {
                // 🔎 CEK APAKAH DATA SUDAH ADA (UNTUK EDIT)
                var existing = _context.JobPODs
                    .FirstOrDefault(x => x.jobid == item.jobid && x.inv_no == item.inv_no);

                if (existing != null)
                {
                    // ===== UPDATE =====
                    existing.outorigin_date = item.outorigin_date;
                    existing.outorigin_time = item.outorigin_time;
                    existing.arriv_date = item.arriv_date;
                    existing.arriv_time = item.arriv_time;
                    existing.arriv_pic = item.arriv_pic;
                    existing.pod_ret_date = item.pod_ret_date;
                    existing.pod_ret_time = item.pod_ret_time;
                    existing.pod_ret_pic = item.pod_ret_pic;
                    existing.pod_send_date = item.pod_send_date;
                    existing.pod_send_time = item.pod_send_time;
                    existing.pod_send_pic = item.pod_send_pic;
                    existing.pod_status = item.pod_status;
                    existing.spd_no = item.spd_no;
                    existing.pod_remark = item.pod_remark;

                    existing.update_user = username;
                    existing.update_date = DateTime.Now;
                    existing.input_method = "BY_ORDER";
                }
                else
                {
                    // ===== INSERT =====
                    var model = new JobPOD
                    {
                        jobid = item.jobid,
                        inv_no = item.inv_no,
                        outorigin_date = item.outorigin_date,
                        outorigin_time = item.outorigin_time,
                        arriv_date = item.arriv_date,
                        arriv_time = item.arriv_time,
                        arriv_pic = item.arriv_pic,
                        pod_ret_date = item.pod_ret_date,
                        pod_ret_time = item.pod_ret_time,
                        pod_ret_pic = item.pod_ret_pic,
                        pod_send_date = item.pod_send_date,
                        pod_send_time = item.pod_send_time,
                        pod_send_pic = item.pod_send_pic,
                        pod_status = item.pod_status,
                        spd_no = item.spd_no,
                        pod_remark = item.pod_remark,
                        entry_user = username,
                        entry_date = DateTime.Now,
                        input_method = "BY_ORDER"
                    };

                    _context.JobPODs.Add(model);
                }
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult SavePodByCnee([FromBody] List<JobPOD> data)
        {
            if (data == null || data.Count == 0)
                return Json(new { success = false, message = "Data kosong" });

            var username = HttpContext.Session.GetString("username") ?? "System";

            // 1. Group input by cnee_code
            var groupedByCnee = data.GroupBy(x => x.cnee_code);

            foreach (var cneeGroup in groupedByCnee)
            {
                var cneeCode = cneeGroup.Key;
                var podData = cneeGroup.First(); // data POD yang akan di-apply ke semua inv

                // 2. Cari semua inv_no yang punya cnee_code ini di Orders
                var invNos = _context.Orders
                    .Where(o => o.cnee_code == cneeCode)
                    .Select(o => o.inv_no)
                    .ToList();

                // Ambil semua jobid dari payload untuk cnee ini
                var jobIdsInPayload = cneeGroup.Select(x => x.jobid).ToList();

                // 3. Loop per inv_no, insert/update JobPOD
                foreach (var inv_no in invNos)
                {

                    // Ambil semua job yang match inv_no DAN jobid ada di payload
                    var jobs = _context.Jobs
                        .Where(j => j.inv_no == inv_no && jobIdsInPayload.Contains(j.jobid))
                        .ToList();

                    foreach (var job in jobs)
                    {
                        var existing = _context.JobPODs
                            .FirstOrDefault(x => x.jobid == job.jobid && x.inv_no == job.inv_no);

                        if (existing != null)
                        {
                            // UPDATE
                            existing.outorigin_date = podData.outorigin_date;
                            existing.outorigin_time = podData.outorigin_time;
                            existing.arriv_date = podData.arriv_date;
                            existing.arriv_time = podData.arriv_time;
                            existing.arriv_pic = podData.arriv_pic;
                            existing.pod_ret_date = podData.pod_ret_date;
                            existing.pod_ret_time = podData.pod_ret_time;
                            existing.pod_ret_pic = podData.pod_ret_pic;
                            existing.pod_send_date = podData.pod_send_date;
                            existing.pod_send_time = podData.pod_send_time;
                            existing.pod_send_pic = podData.pod_send_pic;
                            existing.pod_status = podData.pod_status;
                            existing.spd_no = podData.spd_no;
                            existing.pod_remark = podData.pod_remark;
                            existing.update_user = username;
                            existing.update_date = DateTime.Now;
                        }
                        else
                        {
                            // INSERT
                            var model = new JobPOD
                            {
                                jobid = job.jobid,
                                inv_no = job.inv_no,
                                outorigin_date = podData.outorigin_date,
                                outorigin_time = podData.outorigin_time,
                                arriv_date = podData.arriv_date,
                                arriv_time = podData.arriv_time,
                                arriv_pic = podData.arriv_pic,
                                pod_ret_date = podData.pod_ret_date,
                                pod_ret_time = podData.pod_ret_time,
                                pod_ret_pic = podData.pod_ret_pic,
                                pod_send_date = podData.pod_send_date,
                                pod_send_time = podData.pod_send_time,
                                pod_send_pic = podData.pod_send_pic,
                                pod_status = podData.pod_status,
                                spd_no = podData.spd_no,
                                pod_remark = podData.pod_remark,
                                entry_user = username,
                                entry_date = DateTime.Now
                            };
                            _context.JobPODs.Add(model);
                        }
                    }

                    // Cari jobid dari Jobs
                    //var job = _context.Jobs
                    //    .FirstOrDefault(j => j.inv_no == inv_no);

                    // Cari job yang inv_no-nya match DAN jobid-nya ada di payload
                    //var job = _context.Jobs
                    //    .FirstOrDefault(j => j.inv_no == inv_no && jobIdsInPayload.Contains(j.jobid));

                    //if (job == null) continue;

                    //var existing = _context.JobPODs
                    //    .FirstOrDefault(x => x.jobid == job.jobid && x.inv_no == inv_no);

                    //if (existing != null)
                    //{
                    //    // ===== UPDATE =====
                    //    existing.outorigin_date = podData.outorigin_date;
                    //    existing.outorigin_time = podData.outorigin_time;
                    //    existing.arriv_date = podData.arriv_date;
                    //    existing.arriv_time = podData.arriv_time;
                    //    existing.arriv_pic = podData.arriv_pic;
                    //    existing.pod_ret_date = podData.pod_ret_date;
                    //    existing.pod_ret_time = podData.pod_ret_time;
                    //    existing.pod_ret_pic = podData.pod_ret_pic;
                    //    existing.pod_send_date = podData.pod_send_date;
                    //    existing.pod_send_time = podData.pod_send_time;
                    //    existing.pod_send_pic = podData.pod_send_pic;
                    //    existing.pod_status = podData.pod_status;
                    //    existing.spd_no = podData.spd_no;
                    //    existing.pod_remark = podData.pod_remark;
                    //    existing.update_user = username;
                    //    existing.update_date = DateTime.Now;
                    //    existing.input_method = "BY_CNEE";
                    //}
                    //else
                    //{
                    //    // ===== INSERT =====
                    //    var model = new JobPOD
                    //    {
                    //        jobid = job.jobid,
                    //        inv_no = inv_no,
                    //        outorigin_date = podData.outorigin_date,
                    //        outorigin_time = podData.outorigin_time,
                    //        arriv_date = podData.arriv_date,
                    //        arriv_time = podData.arriv_time,
                    //        arriv_pic = podData.arriv_pic,
                    //        pod_ret_date = podData.pod_ret_date,
                    //        pod_ret_time = podData.pod_ret_time,
                    //        pod_ret_pic = podData.pod_ret_pic,
                    //        pod_send_date = podData.pod_send_date,
                    //        pod_send_time = podData.pod_send_time,
                    //        pod_send_pic = podData.pod_send_pic,
                    //        pod_status = podData.pod_status,
                    //        spd_no = podData.spd_no,
                    //        pod_remark = podData.pod_remark,
                    //        entry_user = username,
                    //        entry_date = DateTime.Now,
                    //        input_method = "BY_CNEE"
                    //    };
                    //    _context.JobPODs.Add(model);
                    //}
                }
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        public async Task<IActionResult> PrintSPK(string jobid, bool preview = false)
        {
            if (string.IsNullOrEmpty(jobid))
            {
                return NotFound("Job ID tidak ditemukan");
            }

            // Ambil data dari database berdasarkan JobId
            var jobData = GetJobDataByJobId(jobid);

            if (jobData == null)
            {
                return NotFound($"Job dengan ID {jobid} tidak ditemukan");
            }


            // Prepare view model untuk SPK
            var spkViewModel = new SPKViewModel
            {
                NoSPK = jobData.JobId,
                TglOrder = jobData.DelivDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd"),
                TipeOrder = jobData.ServiceType,
                JenisTruck = jobData.TruckSize ?? "",
                TglMuat = jobData.DelivDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd"),
                JamMulaiMuat = "",
                JamSelesaiMuat = "",
                Transporter = jobData.VendorPlan,
                NamaSopir = jobData.DriverName,
                NoPol = jobData.TruckNo,
                TglTiba = "",
                JamTiba = "",
                Catatan = "",

                // Kelengkapan Dokumen
                KelengkapanDokumen = new KelengkapanDokumen
                {
                    Berangkat = true,
                    Kembali = false
                },

                // List Delivery Orders
                DeliveryOrders = GetDeliveryOrdersByJobId(jobid),
                IsPreview = preview,
            };

            var jobHeader = _context.JobHeaders.FirstOrDefault(j => j.jobid == jobid);
            var custGroup = _context.CustomerGroups.FirstOrDefault(cg => cg.SUB_CODE == jobHeader.cust_group);
            if (jobHeader != null)
            {
                if (jobHeader != null && !preview)
                {
                    jobHeader.spk_print_count = (jobHeader.spk_print_count ?? 0) + 1;
                    _context.SaveChanges();

                    var emailVendors = await _context.VendorTruckEmails
                    .Where(vte => vte.sup_code == jobHeader.vendor_act && vte.is_active == 1)
                    .ToListAsync();

                    var emailTo = string.Join(",", emailVendors
                        .Where(e => e.email_type == "TO")
                        .Select(e => e.email_address));

                    var emailCc = string.Join(",", emailVendors
                        .Where(e => e.email_type == "CC")
                        .Select(e => e.email_address));


                    // Ambil email dari CustomerMain
                    if (custGroup != null)
                    {
                        var customerMain = await _context.CustomerMains
                            .FirstOrDefaultAsync(c => c.MAIN_CUST == custGroup.MAIN_CUST);

                        if (customerMain != null)
                        {
                            if (!string.IsNullOrWhiteSpace(customerMain.TO_EMAIL))
                                emailTo = string.Join(",", new[] { emailTo, customerMain.TO_EMAIL }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                            if (!string.IsNullOrWhiteSpace(customerMain.CC_EMAIL))
                                emailCc = string.Join(",", new[] { emailCc, customerMain.CC_EMAIL }
                                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                        }
                    }


                    _mailer.TriggerEvent("spk.printed", new Dictionary<string, string>
                    {

                        ["nomor_spk"] = jobid,
                        ["customer"] = custGroup != null ? custGroup.MAIN_CUST : "UNKNOWN",
                        ["vendor_name"] = jobHeader.vendor_act,
                        ["tanggal"] = DateTime.UtcNow.ToString("dd MMMM yyyy"),
                        ["order_id"] = jobid,
                        ["email_to"] = emailTo,
                        ["email_cc"] = emailCc
                    }, HttpContext.Session.GetString("username") ?? "System");

                }
            }

            return View(spkViewModel);
        }

        // Method helper untuk mengambil data Job dari database
        private JobSummaryViewModel GetJobDataByJobId(string jobId)
        {
            // TODO: Replace with actual database query
            // Contoh implementasi:
            //return _context.JobHeaders.FirstOrDefault(j => j.jobid == jobId);

            var jobHeader = _context.JobHeaders.FirstOrDefault(or => or.jobid == jobId);
            if (jobHeader == null)
            {
                return new JobSummaryViewModel
                {
                    JobId = jobId,
                    TruckNo = "0000",
                    DelivDate = DateTime.Parse("2026-02-07"),
                    Origin = "JAKARTA",
                    Dest = "MALANG",
                    VendorPlan = "YUSEN",
                    MCStatus = "CONFIRMED"
                };
            }

            // Sementara return dummy data untuk testing
            return new JobSummaryViewModel
            {
                JobId = jobId,
                TruckNo = jobHeader.truck_no,
                TruckSize = jobHeader.truck_size,
                DelivDate = jobHeader.deliv_date,
                Origin = jobHeader.origin,
                Dest = jobHeader.dest,
                VendorPlan = jobHeader.vendor_act,
                DriverName = jobHeader.driver_name,
                ServiceType = jobHeader.serv_type,
                MCStatus = "CONFIRMED"
            };
        }

        // Method helper untuk mengambil Delivery Orders berdasarkan JobId
        private List<DeliveryOrderItem> GetDeliveryOrdersByJobId(string jobId)
        {

            // Query menggunakan Entity Framework dengan Join
            var deliveryOrders = (from job in _context.Jobs
                                  join order in _context.Orders on job.inv_no equals order.inv_no into orderGroup
                                  from order in orderGroup.DefaultIfEmpty()
                                  join geofence in _context.Geofences on order.cnee_code equals geofence.FenceName into geoGroup
                                  from geofence in geoGroup.DefaultIfEmpty()
                                  join orderDtl in _context.OrderDetails on order.inv_no equals orderDtl.inv_no into dtlGroup
                                  from orderDtl in dtlGroup.DefaultIfEmpty()
                                  where job.jobid == jobId
                                  group new { job, order, geofence, orderDtl } by new
                                  {
                                      job.jobid,
                                      job.inv_no,
                                      ShipTo = order.ship_to_name,
                                      ShipToAddress = order.ship_to_address,
                                      City = order.dest_area,
                                  } into g
                                  select new DeliveryOrderItem
                                  {
                                      ShipTo = g.Key.ShipToAddress ?? "",
                                      ShipToName = g.Key.ShipTo ?? "",
                                      City = g.Key.City ?? "",
                                      DO = g.Key.inv_no ?? "",
                                      TotalBoxKoli = g.Sum(x => x.orderDtl != null ? x.orderDtl.item_qty ?? 0 : 0),
                                      TotalQtyPcs = g.Sum(x => x.orderDtl != null ? x.orderDtl.unit_qty ?? 0 : 0),
                                      TotalVolume = 0
                                  })
                                 .ToList();

            return deliveryOrders;

        }


        // ================================================================
        // TAMBAHKAN METHOD-METHOD INI KE DALAM JobController
        // ================================================================

        // ------------------------------------------------------------------
        // 1. SEARCH SHIPTO
        //    GET /Job/SearchShipTo?q=keyword&delivDate=yyyy-MM-dd
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult SearchShipTo(string? q, string? delivDate)
        {
            var username = HttpContext.Session.GetString("username") ?? "System";

            var allowedCustomers = _context.UserXCustomers
                .Where(x => x.UserName == username)
                .Select(x => x.CustomerMain)
                .Distinct()
                .ToList();

            var allowedSubCodes = _context.CustomerGroups
                .Where(cg => allowedCustomers.Contains(cg.MAIN_CUST))
                .Select(cg => cg.SUB_CODE)
                .Distinct()
                .ToList();

            var query = _context.Orders
                .Where(o =>
                    o.jobid != null &&
                    allowedSubCodes.Contains(o.sub_custid)
                );

            if (!string.IsNullOrEmpty(delivDate) && DateTime.TryParse(delivDate, out var parsedDate))
            {
                query = query.Where(o => EF.Functions.DateDiffDay(o.delivery_date, parsedDate) == 0);
            }

            if (!string.IsNullOrEmpty(q))
            {
                var keyword = q.ToLower();
                query = query.Where(o =>
                    (o.ship_to_name != null && o.ship_to_name.ToLower().Contains(keyword)) ||
                    (o.ship_to_address != null && o.ship_to_address.ToLower().Contains(keyword)) ||
                    (o.ship_to_city != null && o.ship_to_city.ToLower().Contains(keyword))
                );
            }

            var grouped = query
                .GroupBy(o => new
                {
                    o.ship_to_name,
                    o.ship_to_address,
                    o.ship_to_city
                })
                .Select(g => new
                {
                    ship_to_name = g.Key.ship_to_name,
                    ship_to_address = g.Key.ship_to_address,
                    ship_to_city = g.Key.ship_to_city,
                    total_order = g.Count(),
                    job_ids = g.Select(o => o.jobid).Distinct().ToList(),
                    delivery_dates = g.Select(o => o.delivery_date).Distinct().ToList(),
                })
                .OrderBy(g => g.ship_to_name)
                .Take(50)
                .ToList();

            return Ok(new { success = true, data = grouped });
        }

        // ------------------------------------------------------------------
        // 2. SIMPAN CONTEXT KE SESSION → REDIRECT KE HALAMAN POD
        //    POST /Job/OpenPodByShipTo
        // ------------------------------------------------------------------
        [HttpPost]
        public IActionResult OpenPodByShipTo([FromBody] OpenPodByShipToRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.ShipToName))
                return BadRequest(new { success = false, message = "ShipTo tidak valid" });

            HttpContext.Session.SetString("PodShipToName", req.ShipToName ?? "");
            HttpContext.Session.SetString("PodShipToAddress", req.ShipToAddress ?? "");
            HttpContext.Session.SetString("PodShipToCity", req.ShipToCity ?? "");
            HttpContext.Session.SetString("PodDelivDate", req.DelivDate ?? "");

            return Ok(new
            {
                success = true,
                redirectUrl = Url.Action("JobPodByShipTo", "Job")
            });
        }

        // ------------------------------------------------------------------
        // 3. HALAMAN POD BY SHIPTO
        //    GET /Job/JobPodByShipTo
        // ------------------------------------------------------------------
        public IActionResult JobPodByShipTo()
        {
            var shipToName = HttpContext.Session.GetString("PodShipToName") ?? "";
            var shipToAddress = HttpContext.Session.GetString("PodShipToAddress") ?? "";
            var shipToCity = HttpContext.Session.GetString("PodShipToCity") ?? "";
            var delivDateStr = HttpContext.Session.GetString("PodDelivDate") ?? "";

            if (string.IsNullOrEmpty(shipToName))
                return RedirectToAction("Index");

            // Clear session setelah dibaca
            HttpContext.Session.Remove("PodShipToName");
            HttpContext.Session.Remove("PodShipToAddress");
            HttpContext.Session.Remove("PodShipToCity");
            HttpContext.Session.Remove("PodDelivDate");

            var ordersQuery = _context.Orders
                .Where(o =>
                    o.jobid != null &&
                    o.ship_to_name == shipToName &&
                    o.ship_to_address == shipToAddress &&
                    o.ship_to_city == shipToCity
                );

            if (!string.IsNullOrEmpty(delivDateStr) && DateTime.TryParse(delivDateStr, out var delivDate))
            {
                ordersQuery = ordersQuery.Where(o => EF.Functions.DateDiffDay(o.delivery_date, delivDate) == 0);
            }

            var orders = ordersQuery.ToList();

            if (!orders.Any())
                return RedirectToAction("Index");

            var jobIds = orders.Select(o => o.jobid).Distinct().ToList();
            var invNos = orders.Select(o => o.inv_no).Distinct().ToList();

            var headers = _context.JobHeaders
                .Where(h => jobIds.Contains(h.jobid))
                .Select(h => new
                {
                    h.jobid,
                    h.deliv_date,
                    h.origin,
                    h.dest,
                    h.vendor_act,
                    h.truck_no,
                    h.driver_name,
                    h.status_job
                })
                .ToList();

            var details = (
                from o in _context.Orders
                join pod in _context.JobPODs
                    on new { jobid = o.jobid, inv_no = o.inv_no }
                    equals new { jobid = pod.jobid, inv_no = pod.inv_no }
                    into podGroup
                from pod in podGroup.DefaultIfEmpty()
                where invNos.Contains(o.inv_no) && jobIds.Contains(o.jobid)
                orderby o.jobid, o.inv_no
                select new PodDetailViewModel
                {
                    jobid = o.jobid,
                    inv_no = o.inv_no,
                    ship_to_name = o.ship_to_name,
                    ship_to_address = o.ship_to_address,
                    ship_to_city = o.ship_to_city,
                    delivery_date = o.delivery_date,

                    outorigin_date = pod != null ? pod.outorigin_date : null,
                    outorigin_time = pod != null ? pod.outorigin_time : null,
                    arriv_date = pod != null ? pod.arriv_date : null,
                    arriv_time = pod != null ? pod.arriv_time : null,
                    arriv_pic = pod != null ? pod.arriv_pic : null,
                    pod_ret_date = pod != null ? pod.pod_ret_date : null,
                    pod_ret_time = pod != null ? pod.pod_ret_time : null,
                    pod_ret_pic = pod != null ? pod.pod_ret_pic : null,
                    pod_send_date = pod != null ? pod.pod_send_date : null,
                    pod_send_time = pod != null ? pod.pod_send_time : null,
                    pod_send_pic = pod != null ? pod.pod_send_pic : null,
                    pod_status = pod != null ? pod.pod_status : null,
                    spd_no = pod != null ? pod.spd_no : null,
                    pod_remark = pod != null ? pod.pod_remark : null,
                }
            ).ToList();

            ViewBag.Headers = headers;
            ViewBag.Details = details;
            ViewBag.ShipToName = shipToName;
            ViewBag.ShipToAddress = shipToAddress;
            ViewBag.ShipToCity = shipToCity;

            return View("JobPodByShipTo");
        }

        // ------------------------------------------------------------------
        // 4. SAVE POD — fix dari SavePod lama:
        //    - tambah [ValidateAntiForgeryToken]
        //    - hanya upsert row yang ada perubahannya (dirty check)
        //    - return pesan error detail
        //    POST /Job/SavePod  (replace method lama)
        // ------------------------------------------------------------------
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult SavePod([FromBody] List<JobPOD> data)
        //{
        //    if (data == null || data.Count == 0)
        //        return Json(new { success = false, message = "Data kosong" });

        //    var username = HttpContext.Session.GetString("username") ?? "System";
        //    var savedCount = 0;

        //    foreach (var item in data)
        //    {
        //        if (string.IsNullOrEmpty(item.jobid) || string.IsNullOrEmpty(item.inv_no))
        //            continue;

        //        var existing = _context.JobPODs
        //            .FirstOrDefault(x => x.jobid == item.jobid && x.inv_no == item.inv_no);

        //        if (existing != null)
        //        {
        //            existing.outorigin_date = item.outorigin_date;
        //            existing.outorigin_time = item.outorigin_time;
        //            existing.arriv_date = item.arriv_date;
        //            existing.arriv_time = item.arriv_time;
        //            existing.arriv_pic = item.arriv_pic;
        //            existing.pod_ret_date = item.pod_ret_date;
        //            existing.pod_ret_time = item.pod_ret_time;
        //            existing.pod_ret_pic = item.pod_ret_pic;
        //            existing.pod_send_date = item.pod_send_date;
        //            existing.pod_send_time = item.pod_send_time;
        //            existing.pod_send_pic = item.pod_send_pic;
        //            existing.pod_status = item.pod_status;
        //            existing.spd_no = item.spd_no;
        //            existing.pod_remark = item.pod_remark;
        //            existing.update_user = username;
        //            existing.update_date = DateTime.Now;
        //        }
        //        else
        //        {
        //            _context.JobPODs.Add(new JobPOD
        //            {
        //                jobid = item.jobid,
        //                inv_no = item.inv_no,
        //                outorigin_date = item.outorigin_date,
        //                outorigin_time = item.outorigin_time,
        //                arriv_date = item.arriv_date,
        //                arriv_time = item.arriv_time,
        //                arriv_pic = item.arriv_pic,
        //                pod_ret_date = item.pod_ret_date,
        //                pod_ret_time = item.pod_ret_time,
        //                pod_ret_pic = item.pod_ret_pic,
        //                pod_send_date = item.pod_send_date,
        //                pod_send_time = item.pod_send_time,
        //                pod_send_pic = item.pod_send_pic,
        //                pod_status = item.pod_status,
        //                spd_no = item.spd_no,
        //                pod_remark = item.pod_remark,
        //                entry_user = username,
        //                entry_date = DateTime.Now
        //            });
        //        }

        //        savedCount++;
        //    }

        //    try
        //    {
        //        _context.SaveChanges();
        //        return Json(new { success = true, message = $"{savedCount} data berhasil disimpan" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = $"Gagal menyimpan: {ex.Message}" });
        //    }
        //}

        // ------------------------------------------------------------------
        // FIX: SetStarted dan SetClosed — tambah [HttpPost]
        // (sudah ada di controller, tinggal tambah attribute)
        // ------------------------------------------------------------------
        // [HttpPost]  ← tambahkan ini di atas method SetStarted
        // [HttpPost]  ← tambahkan ini di atas method SetClosed



    }
    public class OrderForJob {
        public int id_seq { get; set; }

        [StringLength(50)]
        public string? wh_code { get; set; }

        [StringLength(50)]
        public string? sub_custid { get; set; }

        [StringLength(50)]
        public string? cnee_code { get; set; }

        [StringLength(50)]
        public string? inv_no { get; set; }

        public DateTime? delivery_date { get; set; }

        public DateTime? pickup_date { get; set; }


        [StringLength(50)]
        public string? origin_id { get; set; }

        [StringLength(50)]
        public string? dest_area { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal? tot_pkgs { get; set; }

        [StringLength(10)]
        public string? uom { get; set; }

        public int? pallet_consume { get; set; }

        public int? pallet_delivery { get; set; }

        [StringLength(50)]
        public string? si_no { get; set; }

        public DateTime? do_rcv_date { get; set; }

        [StringLength(10)]
        public string? do_rcv_time { get; set; }

        [StringLength(10)]
        public string? moda_req { get; set; }

        [StringLength(10)]
        public string? serv_req { get; set; }

        [StringLength(50)]
        public string? truck_size { get; set; }

        [StringLength(50)]
        public string? remark { get; set; }

        public byte? order_status { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }

        [StringLength(50)]
        public string? jobid { get; set; }

        public int? total_pkgs { get; set; }

        [StringLength(50)]
        public string? mceasy_order_id { get; set; }

        [StringLength(50)]
        public string? mceasy_do_number { get; set; }

        public int? mceasy_origin_address_id { get; set; }
        public int? mceasy_destination_address_id { get; set; }

        [StringLength(50)]
        public string? mceasy_origin_name { get; set; }
        [StringLength(50)]
        public string? mceasy_dest_name { get; set; }

        [StringLength(20)]
        public string? mceasy_status { get; set; }

        public bool? mceasy_is_upload { get; set; } = false;

        public byte? flag_pu { get; set; }
        public byte? flag_diffa { get; set; }
        public byte? flag_ep { get; set; }
        public byte? flag_rc { get; set; }
        public byte? flag_ov { get; set; }
        public byte? flag_cc { get; set; }
        public byte? flag_charge { get; set; }
    }
    public class JobOrder
    {
        public string? OrderID { get; set; }
    }
    public class JobSummaryViewModel
    {
        public int IdSeq { get; set; }
        public string? JobId { get; set; }
        public string? CustomerMain { get; set; }
        public string? TruckNo { get; set; }
        public string? TruckSize { get; set; }
        public DateTime? DelivDate { get; set; }
        public string? Origin { get; set; }
        public string? Dest { get; set; }
        public string? VendorPlan { get; set; }
        public string? DriverName { get; set; }
        public string? ServiceType { get; set; }
        public string? MCStatus { get; set; }
        public int TotalDo { get; set; }

        public int? IsIntegration { get; set; }
    }
    public class VendorViewModel
    {
        [JsonPropertyName("vendor_code")]
        public string? VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; } = string.Empty;
    }

    // View Models
    public class SPKViewModel
    {
        public string NoSPK { get; set; }
        public string TglOrder { get; set; }
        public string TipeOrder { get; set; }
        public string JenisTruck { get; set; }
        public string TglMuat { get; set; }
        public string JamMulaiMuat { get; set; }
        public string JamSelesaiMuat { get; set; }
        public string Transporter { get; set; }
        public string NamaSopir { get; set; }
        public string NoPol { get; set; }
        public string TglTiba { get; set; }
        public string JamTiba { get; set; }
        public string Catatan { get; set; }
        public KelengkapanDokumen KelengkapanDokumen { get; set; }
        public List<DeliveryOrderItem> DeliveryOrders { get; set; }

        public int GrandTotalBox => DeliveryOrders?.Sum(d => d.TotalBoxKoli) ?? 0;
        public int GrandTotalQty => DeliveryOrders?.Sum(d => d.TotalQtyPcs) ?? 0;
        public decimal GrandTotalVolume => DeliveryOrders?.Sum(d => d.TotalVolume) ?? 0;

        public bool IsPreview { get; set; } = false;
    }

    public class KelengkapanDokumen
    {
        public bool Berangkat { get; set; }
        public bool Kembali { get; set; }
    }

    public class DeliveryOrderItem
    {
        public string ShipTo { get; set; }
        public string ShipToName { get; set; }
        public string City { get; set; }
        public string DO { get; set; }
        public int TotalBoxKoli { get; set; }
        public int TotalQtyPcs { get; set; }
        public decimal TotalVolume { get; set; }
    }

    public class PodDetailViewModel
    {
        public string? jobid { get; set; }
        public string? inv_no { get; set; }
        public string? ship_to_name { get; set; }
        public string? ship_to_address { get; set; }
        public string? ship_to_city { get; set; }
        public DateTime? delivery_date { get; set; }

        // POD fields
        public DateTime? outorigin_date { get; set; }
        public string? outorigin_time { get; set; }
        public DateTime? arriv_date { get; set; }
        public string? arriv_time { get; set; }
        public string? arriv_pic { get; set; }
        public DateTime? pod_ret_date { get; set; }
        public string? pod_ret_time { get; set; }
        public string? pod_ret_pic { get; set; }
        public DateTime? pod_send_date { get; set; }
        public string? pod_send_time { get; set; }
        public string? pod_send_pic { get; set; }
        public byte? pod_status { get; set; }
        public string? spd_no { get; set; }
        public string? pod_remark { get; set; }
    }

    public class OpenPodByShipToRequest
    {
        public string? ShipToName { get; set; }
        public string? ShipToAddress { get; set; }
        public string? ShipToCity { get; set; }
        public string? DelivDate { get; set; }
    }
}

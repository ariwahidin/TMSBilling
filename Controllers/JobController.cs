
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

        public JobController(AppDbContext context, 
            SelectListService selectList, 
            ApiService apiService, 
            IConfiguration configuration,
            SyncronizeWithMcEasy sync
            )
        {
            _context = context;
            _selectList = selectList;
            _apiService = apiService;
            _configuration = configuration;
            _sync = sync;
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

        //public async Task<IActionResult> Index()
        //{
        //    var data = await GetJobSummaryQuery().ToListAsync();
        //    return View(data);
        //}


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
            a.truck_no AS TruckNo,
            a.deliv_date AS DelivDate,
            a.origin AS Origin,
            a.dest AS Dest,
            mc_fo.status AS MCStatus,
            a.vendor_plan AS VendorPlan,
            COALESCE(tj.total_do, 0) AS TotalDo
        FROM TRC_JOB_H a
        LEFT JOIN tj ON a.jobid = tj.jobid
        LEFT JOIN mc_fo ON mc_fo.id = a.mceasy_job_id
        INNER JOIN TRC_CUST_GROUP c ON a.cust_group = c.SUB_CODE
        INNER JOIN UserXCustomers d ON d.CustomerMain = c.MAIN_CUST
        WHERE d.Username = {0}
            AND a.deliv_date >= {1}
            AND a.deliv_date < {2}
        ORDER BY a.id_seq DESC
    ";
            return _context.JobSummaryView.FromSqlRaw(sql, username, startDate, endDate);
        }

        //public async Task<IActionResult> IndexSync()
        //{

        //    try
        //    {
        //        var ordersFromApi = await _sync.FetchFO();
        //        var result = await _sync.SyncFOToDatabase(ordersFromApi);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error sync data: {ex.Message}");
        //    }


        //    var data = await GetJobSummaryQuery().ToListAsync();

        //    return View("Index", data);
        //}

        //private IQueryable<JobSummaryViewModel> GetJobSummaryQuery() {

        //    var username = HttpContext.Session.GetString("username") ?? "System";

        //    var sql = @"
        //        WITH tj AS (
        //            SELECT 
        //                jobid,
        //                COUNT(inv_no) AS total_do
        //            FROM TRC_JOB
        //            GROUP BY jobid
        //        )
        //        SELECT 
        //            a.id_seq AS IdSeq,
        //            a.jobid AS JobId,
        //            a.truck_no AS TruckNo,
        //            a.deliv_date AS DelivDate,
        //            a.origin AS Origin,
        //            a.dest AS Dest,
				    //            mc_fo.status AS MCStatus,
        //            a.vendor_plan AS VendorPlan,
        //            COALESCE(tj.total_do, 0) AS TotalDo
        //        FROM TRC_JOB_H a
        //        LEFT JOIN tj ON a.jobid = tj.jobid
        //        LEFT JOIN mc_fo ON mc_fo.id = a.mceasy_job_id
        //        INNER JOIN TRC_CUST_GROUP c ON a.cust_group = c.SUB_CODE
        //        INNER JOIN UserXCustomers d ON d.CustomerMain = c.MAIN_CUST
        //        WHERE d.Username = {0}
        //        ORDER BY a.id_seq DESC
        //    ";

        //    return _context.JobSummaryView.FromSqlRaw(sql,username);
        //}

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


            //ViewBag.ListCustomer = _selectList.getCustomers();
            //ViewBag.ListCustomerGroup = _selectList.getCustomerGroup();
            ViewBag.ListWarehouse = _selectList.GetWarehouse();
            ViewBag.ListConsignee = _selectList.GetConsignee();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
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
                    //query = query.Where(o =>
                    //    (o.delivery_date == parsedDate && o.order_status == 0) ||
                    //    o.jobid == jobid
                    //);

                    query = query.Where(o =>
                        (EF.Functions.DateDiffDay(o.delivery_date, parsedDate) == 0 && o.order_status == 0)
                        || o.jobid == jobid
                    );
                }
                else
                {
                    //query = query.Where(o =>
                    //    o.delivery_date == parsedDate && o.order_status == 0
                    //);

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

            //return Ok(new { success = true, message = $"masok 1" });

            var Header = model.FormJobHeader;
            var Details = model.FormJobDetails;

            var customerGroup = await _context.CustomerGroups.FirstOrDefaultAsync( c => c.SUB_CODE == Header.cust_group);
            if (customerGroup == null) {
                return Json(new { success = true, message = "Customer group not found!" });
            }

            var customerApi = await _context.Customers.AnyAsync(cs => cs.MAIN_CUST == customerGroup.MAIN_CUST && cs.API_FLAG == 1);

            var GeofenceStartingPoint = await _context.Geofences.FirstOrDefaultAsync(f => f.Id == Header.starting_point);

            if (GeofenceStartingPoint != null) {
                Header.starting_point = GeofenceStartingPoint.GeofenceId;
            }

            if (customerApi)
            {
                var run = await RunSaveWithApi(Header, Details, jobid);
                if (!run.ok) return BadRequest(new { success = false, message = run.message });
            }
            else {
                    //return BadRequest(new { success = false, message = "Customer not using API" });
                var run = await RunSaveWithOutApi(Header, Details, jobid);
                if (!run.ok) return BadRequest( new { success = false, message = run.message });
            }

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
             join p in _context.JobPODs
                 on j.inv_no equals p.inv_no into podGroup
             from pod in podGroup.DefaultIfEmpty()
             where jobIds.Contains(j.jobid)
             select new
             {
                 j.jobid,
                 j.inv_no,

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
                        entry_date = DateTime.Now
                    };

                    _context.JobPODs.Add(model);
                }
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }



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
        public string? TruckNo { get; set; }
        public DateTime? DelivDate { get; set; }
        public string? Origin { get; set; }
        public string? Dest { get; set; }
        public string? VendorPlan { get; set; }
        public string? MCStatus { get; set; }
        public int TotalDo { get; set; }
    }
    public class VendorViewModel
    {
        [JsonPropertyName("vendor_code")]
        public string? VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("vendor_name")]
        public string? VendorName { get; set; } = string.Empty;
    }
    
}

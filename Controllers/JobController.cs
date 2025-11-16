
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public JobController(AppDbContext context, SelectListService selectList, ApiService apiService, IConfiguration configuration)
        {
            _context = context;
            _selectList = selectList;
            _apiService = apiService;
            _configuration = configuration;
        }

        private async Task<List<FleetOrderMcEasy>> FetchFO()
        {

            var sql = @"SELECT 
                mceasy_job_id AS JobID
                FROM
                TRC_JOB_H
                WHERE status_job IN ('DRAFT', 'STARTED', 'SCHEDULED')";
            var data = await _context.JobOrder
                .FromSqlRaw(sql)
                .ToListAsync();

            var allFO = new List<FleetOrderMcEasy>();
            bool ok;
            JsonElement json = default;

            for (int i = 0; i < data.Count; i++)
            {
                (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Get,
                    $"fleet-planning/api/web/v1/fleet-task/{data[i].OrderID}"
                );

                if (!ok)
                    throw new Exception($"Gagal ambil halaman ke-{i} dari API get fo");

                var orders = json
                    .GetProperty("data")
                    .Deserialize<FleetOrderMcEasy>() ?? new FleetOrderMcEasy();

                allFO.Add(orders);
            }

            return allFO;
        }

        private async Task<int> SyncFOToDatabase(List<FleetOrderMcEasy> orders)
        {
            int insertedCount = 0;
            int updatedCount = 0;

            if (orders == null || !orders.Any())
                return 0;

            // Ambil ID yang sudah ada untuk mencegah duplikasi
            var existingIds = _context.MCFleetOrders
                .Select(p => p.id)
                .ToHashSet();

            var existingOrders = _context.MCFleetOrders.ToDictionary(p => p.id, p => p);
            var newOrders = new List<MCFleetOrder>();


            foreach (var p in orders)
            {
                if (string.IsNullOrEmpty(p.id))
                    continue;

                if (!existingOrders.TryGetValue(p.id, out var existing))
                {
                    // Data baru → insert
                    var newOrder = new MCFleetOrder
                    {
                        id = p.id,
                        number = p.number,
                        shipment_reference = p.shipment_reference,
                        status = p.status?.name,
                        status_raw_type = p.status?.raw_type,
                        entry_date = DateTime.Now
                    };

                    newOrders.Add(newOrder);
                    insertedCount++;
                }
                else
                {
                    // Data sudah ada → update kalau ada perubahan
                    bool updated = false;

                    if (existing.status != p.status?.name)
                    {
                        existing.status = p.status?.name;
                        updated = true;
                    }

                    if (existing.status_raw_type != p.status?.raw_type)
                    {
                        existing.status_raw_type = p.status?.raw_type;
                        updated = true;
                    }

                    if (existing.shipment_reference != p.shipment_reference)
                    {
                        existing.shipment_reference = p.shipment_reference;
                        updated = true;
                    }

                    if (updated)
                    {
                        existing.entry_date = DateTime.Now; // update timestamp
                        //updatedCount++;
                    }
                }
            }

            // insert data baru dalam batch
            if (newOrders.Any())
            {
                await _context.MCFleetOrders.AddRangeAsync(newOrders);
            }

            // simpan semua perubahan (insert + update)
            await _context.SaveChangesAsync();


            //foreach (var p in orders)
            //{
            //    if (!existingIds.Contains(p.id))
            //    {
            //        var newOrder = new MCFleetOrder
            //        {
            //            id = p.id,
            //            number = p.number,
            //            shipment_reference = p.shipment_reference,
            //            status = p.status?.name,
            //            status_raw_type = p.status?.raw_type,
            //            entry_date = DateTime.Now
            //        };

            //        newOrders.Add(newOrder);
            //    }

            //    insertedCount++;
            //}

            // Insert semua data baru
            if (newOrders.Any())
            {
                _context.MCFleetOrders.AddRange(newOrders);
                await _context.SaveChangesAsync();
            }

            //var scheduledIds = orders
            //    .Where(o => string.Equals(o.status?.raw_type, "ENDED", StringComparison.OrdinalIgnoreCase)
            //             || string.Equals(o.status?.raw_type, "SCHEDULED", StringComparison.OrdinalIgnoreCase)
            //             || string.Equals(o.status?.raw_type, "STARTED", StringComparison.OrdinalIgnoreCase))
            //    .Select(o => o.id)
            //    .Where(id => !string.IsNullOrEmpty(id))
            //    .ToList();

            //if (scheduledIds.Any())
            //{
            //    var idList = string.Join(",", scheduledIds.Select(id => $"'{id}'"));

            //    var updateSql = $@"
            //        UPDATE TRC_JOB_H
            //        SET status_job = 'ENDED'
            //        WHERE mceasy_job_id IN ({idList})
            //    ";

            //    updatedCount = await _context.Database.ExecuteSqlRawAsync(updateSql);
            //}

            var filteredIds = orders
                .Where(o => o.status?.raw_type != null)
                .Select(o => o.id)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            if (filteredIds.Any())
            {
                // ambil rows yang ada di DB
                var jobs = await _context.JobHeaders
                    .Where(j => filteredIds.Contains(j.mceasy_job_id))
                    .ToListAsync();

                // buat lookup status berdasarkan id
                var statusLookup = orders
                    .Where(o => o.status?.raw_type != null && !string.IsNullOrEmpty(o.id))
                    .ToDictionary(o => o.id, o => o.status.raw_type.Trim().ToUpperInvariant());

                foreach (var job in jobs)
                {
                    if (statusLookup.TryGetValue(job.mceasy_job_id, out var raw))
                    {
                        // mapping kalau perlu
                        job.status_job = raw switch
                        {
                            "ENDED" => "ENDED",
                            "SCHEDULED" => "SCHEDULED",
                            "STARTED" => "STARTED",
                            _ => job.status_job // biarkan kalau unknown
                        };
                    }
                }

                updatedCount = await _context.SaveChangesAsync();
            }


            return updatedCount;
        }

        [HttpPost]
        public async Task<IActionResult> SyncronizeFO()
        {
            try
            {
                var ordersFromApi = await FetchFO();
                var result = await SyncFOToDatabase(ordersFromApi);
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

        public async Task<IActionResult> Index()
        {


            try
            {
                var ordersFromApi = await FetchFO();
                var result = await SyncFOToDatabase(ordersFromApi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sync data: {ex.Message}");
            }


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
                ORDER BY a.id_seq DESC
            ";

            var data = _context.JobSummaryView
                .FromSqlRaw(sql)
                .ToList();

            return View(data);
        }


        [Route("Job/Form/{jobid?}")]
        public IActionResult Form(string? jobid)
        {
            var vm = new JobViewModel();

            // ViewBag dropdown
            ViewBag.ListCustomer = _selectList.getCustomers();
            ViewBag.ListCustomerGroup = _selectList.getCustomerGroup();
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

            if (!string.IsNullOrEmpty(jobid))
            {

                var jobHeader = _context.JobHeaders.FirstOrDefault(or => or.jobid == jobid);
                if (jobHeader == null) {
                    return View(vm);
                }

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

            // Bagian Validasi

            if (Details == null || Details.Count == 0)
            {
                return BadRequest(new { success = false, message = "Order detail not found." });

            }


            //if (!string.IsNullOrEmpty(jobid))
            //{
            //    // delete existing job first
            //    var jobExistingList = _context.Jobs.Where(j => j.jobid == jobid).ToList();

            //    if (jobExistingList.Any())
            //    {
            //        _context.Jobs.RemoveRange(jobExistingList); 
            //        _context.SaveChanges();
            //    }

            //    _context.Database.ExecuteSqlRaw("UPDATE TRC_ORDER SET order_status = 0, jobid = NULL WHERE jobid = {0}", jobid);

            //}



            var CostRate = _context.PriceBuys.FirstOrDefault(hm =>
            hm.sup_code == Header.vendor_id
            && hm.origin == Header.origin_id
            && hm.dest == Header.dest_area
            && hm.serv_moda == Header.serv_moda
            && hm.truck_size == Header.truck_size);

            if (CostRate == null)
            {
                return BadRequest(new { success = false, message = "Header, price buy not found" });
            }

            var OriginIsMatch = false;
            var DestinationMatch = false;

            // check origin order min 1 is match
            foreach (var oro in Details) {
                if (CostRate.origin == oro.origin_id) { 
                    OriginIsMatch = true;
                    break;
                }
            }

            if (!OriginIsMatch) {
                return BadRequest(new
                {
                    success = false,
                    message = $"Origin not found in order. Required origin: {CostRate.origin}"
                });
            }

            // check destination order min 1 is match
            foreach (var ordes in Details) {
                if (CostRate.dest == ordes.dest_area) { 
                    DestinationMatch = true;
                    break;
                }
            }

            if (!DestinationMatch) {
                return BadRequest(new { 
                    success = false,
                    message = $"Destination not found in order. Required destiation : {CostRate.dest}"
                });
            }
            

            foreach (var ord in Details)
            {
                var orderExisting = _context.Orders.FirstOrDefault(or => or.inv_no == ord.inv_no);
                if (orderExisting == null)
                {
                    return BadRequest(new { success = false, message = "Order not found" });
                }

                var customerGroup = _context.CustomerGroups.FirstOrDefault(g => g.SUB_CODE == orderExisting.sub_custid);

                if (customerGroup == null) {
                    return NotFound(new { success = false, message = "Customer group not found" });
                }

                var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

                if (customer == null) {
                    return BadRequest(new { success = false, message = "Customer not found" });
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
                if (SellRateCheck == null) {
                    return BadRequest(new { success = false, message = "Sell rate not found for INV "+ orderExisting.inv_no });
                }
            }


            // Bagian Eksekusi
            // Buat prefix bulanan: TRYP2507
            //string jobPrefix = _configuration["Tms:JobPrefix"];
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
                return BadRequest(new { success = false, message = result.message });

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
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Store Fleet Task",
                        detail = json
                    });
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
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Patch Fleet Task",
                        detail = json2
                    });
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
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Do Transition Fleet Task",
                        detail = json3
                    });
                }

            }

            if (!string.IsNullOrEmpty(jobid))
            {

                var jobHeader = _context.JobHeaders.FirstOrDefault(jo => jo.jobid == jobid);
                if (jobHeader != null)
                {
                    if (!string.IsNullOrEmpty(jobHeader.mceasy_job_id))
                    {
                        foreach (var idDel in deliveryOrderIds) {
                            var payloadDel = new { };
                            var (okDel, jsonDel) = await _apiService.SendRequestAsync(
                                HttpMethod.Delete,
                                $"fleet-planning/api/web/v1/fleet-task/{jobHeader.mceasy_job_id}/delivery-order/{idDel}",
                                payloadDel
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
                            return BadRequest(new
                            {
                                success = false,
                                message = "Gagal kirim ke API Patch Fleet Task",
                                detail = json
                            });
                        }

                        var newOrderIDs = new List<string>();

                        foreach (var item in Details) { 
                            Console.WriteLine("NEW ORDER ID : {0}", item.mceasy_order_id);
                            var orderExisting = _context.Orders.FirstOrDefault(or => or.inv_no == item.inv_no);
                            if (orderExisting != null && orderExisting.mceasy_order_id != null) { 
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
                            return BadRequest(new
                            {
                                success = false,
                                message = "Gagal kirim ke API Store Fleet Task",
                                detail = jsonNew
                            });
                        }

                    }
                    jobHeader.cust_group = Header.cust_group;
                    jobHeader.vendor_plan = Header.vendor_id;
                    jobHeader.vendor_act = Header.vendor_act;
                    jobHeader.is_vendor = Header.vendor_id == Header.vendor_act ? true : false;
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
                    jobHeader.starting_point = Header.starting_point;
                    jobHeader.update_user = HttpContext.Session.GetString("username") ?? "System";
                    jobHeader.update_date = DateTime.Now;
                    if (!string.IsNullOrEmpty(mceasy_job_id))
                    {
                        jobHeader.mceasy_job_id = mceasy_job_id;
                    }
                    _context.JobHeaders.Update(jobHeader);

                    // delete existing job first
                    var jobExistingList = _context.Jobs.Where(j => j.jobid == jobid).ToList();

                    if (jobExistingList.Any())
                    {
                        Console.WriteLine("DELETE JOB : {0}",jobid);
                        var rows = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_JOB WHERE jobid = {0}", jobid);
                        Console.WriteLine("Rows delete affected: " + rows);
                    }

                    var rowUpd =  _context.Database.ExecuteSqlRaw("UPDATE TRC_ORDER SET order_status = 0, jobid = NULL WHERE jobid = {0}", jobid);
                    Console.WriteLine("Rows update affected: " + rowUpd);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Data job not found!" });
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
                jobHeader.entry_user = HttpContext.Session.GetString("username") ?? "System";
                jobHeader.entry_date = DateTime.Now;
                jobHeader.mceasy_job_id = mceasy_job_id;
                jobHeader.starting_point = Header.starting_point;
                jobHeader.status_job = "DRAFT";
                _context.JobHeaders.Add(jobHeader);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Job saved successfully" });
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


        public async Task<IActionResult> GetOrders(string originId, string destArea, DateTime deliveryDate, bool multidrop)
        {
            var allowedStatus = new[] { "Dikonfirmasi", "Dijadwalkan" };

            var query = _context.Orders
                .Where(o =>
                    o.origin_id == originId &&
                    EF.Functions.DateDiffDay(o.delivery_date, deliveryDate) == 0 &&
                    allowedStatus.Contains(o.mceasy_status) &&
                    o.jobid == null
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

            //var nextDay = deliveryDate.AddDays(1);
            //Console.WriteLine($"deliveryDate: {deliveryDate:yyyy-MM-dd HH:mm:ss}");
            //Console.WriteLine($"nextDay: {nextDay:yyyy-MM-dd HH:mm:ss}");

            //var result = await _context.Orders
            //    .Where(o => o.origin_id == originId
            //     && o.dest_area == destArea
            //     && o.delivery_date >= deliveryDate
            //     && o.delivery_date < nextDay
            //     && o.order_status == 1)

            var result = await _context.Vendors
            //.Where(o => o.origin_id == originId
            //    && o.dest_area == destArea
            //    && EF.Functions.DateDiffDay(o.delivery_date, deliveryDate) == 0
            //    && o.mceasy_status == "CONFIRMED"
            //    && o.mceasy_is_upload == false)
            .Select(v => new VendorViewModel
            {
                VendorCode = v.SUP_CODE,
                VendorName = v.SUP_NAME
            })
            .ToListAsync();
            return Ok(new { success = true, data = result }); // hasilnya JSON
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

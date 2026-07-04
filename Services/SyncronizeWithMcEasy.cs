using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Models;
using TMSBilling.Services.Integration;

namespace TMSBilling.Services
{
    public class SyncronizeWithMcEasy
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SyncronizeWithMcEasy> _logger;
        private readonly ApiService _apiService;

        public SyncronizeWithMcEasy(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<SyncronizeWithMcEasy> logger,
            ApiService apiService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiService = apiService;
        }

        public async Task Run()
        {
            var totalStart = DateTime.Now;
            _logger.LogInformation("=== Mulai sync data McEasy === {time}", DateTime.Now);

            // Step 1: ORDER
            try
            {
                _logger.LogInformation("Mulai sync ORDER...");
                var orders = await FetchOrderFromApi(1000);
                _logger.LogInformation("Order API mengembalikan {count} data", orders?.Count ?? 0);
                if (orders?.Any() == true)
                    await SyncOrderToDatabase(orders);
                else
                    _logger.LogWarning("Tidak ada data ORDER yang perlu disinkronkan.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal sync ORDER");
            }

            // Step 2: JOB
            try
            {
                _logger.LogInformation("Mulai sync JOB...");
                var jobs = await FetchFO(1000);
                _logger.LogInformation("Job API mengembalikan {count} data", jobs?.Count ?? 0);
                if (jobs?.Any() == true)
                    await SyncFOToDatabase(jobs);
                else
                    _logger.LogWarning("Tidak ada data JOB yang perlu disinkronkan.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal sync JOB");
            }

            // Step 3: ORDER IN JOB
            try
            {
                _logger.LogInformation("Mulai sync ORDER IN JOB...");
                var resultCount = await SyncOrderInJob();
                _logger.LogInformation("ORDER IN JOB selesai: {count}", resultCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal sync ORDER IN JOB");
            }


            // Step 4: ORDER POD
            try
            {
                _logger.LogInformation("Mulai sync ORDER POD...");
                var podOrders = await FetchOrderPODFromApi(1000);
                _logger.LogInformation("Order POD API FROM MCEasy mengembalikan {count} data", podOrders?.Count ?? 0);

                var debugJson = System.Text.Json.JsonSerializer.Serialize(podOrders, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                _logger.LogInformation("Isi podOrders:\n{data}", debugJson);

                if (podOrders != null && podOrders.Count > 0)
                {
                    await SaveJobPODAsync(podOrders);
                }

                // Implementasi sinkronisasi ORDER POD ke database jika diperlukan
                //if (orders?.Any() == true)
                //    await SyncOrderToDatabase(orders);
                //else
                //    _logger.LogWarning("Tidak ada data ORDER yang perlu disinkronkan.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal sync ORDER POD");
            }

            var totalDuration = DateTime.Now - totalStart;
            _logger.LogInformation("=== Sync McEasy selesai. Total durasi: {duration} detik ===",
                totalDuration.TotalSeconds.ToString("0.000"));
        }

        // Entry point utama (ini dipanggil dari controller atau console app)
        //public async Task Run()
        //{
        //    var totalStart = DateTime.Now;
        //    _logger.LogInformation("=== Mulai sync data McEasy === {time}", DateTime.Now);

        //    try
        //    {
        //        // --------------------- ORDER ------------------------
        //        var orderStart = DateTime.Now;
        //        _logger.LogInformation("Mulai sync ORDER...");

        //        var orders = await FetchOrderFromApi(1000);
        //        var orderCount = orders?.Count ?? 0;

        //        _logger.LogInformation("Order API mengembalikan {count} data", orderCount);

        //        if (orderCount > 0)
        //        {
        //            await SyncOrderToDatabase(orders);
        //            var orderDuration = DateTime.Now - orderStart;

        //            _logger.LogInformation(
        //                "Sync ORDER selesai. Total: {count} | Durasi: {duration} detik",
        //                orderCount,
        //                orderDuration.TotalSeconds.ToString("0.000")
        //            );
        //        }
        //        else
        //        {
        //            _logger.LogWarning("Tidak ada data ORDER yang perlu disinkronkan.");
        //        }


        //        // --------------------- JOB ------------------------
        //        var jobStart = DateTime.Now;
        //        _logger.LogInformation("Mulai sync JOB...");

        //        var jobs = await FetchFO(1000);
        //        var jobCount = jobs?.Count ?? 0;

        //        _logger.LogInformation("Job API mengembalikan {count} data", jobCount);

        //        if (jobCount > 0)
        //        {
        //            await SyncFOToDatabase(jobs);
        //            var jobDuration = DateTime.Now - jobStart;

        //            _logger.LogInformation(
        //                "Sync JOB selesai. Total: {count} | Durasi: {duration} detik",
        //                jobCount,
        //                jobDuration.TotalSeconds.ToString("0.000")
        //            );
        //        }
        //        else
        //        {
        //            _logger.LogWarning("Tidak ada data JOB yang perlu disinkronkan.");
        //        }

        //        //---------------------- ORDER IN JOB -----------------
        //        var jobOrderStart = DateTime.Now;
        //        _logger.LogInformation("Mulai sync ORDER IN JOB...");

        //        var resultCount = await SyncOrderInJob();

        //        _logger.LogInformation("ORDER IN JOB mengembalikan data : {count}", resultCount);


        //        // --------------------- TOTAL ------------------------
        //        var totalDuration = DateTime.Now - totalStart;
        //        _logger.LogInformation(
        //            "=== Sync selesai. Total durasi: {duration} detik ===",
        //            totalDuration.TotalSeconds.ToString("0.000")
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Terjadi error saat sync");
        //    }
        //}


        // ================================
        // FETCH ORDER
        // ================================
        public async Task<List<OrderMcEasy>> FetchOrderFromApi(int? limit = null)
        {
            // Base SQL tanpa TOP
            string sql = @"
                SELECT {0} 
                    mceasy_order_id AS OrderID,
                    CAST(order_status AS VARCHAR(20)) AS OrderStatus,
                    inv_no AS InvNo,
                    jobid AS JobID
                FROM TRC_ORDER
                WHERE 
                    mceasy_status != 'Terkirim'
                    AND mceasy_order_id IS NOT NULL
                    AND mceasy_order_id <> '0'
                    AND pickup_date >= DATEADD(DAY, -30, GETDATE())
                ORDER BY pickup_date DESC
            ";

            // Jika limit ada → tambahkan TOP
            string topClause = "";
            if (limit.HasValue && limit.Value > 0)
            {
                topClause = $"TOP {limit.Value}";
            }

            // Final SQL
            sql = string.Format(sql, topClause);

            var data = await _context.ConfirmOrderID
                .FromSqlRaw(sql)
                .ToListAsync();

            var allOrders = new List<OrderMcEasy>();

            for (int i = 0; i < data.Count; i++)
            {
                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Get,
                    $"order/api/web/v1/delivery-order/{data[i].OrderID}"
                );

                //if (!ok)
                //    throw new Exception($"Gagal ambil halaman ke-{i} dari API get order");

                if (!ok)
                {
                    _logger.LogWarning("Gagal fetch order index ke-{i}, dilewati.", i);
                    continue;
                }

                var order = json
                    .GetProperty("data")
                    .Deserialize<OrderMcEasy>() ?? new OrderMcEasy();

                allOrders.Add(order);
            }

            return allOrders;
        }

        // ================================
        // SYNC ORDER
        // ================================

        public async Task<int> SyncOrderToDatabase(List<OrderMcEasy> orders)
        {
            if (orders == null || !orders.Any())
                return 0;

            int insertedCount = 0;
            int updatedCount = 0;

            // Ambil ID existing dari MC_ORDER
            var existingIds = _context.MCOrders
                .Select(p => p.id)
                .ToHashSet();

            var newOrders = new List<MCOrder>();

            foreach (var p in orders)
            {
                if (!existingIds.Contains(p.id))
                {
                    newOrders.Add(new MCOrder
                    {
                        id = p.id,
                        number = p.number,
                        reference_number = p.reference_number,
                        shipment_number = p.shipment_number,
                        shipment_type = p.shipment_type,
                        status = p.status?.name,
                        fleet_task_id = p.fleet_task?.id,
                        fleet_task_number = p.fleet_task?.number,
                        entry_date = DateTime.Now
                    });

                    insertedCount++;
                }
                else {
                    var existing = _context.MCOrders.FirstOrDefault(x => x.id == p.id);

                    if (existing != null)
                    {
                        existing.shipment_type = p.shipment_type;
                        existing.status = p.status?.name;
                        existing.fleet_task_id = p.fleet_task?.id;
                        existing.fleet_task_number = p.fleet_task?.number;
                        existing.updated_date = DateTime.Now;

                        _context.SaveChanges();              // WAJIB agar tersimpan ke DB
                        updatedCount++;
                    }

                    var trcOrder = _context.Orders.FirstOrDefault(f => f.mceasy_order_id == p.id);
                    if (trcOrder != null)
                    {

                        Console.WriteLine("ppp {0}", existing?.fleet_task_id);

                        if (existing?.fleet_task_id == null)
                        {
                            trcOrder.jobid = null;
                        }
                        trcOrder.mceasy_status = p.status?.name;
                        _context.SaveChanges();
                    }

                }
            }

            if (newOrders.Any())
            {
                _context.MCOrders.AddRange(newOrders);
                await _context.SaveChangesAsync();
            }

            var allowedStatuses = new[]
            {
                "Draf", "Dijadwalkan", "Dijalankan", "Diambil", "Terkirim"
            };

            var filteredOrders = orders
                .Where(o => allowedStatuses.Contains(o.status?.name, StringComparer.OrdinalIgnoreCase))
                .Select(o => new
                {
                    Id = o.id,
                    Status = o.status?.name
                })
                .Where(x => !string.IsNullOrEmpty(x.Id))
                .ToList();

            if (filteredOrders.Any())
            {
                foreach (var ford in filteredOrders)
                {
                    var updateSql = $@"
                UPDATE TRC_ORDER 
                SET order_status = 2,
                    mceasy_status = '{ford.Status.Replace("'", "''")}'
                WHERE mceasy_order_id = '{ford.Id.Replace("'", "''")}'
            ";

                    updatedCount += await _context.Database.ExecuteSqlRawAsync(updateSql);
                }
            }

            // Mengembalikan total updatedCount saja (sesuai logic kamu)
            return updatedCount;
        }

        // ================================
        // FETCH FLEET ORDER
        // ================================
        public async Task<List<FleetOrderMcEasy>> FetchFO(int? limit = null)
        {
            string sql = @"
                SELECT {0}
                    mceasy_job_id AS JobID
                FROM TRC_JOB_H
                WHERE status_job IN ('DRAFT', 'STARTED', 'SCHEDULED')
                AND mceasy_job_id <> ''
            ";

            string topClause = limit.HasValue && limit.Value > 0
                ? $"TOP {limit.Value}"
                : "";

            sql = string.Format(sql, topClause);

            var data = await _context.JobOrder
                .FromSqlRaw(sql)
                .ToListAsync();

            var allFO = new List<FleetOrderMcEasy>();

            for (int i = 0; i < data.Count; i++)
            {
                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Get,
                    $"fleet-planning/api/web/v1/fleet-task/{data[i].OrderID}"
                );

                //if (!ok)
                //    throw new Exception($"Gagal ambil halaman ke-{i} dari API get fo");

                if (!ok)
                {
                    _logger.LogWarning("Gagal ambil FO index ke-{i}, dilewati.", i);
                    continue;
                }

                var fo = json.GetProperty("data")
                             .Deserialize<FleetOrderMcEasy>()
                          ?? new FleetOrderMcEasy();

                allFO.Add(fo);
            }

            return allFO;
        }

        // ================================
        // SYNC FLEET ORDER
        // ================================
        public async Task<int> SyncFOToDatabase(List<FleetOrderMcEasy> orders)
        {
            if (orders == null || !orders.Any())
                return 0;

            int updatedCount = 0;

            var existingOrders = _context.MCFleetOrders
                .ToDictionary(p => p.id, p => p);

            var newOrders = new List<MCFleetOrder>();

            foreach (var p in orders)
            {
                if (string.IsNullOrEmpty(p.id))
                    continue;

                if (!existingOrders.TryGetValue(p.id, out var existing))
                {
                    newOrders.Add(new MCFleetOrder
                    {
                        id = p.id,
                        number = p.number,
                        shipment_reference = p.shipment_reference,
                        status = p.status?.name,
                        status_raw_type = p.status?.raw_type,
                        entry_date = DateTime.Now
                    });
                }
                else
                {
                    bool update = false;

                    if (existing.status != p.status?.name)
                    {
                        existing.status = p.status?.name;
                        update = true;
                    }

                    if (existing.status_raw_type != p.status?.raw_type)
                    {
                        existing.status_raw_type = p.status?.raw_type;
                        update = true;
                    }

                    if (existing.shipment_reference != p.shipment_reference)
                    {
                        existing.shipment_reference = p.shipment_reference;
                        update = true;
                    }

                    if (update)
                        existing.entry_date = DateTime.Now;
                }
            }

            if (newOrders.Any())
                await _context.MCFleetOrders.AddRangeAsync(newOrders);

            await _context.SaveChangesAsync();

            // Update TRC_JOB_H berdasarkan raw type
            var filteredIds = orders
                .Where(o => !string.IsNullOrEmpty(o.id) && o.status?.raw_type != null)
                .Select(o => o.id)
                .ToList();

            if (filteredIds.Any())
            {
                var jobs = await _context.JobHeaders
                    .Where(j => filteredIds.Contains(j.mceasy_job_id))
                    .ToListAsync();

                var lookup = orders
                    .Where(o => o.status?.raw_type != null)
                    .ToDictionary(p => p.id, p => p.status.raw_type.ToUpper());

                foreach (var job in jobs)
                {
                    if (lookup.TryGetValue(job.mceasy_job_id, out var raw))
                    {
                        job.status_job = raw switch
                        {
                            "ENDED" => "ENDED",
                            "STARTED" => "STARTED",
                            "SCHEDULED" => "SCHEDULED",
                            _ => job.status_job
                        };
                    }
                }

                updatedCount = await _context.SaveChangesAsync();
            }

            return updatedCount;
        }

        // ================================
        // SYNC ORDER NOT FOUN IN JOB
        // ================================

        public async Task<int> SyncOrderInJob(int? limit = null) {
            string sql = @"
                WITH fo_job AS
                (SELECT
                a.number as fo,
                a.shipment_reference as jobid,
                b.shipment_number as inv_no,
                a.[status] as fo_status
                FROM MC_FO a
                INNER JOIN MC_ORDER b ON a.number = b.fleet_task_number )
                select 
                a.fo AS FoNumber,
                a.jobid AS JobId,
                a.inv_no AS InvNo,
                a.fo_status AS FoStatus,
                isnull(b.inv_no, 0) AS IsJob
                from fo_job a
                inner join TRC_ORDER tor ON a.inv_no = tor.inv_no
                left join TRC_JOB b ON a.inv_no = b.inv_no
                WHERE 
                fo_status <> 'Draf'
                AND a.inv_no is not null
                AND b.inv_no is null";

            var orders = await _context.OrderNotInJob
                .FromSqlRaw(sql)
                .ToListAsync();

            int count = 0;

            foreach (var item in orders)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(i => i.inv_no == item.InvNo);
                var job = await _context.Jobs
                                .Where(o => o.jobid == item.JobId)
                                .OrderByDescending(o => o.drop_seq)
                                .FirstOrDefaultAsync();
                var customerGroup = await _context.CustomerGroups.FirstOrDefaultAsync(g => g.SUB_CODE == order.sub_custid);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CUST_CODE == customerGroup.CUST_CODE);
                var SellRate = await _context.PriceSells.FirstOrDefaultAsync(sr =>
                                    sr.cust_code == customer.MAIN_CUST
                                    && sr.origin == order.origin_id
                                    && sr.dest == order.dest_area
                                    && sr.truck_size == order.truck_size
                                    && sr.serv_type == order.serv_req
                                    && sr.serv_moda == order.moda_req
                                    && sr.charge_uom == order.uom
                                    );

                if (job != null) {

                    order.jobid = job.jobid;
                    order.update_user = "System";
                    order.update_date = DateTime.Now;
                    _context.Orders.Update(order);


                    var newJob = new Job
                    {
                        jobid = job.jobid,
                        vendorid = job.vendorid,
                        truckid = job.truckid,
                        drivername = job.drivername,
                        moda_req = order.moda_req,
                        serv_req = order.serv_req,
                        truck_size = order.truck_size,
                        flag_ep = job.flag_ep,
                        flag_rc = job.flag_rc,
                        flag_ov = job.flag_ov,
                        flag_cc = job.flag_cc,
                        flag_diffa = job.flag_diffa,
                        charge_uom_v = job.charge_uom,
                        charge_uom_c = order.uom,
                        drop_seq = job.drop_seq + 1,
                        multidrop = job.multidrop,
                        flag_charge = job.flag_charge,

                        charge_uom = job.charge_uom,
                        inv_no = order.inv_no,
                        origin_id = order.origin_id,
                        dest_id = order.dest_area,
                        dvdate = job.dvdate,

                        buy1 = job.buy1,
                        buy2 = job.buy2,
                        buy3 = job.buy3,
                        buy_ov = job.buy_ov,
                        buy_cc = job.buy_cc,
                        buy_rc = job.buy_rc,
                        buy_ep = job.buy_ep,
                        buy_diffa = job.buy_diffa,
                        buy_trip2 = job.buy_trip2,
                        buy_trip3 = job.buy_trip3,

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

                        entry_user = "System",
                        entry_date = DateTime.Now,
                        update_user = "System",
                        update_date = DateTime.Now
                    };

                    _context.Jobs.Add(newJob);
                    await _context.SaveChangesAsync();

                    count++;

                }
            }

            return count;
        }


        // ================================
        // FETCH ORDER POD FROM MCEASY API
        // ================================

        public async Task<List<OrderPODMcEasy>> FetchOrderPODFromApi(int? limit = null)
        {
            string sql = @"
                SELECT {0} 
                    mceasy_order_id AS OrderID,
                    CAST(order_status AS VARCHAR(20)) AS OrderStatus,
                    inv_no AS InvNo,
                    jobid AS JobID
                FROM TRC_ORDER
                WHERE 
                    mceasy_status = 'Terkirim'
                    AND mceasy_order_id IS NOT NULL
                    AND pickup_date >= DATEADD(DAY, -30, GETDATE())
            ";
            string topClause = "";
            if (limit.HasValue && limit.Value > 0)
            {
                topClause = $"TOP {limit.Value}";
            }
            sql = string.Format(sql, topClause);

            var data = await _context.ConfirmOrderID
                .FromSqlRaw(sql)
                .ToListAsync();

            var allOrders = new List<OrderPODMcEasy>();
            for (int i = 0; i < data.Count; i++)
            {
                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Get,
                    $"order/api/web/v1/delivery-order/{data[i].OrderID}/activity"
                );
                if (!ok)
                {
                    _logger.LogWarning("Gagal fetch order POD index ke-{i}, dilewati.", i);
                    continue;
                }

                var activities = json
                    .GetProperty("data")
                    .Deserialize<List<OrderPODMcEasy>>() ?? new List<OrderPODMcEasy>();

                foreach (var activity in activities)
                {
                    activity.InvNo = data[i].InvNo;
                    activity.JobID = data[i].JobID;
                }

                allOrders.AddRange(activities);
            }
            return allOrders;
        }

        private async Task SaveJobPODAsync(List<OrderPODMcEasy> podOrders)
        {
            if (podOrders == null || podOrders.Count == 0)
            {
                _logger.LogInformation("Tidak ada data POD untuk disimpan.");
                return;
            }

            int inserted = 0;
            int updated = 0;

            // Preload existing records dari DB sekali di awal
            var jobIds = podOrders.Select(x => x.JobID).Distinct().ToList();
            var existingList = await _context.JobPODs
                .Where(x => jobIds.Contains(x.jobid))
                .ToListAsync();

            // Dictionary buat tracking, termasuk yang baru di-Add dalam batch ini
            var tracker = existingList
                .ToDictionary(x => $"{x.jobid}_{x.inv_no}");

            foreach (var order in podOrders)
            {
                if (string.IsNullOrEmpty(order.JobID) || string.IsNullOrEmpty(order.InvNo))
                {
                    _logger.LogWarning("Skip data POD karena JobID/InvNo kosong. id={id}", order.id);
                    continue;
                }

                var key = $"{order.JobID}_{order.InvNo}";

                if (!tracker.TryGetValue(key, out var existing))
                {
                    existing = new JobPOD
                    {
                        jobid = order.JobID,
                        inv_no = order.InvNo,
                        entry_user = "SYSTEM_MCEASY",
                        entry_date = DateTime.Now
                    };
                    _context.JobPODs.Add(existing);
                    tracker[key] = existing; // penting: masukkan ke tracker biar iterasi berikutnya ketemu
                    inserted++;
                }
                else
                {
                    updated++;
                }

                if (order.type == "PICKUP")
                {
                    existing.picked_by = order.contact_person_name;
                    existing.picked_on = order.completed_on?.DateTime;
                }
                else if (order.type == "DROP")
                {
                    existing.dropped_by = order.contact_person_name;
                    existing.dropped_on = order.completed_on?.DateTime;
                }

                existing.update_user = "SYSTEM_MCEASY";
                existing.update_date = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Sync JobPOD selesai. Insert: {inserted}, Update: {updated}", inserted, updated);
        }
    }


    public class OrderNotInJob
    {
        public string? FoNumber { get; set; }
        public string? JobId { get; set; }

        public string? InvNo { get; set; }

        public string? FoStatus { get; set; }

        public string? IsJob { get; set; }
    }


    public class SyncWithAfterShip
    {
        private readonly AppDbContext _context;
        private readonly IIntegrationDispatcher _dispatcher;
        private readonly ILogger<SyncWithAfterShip> _logger;

        public SyncWithAfterShip(
            AppDbContext context,
            IIntegrationDispatcher dispatcher,
            ILogger<SyncWithAfterShip> logger)
        {
            _context = context;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Run()
        {
            _logger.LogInformation("=== Mulai SyncWithAfterShip === {time}", DateTime.Now);

            try
            {
                // ← Cek dulu apakah ada integrasi aktif untuk event ini
                var hasActiveIntegration = await _context.Integrations
                    .AnyAsync(i => i.EventKey == "spk_bosch_started" && i.IsActive == true);

                if (!hasActiveIntegration)
                {
                    _logger.LogInformation("Tidak ada integrasi aktif untuk spk_bosch_started, skip.");
                    return;
                }

                // Query job STARTED ...
                var jobs = await _context.JobHeaders
                    .Where(j =>
                        j.status_job == "STARTED" &&
                        j.job_in_plan == true &&
                        (j.job_on_delivery == false || j.job_on_delivery == null))
                    .ToListAsync();

                _logger.LogInformation("Ditemukan {count} job untuk di-sync ke AfterShip", jobs.Count);

                foreach (var job in jobs)
                {
                    try
                    {
                        // Cek MAIN_CUST = BOSCH
                        var customerGroup = await _context.CustomerGroups
                            .FirstOrDefaultAsync(g => g.SUB_CODE == job.cust_group);

                        if (customerGroup == null)
                        {
                            _logger.LogWarning("CustomerGroup tidak ditemukan untuk job {jobid}", job.jobid);
                            continue;
                        }

                        if (customerGroup.MAIN_CUST != "BOSCH")
                            continue;

                        _logger.LogInformation("Dispatch AfterShip untuk job {jobid}", job.jobid);

                        await _dispatcher.DispatchAsync("spk_bosch_started", new Dictionary<string, object?>
                        {
                            { "jobid", job.jobid },
                            { "status", "On Delivery" },
                            { "raw_tag", 2 },
                            { "platform", "AfterShip" },
                            { "date_time", DateTime.Now }
                        });

                        // Update flag
                        job.job_on_delivery = true;
                        job.job_on_delivery_time = DateTime.Now;
                        job.update_user = "System";
                        job.update_date = DateTime.Now;
                        _context.JobHeaders.Update(job);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gagal dispatch job {jobid}", job.jobid);
                        // Lanjut ke job berikutnya, tidak stop semua
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error di SyncWithAfterShip");
            }

            _logger.LogInformation("=== Selesai SyncWithAfterShip === {time}", DateTime.Now);
        }


        public async Task RunFinished()
        {
            _logger.LogInformation("=== Mulai SyncWithAfterShip FINISHED === {time}", DateTime.Now);
            try
            {
                var hasActiveIntegration = await _context.Integrations
                    .AnyAsync(i => i.EventKey == "spk_bosch_finished" && i.IsActive == true);

                if (!hasActiveIntegration)
                {
                    _logger.LogInformation("Tidak ada integrasi aktif untuk spk_bosch_finished, skip.");
                    return;
                }

                var jobs = await _context.JobHeaders
                    .Where(j =>
                        (j.status_job == "ENDED" || j.status_job == "CLOSED") &&
                        j.job_on_delivery == true &&
                        (j.job_is_finish == false || j.job_is_finish == null) &&
                        j.job_finish_time == null)
                    .ToListAsync();

                _logger.LogInformation("Ditemukan {count} job finished untuk di-sync ke AfterShip", jobs.Count);

                foreach (var job in jobs)
                {
                    try
                    {
                        var customerGroup = await _context.CustomerGroups
                            .FirstOrDefaultAsync(g => g.SUB_CODE == job.cust_group);

                        if (customerGroup == null)
                        {
                            _logger.LogWarning("CustomerGroup tidak ditemukan untuk job {jobid}", job.jobid);
                            continue;
                        }

                        if (customerGroup.MAIN_CUST != "BOSCH")
                            continue;

                        _logger.LogInformation("Dispatch AfterShip FINISHED untuk job {jobid}", job.jobid);

                        await _dispatcher.DispatchAsync("spk_bosch_finished", new Dictionary<string, object?>
                        {
                            { "jobid", job.jobid },
                            { "status", "Finished" },
                            { "raw_tag", 3 },
                            { "platform", "AfterShip" },
                            { "date_time", DateTime.Now }
                        });

                        // Update flag
                        job.job_is_finish = true;
                        job.job_finish_time = DateTime.Now;
                        job.update_user = "System";
                        job.update_date = DateTime.Now;
                        _context.JobHeaders.Update(job);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gagal dispatch finished job {jobid}", job.jobid);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error di SyncWithAfterShip RunFinished");
            }

            _logger.LogInformation("=== Selesai SyncWithAfterShip FINISHED === {time}", DateTime.Now);
        }
    }
}




using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Models;

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

        // Entry point utama (ini dipanggil dari controller atau console app)
        public async Task Run()
        {
            var totalStart = DateTime.UtcNow;
            _logger.LogInformation("=== Mulai sync data McEasy === {time}", DateTime.UtcNow);

            try
            {
                // --------------------- ORDER ------------------------
                var orderStart = DateTime.UtcNow;
                _logger.LogInformation("Mulai sync ORDER...");

                var orders = await FetchOrderFromApi(1000);
                var orderCount = orders?.Count ?? 0;

                _logger.LogInformation("Order API mengembalikan {count} data", orderCount);

                if (orderCount > 0)
                {
                    await SyncOrderToDatabase(orders);
                    var orderDuration = DateTime.UtcNow - orderStart;

                    _logger.LogInformation(
                        "Sync ORDER selesai. Total: {count} | Durasi: {duration} detik",
                        orderCount,
                        orderDuration.TotalSeconds.ToString("0.000")
                    );
                }
                else
                {
                    _logger.LogWarning("Tidak ada data ORDER yang perlu disinkronkan.");
                }


                // --------------------- JOB ------------------------
                var jobStart = DateTime.UtcNow;
                _logger.LogInformation("Mulai sync JOB...");

                var jobs = await FetchFO(1000);
                var jobCount = jobs?.Count ?? 0;

                _logger.LogInformation("Job API mengembalikan {count} data", jobCount);

                if (jobCount > 0)
                {
                    await SyncFOToDatabase(jobs);
                    var jobDuration = DateTime.UtcNow - jobStart;

                    _logger.LogInformation(
                        "Sync JOB selesai. Total: {count} | Durasi: {duration} detik",
                        jobCount,
                        jobDuration.TotalSeconds.ToString("0.000")
                    );
                }
                else
                {
                    _logger.LogWarning("Tidak ada data JOB yang perlu disinkronkan.");
                }

                //---------------------- ORDER IN JOB -----------------
                var jobOrderStart = DateTime.UtcNow;
                _logger.LogInformation("Mulai sync ORDER IN JOB...");

                var resultCount = await SyncOrderInJob();

                _logger.LogInformation("ORDER IN JOB mengembalikan data : {count}", resultCount);


                // --------------------- TOTAL ------------------------
                var totalDuration = DateTime.UtcNow - totalStart;
                _logger.LogInformation(
                    "=== Sync selesai. Total durasi: {duration} detik ===",
                    totalDuration.TotalSeconds.ToString("0.000")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terjadi error saat sync");
            }
        }


        // ================================
        // FETCH ORDER
        // ================================
        public async Task<List<OrderMcEasy>> FetchOrderFromApi(int? limit = null)
        {
            // Base SQL tanpa TOP
            string sql = @"
                SELECT {0} 
                    mceasy_order_id AS OrderID,
                    order_status AS OrderStatus
                FROM TRC_ORDER
                WHERE 
                    mceasy_status != 'Terkirim'
                    AND mceasy_order_id IS NOT NULL
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

                if (!ok)
                    throw new Exception($"Gagal ambil halaman ke-{i} dari API get order");

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

                        _context.MCOrders.Update(existing);  // opsional, tapi aman
                        _context.SaveChanges();              // WAJIB agar tersimpan ke DB
                        updatedCount++;
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

                if (!ok)
                    throw new Exception($"Gagal ambil halaman ke-{i} dari API get fo");

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
    }


    public class OrderNotInJob
    {
        public string? FoNumber { get; set; }
        public string? JobId { get; set; }

        public string? InvNo { get; set; }

        public string? FoStatus { get; set; }

        public string? IsJob { get; set; }
    }
}




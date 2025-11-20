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
        //public async Task Run()
        //{
        //    try
        //    {
        //        var orders = await FetchOrderFromApi(10);

        //        if (orders != null && orders.Any())
        //        {
        //            await SyncOrderToDatabase(orders);
        //        }

        //        _logger.LogInformation("Sync selesai");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error ketika sync");
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

    }
}




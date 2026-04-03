using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;

namespace TMSBilling.Controllers
{
    [SessionAuthorize]
    public class ProformaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ProformaController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ─────────────────────────────────────────────
        // GET: /Proforma/Index
        // List semua proforma headers + filter
        // ─────────────────────────────────────────────
        public IActionResult Index(string? customerId, DateTime? dateFrom, DateTime? dateTo)
        {
            // Dummy customer list — ganti dengan query ke tabel customer
            //var customers = GetDummyCustomers();
            //ViewBag.Customers = new SelectList(customers, "Value", "Text");
            ViewBag.Customers = _context.CustomerMains
            .OrderBy(c => c.MAIN_CUST)
            .Select(c => new SelectListItem
            {
                Value = c.MAIN_CUST,
                Text = c.MAIN_CUST
            }).ToList();
            ViewBag.SelectedCustomer = customerId;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            // Query headers
            var query = _context.ProformaHeaders
                .Include(h => h.Details)
                .AsQueryable();

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(h => h.CustomerId == customerId);

            if (dateFrom.HasValue)
                query = query.Where(h => h.PeriodeStart >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(h => h.PeriodeEnd <= dateTo.Value);

            var list = query.OrderByDescending(h => h.CreatedAt).ToList();

            return View(list);
        }

        // ─────────────────────────────────────────────
        // GET: /Proforma/Form
        // Form generate baru atau edit existing
        // ─────────────────────────────────────────────
        public IActionResult Form(int? id)
        {
            //var customers = GetDummyCustomers();
            //ViewBag.Customers = new SelectList(customers, "Value", "Text");

            ViewBag.Customers = _context.CustomerMains
                .OrderBy(c => c.MAIN_CUST)
                .Select(c => new SelectListItem
                {
                    Value = c.MAIN_CUST,
                    Text = c.MAIN_CUST
                }).ToList();

            if (id.HasValue)
            {
                // Mode Edit: load existing header + detail
                var header = _context.ProformaHeaders
                    .Include(h => h.Details)
                    .FirstOrDefault(h => h.Id == id.Value);

                if (header == null) return NotFound();

                ViewBag.Header = header;
                ViewBag.Mode = "Edit";

                // Detail sudah ada, langsung pass ke view
                //var detailJson = JsonSerializer.Serialize(header.Details.Select(d => new
                //{
                //    d.Id,
                //    d.Type,
                //    d.Area,
                //    d.GrandTotal,
                //    d.ApprovalValue,
                //    d.Remarks
                //}));
                var detailJson = JsonSerializer.Serialize(
                    header.Details.Select(d => new
                    {
                        id = d.Id,
                        type = d.Type,
                        area = d.Area,
                        grandTotal = d.GrandTotal,
                        approvalValue = d.ApprovalValue,
                        remarks = d.Remarks
                    }),
                    new JsonSerializerOptions { PropertyNamingPolicy = null }
                );

                ViewBag.DetailJson = detailJson;
            }
            else
            {
                ViewBag.Mode = "Create";
                ViewBag.Header = null;
                ViewBag.DetailJson = "[]";
            }

            return View();
        }

        // ─────────────────────────────────────────────
        // POST: /Proforma/Generate
        // Ambil data dummy (seolah dari query), return JSON
        // ─────────────────────────────────────────────
        //[HttpPost]
        //public IActionResult Generate([FromBody] GenerateRequest req)
        //{
        //    if (string.IsNullOrEmpty(req.CustomerId) || req.DateFrom == default || req.DateTo == default)
        //        return BadRequest(new { success = false, message = "Parameter tidak lengkap" });

        //    // ─── DUMMY DATA ───────────────────────────────────────────────
        //    // Ganti blok ini dengan raw SQL query ke tabel transaksi sesungguhnya
        //    // Contoh query asli nanti:
        //    // SELECT type, area, SUM(amount) as grand_total
        //    // FROM job_transactions
        //    // WHERE customer_id = @customerId
        //    //   AND deliv_date BETWEEN @dateFrom AND @dateTo
        //    // GROUP BY type, area
        //    // ─────────────────────────────────────────────────────────────
        //    var dummyData = new List<ProformaDetailRaw>
        //    {
        //        new() { Type = "LTL",                   Area = "Jabodetabek",          GrandTotal = 93817071 },
        //        new() { Type = "LTL",                   Area = "Luar Kota",             GrandTotal = 62226803 },
        //        new() { Type = "LTL",                   Area = "Medan & Surabaya",      GrandTotal = 3542045  },
        //        new() { Type = "FTL & FCL",             Area = "All Area",              GrandTotal = 32959375 },
        //        new() { Type = "Rent",                  Area = "Jabodetabek",          GrandTotal = 14545000 },
        //        new() { Type = "Transfer Stock",        Area = "Transmart & Informa",  GrandTotal = 0        },
        //        new() { Type = "FCL",                   Area = "STO",                  GrandTotal = 71450000 },
        //        new() { Type = "Air Service",           Area = "All Area",              GrandTotal = 46936167 },
        //        new() { Type = "Dealer Return",         Area = "All Area",              GrandTotal = 11374938 },
        //        new() { Type = "Moving Stock",          Area = "Nagrak to Cakung",     GrandTotal = 0        },
        //        new() { Type = "Other (Marketing Event)", Area = "All Area",           GrandTotal = 0        },
        //    };

        //    return Ok(new { success = true, data = dummyData });
        //}

        public IActionResult Generate([FromBody] GenerateRequest req)
        {
            if (string.IsNullOrEmpty(req.CustomerId) || req.DateFrom == default || req.DateTo == default)
                return BadRequest(new { success = false, message = "Parameter tidak lengkap" });

            var data = new List<ProformaDetailRaw>();

            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            //var sql = @"
            //    SELECT
            //        jt.type,
            //        jt.area,
            //        SUM(jt.amount) AS grand_total
            //    FROM job_transactions jt
            //    WHERE jt.customer_id  = @customerId
            //      AND jt.deliv_date  >= @dateFrom
            //      AND jt.deliv_date  <= @dateTo
            //    GROUP BY jt.type, jt.area
            //    ORDER BY jt.type, jt.area
            //";

            var sql = @"
                WITH OrderDetail AS (
                    SELECT a.inv_no, SUM(a.item_qty) as QtyKoli, COALESCE(SUM(a.unit_qty), 0) as QtyPcs 
                    FROM TRC_ORDER_DTL a
                    GROUP BY a.inv_no
                ),
                ProformaDetail AS (
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY a.deliv_date, a.jobid) AS RowNo,
                    a.jobid                    AS SpkNo,
                    b.inv_no                   AS DnNo,
                    c.MAIN_CUST                AS Customer,
	                d.dest_area AS Destination, 
	                g.Area AS Area,
                    ISNULL(a.serv_type, '')    AS OrderType,
                    ISNULL(b.sell1, 0)         AS Price,
                    ISNULL(d.cnee_code, '')    AS ShipTo,
                    ISNULL(e.[ADDRESS], '')    AS Address,
                    ISNULL(e.CITY, '')         AS City,
                    CAST(f.QtyPcs AS INT)      AS QtyPcs,
                    CAST(f.QtyKoli AS INT)     AS QtyKoli,
                    CAST(a.deliv_date AS DATE) AS DeliveryDate,
                    ISNULL(a.vendor_act, '')   AS Transporter,
                    ISNULL(a.truck_size, '')   AS TruckType,
                    ISNULL(a.truck_no, '')     AS TruckNo,
                    ISNULL(a.driver_name, '')  AS Driver,
                    a.entry_date               AS SpkDate,
                    CAST(0 AS DECIMAL)         AS Volume,
                    ISNULL(a.charge_uom, '')   AS Uom
                FROM TRC_JOB_H a
                INNER JOIN TRC_JOB b         ON a.jobid     = b.jobid
                INNER JOIN TRC_CUST_GROUP c  ON c.SUB_CODE  = a.cust_group
                LEFT  JOIN TRC_ORDER d       ON b.inv_no    = d.inv_no
                LEFT  JOIN TRC_GEOFENCE e    ON d.cnee_code = e.FENCE_NAME
                LEFT  JOIN OrderDetail f     ON b.inv_no    = f.inv_no
                LEFT JOIN TRC_DESTINATION g ON d.dest_area = g.destination_code AND c.MAIN_CUST = g.MAIN_CUST
                WHERE c.MAIN_CUST = @customerId
                  AND CAST(a.deliv_date AS DATE) >= @dateFrom
                  AND CAST(a.deliv_date AS DATE) <= @dateTo
                )
                SELECT  
                OrderType AS [type],
                Area AS [area],
                SUM(Price) AS grand_total
                FROM ProformaDetail
                GROUP BY OrderType, Area
                ORDER BY Area ASC
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@customerId", req.CustomerId);
            cmd.Parameters.AddWithValue("@dateFrom", req.DateFrom.Date);
            cmd.Parameters.AddWithValue("@dateTo", req.DateTo.Date);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                data.Add(new ProformaDetailRaw
                {
                    Type = reader["type"] as string ?? "",
                    Area = reader["area"] as string ?? "",
                    GrandTotal = reader["grand_total"] != DBNull.Value
                                 ? Convert.ToDecimal(reader["grand_total"])
                                 : 0
                });
            }

            return Ok(new { success = true, data });
        }

        // ─────────────────────────────────────────────
        // POST: /Proforma/Save
        // Simpan header + detail (insert atau update)
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult Save([FromBody] SaveRequest req)
        {
            if (req == null || req.Details == null || !req.Details.Any())
                return BadRequest(new { success = false, message = "Data kosong" });

            var approvedBy = HttpContext.Session.GetString("username") ?? "Unknown";

            if (req.HeaderId.HasValue && req.HeaderId.Value > 0)
            {
                // ── UPDATE ──
                var header = _context.ProformaHeaders
                    .Include(h => h.Details)
                    .FirstOrDefault(h => h.Id == req.HeaderId.Value);

                if (header == null)
                    return NotFound(new { success = false, message = "Header tidak ditemukan" });

                header.CustomerId = req.CustomerId;
                header.CustomerName = req.CustomerName;
                header.PeriodeStart = req.DateFrom;
                header.PeriodeEnd = req.DateTo;
                header.ApprovedBy = approvedBy;

                // Hapus detail lama, insert baru
                _context.ProformaDetails.RemoveRange(header.Details);

                foreach (var d in req.Details)
                {
                    _context.ProformaDetails.Add(new ProformaDetail
                    {
                        HeaderId = header.Id,
                        Type = d.Type,
                        Area = d.Area,
                        GrandTotal = d.GrandTotal,
                        ApprovalValue = d.ApprovalValue,
                        Remarks = d.Remarks
                    });
                }

                _context.SaveChanges();
                return Ok(new { success = true, message = "Update data successfully" });
            }
            else
            {
                // ── INSERT ──
                var transNo = GenerateTransactionNo();

                var header = new ProformaHeader
                {
                    TransactionNo = transNo,
                    CustomerId = req.CustomerId,
                    CustomerName = req.CustomerName,
                    PeriodeStart = req.DateFrom,
                    PeriodeEnd = req.DateTo,
                    ApprovedBy = approvedBy,
                    CreatedAt = DateTime.Now
                };

                _context.ProformaHeaders.Add(header);
                _context.SaveChanges(); // supaya header.Id terisi

                foreach (var d in req.Details)
                {
                    _context.ProformaDetails.Add(new ProformaDetail
                    {
                        HeaderId = header.Id,
                        Type = d.Type,
                        Area = d.Area,
                        GrandTotal = d.GrandTotal,
                        ApprovalValue = d.ApprovalValue,
                        Remarks = d.Remarks
                    });
                }

                _context.SaveChanges();
                return Ok(new { success = true, message = "Saving data successfully", transactionNo = transNo });
            }
        }

        // ─────────────────────────────────────────────
        // POST: /Proforma/Delete
        // Hapus header + detail
        // ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult Delete([FromBody] DeleteRequest req)
        {
            var header = _context.ProformaHeaders
                .Include(h => h.Details)
                .FirstOrDefault(h => h.Id == req.Id);

            if (header == null)
                return NotFound(new { success = false, message = "Data tidak ditemukan" });

            _context.ProformaDetails.RemoveRange(header.Details);
            _context.ProformaHeaders.Remove(header);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Deleting data successfully" });
        }

        // ─────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────
        private string GenerateTransactionNo()
        {
            var today = DateTime.Now;
            var prefix = $"PI-{today:yyyyMMdd}-";
            var lastNo = _context.ProformaHeaders
                .Where(h => h.TransactionNo.StartsWith(prefix))
                .OrderByDescending(h => h.TransactionNo)
                .Select(h => h.TransactionNo)
                .FirstOrDefault();

            int seq = 1;
            if (lastNo != null)
            {
                var parts = lastNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                    seq = lastSeq + 1;
            }

            return $"{prefix}{seq:D3}";
        }

        private List<SelectListItem> GetDummyCustomers()
        {
            // Ganti dengan query ke tabel customer
            return new List<SelectListItem>
            {
                new() { Value = "SONY",   Text = "SONY (YPID)"    },
                new() { Value = "SAMSUNG",Text = "SAMSUNG"         },
                new() { Value = "LG",     Text = "LG Electronics"  },
            };
        }
    }

    // ─────────────────────────────────────────────
    // REQUEST MODELS
    // ─────────────────────────────────────────────
    public class GenerateRequest
    {
        public string CustomerId { get; set; } = "";
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }

    public class SaveRequest
    {
        public int? HeaderId { get; set; }
        public string CustomerId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public List<SaveDetailItem> Details { get; set; } = new();
    }

    public class SaveDetailItem
    {
        public string Type { get; set; } = "";
        public string Area { get; set; } = "";
        public decimal GrandTotal { get; set; }
        public decimal ApprovalValue { get; set; }
        public string Remarks { get; set; } = "";
    }

    public class DeleteRequest
    {
        public int Id { get; set; }
    }

    public class ProformaDetailRaw
    {
        public string Type { get; set; } = "";
        public string Area { get; set; } = "";
        public decimal GrandTotal { get; set; }
    }
}
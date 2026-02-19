using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Filters;

namespace TMSBilling.Controllers
{
    public class ReportingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;

        public ReportingController(AppDbContext context, SelectListService selectList)
        {
            _context = context;
            _selectList = selectList;
        }

        public IActionResult Index()
        {
            var config = _context.Configs
                .Where(e => e.key == "reporting")
                .FirstOrDefault();

            if (config == null)
            {
                ViewBag.ErrorMessage = "Configuration not found. Please set up the configuration first.";
                return View("Error");
            }

            ViewBag.Config = config;
            return View();
        }

        [SessionAuthorize]
        public IActionResult Custom()
        {
            var customers = _context.CustomerMains
                .Where(c => c.STATUS_FLAG == 1)
                .OrderBy(c => c.CUST_NAME)
                .ToList();

            return View("Custom", customers);
        }

        [SessionAuthorize]
        [HttpPost]
        public IActionResult DownloadCustomReport(int customerId, DateTime dateFrom, DateTime dateTo)
        {
            // Ambil nama customer berdasarkan ID
            var customerName = _context.CustomerMains
                .Where(c => c.ID == customerId)
                .Select(c => c.MAIN_CUST)
                .FirstOrDefault() ?? $"Customer_{customerId}";

            // ─── Raw SQL Query ─────────────────────────────────────────
            var sql = @"
                WITH OrderDetail AS(
	                SELECT a.inv_no, SUM(a.item_qty) as QtyKoli, COALESCE(SUM(a.unit_qty), 0) as QtyPcs FROM TRC_ORDER_DTL a
	                GROUP BY a.inv_no
                )

                SELECT 
                    ROW_NUMBER() OVER (ORDER BY a.deliv_date, a.jobid) AS RowNo,
                    a.jobid          AS SpkNo,
                    b.inv_no         AS DnNo,
                    c.MAIN_CUST      AS Customer,
                    ISNULL(d.cnee_code, '')   AS ShipTo,
                    ISNULL(e.[ADDRESS], '')   AS Address,
                    ISNULL(e.CITY, '')        AS City,
                    CAST(f.QtyPcs AS INT)      AS QtyPcs,
                    CAST(f.QtyKoli AS INT)      AS QtyKoli,
                    CAST(a.deliv_date AS DATE) AS DeliveryDate,
                    ISNULL(a.vendor_act, '')  AS Transporter,
                    ISNULL(a.serv_type, '')   AS OrderType,
                    ISNULL(a.truck_size, '')  AS TruckType,
                    ISNULL(a.truck_no, '')    AS TruckNo,
                    ISNULL(a.driver_name, '') AS Driver,
                    a.entry_date     AS SpkDate,
                    CAST(0 AS DECIMAL)  AS Volume,
                    ISNULL(a.charge_uom, '')  AS Uom,
                    ISNULL(b.sell1, 0) AS Price
                FROM 
                    TRC_JOB_H a
                    INNER JOIN TRC_JOB b          ON a.jobid     = b.jobid
                    INNER JOIN TRC_CUST_GROUP c   ON c.SUB_CODE  = a.cust_group
                    LEFT  JOIN TRC_ORDER d        ON b.inv_no    = d.inv_no
                    LEFT  JOIN TRC_GEOFENCE e     ON d.cnee_code = e.FENCE_NAME
	                LEFT JOIN OrderDetail f ON b.inv_no = f.inv_no
                WHERE 
                    c.MAIN_CUST = {0}
                    AND CAST(a.deliv_date AS DATE) >= {1}
                    AND CAST(a.deliv_date AS DATE) <= {2}
                ORDER BY
                    a.deliv_date, a.jobid";

            var data = _context.Database
                .SqlQueryRaw<ReportRow>(sql, customerName, dateFrom.Date, dateTo.Date)
                .ToList();

            // RowNo sudah dari SQL via ROW_NUMBER()

            // ─── Build Excel dengan ClosedXML ──────────────────────────
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Report");

            // Header kolom
            var headers = new[]
            {
                "No", "SPK NO.", "DN NO.", "CUSTOMER", "SHIP TO", "ADDRESS", "CITY",
                "QTY PCS", "QTY KOLI", "DELIVERY DATE", "TRANSPORTER", "ORDER TYPE",
                "TRUCK TYPE", "TRUCK NO", "DRIVER", "SPK DATE", "VOLUME", "UOM", "PRICE"
            };
            int totalCols = headers.Length;

            // ── Title ──────────────────────────────────────────────────
            ws.Cell(1, 1).Value = "CUSTOM BILLING REPORT";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            // ── Meta ───────────────────────────────────────────────────
            ws.Cell(2, 1).Value = $"Customer: {customerName}     Period: {dateFrom:dd MMM yyyy} – {dateTo:dd MMM yyyy}     Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetFontSize(9)
                .Font.SetFontColor(XLColor.FromArgb(108, 117, 125));

            // ── Header row ─────────────────────────────────────────────
            int headerRow = 4;
            // Kolom yang left-align: Address (6), City (7), Transporter (11), Order Type (12), Driver (15)
            var leftAlignCols = new HashSet<int> { 4, 5, 6, 7, 11, 12, 15 };

            for (int col = 1; col <= totalCols; col++)
            {
                ws.Cell(headerRow, col).Value = headers[col - 1];
                ws.Cell(headerRow, col).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(9)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(13, 110, 253))
                    .Alignment.SetHorizontal(leftAlignCols.Contains(col)
                        ? XLAlignmentHorizontalValues.Left
                        : XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true);
            }
            ws.Row(headerRow).Height = 28;

            // ── Data rows ──────────────────────────────────────────────
            int dataStartRow = 5;
            int r = dataStartRow;

            foreach (var item in data)
            {
                bool alt = (r - dataStartRow) % 2 == 1;
                var rowBg = alt ? XLColor.FromArgb(248, 249, 250) : XLColor.White;

                var values = new object?[]
                {
                    item.RowNo,
                    item.SpkNo,
                    item.DnNo,
                    item.Customer,
                    item.ShipTo,
                    item.Address,
                    item.City,
                    item.QtyPcs,
                    item.QtyKoli,
                    item.DeliveryDate,
                    item.Transporter,
                    item.OrderType,
                    item.TruckType,
                    item.TruckNo,
                    item.Driver,
                    item.SpkDate,
                    item.Volume,
                    item.Uom,
                    item.Price
                };

                for (int col = 1; col <= totalCols; col++)
                {
                    var cell = ws.Cell(r, col);
                    cell.Value = values[col - 1] is null ? "" : XLCellValue.FromObject(values[col - 1]!);
                    cell.Style
                        .Font.SetFontSize(9)
                        .Fill.SetBackgroundColor(rowBg)
                        .Border.SetBottomBorder(XLBorderStyleValues.Hair)
                        .Border.SetBottomBorderColor(XLColor.FromArgb(206, 212, 218))
                        .Alignment.SetHorizontal(leftAlignCols.Contains(col)
                            ? XLAlignmentHorizontalValues.Left
                            : XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                }

                // Format tanggal
                ws.Cell(r, 10).Style.NumberFormat.Format = "dd/MM/yyyy"; // DELIVERY DATE
                ws.Cell(r, 16).Style.NumberFormat.Format = "dd/MM/yyyy"; // SPK DATE

                // Format angka
                ws.Cell(r, 8).Style.NumberFormat.Format = "#,##0";      // QTY PCS
                ws.Cell(r, 9).Style.NumberFormat.Format = "#,##0";      // QTY KOLI
                ws.Cell(r, 17).Style.NumberFormat.Format = "#,##0.##";   // VOLUME
                ws.Cell(r, 19).Style.NumberFormat.Format = "#,##0";      // PRICE

                ws.Row(r).Height = 18;
                r++;
            }

            int dataEndRow = r - 1;

            // ── Total row ──────────────────────────────────────────────
            ws.Cell(r, 1).Value = "TOTAL";
            ws.Range(r, 1, r, 18).Merge();
            ws.Cell(r, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Cell(r, 19).FormulaA1 = $"=SUM(S{dataStartRow}:S{dataEndRow})";
            ws.Cell(r, 19).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 19).Style
                .Font.SetBold(true)
                .Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Range(r, 1, r, totalCols).Style
                .Fill.SetBackgroundColor(XLColor.FromArgb(240, 245, 255))
                .Border.SetTopBorder(XLBorderStyleValues.Medium)
                .Border.SetTopBorderColor(XLColor.FromArgb(13, 110, 253));

            ws.Row(r).Height = 20;

            // ── Column widths ──────────────────────────────────────────
            var colWidths = new double[]
            {
                5,   // No
                14,  // SPK NO.
                14,  // DN NO.
                18,  // CUSTOMER
                16,  // SHIP TO
                30,  // ADDRESS
                14,  // CITY
                9,   // QTY PCS
                9,   // QTY KOLI
                14,  // DELIVERY DATE
                18,  // TRANSPORTER
                13,  // ORDER TYPE
                12,  // TRUCK TYPE
                13,  // TRUCK NO
                16,  // DRIVER
                13,  // SPK DATE
                9,   // VOLUME
                7,   // UOM
                14   // PRICE
            };

            for (int col = 1; col <= totalCols; col++)
                ws.Column(col).Width = colWidths[col - 1];

            // Freeze header
            ws.SheetView.FreezeRows(headerRow);

            // ── Stream & return ────────────────────────────────────────
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var safeCustomer = customerName.Replace(" ", "_");
            var fileName = $"Report_{safeCustomer}_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xlsx";

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    // ─── DTO untuk hasil SQL query ─────────────────────────────────────────
    public class ReportRow
    {
        public long? RowNo { get; set; }
        public string? SpkNo { get; set; }
        public string? DnNo { get; set; }
        public string? Customer { get; set; }
        public string? ShipTo { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public int? QtyPcs { get; set; }
        public int? QtyKoli { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Transporter { get; set; }
        public string? OrderType { get; set; }
        public string? TruckType { get; set; }
        public string? TruckNo { get; set; }
        public string? Driver { get; set; }
        public DateTime? SpkDate { get; set; }
        public decimal? Volume { get; set; }
        public string? Uom { get; set; }
        public decimal? Price { get; set; }
    }
}
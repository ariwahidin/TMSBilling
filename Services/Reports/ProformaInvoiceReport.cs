using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;

namespace TMSBilling.Services.Reports
{
    public class ProformaInvoiceReport : IReportGenerator
    {
        private readonly AppDbContext _context;

        public ProformaInvoiceReport(AppDbContext context)
        {
            _context = context;
        }

        public string ReportType => "PROFORMA-INVOICE";

        public byte[] Generate(int customerId, string customerName, DateTime dateFrom, DateTime dateTo)
        {
            var data = FetchData(customerName, dateFrom, dateTo);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Proforma Invoice");

            BuildSheet(ws, customerName, dateFrom, dateTo, data);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ─── Query ────────────────────────────────────────────────────────────
        private List<ProformaInvoiceRow> FetchData(string customerName, DateTime dateFrom, DateTime dateTo)
        {
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

            return _context.Database
                .SqlQueryRaw<ProformaInvoiceRow>(sql, customerName, dateFrom.Date, dateTo.Date)
                .ToList();
        }

        // ─── Excel Builder ────────────────────────────────────────────────────
        private static void BuildSheet(IXLWorksheet ws, string customerName, DateTime dateFrom, DateTime dateTo, List<ProformaInvoiceRow> data)
        {
            var headers = new[]
            {
                "No", "SPK NO.", "DN NO.", "CUSTOMER", "SHIP TO", "ADDRESS", "CITY",
                "QTY PCS", "QTY KOLI", "DELIVERY DATE", "TRANSPORTER", "ORDER TYPE",
                "TRUCK TYPE", "TRUCK NO", "DRIVER", "SPK DATE", "VOLUME", "UOM", "PRICE"
            };
            int totalCols = headers.Length;
            var leftAlignCols = new HashSet<int> { 4, 5, 6, 7, 11, 12, 15 };

            // ── Title ─────────────────────────────────────────────────────────
            ws.Cell(1, 1).Value = "PROFORMA INVOICE";
            ws.Range(1, 1, 1, totalCols).Merge();
            ws.Cell(1, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            // ── Meta ──────────────────────────────────────────────────────────
            ws.Cell(2, 1).Value = $"Customer: {customerName}     Period: {dateFrom:dd MMM yyyy} – {dateTo:dd MMM yyyy}     Generated: {DateTime.Now:dd MMM yyyy HH:mm}";
            ws.Range(2, 1, 2, totalCols).Merge();
            ws.Cell(2, 1).Style
                .Font.SetFontSize(9)
                .Font.SetFontColor(XLColor.FromArgb(108, 117, 125));

            // ── Header row ────────────────────────────────────────────────────
            int headerRow = 4;
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

            // ── Data rows ─────────────────────────────────────────────────────
            int dataStartRow = 5;
            int r = dataStartRow;

            foreach (var item in data)
            {
                bool alt = (r - dataStartRow) % 2 == 1;
                var rowBg = alt ? XLColor.FromArgb(248, 249, 250) : XLColor.White;

                var values = new object?[]
                {
                    item.RowNo, item.SpkNo, item.DnNo, item.Customer,
                    item.ShipTo, item.Address, item.City,
                    item.QtyPcs, item.QtyKoli, item.DeliveryDate,
                    item.Transporter, item.OrderType, item.TruckType,
                    item.TruckNo, item.Driver, item.SpkDate,
                    item.Volume, item.Uom, item.Price
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

                ws.Cell(r, 10).Style.NumberFormat.Format = "dd/MM/yyyy"; // DELIVERY DATE
                ws.Cell(r, 16).Style.NumberFormat.Format = "dd/MM/yyyy"; // SPK DATE
                ws.Cell(r, 8).Style.NumberFormat.Format = "#,##0";      // QTY PCS
                ws.Cell(r, 9).Style.NumberFormat.Format = "#,##0";      // QTY KOLI
                ws.Cell(r, 17).Style.NumberFormat.Format = "#,##0.##";   // VOLUME
                ws.Cell(r, 19).Style.NumberFormat.Format = "#,##0";      // PRICE

                ws.Row(r).Height = 18;
                r++;
            }

            int dataEndRow = r - 1;

            // ── Total row ─────────────────────────────────────────────────────
            ws.Cell(r, 1).Value = "TOTAL";
            ws.Range(r, 1, r, totalCols - 1).Merge();
            ws.Cell(r, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Cell(r, 19).FormulaA1 = $"=SUM(S{dataStartRow}:S{dataEndRow})";
            ws.Cell(r, 19).Style
                .NumberFormat.SetFormat("#,##0");
            ws.Cell(r, 19).Style
                .Font.SetBold(true)
                .Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Range(r, 1, r, totalCols).Style
                .Fill.SetBackgroundColor(XLColor.FromArgb(240, 245, 255))
                .Border.SetTopBorder(XLBorderStyleValues.Medium)
                .Border.SetTopBorderColor(XLColor.FromArgb(13, 110, 253));

            ws.Row(r).Height = 20;

            // ── Column widths ─────────────────────────────────────────────────
            double[] colWidths = { 5, 14, 14, 18, 16, 30, 14, 9, 9, 14, 18, 13, 12, 13, 16, 13, 9, 7, 14 };
            for (int col = 1; col <= totalCols; col++)
                ws.Column(col).Width = colWidths[col - 1];

            ws.SheetView.FreezeRows(headerRow);
        }
    }

    // ─── DTO ─────────────────────────────────────────────────────────────────
    public class ProformaInvoiceRow
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
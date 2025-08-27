using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;

namespace TMSBilling.Controllers
{

    [SessionAuthorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        public OrderController(AppDbContext context, SelectListService selectList, HttpClient httpClient, IOptions<ApiSettings> apiSettings)
        {
            _context = context;
            _selectList = selectList;
            _httpClient = httpClient;
            _apiSettings = apiSettings.Value;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders
                .OrderByDescending(o => o.entry_date)
                .Take(100)
                .ToList();

            return View(orders); // kirim list ke view
        }

        public IActionResult Import() {
            return View();
        }

        public IActionResult Form(int? id)
        {
            var vm = new OrderViewModel();
            ViewBag.ListCustomer = _selectList.getCustomers();
            ViewBag.ListWarehouse = _selectList.GetWarehouse();
            ViewBag.ListConsignee = _selectList.GetConsignee();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
            ViewBag.ListUoM = _selectList.GetChargeUoms();
            ViewBag.ListModa = _selectList.GetServiceModas();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();

            if (id.HasValue)
            {
                vm.Header = _context.Orders.FirstOrDefault(o => o.id_seq == id.Value) ?? new Order();
                vm.Details = _context.OrderDetails
                    .Where(d => d.id_seq_order == id.Value)
                    .ToList();
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] OrderViewModel model)
        {
            if (model == null || model.Header == null || model.Details == null || model.Details.Count == 0)
            {
                return BadRequest(new { success = false, message = "Data tidak lengkap!" });
            }

            var header = model.Header;
            var details = model.Details;

            // ==== Validasi ke master ====
            var errors = new List<string>();

            if (!_context.Warehouses.Any(w => w.wh_code == header.wh_code))
                errors.Add($"Warehouse '{header.wh_code}' tidak ditemukan");

            if (!_context.Customers.Any(s => s.CUST_CODE == header.sub_custid))
                errors.Add($"SubCustomer '{header.sub_custid}' tidak ditemukan");

            if (!_context.Consignees.Any(c => c.CNEE_CODE == header.cnee_code))
                errors.Add($"Consignee '{header.cnee_code}' tidak ditemukan");

            if (!_context.Origins.Any(l => l.origin_code == header.origin_id))
                errors.Add($"Origin '{header.origin_id}' tidak ditemukan");


            if (header.id_seq == 0)
                if (_context.Orders.Any(o => o.inv_no == header.inv_no))
                errors.Add($"Inv No '{header.inv_no}' already exists!");

            if (errors.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Validasi gagal",
                    errors
                });
            }

            var deliveryDateTime = header.delivery_date?.ToString("yyyy-MM-ddTHH:mm:sszzz");

            // === Proses Simpan ===
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var username = HttpContext.Session.GetString("username") ?? "System";

                if (header.id_seq == 0)
                {


                    // INSERT KE API

                    // set header Authorization dulu
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

                    var payload = new
                    {
                        customer_id = "e43b5605-d9c8-4bfb-83e6-80e7cf562e6a", // atau ambil dari field lain kalau beda
                        billed_customer_id = "e43b5605-d9c8-4bfb-83e6-80e7cf562e6a",
                        destination_address_id = 9122, // sementara hardcode, nanti bisa mapping
                        expected_delivered_on = deliveryDateTime,
                        expected_pickup_on = deliveryDateTime,
                        origin_address_id = 8898,
                        shipment_number = header.inv_no
                    };

                    // ✅ PERBAIKAN: Serialize object langsung dengan options untuk tidak escape karakter
                    var options = new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    var jsonPayload = JsonSerializer.Serialize(payload, options);
                    var jsonContent = new StringContent(
                        jsonPayload,
                        Encoding.UTF8,
                        "application/json"
                    );

                    Console.WriteLine("=== DEBUG PAYLOAD DETAIL ===");
                    Console.WriteLine($"customer_id: {payload.customer_id}");
                    Console.WriteLine($"billed_customer_id: {payload.billed_customer_id}");
                    Console.WriteLine($"destination_address_id: {payload.destination_address_id}");
                    Console.WriteLine($"expected_delivered_on: {payload.expected_delivered_on}");
                    Console.WriteLine($"expected_pickup_on: {payload.expected_pickup_on}");
                    Console.WriteLine($"origin_address_id: {payload.origin_address_id}");
                    Console.WriteLine($"shipment_number: {payload.shipment_number}");
                    Console.WriteLine("=== JSON PAYLOAD ===");
                    Console.WriteLine(jsonPayload);
                    Console.WriteLine("=== HEADERS ===");
                    Console.WriteLine($"Authorization: {_httpClient.DefaultRequestHeaders.Authorization}");
                    Console.WriteLine($"Content-Type: application/json");
                    Console.WriteLine("=====================");

                    var response = await _httpClient.PostAsync(
                        $"{_apiSettings.BaseUrl}/delivery-order",
                        jsonContent
                    );

                    var responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        transaction.Rollback();
                        return BadRequest(new
                        {
                            success = false,
                            message = "Gagal kirim ke API ne",
                            detail = responseText
                        });
                    }

                    // KALO BERHASIL BARU LANJUT

                    // INSERT
                    header.order_status = 0;
                    header.entry_date = DateTime.Now;
                    header.entry_user = username;
                    _context.Orders.Add(header);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // UPDATE
                    var existingHeader = _context.Orders.FirstOrDefault(o => o.id_seq == header.id_seq);
                    if (existingHeader == null)
                        return NotFound(new { success = false, message = "Data tidak ditemukan untuk update." });

                    existingHeader.wh_code = header.wh_code;
                    existingHeader.sub_custid = header.sub_custid;
                    existingHeader.cnee_code = header.cnee_code;
                    existingHeader.inv_no = header.inv_no;
                    existingHeader.delivery_date = header.delivery_date;
                    existingHeader.origin_id = header.origin_id;
                    existingHeader.dest_area = header.dest_area;
                    existingHeader.truck_size = header.truck_size;
                    existingHeader.moda_req = header.moda_req;
                    existingHeader.serv_req = header.serv_req;
                    existingHeader.tot_pkgs = header.tot_pkgs;
                    existingHeader.do_rcv_time = header.do_rcv_time;
                    existingHeader.remark = header.remark;
                    existingHeader.order_status = header.order_status ?? 0;
                    existingHeader.uom = header.uom;
                    existingHeader.update_date = DateTime.Now;
                    existingHeader.update_user = username;

                    _context.Orders.Update(existingHeader);

                    // hapus detail lama
                    var existingDetails = await _context.OrderDetails.Where(d => d.id_seq_order == header.id_seq).ToListAsync();
                    _context.OrderDetails.RemoveRange(existingDetails);
                    await _context.SaveChangesAsync();
                }

                // Insert ulang detail
                foreach (var detail in details)
                {
                    detail.id_seq = 0; // pastikan selalu reset
                    detail.id_seq_order = header.id_seq;
                    detail.entry_date = DateTime.Now;
                    detail.entry_user = username;
                    detail.update_date = DateTime.Now;
                    detail.update_user = username;
                    _context.OrderDetails.Add(detail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Data berhasil disimpan!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan: " + msg });
            }
        }

        [HttpGet]
        public IActionResult DownloadSample()
        {
            using var workbook = new XLWorkbook();

            // HEADER
            var headerSheet = workbook.Worksheets.Add("Header");
            headerSheet.Cell("A1").Value = "wh_code";
            headerSheet.Cell("B1").Value = "sub_custid";
            headerSheet.Cell("C1").Value = "cnee_code";
            headerSheet.Cell("D1").Value = "inv_no";
            headerSheet.Cell("E1").Value = "delivery_date";
            headerSheet.Cell("F1").Value = "origin_id";
            headerSheet.Cell("G1").Value = "dest_area";
            headerSheet.Cell("H1").Value = "tot_pkgs";
            headerSheet.Cell("I1").Value = "uom";
            headerSheet.Cell("J1").Value = "pallet_consume";
            headerSheet.Cell("K1").Value = "pallet_delivery";
            headerSheet.Cell("L1").Value = "si_no";
            headerSheet.Cell("M1").Value = "do_rcv_date";
            headerSheet.Cell("N1").Value = "do_rcv_time";
            headerSheet.Cell("O1").Value = "moda_req";
            headerSheet.Cell("P1").Value = "serv_req";
            headerSheet.Cell("Q1").Value = "truck_size";
            headerSheet.Cell("R1").Value = "remark";

            headerSheet.Cell("A2").Value = "NWW1-NRML";
            headerSheet.Cell("B2").Value = "SUT001";
            headerSheet.Cell("C2").Value = "CONS11";
            headerSheet.Cell("D2").Value = "INV001";
            headerSheet.Cell("E2").Value = DateTime.Today;
            headerSheet.Cell("F2").Value = "Cikarang";
            headerSheet.Cell("G2").Value = "Bandung";
            headerSheet.Cell("H2").Value = 10;
            headerSheet.Cell("I2").Value = "KG";
            headerSheet.Cell("J2").Value = "2";
            headerSheet.Cell("K2").Value = "5";
            headerSheet.Cell("L2").Value = "SI001";
            headerSheet.Cell("M2").Value = DateTime.Today;
            headerSheet.Cell("N2").Value = DateTime.Now.ToString("HH:mm");
            headerSheet.Cell("O2").Value = "LAND";
            headerSheet.Cell("P2").Value = "FTL";
            headerSheet.Cell("Q2").Value = "1T";
            headerSheet.Cell("R2").Value = "";

            // DETAIL
            var detailSheet = workbook.Worksheets.Add("Details");
            detailSheet.Cell("A1").Value = "inv_no";
            detailSheet.Cell("B1").Value = "item_name";
            detailSheet.Cell("C1").Value = "item_category";
            detailSheet.Cell("D1").Value = "item_length";
            detailSheet.Cell("E1").Value = "item_width";
            detailSheet.Cell("F1").Value = "item_height";
            detailSheet.Cell("G1").Value = "item_weight";
            detailSheet.Cell("H1").Value = "pkg_unit";
            detailSheet.Cell("I1").Value = "pack_unit";
            detailSheet.Cell("J1").Value = "pallet_qty";
            detailSheet.Cell("K1").Value = "koli_qty";
            detailSheet.Cell("L1").Value = "full_addres";


            detailSheet.Cell("A2").Value = "INV001";
            detailSheet.Cell("B2").Value = "ITM001";
            detailSheet.Cell("C2").Value = "BOOK";
            detailSheet.Cell("D2").Value = 4;
            detailSheet.Cell("E2").Value = 5;
            detailSheet.Cell("F2").Value = 6;
            detailSheet.Cell("G2").Value = 7;
            detailSheet.Cell("H2").Value = "PCS";
            detailSheet.Cell("I2").Value = "BOX";
            detailSheet.Cell("J2").Value = 2;
            detailSheet.Cell("K2").Value = 1;
            detailSheet.Cell("L2").Value = "CAKUNG CILINCING, JAKARTA UTARA";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sample_import_order.xlsx");
        }


        [HttpPost]
        public IActionResult Preview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File kosong");

            var result = new List<OrderViewModel>();
            var errors = new List<object>();
            var uniqueKeys = new HashSet<string>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var headerSheet = workbook.Worksheet("Header");
            var detailSheet = workbook.Worksheet("Details");

            int row = 2;
            while (!headerSheet.Cell(row, 1).IsEmpty())
            {
                string invoice_no = headerSheet.Cell(row, 4).GetString();

                // === Kombinasi 7 kolom unik ===
                string uniqueKey = string.Join("|", Enumerable.Range(1, 7).Select(c => headerSheet.Cell(row, c).GetString()));
                if (!uniqueKeys.Add(uniqueKey))
                {
                    errors.Add(new { row, section = "header", field = "composite key", message = "Duplikat kombinasi 7 kolom (wh_code - dest_area) ditemukan." });
                }

                var header = new Order();

                try
                {
                    string whCode = headerSheet.Cell(row, 1).GetString();
                    string subCustId = headerSheet.Cell(row, 2).GetString();
                    string cneeCode = headerSheet.Cell(row, 3).GetString();
                    string originId = headerSheet.Cell(row, 6).GetString();

                    // === Validasi ke master table (ganti sesuai tabel kamu)
                    if (!_context.Warehouses.Any(w => w.wh_code == whCode))
                        errors.Add(new { row, section = "header", field = "wh_code", message = $"Warehouse '{whCode}' tidak ditemukan" });

                    if (!_context.Customers.Any(s => s.CUST_CODE == subCustId))
                        errors.Add(new { row, section = "header", field = "sub_custid", message = $"SubCustomer '{subCustId}' tidak ditemukan" });

                    if (!_context.Consignees.Any(c => c.CNEE_CODE == cneeCode))
                        errors.Add(new { row, section = "header", field = "cnee_code", message = $"Consignee '{cneeCode}' tidak ditemukan" });

                    if (!_context.Origins.Any(l => l.origin_code == originId))
                        errors.Add(new { row, section = "header", field = "origin_id", message = $"Origin '{originId}' tidak ditemukan" });

                    header = new Order
                    {
                        wh_code = whCode,
                        sub_custid = subCustId,
                        cnee_code = cneeCode,
                        inv_no = invoice_no,
                        delivery_date = DateTime.TryParse(headerSheet.Cell(row, 5).GetString(), out var dateVal) ? dateVal : (DateTime?)null,
                        origin_id = originId,
                        dest_area = headerSheet.Cell(row, 7).GetString(),
                        tot_pkgs = decimal.TryParse(headerSheet.Cell(row, 8).GetValue<string>(), out var tot) ? tot : null,
                        uom = headerSheet.Cell(row, 9).GetString(),
                        pallet_consume = long.TryParse(headerSheet.Cell(row, 10).GetValue<string>(), out var pc) ? (int?)pc : null,
                        pallet_delivery = long.TryParse(headerSheet.Cell(row, 11).GetValue<string>(), out var pd) ? (int?)pd : null,
                        si_no = headerSheet.Cell(row, 12).GetString(),
                        do_rcv_date = DateTime.TryParse(headerSheet.Cell(row, 13).GetString(), out var drd) ? drd : (DateTime?)null,
                        do_rcv_time = headerSheet.Cell(row, 14).GetString(),
                        moda_req = headerSheet.Cell(row, 15).GetString(),
                        serv_req = headerSheet.Cell(row, 16).GetString(),
                        truck_size = headerSheet.Cell(row, 17).GetString(),
                        remark = headerSheet.Cell(row, 18).GetString()
                    };
                }
                catch (Exception ex)
                {
                    errors.Add(new { row, section = "header", field = "ALL", message = "Format tidak valid: " + ex.Message });
                }

                // ==== Details ====
                var details = new List<OrderDetail>();
                int drow = 2;
                while (!detailSheet.Cell(drow, 1).IsEmpty())
                {
                    string invNoDetail = detailSheet.Cell(drow, 1).GetString();
                    if (invNoDetail == invoice_no)
                    {
                        try
                        {
                            string itemName = detailSheet.Cell(drow, 2).GetString();
                            //if (!_context.ChargeUoms.Any(i => i.charge_name == ))
                            //{
                            //    errors.Add(new { row = drow, section = "detail", field = "item_name", message = $"Item '{itemName}' tidak ditemukan di master" });
                            //}

                            details.Add(new OrderDetail
                            {
                                inv_no = invNoDetail,
                                item_name = itemName,
                                item_category = detailSheet.Cell(drow, 3).GetString(),
                                item_length = long.TryParse(detailSheet.Cell(drow, 4).GetValue<string>(), out var l) ? (int?)l : null,
                                item_width = long.TryParse(detailSheet.Cell(drow, 5).GetValue<string>(), out var w) ? (int?)w : null,
                                item_height = long.TryParse(detailSheet.Cell(drow, 6).GetValue<string>(), out var h) ? (int?)h : null,
                                item_wgt = long.TryParse(detailSheet.Cell(drow, 7).GetValue<string>(), out var wg) ? (int?)wg : null,
                                pkg_unit = detailSheet.Cell(drow, 8).GetString(),
                                pack_unit = detailSheet.Cell(drow, 9).GetString(),
                                pallet_qty = long.TryParse(detailSheet.Cell(drow, 10).GetValue<string>(), out var pq) ? (int?)pq : null,
                                koli_qty = long.TryParse(detailSheet.Cell(drow, 11).GetValue<string>(), out var kq) ? (int?)kq : null,
                                full_addres = detailSheet.Cell(drow, 12).GetString()
                            });
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new { row = drow, section = "detail", field = "ALL", message = "Detail tidak valid: " + ex.Message });
                        }
                    }
                    drow++;
                }

                result.Add(new OrderViewModel
                {
                    Header = header,
                    Details = details
                });

                row++;
            }

            return Ok(new
            {
                isValid = errors.Count == 0,
                message = errors.Count == 0 ? "Data valid" : "Terdapat error pada data",
                errors,
                data = result
            });
        }


        [HttpPost]
        public IActionResult SaveExcel([FromBody] List<OrderViewModel> data)
        {
            if (data == null || data.Count == 0)
                return BadRequest(new { success = false, message = "Data tidak ditemukan." });

            foreach (var item in data)
            {
                var header = item.Header;
                var details = item.Details;

                if (header == null || details == null)
                    continue;

                // Simpan Header
                header.order_status = 0;
                header.entry_user = HttpContext.Session.GetString("username") ?? "System";
                header.entry_date = DateTime.Now;
                _context.Orders.Add(header);
                _context.SaveChanges(); // Supaya header.id_seq terisi otomatis

                // Set id_seq_order pada detail dan simpan
                foreach (var detail in details)
                {
                    detail.id_seq_order = header.id_seq;
                    detail.entry_user = header.entry_user;
                    detail.entry_date = DateTime.Now;
                }

                _context.OrderDetails.AddRange(details);
                _context.SaveChanges();
            }

            return Ok(new { success = true, message = "Data berhasil disimpan." });
        }

    }

}

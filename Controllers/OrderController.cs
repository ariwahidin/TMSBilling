using ClosedXML.Excel;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TMSBilling.Data;
using TMSBilling.Filters;
using TMSBilling.Models;
using TMSBilling.Models.ViewModels;
using TMSBilling.Services;

namespace TMSBilling.Controllers
{

    [SessionAuthorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SelectListService _selectList;
        private readonly HttpClient _httpClient;
        private readonly ApiSettings _apiSettings;
        private readonly ApiService _apiService;

        public OrderController(AppDbContext context, SelectListService selectList, HttpClient httpClient, IOptions<ApiSettings> apiSettings, ApiService apiService)
        {
            _context = context;
            _selectList = selectList;
            _httpClient = httpClient;
            _apiSettings = apiSettings.Value;
            _apiService = apiService;
        }



        public IActionResult Index()
        {
            //var orders = _context.Orders
            //    .OrderByDescending(o => o.entry_date)
            //    .Take(100)
            //    .ToList();

            //return View(orders); // kirim list ke view

            var sql = @"
                WITH od AS (
                    SELECT 
                        id_seq_order, 
                        COUNT(item_name) AS total_item,
                        SUM(item_qty) AS total_qty
                    FROM TRC_ORDER_DTL
                    GROUP BY id_seq_order
                )
                SELECT 
                    a.id_seq AS IdSeq, 
                    a.wh_code AS WhCode, 
                    a.sub_custid AS SubCustId,
                    a.cnee_code AS CneeCode,
                    a.inv_no AS InvNo,
                    CAST(a.delivery_date AS date) AS DeliveryDate,
                    a.origin_id AS OriginId,
                    a.dest_area AS DestArea,
                    a.order_status AS OrderStatus,
                    a.mceasy_order_id AS McEasyOrderId,
                    COALESCE(od.total_item, 0) AS TotalItem,
                    COALESCE(od.total_qty, 0) AS TotalQty
                FROM TRC_ORDER a 
                LEFT JOIN od ON a.id_seq = od.id_seq_order
                ORDER BY a.id_seq DESC
            ";

            var data = _context.OrderSummaryView
                .FromSqlRaw(sql)
                .ToList();

            return View(data);
        }

        public IActionResult Import()
        {
            return View();
        }

        public IActionResult Form(int? id)
        {
            var vm = new OrderViewModel();
            ViewBag.ListCustomer = _selectList.getCustomerGroup();
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
            //var details = model.Details;

            // ==== Validasi ke master ====
            var errors = new List<string>();

            if (!_context.Warehouses.Any(w => w.wh_code == header.wh_code))
                errors.Add($"Warehouse '{header.wh_code}' not found");

            if (!_context.Consignees.Any(c => c.CNEE_CODE == header.cnee_code))
                errors.Add($"Consignee '{header.cnee_code}' not found");

            if (!_context.Origins.Any(l => l.origin_code == header.origin_id))
                errors.Add($"Origin '{header.origin_id}' not found");


            if (header.id_seq == 0)
                if (_context.Orders.Any(o => o.inv_no == header.inv_no))
                    errors.Add($"Inv No '{header.inv_no}' already exists!");

            if (errors.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed validation",
                    errors
                });
            }

            var customerGroup = _context.CustomerGroups.FirstOrDefault(cg => cg.SUB_CODE == header.sub_custid);
            if (customerGroup == null || string.IsNullOrEmpty(customerGroup.MCEASY_CUST_ID))
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Customer Group '{header.sub_custid}' tidak memiliki MCEASY_CUST_ID yang valid."
                });
            }

            //var deliveryDateTime = header.delivery_date?.ToString("yyyy-MM-ddTHH:mm:sszzz");
            var deliveryDateTime = header.delivery_date.HasValue ? header.delivery_date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null;
            var pickupDateTime = header.pickup_date.HasValue ? header.pickup_date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null;


            var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

            if (customer == null)
            {
                  return NotFound(new { success = false, message = $"Customer '{customerGroup.CUST_CODE}' tidak ditemukan." });
            }

            //if (customer.API_FLAG == 1) {
            //    var payload = new
            //    {
            //        customer_id = customerGroup.MCEASY_CUST_ID,
            //        billed_customer_id = customerGroup.MCEASY_CUST_ID,
            //        origin_address_id = header.mceasy_origin_address_id,
            //        destination_address_id = header.mceasy_destination_address_id,
            //        expected_pickup_on = pickupDateTime,
            //        expected_delivered_on = deliveryDateTime,
            //        shipment_number = header.inv_no
            //    };

            //    if (header.id_seq == 0) {


            //        var (ok, json) = await _apiService.SendRequestAsync(
            //                HttpMethod.Post,
            //                "order/api/web/v1/delivery-order",
            //                payload
            //        );


            //        if (!ok)
            //        {
            //            return BadRequest(new
            //            {
            //                success = false,
            //                message = "Gagal kirim ke API Store Order",
            //                detail = json
            //            });
            //        }


            //    } else {

            //        var (ok, json) = await _apiService.SendRequestAsync(
            //               HttpMethod.Patch,
            //               $"order/api/web/v1/delivery-order/{header.mceasy_order_id}",
            //               payload
            //       );


            //        if (!ok)
            //        {
            //            return BadRequest(new
            //            {
            //                success = false,
            //                message = "Gagal kirim ke API Store Order",
            //                detail = json
            //            });
            //        }

            //    }




            //    var order_id = json.GetProperty("data").GetProperty("id").GetString();
            //    var do_number = json.GetProperty("data").GetProperty("number").GetString();
            //    header.mceasy_order_id = order_id;
            //    header.mceasy_do_number = do_number;
            //}

            if (customer.API_FLAG == 1)
            {
                var payload = new
                {
                    customer_id = customerGroup.MCEASY_CUST_ID,
                    billed_customer_id = customerGroup.MCEASY_CUST_ID,
                    origin_address_id = header.mceasy_origin_address_id,
                    destination_address_id = header.mceasy_destination_address_id,
                    expected_pickup_on = pickupDateTime,
                    expected_delivered_on = deliveryDateTime,
                    shipment_number = header.inv_no
                };

                bool ok;
                JsonElement json = default;

                if (header.id_seq == 0)
                {
                    (ok, json) = await _apiService.SendRequestAsync(
                        HttpMethod.Post,
                        "order/api/web/v1/delivery-order",
                        payload
                    );

                    header.mceasy_order_id = json.GetProperty("data").GetProperty("id").GetString();
                    header.mceasy_do_number = json.GetProperty("data").GetProperty("number").GetString();
                }
                else
                { 
                
                    var orderHeader = await _context.Orders.FirstOrDefaultAsync(h => h.id_seq == header.id_seq);

                    if (orderHeader == null) { 
                        return NotFound(new {message = "Order not found", success = true});
                    }

                    (ok, json) = await _apiService.SendRequestAsync(
                        HttpMethod.Patch,
                        $"order/api/web/v1/delivery-order/{orderHeader.mceasy_order_id}",
                        payload
                    );
                }

                if (!ok)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Store Order",
                        detail = json
                    });
                }
            }



            // === Proses Simpan ===
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var username = HttpContext.Session.GetString("username") ?? "System";

                if (header.id_seq == 0)
                {

                    // INSERT
                    header.mceasy_origin_address_id = header.mceasy_origin_address_id;
                    header.mceasy_destination_address_id = header.mceasy_destination_address_id;
                    header.mceasy_origin_name = header.mceasy_origin_name;
                    header.mceasy_dest_name = header.mceasy_dest_name;
                    header.order_status = 0;
                    header.entry_date = DateTime.Now;
                    header.entry_user = username;
                    _context.Orders.Add(header);
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
                    existingHeader.pickup_date = header.pickup_date;
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
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Created order successfully", data = header });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var msg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan: " + msg });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Confirm(Guid? id)
        {
            if (id == null)
                return BadRequest("ID tidak valid");
            var order = _context.Orders.FirstOrDefault(o => o.mceasy_order_id == id.ToString());

            if (order == null)
                return NotFound("Data tidak ditemukan");
            if (order.order_status != 0)
                return BadRequest("Order sudah dikonfirmasi atau diproses lebih lanjut.");

            // set header Authorization dulu
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiSettings.Token.Replace("Bearer ", ""));

            var payload = new
            {
                status = "CONFIRMED"
            };

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

            Console.WriteLine("=== JSON PAYLOAD ===");
            Console.WriteLine(jsonPayload);
            Console.WriteLine("=== HEADERS ===");
            Console.WriteLine($"Authorization: {_httpClient.DefaultRequestHeaders.Authorization}");
            Console.WriteLine($"Content-Type: application/json");
            Console.WriteLine("=====================");

            var response = await _httpClient.PostAsync(
                $"{_apiSettings.BaseUrl}/order/api/web/v1/delivery-order/{id}/transition",
                jsonContent
            );

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = JObject.Parse(responseText);
                // Ambil 1 pesan error pertama
                var firstError = errorJson["errors_v2"]?["issues"]?
                                    .FirstOrDefault()?["message"]?.ToString();


                return BadRequest(new
                {
                    success = false,
                    message = firstError ?? "Gagal kirim API",
                    detail = responseText
                });
            }
            order.mceasy_status = payload.status;
            order.order_status = 1; // misal 1 = confirmed
            order.update_date = DateTime.Now;
            order.update_user = HttpContext.Session.GetString("username") ?? "System";
            _context.Orders.Update(order);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Order berhasil dikonfirmasi." });
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


        public async Task<IActionResult> FormLoad(int? id)
        {
            var (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Get,
                $"/order/api/web/v1/product?limit={1000}&page={1}&search={""}",
                new { }
            );
            if (!ok)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Gagal kirim ke API Add Order Load",
                    detail = json
                });
            }
            var productsJson = json.GetProperty("data").GetProperty("paginated_result");
            var products = JsonSerializer.Deserialize<List<Product>>(
                productsJson.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var query = _context.Orders
            .Where(o => o.id_seq == id);

            Console.WriteLine($"Query id_seq: {id}");
            Console.WriteLine(query.ToQueryString());

            var order = await query.FirstOrDefaultAsync();
            if (order == null)
            {
                return NotFound( new {message = "Order not found"});
            }


            var orderLoad = await _context.OrderDetails
                .Where(od => od.id_seq_order == order.id_seq)
                .ToListAsync();

            ViewBag.OrderIDMcEasy = order?.mceasy_order_id;
            ViewBag.MasterProducts = products;
            ViewBag.OrderLoad = orderLoad;
            ViewBag.Order = order;

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> SaveProduct([FromBody] ProductViewModel model)
        {

            Console.WriteLine("=== DEBUG REQUEST PAYLOAD ===");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(model));

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "All fields must be filled in" });
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.id_seq == model.order_h_id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            var customerGroup = await _context.CustomerGroups.FirstOrDefaultAsync(cg => cg.SUB_CODE == order.sub_custid);

            if (customerGroup == null) {
                return BadRequest(new { message = $"Customer group not found" });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CUST_CODE == customerGroup.CUST_CODE);

            if (customer == null) {
                return BadRequest(new { message = $"Customer not found" });
            }

            

            var NewOrderDetail = new OrderDetail
            {
                id_seq_order = model.order_h_id ?? 0,
                wh_code = order.wh_code,
                sub_custid = order.sub_custid,
                cnee_code = order.cnee_code,
                inv_no = order.inv_no,
                delivery_date = order.delivery_date,
                pkg_unit = model.uom,
                item_name = model.name,
                item_length = model.length,
                item_category = model.category,
                item_height = model.height,
                item_width = model.width,
                item_wgt = (int?)model.weight,
                item_qty = (int?)model.quantity,
                pack_unit = model.uom,
                entry_date = DateTime.Now,
                entry_user = HttpContext.Session.GetString("username") ?? "System"
            };

            if (customer.API_FLAG == 1)
            {

                var payload = new
                {
                    product_id = model.product_id,
                    quantity = model.quantity,
                    uom = model.uom,
                    note = model.note,
                };

                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Post,
                    "order/api/web/v1/delivery-order/" + model.order_id + "/load",
                    payload
                );
                if (!ok)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Add Order Load",
                        detail = json
                    });
                }

                NewOrderDetail.mceasy_order_id = order.mceasy_order_id;
                NewOrderDetail.mceasy_order_dtl_id = json.GetProperty("data").GetProperty("id").GetString();
                NewOrderDetail.mceasy_product_id = model.product_id;

            }

            _context.OrderDetails.Add(NewOrderDetail);
            await _context.SaveChangesAsync();

            return Ok(new {message = "Created order load succesfully", data = NewOrderDetail});

        }


        [HttpPatch] 
        public async Task<IActionResult> EditProduct([FromBody] ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "All fields must be filled in" });
            }
            var orderLoad = await _context.OrderDetails.FirstOrDefaultAsync(od => od.id_seq == (int?)model.id_seq);
            if (orderLoad == null)
            {
                return NotFound(new { message = "Order load not found" });
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.id_seq == model.order_h_id);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            var customerGroup = await _context.CustomerGroups.FirstOrDefaultAsync(cg => cg.SUB_CODE == order.sub_custid);
            if (customerGroup == null) {
                return BadRequest(new { message = $"Customer group not found" });
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CUST_CODE == customerGroup.CUST_CODE);
            if (customer == null) {
                return BadRequest(new { message = $"Customer not found" });
            }

            orderLoad.item_name = model.name;
            orderLoad.item_length = model.length;
            orderLoad.item_category = model.category;
            orderLoad.item_height = model.height;
            orderLoad.item_width = model.width;
            orderLoad.item_wgt = (int?)model.weight;
            orderLoad.item_qty = (int?)model.quantity;
            orderLoad.pkg_unit = model.uom;
            orderLoad.pack_unit = model.uom;
            orderLoad.update_date = DateTime.Now;
            orderLoad.update_user = HttpContext.Session.GetString("username") ?? "System";
            
            if (customer.API_FLAG == 1 && orderLoad.mceasy_order_dtl_id != null)
            {
                var payload = new
                {
                    product_id = orderLoad.mceasy_product_id,
                    quantity = model.quantity,
                    uom = model.uom,
                    note = model.note,
                };
                var (ok, json) = await _apiService.SendRequestAsync(
                    HttpMethod.Patch,
                    "order/api/web/v1/delivery-order/" + model.order_id + "/load/" + orderLoad.mceasy_order_dtl_id,
                    payload
                );
                if (!ok)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Edit Order Load",
                        detail = json
                    });
                }
            }

            _context.OrderDetails.Update(orderLoad);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated order load succesfully", data = orderLoad });
        }


        [HttpGet]
        public async Task<IActionResult> DeleteLoad(int? id)
        {
            if (id == null)
                return BadRequest("ID tidak valid");
           
            var orderLoad = await _context.OrderDetails.FirstOrDefaultAsync(od => od.id_seq == id);

            if(orderLoad == null)
            {
                return NotFound(new { message = "Order load not found" });
            }

            if (orderLoad.mceasy_order_dtl_id != null)
            {
                var (ok, json) = await _apiService.SendRequestAsync(
                   HttpMethod.Delete,
                   $"delivery-order/{orderLoad.mceasy_order_id}/load/{orderLoad.mceasy_order_dtl_id}",
                   new { }
               );
                if (!ok)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal kirim ke API Delete Order Load",
                        detail = json
                    });
                }
            }
            _context.OrderDetails.Remove(orderLoad);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Order load berhasil dihapus." });
        }
    }

    public class ProductViewModel
    {

        public int? id_seq { get; set; }
        public int? order_h_id { get; set; }
        public string? order_id { get; set; }
        public string? product_id { get; set; }
        public string? name { get; set; }
        public string? sku { get; set; }
        public string? description { get; set; }
        public string? uom { get; set; }
        public decimal? weight { get; set; }
        public decimal? volume { get; set; }
        public decimal? price { get; set; }
        public decimal? quantity { get; set; }
        public string? note { get; set; }
        public int? length { get; set; }

        public int? width { get; set; }

        public int? height { get; set; }

        public string? category { get; set; }
    }

    public class OrderSummaryViewModel
    {
        public int IdSeq { get; set; }
        public string? WhCode { get; set; }
        public string? SubCustId { get; set; }
        public string? CneeCode { get; set; }
        public string? InvNo { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string? OriginId { get; set; }
        public string? DestArea { get; set; }
        public byte? OrderStatus { get; set; }

        public string? McEasyOrderId { get; set; }
        public int? TotalItem { get; set; }
        public int? TotalQty { get; set; }
    }

}



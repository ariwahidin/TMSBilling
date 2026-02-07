using ClosedXML.Excel;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        private readonly SyncronizeWithMcEasy _sync;

        public OrderController(AppDbContext context, 
            SelectListService selectList, 
            HttpClient httpClient, 
            IOptions<ApiSettings> apiSettings, 
            ApiService apiService, 
            SyncronizeWithMcEasy sync)
        {
            _context = context;
            _selectList = selectList;
            _httpClient = httpClient;
            _apiSettings = apiSettings.Value;
            _apiService = apiService;
            _sync = sync;
        }

        private async Task<OrderMcEasy> GetOrderMcEasyByID(string id)
        {
            var (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Get,
                $"order/api/web/v1/delivery-order/{id}"
            );

            if (!ok)
                throw new Exception($"Gagal ambil data order dari API (ID: {id})");

            var order = json
                .GetProperty("data")
                .Deserialize<OrderMcEasy>() ?? new OrderMcEasy();

            return order;
        }

        private IQueryable<OrderSummaryViewModel> GetOrderSummaryQuery(DateTime? startDate = null, DateTime? endDate = null)
        {
            var username = HttpContext.Session.GetString("username") ?? "System";

            // Set default date range (last 7 days) if not provided
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddDays(-7).Date;

            if (!endDate.HasValue)
                endDate = DateTime.Now.Date;

            var sql = @"
                WITH od AS (
                    SELECT 
                        id_seq_order, 
                        COUNT(item_name) AS total_item,
                        SUM(item_qty) AS total_qty
                    FROM TRC_ORDER_DTL
                    GROUP BY id_seq_order
                ),
                ord AS (SELECT 
                    a.id_seq AS IdSeq, 
                    a.wh_code AS WhCode, 
                    a.sub_custid AS SubCustId,
                    a.cnee_code AS CneeCode,
                    a.inv_no AS InvNo,
                    CAST(a.pickup_date AS date) AS PickupDate,
                    CAST(a.delivery_date AS date) AS DeliveryDate,
                    a.origin_id AS OriginId,
                    a.dest_area AS DestArea,
                    a.order_status AS OrderStatus,
                    a.mceasy_status AS MCOrderStatus,
                    a.mceasy_order_id AS McEasyOrderId,
                    COALESCE(od.total_item, 0) AS TotalItem,
                    COALESCE(od.total_qty, 0) AS TotalQty
                FROM TRC_ORDER a 
                LEFT JOIN od ON a.id_seq = od.id_seq_order
                LEFT JOIN MC_ORDER mo ON a.mceasy_order_id = mo.id
                WHERE CAST(a.pickup_date AS date) BETWEEN {1} AND {2})
                SELECT ord.*, b.jobid AS JobID, c.MAIN_CUST
                FROM ord 
                LEFT JOIN TRC_JOB b ON ord.InvNo = b.inv_no
                INNER JOIN TRC_CUST_GROUP c ON ord.SubCustId = c.SUB_CODE
                INNER JOIN UserXCustomers d ON d.CustomerMain = c.MAIN_CUST
                WHERE d.Username = {0}
                ORDER BY ord.IdSeq DESC
            ";

            return _context.OrderSummaryView.FromSqlRaw(sql, username, startDate.Value, endDate.Value);
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            // Set default values to ViewBag for the date inputs
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

            var data = await GetOrderSummaryQuery(startDate, endDate).ToListAsync();
            return View(data);
        }


        public async Task<IActionResult> IndexSync()
        {

            try
            {
                var orders = await _sync.FetchOrderFromApi();

                if (orders != null && orders.Any())
                {
                    await _sync.SyncOrderToDatabase(orders);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sync data: {ex.Message}");
            }

            var data = await GetOrderSummaryQuery().ToListAsync();

            return View("Index", data);
        }

        [HttpPost]
        public async Task<IActionResult> SyncronizeOrder()
        {
            try
            {
                var ordersFromApi = await _sync.FetchOrderFromApi();
                var result = await _sync.SyncOrderToDatabase(ordersFromApi);
                return Json(new
                {
                    success = true,
                    message = $"Order have been updated.",
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

        public IActionResult Import()
        {
            return View();
        }

        public IActionResult Form(int? id)
        {
            var vm = new OrderViewModel();
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
            ViewBag.ListCustomer = _context.CustomerGroups
            .Where(o => customerGroupAllowed.Contains(o.SUB_CODE))
            .Select(c => new SelectListItem
            {
                Value = c.SUB_CODE,
                Text = c.SUB_CODE,
            }).ToList();
            ViewBag.ListWarehouse = _selectList.GetWarehouse();
            ViewBag.ListConsignee = _selectList.GetConsignee();
            ViewBag.ListOrigin = _selectList.GetOrigins();
            ViewBag.ListDestination = _selectList.GetDestinations();
            ViewBag.ListUoM = _selectList.GetChargeUoms();
            ViewBag.ListModa = _selectList.GetServiceModas();
            ViewBag.ListServiceType = _selectList.GetServiceTypes();
            ViewBag.ListTruckSize = _selectList.GetTruckSizes();
            ViewBag.ListPackingType = _selectList.PackingTypeOption();

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

            var header = model.Header;
            var errors = new List<string>();

            if (!_context.Warehouses.Any(w => w.wh_code == header.wh_code))
                errors.Add($"Warehouse '{header.wh_code}' not found");

            if (!_context.Origins.Any(l => l.origin_code == header.origin_id))
                errors.Add($"Origin '{header.origin_id}' not found");

            if (header.id_seq == 0)
                if (_context.Orders.Any(o => o.inv_no == header.inv_no))
                    errors.Add($"Inv No '{header.inv_no}' already exists!");


            if (header.id_seq > 0)
            {
                var existingOrder = _context.Orders.FirstOrDefault(e => e.inv_no == header.inv_no);
                if (existingOrder != null && existingOrder.mceasy_status == "Dijalankan" && existingOrder.inv_no != null) {
                    (bool ok, string message) = UpdateOrder(existingOrder.inv_no, header);

                    if (!ok)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = message,
                        });
                    }
                    else
                    {
                        return Json(new { success = true, message = "Order updated successfully", data = header });
                    }
                }  
            }

            var customerGroup = _context.CustomerGroups.FirstOrDefault(cg => cg.SUB_CODE == header.sub_custid);
            if (customerGroup == null)
            {
                errors.Add($"Customer Group '{header.sub_custid}' not found!.");
                return BadRequest(new
                {
                    success = false,
                    message = "Failed validation",
                    errors
                });
            }

            var priceSell = _context.PriceSells.FirstOrDefault(ps =>
            ps.cust_code == customerGroup.MAIN_CUST
            && ps.origin == header.origin_id
            && ps.dest == header.dest_area
            && ps.serv_type == header.serv_req
            && ps.serv_moda == header.moda_req
            && ps.truck_size == header.truck_size
            && ps.charge_uom == header.uom);

            if (priceSell == null)
            {
                errors.Add($"Price sell not found for this transaction");
            }


            var priceBuy = _context.PriceBuys.FirstOrDefault(pb =>
            pb.origin == header.origin_id
            && pb.dest == header.dest_area
            && pb.serv_type == header.serv_req
            && pb.serv_moda == header.moda_req
            && pb.truck_size == header.truck_size
            && pb.charge_uom == header.uom);

            if (errors.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed validation",
                    errors
                });
            }

            var deliveryDateTime = header.delivery_date.HasValue ? header.delivery_date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null;
            var pickupDateTime = header.pickup_date.HasValue ? header.pickup_date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null;


            var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);
            if (customer == null)
            {
                  return NotFound(new { success = false, message = $"Customer '{customerGroup.CUST_CODE}' tidak ditemukan." });
            }

            

            

            if (customerGroup.API_FLAG == 1)
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
                    if (!header.delivery_date.HasValue || header.delivery_date.Value.ToUniversalTime() <= DateTime.UtcNow)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Expected delivery must be greater than the current time"
                        });
                    }

                    if (!header.pickup_date.HasValue || header.pickup_date.Value.ToUniversalTime() <= DateTime.UtcNow)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Pick up time must be greater than the current time"
                        });
                    }

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

                    var payloadPatch = new Dictionary<string, object>
                    {
                        ["expected_pickup_on"] = pickupDateTime,
                        ["expected_delivered_on"] = deliveryDateTime,
                        ["shipment_number"] = header.inv_no
                    };

                    if (orderHeader.mceasy_origin_address_id != header.mceasy_origin_address_id ||
                        orderHeader.mceasy_destination_address_id != header.mceasy_destination_address_id)
                    {
                        payloadPatch["origin_address_id"] = header.mceasy_origin_address_id;
                        payloadPatch["destination_address_id"] = header.mceasy_destination_address_id;
                    }


                    var (okOpt, jsonOpt) = await _apiService.SendRequestAsync(
                        HttpMethod.Options,
                        $"order/api/web/v1/delivery-order/{orderHeader.mceasy_order_id}"
                    );

                    //if (!okOpt)
                    //{
                    //    return BadRequest(new
                    //    {
                    //        success = false,
                    //        message = "Gagal kirim ke API [OPTIONS] Order",
                    //        detail = jsonOpt
                    //    });
                    //}

                    (ok, json) = await _apiService.SendRequestAsync(
                        HttpMethod.Patch,
                        $"order/api/web/v1/delivery-order/{orderHeader.mceasy_order_id}",
                        payloadPatch
                    );
                }

                if (!ok)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = _apiService.ExtractErrorMessage(json),
                        detail = json
                    });
                }
            }


            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var username = HttpContext.Session.GetString("username") ?? "System";

                if (header.id_seq == 0)
                {
                    header.cnee_code = header.mceasy_dest_name;
                    header.mceasy_origin_address_id = header.mceasy_origin_address_id;
                    header.mceasy_destination_address_id = header.mceasy_destination_address_id;
                    header.mceasy_origin_name = header.mceasy_origin_name;
                    header.mceasy_dest_name = header.mceasy_dest_name;
                    header.order_status = 0;
                    header.mceasy_status = "Draf";
                    header.pallet_delivery = header.pallet_delivery;
                    header.pallet_consume = header.pallet_consume;
                    header.packing_type = header.packing_type;
                    header.entry_date = DateTime.Now;
                    header.entry_user = username;
                    _context.Orders.Add(header);
                }
                else
                {
                    var existingHeader = _context.Orders.FirstOrDefault(o => o.id_seq == header.id_seq);
                    if (existingHeader == null)
                        return NotFound(new { success = false, message = "Data tidak ditemukan untuk update." });

                    existingHeader.wh_code = header.wh_code;
                    existingHeader.sub_custid = header.sub_custid;
                    existingHeader.cnee_code = header.mceasy_dest_name;
                    existingHeader.inv_no = header.inv_no;
                    existingHeader.delivery_date = header.delivery_date;
                    existingHeader.pickup_date = header.pickup_date;
                    existingHeader.origin_id = header.origin_id;
                    existingHeader.dest_area = header.dest_area;
                    existingHeader.truck_size = header.truck_size;
                    existingHeader.moda_req = header.moda_req;
                    existingHeader.serv_req = header.serv_req;
                    existingHeader.tot_pkgs = header.tot_pkgs;
                    existingHeader.pallet_delivery = header.pallet_delivery;
                    existingHeader.pallet_consume = header.pallet_consume;
                    existingHeader.do_rcv_time = header.do_rcv_time;
                    existingHeader.remark = header.remark;
                    existingHeader.order_status = header.order_status ?? 0;
                    existingHeader.uom = header.uom;
                    existingHeader.update_date = DateTime.Now;
                    existingHeader.update_user = username;
                    existingHeader.packing_type = header.packing_type;

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


        private (bool ok, string message) UpdateOrder(string inv_no, Order header) {

            var order = _context.Orders.FirstOrDefault(j => j.inv_no == inv_no);
            if (order == null) return (false, $"Order not found for DO {inv_no}");

            var customerGroup = _context.CustomerGroups.FirstOrDefault(g => g.SUB_CODE == order.sub_custid);

            if (customerGroup == null) return (false, "Customer group not found");

            var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

            if (customer == null)return (false, "Customer not found");

            var SellRate = _context.PriceSells.FirstOrDefault(sr =>
                                sr.cust_code == customer.MAIN_CUST
                                && sr.origin == header.origin_id
                                && sr.dest == header.dest_area
                                && sr.truck_size == header.truck_size
                                && sr.serv_type == header.serv_req
                                && sr.serv_moda == header.moda_req
                                && sr.charge_uom == header.uom);

            if (SellRate == null) return (false, $"Sell rate not found for INV {order.inv_no}");

            order.wh_code = header.wh_code;
            order.sub_custid = header.sub_custid;
            order.cnee_code = header.mceasy_dest_name;
            order.inv_no = header.inv_no;
            order.delivery_date = header.delivery_date;
            order.pickup_date = header.pickup_date;
            order.origin_id = header.origin_id;
            order.dest_area = header.dest_area;
            order.truck_size = header.truck_size;
            order.moda_req = header.moda_req;
            order.serv_req = header.serv_req;
            order.tot_pkgs = header.tot_pkgs;
            order.pallet_delivery = header.pallet_delivery;
            order.pallet_consume = header.pallet_consume;
            order.do_rcv_time = header.do_rcv_time;
            order.remark = header.remark;
            order.order_status = header.order_status ?? 0;
            order.uom = header.uom;
            order.update_date = DateTime.Now;
            order.update_user = HttpContext.Session.GetString("username") ?? "System";
            order.packing_type = header.packing_type;

            var job = _context.Jobs.FirstOrDefault(d => d.inv_no == inv_no);

            if (job != null) {

                job.moda_req = order.moda_req;
                job.serv_req = order.serv_req;
                job.truck_size = order.truck_size;
                job.charge_uom_c = order.uom;
                job.inv_no = inv_no;
                job.origin_id = order.origin_id;
                job.dest_id = order.dest_area;
                job.cust_ori = order.sub_custid;
                job.sell1 = SellRate.sell1;
                job.sell2 = SellRate.sell2;
                job.sell3 = SellRate.sell3;
                job.sell_trip2 = SellRate.selltrip2;
                job.sell_trip3 = SellRate.selltrip3;
                job.sell_diffa = SellRate.sell_diff_area;
                job.sell_ep = SellRate.sell_ret_empty;
                job.sell_rc = SellRate.sell_ret_cargo;
                job.sell_ov = SellRate.sell_ovnight;
                job.sell_cc = SellRate.sell_cancel;
                job.update_user = HttpContext.Session.GetString("username") ?? "System";
                job.update_date = DateTime.Now;

            }  

            _context.SaveChanges();

            return (true, "OK");

        }

        [HttpGet]
        public IActionResult DownloadSample()
        {
            using var workbook = new XLWorkbook();

            // HEADER
            var headerSheet = workbook.Worksheets.Add("Header");
            headerSheet.Cell("A1").Value = "No.";
            headerSheet.Cell("B1").Value = "Nama Pelanggan*";
            headerSheet.Cell("C1").Value = "Karyawan Pemasaran";
            headerSheet.Cell("D1").Value = "Target Diambil*";
            headerSheet.Cell("E1").Value = "Target Dikirim*";
            headerSheet.Cell("F1").Value = "No. AJU";
            headerSheet.Cell("G1").Value = "No. DO*";
            headerSheet.Cell("H1").Value = "No. Referensi";
            headerSheet.Cell("I1").Value = "Catatan Lainya";
            headerSheet.Cell("J1").Value = "Alamat Asal*";
            headerSheet.Cell("K1").Value = "Alamat Tujuan*";
            headerSheet.Cell("L1").Value = "Truck Size*";
            headerSheet.Cell("M1").Value = "Service Type*";
            headerSheet.Cell("N1").Value = "Moda*";
            headerSheet.Cell("O1").Value = "UOM*";
            headerSheet.Cell("P1").Value = "Whs Code*";
            headerSheet.Cell("Q1").Value = "Rcv DO Date*";
            headerSheet.Cell("R1").Value = "Rcv DO Time*";
            headerSheet.Cell("S1").Value = "Origin Area*";
            headerSheet.Cell("T1").Value = "Destination Area*";
            headerSheet.Cell("U1").Value = "Packing Type*";

            // Format header (bold + background biru muda)
            var headerRange = headerSheet.Range("A1:T1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            headerSheet.Columns().AdjustToContents();

            // SAMPLE DATA
            headerSheet.Cell("A2").Value = "1";
            headerSheet.Cell("B2").Value = "RBID-API";
            headerSheet.Cell("C2").Value = "-";
            headerSheet.Cell("D2").Value = DateTime.Now.AddDays(1);
            headerSheet.Cell("E2").Value = DateTime.Now.AddDays(1);
            headerSheet.Cell("F2").Value = "-";
            headerSheet.Cell("G2").Value = "IBM00011";
            headerSheet.Cell("H2").Value = "-";
            headerSheet.Cell("I2").Value = "-";
            headerSheet.Cell("J2").Value = "RBID Nagrak";
            headerSheet.Cell("K2").Value = "RBID Makasar";
            headerSheet.Cell("L2").Value = "NON-SIZE";
            headerSheet.Cell("M2").Value = "LTL";
            headerSheet.Cell("N2").Value = "LAND";
            headerSheet.Cell("O2").Value = "CBM";
            headerSheet.Cell("P2").Value = "NWW1-NRML";
            headerSheet.Cell("Q2").Value = DateTime.Now;
            headerSheet.Cell("R2").Value = DateTime.Now.ToString("HH:mm:ss");
            headerSheet.Cell("S2").Value = "JAKARTA";
            headerSheet.Cell("T2").Value = "MAKASSAR";
            headerSheet.Cell("U2").Value = "SEMI WOODEN";

            // DETAIL
            var detailSheet = workbook.Worksheets.Add("Details");
            detailSheet.Cell("A1").Value = "No. DO*";
            detailSheet.Cell("B1").Value = "Nama Produk*";
            detailSheet.Cell("C1").Value = "Kuantitas*";
            detailSheet.Cell("D1").Value = "Satuan Kuantitas*";
            detailSheet.Cell("E1").Value = "Catatan";
            detailSheet.Cell("F1").Value = "Panjang";
            detailSheet.Cell("G1").Value = "Lebar";
            detailSheet.Cell("H1").Value = "Tinggi";
            detailSheet.Cell("I1").Value = "Berat";
            detailSheet.Cell("J1").Value = "Qty (Pcs)";

            // Format header details
            var detailHeader = detailSheet.Range("A1:I1");
            detailHeader.Style.Font.Bold = true;
            detailHeader.Style.Fill.BackgroundColor = XLColor.LightGreen;
            detailHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            detailHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            detailHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            detailSheet.Columns().AdjustToContents();

            // SAMPLE DETAIL DATA
            detailSheet.Cell("A2").Value = "IBM00011";
            detailSheet.Cell("B2").Value = "Box 1";
            detailSheet.Cell("C2").Value = 7;
            detailSheet.Cell("D2").Value = "PCS";
            detailSheet.Cell("E2").Value = "-";
            detailSheet.Cell("F2").Value = 13;
            detailSheet.Cell("G2").Value = 4;
            detailSheet.Cell("H2").Value = 17;
            detailSheet.Cell("I2").Value = 0.5;
            detailSheet.Cell("J2").Value = 0;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sample_import_order.xlsx");
        }

        [HttpPost]
        public IActionResult DownloadOrder([FromBody] BulkDownloadRequest request)
        {
            var orders = _context.Orders
                .Where(o => request.OrderIds.Contains(o.id_seq))
                .ToList();

            var orderDetails = _context.OrderDetails
                .Where(d => request.OrderIds.Contains(d.id_seq_order))
                .ToList();

            using var workbook = new XLWorkbook();

            // HEADER
            var headerSheet = workbook.Worksheets.Add("Header");
            headerSheet.Cell("A1").Value = "No.";
            headerSheet.Cell("B1").Value = "Nama Pelanggan*";
            headerSheet.Cell("C1").Value = "Karyawan Pemasaran";
            headerSheet.Cell("D1").Value = "Target Diambil*";
            headerSheet.Cell("E1").Value = "Target Dikirim*";
            headerSheet.Cell("F1").Value = "No. AJU";
            headerSheet.Cell("G1").Value = "No. DO*";
            headerSheet.Cell("H1").Value = "No. Referensi";
            headerSheet.Cell("I1").Value = "Catatan Lainya";
            headerSheet.Cell("J1").Value = "Alamat Asal*";
            headerSheet.Cell("K1").Value = "Alamat Tujuan*";
            headerSheet.Cell("L1").Value = "Truck Size*";
            headerSheet.Cell("M1").Value = "Service Type*";
            headerSheet.Cell("N1").Value = "Moda*";
            headerSheet.Cell("O1").Value = "UOM*";
            headerSheet.Cell("P1").Value = "Whs Code*";
            headerSheet.Cell("Q1").Value = "Rcv DO Date*";
            headerSheet.Cell("R1").Value = "Rcv DO Time*";
            headerSheet.Cell("S1").Value = "Origin Area*";
            headerSheet.Cell("T1").Value = "Destination Area*";
            headerSheet.Cell("U1").Value = "Packing Type*";

            // Format header (bold + background biru muda)
            var headerRange = headerSheet.Range("A1:T1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            headerSheet.Columns().AdjustToContents();

            int row = 2;
            int no = 1;

            foreach (var order in orders)
            {
                headerSheet.Cell(row, 1).Value = no++;
                headerSheet.Cell(row, 2).Value = order.sub_custid;
                headerSheet.Cell(row, 3).Value = "";
                headerSheet.Cell(row, 4).Value = order.pickup_date;
                headerSheet.Cell(row, 5).Value = order.delivery_date;
                headerSheet.Cell(row, 6).Value = "";
                headerSheet.Cell(row, 7).Value = order.inv_no;
                headerSheet.Cell(row, 8).Value = "";
                headerSheet.Cell(row, 9).Value = "";
                headerSheet.Cell(row, 10).Value = order.mceasy_origin_name;
                headerSheet.Cell(row, 11).Value = order.mceasy_dest_name;
                headerSheet.Cell(row, 12).Value = order.truck_size;
                headerSheet.Cell(row, 13).Value = order.serv_req;
                headerSheet.Cell(row, 14).Value = order.moda_req;
                headerSheet.Cell(row, 15).Value = order.uom;
                headerSheet.Cell(row, 16).Value = order.wh_code;
                headerSheet.Cell(row, 17).Value = order.do_rcv_date;
                headerSheet.Cell(row, 18).Value = order.do_rcv_time;
                headerSheet.Cell(row, 19).Value = order.origin_id;
                headerSheet.Cell(row, 20).Value = order.dest_area;
                headerSheet.Cell(row, 21).Value = order.packing_type;

                row++;
            }

            // DETAIL
            var detailSheet = workbook.Worksheets.Add("Details");
            detailSheet.Cell("A1").Value = "No. DO*";
            detailSheet.Cell("B1").Value = "Nama Produk*";
            detailSheet.Cell("C1").Value = "Kuantitas*";
            detailSheet.Cell("D1").Value = "Satuan Kuantitas*";
            detailSheet.Cell("E1").Value = "Catatan";
            detailSheet.Cell("F1").Value = "Panjang";
            detailSheet.Cell("G1").Value = "Lebar";
            detailSheet.Cell("H1").Value = "Tinggi";
            detailSheet.Cell("I1").Value = "Berat";

            // Format header details
            var detailHeader = detailSheet.Range("A1:I1");
            detailHeader.Style.Font.Bold = true;
            detailHeader.Style.Fill.BackgroundColor = XLColor.LightGreen;
            detailHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            detailHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            detailHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            detailSheet.Columns().AdjustToContents();


            int dRow = 2;

            foreach (var detail in orderDetails)
            {
                var order = orders.FirstOrDefault(o => o.id_seq == detail.id_seq_order);

                detailSheet.Cell(dRow, 1).Value = order?.inv_no;
                detailSheet.Cell(dRow, 2).Value = detail.item_name;
                detailSheet.Cell(dRow, 3).Value = detail.item_qty;
                detailSheet.Cell(dRow, 4).Value = detail.pkg_unit;
                detailSheet.Cell(dRow, 5).Value = "";
                detailSheet.Cell(dRow, 6).Value = detail.item_length;
                detailSheet.Cell(dRow, 7).Value = detail.item_width;
                detailSheet.Cell(dRow, 8).Value = detail.item_height;
                detailSheet.Cell(dRow, 9).Value = detail.item_wgt;

                dRow++;
            }


            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "data_order.xlsx");
        }

        [HttpPost]
        public IActionResult Preview(IFormFile file, string operationType)
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


                string uniqueKey = string.Join("|", Enumerable.Range(1, 7).Select(c => headerSheet.Cell(row, c).GetString()));
                if (!uniqueKeys.Add(uniqueKey))
                {
                    errors.Add(new { row, section = "header", field = "composite key", message = "Duplikat kombinasi 7 kolom (wh_code - dest_area) ditemukan." });
                }

                string invoiceNo = headerSheet.Cell(row, 7).GetString();
                var header = new Order();

                try
                {
                    string whCode = headerSheet.Cell(row, 16).GetString();
                    string customer = headerSheet.Cell(row, 2).GetString();
                    string originArea = headerSheet.Cell(row, 19).GetString();
                    string destArea = headerSheet.Cell(row, 20).GetString();
                    string originName = headerSheet.Cell(row, 10).GetString();
                    string destName = headerSheet.Cell(row, 11).GetString();
                    DateTime? expectedDelivery = DateTime.TryParse(headerSheet.Cell(row, 5).GetString(), out var dateVal) ? dateVal : (DateTime?)null;
                    DateTime? pickupDate = DateTime.TryParse(headerSheet.Cell(row, 4).GetString(), out var dateVa) ? dateVa : (DateTime?)null;
                    DateTime? doRcvDate = DateTime.TryParse(headerSheet.Cell(row, 17).GetString(), out var drd) ? drd : (DateTime?)null;
                    string? doRcvTime = headerSheet.Cell(row, 18).GetString();
                    string? uomHeader = headerSheet.Cell(row, 15).GetString();
                    string? modaHeader = headerSheet.Cell(row, 14).GetString();
                    string? serviceHeader = headerSheet.Cell(row, 13).GetString();
                    string? truckSizeHeader = headerSheet.Cell(row, 12).GetString();
                    string? remarkHeader = headerSheet.Cell(row, 9).GetString();
                    string? packingType = headerSheet.Cell(row, 21).GetString();


                    if (pickupDate == null || pickupDate <= DateTime.Now)
                    {
                        errors.Add(new
                        {
                            row,
                            section = "header",
                            field = "Target Diambil",
                            message = "Target Diambil harus lebih besar dari waktu sekarang"
                        });
                    }

                    if (expectedDelivery == null || expectedDelivery <= DateTime.Now)
                    {
                        errors.Add(new
                        {
                            row,
                            section = "header",
                            field = "Target Dikirim",
                            message = "Target Dikirim harus lebih besar dari waktu sekarang"
                        });
                    }


                    if (!_context.Warehouses.Any(w => w.wh_code == whCode))
                        errors.Add(new { row, section = "header", field = "Whs Code", message = $"Warehouse '{whCode}' tidak ditemukan" });

                    var originMaster = _context.Origins.FirstOrDefault(or => or.origin_code == originArea);

                    if (originMaster == null) {
                        errors.Add(new { row, section = "header", field = "Origin Area", message = $"Origin '{originArea}' tidak ditemukan" });
                    }
                        

                    var geofenceOrigin = _context.Geofences.FirstOrDefault(o => o.FenceName == originName);

                    if (geofenceOrigin == null) {
                        errors.Add(new { row, section = "header", field = "Alamat asal", message = $"'{originName}' tidak ditemukan" });
                    }

                    var geofenceDestination = _context.Geofences.FirstOrDefault(d => d.FenceName == destName && d.CustomerName == customer);

                    if (geofenceDestination == null)
                    {
                        errors.Add(new { row, section = "header", field = "Alamat tujuan", message = $"'{destName}' tidak ditemukan" });
                    }

                    var customerGroup = _context.CustomerGroups.FirstOrDefault(cg => cg.SUB_CODE == customer);

                    if (customerGroup == null) {
                        errors.Add(new { row, section = "header", field = "Nama Pelanggan", message = $"'{customer}' tidak ditemukan" });
                    }

                    if (customerGroup != null && originMaster != null && destArea != null && originArea != null)
                    {
                        var priceSell = _context.PriceSells.FirstOrDefault(ps =>
                        ps.cust_code == customerGroup.MAIN_CUST &&
                        ps.origin == originArea &&
                        ps.dest == destArea &&
                        ps.serv_type == serviceHeader &&
                        ps.serv_moda == modaHeader &&
                        ps.truck_size == truckSizeHeader &&
                        ps.charge_uom == uomHeader
                    );

                        if (priceSell == null)
                        {
                            // Buat pesan lebih detail tanpa ubah struktur
                            var detailMsg = $"Price sell tidak ditemukan untuk kombinasi berikut: " +
                                            $"Customer={customerGroup.MAIN_CUST}, Origin={originArea}, " +
                                            $"Destination={destArea}, Service={serviceHeader}, " +
                                            $"Moda={modaHeader}, Truck={truckSizeHeader}, UOM={uomHeader}";

                            errors.Add(new
                            {
                                row,
                                section = "Header",
                                field = $"No DO {invoiceNo}",
                                message = detailMsg
                            });
                        }


                        var orderExist = _context.Orders.FirstOrDefault(or => or.inv_no == invoiceNo);
                        if (orderExist != null && operationType == "add")
                        {
                            errors.Add(new { row, section = "Header", field = $"No DO {invoiceNo}", message = $" Already exists" });
                        }
                        else if (orderExist == null && operationType == "edit") {
                            errors.Add(new { row, section = "Header", field = $"No DO {invoiceNo}", message = $"Not found for edit" });
                        }
                    }
                    else
                    {

                        errors.Add(new { row, section = "Header", field = "Your data invalid!" });

                    }

                    header = new Order
                    {
                        inv_no = invoiceNo,
                        wh_code = whCode,
                        sub_custid = customer,
                        cnee_code = destName,
                        delivery_date = expectedDelivery,
                        pickup_date = pickupDate,
                        origin_id = originArea,
                        dest_area = destArea,
                        uom = uomHeader,
                        do_rcv_date = doRcvDate,
                        do_rcv_time = doRcvTime,
                        moda_req = modaHeader,
                        serv_req = serviceHeader,
                        truck_size = truckSizeHeader,
                        remark = remarkHeader,
                        mceasy_origin_address_id = geofenceOrigin?.GeofenceId,
                        mceasy_destination_address_id = geofenceDestination?.GeofenceId,
                        mceasy_origin_name = originName,
                        mceasy_dest_name = destName,
                        packing_type = packingType,
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
                    if (invNoDetail == invoiceNo)
                    {
                        try
                        {
                            string itemName = detailSheet.Cell(drow, 2).GetString();
                            var product = _context.Products.FirstOrDefault(i => i.Name == itemName);

                            if (product == null)
                            {
                                errors.Add(new { row = drow, section = "detail", field = "Nama Product*", message = $"Item '{itemName}' tidak ditemukan di master" });
                            }

                            details.Add(new OrderDetail
                            {
                                inv_no = invNoDetail,
                                item_name = itemName,
                                item_category = product?.ProductCategoryName,
                                mceasy_product_id = product?.ProductID,
                                item_length = long.TryParse(detailSheet.Cell(drow, 6).GetValue<string>(), out var l) ? (int?)l : null,
                                item_width = long.TryParse(detailSheet.Cell(drow, 7).GetValue<string>(), out var w) ? (int?)w : null,
                                item_height = long.TryParse(detailSheet.Cell(drow, 8).GetValue<string>(), out var h) ? (int?)h : null,
                                item_wgt = decimal.TryParse(detailSheet.Cell(drow, 9).GetValue<string>(),
                                                            System.Globalization.NumberStyles.Any,
                                                            System.Globalization.CultureInfo.InvariantCulture,
                                                            out var wg)
                                            ? (decimal?)wg
                                            : null,
                                item_qty = long.TryParse(detailSheet.Cell(drow, 3).GetValue<string>(), out var iq) ? (int?)iq : null,
                                pkg_unit = detailSheet.Cell(drow, 4).GetString(),
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
                    OperationType = operationType,
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
        public async Task<IActionResult> SaveExcel([FromBody] List<OrderViewModel> data)
        {
            if (data == null || data.Count == 0)
                return NotFound(new { success = false, message = "Payload data not found." });

            foreach (var item in data)
            {
                var header = item.Header;
                var details = item.Details;
                var operationType = item.OperationType;

                if (header == null || details == null)
                    continue;


                var customerGroup = _context.CustomerGroups.FirstOrDefault(cg => cg.SUB_CODE == header.sub_custid);
                if (customerGroup == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Customer group not found"
                    });
                }
                var deliveryDateTime = header.delivery_date.HasValue ? header.delivery_date.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null;
                var pickupDateTime = header.delivery_date.HasValue ? header?.pickup_date?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null;
                var customer = _context.Customers.FirstOrDefault(c => c.CUST_CODE == customerGroup.CUST_CODE);

                if (customer == null)
                {
                    return NotFound(new { success = false, message = $"Customer '{customerGroup.CUST_CODE}' not found." });
                }

                if (header == null)
                {
                    return NotFound(new { success = false, message = $"Header not found." });
                }

                var origin = _context.Geofences.FirstOrDefault(o => o.FenceName == header.mceasy_origin_name);

                if (customerGroup.API_FLAG == 1) {
                    origin = _context.Geofences.FirstOrDefault(o => o.FenceName == header.mceasy_origin_name && o.GeofenceId != null);
                }

                if (origin == null)
                {
                    return NotFound(new { success = false, message = $"Origin '{header.mceasy_origin_name}' not found." });
                }

                var dest = _context.Geofences.FirstOrDefault(d => d.FenceName == header.mceasy_dest_name);
                if (dest == null)
                {
                    return NotFound(new { success = false, message = $"Destination '{header.mceasy_dest_name}' not found." });
                }


                // Check Order jika ingin edit
                var orderExisting = _context.Orders.FirstOrDefault(o => o.inv_no == header.inv_no);

                if (operationType == "edit") {

                    if (orderExisting != null) {
                        // disini di hapus semua aja dulu tapi yang statusnya draf aja
                        if (orderExisting.mceasy_status != "Draf") {
                            return BadRequest(new { success = false, message = $"Order with status Draf not found." });
                        }


                        var (okDelete, jsonDelete) = await _apiService.SendRequestAsync(
                            HttpMethod.Delete,
                            $"/order/api/web/v1/delivery-order/{orderExisting.mceasy_order_id}"
                        );
                        //if (!okDelete)
                        //{
                        //    return BadRequest(new
                        //    {
                        //        success = false,
                        //        message = "Failed deleting order",
                        //        detail = jsonDelete
                        //    });
                        //}
                        var rows = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_ORDER WHERE id_seq = {0}", orderExisting.id_seq);
                        var rows2 = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_ORDER_DTL WHERE id_seq_order = {0}", orderExisting.id_seq);
                        var rows3 = _context.Database.ExecuteSqlRaw("DELETE FROM MC_ORDER WHERE id = {0}", orderExisting.mceasy_order_id);

                        if (rows < 1)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Failed deleting order from database",
                            });
                        }


                    }
                }


                if (customerGroup.API_FLAG == 1)
                {
                   
                    var payload = new
                    {
                        customer_id = customerGroup.MCEASY_CUST_ID,
                        billed_customer_id = customerGroup.MCEASY_CUST_ID,
                        origin_address_id = origin.GeofenceId,
                        destination_address_id = dest.GeofenceId,
                        expected_pickup_on = pickupDateTime,
                        expected_delivered_on = deliveryDateTime,
                        shipment_number = header.inv_no,
                    };

                    var nullFields = payload
                        .GetType()
                        .GetProperties()
                        .Where(p => p.GetValue(payload) == null)
                        .Select(p => p.Name)
                        .ToList();

                    if (nullFields.Any())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Saving order failed",
                            detail = new
                            {
                                null_fields = nullFields
                            }
                        });
                    }

                    bool ok;
                    JsonElement json = default;

                    (ok, json) = await _apiService.SendRequestAsync(
                        HttpMethod.Post,
                        "order/api/web/v1/delivery-order",
                        payload
                    );

                    if (!ok)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Error fetching API",
                            detail = json
                        });
                    }
                  
                    var orderId = json.GetProperty("data").GetProperty("id").GetString();

                    if (orderId == null) {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Order id is null",
                        });
                    }

                    try
                    {
                        var order = await GetOrderMcEasyByID(orderId);

                        if (order?.status?.name == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Status order from API not found.",
                            });
                        }

                        // Simpan Header
                        header.mceasy_status = order.status.name;
                        header.mceasy_order_id = json.GetProperty("data").GetProperty("id").GetString();
                        header.mceasy_do_number = json.GetProperty("data").GetProperty("number").GetString();
                        header.order_status = 0;
                        header.tot_pkgs = 0;
                        header.pallet_consume = 0;
                        header.pallet_delivery = 0;
                        header.entry_user = HttpContext.Session.GetString("username") ?? "System";
                        header.entry_date = DateTime.Now;
                        _context.Orders.Add(header);
                        _context.SaveChanges();

                        // Set id_seq_order pada detail dan simpan
                        foreach (var detail in details)
                        {

                            bool okDetail;
                            JsonElement jsonDetail = default;

                            if (detail == null)
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = "Failed validasi detail",
                                });
                            }

                            string itemName = detail.item_name ?? "";
                            var product = _context.Products.FirstOrDefault(i => i.Name == itemName);

                            if (product == null)
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = "Item name not valid",
                                });
                            }

                            var payloadDetail = new
                            {
                                product_id = product.ProductID,
                                quantity = detail.item_qty,
                                uom = product.Uom,
                                note = "",
                            };

                            (okDetail, jsonDetail) = await _apiService.SendRequestAsync(
                                HttpMethod.Post,
                                "order/api/web/v1/delivery-order/" + header.mceasy_order_id + "/load",
                                payloadDetail
                            );
                            if (!okDetail)
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = "Failed Add Order to API : " + json,
                                    detail = json
                                });
                            }

                            


                            detail.mceasy_order_id = header.mceasy_order_id;
                            detail.mceasy_order_dtl_id = jsonDetail.GetProperty("data").GetProperty("id").GetString();
                            detail.mceasy_product_id = product.ProductID;
                            detail.id_seq_order = header.id_seq;
                            detail.wh_code = header.wh_code;
                            detail.sub_custid = header.sub_custid;
                            detail.cnee_code = header.cnee_code;
                            detail.delivery_date = header.delivery_date;
                            detail.item_qty = detail.item_qty;
                            detail.unit_qty ??= 0;
                            detail.entry_user = header.entry_user;
                            detail.entry_date = DateTime.Now;
                        }
                        _context.OrderDetails.AddRange(details);
                        _context.SaveChanges();

                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = ex.Message,
                        });
                    }
                } 
                else 
                {

                    if (header == null)
                    {
                        return NotFound(new { success = false, message = $"Header not found." });
                    }

                    // Simpan Header
                    header.order_status = 0;
                    header.mceasy_status = "Draf";
                    header.entry_user = HttpContext.Session.GetString("username") ?? "System";
                    header.entry_date = DateTime.Now;
                    _context.Orders.Add(header);
                    _context.SaveChanges();


                    foreach (var detail in details)
                    {
                        detail.id_seq_order = header.id_seq;
                        detail.wh_code = header.wh_code;
                        detail.sub_custid = header.sub_custid;
                        detail.cnee_code = header.cnee_code;
                        detail.delivery_date = header.delivery_date;
                        detail.item_qty = detail.item_qty;
                        detail.entry_user = header.entry_user;
                        detail.entry_date = DateTime.Now;
                    }
                    _context.OrderDetails.AddRange(details);
                    _context.SaveChanges();


                }
            }

            return Ok(new { success = true, message = "Data submited successfully." });
        }

        public async Task<IActionResult> FormLoad(int? id)
        {
            var (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Get,
                $"/order/api/web/v1/product?limit={1000}&page={1}"
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
                unit_qty = (int?)model.unit_qty,
                pack_unit = model.uom,
                entry_date = DateTime.Now,
                entry_user = HttpContext.Session.GetString("username") ?? "System"
            };

            if (customerGroup.API_FLAG == 1)
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
            orderLoad.unit_qty = (int?)model.unit_qty;
            orderLoad.pkg_unit = model.uom;
            orderLoad.pack_unit = model.uom;
            orderLoad.update_date = DateTime.Now;
            orderLoad.update_user = HttpContext.Session.GetString("username") ?? "System";
            
            if (customerGroup.API_FLAG == 1 && orderLoad.mceasy_order_dtl_id != null)
            {
                var payload = new
                {
                    //product_id = orderLoad.mceasy_product_id,
                    product_id = model.product_id,
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

        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int? id) {

            var order = await _context.Orders.FirstOrDefaultAsync(od => od.id_seq == id);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Order load not found" });
            }

            if (order.mceasy_status != "Draf") {
                return NotFound(new { success = false, message = "Order cannot be delete" });
            }

            var (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Delete,
                $"/order/api/web/v1/delivery-order/{order.mceasy_order_id}"
            );
            if (!ok)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed deleting order",
                    detail = json
                });
            }

            Console.WriteLine("DELETE ORDER : {0}", id);
            var rows = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_ORDER WHERE id_seq = {0}", id);
            var rows2 = _context.Database.ExecuteSqlRaw("DELETE FROM TRC_ORDER_DTL WHERE id_seq_order = {0}", id);
            Console.WriteLine("Rows delete affected: " + rows);

            if (rows < 1) {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed deleting order from database",
                    detail = json
                });
            }

            return Ok(new { success = true, message = "Order deleted successfully" });
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
                   $"/order/api/web/v1/delivery-order/{orderLoad.mceasy_order_id}/load/{orderLoad.mceasy_order_dtl_id}",
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

        [HttpPost]
        public async Task<IActionResult> Confirm(Guid? id)
        {
            if (id == null)
                return BadRequest("ID not valid");
            var order = _context.Orders.FirstOrDefault(o => o.mceasy_order_id == id.ToString());

            if (order == null)
                return NotFound("Order not found");
            if (order.mceasy_status != "Draf")
                return BadRequest("Status order not in Draf");

            var payload = new
            {
                status = "CONFIRMED"
            };

            var (ok, json) = await _apiService.SendRequestAsync(
                HttpMethod.Post,
                $"/order/api/web/v1/delivery-order/{id}/transition",
                payload
            );
            if (!ok)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed confirm order",
                    detail = json
                });
            }

            order.mceasy_status = "Dikonfirmasi";
            order.order_status = 1;
            order.update_date = DateTime.Now;
            order.update_user = HttpContext.Session.GetString("username") ?? "System";
            _context.Orders.Update(order);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Order confirm successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> BulkConfirm([FromBody] BulkConfirmRequest request)
        {
            try
            {
                if (request.OrderIds == null)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Failed bulk confirm, order not valid"
                    });
                }
                else {
                    foreach (var orderId in request.OrderIds)
                    {
                        // Logic confirm per order
                        // Sesuaikan dengan logic Confirm yang sudah ada

                        if (orderId == null)
                            return BadRequest("ID not valid");
                        var order = _context.Orders.FirstOrDefault(o => o.id_seq == orderId);

                        if (order == null || order.mceasy_order_id == null)
                            return NotFound("Order not found");
                        if (order.mceasy_status != "Draf")
                            return BadRequest("Status order not in Draf");

                        var customerGroup = _context.CustomerGroups.FirstOrDefault(v => v.SUB_CODE == order.sub_custid);

                        if (customerGroup != null && customerGroup.API_FLAG == 1) {

                            var payload = new
                            {
                                status = "CONFIRMED"
                            };

                            var (ok, json) = await _apiService.SendRequestAsync(
                                HttpMethod.Post,
                                $"/order/api/web/v1/delivery-order/{order.mceasy_order_id}/transition",
                                payload
                            );
                            if (!ok)
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = "Failed confirm order",
                                    detail = json
                                });
                            }

                        }



                        order.mceasy_status = "Dikonfirmasi";
                        order.order_status = 1;
                        order.update_date = DateTime.Now;
                        order.update_user = HttpContext.Session.GetString("username") ?? "System";
                        _context.Orders.Update(order);
                        _context.SaveChanges();

                    }
                }
                return Json(new
                {
                    success = true,
                    message = $"{request.OrderIds.Count} order(s) confirmed successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }

    public class ConfirmOrderID
    {
        public string? OrderID { get; set; }
    }

    public class BulkDownloadRequest
    {
        public List<int> OrderIds { get; set; }
    }


    public class BulkConfirmRequest
    {
        public List<int>? OrderIds { get; set; }
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

        public decimal? unit_qty { get; set; }
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
        public DateTime PickupDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string? OriginId { get; set; }
        public string? DestArea { get; set; }
        public byte? OrderStatus { get; set; }

        public string? MCOrderStatus { get; set; }

        public string? McEasyOrderId { get; set; }
        public int? TotalItem { get; set; }
        public int? TotalQty { get; set; }

        public string? JobID { get; set; }
    }

}



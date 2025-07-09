namespace TMSBilling.Models.UploadModels
{
    public class UploadPriceBuyRequest
    {
        public string mode { get; set; } = "add"; // "add" atau "edit"
        public List<PriceBuyDto> data { get; set; } = new();
    }

    public class PriceBuyDto
    {
        public string? sup_code { get; set; }
        public string? origin { get; set; }
        public string? dest { get; set; }
        public string? serv_type { get; set; }
        public string? serv_moda { get; set; }
        public string? truck_size { get; set; }
        public string? charge_uom { get; set; }
        public decimal? flag_min { get; set; }
        public decimal? charge_min { get; set; }
        public decimal? flag_range { get; set; }
        public decimal? min_range { get; set; }
        public decimal? max_range { get; set; }
        public decimal? buy1 { get; set; }
        public decimal? buy2 { get; set; }
        public decimal? buy3 { get; set; }
        public decimal? buy_ret_empt { get; set; }
        public decimal? buy_ret_cargo { get; set; }
        public decimal? buy_ovnight { get; set; }
        public decimal? buy_cancel { get; set; }
        public decimal? buytrip2 { get; set; }
        public decimal? buytrip3 { get; set; }
        public decimal? buy_diff_area { get; set; }
        public DateTime? valid_date { get; set; }
        public byte? active_flag { get; set; }
        public string? curr { get; set; }
        public decimal? rate_value { get; set; }
    }

}

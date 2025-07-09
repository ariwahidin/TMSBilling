namespace TMSBilling.Models.UploadModels
{
    public class UploadPriceSellRequest
    {
        public string mode { get; set; } = "add"; // "add" atau "edit"
        public List<PriceSellDto> data { get; set; } = new();
    }

    public class PriceSellDto
    {
        public string? cust_code { get; set; }
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

        public decimal? sell1 { get; set; }
        public decimal? sell2 { get; set; }
        public decimal? sell3 { get; set; }

        public decimal? sell_ret_empty { get; set; }
        public decimal? sell_ret_cargo { get; set; }
        public decimal? sell_ovnight { get; set; }
        public decimal? sell_cancel { get; set; }

        public decimal? selltrip2 { get; set; }
        public decimal? selltrip3 { get; set; }
        public decimal? sell_diff_area { get; set; }

        public DateTime? valid_date { get; set; }
        public byte? active_flag { get; set; }

        public string? curr { get; set; }
        public decimal? rate_value { get; set; }
    }
}

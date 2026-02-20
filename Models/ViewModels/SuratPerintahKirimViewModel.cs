namespace TMSBilling.Models.ViewModels
{
    public class SuratPerintahKirimViewModel
    {
        // Header
        public string NomorOrder { get; set; } = string.Empty;
        public string Transporter { get; set; } = string.Empty;
        public string JenisTruck { get; set; } = string.Empty;
        public string NomorPolisi { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public DateTime? TanggalOrder { get; set; }
        public DateTime? TanggalMuat { get; set; }
        public string? JamMulai { get; set; }
        public string? JamSelesai { get; set; }
        public string? Remarks { get; set; }

        // Detail pengiriman
        public List<SuratPerintahKirimDetailViewModel> Details { get; set; } = new();
    }

    public class SuratPerintahKirimDetailViewModel
    {
        public int No { get; set; }
        public string ShipToParty { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime? DateUnloading { get; set; }
        public string DeliveryNo { get; set; } = string.Empty;
        public int TotalBox { get; set; }
        public int TotalQty { get; set; }
        public decimal Volume { get; set; }
    }
}
namespace TMSBilling.Models.ViewModels
{
    public class JobViewModel
    {
        public Job Header { get; set; }
        //public List<OrderDetail> Details { get; set; }

        public JobViewModel()
        {
            Header = new Job();
            //Details = new List<OrderDetail>();
        }

        public class JobListViewModel
        {
            public string? JobId { get; set; }
            public string? Origin { get; set; }
            public string? Destination { get; set; }
            public DateTime? DeliveryDate { get; set; }
            public string? Vendor { get; set; }

            public string? TruckID { get; set; }
        }
    }
}
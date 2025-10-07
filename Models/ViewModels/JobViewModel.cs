namespace TMSBilling.Models.ViewModels
{
    public class JobViewModel
    {
        public JobHeader Header { get; set; }
        public HeaderFormJob FormJobHeader { get; set; }
        public List<Order> FormJobDetails { get; set; }

        public JobViewModel()
        {
            Header = new JobHeader();
            FormJobHeader = new HeaderFormJob();
            FormJobDetails = new List<Order>();
        }


        public class HeaderFormJob
        {

            public string job_id { get; set; } = string.Empty;

            public string cust_group { get; set; } = string.Empty;
            public string vendor_id { get; set; } = string.Empty;
            public string vendor_act { get; set; } = string.Empty;
            public DateTime? dvdate { get; set; }
            public string truck_id { get; set; } = string.Empty;

            public string driver_name { get; set; } = string.Empty;

            public string origin_id { get; set; } = string.Empty;

            public string dest_area { get; set; } = string.Empty;

            public string truck_size { get; set; } = string.Empty;

            public string serv_moda { get; set; } = string.Empty;

            public string? serv_type { get; set; } = string.Empty;

            public string charge_uom { get; set; } = string.Empty;

            public bool? multidrop { get; set; } = false;

            public int? starting_point { get; set; }

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
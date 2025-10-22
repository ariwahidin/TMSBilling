using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models.ViewModels
{
    public class JobViewModel
    {
        public JobHeader Header { get; set; }
        public HeaderFormJob FormJobHeader { get; set; }
        public List<OrderForJobForm> FormJobDetails { get; set; }

        public JobViewModel()
        {
            Header = new JobHeader();
            FormJobHeader = new HeaderFormJob();
            FormJobDetails = new List<OrderForJobForm>();
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

        public class OrderForJobForm
        {
            public int id_seq { get; set; }
            public int drop_seq { get; set; }

            [StringLength(50)]
            public string? wh_code { get; set; }

            [StringLength(50)]
            public string? sub_custid { get; set; }

            [StringLength(50)]
            public string? cnee_code { get; set; }

            [StringLength(50)]
            public string? inv_no { get; set; }

            public DateTime? delivery_date { get; set; }

            public DateTime? pickup_date { get; set; }


            [StringLength(50)]
            public string? origin_id { get; set; }

            [StringLength(50)]
            public string? dest_area { get; set; }

            [Column(TypeName = "decimal(9,2)")]
            public decimal? tot_pkgs { get; set; }

            [StringLength(10)]
            public string? uom { get; set; }

            public int? pallet_consume { get; set; }

            public int? pallet_delivery { get; set; }

            [StringLength(50)]
            public string? si_no { get; set; }

            public DateTime? do_rcv_date { get; set; }

            [StringLength(10)]
            public string? do_rcv_time { get; set; }

            [StringLength(10)]
            public string? moda_req { get; set; }

            [StringLength(10)]
            public string? serv_req { get; set; }

            [StringLength(50)]
            public string? truck_size { get; set; }

            [StringLength(50)]
            public string? remark { get; set; }

            public byte? order_status { get; set; }

            [StringLength(50)]
            public string? entry_user { get; set; }

            public DateTime? entry_date { get; set; }

            [StringLength(50)]
            public string? update_user { get; set; }

            public DateTime? update_date { get; set; }

            [StringLength(50)]
            public string? jobid { get; set; }

            public int? total_pkgs { get; set; }

            [StringLength(50)]
            public string? mceasy_order_id { get; set; }

            [StringLength(50)]
            public string? mceasy_do_number { get; set; }

            public int? mceasy_origin_address_id { get; set; }
            public int? mceasy_destination_address_id { get; set; }

            [StringLength(50)]
            public string? mceasy_origin_name { get; set; }
            [StringLength(50)]
            public string? mceasy_dest_name { get; set; }

            [StringLength(20)]
            public string? mceasy_status { get; set; }

            public bool? mceasy_is_upload { get; set; } = false;

        }
    }
}
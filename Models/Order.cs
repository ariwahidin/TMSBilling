using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_ORDER")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

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

    [Table("TRC_ORDER_DTL")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        public int id_seq_order { get; set; }

        [StringLength(50)]
        public string? wh_code { get; set; }

        [StringLength(50)]
        public string? sub_custid { get; set; }

        [StringLength(50)]
        public string? cnee_code { get; set; }

        [StringLength(50)]
        public string? inv_no { get; set; }

        public DateTime? delivery_date { get; set; }

        public DateTime? gi_date { get; set; }

        public DateTime? pu_date { get; set; }

        [StringLength(200)]
        public string? item_category { get; set; }

        [StringLength(10)]
        public string? pkg_unit { get; set; }

        [StringLength(50)]
        public string? item_name { get; set; }

        public int? item_length { get; set; }

        public int? item_width { get; set; }

        public int? item_height { get; set; }

        public int? item_qty { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal? item_wgt { get; set; }

        [StringLength(20)]
        public string? pack_unit { get; set; }

        public int? pallet_qty { get; set; }

        public int? koli_qty { get; set; }

        [StringLength(200)]
        public string? full_addres { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }

        public string? mceasy_order_dtl_id { get; set; }

        public string? mceasy_order_id { get; set; }

        public string? mceasy_product_id { get; set; }

    }
}

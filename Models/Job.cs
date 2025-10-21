using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_JOB")]
    public class Job
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [StringLength(30)]
        public string? jobid { get; set; }

        [StringLength(30)]
        public string? vendorid { get; set; }

        [StringLength(11)]
        public string? truckid { get; set; }

        [StringLength(30)]
        public string? drivername { get; set; }

        [StringLength(30)]
        public string? driverhp { get; set; }

        [StringLength(10)]
        public string? moda_req { get; set; }

        [StringLength(10)]
        public string? serv_req { get; set; }

        [StringLength(50)]
        public string? truck_size { get; set; }

        [StringLength(50)]
        public string? serv_moda { get; set; }

        [StringLength(50)]
        public string? charge_uom { get; set; }

        public byte? multidrop { get; set; }

        public byte? multitrip { get; set; }

        public int? drop_seq { get; set; }

        //public int? total_pkgs { get; set; }

        public int? ritase_seq { get; set; }

        [StringLength(50)]
        public string? inv_no { get; set; }

        [StringLength(50)]
        public string? origin_id { get; set; }

        [StringLength(50)]
        public string? dest_id { get; set; }

        public DateTime? dvdate { get; set; }

        [StringLength(50)]
        public string? cust_ori { get; set; }

        [StringLength(10)]
        public string? servtype_ori { get; set; }

        [StringLength(50)]
        public string? trucksize_ori { get; set; }

        [StringLength(50)]
        public string? origin_ori { get; set; }

        [StringLength(50)]
        public string? dest_ori { get; set; }

        [StringLength(50)]
        public string? container_no { get; set; }

        public byte? vendor_job { get; set; }

        [StringLength(50)]
        public string? vendorid_act { get; set; }

        public int? job_status { get; set; }

        public byte? flag_pu { get; set; }
        public byte? flag_diffa { get; set; }
        public byte? flag_ep { get; set; }
        public byte? flag_rc { get; set; }
        public byte? flag_ov { get; set; }
        public byte? flag_cc { get; set; }
        public byte? flag_charge { get; set; }

        [StringLength(10)]
        public string? charge_uom_v { get; set; }

        [StringLength(10)]
        public string? charge_uom_c { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy1 { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy2 { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy3 { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_trip2 { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_trip3 { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_diffa { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_ep { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_rc { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_ov { get; set; }

        [Column(TypeName = "money")]
        public decimal? buy_cc { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell1 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell2 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell3 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_trip2 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_trip3 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_diffa { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_ep { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_rc { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_ov { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell_cc { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        //public string? mceasy_job_id { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }
    }

    [Table("TRC_JOB_H")]
    public class JobHeader
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [StringLength(30)]
        public string? jobid { get; set; }

        public string? cust_group { get; set; }

        public DateTime? deliv_date { get; set; }

        [StringLength(30)]
        public string? status_job { get; set; }

        [StringLength(50)]
        public string? origin { get; set; }
        [StringLength(50)]
        public string? dest { get; set; }

        [StringLength(30)]
        public string? vendor_plan { get; set; }

        public bool? is_vendor { get; set; } = false;

        [StringLength(30)]
        public string? vendor_act { get; set; }

        [StringLength(11)]
        public string? truck_no { get; set; }

        [StringLength(30)]
        public string? driver_name { get; set; }

        [StringLength(30)]
        public string? driver_phone { get; set; }

        [StringLength(10)]
        public string? serv_moda { get; set; }

        [StringLength(10)]
        public string? serv_type { get; set; }

        [StringLength(50)]
        public string? truck_size { get; set; }

        [StringLength(50)]
        public string? charge_uom { get; set; }

        public bool? multidrop { get; set; } = false;

        public int? starting_point { get; set; }

        public string? mceasy_job_id { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }
    }


    [Table("MC_FO")]
    public class MCFleetOrder
    {
        [Key]
        public string? id { get; set; }
        public string? number { get; set; }
        public string? shipment_reference { get; set; }
        public string? status { get; set; }
        public string? status_raw_type { get; set; }

        public DateTime? entry_date { get; set; }

        public DateTime? update_date { get; set; }
    }

    public class FleetOrderMetaDataMcEasy
    {
        public int? count { get; set; }
        public int? page { get; set; }
        public int? total_count { get; set; }
        public int? total_page { get; set; }
    }

    public class FleetOrderMcEasy
    {
        public string? id { get; set; }
        public string? number { get; set; }
        public string? shipment_reference { get; set; }
        public FleetOrderStatusMcEasy? status { get; set; }
        public FleetOrderStatusMcEasy? status_raw_type { get; set; }
    }

    public class FleetOrderStatusMcEasy
    {
        public string? name { get; set; }
        public string? raw_type { get; set; }
    }
}

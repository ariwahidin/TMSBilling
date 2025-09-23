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

        public string? mceasy_job_id { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }
    }
}

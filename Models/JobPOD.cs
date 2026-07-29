using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace TMSBilling.Models
{
    [Table("TRC_JOB_POD")]
    public class JobPOD
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [StringLength(30)]
        public string? jobid { get; set; }

        [StringLength(50)]
        public string? inv_no { get; set; }

        public string? cnee_code { get; set; }

        public DateTime? outorigin_date { get; set; }

        [StringLength(10)]
        public string? outorigin_time { get; set; }

        public DateTime? unloading_date { get; set; }
        public DateTime? arriv_date { get; set; }

        [StringLength(10)]
        public string? arriv_time { get; set; }

        [StringLength(50)]
        public string? arriv_pic { get; set; }

        public DateTime? pod_ret_date { get; set; }

        public string? picked_by { get; set; }

        public DateTime? picked_on { get; set; }

        [StringLength(10)]
        public string? picked_time { get; set; }

        public string? dropped_by { get; set; }

        public DateTime? dropped_on { get; set; }

        [StringLength(10)]
        public string? pod_ret_time { get; set; }

        [StringLength(50)]
        public string? pod_ret_pic { get; set; }

        public DateTime? pod_send_date { get; set; }

        [StringLength(10)]
        public string? pod_send_time { get; set; }

        [StringLength(50)]
        public string? pod_send_pic { get; set; }

        [StringLength(50)]
        public string? no_cn { get; set; }

        public byte? pod_status { get; set; }

        [StringLength(10)]
        public string? spd_no { get; set; }

        [StringLength(255)]
        public string? pod_remark { get; set; }

        public int? failure_id { get; set; }
        public int? failure_type_id { get; set; }

        public DateTime? epod_date { get; set; }

        [StringLength(10)]
        public string? input_method { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }
    }
}

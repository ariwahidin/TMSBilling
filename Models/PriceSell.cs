using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMSBilling.Models
{
    [Table("TRC_PRICESELL")]
    public class PriceSell
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_seq { get; set; }

        [StringLength(10)]
        public string? cust_code { get; set; }

        [StringLength(50)]
        public string? origin { get; set; }

        [StringLength(50)]
        public string? dest { get; set; }

        [StringLength(10)]
        public string? serv_type { get; set; }

        [StringLength(10)]
        public string? serv_moda { get; set; }

        [StringLength(50)]
        public string? truck_size { get; set; }

        [StringLength(10)]
        public string? charge_uom { get; set; }

        public byte? flag_min { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? charge_min { get; set; }

        public byte? flag_range { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? min_range { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? max_range { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell1 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell2 { get; set; }

        [Column(TypeName = "money")]
        public decimal? sell3 { get; set; }

        [Column("sell_ret_empty", TypeName = "money")]
        public decimal? sell_ret_empty { get; set; }

        [Column("sell_ret_cargo", TypeName = "money")]
        public decimal? sell_ret_cargo { get; set; }

        [Column("sell_ovnight", TypeName = "money")]
        public decimal? sell_ovnight { get; set; }

        [Column("sell_cancel", TypeName = "money")]
        public decimal? sell_cancel { get; set; }

        [Column("selltrip2", TypeName = "money")]
        public decimal? selltrip2 { get; set; }

        [Column("selltrip3", TypeName = "money")]
        public decimal? selltrip3 { get; set; }

        [Column("sell_diff_area", TypeName = "money")]
        public decimal? sell_diff_area { get; set; }

        public DateTime? valid_date { get; set; }

        public byte? active_flag { get; set; }

        [StringLength(10)]
        public string? curr { get; set; }

        [Column(TypeName = "money")]
        public decimal? rate_value { get; set; }

        [StringLength(50)]
        public string? entry_user { get; set; }

        public DateTime? entry_date { get; set; }

        [StringLength(50)]
        public string? update_user { get; set; }

        public DateTime? update_date { get; set; }
    }
}

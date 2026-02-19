namespace TMSBilling.Services.Reports
{
    /// <summary>
    /// Interface yang harus diimplementasi oleh setiap report type.
    /// Untuk menambah report type baru, cukup buat class baru yang implement interface ini
    /// lalu daftarkan di Program.cs — controller tidak perlu diubah sama sekali.
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// Nilai yang harus cocok dengan value di dropdown reportType di view.
        /// Contoh: "PROFORMA-INVOICE", "DELIVERY-ORDER", dst.
        /// </summary>
        string ReportType { get; }

        /// <summary>
        /// Generate Excel dan return sebagai byte array.
        /// </summary>
        byte[] Generate(int customerId, string customerName, DateTime dateFrom, DateTime dateTo);
    }
}
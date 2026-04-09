using Microsoft.EntityFrameworkCore;
using System.Linq;
using TMSBilling.Models;

namespace TMSBilling.Data.Seeders
{
    public static class MstHolidaySeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.MstHolidays.AnyAsync()) return;

            var holidays = new List<MstHoliday>();

            // ============================================================
            // TAHUN 2026
            // Sumber: SKB 3 Menteri No.1497/2025 — RESMI (setneg.go.id)
            // ============================================================
            var national2026 = new List<(string Date, string Name)>
            {
                ("2026-01-01", "Tahun Baru 2026 Masehi"),
                ("2026-01-16", "Isra Mikraj Nabi Muhammad SAW"),
                ("2026-02-17", "Tahun Baru Imlek 2577 Kongzili"),
                ("2026-03-19", "Hari Suci Nyepi (Tahun Baru Saka 1948)"),
                ("2026-03-21", "Idul Fitri 1447 H (Hari 1)"),
                ("2026-03-22", "Idul Fitri 1447 H (Hari 2)"),
                ("2026-04-03", "Wafat Yesus Kristus (Good Friday)"),
                ("2026-04-05", "Kebangkitan Yesus Kristus (Paskah)"),
                ("2026-05-01", "Hari Buruh Internasional"),
                ("2026-05-14", "Kenaikan Yesus Kristus"),
                ("2026-05-27", "Idul Adha 1447 H"),
                ("2026-05-31", "Hari Raya Waisak 2570 BE"),
                ("2026-06-01", "Hari Lahir Pancasila"),
                ("2026-06-16", "Tahun Baru Islam 1448 H"),
                ("2026-08-17", "Hari Kemerdekaan Republik Indonesia"),
                ("2026-08-25", "Maulid Nabi Muhammad SAW"),
                ("2026-12-25", "Hari Raya Natal"),
            };

            var cuti2026 = new List<(string Date, string Name)>
            {
                ("2026-02-16", "Cuti Bersama Tahun Baru Imlek"),
                ("2026-03-18", "Cuti Bersama Hari Suci Nyepi"),
                ("2026-03-20", "Cuti Bersama Idul Fitri"),
                ("2026-03-23", "Cuti Bersama Idul Fitri"),
                ("2026-03-24", "Cuti Bersama Idul Fitri"),
                ("2026-05-15", "Cuti Bersama Kenaikan Yesus Kristus"),
                ("2026-05-28", "Cuti Bersama Idul Adha"),
                ("2026-12-24", "Cuti Bersama Hari Raya Natal"),
            };

            holidays.AddRange(BuildHolidays(national2026, "Libur Nasional"));
            holidays.AddRange(BuildHolidays(cuti2026, "Cuti Bersama"));
            holidays.AddRange(BuildWeekends(2026, holidays));

            // ============================================================
            // TAHUN 2027
            // Sumber: PERKIRAAN berbasis kalender Hijriah & pola SKB
            // TODO: Update setelah SKB resmi diterbitkan (est. akhir 2026)
            // ============================================================
            var national2027 = new List<(string Date, string Name)>
            {
                ("2027-01-01", "Tahun Baru 2027 Masehi"),
                ("2027-01-05", "Isra Mikraj Nabi Muhammad SAW"),
                ("2027-02-06", "Tahun Baru Imlek 2578 Kongzili"),
                ("2027-03-09", "Hari Suci Nyepi (Tahun Baru Saka 1949)"),
                ("2027-03-10", "Idul Fitri 1448 H (Hari 1)"),
                ("2027-03-11", "Idul Fitri 1448 H (Hari 2)"),
                ("2027-03-26", "Wafat Yesus Kristus (Good Friday)"),
                ("2027-03-28", "Kebangkitan Yesus Kristus (Paskah)"),
                ("2027-05-01", "Hari Buruh Internasional"),
                ("2027-05-06", "Kenaikan Yesus Kristus"),
                ("2027-05-17", "Idul Adha 1448 H"),
                ("2027-05-20", "Hari Raya Waisak 2571 BE"),
                ("2027-06-01", "Hari Lahir Pancasila"),
                ("2027-06-06", "Tahun Baru Islam 1449 H"),
                ("2027-08-15", "Maulid Nabi Muhammad SAW"),
                ("2027-08-17", "Hari Kemerdekaan Republik Indonesia"),
                ("2027-12-25", "Hari Raya Natal"),
            };

            // Cuti bersama 2027 belum resmi — estimasi pola umum
            var cuti2027 = new List<(string Date, string Name)>
            {
                ("2027-03-08", "Cuti Bersama Hari Suci Nyepi"),
                ("2027-03-12", "Cuti Bersama Idul Fitri"),
                ("2027-03-15", "Cuti Bersama Idul Fitri"),
                ("2027-12-24", "Cuti Bersama Hari Raya Natal"),
            };

            holidays.AddRange(BuildHolidays(national2027, "Libur Nasional"));
            holidays.AddRange(BuildHolidays(cuti2027, "Cuti Bersama (Estimasi)"));
            holidays.AddRange(BuildWeekends(2027, holidays));

            // ============================================================
            // TAHUN 2028
            // Sumber: PERKIRAAN berbasis kalender Hijriah & pola SKB
            // TODO: Update setelah SKB resmi diterbitkan (est. akhir 2027)
            // ============================================================
            var national2028 = new List<(string Date, string Name)>
            {
                ("2028-01-01", "Tahun Baru 2028 Masehi"),
                ("2028-01-26", "Tahun Baru Imlek 2579 Kongzili"),
                ("2028-02-26", "Idul Fitri 1449 H (Hari 1)"),
                ("2028-02-27", "Idul Fitri 1449 H (Hari 2)"),
                ("2028-03-26", "Hari Suci Nyepi (Tahun Baru Saka 1950)"),
                ("2028-04-14", "Wafat Yesus Kristus (Good Friday)"),
                ("2028-04-16", "Kebangkitan Yesus Kristus (Paskah)"),
                ("2028-05-01", "Hari Buruh Internasional"),
                ("2028-05-05", "Idul Adha 1449 H"),
                ("2028-05-09", "Hari Raya Waisak 2572 BE"),
                ("2028-05-25", "Tahun Baru Islam 1450 H"),
                ("2028-05-25", "Kenaikan Yesus Kristus"),
                ("2028-06-01", "Hari Lahir Pancasila"),
                ("2028-08-03", "Maulid Nabi Muhammad SAW"),
                ("2028-08-17", "Hari Kemerdekaan Republik Indonesia"),
                ("2028-12-14", "Isra Mikraj Nabi Muhammad SAW"),
                ("2028-12-25", "Hari Raya Natal"),
            };

            // Cuti bersama 2028 belum resmi — estimasi pola umum
            var cuti2028 = new List<(string Date, string Name)>
            {
                ("2028-02-28", "Cuti Bersama Idul Fitri"),
                ("2028-02-29", "Cuti Bersama Idul Fitri"),
                ("2028-03-01", "Cuti Bersama Idul Fitri"),
                ("2028-12-24", "Cuti Bersama Hari Raya Natal"),
                ("2028-12-26", "Cuti Bersama Hari Raya Natal"),
            };

            holidays.AddRange(BuildHolidays(national2028, "Libur Nasional"));
            holidays.AddRange(BuildHolidays(cuti2028, "Cuti Bersama (Estimasi)"));
            holidays.AddRange(BuildWeekends(2028, holidays));

            // Simpan ke DB
            await context.MstHolidays.AddRangeAsync(holidays);
            await context.SaveChangesAsync();

            Console.WriteLine($"[Seeder] MstHoliday: {holidays.Count} data berhasil di-seed (2026-2028).");
        }

        // ────────────────────────────────────────────────
        // HELPER: Konversi list tuple ke entity
        // ────────────────────────────────────────────────
        private static List<MstHoliday> BuildHolidays(
            List<(string Date, string Name)> data, string desc)
        {
            return data.Select(x => new MstHoliday
            {
                HolDate = DateOnly.Parse(x.Date),
                HolName = x.Name,
                Description = desc,
                IsActive = true
            }).ToList();
        }

        // ────────────────────────────────────────────────
        // HELPER: Generate Sabtu & Minggu 1 tahun penuh
        // Skip tanggal yang sudah ada di list
        // ────────────────────────────────────────────────
        private static List<MstHoliday> BuildWeekends(int year, List<MstHoliday> existing)
        {
            var existingDates = existing
                .Where(h => h.HolDate.Year == year)
                .Select(h => h.HolDate)
                .ToHashSet();

            var result = new List<MstHoliday>();
            var date = new DateOnly(year, 1, 1);
            var end = new DateOnly(year, 12, 31);

            while (date <= end)
            {
                if ((date.DayOfWeek == DayOfWeek.Saturday ||
                     date.DayOfWeek == DayOfWeek.Sunday)
                    && !existingDates.Contains(date))
                {
                    var dayName = date.DayOfWeek == DayOfWeek.Saturday ? "Sabtu" : "Minggu";
                    result.Add(new MstHoliday
                    {
                        HolDate = date,
                        HolName = $"Hari {dayName}",
                        Description = "Weekend",
                        IsActive = true
                    });
                    existingDates.Add(date); // hindari duplikat kalau dipanggil lagi
                }
                date = date.AddDays(1);
            }

            return result;
        }
    }
}

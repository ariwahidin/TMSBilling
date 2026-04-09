using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Data.Seeders;

namespace TMSBilling.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task<IApplicationBuilder> SeedDatabaseAsync(
            this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync();
            await MstHolidaySeeder.SeedAsync(db);

            return app;
        }
    }
}

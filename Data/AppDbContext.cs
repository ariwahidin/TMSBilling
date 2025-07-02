using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TMSBilling.Models;

namespace TMSBilling.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<CustomerMain> CustomerMains { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<CustomerGroup> CustomerGroups { get; set; }

        public DbSet<Consignee> Consignees { get; set; }

        public DbSet<TruckSize> TruckSizes { get; set; }

        public DbSet<Origin> Origins { get; set; }

        public DbSet<Destination> Destinations { get; set; }

        public DbSet<Warehouse> Warehouses { get; set; }

        public DbSet<Vendor> Vendors { get; set; }

        public DbSet<VendorTruck> VendorTrucks { get; set; }

    }
}

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

        public DbSet<Driver> Drivers { get; set; }

        public DbSet<ServiceModa>  ServiceModas { get; set; }

        public DbSet<ServiceType> ServiceTypes { get; set; }

        public DbSet<ChargeUom> ChargeUoms { get; set; }

        public DbSet<AreaGroup> AreaGroups { get; set; }

        //public DbSet<PriceBuyHeader> PriceBuyHeaders { get; set; }

        public DbSet<PriceBuy> PriceBuys { get; set; }

        public DbSet<PriceSell> PriceSells { get; set; }


        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<JobHeader> JobHeaders { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<JobPOD> JobPODs { get; set; }

    }
}

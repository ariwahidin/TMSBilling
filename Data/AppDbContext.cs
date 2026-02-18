using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TMSBilling.Controllers;
using TMSBilling.Models;
using TMSBilling.Services;

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

        public DbSet<PriceBuy> PriceBuys { get; set; }

        public DbSet<PriceSell> PriceSells { get; set; }


        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<JobHeader> JobHeaders { get; set; }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<JobPOD> JobPODs { get; set; }

        public DbSet<Config> Configs { get; set; }

        public DbSet<ProductTable> Products { get; set; }

        public DbSet<GeofenceTable> Geofences { get; set; }

        public DbSet<MCOrder> MCOrders { get; set; }

        public DbSet<MCFleetOrder> MCFleetOrders { get; set; }

        public DbSet<RouteType> RouteTypes { get; set; }

        public DbSet<RouteGroup> RouteGroups { get; set; }

        public DbSet<UserXCustomer> UserXCustomers { get; set; }

        public DbSet<KMOrder> KMOrders { get; set; }




        public DbSet<OrderSummaryViewModel> OrderSummaryView { get; set; }
        public DbSet<JobSummaryViewModel> JobSummaryView { get; set; }
        public DbSet<ConsigneeViewModel> ConsigneeView { get; set; }

        public DbSet<ConfirmOrderID> ConfirmOrderID { get; set; }

        public DbSet<JobOrder> JobOrder { get; set; }

        public DbSet<OrderForJob> OrderForJob { get; set; }

        public DbSet<OrderNotInJob> OrderNotInJob { get; set; }

        // Data/AppDbContext.cs — tambahkan DbSet dan konfigurasi

        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Menu> Menus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Role self-reference — hindari cascade delete loop
            modelBuilder.Entity<Role>()
                .HasOne(r => r.ParentRole)
                .WithMany(r => r.ChildRoles)
                .HasForeignKey(r => r.ParentRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // RolePermission unique constraint
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            // UserRole unique constraint
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            // Menu FK ke Permission.Code (bukan Id)
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Permission)
                .WithMany(p => p.Menus)
                .HasForeignKey(m => m.PermissionCode)
                .HasPrincipalKey(p => p.Code)  // <-- ini kuncinya
                .OnDelete(DeleteBehavior.SetNull);

            // Permission.Code harus unique agar bisa jadi principal key
            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // Seed Permissions
            modelBuilder.Entity<Permission>().HasData(
                new Permission { Id = 1, Code = "master_data", Name = "Master Data" },
                new Permission { Id = 2, Code = "user", Name = "User Management" },
                new Permission { Id = 3, Code = "customer", Name = "Customer" },
                new Permission { Id = 4, Code = "transaksi", Name = "Transaksi" },
                new Permission { Id = 5, Code = "invoice", Name = "Invoice" },
                new Permission { Id = 6, Code = "report", Name = "Report" },
                new Permission { Id = 7, Code = "settings", Name = "Settings" },
                new Permission { Id = 8, Code = "role_management", Name = "Role Management" }
            );

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "SuperAdmin", Description = "Full access, hardcoded", ParentRoleId = null },
                new Role { Id = 2, Name = "Admin", Description = "Manage data & users", ParentRoleId = 3 },
                new Role { Id = 3, Name = "Operator", Description = "Input & process data", ParentRoleId = 4 },
                new Role { Id = 4, Name = "Viewer", Description = "Read only", ParentRoleId = null }
            );

            // Seed RolePermissions
            // Viewer: bisa lihat report & invoice saja
            // Operator: tambah customer, invoice input (inherit Viewer)
            // Admin: tambah master_data, user, transaksi (inherit Operator)
            // SuperAdmin: bypass hardcoded, tidak perlu seed
            modelBuilder.Entity<RolePermission>().HasData(
                // Viewer
                new RolePermission { Id = 1, RoleId = 4, PermissionId = 6 }, // report
                                                                             // Operator (tambahan dari Viewer)
                new RolePermission { Id = 2, RoleId = 3, PermissionId = 4 }, // transaksi
                new RolePermission { Id = 3, RoleId = 3, PermissionId = 5 }, // invoice
                                                                             // Admin (tambahan dari Operator)
                new RolePermission { Id = 4, RoleId = 2, PermissionId = 1 }, // master_data
                new RolePermission { Id = 5, RoleId = 2, PermissionId = 2 }, // user
                new RolePermission { Id = 6, RoleId = 2, PermissionId = 3 }, // customer
                new RolePermission { Id = 7, RoleId = 2, PermissionId = 7 }, // settings
                new RolePermission { Id = 8, RoleId = 2, PermissionId = 8 }  // role_management
            );

            // Seed Menus
            modelBuilder.Entity<Menu>().HasData(
                new Menu { Id = 1, Name = "Master Data", Url = null, Icon = "fa-database", ParentId = null, PermissionCode = "master_data", OrderIndex = 1 },
                new Menu { Id = 2, Name = "User", Url = "/User/Index", Icon = "fa-users", ParentId = 1, PermissionCode = "user", OrderIndex = 1 },
                new Menu { Id = 3, Name = "Customer", Url = "/Customer/Index", Icon = "fa-building", ParentId = 1, PermissionCode = "customer", OrderIndex = 2 },
                new Menu { Id = 4, Name = "Transaksi", Url = null, Icon = "fa-file-alt", ParentId = null, PermissionCode = "transaksi", OrderIndex = 2 },
                new Menu { Id = 5, Name = "Invoice", Url = "/Invoice/Index", Icon = "fa-receipt", ParentId = 4, PermissionCode = "invoice", OrderIndex = 1 },
                new Menu { Id = 6, Name = "Report", Url = "/Report/Index", Icon = "fa-chart-bar", ParentId = null, PermissionCode = "report", OrderIndex = 3 },
                new Menu { Id = 7, Name = "Settings", Url = null, Icon = "fa-cog", ParentId = null, PermissionCode = "settings", OrderIndex = 4 },
                new Menu { Id = 8, Name = "Roles", Url = "/Role/Index", Icon = "fa-shield-alt", ParentId = 7, PermissionCode = "role_management", OrderIndex = 1 },
                new Menu { Id = 9, Name = "Menus", Url = "/Menu/Index", Icon = "fa fa-bars", ParentId = 7, PermissionCode = null, OrderIndex = 2, IsActive = true },
                new Menu { Id = 11, Name = "Vendor Truck", Url = "/Vendor/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 4, IsActive = true },
                new Menu { Id = 12, Name = "Vendor Vechile", Url = "/VendorTruck/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 5, IsActive = true },
                new Menu { Id = 13, Name = "Driver", Url = "/Driver/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 6, IsActive = true },
                new Menu { Id = 14, Name = "Truck Size", Url = "/TruckSize/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 7, IsActive = true },
                new Menu { Id = 15, Name = "Origin Area", Url = "/Origin/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 8, IsActive = true },
                new Menu { Id = 16, Name = "Destination Area", Url = "/Destination/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 9, IsActive = true },
                new Menu { Id = 17, Name = "Warehouse", Url = "/Warehouse/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 10, IsActive = true },
                new Menu { Id = 18, Name = "Service Moda", Url = "/ServiceModa/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 11, IsActive = true },
                new Menu { Id = 19, Name = "Service Type", Url = "/ServiceType/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 12, IsActive = true },
                new Menu { Id = 20, Name = "Charge UoM", Url = "/ChargeUom/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 13, IsActive = true },
                new Menu { Id = 21, Name = "Area Group", Url = "/AreaGroup/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 14, IsActive = true },
                new Menu { Id = 22, Name = "Price Buy", Url = "/PriceBuy/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 15, IsActive = true },
                new Menu { Id = 23, Name = "Price Sell", Url = "/PriceSell/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 16, IsActive = true },
                new Menu { Id = 24, Name = "Product", Url = "/Product/Index", Icon = "fa-database", ParentId = 1, PermissionCode = null, OrderIndex = 17, IsActive = true }
            );



            modelBuilder.Entity<OrderSummaryViewModel>().HasNoKey().ToView(null);
            modelBuilder.Entity<JobSummaryViewModel>().HasNoKey().ToView(null);
            modelBuilder.Entity<ConsigneeViewModel>().HasNoKey().ToView(null);
            modelBuilder.Entity<ConfirmOrderID>().HasNoKey().ToView(null);
            modelBuilder.Entity<JobOrder>().HasNoKey().ToView(null);
            modelBuilder.Entity<OrderForJob>().HasNoKey().ToView(null);
            modelBuilder.Entity<OrderNotInJob>().HasNoKey().ToView(null);
        }


        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<OrderSummaryViewModel>().HasNoKey().ToView(null);
        //    modelBuilder.Entity<JobSummaryViewModel>().HasNoKey().ToView(null);
        //    modelBuilder.Entity<ConsigneeViewModel>().HasNoKey().ToView(null);
        //    modelBuilder.Entity<ConfirmOrderID>().HasNoKey().ToView(null);
        //    modelBuilder.Entity<JobOrder>().HasNoKey().ToView(null);
        //    modelBuilder.Entity<OrderForJob>().HasNoKey().ToView(null);
        //    modelBuilder.Entity<OrderNotInJob>().HasNoKey().ToView(null);
        //}


    }
}

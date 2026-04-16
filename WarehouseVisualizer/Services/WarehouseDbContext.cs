using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Services
{
    public class WarehouseDbContext : DbContext
    {
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseCell> WarehouseCells { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<MaterialHistoryItem> OperationHistory { get; set; }
        public DbSet<User> Users { get; set; }

        public WarehouseDbContext()
        {
        }

        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("WarehouseDb")
                ?? configuration["ConnectionStrings:WarehouseDb"]
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__WarehouseDb")
                ?? "Data Source=localhost;Initial Catalog=WarehouseDB;Integrated Security=True;TrustServerCertificate=True";

            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MaterialHistoryItem>(entity =>
            {
                entity.Property(h => h.Timestamp).IsRequired();
                entity.Property(h => h.Action).IsRequired().HasMaxLength(50);
                entity.Property(h => h.Location).HasMaxLength(20);
                entity.Property(h => h.MaterialName).HasMaxLength(100);
            });

            modelBuilder.Entity<Warehouse>()
                .HasMany(w => w.Cells)
                .WithOne()
                .HasForeignKey(c => c.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseCell>()
                .HasOne(c => c.Material)
                .WithMany()
                .HasForeignKey(c => c.MaterialId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
                entity.Property(u => u.FullName).HasMaxLength(100);
                entity.Property(u => u.Role).IsRequired();
            });
        }
    }
}

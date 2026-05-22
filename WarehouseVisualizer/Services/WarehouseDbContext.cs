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
        public DbSet<Notification> Notifications { get; set; }
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
                entity.Property(h => h.UserName).HasMaxLength(100);
                entity.Property(h => h.FromLocation).HasMaxLength(20);
                entity.Property(h => h.ToLocation).HasMaxLength(20);
                entity.Property(h => h.Location).HasMaxLength(20);
                entity.Property(h => h.MaterialName).HasMaxLength(100);
                entity.Property(h => h.Reason).HasMaxLength(250);
                entity.Property(h => h.Comment).HasMaxLength(500);
                entity.HasIndex(h => h.MaterialId);
                entity.HasOne(h => h.Material)
                    .WithMany()
                    .HasForeignKey(h => h.MaterialId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Material>(entity =>
            {
                entity.Property(m => m.QrCode).HasMaxLength(128);
                entity.HasIndex(m => m.QrCode);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
                entity.Property(n => n.Timestamp).IsRequired();
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.Type);
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

        public void EnsureDiplomaSchema()
        {
            Database.ExecuteSqlRaw("""
                IF OBJECT_ID('OperationHistory', 'U') IS NULL
                BEGIN
                    CREATE TABLE [OperationHistory] (
                        [Id] int NOT NULL IDENTITY,
                        [Timestamp] datetime2 NOT NULL,
                        [Action] nvarchar(50) NOT NULL,
                        [Location] nvarchar(20) NOT NULL,
                        [MaterialName] nvarchar(100) NOT NULL,
                        [Quantity] int NOT NULL,
                        CONSTRAINT [PK_OperationHistory] PRIMARY KEY ([Id])
                    );
                END
                """);

            Database.ExecuteSqlRaw("""
                IF OBJECT_ID('Users', 'U') IS NULL
                BEGIN
                    CREATE TABLE [Users] (
                        [Id] int NOT NULL IDENTITY,
                        [Username] nvarchar(50) NOT NULL,
                        [PasswordHash] nvarchar(256) NOT NULL,
                        [Role] int NOT NULL,
                        [FullName] nvarchar(100) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    );
                END
                """);

            Database.ExecuteSqlRaw("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('Users'))
                    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('Materials', 'CreatedAt') IS NULL
                    ALTER TABLE [Materials] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Materials_CreatedAt] DEFAULT GETDATE();
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('Materials', 'QrCode') IS NULL
                    ALTER TABLE [Materials] ADD [QrCode] nvarchar(128) NOT NULL CONSTRAINT [DF_Materials_QrCode] DEFAULT '';
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('Materials', 'Status') IS NULL
                    ALTER TABLE [Materials] ADD [Status] int NOT NULL CONSTRAINT [DF_Materials_Status] DEFAULT 0;
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'ActionType') IS NULL
                    ALTER TABLE [OperationHistory] ADD [ActionType] int NOT NULL CONSTRAINT [DF_OperationHistory_ActionType] DEFAULT 1;
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'MaterialId') IS NULL
                    ALTER TABLE [OperationHistory] ADD [MaterialId] int NULL;
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'FromLocation') IS NULL
                    ALTER TABLE [OperationHistory] ADD [FromLocation] nvarchar(20) NOT NULL CONSTRAINT [DF_OperationHistory_FromLocation] DEFAULT '';
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'ToLocation') IS NULL
                    ALTER TABLE [OperationHistory] ADD [ToLocation] nvarchar(20) NOT NULL CONSTRAINT [DF_OperationHistory_ToLocation] DEFAULT '';
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'UserName') IS NULL
                    ALTER TABLE [OperationHistory] ADD [UserName] nvarchar(100) NOT NULL CONSTRAINT [DF_OperationHistory_UserName] DEFAULT '';
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'Reason') IS NULL
                    ALTER TABLE [OperationHistory] ADD [Reason] nvarchar(250) NOT NULL CONSTRAINT [DF_OperationHistory_Reason] DEFAULT '';
                """);

            Database.ExecuteSqlRaw("""
                IF COL_LENGTH('OperationHistory', 'Comment') IS NULL
                    ALTER TABLE [OperationHistory] ADD [Comment] nvarchar(500) NOT NULL CONSTRAINT [DF_OperationHistory_Comment] DEFAULT '';
                """);

            Database.ExecuteSqlRaw("""
                IF OBJECT_ID('Notifications', 'U') IS NULL
                BEGIN
                    CREATE TABLE [Notifications] (
                        [Id] int NOT NULL IDENTITY,
                        [Message] nvarchar(500) NOT NULL,
                        [Type] int NOT NULL,
                        [Priority] int NOT NULL,
                        [IsRead] bit NOT NULL,
                        [Timestamp] datetime2 NOT NULL,
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
                    );
                END
                """);

            Database.ExecuteSqlRaw("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Materials_QrCode' AND object_id = OBJECT_ID('Materials'))
                    CREATE INDEX [IX_Materials_QrCode] ON [Materials] ([QrCode]);
                """);

            Database.ExecuteSqlRaw("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OperationHistory_MaterialId' AND object_id = OBJECT_ID('OperationHistory'))
                    CREATE INDEX [IX_OperationHistory_MaterialId] ON [OperationHistory] ([MaterialId]);
                """);

            Database.ExecuteSqlRaw("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_IsRead' AND object_id = OBJECT_ID('Notifications'))
                    CREATE INDEX [IX_Notifications_IsRead] ON [Notifications] ([IsRead]);
                """);

            Database.ExecuteSqlRaw("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_Type' AND object_id = OBJECT_ID('Notifications'))
                    CREATE INDEX [IX_Notifications_Type] ON [Notifications] ([Type]);
                """);

            Database.ExecuteSqlRaw("""
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OperationHistory_Materials_MaterialId')
                    ALTER TABLE [OperationHistory] ADD CONSTRAINT [FK_OperationHistory_Materials_MaterialId]
                    FOREIGN KEY ([MaterialId]) REFERENCES [Materials] ([Id]) ON DELETE SET NULL;
                """);
        }
    }
}

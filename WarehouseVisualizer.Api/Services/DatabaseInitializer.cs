using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Services;

public sealed class DatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 15;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

                if (await TableExistsAsync(context, "Materials", cancellationToken))
                {
                    context.EnsureDiplomaSchema();
                }
                else
                {
                    await context.Database.MigrateAsync(cancellationToken);
                    context.EnsureDiplomaSchema();
                }

                await SeedAdminUserAsync(context, authService, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Database initialization attempt {Attempt} of {MaxAttempts} failed. Retrying...", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        using var finalScope = _serviceProvider.CreateScope();
        var finalContext = finalScope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        if (await TableExistsAsync(finalContext, "Materials", cancellationToken))
        {
            finalContext.EnsureDiplomaSchema();
        }
        else
        {
            await finalContext.Database.MigrateAsync(cancellationToken);
            finalContext.EnsureDiplomaSchema();
        }

        var finalAuthService = finalScope.ServiceProvider.GetRequiredService<IAuthService>();
        await SeedAdminUserAsync(finalContext, finalAuthService, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(WarehouseDbContext context, string tableName, CancellationToken cancellationToken)
    {
        if (!string.Equals(tableName, "Materials", StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unsupported table check.");
        }

        var result = await context.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'[Materials]', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(cancellationToken);

        return result == 1;
    }

    private async Task SeedAdminUserAsync(WarehouseDbContext context, IAuthService authService, CancellationToken cancellationToken)
    {
        var username = _configuration["AdminSeed:Username"] ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
        var password = _configuration["AdminSeed:Password"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "123";
        var fullName = _configuration["AdminSeed:FullName"] ?? Environment.GetEnvironmentVariable("ADMIN_FULLNAME") ?? "System Administrator";
        var passwordHash = authService.HashPassword(password);

        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (existingAdmin is not null)
        {
            if (existingAdmin.PasswordHash != passwordHash ||
                existingAdmin.FullName != fullName ||
                existingAdmin.Role != UserRole.Admin ||
                !existingAdmin.IsActive)
            {
                existingAdmin.PasswordHash = passwordHash;
                existingAdmin.FullName = fullName;
                existingAdmin.Role = UserRole.Admin;
                existingAdmin.IsActive = true;
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated admin user '{Username}' with configured credentials.", username);
            }

            return;
        }

        context.Users.Add(new User
        {
            Username = username,
            PasswordHash = passwordHash,
            Role = UserRole.Admin,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded admin user '{Username}'.", username);
    }
}

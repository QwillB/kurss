using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Api.Services;
using WarehouseVisualizer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("WarehouseDb")
    ?? builder.Configuration["ConnectionStrings:WarehouseDb"]
    ?? builder.Configuration["ConnectionStrings__WarehouseDb"]
    ?? "Server=localhost,1433;Database=WarehouseDb;User Id=sa;Password=Strong_password_123!;TrustServerCertificate=True;Encrypt=False";

builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<WarehouseApiService>();
builder.Services.AddScoped<DatabaseInitializer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

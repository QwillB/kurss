using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Contracts;

public sealed class DashboardSummary
{
    public int TotalMaterials { get; set; }
    public int TotalQuantity { get; set; }
    public int TotalCells { get; set; }
    public int OccupiedCells { get; set; }
    public int FreeCells { get; set; }
    public double OccupancyPercentage { get; set; }
    public int LowStockCount { get; set; }
    public int OperationsToday { get; set; }
    public int OperationsWeek { get; set; }
    public int OperationsMonth { get; set; }
}

public sealed class CategoryCountDto
{
    public MaterialType Category { get; set; }
    public int Count { get; set; }
    public int Quantity { get; set; }
}

public sealed class WarehouseOccupancyDto
{
    public int TotalCells { get; set; }
    public int OccupiedCells { get; set; }
    public int FreeCells { get; set; }
    public double OccupancyPercentage { get; set; }
}

public sealed class ActivityDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public sealed class MovementTopDto
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public int MovesCount { get; set; }
}

using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Contracts;

public sealed class MoveMaterialRequest
{
    public int? FromCellId { get; set; }
    public int? ToCellId { get; set; }
    public int? TargetRow { get; set; }
    public int? TargetColumn { get; set; }
    public string? UserName { get; set; }
    public string? Reason { get; set; }
    public string? Comment { get; set; }
}

public sealed class MaterialSearchQuery
{
    public string? Name { get; set; }
    public MaterialType? Category { get; set; }
    public string? Cell { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? LowStock { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? LastUser { get; set; }
    public MaterialStatus? Status { get; set; }
    public string? Code { get; set; }
    public string? SortBy { get; set; }
    public bool Desc { get; set; }
}

public sealed class MaterialSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MaterialType Type { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Cell { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LastUser { get; set; }
    public MaterialStatus Status { get; set; }
    public string QrCode { get; set; } = string.Empty;
}

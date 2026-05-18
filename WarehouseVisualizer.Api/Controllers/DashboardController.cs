using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Api.Contracts;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public DashboardController(WarehouseDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary(CancellationToken cancellationToken)
    {
        var occupancy = await BuildOccupancyAsync(cancellationToken);
        var now = DateTime.Now;
        var today = now.Date;
        var week = today.AddDays(-7);
        var month = today.AddMonths(-1);

        return Ok(new DashboardSummary
        {
            TotalMaterials = await _context.Materials.CountAsync(cancellationToken),
            TotalQuantity = await _context.Materials.SumAsync(m => (int?)m.Quantity, cancellationToken) ?? 0,
            TotalCells = occupancy.TotalCells,
            OccupiedCells = occupancy.OccupiedCells,
            FreeCells = occupancy.FreeCells,
            OccupancyPercentage = occupancy.OccupancyPercentage,
            LowStockCount = await _context.Materials.CountAsync(m => m.Quantity <= 5, cancellationToken),
            OperationsToday = await _context.OperationHistory.CountAsync(h => h.Timestamp >= today, cancellationToken),
            OperationsWeek = await _context.OperationHistory.CountAsync(h => h.Timestamp >= week, cancellationToken),
            OperationsMonth = await _context.OperationHistory.CountAsync(h => h.Timestamp >= month, cancellationToken)
        });
    }

    [HttpGet("materials-by-category")]
    public async Task<ActionResult<IReadOnlyList<CategoryCountDto>>> GetMaterialsByCategory(CancellationToken cancellationToken)
    {
        return Ok(await _context.Materials
            .AsNoTracking()
            .GroupBy(m => m.Type)
            .Select(g => new CategoryCountDto
            {
                Category = g.Key,
                Count = g.Count(),
                Quantity = g.Sum(m => m.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("warehouse-occupancy")]
    public async Task<ActionResult<WarehouseOccupancyDto>> GetWarehouseOccupancy(CancellationToken cancellationToken)
    {
        return Ok(await BuildOccupancyAsync(cancellationToken));
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetRecentActivity([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await _context.OperationHistory
            .AsNoTracking()
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .Select(h => new ActivityDto
            {
                Timestamp = h.Timestamp,
                Action = h.Action,
                MaterialName = h.MaterialName,
                UserName = h.UserName,
                Location = string.IsNullOrWhiteSpace(h.ToLocation) ? h.Location : h.ToLocation
            })
            .ToListAsync(cancellationToken));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<Material>>> GetLowStock([FromQuery] int threshold = 5, CancellationToken cancellationToken = default)
    {
        return Ok(await _context.Materials
            .AsNoTracking()
            .Where(m => m.Quantity <= threshold)
            .OrderBy(m => m.Quantity)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("top-moved")]
    public async Task<ActionResult<IReadOnlyList<MovementTopDto>>> GetTopMoved([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        return Ok(await _context.OperationHistory
            .AsNoTracking()
            .Where(h => h.ActionType == MaterialHistoryActionType.Moved && h.MaterialId.HasValue)
            .GroupBy(h => new { h.MaterialId, h.MaterialName })
            .Select(g => new MovementTopDto
            {
                MaterialId = g.Key.MaterialId!.Value,
                MaterialName = g.Key.MaterialName,
                MovesCount = g.Count()
            })
            .OrderByDescending(x => x.MovesCount)
            .Take(limit)
            .ToListAsync(cancellationToken));
    }

    private async Task<WarehouseOccupancyDto> BuildOccupancyAsync(CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .OrderByDescending(w => w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var totalCells = warehouse is null ? 0 : warehouse.Rows * warehouse.Columns;
        var occupied = await _context.WarehouseCells.CountAsync(c => c.MaterialId != null, cancellationToken);
        var free = Math.Max(0, totalCells - occupied);

        return new WarehouseOccupancyDto
        {
            TotalCells = totalCells,
            OccupiedCells = occupied,
            FreeCells = free,
            OccupancyPercentage = totalCells > 0 ? occupied * 100.0 / totalCells : 0
        };
    }
}

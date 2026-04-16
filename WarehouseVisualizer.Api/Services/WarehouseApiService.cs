using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Services;

public sealed class WarehouseApiService
{
    private readonly WarehouseDbContext _context;

    public WarehouseApiService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<Warehouse> LoadWarehouseAsync(CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.Cells)
            .ThenInclude(c => c.Material)
            .OrderByDescending(w => w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null)
        {
            warehouse = new Warehouse
            {
                Rows = 8,
                Columns = 8
            };

            warehouse.RebuildCells();
            return warehouse;
        }

        warehouse.RebuildCells();
        return warehouse;
    }

    public async Task<Warehouse> SaveWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        var dbWarehouse = await _context.Warehouses
            .OrderByDescending(w => w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (dbWarehouse is null)
        {
            dbWarehouse = new Warehouse
            {
                Rows = warehouse.Rows,
                Columns = warehouse.Columns
            };

            _context.Warehouses.Add(dbWarehouse);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            dbWarehouse.Rows = warehouse.Rows;
            dbWarehouse.Columns = warehouse.Columns;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var oldCells = await _context.WarehouseCells
            .Where(c => c.WarehouseId == dbWarehouse.Id)
            .ToListAsync(cancellationToken);

        if (oldCells.Count > 0)
        {
            _context.WarehouseCells.RemoveRange(oldCells);
            await _context.SaveChangesAsync(cancellationToken);
        }

        foreach (var cell in warehouse.Cells)
        {
            var newCell = new WarehouseCell
            {
                WarehouseId = dbWarehouse.Id,
                Row = cell.Row,
                Column = cell.Column
            };

            if (cell.Material is not null && !string.IsNullOrWhiteSpace(cell.Material.Name))
            {
                Material? dbMaterial = null;

                if (cell.Material.Id > 0)
                {
                    dbMaterial = await _context.Materials.FindAsync([cell.Material.Id], cancellationToken);
                }

                if (dbMaterial is null)
                {
                    dbMaterial = new Material
                    {
                        Name = cell.Material.Name,
                        Type = cell.Material.Type,
                        Quantity = cell.Material.Quantity,
                        Unit = cell.Material.Unit
                    };

                    _context.Materials.Add(dbMaterial);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    dbMaterial.Name = cell.Material.Name;
                    dbMaterial.Type = cell.Material.Type;
                    dbMaterial.Quantity = cell.Material.Quantity;
                    dbMaterial.Unit = cell.Material.Unit;
                }

                newCell.MaterialId = dbMaterial.Id;
            }

            _context.WarehouseCells.Add(newCell);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await LoadWarehouseAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialHistoryItem>> LoadHistoryAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.OperationHistory
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<MaterialHistoryItem> AddHistoryItemAsync(MaterialHistoryItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        _context.OperationHistory.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<WarehouseReport> BuildReportAsync(CancellationToken cancellationToken = default)
    {
        var warehouse = await LoadWarehouseAsync(cancellationToken);
        var report = new WarehouseReport
        {
            ReportDate = DateTime.UtcNow,
            TotalRows = warehouse.Rows,
            TotalColumns = warehouse.Columns,
            TotalCells = warehouse.Rows * warehouse.Columns
        };

        var materialGroups = new Dictionary<int, (Material Material, List<string> Locations)>();

        foreach (var cell in warehouse.Cells)
        {
            if (!cell.HasMaterial || cell.Material is null)
            {
                continue;
            }

            report.OccupiedCells++;

            if (!materialGroups.ContainsKey(cell.Material.Id))
            {
                materialGroups[cell.Material.Id] = (cell.Material, new List<string>());
            }

            materialGroups[cell.Material.Id].Locations.Add(cell.Location);
        }

        report.FreeCells = report.TotalCells - report.OccupiedCells;

        foreach (var entry in materialGroups.Values)
        {
            report.Materials.Add(new MaterialReportItem
            {
                Name = entry.Material.Name,
                Type = entry.Material.Type,
                Quantity = entry.Material.Quantity * entry.Locations.Count,
                Unit = entry.Material.Unit,
                CellCount = entry.Locations.Count,
                Locations = string.Join(", ", entry.Locations.Take(5)) + (entry.Locations.Count > 5 ? "..." : string.Empty)
            });
        }

        return report;
    }
}

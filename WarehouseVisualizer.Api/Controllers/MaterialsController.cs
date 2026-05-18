using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Api.Contracts;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MaterialsController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public MaterialsController(WarehouseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialSearchResult>>> GetAll([FromQuery] MaterialSearchQuery query, CancellationToken cancellationToken)
    {
        var materialsQuery = _context.Materials
            .AsNoTracking()
            .Select(m => new MaterialSearchResult
            {
                Id = m.Id,
                Name = m.Name,
                Type = m.Type,
                Quantity = m.Quantity,
                Unit = m.Unit,
                CreatedAt = m.CreatedAt,
                Status = m.Status,
                QrCode = m.QrCode,
                IsLowStock = m.Quantity <= 5,
                Cell = _context.WarehouseCells
                    .Where(c => c.MaterialId == m.Id)
                    .Select(c => (c.Row + 1) + "-" + (c.Column + 1))
                    .FirstOrDefault(),
                LastUser = _context.OperationHistory
                    .Where(h => h.MaterialId == m.Id)
                    .OrderByDescending(h => h.Timestamp)
                    .Select(h => h.UserName)
                    .FirstOrDefault()
            });

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            materialsQuery = materialsQuery.Where(m => m.Name.Contains(query.Name));
        }

        if (query.Category.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.Type == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Cell))
        {
            materialsQuery = materialsQuery.Where(m => m.Cell != null && m.Cell.Contains(query.Cell));
        }

        if (query.MinQuantity.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.Quantity >= query.MinQuantity);
        }

        if (query.MaxQuantity.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.Quantity <= query.MaxQuantity);
        }

        if (query.LowStock.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.IsLowStock == query.LowStock.Value);
        }

        if (query.CreatedFrom.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.CreatedAt >= query.CreatedFrom);
        }

        if (query.CreatedTo.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.CreatedAt <= query.CreatedTo);
        }

        if (!string.IsNullOrWhiteSpace(query.LastUser))
        {
            materialsQuery = materialsQuery.Where(m => m.LastUser != null && m.LastUser.Contains(query.LastUser));
        }

        if (query.Status.HasValue)
        {
            materialsQuery = materialsQuery.Where(m => m.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            materialsQuery = materialsQuery.Where(m => m.QrCode.Contains(query.Code) || m.Id.ToString() == query.Code);
        }

        materialsQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => query.Desc ? materialsQuery.OrderByDescending(m => m.Quantity) : materialsQuery.OrderBy(m => m.Quantity),
            "category" or "type" => query.Desc ? materialsQuery.OrderByDescending(m => m.Type) : materialsQuery.OrderBy(m => m.Type),
            "created" => query.Desc ? materialsQuery.OrderByDescending(m => m.CreatedAt) : materialsQuery.OrderBy(m => m.CreatedAt),
            _ => query.Desc ? materialsQuery.OrderByDescending(m => m.Name) : materialsQuery.OrderBy(m => m.Name)
        };

        return Ok(await materialsQuery.ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Material>> GetById(int id, CancellationToken cancellationToken)
    {
        var material = await _context.Materials.FindAsync([id], cancellationToken);
        return material is null ? NotFound() : Ok(material);
    }

    [HttpPost]
    public async Task<ActionResult<Material>> Create([FromBody] Material material, CancellationToken cancellationToken)
    {
        material.CreatedAt = DateTime.Now;
        material.QrCode = string.IsNullOrWhiteSpace(material.QrCode) ? $"MAT-{Guid.NewGuid():N}" : material.QrCode;
        _context.Materials.Add(material);
        await _context.SaveChangesAsync(cancellationToken);
        await AddHistoryAsync(material, MaterialHistoryActionType.Created, string.Empty, string.Empty, "Создание материала", cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Material>> Update(int id, [FromBody] Material material, CancellationToken cancellationToken)
    {
        var existing = await _context.Materials.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var oldQuantity = existing.Quantity;
        existing.Name = material.Name;
        existing.Type = material.Type;
        existing.Quantity = material.Quantity;
        existing.Unit = material.Unit;
        existing.Status = material.Status;
        existing.QrCode = material.QrCode;

        await _context.SaveChangesAsync(cancellationToken);
        var action = oldQuantity != material.Quantity ? MaterialHistoryActionType.QuantityChanged : MaterialHistoryActionType.Updated;
        await AddHistoryAsync(existing, action, string.Empty, string.Empty, "Обновление материала", cancellationToken);
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _context.Materials.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        await AddHistoryAsync(existing, MaterialHistoryActionType.Deleted, string.Empty, string.Empty, "Удаление материала", cancellationToken);
        _context.Materials.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<MaterialHistoryItem>>> GetHistory(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? user,
        [FromQuery] MaterialHistoryActionType? actionType,
        CancellationToken cancellationToken)
    {
        var materialName = await _context.Materials
            .Where(m => m.Id == id)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var historyQuery = _context.OperationHistory
            .AsNoTracking()
            .Where(h => h.MaterialId == id || h.MaterialName == materialName);

        if (from.HasValue)
        {
            historyQuery = historyQuery.Where(h => h.Timestamp >= from);
        }

        if (to.HasValue)
        {
            historyQuery = historyQuery.Where(h => h.Timestamp <= to);
        }

        if (!string.IsNullOrWhiteSpace(user))
        {
            historyQuery = historyQuery.Where(h => h.UserName.Contains(user));
        }

        if (actionType.HasValue)
        {
            historyQuery = historyQuery.Where(h => h.ActionType == actionType);
        }

        return Ok(await historyQuery.OrderByDescending(h => h.Timestamp).ToListAsync(cancellationToken));
    }

    [HttpPost("{id:int}/move")]
    public async Task<ActionResult<MaterialHistoryItem>> Move(int id, [FromBody] MoveMaterialRequest request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials.FindAsync([id], cancellationToken);
        if (material is null)
        {
            return NotFound();
        }

        var fromCell = request.FromCellId.HasValue
            ? await _context.WarehouseCells.FirstOrDefaultAsync(c => c.Id == request.FromCellId && c.MaterialId == id, cancellationToken)
            : await _context.WarehouseCells.FirstOrDefaultAsync(c => c.MaterialId == id, cancellationToken);

        var toCell = request.ToCellId.HasValue
            ? await _context.WarehouseCells.FirstOrDefaultAsync(c => c.Id == request.ToCellId, cancellationToken)
            : await _context.WarehouseCells.FirstOrDefaultAsync(c => c.Row == request.TargetRow && c.Column == request.TargetColumn, cancellationToken);

        if (toCell is null)
        {
            return BadRequest("Target cell was not found.");
        }

        if (toCell.MaterialId.HasValue && toCell.MaterialId.Value != id)
        {
            return Conflict("Target cell is already occupied.");
        }

        var fromLocation = fromCell is null ? string.Empty : $"{fromCell.Row + 1}-{fromCell.Column + 1}";
        var toLocation = $"{toCell.Row + 1}-{toCell.Column + 1}";

        if (fromCell is not null && fromCell.Id != toCell.Id)
        {
            fromCell.MaterialId = null;
        }

        toCell.MaterialId = id;

        var history = new MaterialHistoryItem
        {
            MaterialId = id,
            MaterialName = material.Name,
            Quantity = material.Quantity,
            Action = "Перемещение",
            ActionType = MaterialHistoryActionType.Moved,
            FromLocation = fromLocation,
            ToLocation = toLocation,
            Location = toLocation,
            UserName = request.UserName ?? User.Identity?.Name ?? "system",
            Reason = request.Reason ?? string.Empty,
            Comment = request.Comment ?? string.Empty,
            Timestamp = DateTime.Now
        };

        _context.OperationHistory.Add(history);
        _context.Notifications.Add(new Notification
        {
            Message = $"Материал '{material.Name}' перемещен: {fromLocation} -> {toLocation}",
            Type = NotificationType.MaterialMoved,
            Priority = NotificationPriority.Medium,
            Timestamp = DateTime.Now
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(history);
    }

    private async Task AddHistoryAsync(Material material, MaterialHistoryActionType actionType, string from, string to, string reason, CancellationToken cancellationToken)
    {
        _context.OperationHistory.Add(new MaterialHistoryItem
        {
            MaterialId = material.Id,
            MaterialName = material.Name,
            Quantity = material.Quantity,
            Action = actionType.ToString(),
            ActionType = actionType,
            FromLocation = from,
            ToLocation = to,
            Location = to,
            UserName = User.Identity?.Name ?? "system",
            Reason = reason,
            Timestamp = DateTime.Now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}

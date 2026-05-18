using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public NotificationsController(WarehouseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Notification>>> Get(
        [FromQuery] NotificationType? type,
        [FromQuery] bool? isRead,
        CancellationToken cancellationToken)
    {
        var query = _context.Notifications.AsNoTracking();

        if (type.HasValue)
        {
            query = query.Where(n => n.Type == type.Value);
        }

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        return Ok(await query
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.Priority)
            .ThenByDescending(n => n.Timestamp)
            .ToListAsync(cancellationToken));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications.FindAsync([id], cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _context.Notifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);

        return NoContent();
    }
}

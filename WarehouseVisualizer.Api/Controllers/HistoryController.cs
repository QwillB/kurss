using Microsoft.AspNetCore.Mvc;
using WarehouseVisualizer.Api.Services;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HistoryController : ControllerBase
{
    private readonly WarehouseApiService _warehouseService;

    public HistoryController(WarehouseApiService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialHistoryItem>>> Get([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        return Ok(await _warehouseService.LoadHistoryAsync(limit, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<MaterialHistoryItem>> Create([FromBody] MaterialHistoryItem item, CancellationToken cancellationToken)
    {
        var created = await _warehouseService.AddHistoryItemAsync(item, cancellationToken);
        return CreatedAtAction(nameof(Get), new { limit = 1 }, created);
    }
}

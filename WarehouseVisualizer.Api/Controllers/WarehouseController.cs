using Microsoft.AspNetCore.Mvc;
using WarehouseVisualizer.Api.Services;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WarehouseController : ControllerBase
{
    private readonly WarehouseApiService _warehouseService;

    public WarehouseController(WarehouseApiService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<ActionResult<Warehouse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.LoadWarehouseAsync(cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<Warehouse>> Save([FromBody] Warehouse warehouse, CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.SaveWarehouseAsync(warehouse, cancellationToken));
    }
}

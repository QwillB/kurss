using Microsoft.AspNetCore.Mvc;
using WarehouseVisualizer.Api.Services;
using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly WarehouseApiService _warehouseService;

    public ReportsController(WarehouseApiService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet("warehouse")]
    public async Task<ActionResult<WarehouseReport>> GetWarehouseReport(CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.BuildReportAsync(cancellationToken));
    }
}

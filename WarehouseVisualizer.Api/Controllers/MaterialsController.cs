using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<ActionResult<IReadOnlyList<Material>>> GetAll(CancellationToken cancellationToken)
    {
        var materials = await _context.Materials
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return Ok(materials);
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
        _context.Materials.Add(material);
        await _context.SaveChangesAsync(cancellationToken);
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

        existing.Name = material.Name;
        existing.Type = material.Type;
        existing.Quantity = material.Quantity;
        existing.Unit = material.Unit;

        await _context.SaveChangesAsync(cancellationToken);
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

        _context.Materials.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

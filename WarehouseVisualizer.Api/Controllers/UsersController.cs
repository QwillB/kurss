using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Api.Contracts;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly IAuthService _authService;

    public UsersController(WarehouseDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] UpsertUserRequest request, CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            return Conflict($"User '{request.Username}' already exists.");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _authService.HashPassword(string.IsNullOrWhiteSpace(request.Password) ? "password123" : request.Password),
            Role = request.Role,
            FullName = request.FullName,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UpsertUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var usernameTaken = await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != id, cancellationToken);
        if (usernameTaken)
        {
            return Conflict($"User '{request.Username}' already exists.");
        }

        user.Username = request.Username;
        user.FullName = request.FullName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _authService.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Role == UserRole.Admin)
        {
            return BadRequest("Admin user cannot be deleted.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

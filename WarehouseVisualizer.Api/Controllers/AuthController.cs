using Microsoft.AspNetCore.Mvc;
using WarehouseVisualizer.Api.Contracts;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var user = _authService.Authenticate(request.Username ?? string.Empty, request.Password ?? string.Empty);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new LoginResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }
}

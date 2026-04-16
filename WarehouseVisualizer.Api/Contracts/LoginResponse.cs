using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Contracts;

public sealed class LoginResponse
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
}

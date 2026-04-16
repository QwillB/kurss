using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Api.Contracts;

public sealed class UpsertUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string? Password { get; init; }
    public UserRole Role { get; init; }
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed class UserResponse
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

namespace WarehouseVisualizer.Models
{
    public enum MaterialType
    {
        Cable,
        Pipe,
        Tool,
        Lumber,
        Metal,
        Concrete,
        Insulation,
        Paint,
        Other
    }

    public enum UserRole
    {
        Admin = 0,
        Storekeeper = 1,
        Auditor = 2
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}
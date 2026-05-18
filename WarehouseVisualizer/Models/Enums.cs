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
        Success,
        LowStock,
        WarehouseFull,
        MaterialMoved,
        UnauthorizedActionAttempt,
        ReportGenerated,
        BackupCompleted,
        PlacementSuggestion,
        SystemWarning
    }

    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum MaterialHistoryActionType
    {
        Created,
        Updated,
        Moved,
        Deleted,
        QuantityChanged,
        Assigned,
        Restored
    }

    public enum MaterialStatus
    {
        Active,
        Reserved,
        Archived,
        Damaged
    }
}

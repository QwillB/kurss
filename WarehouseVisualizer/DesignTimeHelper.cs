using WarehouseVisualizer.Models;

namespace WarehouseVisualizer
{
    public static class DesignTimeHelper
    {
        public static Warehouse WarehouseStatic { get; } = new Warehouse { Rows = 8, Columns = 8 };
    }
}
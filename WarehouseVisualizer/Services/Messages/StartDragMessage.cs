using WarehouseVisualizer.Models;

namespace WarehouseVisualizer.Services
{
    public class StartDragMessage
    {
        public Material Material { get; }

        public StartDragMessage(Material material)
        {
            Material = material;
        }
    }
}

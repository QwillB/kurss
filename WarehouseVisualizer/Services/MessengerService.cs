using WarehouseVisualizer.Models; // Добавьте эту строку
using CommunityToolkit.Mvvm.Messaging;
using WarehouseVisualizer.ViewModels;



namespace WarehouseVisualizer.Services
{
    public static class MessengerService
    {
        public static void SendNotification(string message)
        {
            WeakReferenceMessenger.Default.Send(new VmNotificationMessage(message));
        }

        public static void SendStartDrag(Material material) // Теперь Material будет распознан
        {
            WeakReferenceMessenger.Default.Send(new StartDragMessage(material));
        }
    }
}
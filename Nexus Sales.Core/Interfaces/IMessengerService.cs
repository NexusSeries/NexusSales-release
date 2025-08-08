namespace NexusSales.Core.Interfaces
{
    public interface IMessengerService
    {
        void SendMessage(string userId, string message);
    }
}
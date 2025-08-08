using System;

namespace NexusSales.Core.Models
{
    public class NotificationItem
    {
        public int Id { get; set; }
        public int? NotificationId { get; set; }         // For private notification bookmarks
        public int? PublicNotificationId { get; set; }   // For public notification bookmarks
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; }

        // Default constructor
        public NotificationItem() { }

        // Optionally, you can add constructors for convenience if needed
        public NotificationItem(
            int id,
            int? notificationId,
            int? publicNotificationId,
            string title,
            string message,
            DateTime timestamp,
            bool isRead,
            string type)
        {
            Id = id;
            NotificationId = notificationId;
            PublicNotificationId = publicNotificationId;
            Title = title;
            Message = message;
            Timestamp = timestamp;
            IsRead = isRead;
            Type = type;
        }
    }
}
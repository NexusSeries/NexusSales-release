using NexusSales.Core.Models;
using System;

namespace NexusSales.UI.ViewModels
{
    public class NotificationItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; }
        public bool IsPublic => string.Equals(Type, "public", StringComparison.OrdinalIgnoreCase);
        public int? PublicNotificationId { get; set; } // Only set for public notifications
        public int? PrivateNotificationId { get; set; } // Only set for private notifications
        public bool IsRemoving { get; set; }

        // Constructor to create from model
        public NotificationItemViewModel(NotificationItem model)
        {
            if (model != null)
            {
                Id = model.Id;
                Title = model.Title;
                Message = model.Message;
                Timestamp = model.Timestamp;
                IsRead = model.IsRead;
                Type = model.Type;
            }
        }

        // Default constructor
        public NotificationItemViewModel() { }
    }
}
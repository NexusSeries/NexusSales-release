using System;

namespace NexusSales.Common.Models
{
    public class NotificationItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; }
    }
}
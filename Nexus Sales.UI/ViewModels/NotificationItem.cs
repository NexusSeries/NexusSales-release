using System;
using System.ComponentModel;

namespace NexusSales.UI.ViewModels
{
    public class NotificationItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int? NotificationId { get; set; }
        public int? PublicNotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; }

        private bool _isRemoving;
        public bool IsRemoving
        {
            get => _isRemoving;
            set
            {
                if (_isRemoving != value)
                {
                    _isRemoving = value;
                    OnPropertyChanged(nameof(IsRemoving));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        // Default constructor
        public NotificationItem() { }

        // Constructor to create from Core model
        public NotificationItem(NexusSales.Core.Models.NotificationItem model)
        {
            if (model != null)
            {
                Id = model.Id;
                // Map Id to NotificationId or PublicNotificationId based on Type
                if (string.Equals(model.Type, "Public", StringComparison.OrdinalIgnoreCase))
                {
                    PublicNotificationId = model.Id;
                    NotificationId = null;
                }
                else
                {
                    NotificationId = model.Id;
                    PublicNotificationId = null;
                }
                Title = model.Title;
                Message = model.Message;
                Timestamp = model.Timestamp;
                IsRead = model.IsRead;
                Type = model.Type;
            }
        }

        // Constructor to create from repository with IDs
        public NotificationItem(
            int? notificationId,
            int? publicNotificationId,
            string title,
            string message,
            DateTime timestamp,
            string type)
        {
            NotificationId = notificationId;
            PublicNotificationId = publicNotificationId;
            Title = title;
            Message = message;
            Timestamp = timestamp;
            Type = type;
            IsRead = false;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
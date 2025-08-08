using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusSales.Core.Models;

namespace NexusSales.Services
{
    public interface INotificationService
    {
        Task<List<NotificationItem>> GetNotificationsAsync(int count = 10);
        Task<List<NotificationItem>> GetAllNotificationsAsync();
        Task<int> GetUnreadCountAsync();
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync();
        void StartMonitoring(Action callback);
    }
}
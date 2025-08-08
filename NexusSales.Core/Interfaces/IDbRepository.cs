using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusSales.Core.Models;

namespace NexusSales.Core.Interfaces
{
    public interface IDbRepository
    {
        Task<List<NotificationItem>> GetUpdatesAsync(DateTime since);
        Task<List<NotificationItem>> GetNotificationsAsync();
        Task MarkNotificationAsReadAsync(int notificationId);
        Task RemoveNotificationAsync(int notificationId);
        Task<List<NotificationItem>> GetUnseenPublicNotificationsAsync(string laptopSerial);
        Task MarkPublicNotificationSeenAsync(int publicNotificationId, string laptopSerial);
        Task<int> GetUnseenPublicNotificationsCountAsync(string laptopSerial);

        // Add these for bookmarks:
        Task<List<NotificationItem>> GetBookmarksAsync(string userEmail);
        Task AddBookmarkAsync(string userEmail, int notificationId);
        Task RemoveBookmarkAsync(string userEmail, int notificationId);

        // Add this for user-specific bookmarks
        Task<List<NotificationItem>> GetUserBookmarkedNotificationsAsync(string userEmail);
    }
}
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

        // Add these for bookmarks support:
        Task<List<NotificationItem>> GetBookmarksAsync(string userEmail);
        Task RemoveBookmarkAsync(string userEmail, int notificationId);
        Task<List<NotificationItem>> GetUserBookmarkedNotificationsAsync(string userEmail);
        Task AddUserBookmarkedNotificationAsync(string userEmail, string title, string message, string type, int notificationId);
        Task AddBookmarkAsync(string userEmail, int? notificationId, int? publicNotificationId);
        Task AddUserBookmarkedNotificationAsync(string userEmail, string title, string message, string type, int? notificationId, int? publicNotificationId);
        Task RemoveBookmarkAsync(string userEmail, int? notificationId, int? publicNotificationId);

        Task<int> AddNotificationAndReturnIdAsync(string title, string message, string type);
        Task<List<NotificationItem>> GetNotificationsForUserAsync(string userEmail);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NexusSales.Core.Interfaces;
using NexusSales.Core.Models;
using Npgsql;

namespace NexusSales.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDbRepository _repository;
        private System.Timers.Timer _notificationTimer;
        private DateTime _lastChecked = DateTime.UtcNow;
        private Action _notificationCallback;
        private SynchronizationContext _synchronizationContext;
        private readonly string _connectionString;

        public NotificationService(IDbRepository repository, string connectionString)
        {
            _repository = repository;
            _synchronizationContext = SynchronizationContext.Current;
            _connectionString = connectionString;
        }

        public async Task<List<NotificationItem>> GetNotificationsAsync(int count = 10)
        {
            var allNotifications = await _repository.GetNotificationsAsync();
            return allNotifications.Take(count).ToList();
        }

        public async Task<List<NotificationItem>> GetAllNotificationsAsync()
        {
            return await _repository.GetNotificationsAsync();
        }

        public async Task<List<NotificationItem>> GetAllNotificationsIncludingPublicAsync(string laptopSerial)
        {
            var privateNotifications = await _repository.GetNotificationsAsync();
            var publicNotifications = await _repository.GetUnseenPublicNotificationsAsync(laptopSerial);
            return privateNotifications.Concat(publicNotifications).OrderByDescending(n => n.Timestamp).ToList();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var notifications = await _repository.GetNotificationsAsync();
            return notifications.Count(n => !n.IsRead);
        }

        public async Task<int> GetTotalUnreadCountAsync(string laptopSerial)
        {
            var privateUnread = await GetUnreadCountAsync();
            var publicUnread = await _repository.GetUnseenPublicNotificationsCountAsync(laptopSerial);
            return privateUnread + publicUnread;
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await _repository.MarkNotificationAsReadAsync(notificationId);
        }

        public async Task MarkAllAsReadAsync()
        {
            var notifications = await _repository.GetNotificationsAsync();
            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                await _repository.MarkNotificationAsReadAsync(notification.Id);
            }
        }

        public async Task RemoveNotificationAsync(int notificationId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                // Remove the notification itself
                using (var cmd = new NpgsqlCommand("DELETE FROM notifications WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", notificationId);
                    await cmd.ExecuteNonQueryAsync();
                }
                // Do NOT delete bookmarks or user_bookmarked_notifications here!
                // Bookmarks should only be removed when the user explicitly unpins.
            }
        }

        public async Task MarkPublicNotificationSeenAsync(int publicNotificationId, string laptopSerial)
        {
            await _repository.MarkPublicNotificationSeenAsync(publicNotificationId, laptopSerial);
        }

        public void StartMonitoring(Action callback)
        {
            _notificationCallback = callback;
            _notificationTimer = new System.Timers.Timer(30000); // Check every 30 seconds
            _notificationTimer.Elapsed += async (s, e) => await CheckForNewNotificationsAsync();
            _notificationTimer.Start();
        }

        private async Task CheckForNewNotificationsAsync()
        {
            try
            {
                // Always check for new notifications (private and public)
                var newNotifications = await _repository.GetUpdatesAsync(_lastChecked);
                _lastChecked = DateTime.UtcNow;

                // Always trigger the callback to reload notifications
                if (_notificationCallback != null && _synchronizationContext != null)
                {
                    _synchronizationContext.Post(_ => _notificationCallback(), null);
                }
            }
            catch (Exception ex)
            {
                // Use MessageDialog for error reporting in UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Console.WriteLine($"Error checking for notifications: {ex.Message}");
                });
            }
        }
    }
}
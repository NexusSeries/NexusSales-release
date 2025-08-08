using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql; // Add this for NpgsqlConnection and NpgsqlCommand
using NexusSales.Core.Models; // Add this for NotificationItem

namespace NexusSales.Data
{
    internal class NotificationsRepository
    {
        private readonly string _connectionString;

        public NotificationsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<NotificationItem>> GetNotificationsForUserAsync(string userEmail)
        {
            var notifications = new List<NotificationItem>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, title, message, timestamp, is_read, type FROM notifications WHERE user_email = @userEmail ORDER BY timestamp DESC", conn))
                {
                    cmd.Parameters.AddWithValue("userEmail", userEmail);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notifications.Add(new NotificationItem
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Message = reader.GetString(2),
                                Timestamp = reader.GetDateTime(3),
                                IsRead = reader.GetBoolean(4),
                                Type = reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return notifications;
        }
    }
}

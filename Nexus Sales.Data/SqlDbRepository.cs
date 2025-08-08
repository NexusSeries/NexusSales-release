using NexusSales.Core.Interfaces;
using NexusSales.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NexusSales.Core.Models;

namespace NexusSales.Data
{
    public class SqlDbRepository : IDbRepository
    {
        private readonly NexusSalesDbContext _context;
        private readonly string _connectionString;

        public SqlDbRepository(NexusSalesDbContext context, string connectionString)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<List<NotificationItem>> GetUpdatesAsync(DateTime since)
        {
            var notifications = new List<NotificationItem>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, title, message, timestamp, is_read, type FROM notifications WHERE timestamp > @since AND is_read = FALSE", conn))
                {
                    cmd.Parameters.AddWithValue("since", since);
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
                                Type = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return notifications;
        }

        public async Task<List<NotificationItem>> GetNotificationsAsync()
        {
            var notifications = new List<NotificationItem>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT id, title, message, timestamp, is_read, type FROM notifications ORDER BY timestamp DESC", conn))
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
                            Type = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }
            }
            return notifications;
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
                                Type = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return notifications;
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("UPDATE notifications SET is_read = TRUE WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", notificationId);
                    await cmd.ExecuteNonQueryAsync();
                }
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
                // Remove any bookmarks for this notification
                using (var cmd = new NpgsqlCommand("DELETE FROM notification_bookmarks WHERE notification_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", notificationId);
                    await cmd.ExecuteNonQueryAsync();
                }
                // Remove from user_bookmarked_notifications as well
                using (var cmd = new NpgsqlCommand("DELETE FROM user_bookmarked_notifications WHERE notification_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", notificationId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task MarkPublicNotificationSeenAsync(int publicNotificationId, string laptopSerial)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO publicnotificationseen (publicnotificationid, laptopserial, seenat)
                      VALUES (@publicNotificationId, @laptopSerial, NOW())
                      ON CONFLICT DO NOTHING", conn))
                {
                    cmd.Parameters.AddWithValue("publicNotificationId", publicNotificationId);
                    cmd.Parameters.AddWithValue("laptopSerial", laptopSerial);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<NotificationItem>> GetUnseenPublicNotificationsAsync(string laptopSerial)
        {
            var sql = @"
                SELECT pn.id, pn.title, pn.message, pn.timestamp, false as is_read, pn.type
                FROM publicnotifications pn
                LEFT JOIN publicnotificationseen pns ON pn.id = pns.publicnotificationid AND pns.laptopserial = @laptopSerial
                WHERE pns.id IS NULL
                ORDER BY pn.timestamp DESC";
            var notifications = new List<NotificationItem>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("laptopSerial", laptopSerial);
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
                                IsRead = false,
                                Type = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return notifications;
        }

        public async Task<int> GetUnseenPublicNotificationsCountAsync(string laptopSerial)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM publicnotifications pn
                LEFT JOIN publicnotificationseen pns ON pn.id = pns.publicnotificationid AND pns.laptopserial = @laptopSerial
                WHERE pns.id IS NULL";
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("laptopSerial", laptopSerial);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task AddPublicNotificationAsync(string title, string message, string type)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO publicnotifications (title, message, timestamp, type)
                      VALUES (@title, @message, NOW(), @type)", conn))
                {
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("message", message);
                    cmd.Parameters.AddWithValue("type", type);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<bool> BookmarkExistsAsync(string userEmail, int? notificationId, int? publicNotificationId)
        {
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new Npgsql.NpgsqlCommand(
                    @"SELECT 1 FROM notification_bookmarks 
                      WHERE user_email = @userEmail 
                        AND (
                            (@notificationId IS NOT NULL AND notification_id = @notificationId::integer) 
                            OR 
                            (@publicNotificationId IS NOT NULL AND public_notification_id = @publicNotificationId::integer)
                        )
                      LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("@userEmail", userEmail);
                cmd.Parameters.AddWithValue("@notificationId", (object)notificationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@publicNotificationId", (object)publicNotificationId ?? DBNull.Value);
                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
        }

        public async Task AddBookmarkAsync(string userEmail, int? notificationId, int? publicNotificationId)
        {
            // Guard: Only one of notificationId or publicNotificationId should be set
            if ((notificationId == null && publicNotificationId == null) ||
                (notificationId != null && publicNotificationId != null))
            {
                throw new ArgumentException("Exactly one of notificationId or publicNotificationId must be set.");
            }

            // Check for existing bookmark
            

            string notificationType = notificationId == null ? "Public" : "Private";

            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new Npgsql.NpgsqlCommand(
                    @"INSERT INTO notification_bookmarks 
                        (user_email, notification_id, public_notification_id, notification_type, created_at)
                      VALUES (@userEmail, @notificationId, @publicNotificationId, @notificationType, NOW())
                      ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("@userEmail", userEmail);
                cmd.Parameters.AddWithValue("@notificationId", (object)notificationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@publicNotificationId", (object)publicNotificationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notificationType", notificationType);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task RemoveBookmarkAsync(string userEmail, int notificationId)
        {
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Remove from notification_bookmarks (private)
                var cmd1 = new NpgsqlCommand(
                    "DELETE FROM notification_bookmarks WHERE user_email = @userEmail AND notification_id = @notificationId;", conn);
                cmd1.Parameters.AddWithValue("@userEmail", userEmail);
                cmd1.Parameters.AddWithValue("@notificationId", notificationId);
                await cmd1.ExecuteNonQueryAsync();

                // Remove from user_bookmarked_notifications (private)
                var cmd2 = new NpgsqlCommand(
                    "DELETE FROM user_bookmarked_notifications WHERE user_email = @userEmail AND notification_id = @notificationId;", conn);
                cmd2.Parameters.AddWithValue("@userEmail", userEmail);
                cmd2.Parameters.AddWithValue("@notificationId", notificationId);
                await cmd2.ExecuteNonQueryAsync();
            }
        }

        public async Task RemoveBookmarkAsync(string userEmail, int? notificationId, int? publicNotificationId)
        {
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                if (notificationId.HasValue)
                {
                    // Remove private bookmark
                    var cmd1 = new NpgsqlCommand(
                        "DELETE FROM notification_bookmarks WHERE user_email = @userEmail AND notification_id = @notificationId;", conn);
                    cmd1.Parameters.AddWithValue("@userEmail", userEmail);
                    cmd1.Parameters.AddWithValue("@notificationId", notificationId.Value);
                    await cmd1.ExecuteNonQueryAsync();

                    var cmd2 = new NpgsqlCommand(
                        "DELETE FROM user_bookmarked_notifications WHERE user_email = @userEmail AND notification_id = @notificationId;", conn);
                    cmd2.Parameters.AddWithValue("@userEmail", userEmail);
                    cmd2.Parameters.AddWithValue("@notificationId", notificationId.Value);
                    await cmd2.ExecuteNonQueryAsync();
                }
                else if (publicNotificationId.HasValue)
                {
                    // Remove public bookmark
                    var cmd1 = new NpgsqlCommand(
                        "DELETE FROM notification_bookmarks WHERE user_email = @userEmail AND public_notification_id = @publicNotificationId;", conn);
                    cmd1.Parameters.AddWithValue("@userEmail", userEmail);
                    cmd1.Parameters.AddWithValue("@publicNotificationId", publicNotificationId.Value);
                    await cmd1.ExecuteNonQueryAsync();

                    var cmd2 = new NpgsqlCommand(
                        "DELETE FROM user_bookmarked_notifications WHERE user_email = @userEmail AND public_notification_id = @publicNotificationId;", conn);
                    cmd2.Parameters.AddWithValue("@userEmail", userEmail);
                    cmd2.Parameters.AddWithValue("@publicNotificationId", publicNotificationId.Value);
                    await cmd2.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<NotificationItem>> GetBookmarksAsync(string userEmail)
        {
            var bookmarks = new List<NotificationItem>();
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new Npgsql.NpgsqlCommand(
                    @"SELECT n.id, n.title, n.message, n.timestamp, n.is_read, n.type
                        FROM notifications n
                        INNER JOIN notification_bookmarks b ON n.id = b.notification_id
                        WHERE b.user_email = @userEmail
                     UNION
                      SELECT pn.id, pn.title, pn.message, pn.timestamp, FALSE as is_read, pn.type
                        FROM publicnotifications pn
                        INNER JOIN notification_bookmarks b ON pn.id = b.public_notification_id
                        WHERE b.user_email = @userEmail
                     ORDER BY timestamp DESC;", conn);
                cmd.Parameters.AddWithValue("userEmail", userEmail);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var type = reader.IsDBNull(5) ? null : reader.GetString(5);
                        var isPublic = string.Equals(type, "public", StringComparison.OrdinalIgnoreCase);

                        bookmarks.Add(new NotificationItem
                        {
                            NotificationId = isPublic ? (int?)null : (reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0)),
                            PublicNotificationId = isPublic ? (reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1)) : (int?)null,
                            Id = isPublic
                                ? (reader.IsDBNull(1) ? 0 : reader.GetInt32(1))
                                : (reader.IsDBNull(0) ? 0 : reader.GetInt32(0)),
                            Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Message = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Timestamp = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                            Type = type,
                            IsRead = false
                        });
                    }
                }
            }
            return bookmarks;
        }

        public async Task<List<NotificationItem>> GetUserBookmarkedNotificationsAsync(string userEmail)
        {
            var bookmarks = new List<NotificationItem>();
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new Npgsql.NpgsqlCommand(
                    @"SELECT 
                        notification_id, 
                        public_notification_id, 
                        title, 
                        message, 
                        created_at, 
                        notification_type
                      FROM user_bookmarked_notifications
                      WHERE user_email = @userEmail
                      ORDER BY created_at DESC;", conn);
                cmd.Parameters.AddWithValue("@userEmail", userEmail);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var type = reader.IsDBNull(5) ? null : reader.GetString(5);
                        var isPublic = string.Equals(type, "public", StringComparison.OrdinalIgnoreCase);

                        bookmarks.Add(new NotificationItem
                        {
                            NotificationId = isPublic ? (int?)null : (reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0)),
                            PublicNotificationId = isPublic ? (reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1)) : (int?)null,
                            Id = isPublic
                                ? (reader.IsDBNull(1) ? 0 : reader.GetInt32(1))
                                : (reader.IsDBNull(0) ? 0 : reader.GetInt32(0)),
                            Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Message = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Timestamp = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                            Type = type,
                            IsRead = false
                        });
                    }
                }
            }
            return bookmarks;
        }

        public async Task AddNotificationAndBookmarkAsync(string title, string message, string userEmail, int newNotificationId)
        {
            // Insert notification (if needed)
            await AddNotificationAsync(title, message, null);
            // Then bookmark it
            await AddBookmarkAsync(userEmail, newNotificationId, null);
        }

        private async Task AddNotificationAsync(string title, string message, string type)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    @"INSERT INTO notifications (title, message, timestamp, type)
                      VALUES (@title, @message, NOW(), @type)", conn))
                {
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("message", message);
                    cmd.Parameters.AddWithValue("type", type);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> AddNotificationAndReturnIdAsync(string title, string message, string type)
        {
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new Npgsql.NpgsqlCommand(
                    @"INSERT INTO notifications (title, message, timestamp, type)
                      VALUES (@title, @message, NOW(), @type)
                      RETURNING id;", conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@message", message);
                    cmd.Parameters.AddWithValue("@type", (object)type ?? DBNull.Value);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task AddUserBookmarkedNotificationAsync(string userEmail, string title, string message, string type, int notificationId)
        {
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new Npgsql.NpgsqlCommand(
                    @"INSERT INTO user_bookmarked_notifications (user_email, title, message, created_at, notification_type, notification_id)
                      VALUES (@userEmail, @title, @message, NOW(), @type, @notificationId)
                      ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("@userEmail", userEmail);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@type", (object)type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notificationId", notificationId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task AddUserBookmarkedNotificationAsync(
            string userEmail, string title, string message, string type, int? notificationId, int? publicNotificationId)
        {
            using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new Npgsql.NpgsqlCommand(
                    @"INSERT INTO user_bookmarked_notifications 
                        (user_email, title, message, created_at, notification_type, notification_id, public_notification_id)
                      VALUES (@userEmail, @title, @message, NOW(), @type, @notificationId, @publicNotificationId)
                      ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("@userEmail", userEmail);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@type", (object)type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notificationId", (object)notificationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@publicNotificationId", (object)publicNotificationId ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task RemoveUserBookmarkedNotificationAsync(string userEmail, string title, string message)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    @"DELETE FROM user_bookmarked_notifications
                      WHERE user_email = @userEmail AND title = @title AND message = @message;", conn))
                {
                    cmd.Parameters.AddWithValue("userEmail", userEmail);
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("message", message);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task AddBookmarkBasedOnNotificationTypeAsync(NotificationItem notification, string userEmail)
        {
            if (notification.Type != null && notification.Type.Equals("public", StringComparison.OrdinalIgnoreCase))
            {
                await AddBookmarkAsync(userEmail, null, notification.Id); // public notification
            }
            else
            {
                await AddBookmarkAsync(userEmail, notification.Id, null); // private notification
            }
        }

        public async Task AddBookmarkBasedOnItemTypeAsync(NotificationItem item, string userEmail)
        {
            if (item.Type != null && item.Type.Equals("public", StringComparison.OrdinalIgnoreCase))
            {
                // For public notifications, pass as publicNotificationId
                Console.WriteLine($"Adding bookmark for public notification $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$: {item.Id}");
                await AddBookmarkAsync(userEmail, null, item.Id);
            }
            else
            {
                // For private notifications, pass as notificationId
                await AddBookmarkAsync(userEmail, item.Id, null);
            }
        }

        private async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task RemoveBookmarkBasedOnItemAsync(NotificationItem item, string userEmail)
        {
            if (item.Type != null && item.Type.Equals("public", StringComparison.OrdinalIgnoreCase))
            {
                await RemoveBookmarkAsync(userEmail, item.PublicNotificationId ?? item.Id);
            }
            else
            {
                await RemoveBookmarkAsync(userEmail, item.NotificationId ?? item.Id);
            }
        }
    }
}
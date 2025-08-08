using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Input;
using NexusSales.Core.Models;
using NexusSales.Services;
using NexusSales.UI.Commands;
using CommandsRC = NexusSales.UI.Commands.RelayCommand;
using System.Collections.Generic;
using System.Linq;
using NexusSales.Core.Interfaces;
using System.Collections.Specialized;

namespace NexusSales.UI.ViewModels
{
    public class NotificationsViewModel : INotifyPropertyChanged
    {
        private readonly NotificationService _notificationService;
        private readonly IDbRepository _repository;
        private readonly string _userEmail;
        private int _unreadCount = 0;
        private ObservableCollection<NotificationItem> _notifications;
        private bool _isLoading;
        private bool _hasNewNotifications;
        private List<int> _lastNotificationIds = new List<int>();

        public event PropertyChangedEventHandler PropertyChanged;

        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                if (_unreadCount != value)
                {
                    _unreadCount = value;
                    OnPropertyChanged(nameof(UnreadCount));
                }
            }
        }

        public ObservableCollection<NotificationItem> Notifications
        {
            get => _notifications;
            set
            {
                if (_notifications != null)
                    _notifications.CollectionChanged -= Notifications_CollectionChanged;

                _notifications = value;
                OnPropertyChanged(nameof(Notifications));
                OnPropertyChanged(nameof(HasNotifications));

                if (_notifications != null)
                    _notifications.CollectionChanged += Notifications_CollectionChanged;
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public bool HasNewNotifications
        {
            get => _hasNewNotifications;
            set
            {
                if (_hasNewNotifications != value)
                {
                    _hasNewNotifications = value;
                    OnPropertyChanged(nameof(HasNewNotifications));
                }
            }
        }

        public bool HasNotifications
        {
            get => Notifications != null && Notifications.Count > 0;
        }

        public ICommand MarkAsReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }
        public ICommand CollapseNotificationCommand { get; }
        public ICommand PinNotificationCommand { get; }
        public ICommand UnpinNotificationCommand { get; }

        public ObservableCollection<NotificationItem> Bookmarks { get; } = new ObservableCollection<NotificationItem>();
        public ObservableCollection<NotificationItem> UserBookmarks { get; } = new ObservableCollection<NotificationItem>();

        // Parameterless constructor for design-time support
        public NotificationsViewModel() { }

        public NotificationsViewModel(NotificationService notificationService, IDbRepository repository, string userEmail)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userEmail = userEmail ?? throw new ArgumentNullException(nameof(userEmail));
            _notifications = new ObservableCollection<NotificationItem>();

            MarkAsReadCommand = new CommandsRC(async param => await MarkAsRead((int)param));
            MarkAllAsReadCommand = new CommandsRC(async param => await MarkAllAsRead());
            CollapseNotificationCommand = new CommandsRC(async param => await CollapseNotification(param as NotificationItem));
            PinNotificationCommand = new RelayCommand(async param => await PinNotificationAsync(param as NotificationItem));
            UnpinNotificationCommand = new RelayCommand(async param => await UnpinNotificationAsync(param as NotificationItem));

            // Initial load (do not await here)
            LoadNotificationsAsync().ConfigureAwait(false);

            // Start monitoring for new notifications
            _notificationService.StartMonitoring(async () =>
            {
                string laptopSerial = GetLaptopSerial();
                var notificationModels = await _notificationService.GetAllNotificationsIncludingPublicAsync(laptopSerial);
                var newIds = notificationModels.Select(n => n.Id).ToList();

                if (_lastNotificationIds.Count > 0 && newIds.Except(_lastNotificationIds).Any())
                {
                    HasNewNotifications = true;
                }

                _lastNotificationIds = newIds;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadNotificationsAsync();
                });
            });

            _notifications.CollectionChanged += Notifications_CollectionChanged;
            // Instead, call InitializeAsync() after construction
        }

        public async Task InitializeAsync()
        {
            await LoadUserBookmarksAsync();
        }

        // After loading notifications, update unread count
        public async Task LoadNotificationsAsync()
        {
            if (string.IsNullOrEmpty(_userEmail)) return;
            string laptopSerial = GetLaptopSerial();
            var privateNotifications = await _repository.GetNotificationsForUserAsync(_userEmail);
            var publicNotifications = await _repository.GetUnseenPublicNotificationsAsync(laptopSerial);
            var allNotifications = privateNotifications.Concat(publicNotifications)
                .OrderByDescending(n => n.Timestamp)
                .Select(n => new NotificationItem(n));
            Notifications = new ObservableCollection<NotificationItem>(allNotifications);
            UnreadCount = Notifications.Count(n => !n.IsRead);
        }

        private async Task MarkAsRead(int notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId);
            await LoadNotificationsAsync();
        }

        private async Task MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync();
            await LoadNotificationsAsync();
        }

        private async Task CollapseNotification(NotificationItem notification)
        {
            if (notification == null) return;

            if (notification.Type == "Public")
            {
                string laptopSerial = GetLaptopSerial();
                await _notificationService.MarkPublicNotificationSeenAsync(notification.Id, laptopSerial);
            }
            else
            {
                await _notificationService.RemoveNotificationAsync(notification.Id);
            }

            // Always reload notifications after any change
            await LoadNotificationsAsync();
        }

        private async Task RefreshUnreadCountAsync()
        {
            string laptopSerial = GetLaptopSerial();
            UnreadCount = await _notificationService.GetTotalUnreadCountAsync(laptopSerial);
        }

        private async void RefreshUnreadCount()
        {
            UnreadCount = await _notificationService.GetUnreadCountAsync();
        }

        private string GetLaptopSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (ManagementObject wmi in searcher.Get().Cast<ManagementObject>()) // Explicit cast for IDE0220
                    {
                        return wmi["SerialNumber"]?.ToString() ?? "UnknownSerial";
                    }
                }
            }
            catch
            {
                // Fallback if WMI fails
                return Environment.MachineName;
            }
            return "UnknownSerial";
        }

        public void ClearNewNotificationIndicator()
        {
            HasNewNotifications = false;
        }

        private void Notifications_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasNotifications));
        }

        public async Task LoadBookmarksAsync()
        {
            var bookmarks = await _repository.GetBookmarksAsync(_userEmail);
            Bookmarks.Clear();
            UserBookmarks.Clear();
            foreach (var item in bookmarks)
            {
                var isPublic = item.Type?.Equals("public", StringComparison.OrdinalIgnoreCase) == true;
                var uiItem = new NotificationItem(item)
                {
                    IsRead = false,
                    NotificationId = isPublic ? (int?)null : (item.NotificationId ?? item.Id),
                    PublicNotificationId = isPublic ? (item.PublicNotificationId ?? item.Id) : (int?)null
                };
                Bookmarks.Add(uiItem);
                UserBookmarks.Add(uiItem);
            }
        }

        public async Task LoadUserBookmarksAsync()
        {
            var bookmarks = await _repository.GetUserBookmarkedNotificationsAsync(_userEmail);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UserBookmarks.Clear();
                foreach (var item in bookmarks)
                {
                    var isPublic = item.Type?.Equals("public", StringComparison.OrdinalIgnoreCase) == true;
                    var uiItem = new NotificationItem(item)
                    {
                        IsRead = false,
                        NotificationId = isPublic ? (int?)null : (item.NotificationId ?? item.Id),
                        PublicNotificationId = isPublic ? (item.PublicNotificationId ?? item.Id) : (int?)null
                    };
                    UserBookmarks.Add(uiItem);
                }
            });
        }

        public async Task AddBookmarkAsync(string userEmail, int? notificationId, int? publicNotificationId)
        {
            // Always pass null for the unused ID
            await _repository.AddBookmarkAsync(
                userEmail,
                notificationId.HasValue ? notificationId : null,
                publicNotificationId.HasValue ? publicNotificationId : null
            );
        }

        private async Task PinNotificationAsync(NotificationItem item)
        {
            if (item == null) return;

            if (item.Type == "Public")
            {
                // Pin public notification: notificationId = null, publicNotificationId = item.Id
                await _repository.AddBookmarkAsync(_userEmail, null, item.Id);
                await _repository.AddUserBookmarkedNotificationAsync(
                    _userEmail,
                    item.Title,
                    item.Message,
                    item.Type,
                    null, // notificationId
                    item.Id // publicNotificationId
                );
                // Debug print for public notification
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Pinned PUBLIC notification. User: {_userEmail}, PublicNotificationId: {item.Id}");
            }
            else
            {
                // Pin private notification: notificationId = item.Id, publicNotificationId = null
                await _repository.AddBookmarkAsync(_userEmail, item.Id, null);
                await _repository.AddUserBookmarkedNotificationAsync(
                    _userEmail,
                    item.Title,
                    item.Message,
                    item.Type,
                    item.Id,
                    null // publicNotificationId
                );
                // Debug print for private notification
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Pinned PRIVATE notification. User: {_userEmail}, NotificationId: {item.Id}");
            }
            await LoadUserBookmarksAsync();
        }

        private async Task UnpinNotificationAsync(NotificationItem item)
        {
            if (item == null) return;
            item.IsRemoving = true;
            await Task.Delay(300);

            await _repository.RemoveBookmarkAsync(_userEmail, item.NotificationId, item.PublicNotificationId);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UserBookmarks.Remove(item);
                Bookmarks.Remove(item);
            });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
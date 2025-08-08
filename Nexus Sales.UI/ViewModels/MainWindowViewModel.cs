using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NexusSales.Core.Interfaces;
using NexusSales.Core.Models;
using NexusSales.Services;
using NexusSales.UI.Commands;


namespace NexusSales.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IMessengerService _messengerService;
        private readonly NotificationService _notificationService;
        private readonly IDbRepository _repository; // Added field
        private readonly string _userEmail;         // Added field
        private bool _hasNotifications;

        public MainWindowViewModel(IMessengerService messengerService, IDbRepository repository, string userEmail, string connectionString)
        {
            _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository)); // Assign repository
            _userEmail = userEmail ?? throw new ArgumentNullException(nameof(userEmail));   // Assign userEmail
            _notificationService = new NotificationService(_repository, connectionString);

            // Initialize commands
            ViewNotificationsCommand = new RelayCommand(param => ViewNotifications());
            MarkNotificationAsReadCommand = new RelayCommand(param =>
            {
                if (param is int notificationId)
                    MarkAsRead(notificationId);
            });

            // Set up notification monitoring
            _notificationService.StartMonitoring(() =>
            {
                HasNotifications = true;
                RefreshNotifications();
            });
        }

        public ObservableCollection<NotificationItem> Notifications { get; } = new ObservableCollection<NotificationItem>();
        public ObservableCollection<NotificationItem> Bookmarks { get; } = new ObservableCollection<NotificationItem>();
        public ObservableCollection<NotificationItem> UserBookmarks { get; } = new ObservableCollection<NotificationItem>();

        public NotificationsViewModel NotificationsViewModel { get; }

        public bool HasNotifications
        {
            get => _hasNotifications;
            set
            {
                if (_hasNotifications != value)
                {
                    _hasNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CloseCommand { get; set; }
        public ICommand MinMaxCommand { get; set; }
        public ICommand HideCommand { get; set; }
        public ICommand NavigateCommand { get; set; }
        public ICommand ShutdownCommand { get; set; }
        public ICommand ViewNotificationsCommand { get; set; }
        public ICommand MarkNotificationAsReadCommand { get; set; }

        private string _windowStateText;
        public string WindowStateText
        {
            get => _windowStateText;
            set
            {
                if (_windowStateText != value)
                {
                    _windowStateText = value;
                    OnPropertyChanged();
                }
            }
        }

        private void ViewNotifications()
        {
            // Show notifications dialog or navigate to notifications page
            // This would be implemented based on your UI design
            RefreshNotifications();
        }

        public async void RefreshNotifications()
        {
            var notificationModels = await _notificationService.GetAllNotificationsAsync();

            Notifications.Clear();
            foreach (var model in notificationModels)
            {
                // Use the constructor instead of object initializer
                var notificationViewModel = new NotificationItem(model);
                Notifications.Add(notificationViewModel);
            }

            HasNotifications = Notifications.Any(n => !n.IsRead);
        }

        public async void MarkAsRead(int notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId);
            RefreshNotifications();
        }

        public async Task PinNotification(NotificationItem item)
        {
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
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
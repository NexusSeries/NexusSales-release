using NexusSales.Core.Interfaces; // <-- Add this line
using NexusSales.Data;
using NexusSales.FrontEnd.Buttons;
using NexusSales.Services;
using NexusSales.UI.Dialogs;
using NexusSales.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NexusSales.FrontEnd.Pages;
using System.Windows.Media;
using CommandsRC = NexusSales.UI.Commands.RelayCommand;

namespace NexusSales.UI
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, PanelButton> _navigationButtons;
        private string _currentPage = "Home";
        private NotificationsViewModel _notificationsViewModel;
        private readonly string _userEmail;

        public MainWindow(NotificationService notificationService, IDbRepository repository, string userEmail)
        {
            InitializeComponent();

            _userEmail = userEmail ?? throw new ArgumentNullException(nameof(userEmail));

            // Enforce updater presence
            string nexusDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Nexus");
            string updaterPath = Path.Combine(nexusDir, "NexusUpdate.exe");
            if (!File.Exists(updaterPath))
            {
                // Try self-repair: download updater from configured URL
                string updaterUrl = ConfigurationManager.AppSettings["UpdaterDownloadUrl"];
                bool repairSuccess = false;
                try
                {
                    Directory.CreateDirectory(nexusDir);
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(updaterUrl, updaterPath);
                        repairSuccess = File.Exists(updaterPath);
                    }
                }
                catch
                {
                    repairSuccess = false;
                }

                if (!repairSuccess)
                {
                    var dialog = new MessageDialog(
                        "Critical update component missing and could not be restored automatically. Please restore NexusUpdate.exe to continue.",
                        "Update Required",
                        soundFileName: "Warning.wav",
                        titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                    );
                    dialog.ShowDialog();
                    Application.Current.Shutdown();
                    return;
                }
                else
                {
                    var dialog = new MessageDialog(
                        "Critical update component was missing but has been restored automatically. Please restart the application.",
                        "Update Restored",
                        soundFileName: "Success.wav",
                        titleColor: (Brush)Application.Current.FindResource("FontSuccessfulBrush")
                    );
                    dialog.ShowDialog();
                    Application.Current.Shutdown();
                    return;
                }
            }

            // Create notifications view model with all required dependencies
            _notificationsViewModel = new NotificationsViewModel(notificationService, repository, _userEmail);

            // Set up the notification badge view model
            if (NotificationBadgeControl != null)
            {
                NotificationBadgeControl.ViewModel = _notificationsViewModel;
            }

            // Get the connection string from App.config
            var connectionString = ConfigurationManager.ConnectionStrings["NexusSalesConnection"].ConnectionString;

            // Create and set up view model with commands
            var viewModel = new MainWindowViewModel(new MessengerService(), repository, _userEmail, connectionString);

            // Add commands to view model
            viewModel.CloseCommand = new CommandsRC(param => Close());
            viewModel.MinMaxCommand = new CommandsRC(param => ToggleWindowState());
            viewModel.HideCommand = new CommandsRC(param => WindowState = WindowState.Minimized);
            viewModel.NavigateCommand = new CommandsRC(NavigateTo);
            viewModel.ShutdownCommand = new CommandsRC(param => Application.Current.Shutdown());

            // Initialize the WindowStateText property
            viewModel.WindowStateText = GetWindowStateText();

            // Set data context
            DataContext = viewModel;

            // Listen for notification changes
            if (viewModel is INotifyPropertyChanged inpc)
            {
                PropertyChangedEventHandler handler = (s, e) =>
                {
                    if (e.PropertyName == "HasNotifications")
                    {
                        // You need to ensure HasNotifications is implemented in your ViewModel
                        // or remove this section if not needed
                    }
                };
                inpc.PropertyChanged += handler;
            }

            this.Loaded += MainWindow_Loaded;
        }

        private void Move_Window(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _navigationButtons = new Dictionary<string, PanelButton>();
            if (NavScrollViewer.Content is StackPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is PanelButton panelButton && panelButton.CommandParameter is string pageName)
                    {
                        _navigationButtons[pageName] = panelButton;
                    }
                }
            }
            SetActiveButton("Home");

            // Add this line to debug notifications
            DebugNotifications();

            // Load notifications initially
            System.Threading.Tasks.Task.Run(async () =>
            {
                await _notificationsViewModel.LoadNotificationsAsync();
                await _notificationsViewModel.LoadUserBookmarksAsync(); // <-- Ensure this is called here!
            });

            // Use the existing notification service that's already working
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Remove await - Notifications is a property, not an awaitable Task
                    var notifications = _notificationsViewModel.Notifications;


                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var dialog = new MessageDialog(
                            $"Error: {ex.Message}",
                            "Error",
                            soundFileName: "Warning.wav",
                            titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                        );
                        dialog.ShowDialog();
                    });
                }
            });

            // Load bookmarks initially
            System.Threading.Tasks.Task.Run(async () =>
            {
                await _notificationsViewModel.LoadBookmarksAsync();
                await _notificationsViewModel.LoadUserBookmarksAsync();
            });

            // Verify database setup
            VerifyDatabaseSetup();

            // Set up BookmarksPanelControl
            BookmarksPanelControl.DataContext = _notificationsViewModel;
            BookmarksPanelControl.CloseRequested += BookmarksPanel_CloseRequested;

            
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            // Update the window state text in view model
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.WindowStateText = GetWindowStateText();
            }
        }

        private string GetWindowStateText()
        {
            return WindowState == WindowState.Maximized ? "Restore" : "Maximize";
        }

        private void NavigateTo(object parameter)
        {
            string pageName = parameter as string;
            if (string.IsNullOrEmpty(pageName) || _currentPage == pageName)
                return;

            _currentPage = pageName;

            // Navigation logic
            if (pageName == "Facebook")
            {
                ContentFrame.Content = new FacebookPage();
            }
            // Add other pages as needed

            SetActiveButton(pageName);
        }

        private void SetActiveButton(string pageName)
        {
            foreach (var kvp in _navigationButtons)
            {
                kvp.Value.IsActive = kvp.Key == pageName;
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Optionally show suggestions or clear placeholder
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Example: if Enter is pressed, trigger search
            if (e.Key == Key.Enter)
            {
                // Perform search logic here
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Optionally update suggestions or filter results
        }

        private void SuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle suggestion selection
        }

        private async void DebugNotifications()
        {
            try
            {
                // Get connection string from config
                var connectionString = System.Configuration.ConfigurationManager
                    .ConnectionStrings["NexusSalesConnection"].ConnectionString;

                // Create a SQL repository with proper connection string
                var dbContext = new NexusSalesDbContext();

                // Try simpler approach first
                var notificationService = new NotificationService(new SqlDbRepository(dbContext, connectionString), connectionString);
                var notifications = await notificationService.GetAllNotificationsAsync();



                if (notifications.Count > 0)
                {

                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    $"Error debugging notifications: {ex.Message}\n\nInner exception: {ex.InnerException?.Message}",
                    "Debug Error",
                    soundFileName: "Warning.wav",
                    titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                );
                dialog.ShowDialog();
            }
        }

        private async void VerifyDatabaseSetup()
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["NexusSalesConnection"].ConnectionString;


                using (var conn = new Npgsql.NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = new Npgsql.NpgsqlCommand(
                        "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'notifications')",
                        conn);
                    bool tableExists = (bool)await cmd.ExecuteScalarAsync();

                    if (!tableExists)
                    {
                        var dialogMissing = new MessageDialog(
                            "Notifications table does not exist! Creating it now...",
                            "Database Setup",
                            soundFileName: "Warning.wav",
                            titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                        );
                        dialogMissing.ShowDialog();

                        // Create the table if it doesn't exist
                        var createCmd = new Npgsql.NpgsqlCommand(@"
                            CREATE TABLE IF NOT EXISTS notifications (
                                id SERIAL PRIMARY KEY,
                                title VARCHAR(100) NOT NULL,
                                message VARCHAR(500) NOT NULL,
                                timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                is_read BOOLEAN NOT NULL DEFAULT FALSE,
                                type VARCHAR(50) NULL
                            )", conn);
                        await createCmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    var dialog = new MessageDialog(
                        $"Error: {ex.Message}",
                        "Error",
                        soundFileName: "Warning.wav",
                        titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                    );
                    dialog.ShowDialog();
                });
            }
        }

        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            BookmarksPopup.IsOpen = true;
        }

        private void BookmarksPanel_CloseRequested(object sender, RoutedEventArgs e)
        {
            BookmarksPopup.IsOpen = false;
        }
    }
}
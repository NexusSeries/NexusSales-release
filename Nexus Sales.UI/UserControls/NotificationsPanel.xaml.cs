using System.Windows;
using System.Windows.Controls;
using NexusSales.UI.ViewModels;
using Npgsql;
using System;

namespace NexusSales.UI.UserControls
{
    public partial class NotificationsPanel : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(NotificationsViewModel), typeof(NotificationsPanel),
                new PropertyMetadata(null, OnViewModelChanged));

        public NotificationsViewModel ViewModel
        {
            get => (NotificationsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public NotificationsPanel()
        {
            InitializeComponent();
        }
    
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            // Attach event handler to each NotificationCard
            if (visualAdded is NotificationCard card)
            {
                card.CollapseRequested += NotificationCard_CollapseRequested;
            }
        }

        // Called when a NotificationCard is loaded
        private void NotificationCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NotificationCard card)
            {
                card.CollapseRequested -= NotificationCard_CollapseRequested;
                card.CollapseRequested += NotificationCard_CollapseRequested;
            }
        }

        private void NotificationCard_CollapseRequested(object sender, System.EventArgs e)
        {
            if (sender is NotificationCard card && card.DataContext is NotificationItem item)
            {
                // Use the ViewModel's command to ensure DB deletion
                if (DataContext is NotificationsViewModel vm && vm.CollapseNotificationCommand.CanExecute(item))
                {
                    vm.CollapseNotificationCommand.Execute(item);
                }
            }
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NotificationsPanel panel)
            {
                panel.DataContext = e.NewValue;

                // Load notifications when view model is assigned
                if (e.NewValue is NotificationsViewModel viewModel)
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await viewModel.LoadNotificationsAsync();
                    });
                }
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && ViewModel != null)
            {
                ViewModel.ClearNewNotificationIndicator();
            }
        }

        private async System.Threading.Tasks.Task InsertNotificationAsync(string targetUserEmail, string title, string message, DateTime timestamp, bool isRead, string type)
        {
            using (var conn = new NpgsqlConnection("YourConnectionStringHere"))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO notifications (user_email, title, message, timestamp, is_read, type) VALUES (@userEmail, @title, @message, @timestamp, @isRead, @type)", conn))
                {
                    cmd.Parameters.AddWithValue("userEmail", targetUserEmail);
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("message", message);
                    cmd.Parameters.AddWithValue("timestamp", timestamp);
                    cmd.Parameters.AddWithValue("isRead", isRead);
                    cmd.Parameters.AddWithValue("type", type);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
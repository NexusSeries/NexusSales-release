using System.Windows;
using System.Windows.Controls;
using NexusSales.UI.ViewModels;
using System;

namespace NexusSales.UI.UserControls
{
    /// <summary>
    /// Interaction logic for NotificationBadge.xaml
    /// </summary>
    public partial class NotificationBadge : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(NotificationsViewModel), typeof(NotificationBadge),
                new PropertyMetadata(null, OnViewModelChanged));

        public NotificationsViewModel ViewModel
        {
            get => (NotificationsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public NotificationBadge()
        {
            InitializeComponent();
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NotificationBadge badge)
            {
                badge.DataContext = e.NewValue;
            }
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            NotificationsPopup.IsOpen = !NotificationsPopup.IsOpen;

            // Load/refresh notifications when opening the popup
            if (NotificationsPopup.IsOpen && ViewModel != null)
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await ViewModel.LoadNotificationsAsync();
                });
            }
        }

        private void NotificationsPopup_Opened(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ClearNewNotificationIndicator();
            }
        }
    }
}
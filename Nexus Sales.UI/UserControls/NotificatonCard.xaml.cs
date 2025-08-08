using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace NexusSales.UI.UserControls
{
    public partial class NotificationCard : UserControl
    {
        public NotificationCard()
        {
            InitializeComponent(); 
        }

        public event EventHandler CollapseRequested;

        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            var slideAnim = new ThicknessAnimation
            {
                To = new Thickness(500, 0, -500, 0),
                Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var fadeAnim = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };

            // When both animations complete, raise the event
            int completed = 0;
            EventHandler onComplete = (s, _) =>
            {
                completed++;
                if (completed == 2)
                    CollapseRequested?.Invoke(this, EventArgs.Empty);
            };
            slideAnim.Completed += onComplete;
            fadeAnim.Completed += onComplete;

            this.BeginAnimation(MarginProperty, slideAnim);
            this.BeginAnimation(OpacityProperty, fadeAnim);
        }
    }
}
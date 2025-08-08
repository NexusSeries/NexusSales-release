using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NexusSales.UI.ViewModels;

namespace NexusSales.UI.UserControls
{
    /// <summary>
    /// Interaction logic for BookmarksPanel.xaml
    /// </summary>
    public partial class BookmarksPanel : UserControl
    {
        public event RoutedEventHandler CloseRequested;

        public BookmarksPanel()
        {
            InitializeComponent();

            this.DataContextChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"BookmarksPanel DataContext changed: {DataContext?.GetType().Name}");
            };

            this.Loaded += (s, e) =>
            {
                var vm = DataContext as NotificationsViewModel;
                if (vm != null)
                {
                    System.Diagnostics.Debug.WriteLine($"BookmarksPanel loaded. Bookmarks count: {vm.Bookmarks.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("BookmarksPanel loaded. DataContext is not NotificationsViewModel.");
                }
            };

            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.Deactivated += (s, e) => window.Close();
            }
        }

        public async Task AnimateAndCloseAsync(Window parentWindow)
        {
            var sb = (Storyboard)FindResource("HidePanelStoryboard");
            var tcs = new TaskCompletionSource<bool>();
            sb.Completed += (s, e) => tcs.SetResult(true);
            sb.Begin(this);
            await tcs.Task;
            parentWindow.Close();
        }

        public bool IsMouseOverPanel(Point mousePosition)
        {
            var relativePoint = this.TransformToAncestor((Visual)Parent).Transform(new Point(0, 0));
            var rect = new Rect(relativePoint, new Size(ActualWidth, ActualHeight));
            return rect.Contains(mousePosition);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, e);
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && DataContext is NotificationsViewModel vm)
            {
                System.Threading.Tasks.Task.Run(async () => await vm.LoadUserBookmarksAsync());
            }
        }
    }
}

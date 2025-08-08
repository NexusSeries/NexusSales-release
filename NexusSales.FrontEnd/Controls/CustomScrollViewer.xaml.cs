using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NexusSales.FrontEnd.Controls
{
    public partial class CustomScrollViewer : UserControl
    {
        public CustomScrollViewer()
        {
            InitializeComponent();
        }

        // The ContentProperty is inherited from UserControl, so we don't need to redefine it.

        public static readonly DependencyProperty ScrollThumbColorProperty =
            DependencyProperty.Register("ScrollThumbColor", typeof(Brush), typeof(CustomScrollViewer),
                new PropertyMetadata(Brushes.Purple));

        public Brush ScrollThumbColor
        {
            get { return (Brush)GetValue(ScrollThumbColorProperty); }
            set { SetValue(ScrollThumbColorProperty, value); }
        }

        // Add VerticalScrollBarVisibility property
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), 
                typeof(CustomScrollViewer), new PropertyMetadata(ScrollBarVisibility.Auto));

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        // Add HorizontalScrollBarVisibility property
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), 
                typeof(CustomScrollViewer), new PropertyMetadata(ScrollBarVisibility.Disabled));

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }
    }
}
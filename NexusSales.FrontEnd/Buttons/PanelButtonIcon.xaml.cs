using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NexusSales.FrontEnd.Buttons
{
    public partial class PanelButtonIcon : UserControl
    {
        public PanelButtonIcon()
        {
            InitializeComponent();
            this.Cursor = Cursor;

            // Register the brush with XAML namescope so it can be found by animations
            this.Loaded += (s, e) => {
                var brush = Resources["AnimatedForeground"] as SolidColorBrush;
                if (brush != null)
                {
                    System.Windows.NameScope.GetNameScope(this)?.RegisterName("AnimatedForeground", brush);
                }
            };
        }

        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register("IsHovered", typeof(bool), typeof(PanelButtonIcon), 
                new PropertyMetadata(false, OnIsHoveredChanged));

        public bool IsHovered
        {
            get { return (bool)GetValue(IsHoveredProperty); }
            set { SetValue(IsHoveredProperty, value); }
        }

        private static void OnIsHoveredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PanelButtonIcon icon)
            {
                icon.UpdateIconColor((bool)e.NewValue);
            }
        }

        private void UpdateIconColor(bool isHovered)
        {
            var brush = Resources["AnimatedForeground"] as SolidColorBrush;
            if (brush == null) return;

            var targetColor = isHovered ? HoverColor : IconColor;
            
            var animation = new ColorAnimation
            {
                To = targetColor,
                Duration = TimeSpan.FromSeconds(0.25)
            };
            
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        public static readonly DependencyProperty GlowProperty =
            DependencyProperty.Register("Glow", typeof(bool), typeof(PanelButtonIcon), new PropertyMetadata(false));

        public bool Glow
        {
            get { return (bool)GetValue(GlowProperty); }
            set { SetValue(GlowProperty, value); }
        }

        public static readonly DependencyProperty AnimateProperty =
            DependencyProperty.Register("Animate", typeof(bool), typeof(PanelButtonIcon), new PropertyMetadata(false));

        public bool Animate
        {
            get { return (bool)GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(PanelButtonIcon), new PropertyMetadata(Brushes.Gray));

        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public static readonly DependencyProperty ActiveColorProperty =
            DependencyProperty.Register("ActiveColor", typeof(Brush), typeof(PanelButtonIcon), new PropertyMetadata(null));

        public Brush ActiveColor
        {
            get { return (Brush)GetValue(ActiveColorProperty); }
            set { SetValue(ActiveColorProperty, value); }
        }

        public static readonly DependencyProperty ClickProperty =
            DependencyProperty.Register("Click", typeof(string), typeof(PanelButtonIcon), new PropertyMetadata(null));

        public string Click
        {
            get { return (string)GetValue(ClickProperty); }
            set { SetValue(ClickProperty, value); }
        }

        public new static readonly DependencyProperty CursorProperty =
            DependencyProperty.Register("Cursor", typeof(Cursor), typeof(PanelButtonIcon), new PropertyMetadata(Cursors.Hand));

        public new Cursor Cursor
        {
            get { return (Cursor)GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }

        public static readonly DependencyProperty IconColorProperty =
            DependencyProperty.Register("IconColor", typeof(Color), typeof(PanelButtonIcon),
                new PropertyMetadata(Colors.Gray));

        public Color IconColor
        {
            get { return (Color)GetValue(IconColorProperty); }
            set { SetValue(IconColorProperty, value); }
        }

        public static readonly DependencyProperty HoverColorProperty =
            DependencyProperty.Register("HoverColor", typeof(Color), typeof(PanelButtonIcon),
                new PropertyMetadata(Colors.White));

        public Color HoverColor
        {
            get { return (Color)GetValue(HoverColorProperty); }
            set { SetValue(HoverColorProperty, value); }
        }
    }
}
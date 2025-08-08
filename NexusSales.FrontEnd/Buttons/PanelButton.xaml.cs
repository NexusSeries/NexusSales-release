using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NexusSales.FrontEnd.Buttons
{
    public partial class PanelButton : UserControl
    {
        private FrameworkElement _iconElement;
        private Border _glowBorder;

        public PanelButton()
        {
            InitializeComponent();
            this.Loaded += PanelButton_Loaded;
        }

        private void PanelButton_Loaded(object sender, RoutedEventArgs e)
        {
            // Find template parts
            var button = this.FindName("button") as Button;
            if (button != null && button.IsLoaded && button.Template != null)
            {
                var mainContent = button.Template.FindName("mainContent", button) as ContentPresenter;
                _glowBorder = button.Template.FindName("glowBorder", button) as Border;

                // Find the actual icon content to set foreground
                if (mainContent != null && mainContent.Content != null)
                {
                    _iconElement = FindForegroundElement(mainContent.Content as DependencyObject);
                    
                    // If the content is a PanelButtonIcon, bind its IsHovered to the button's IsMouseOver
                    if (mainContent.Content is PanelButtonIcon icon)
                    {
                        var binding = new Binding("IsMouseOver") 
                        { 
                            Source = button,
                            Mode = BindingMode.OneWay
                        };
                        icon.SetBinding(PanelButtonIcon.IsHoveredProperty, binding);
                    }
                }

                // Apply initial state if needed
                if (IsActive)
                {
                    SetActive();
                }
            }
        }

        private FrameworkElement FindForegroundElement(DependencyObject parent)
        {
            if (parent == null) return null;

            // Check if this is a framework element that can have a foreground
            if (parent is TextBlock || parent is Control)
            {
                return parent as FrameworkElement;
            }

            // If it's a container, try to find a child with Foreground property
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindForegroundElement(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        #region Dependency Properties

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(object), typeof(PanelButton), new PropertyMetadata(null));

        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(PanelButton), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(PanelButton), new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.Register("GlowColor", typeof(Brush), typeof(PanelButton),
                new PropertyMetadata(Brushes.Purple));

        public Brush GlowColor
        {
            get { return (Brush)GetValue(GlowColorProperty); }
            set { SetValue(GlowColorProperty, value); }
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(PanelButton),
                new PropertyMetadata(false, OnIsActiveChanged));

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as PanelButton;
            if (button != null)
            {
                if ((bool)e.NewValue)
                {
                    button.SetActive();
                }
                else
                {
                    button.SetInactive();
                }
            }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register("ToolTipText", typeof(string), typeof(PanelButton), new PropertyMetadata(string.Empty));

        public string ToolTipText
        {
            get { return (string)GetValue(ToolTipTextProperty); }
            set { SetValue(ToolTipTextProperty, value); }
        }

        public static readonly new DependencyProperty CursorProperty =
            DependencyProperty.Register("Cursor", typeof(Cursor), typeof(PanelButton), 
                new PropertyMetadata(Cursors.Hand, OnCursorChanged));

        public new Cursor Cursor
        {
            get { return (Cursor)GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }

        private static void OnCursorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as PanelButton;
            if (button != null)
            {
                var buttonElement = button.FindName("button") as Button;
                if (buttonElement != null)
                {
                    buttonElement.Cursor = (Cursor)e.NewValue;
                }
            }
        }

        public static readonly DependencyProperty GlowEnabledProperty =
            DependencyProperty.Register("GlowEnabled", typeof(bool), typeof(PanelButton), 
                new PropertyMetadata(true));

        public bool GlowEnabled
        {
            get { return (bool)GetValue(GlowEnabledProperty); }
            set { SetValue(GlowEnabledProperty, value); }
        }

        public static readonly DependencyProperty AnimateEnabledProperty =
            DependencyProperty.Register("AnimateEnabled", typeof(bool), typeof(PanelButton), 
                new PropertyMetadata(true));

        public bool AnimateEnabled
        {
            get { return (bool)GetValue(AnimateEnabledProperty); }
            set { SetValue(AnimateEnabledProperty, value); }
        }

        #endregion

        private void SetActive()
        {
            if (_glowBorder != null)
            {
                _glowBorder.Opacity = 0.3;
            }
        }

        private void SetInactive()
        {
            if (_glowBorder != null)
            {
                _glowBorder.Opacity = 0;
            }
        }
    }
}
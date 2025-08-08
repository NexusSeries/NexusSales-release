using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System;

namespace NexusSales.UI.UserControls
{
    public partial class FirstButton : UserControl
    {
        // Dependency Properties
        public static readonly DependencyProperty GlowEffectProperty =
            DependencyProperty.Register(nameof(GlowEffect), typeof(bool), typeof(FirstButton), new PropertyMetadata(false));

        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.Register(nameof(GlowColor), typeof(Color), typeof(FirstButton), new PropertyMetadata(Colors.Transparent));

        public static readonly DependencyProperty ZoomEffectProperty =
            DependencyProperty.Register(nameof(ZoomEffect), typeof(bool), typeof(FirstButton), new PropertyMetadata(false));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(FirstButton), new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(nameof(Radius), typeof(CornerRadius), typeof(FirstButton), new PropertyMetadata(new CornerRadius(10)));

        public static readonly DependencyProperty ButtonColorProperty =
            DependencyProperty.Register(nameof(ButtonColor), typeof(Brush), typeof(FirstButton), new PropertyMetadata(null));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(FirstButton), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(FirstButton), new PropertyMetadata(new Thickness(1)));

        // CLR Properties for Dependency Properties
        public bool GlowEffect
        {
            get => (bool)GetValue(GlowEffectProperty);
            set => SetValue(GlowEffectProperty, value);
        }

        public Color GlowColor
        {
            get => (Color)GetValue(GlowColorProperty);
            set => SetValue(GlowColorProperty, value);
        }

        public bool ZoomEffect
        {
            get => (bool)GetValue(ZoomEffectProperty);
            set => SetValue(ZoomEffectProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public CornerRadius Radius
        {
            get => (CornerRadius)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public Brush ButtonColor
        {
            get => (Brush)GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        // Forward Width/Height to PART_Button for easy use in XAML
        // Note: It's generally better to use TemplateBinding for Width/Height in the control's template
        // rather than overriding these properties in code-behind, but keeping your existing pattern.
        public new double Width
        {
            get => base.Width;
            set { base.Width = value; PART_Button.Width = value; }
        }
        public new double Height
        {
            get => base.Height;
            set { base.Height = value; PART_Button.Height = value; }
        }

        // -------------------------------------------------------------------
        // Custom Click Routed Event Implementation
        // -------------------------------------------------------------------

        // 1. Register the RoutedEvent
        // This static field defines the routed event itself.
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            "Click", // The name of the event (as seen in XAML)
            RoutingStrategy.Bubble, // How the event travels through the element tree
            typeof(RoutedEventHandler), // The type of delegate for the event handler
            typeof(FirstButton) // The owner type of this event
        );

        // 2. CLR Event Wrapper
        // This provides the standard C# event syntax (e.g., myButton.Click += MyHandler;).
        // It internally hooks into the AddHandler and RemoveHandler methods of the routed event system.
        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        // -------------------------------------------------------------------
        // Constructor and Event Handlers
        // -------------------------------------------------------------------

        public FirstButton()
        {
            InitializeComponent();

            // Hook up the internal PART_Button's Click event to our custom event raiser
            PART_Button.Click += PART_Button_Click;

            PART_Button.MouseEnter += (s, e) =>
            {
                if (GlowEffect)
                {
                    var effect = new DropShadowEffect
                    {
                        Color = GlowColor == default(Color)
                            ? (Color)FindResource("AccentColor") // Assuming AccentColor is defined in resources
                            : GlowColor,
                        BlurRadius = 0,
                        ShadowDepth = 0,
                        Opacity = 0
                    };
                    PART_Button.Effect = effect;

                    // Animate BlurRadius and Opacity
                    var blurAnim = new DoubleAnimation(0, 20, TimeSpan.FromMilliseconds(300));
                    var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                    effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
                if (ZoomEffect)
                {
                    var anim = new ScaleTransform(1.0, 1.0);
                    PART_Button.RenderTransformOrigin = new Point(0.5, 0.5);
                    PART_Button.RenderTransform = anim;
                    var scaleAnim = new DoubleAnimation(1.0, 1.1, new Duration(TimeSpan.FromMilliseconds(150)));
                    anim.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    anim.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }
            };
            PART_Button.MouseLeave += (s, e) =>
            {
                if (GlowEffect && PART_Button.Effect is DropShadowEffect effect)
                {
                    var blurAnim = new DoubleAnimation(effect.BlurRadius, 0, TimeSpan.FromMilliseconds(300));
                    var opacityAnim = new DoubleAnimation(effect.Opacity, 0, TimeSpan.FromMilliseconds(300));
                    effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                    effect.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
                if (ZoomEffect)
                {
                    var anim = PART_Button.RenderTransform as ScaleTransform;
                    if (anim != null)
                    {
                        var scaleAnim = new DoubleAnimation(1.1, 1.0, new Duration(TimeSpan.FromMilliseconds(150)));
                        anim.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                        anim.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                    }
                }
            };
        }

        // This is the internal event handler for the PART_Button's Click event.
        // It's responsible for raising our custom FirstButton.Click routed event.
        private void PART_Button_Click(object sender, RoutedEventArgs e)
        {
            // Create a new instance of RoutedEventArgs for our custom event.
            // Pass our custom ClickEvent as the RoutedEvent to be raised.
            // The 'sender' will be 'this' (the FirstButton instance) when the event is handled externally.
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        }

        // Dependency Property Changed Callbacks
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as FirstButton;
            if (ctrl != null && ctrl.PART_Button != null)
            {
                ctrl.PART_Button.Content = e.NewValue as string;
            }
        }
    }
}

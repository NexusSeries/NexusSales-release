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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NexusSales.UI.UserControls
{
    /// <summary>
    /// Interaction logic for SecondButton.xaml
    /// </summary>
    public partial class SecondButton : UserControl
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(SecondButton), new PropertyMetadata("Home"));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(SecondButton), new PropertyMetadata(false));

        public static readonly DependencyProperty GlowEffectProperty =
            DependencyProperty.Register("GlowEffect", typeof(bool), typeof(SecondButton), new PropertyMetadata(true));

        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.Register("GlowColor", typeof(Brush), typeof(SecondButton), new PropertyMetadata((Brush)Application.Current.FindResource("AccentBrush")));

        public static readonly DependencyProperty ZoomEffectProperty =
            DependencyProperty.Register("ZoomEffect", typeof(bool), typeof(SecondButton), new PropertyMetadata(true));

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(SecondButton), new PropertyMetadata((Brush)Application.Current.FindResource("FontNormalBrush")));

        public static readonly DependencyProperty ToolTipProperty =
            DependencyProperty.Register("ToolTip", typeof(string), typeof(SecondButton), new PropertyMetadata(""));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public bool GlowEffect
        {
            get => (bool)GetValue(GlowEffectProperty);
            set => SetValue(GlowEffectProperty, value);
        }

        public Brush GlowColor
        {
            get => (Brush)GetValue(GlowColorProperty);
            set => SetValue(GlowColorProperty, value);
        }

        public bool ZoomEffect
        {
            get => (bool)GetValue(ZoomEffectProperty);
            set => SetValue(ZoomEffectProperty, value);
        }

        public Brush Color
        {
            get => (Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public string ToolTip
        {
            get => (string)GetValue(ToolTipProperty);
            set => SetValue(ToolTipProperty, value);
        }

        public SecondButton()
        {
            InitializeComponent();
        }
    }
}

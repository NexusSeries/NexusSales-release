using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NexusSales.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for MessageDialog.xaml
    /// </summary>
    public partial class MessageDialog : Window
    {
        // Private constructor to force use of the static Show method
        private MessageDialog()
        {
            InitializeComponent();
            this.Loaded += MessageDialog_Loaded;
        }

        // Public constructor to allow passing message, title, soundFileName, and titleColor
        public MessageDialog(string message, string title = "Notification", string soundFileName = null, Brush titleColor = null) : this()
        {
            MessageTextBlock.Text = message;
            TitleTextBlock.Text = title;
            TitleTextBlock.Foreground = titleColor ?? (Brush)Application.Current.FindResource("FontNormalBrush");

            // Play the sound if a file name is provided
            if (!string.IsNullOrEmpty(soundFileName))
            {
                PlaySound(soundFileName);
            }
        }

        // Event handler for the OK button click
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            CloseDialogWithAnimation();
        }

        // Animation for showing the dialog
        private void MessageDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 0;
            ScaleTransform scaleTransform = new ScaleTransform(0.8, 0.8);
            DialogRootBorder.RenderTransformOrigin = new Point(0.5, 0.5);
            DialogRootBorder.RenderTransform = scaleTransform;

            DoubleAnimation fadeIn = new DoubleAnimation(1, TimeSpan.FromSeconds(0.3));
            DoubleAnimation scaleX = new DoubleAnimation(1, TimeSpan.FromSeconds(0.3));
            DoubleAnimation scaleY = new DoubleAnimation(1, TimeSpan.FromSeconds(0.3));

            this.BeginAnimation(Window.OpacityProperty, fadeIn);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        // Animation for closing the dialog
        private void CloseDialogWithAnimation()
        {
            DoubleAnimation fadeOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2));
            DoubleAnimation scaleX = new DoubleAnimation(0.8, TimeSpan.FromSeconds(0.2));
            DoubleAnimation scaleY = new DoubleAnimation(0.8, TimeSpan.FromSeconds(0.2));

            fadeOut.Completed += (s, e) => this.Close();

            this.BeginAnimation(Window.OpacityProperty, fadeOut);
            if (DialogRootBorder.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }
        }

        // Helper method to play a sound effect using SoundPlayer
        private void PlaySound(string soundFileName)
        {
            try
            {
                // SoundPlayer works best with WAV files.
                string uriString = $"pack://application:,,,/NexusSales.UI;component/Audio/{soundFileName}";
                var resourceInfo = Application.GetResourceStream(new Uri(uriString));
                if (resourceInfo != null)
                {
                    using (var stream = resourceInfo.Stream)
                    {
                        SoundPlayer player = new SoundPlayer(stream);
                        player.Play();
                    }
                }
                else
                {
                    // Show error dialog if resource not found
                    var errorDialog = new MessageDialog(
                        $"Sound resource '{soundFileName}' not found.",
                        "Sound Error",
                        soundFileName: "Warning.wav",
                        titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                    );
                    errorDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new MessageDialog(
                    $"Error playing sound '{soundFileName}': {ex.Message}",
                    "Sound Error",
                    soundFileName: "Warning.wav",
                    titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                );
                errorDialog.ShowDialog();
            }
        }

        // Static helper method to easily show the dialog from anywhere
        public static bool? Show(string message, string title = "Notification", string soundFileName = null, Brush titleColor = null)
        {
            var dialog = new MessageDialog(message, title, soundFileName, titleColor);
            return dialog.ShowDialog();
        }
    }
}
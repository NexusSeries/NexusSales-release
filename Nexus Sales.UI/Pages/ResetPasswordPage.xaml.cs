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
using System.Net;
using System.Net.Mail;
using System.Windows.Media.Animation;
using Npgsql;
using NexusSales.UI.Dialogs;
using System.IO;
using System.Net.Sockets;

namespace NexusSales.UI.Pages
{
    /// <summary>
    /// Interaction logic for ResetPasswordPage.xaml
    /// </summary>
    public partial class ResetPasswordPage : Page
    {
        private readonly string _email;
        private readonly LoginWindow _parentWindow;
        private readonly bool _isForward = true;
        private readonly string _connectionString;

        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;
        private bool _isLoginPasswordVisible = false;
        private bool _isProcessingResetPassword = false;

        private ResetPasswordPage() { }

        private ResetPasswordPage(string email) { }

        private ResetPasswordPage(LoginWindow parentWindow, string email)
        {
            throw new InvalidOperationException("Use the constructor with connectionString.");
        }

        private ResetPasswordPage(LoginWindow parentWindow, string email, bool isForward)
        {
            throw new InvalidOperationException("Use the constructor with connectionString.");
        }

        public ResetPasswordPage(LoginWindow parentWindow, string email, string connectionString, bool isForward = true)
        {
            InitializeComponent();
            _parentWindow = parentWindow;
            _email = email;
            _connectionString = connectionString;
            _isForward = isForward;
            this.Loaded += ResetPasswordPage_AnimateIn;
        }

        private async void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessingResetPassword)
                return;
            _isProcessingResetPassword = true;

            string newPassword = _isNewPasswordVisible ? NewPasswordTextBox.Text : NewPasswordBox.Password;
            string confirmPassword = _isConfirmPasswordVisible ? ConfirmPasswordTextBox.Text : ConfirmPasswordBox.Password;

            if (newPassword != confirmPassword)
            {
                SetStatus("Passwords don't match", true);
                _isProcessingResetPassword = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                SetStatus("Password cannot be empty.", true);
                _isProcessingResetPassword = false;
                return;
            }

            if (newPassword.Length < 8)
            {
                SetStatus("Password must be at least 8 characters.", true);
                _isProcessingResetPassword = false;
                return;
            }

            var btn = sender as NexusSales.UI.UserControls.FirstButton;
            if (btn != null)
            {
                btn.IsEnabled = false;
                try
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                    using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new Npgsql.NpgsqlCommand(
                            "UPDATE users SET password_hash = @hash, reset_code = NULL, reset_code_expires = NULL WHERE email = @email", conn);
                        cmd.Parameters.AddWithValue("hash", hashedPassword);
                        cmd.Parameters.AddWithValue("email", _email);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Show success dialog
                    var dialog = new MessageDialog("Password reset successfully!", soundFileName: "Success.wav", titleColor: (Brush)Application.Current.FindResource("FontSuccessfulBrush"));
                    dialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    SetStatus("Failed to reset password: " + ex.Message, true);
                    btn.IsEnabled = true;
                    _isProcessingResetPassword = false;
                    return;
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }

            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();

            // Restart the login window instead of navigating to LoginPage
            var parentWindow = Window.GetWindow(this);
            if (parentWindow is LoginWindow)
            {
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
                return;
            }

            _parentWindow.NavigateToLogin();

            SetStatus("", false); // Clear status on success
            _isProcessingResetPassword = false;
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isNewPasswordVisible)
            {
                NewPasswordTextBox.Text = NewPasswordBox.Password;
            }
            // Hide placeholder if either control has text
            NewPasswordPlaceholder.Visibility = 
                string.IsNullOrEmpty(NewPasswordBox.Password) && string.IsNullOrEmpty(NewPasswordTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void NewPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewPasswordVisible)
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
            }
            // Hide placeholder if TextBox has text
            NewPasswordPlaceholder.Visibility = string.IsNullOrEmpty(NewPasswordTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isConfirmPasswordVisible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
            }
            ConfirmPasswordPlaceholder.Visibility =
                string.IsNullOrEmpty(ConfirmPasswordBox.Password) && string.IsNullOrEmpty(ConfirmPasswordTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ConfirmPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmPasswordVisible)
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
            }
            ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPasswordTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void NewPasswordEye_Click(object sender, RoutedEventArgs e)
        {
            _isNewPasswordVisible = !_isNewPasswordVisible;
            NewPasswordBox.Visibility = _isNewPasswordVisible ? Visibility.Collapsed : Visibility.Visible;
            NewPasswordTextBox.Visibility = _isNewPasswordVisible ? Visibility.Visible : Visibility.Collapsed;
            if (_isNewPasswordVisible)
                NewPasswordTextBox.Text = NewPasswordBox.Password;
            else
                NewPasswordBox.Password = NewPasswordTextBox.Text;

            // Toggle icon
            NewPasswordIcon.Kind = _isNewPasswordVisible
                ? MahApps.Metro.IconPacks.PackIconMaterialKind.Eye
                : MahApps.Metro.IconPacks.PackIconMaterialKind.EyeClosed;
        }

        private void ConfirmPasswordEye_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            ConfirmPasswordBox.Visibility = _isConfirmPasswordVisible ? Visibility.Collapsed : Visibility.Visible;
            ConfirmPasswordTextBox.Visibility = _isConfirmPasswordVisible ? Visibility.Visible : Visibility.Collapsed;
            if (_isConfirmPasswordVisible)
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
            else
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;

            // Toggle icon
            ConfirmPasswordIcon.Kind = _isConfirmPasswordVisible
                ? MahApps.Metro.IconPacks.PackIconMaterialKind.Eye
                : MahApps.Metro.IconPacks.PackIconMaterialKind.EyeClosed;
        }

        private void NewPasswordEyeBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NewPasswordEye_Click(sender, e);
        }

        private void ConfirmPasswordEyeBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ConfirmPasswordEye_Click(sender, e);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.NavigateToForgotPasswordWithVerifiedEmail(_email);
        }

        private void SetStatus(string message, bool show = true)
        {
            StatusText.Text = message;
            StatusText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void NavigateBackToLogin()
        {
            _parentWindow.NavigateToLogin();
        }

        private void ResetPasswordPage_AnimateIn(object sender, RoutedEventArgs e)
        {
            double from = _isForward ? ActualWidth : -ActualWidth;
            var slideIn = new System.Windows.Media.Animation.ThicknessAnimation
            {
                From = new Thickness(from, 0, -from, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(500), // 500ms duration
                DecelerationRatio = 0.9
            };
            BeginAnimation(MarginProperty, slideIn);
            NewPasswordBox.Focus();
        }
    }
}

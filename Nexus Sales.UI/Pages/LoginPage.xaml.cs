using NexusSales.UI.Dialogs;
using NexusSales.UI.UserControls;
using Npgsql;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
namespace NexusSales.UI.Pages
{
    public partial class LoginPage : Page
    {
        private const int LockoutThreshold = 3;
        private const int MaxPermanentBlockMinutes = 70;
        private bool _isLoggingIn = false;

        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;
        private bool _isLoginPasswordVisible = false;

        private readonly LoginWindow _parentWindow;
        private readonly bool _isForward = true;
        private readonly string _connectionString;

        public event Action LoginSucceeded;

        public LoginPage(LoginWindow parentWindow, bool isForward = true)
            : this(parentWindow, string.Empty)
        {
            _isForward = isForward;
            this.Loaded += LoginPage_AnimateIn;
        }

        public LoginPage(LoginWindow parentWindow, string connectionString)
        {
            _parentWindow = parentWindow;
            _connectionString = connectionString;
            InitializeComponent();

            // Clear remembered user to stop auto-login
            Properties.Settings.Default.RememberedUserId = string.Empty;
            Properties.Settings.Default.Save();

            string rememberedUserId = Properties.Settings.Default.RememberedUserId;
            if (!string.IsNullOrEmpty(rememberedUserId))
            {
                // Try auto-login
                Guid userId;
                if (Guid.TryParse(rememberedUserId, out userId))
                {
                    TryAutoLogin(userId);
                }
                else
                {
                    // Invalid stored value, clear it
                    Properties.Settings.Default.RememberedUserId = string.Empty;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void LoginPage_AnimateIn(object sender, RoutedEventArgs e)
        {
            double from = _isForward ? ActualWidth : -ActualWidth;
            var slideIn = new System.Windows.Media.Animation.ThicknessAnimation
            {
                From = new Thickness(from, 0, -from, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(300),
                DecelerationRatio = 0.9
            };
            BeginAnimation(MarginProperty, slideIn);
            UsernameBox.Focus();
        }

        private async void TryAutoLogin(Guid userId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = new NpgsqlCommand(@"
                        SELECT email, is_active, is_verified, is_permanently_blocked, lockout_until
                        FROM users
                        WHERE id = @id
                        LIMIT 1;", conn);
                    cmd.Parameters.AddWithValue("id", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            // User not found, clear remembered ID
                            Properties.Settings.Default.RememberedUserId = string.Empty;
                            Properties.Settings.Default.Save();
                            return;
                        }

                        bool isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));
                        bool isVerified = reader.GetBoolean(reader.GetOrdinal("is_verified"));
                        bool isPermanentlyBlocked = reader.GetBoolean(reader.GetOrdinal("is_permanently_blocked"));
                        DateTime? lockoutUntil = reader.IsDBNull(reader.GetOrdinal("lockout_until"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("lockout_until"));

                        if (!isActive || !isVerified || isPermanentlyBlocked ||
                            (lockoutUntil.HasValue && lockoutUntil.Value > DateTime.UtcNow))
                        {
                            // User cannot be auto-logged in, clear remembered ID
                            Properties.Settings.Default.RememberedUserId = string.Empty;
                            Properties.Settings.Default.Save();
                            return;
                        }

                        // Auto-login successful
                        var email = reader.GetString(reader.GetOrdinal("email"));
                        _parentWindow.LoggedInUserEmail = email; // <-- Add this line
                        LoginSucceeded?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                // Optionally log error
                Properties.Settings.Default.RememberedUserId = string.Empty;
                Properties.Settings.Default.Save();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggingIn) return;
            _isLoggingIn = true;

            var btn = sender as FirstButton;
            if (btn != null)
            {
                btn.IsEnabled = false;
            }

            string userInput = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(password))
            {
                var dialog = new MessageDialog("Please enter your Email and password.", "Invalid Credentials", soundFileName: "Warning.wav", titleColor: (Brush)Application.Current.FindResource("FontAlertBrush"));
                dialog.ShowDialog();
                if (btn != null) btn.IsEnabled = true;
                _isLoggingIn = false;
                return;
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT id, username, email, password_hash, wrong_password_attempts, lockout_until, is_permanently_blocked
                    FROM users
                    WHERE username = @input OR email = @input
                    LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("input", userInput);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        NexusSales.UI.Dialogs.MessageDialog.Show("Wrong Email or password", "Invalid Credentials", "Warning.wav", (Brush)Application.Current.FindResource("FontAlertBrush"));
                        if (btn != null) btn.IsEnabled = true;
                        _isLoggingIn = false;
                        return;
                    }

                    var userId = reader.GetGuid(reader.GetOrdinal("id"));
                    var email = reader.GetString(reader.GetOrdinal("email"));
                    var passwordHash = reader.GetString(reader.GetOrdinal("password_hash"));
                    int wrongAttempts = reader.GetInt32(reader.GetOrdinal("wrong_password_attempts"));
                    bool isPermanentlyBlocked = reader.GetBoolean(reader.GetOrdinal("is_permanently_blocked"));
                    DateTime? lockoutUntil = reader.IsDBNull(reader.GetOrdinal("lockout_until"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("lockout_until"));

                    // Check permanent block
                    if (isPermanentlyBlocked)
                    {
                        NexusSales.UI.Dialogs.MessageDialog.Show("This account is permanently blocked.", "Block Alert", "Warning.wav", (Brush)Application.Current.FindResource("FontWarningBrush"));
                        if (btn != null) btn.IsEnabled = true;
                        _isLoggingIn = false;
                        return;
                    }

                    // Check lockout
                    if (lockoutUntil.HasValue && lockoutUntil.Value > DateTime.UtcNow)
                    {
                        var minutesLeft = (int)(lockoutUntil.Value - DateTime.UtcNow).TotalMinutes;
                        NexusSales.UI.Dialogs.MessageDialog.Show($"Account is locked. Try again in {minutesLeft} minutes.", "Block Alert", "Warning.wav", (Brush)Application.Current.FindResource("FontWarningBrush"));
                        if (btn != null) btn.IsEnabled = true;
                        _isLoggingIn = false;
                        return;
                    }

                    // Verify password
                    if (BCrypt.Net.BCrypt.Verify(password, passwordHash))
                    {
                        // Success: reset lockout fields
                        reader.Close();
                        await ResetLockoutAsync(email, conn);

                        // Set the logged-in email on the parent window
                        _parentWindow.LoggedInUserEmail = email;

                        // After successful password verification
                        _parentWindow.LoggedInUserEmail = email;

                        // Raise the event only:
                        LoginSucceeded?.Invoke();

                        if (RememberMeCheckBox.IsChecked == true)
                        {
                            Properties.Settings.Default.RememberedUserId = userId.ToString();
                            Properties.Settings.Default.Save();
                        }
                        else
                        {
                            Properties.Settings.Default.RememberedUserId = string.Empty;
                            Properties.Settings.Default.Save();
                        }

                        SecureEmailStorage.SaveEmail(email);
                        return;
                    }
                    else
                    {
                        // Failed: increment attempts and handle lockout
                        wrongAttempts++;
                        int lockoutMinutes = 0;
                        bool permanentBlock = false;

                        if (wrongAttempts % LockoutThreshold == 0)
                        {
                            // Calculate lockout duration
                            if (wrongAttempts == 3)
                                lockoutMinutes = 10;
                            else if (wrongAttempts == 6)
                                lockoutMinutes = 20;
                            else if (wrongAttempts == 9)
                                lockoutMinutes = 40;
                            else if (wrongAttempts == 12)
                                lockoutMinutes = MaxPermanentBlockMinutes;
                            else
                                lockoutMinutes = 0;

                            if (lockoutMinutes >= MaxPermanentBlockMinutes)
                                permanentBlock = true;
                        }

                        DateTime? newLockoutUntil = lockoutMinutes > 0 ? DateTime.UtcNow.AddMinutes(lockoutMinutes) : (DateTime?)null;

                        // Store these for use after closing the reader
                        var failedAttempts = wrongAttempts;
                        var failedLockoutUntil = newLockoutUntil;
                        var failedPermanentBlock = permanentBlock;

                        // Show dialog after updating attempts
                        if (btn != null) btn.IsEnabled = true;
                        _isLoggingIn = false;
                        reader.Close(); // <-- Close the reader before updating

                        await UpdateFailedLoginAttempts(email, failedAttempts, failedLockoutUntil, failedPermanentBlock, conn);

                        if (failedPermanentBlock)
                        {
                            NexusSales.UI.Dialogs.MessageDialog.Show("If password entered for more 6 times the account will be blocked permanently.", "Block Alert", "Warning.wav", (Brush)Application.Current.FindResource("FontWarningBrush"));
                        }
                        else if (failedLockoutUntil.HasValue)
                        {
                            NexusSales.UI.Dialogs.MessageDialog.Show($"Account locked for {lockoutMinutes} minutes due to multiple failed attempts.", "Block Alert", "Warning.wav", (Brush)Application.Current.FindResource("FontWarningBrush"));
                        }
                        else
                        {
                            NexusSales.UI.Dialogs.MessageDialog.Show("Wrong Email or password", "Block Alert", "Warning.wav", (Brush)Application.Current.FindResource("FontAlertBrush"));
                        }
                        return;
                    }
                }
            }
            _isLoggingIn = false;
        }

        private async System.Threading.Tasks.Task ResetLockoutAsync(string email, NpgsqlConnection conn)
        {
            var cmd = new NpgsqlCommand(@"
                UPDATE users SET
                    wrong_password_attempts = 0,
                    lockout_until = NULL,
                    is_permanently_blocked = FALSE,
                    last_login_at = NOW()
                WHERE email = @userEmail", conn);
            cmd.Parameters.AddWithValue("userEmail", email);
            await cmd.ExecuteNonQueryAsync();
            // Optionally, re-fetch updated_at for local cache freshness
        }

        private async System.Threading.Tasks.Task UpdateFailedLoginAttempts(string userEmail, int wrongAttempts, DateTime? lockoutUntil, bool isPermanentlyBlocked, NpgsqlConnection conn)
        {
            var cmd = new NpgsqlCommand(@"
                UPDATE users SET
                    wrong_password_attempts = @attempts,
                    lockout_until = @lockoutUntil,
                    is_permanently_blocked = @isBlocked
                WHERE email = @userEmail", conn);
            cmd.Parameters.AddWithValue("attempts", wrongAttempts);
            cmd.Parameters.AddWithValue("lockoutUntil", (object)lockoutUntil ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isBlocked", isPermanentlyBlocked);
            cmd.Parameters.AddWithValue("userEmail", userEmail);
            await cmd.ExecuteNonQueryAsync();
            // Optionally, re-fetch updated_at for local cache freshness
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.NavigateToForgotPassword(true); // or just _parentWindow.NavigateToForgotPassword();
        }

        private void SetStatus(string message, bool show = true)
        {
            StatusText.Text = message;
            StatusText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!_isLoginPasswordVisible)
                PasswordTextBox.Text = PasswordBox.Password;
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsernamePlaceholder.Visibility = string.IsNullOrEmpty(UsernameBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void PasswordEye_Click(object sender, RoutedEventArgs e)
        {
            _isLoginPasswordVisible = !_isLoginPasswordVisible;
            PasswordBox.Visibility = _isLoginPasswordVisible ? Visibility.Collapsed : Visibility.Visible;
            PasswordTextBox.Visibility = _isLoginPasswordVisible ? Visibility.Visible : Visibility.Collapsed;
            if (_isLoginPasswordVisible)
                PasswordTextBox.Text = PasswordBox.Password;
            else
                PasswordBox.Password = PasswordTextBox.Text;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoginPasswordVisible)
                PasswordBox.Password = PasswordTextBox.Text;
        }

        private void RegisterText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Assuming you have access to the parent LoginWindow and connection string
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            if (parentWindow != null)
            {
                parentWindow.MainFrame.Navigate(new RegisterPage(parentWindow, parentWindow.DecryptedConnectionString));
            }
        }
    }
}
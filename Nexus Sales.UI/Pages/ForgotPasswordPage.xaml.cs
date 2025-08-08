using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; 
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Npgsql;
using NexusSales.UI.Dialogs;
using NexusSales.UI.UserControls;
using System.Windows.Threading;
using System.IO;
using System.Net.Sockets;

namespace NexusSales.UI.Pages
{
    public partial class ForgotPasswordPage : Page
    {
        private bool _isProcessingReceiveCode = false;
        private bool _isNavigatingToResetPassword = false;
        private DateTime? _blockUntil;
        private int _requestCount;
        private DateTime? _lastRequest;
        private int _wrongAttempts;
        private bool _isSuspended;
        private DispatcherTimer _countdownTimer;
        private readonly LoginWindow _parentWindow;
        private readonly bool _isForward;
        private string _verifiedEmail;
        private bool _isLockedOut = false;
        private readonly string _connectionString;
        private DateTime? _externalBlockUntil;

        public ForgotPasswordPage()
        {
            InitializeComponent();
            this.Loaded += ForgotPasswordPage_Loaded;

            this.Loaded += (s, e) =>
            {
                var slideIn = new ThicknessAnimation
                {
                    From = new Thickness(700, 0, -700, 0),
                    To = new Thickness(this.ActualWidth, 0, -this.ActualWidth, 0),
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    DecelerationRatio = 0.9
                };
                this.BeginAnimation(MarginProperty, slideIn);

                // Set initial focus to EmailTextBox
                EmailTextBox.Focus();
            };
        }

        public ForgotPasswordPage(LoginWindow parentWindow)
        {
            InitializeComponent();
            _parentWindow = parentWindow;
            // ... any other initialization
        }

        public ForgotPasswordPage(LoginWindow parentWindow, bool isForward = true)
        {
            InitializeComponent();
            _parentWindow = parentWindow;
            _isForward = isForward;
            this.Loaded += ForgotPasswordPage_Loaded;
            this.Loaded += ForgotPasswordPage_AnimateIn;
        }

        public ForgotPasswordPage(LoginWindow parentWindow, string connectionString)
        {
            _parentWindow = parentWindow;
            _connectionString = connectionString;
            InitializeComponent();
        }

        public ForgotPasswordPage(LoginWindow parentWindow, string connectionString, bool isForward)
        {
            _parentWindow = parentWindow;
            _connectionString = connectionString;
            _isForward = isForward;
            InitializeComponent();
            this.Loaded += ForgotPasswordPage_Loaded;
            this.Loaded += ForgotPasswordPage_AnimateIn;
        }

        public ForgotPasswordPage(LoginWindow parentWindow, string connectionString, bool isForward, string verifiedEmail = null)
            : this(parentWindow, connectionString, isForward)
        {
            _verifiedEmail = verifiedEmail;
        }

        private void ForgotPasswordPage_Loaded(object sender, RoutedEventArgs e)
        {
            ForwardButton.Visibility = !string.IsNullOrEmpty(_verifiedEmail) ? Visibility.Visible : Visibility.Collapsed;

            if (_externalBlockUntil.HasValue)
            {
                _blockUntil = _externalBlockUntil;
                if (_blockUntil > DateTime.Now)
                {
                    StartCountdown((_blockUntil.Value - DateTime.Now).TotalSeconds);
                    DisableAllActions();
                }
            }
        }

        public void ForgotPasswordPage_AnimateIn(object sender, RoutedEventArgs e)
        {
            double from = _isForward ? ActualWidth : -ActualWidth;
            var slideIn = new ThicknessAnimation
            {
                From = new Thickness(from, 0, -from, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(500),
                DecelerationRatio = 0.9
            };
            BeginAnimation(MarginProperty, slideIn);
            EmailTextBox.Focus();
        }

        private async void ReceiveCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessingReceiveCode)
                return;

            _isProcessingReceiveCode = true;
            var btn = sender as FirstButton;
            if (btn != null)
            {
                btn.IsEnabled = false;

                try
                {
                    SetStatus("Processing...");
                    var email = EmailTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(email))
                    {
                        EmailTextBox.BorderBrush = Brushes.Red;
                        SetStatus("Please enter your email.");
                        return;
                    }
                    EmailTextBox.ClearValue(TextBox.BorderBrushProperty);

                    // 1. Always check the DB for forgot_block_until
                    DateTime? dbBlockUntil = null;
                    using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new Npgsql.NpgsqlCommand("SELECT forgot_block_until FROM users WHERE email = @email", conn);
                        cmd.Parameters.AddWithValue("email", email);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                dbBlockUntil = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0);
                            }
                            else
                            {
                                SetStatus("Email not found.");
                                return;
                            }
                        }
                    }
                    if (dbBlockUntil.HasValue && dbBlockUntil > DateTime.UtcNow)
                    {
                        _blockUntil = dbBlockUntil;
                        _parentWindow.ForgotPasswordBlockUntil = _blockUntil;
                        StartCountdown((_blockUntil.Value - DateTime.UtcNow).TotalSeconds);
                        ReceiveCodeButton.IsEnabled = false;
                        SetStatus("Please wait before requesting another code.");
                        return;
                    }

                    // 2. Check request count and last request in DB
                    int requestCount = 0;
                    DateTime? lastRequest = null;
                    bool isPermanentlyBlocked = false;
                    using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new Npgsql.NpgsqlCommand("SELECT reset_request_count, reset_last_request, is_permanently_blocked FROM users WHERE email = @email", conn);
                        cmd.Parameters.AddWithValue("email", email);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                requestCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                lastRequest = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                                isPermanentlyBlocked = !reader.IsDBNull(2) && reader.GetBoolean(2);
                            }
                        }
                    }

                    if (isPermanentlyBlocked)
                    {
                        SetStatus("Account is permanently blocked. Please contact support: FacebookManagerltd@gmail.com");
                        ShowContactSupportDialog();
                        return;
                    }

                    // 3. If 3 or more requests, block for 20 minutes
                    if (requestCount >= 3)
                    {
                        var blockUntil = DateTime.UtcNow.AddMinutes(20);
                        await UpdateForgotBlockUntilInDb(email, blockUntil);
                        // Reset request count in DB
                        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                        {
                            await conn.OpenAsync();
                            var cmd = new Npgsql.NpgsqlCommand("UPDATE users SET reset_request_count = 0 WHERE email = @email", conn);
                            cmd.Parameters.AddWithValue("email", email);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Re-query the DB for the actual block_until value
                        DateTime? actualBlockUntil = null;
                        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                        {
                            await conn.OpenAsync();
                            var cmd = new Npgsql.NpgsqlCommand("SELECT forgot_block_until FROM users WHERE email = @email", conn);
                            cmd.Parameters.AddWithValue("email", email);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    actualBlockUntil = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0);
                                }
                            }
                        }
                        if (actualBlockUntil.HasValue && actualBlockUntil > DateTime.UtcNow)
                        {
                            _parentWindow.ForgotPasswordBlockUntil = actualBlockUntil;
                            StartCountdown((actualBlockUntil.Value - DateTime.UtcNow).TotalSeconds);
                        }
                        else
                        {
                            // fallback, just in case
                            StartCountdown(20 * 60);
                        }
                        ReceiveCodeButton.IsEnabled = false;
                        SetStatus("Too many requests. Please wait 20 minutes.");
                        return;
                    }

                    // 4. If last request was less than 2 minutes ago, block for 2 minutes
                    if (lastRequest.HasValue && lastRequest.Value.AddMinutes(2) > DateTime.UtcNow)
                    {
                        var blockUntil = lastRequest.Value.AddMinutes(2);
                        await UpdateForgotBlockUntilInDb(email, blockUntil);
                        _parentWindow.ForgotPasswordBlockUntil = blockUntil;
                        StartCountdown((blockUntil - DateTime.UtcNow).TotalSeconds); // <-- Always start countdown here
                        ReceiveCodeButton.IsEnabled = false;
                        SetStatus("Please wait 2 minutes before requesting another code.");
                        return;
                    }

                    // Allow request: increment request count and update last request in DB
                    using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new Npgsql.NpgsqlCommand("UPDATE users SET reset_request_count = reset_request_count + 1, reset_last_request = @now WHERE email = @email", conn);
                        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("email", email);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 6. Generate and store code
                    var code = new Random().Next(100000, 999999).ToString();
                    using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new Npgsql.NpgsqlCommand("UPDATE users SET reset_code = @code, reset_code_expires = @expires WHERE email = @email", conn);
                        cmd.Parameters.AddWithValue("code", code);
                        cmd.Parameters.AddWithValue("expires", DateTime.UtcNow.AddMinutes(10));
                        cmd.Parameters.AddWithValue("email", email);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 7. Send code via email (unchanged)
                    var smtp = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential("FacebookManagerltd@gmail.com", "pugyoinajwkytwvm"),
                        EnableSsl = true,
                    };
                    var mail = new MailMessage("NexusSalesltd@gmail.com", email, "Your Reset Code", $"Your code is: {code}");
                    await smtp.SendMailAsync(mail);

                    var dialog = new MessageDialog("A reset code has been sent to your email.", soundFileName: "Success.wav", titleColor: (Brush)Application.Current.FindResource("FontSuccessfulBrush"));
                    dialog.ShowDialog();
                    SetStatus("Code has been sent", false);
                    CodeTextBox.Focus();

                    // Start UI countdown for 2 minutes
                    StartCountdown(2 * 60);
                    ReceiveCodeButton.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    SetStatus("Failed: " + ex.Message);
                    ShowExceptionDialog(ex);
                }
                finally
                {
                    await Task.Delay(3000);
                    SetStatus("", false);
                    btn.IsEnabled = true;
                    _isProcessingReceiveCode = false;
                }
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string code = CodeTextBox.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                EmailTextBox.BorderBrush = Brushes.Red;
                SetStatus("Please enter your email.");
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Please enter your code.");
                return;
            }

            // Check code in database
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new NpgsqlCommand(
                    "SELECT reset_code, reset_code_expires FROM users WHERE email = @email", conn);
                cmd.Parameters.AddWithValue("email", email);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        SetStatus("Email not found.");
                        return;
                    }

                    string dbCode = reader.IsDBNull(0) ? null : reader.GetString(0);
                    DateTime? expires = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);

                    if (dbCode == null || expires == null || DateTime.UtcNow > expires)
                    {
                        SetStatus("Code expired or not found.");
                        return;
                    }

                    if (dbCode != code)
                    {
                        _wrongAttempts++;

                        SetStatus("Invalid code.");
                        return;
                    }
                }
            }

            // After successful code verification and before navigation:
            if (_isNavigatingToResetPassword)
                return;
            _isNavigatingToResetPassword = true;

            // Reset the delay in the database
            await ClearForgotBlockUntilInDb(email);
            _parentWindow.ForgotPasswordBlockUntil = null;

            // Store the verified email
            _verifiedEmail = email;
            ForwardButton.Visibility = Visibility.Visible;

            // Clear fields and status as requested
            EmailTextBox.Clear();
            CodeTextBox.Clear();
            SetStatus("", false);
            EmailTextBox.Focus();

            // Move to next page (e.g., ResetPasswordPage)
            _parentWindow.NavigateToResetPassword(email, true);
            
            // Optionally, reset the flag after navigation if you want to allow it again later
            // _isNavigatingToResetPassword = false;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.NavigateToLogin(false); // false = backward
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLockedOut)
                return;

            if (!string.IsNullOrEmpty(_verifiedEmail))
            {
                _parentWindow.NavigateToResetPassword(_verifiedEmail, true);
            }
            else
            {
                SetStatus("Please verify your code first.", true);
            }
        }

        private void SetStatus(string message, bool show = true)
        {
            StatusText.Text = message;
            StatusText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrEmpty(EmailTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CodePlaceholder.Visibility = string.IsNullOrEmpty(CodeTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void StartCountdown(double seconds)
        {
            // Stop and dispose any previous timer
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }

            // Clamp to at least 1 second to avoid immediate disappearance
            if (seconds < 1)
                seconds = 1;

            ReceiveCodeCountdown.Visibility = Visibility.Visible;
            var end = DateTime.UtcNow.AddSeconds(seconds);

            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (s, e) =>
            {
                var remaining = end - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    _countdownTimer.Stop();
                    ReceiveCodeCountdown.Visibility = Visibility.Collapsed;
                    ReceiveCodeButton.IsEnabled = true;
                }
                else
                {
                    ReceiveCodeCountdown.Text = $"Please wait {remaining.Minutes:D2}:{remaining.Seconds:D2} before requesting again.";
                }
            };
            _countdownTimer.Start();
        }

        private void ShowContactSupportDialog()
        {
            var dialog = new MessageDialog(
                "Please contact support at NexusSalesltd@gmail.com",
                soundFileName: "Warning.wav",
                title: "Contact support",
                titleColor: (Brush)Application.Current.FindResource("FontAlertBrush"));
            dialog.ShowDialog();
        }

        private void UpdateUserBlockInDb()
        {
            // TODO: Implement logic to update user block status in the database
        }

        private void UpdateUserRequestInDb()
        {
            // TODO: Implement logic to update user request count/timestamp in the database
        }

        private void SendCode()
        {
            // TODO: Implement logic to send the code (if not already handled inline)
        }

        private void UpdateUserSuspensionInDb()
        {
            // TODO: Implement logic to update user suspension status in the database
        }

        private void UpdateUserWrongAttemptsInDb()
        {
            // TODO: Implement logic to update wrong attempts in the database
        }

        public void DisableAllActions()
        {
            ReceiveCodeButton.IsEnabled = false;
            // If you have other buttons, disable them here as well
        }

        public void NavigateToForgotPasswordPage()
        {
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            if (parentWindow != null)
                parentWindow.MainFrame.Navigate(new ForgotPasswordPage());
        }

        public void NavigateBackToLogin()
        {
            _parentWindow.NavigateToLogin(false);
        }

        public void LockOutActions()
        {
            _isLockedOut = true;
            ReceiveCodeButton.IsEnabled = false;
            SubmitButton.IsEnabled = false;
            ForwardButton.IsEnabled = false;
            // Optionally, visually "deem" the buttons (e.g., change opacity)
            ReceiveCodeButton.Opacity = 0.5;
            SubmitButton.Opacity = 0.5;
            ForwardButton.Opacity = 0.5;
        }

        public void SetVerifiedEmail(string email)
        {
            _verifiedEmail = email;
            ForwardButton.Visibility = !string.IsNullOrEmpty(_verifiedEmail) ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetExternalBlockUntil(DateTime? blockUntil)
        {
            _externalBlockUntil = blockUntil;
            if (_externalBlockUntil.HasValue && _externalBlockUntil > DateTime.Now)
            {
                StartCountdown((_externalBlockUntil.Value - DateTime.Now).TotalSeconds);
            }
        }

        private async Task UpdateForgotBlockUntilInDb(string email, DateTime? blockUntil)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new NpgsqlCommand("UPDATE users SET forgot_block_until = @blockUntil WHERE email = @email", conn);
                cmd.Parameters.AddWithValue("blockUntil", (object)blockUntil ?? DBNull.Value);
                cmd.Parameters.AddWithValue("email", email);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ClearForgotBlockUntilInDb(string email)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new NpgsqlCommand("UPDATE users SET forgot_block_until = NULL WHERE email = @email", conn);
                cmd.Parameters.AddWithValue("email", email);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        private void ShowExceptionDialog(Exception ex)
        {
            string title = "Error";
            string message = ex.Message;
            
            // Categorize exception type for more specific title
            if (ex is NpgsqlException || ex.ToString().Contains("Npgsql"))
                title = "Database Error";
            else if (ex is SmtpException || ex.ToString().Contains("Mail") || ex.ToString().Contains("Smtp"))
                title = "Email Error";
            else if (ex is WebException || ex is System.Net.Sockets.SocketException || ex.ToString().Contains("connect"))
                title = "Connection Error";
            else if (ex is System.IO.IOException || ex is System.IO.FileNotFoundException)
                title = "File Error";
            else if (ex is TimeoutException)
                title = "Timeout Error";
            else if (ex is FormatException || ex is ArgumentException)
                title = "Input Error";
            
            // Use the custom dialog
            var dialog = new MessageDialog(
                message,
                soundFileName: "Warning.wav",
                title: title,
                titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
            );
            dialog.ShowDialog();
        }
    }
}
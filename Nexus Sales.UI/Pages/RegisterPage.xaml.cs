using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using BCrypt.Net;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql; // For PostgreSQL access
using BCrypt.Net; // For password hashing (install BCrypt.Net-Next via NuGet)
using System.Windows.Input;
using System.Windows.Media;

namespace NexusSales.UI.Pages
{
    public partial class RegisterPage : Page
    {
        private readonly string _connectionString;
        private readonly Window _parentWindow;

        private string _verificationCode;
        private DateTime? _codeExpiresAt;
        private string _pendingEmail;
        private int _sendCodeAttempts = 0;
        private DateTime? _lastSendCodeTime;

        public RegisterPage(Window parentWindow, string connectionString)
        {
            InitializeComponent();
            _parentWindow = parentWindow;
            _connectionString = connectionString;
            Loaded += RegisterPage_Loaded;
        }

        private async void RegisterPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAccountTypesAsync();
        }

        private async Task LoadAccountTypesAsync()
        {
            try
            {
                AccountTypeComboBox.Items.Clear();
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM account_types ORDER BY name", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            AccountTypeComboBox.Items.Add(new AccountTypeItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to load account types: {ex.Message}", true);
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("", false);

            string email = EmailBox.Text.Trim();
            string firstName = FirstNameBox.Text.Trim();
            string lastName = LastNameBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            var selectedType = AccountTypeComboBox.SelectedItem as AccountTypeItem;

            // Validation
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                selectedType == null)
            {
                ShowStatus("All fields are required.", true);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowStatus("Invalid email format.", true);
                return;
            }

            if (password.Length < 8)
            {
                ShowStatus("Password must be at least 8 characters.", true);
                return;
            }

            if (password != confirmPassword)
            {
                ShowStatus("Passwords do not match.", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(CodeBox.Text))
            {
                ShowStatus("Enter the verification code sent to your email.", true);
                return;
            }
            if (_pendingEmail != email || _verificationCode == null || _codeExpiresAt == null || DateTime.UtcNow > _codeExpiresAt.Value)
            {
                ShowStatus("Verification code expired. Please request a new code.", true);
                return;
            }
            if (CodeBox.Text.Trim() != _verificationCode)
            {
                ShowStatus("Invalid verification code.", true);
                return;
            }

            try
            {
                // Check if email already exists
                if (await EmailExistsAsync(email))
                {
                    ShowStatus("An account with this email already exists.", true);
                    return;
                }

                // Hash password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Insert user
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand(
                        @"INSERT INTO users (email, username, first_name, last_name, password_hash, account_type_id, settings_json)
                          VALUES (@Email, @Username, @FirstName, @LastName, @PasswordHash, @AccountTypeId, '{}'::jsonb)", conn))
                    {
                        cmd.Parameters.AddWithValue("Email", email);
                        cmd.Parameters.AddWithValue("Username", email); // Username = email for now
                        cmd.Parameters.AddWithValue("FirstName", firstName);
                        cmd.Parameters.AddWithValue("LastName", lastName);
                        cmd.Parameters.AddWithValue("PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("AccountTypeId", selectedType.Id);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                ShowStatus("Registration successful! You can now log in.", false);
                await Task.Delay(1200);
                NavigateToLogin();
            }
            catch (Exception ex)
            {
                ShowStatus($"Registration failed: {ex.Message}", true);
            }
        }

        private async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @Email", conn))
                    {
                        cmd.Parameters.AddWithValue("Email", email);
                        var count = (long)await cmd.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch
            {
                // Fail safe: don't allow registration if check fails
                return true;
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError
                ? (System.Windows.Media.Brush)Application.Current.FindResource("FontWarningBrush")
                : (System.Windows.Media.Brush)Application.Current.FindResource("FontSuccessfulBrush");
            StatusText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        private bool IsValidEmail(string email)
        {
            // Simple regex for email validation
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void NavigateToLogin()
        {
            // If parent window has a navigation method, use it. Otherwise, fallback to default.
            if (_parentWindow is LoginWindow loginWindow)
            {
                loginWindow.NavigateToLogin();
            }
            else
            {
                // Optionally, implement navigation logic here
            }
        }

        // Optional: Make "Already have an account? Login" clickable
        private void LoginText_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NavigateToLogin();
        }

        private void LoginText_MouseLeftButtonUp2(object sender, MouseButtonEventArgs e)
        {
            if (_parentWindow is LoginWindow loginWindow)
            {
                loginWindow.NavigateToLogin();
            }
        }

        private async void SendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CodeStatusText.Visibility = Visibility.Collapsed;
            string email = EmailBox.Text.Trim();

            if (!IsValidEmail(email))
            {
                CodeStatusText.Text = "Enter a valid email.";
                CodeStatusText.Foreground = (Brush)Application.Current.FindResource("FontWarningBrush");
                CodeStatusText.Visibility = Visibility.Visible;
                return;
            }

            if (await EmailExistsAsync(email))
            {
                CodeStatusText.Text = "Email already registered.";
                CodeStatusText.Foreground = (Brush)Application.Current.FindResource("FontWarningBrush");
                CodeStatusText.Visibility = Visibility.Visible;
                return;
            }

            // Rate limit: 1 per minute, max 3 per hour
            if (_lastSendCodeTime.HasValue && (DateTime.UtcNow - _lastSendCodeTime.Value).TotalSeconds < 60)
            {
                CodeStatusText.Text = "Please wait before requesting another code.";
                CodeStatusText.Foreground = (Brush)Application.Current.FindResource("FontWarningBrush");
                CodeStatusText.Visibility = Visibility.Visible;
                return;
            }
            if (_sendCodeAttempts >= 3 && _lastSendCodeTime.HasValue && (DateTime.UtcNow - _lastSendCodeTime.Value).TotalHours < 1)
            {
                CodeStatusText.Text = "Too many attempts. Try again later.";
                CodeStatusText.Foreground = (Brush)Application.Current.FindResource("FontWarningBrush");
                CodeStatusText.Visibility = Visibility.Visible;
                return;
            }

            // Generate code
            var code = new Random().Next(100000, 999999).ToString();
            _verificationCode = code;
            _codeExpiresAt = DateTime.UtcNow.AddMinutes(10);
            _pendingEmail = email;
            _sendCodeAttempts++;
            _lastSendCodeTime = DateTime.UtcNow;

            // Send email (reuse SMTP logic)
            try
            {
                var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("FacebookManagerltd@gmail.com", "pugyoinajwkytwvm"),
                    EnableSsl = true,
                };
                var mail = new System.Net.Mail.MailMessage("NexusSalesltd@gmail.com", email, "Your Nexus Sales Verification Code", $"Your verification code is: {code}");
                await smtp.SendMailAsync(mail);

                CodeStatusText.Text = "Verification code sent!";
                CodeStatusText.Foreground = (Brush)Application.Current.FindResource("FontSuccessfulBrush");
                CodeStatusText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                CodeStatusText.Text = "Failed to send code: " + ex.Message;
                CodeStatusText.Foreground = (Brush)Application.Current.FindResource("FontWarningBrush");
                CodeStatusText.Visibility = Visibility.Visible;
            }
        }

        // Helper class for ComboBox items
        private class AccountTypeItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }
    }
}
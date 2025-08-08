using NexusSales.UI.Dialogs;
using System;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.Net.Mail;
using Npgsql;
using System.Configuration;

namespace NexusSales.UI
{
    public partial class LoginWindow : Window
    {
        private readonly Pages.LoginPage _loginPage;
        private readonly Pages.ForgotPasswordPage _forgotPasswordPage;
        private Pages.ResetPasswordPage _resetPasswordPage;
        //private readonly string connectionString = "DCkmQAkFADoNHl5aFQZAIys0IQkBXVxrVydFUBQcGh4heyVbRx0IKwkBDWUHAQgEKzQxCXIIDDwOHVleV0RCQ3dzbQB5CAE4CxdEA1RGSURwd20PcAgbOA4TRVBbAhQAMCEnUUdS";
        private readonly string decryptedConnectionString;

        // Add these fields to LoginWindow
        private DateTime? _forgotPasswordBlockUntil;
        public DateTime? ForgotPasswordBlockUntil
        {
            get => _forgotPasswordBlockUntil;
            set => _forgotPasswordBlockUntil = value;
        }

        public string LoggedInUserEmail { get; set; }

        public string DecryptedConnectionString => decryptedConnectionString;

        public LoginWindow()
        {
            InitializeComponent();
            decryptedConnectionString = ConfigurationManager.ConnectionStrings["NexusSalesConnection"].ConnectionString;//NativeGuard.Decrypt(connectionString);
            _loginPage = new Pages.LoginPage(this, decryptedConnectionString);
            _loginPage.LoginSucceeded += OnLoginSucceeded;
            _forgotPasswordPage = new Pages.ForgotPasswordPage(this, decryptedConnectionString);
            MainFrame.Navigate(_loginPage);
            // AcrylicHelper.EnableBlur(this, dark: false);
        }

        public string CheckWindowState()
        {
            if (WindowState == WindowState.Maximized)
            {
                return "Minimize";
            }
            else
            {
                return "Minimize";
            }
        }

        public void NavigateToForgotPassword()
        {
            var page = new Pages.ForgotPasswordPage(this, decryptedConnectionString);
            page.SetExternalBlockUntil(ForgotPasswordBlockUntil);
            page.Loaded += page.ForgotPasswordPage_AnimateIn;
            MainFrame.Navigate(page);
        }

        public void NavigateToForgotPassword(string email)
        {
            MainFrame.Navigate(new Pages.ResetPasswordPage(this, email, decryptedConnectionString));
        }

        public void NavigateToForgotPassword(bool isForward = true)
        {
            var page = new Pages.ForgotPasswordPage(this, decryptedConnectionString, isForward);
            page.SetExternalBlockUntil(ForgotPasswordBlockUntil);
            page.Loaded += page.ForgotPasswordPage_AnimateIn;
            MainFrame.Navigate(page);
        }

        public void NavigateToForgotPassword(bool isForward, string email = null)
        {
            MainFrame.Navigate(new Pages.ForgotPasswordPage(this, decryptedConnectionString, isForward) { /* set email if needed */ });
        }

        public void NavigateToForgotPasswordWithVerifiedEmail(string previouslyVerifiedEmail)
        {
            MainFrame.Navigate(
                new Pages.ForgotPasswordPage(this, decryptedConnectionString, false, previouslyVerifiedEmail)
            );
        }

        public void NavigateToResetPassword(string email)
        {
            MainFrame.Navigate(new Pages.ResetPasswordPage(this, email, decryptedConnectionString));
        }

        public void NavigateToResetPassword(string email, bool isForward = true)
        {
            MainFrame.Navigate(new Pages.ResetPasswordPage(this, email, decryptedConnectionString));
        }

        public void NavigateToLogin()
        {
            MainFrame.Navigate(_loginPage);
        }

        public void NavigateToLogin(bool isForward = true)
        {
            MainFrame.Navigate(_loginPage);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            AnimateWindowSize(SystemParameters.PrimaryScreenWidth * 0.25, SystemParameters.PrimaryScreenHeight * 0.7, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            // Minimize the parent window
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // This logic will move to LoginPage.xaml.cs
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("Sign up not implemented yet.", "Not Implemented", soundFileName: "Warning.wav", titleColor: (Brush)Application.Current.FindResource("FontWarningBrush"));
            dialog.ShowDialog();
        }

        private void AnimateWindowSize(double fromWidth, double fromHeight, double toWidth, double toHeight)
        {
            var widthAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = fromWidth,
                To = toWidth,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            var heightAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = fromHeight,
                To = toHeight,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            BeginAnimation(WidthProperty, widthAnim);
            BeginAnimation(HeightProperty, heightAnim);
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }

        private void OnLoginSucceeded()
        {
            this.DialogResult = true; // <-- This is the key line
            var dialog = new MessageDialog("Login successful.", "Success", soundFileName: "Success.wav", titleColor: (Brush)Application.Current.FindResource("FontSuccessfulBrush"));
            dialog.ShowDialog();
            // this.Close(); // Not needed, DialogResult=true will close the window
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!this.DialogResult.HasValue)
                this.DialogResult = false;
            base.OnClosing(e);
        }
    }
}
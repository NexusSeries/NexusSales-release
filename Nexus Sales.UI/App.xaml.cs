using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NexusSales.Services;
using NexusSales.Core.Interfaces;
using NexusSales.Data;
using NexusSales.UI.Dialogs;
using NexusSales.UI.ViewModels;
using System.IO;
using System.Net.Sockets;
using System.Windows.Media;
using System.Diagnostics;
using System.Configuration;

namespace NexusSales.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            // Add this to App.xaml.cs constructor or early in application startup
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();

                var login = new LoginWindow();
                bool? result = login.ShowDialog();

                if (result == true)
                {
                    try
                    {
                        // Get userEmail from login page/session
                        string userEmail = login.LoggedInUserEmail;

                        if (string.IsNullOrEmpty(userEmail))
                        {
                            // Show error dialog or handle gracefully
                            MessageDialog.Show("User email is missing after login.", "Error", "Warning.wav", (Brush)Application.Current.FindResource("FontWarningBrush"));
                            return;
                        }

                        // Resolve dependencies from DI
                        var notificationService = ServiceProvider.GetRequiredService<NotificationService>();
                        var repository = ServiceProvider.GetRequiredService<IDbRepository>();

                        // Construct MainWindow manually
                        var mainWindow = new MainWindow(notificationService, repository, userEmail);
                        mainWindow.Show();

                        Application.Current.MainWindow = mainWindow;
                    }
                    catch (Exception ex)
                    {
                        var dialog = new MessageDialog(
                            $"Error creating/showing MainWindow: {ex.Message}",
                            soundFileName: "Warning.wav",
                            title: "Application Error",
                            titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                        );
                        dialog.ShowDialog();
                    }
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    $"Error in OnStartup: {ex.Message}",
                    soundFileName: "Warning.wav",
                    title: "Application Error",
                    titleColor: (Brush)Application.Current.FindResource("FontWarningBrush")
                );
                dialog.ShowDialog();
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Get the connection string from config
            var connectionString = ConfigurationManager.ConnectionStrings["NexusSalesConnection"].ConnectionString;

            // Register database context
            services.AddScoped<NexusSalesDbContext>();

            // Register repository with connection string factory
            services.AddScoped<IDbRepository>(provider =>
            {
                var dbContext = provider.GetRequiredService<NexusSalesDbContext>();
                return new SqlDbRepository(dbContext, connectionString);
            });

            // Register NotificationService with factory to inject connectionString
            services.AddSingleton<NotificationService>(provider =>
            {
                var repo = provider.GetRequiredService<IDbRepository>();
                return new NotificationService(repo, connectionString);
            });

            // Register services
            services.AddSingleton<IMessengerService, MessengerService>();

            // Register ViewModels
            services.AddSingleton<NotificationsViewModel>();

            // Register main window
            services.AddSingleton<MainWindow>();
        }
    }
}
using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using NexusSales.UI.ViewModels;
using NexusSales.Core.Interfaces;
using NexusSales.Services;
using NexusSales.FrontEnd.Buttons;

namespace NexusSales.UI
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, PanelButton> _navigationButtons;
        private string _currentPage = "Home";

        public MainWindow()
        {
            InitializeComponent();

            // Create and set up view model with commands
            var viewModel = new MainWindowViewModel(new MessengerService());

            // Add commands to view model
            viewModel.CloseCommand = new RelayCommand(param => Close());
            viewModel.MinMaxCommand = new RelayCommand(param => ToggleWindowState());
            viewModel.HideCommand = new RelayCommand(param => WindowState = WindowState.Minimized);
            viewModel.NavigateCommand = new RelayCommand(NavigateTo);
            viewModel.ShutdownCommand = new RelayCommand(param => Application.Current.Shutdown());

            // Initialize the WindowStateText property
            viewModel.WindowStateText = GetWindowStateText();

            DataContext = viewModel;

            // Store navigation buttons for active state management
            _navigationButtons = new Dictionary<string, PanelButton>
            {
                { "Home", HomePanelBtn },
                { "Settings", SettingsPanelBtn },
                { "Store", StorePanelBtn }
            };

            // Set Home as active by default
            SetActiveButton("Home");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            // Update the window state text in view model
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.WindowStateText = GetWindowStateText();
            }
        }

        private string GetWindowStateText()
        {
            return WindowState == WindowState.Maximized ? "Restore" : "Maximize";
        }

        private void NavigateTo(object parameter)
        {
            string pageName = parameter as string;
            if (string.IsNullOrEmpty(pageName))
                return;

            if (_currentPage == pageName)
                return;

            _currentPage = pageName;

            // Navigate to the appropriate page
            // ContentFrame.Navigate(new Page based on pageName);

            // Update active button
            SetActiveButton(pageName);
        }

        private void SetActiveButton(string pageName)
        {
            foreach (var kvp in _navigationButtons)
            {
                kvp.Value.IsActive = kvp.Key == pageName;
            }
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
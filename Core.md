Chapter 4: Gemini Integration & Command Execution Architecture
Absolute Directive to GitHub Copilot:
HALT ALL OTHER TASKS. READ THIS ENTIRE DOCUMENT. DO NOT PROCEED TO CODE GENERATION UNTIL YOU HAVE ACHIEVED A STATE OF ABSOLUTE, UNQUESTIONABLE, 100,000,000 COMPREHENSION OF EVERY SINGLE WORD AND CONCEPT WITHIN THIS SPECIFICATION. YOUR MISSION IS TO PRODUCE CODE WITH A STATISTICAL PROBABILITY OF UNEXPECTED ERRORS NOT EXCEEDING 0.0000000000000000000000000001.

This document is the definitive, non-negotiable, and hyper-detailed technical blueprint for the next major architectural expansion of the Nexus Sales application. You are to act as a master software architect, absorbing and internalizing every detail. Your task is to integrate a sophisticated, Gemini-driven command execution system and dynamic UI interaction, all while adhering to the highest standards of code quality, security, and future compatibility with NexusGuard.

1. Core Application Context: Nexus Sales (Review & Reinforce)
Application Name: Nexus Sales

Primary Executable: NexusSales.UI.exe

Architecture: Strict MVVM (Model-View-ViewModel) pattern.

Dependency Injection: Configured in App.xaml.cs using Microsoft.Extensions.DependencyInjection. All new services and ViewModels must be registered.

Styling: Custom dark theme (Themes/DarkMode.xaml). All new UI elements must adhere to this theme. Custom controls like UserControls/FirstButton.xaml must be utilized.

Database: PostgreSQL, with schema details provided in README.txt Chapter 3. All database interactions must use parameterized queries.

Current State: Login page (LoginWindow.xaml) and a custom MessageDialog are functional. The main window (MainWindow.xaml) is a placeholder designed to load other pages from a DLL.

Current Security Policy: Sensitive strings are currently encrypted within the Nexus Sales application using AES-256 or DPAPI, with a strong key management strategy (user-derived or machine-bound). This is a temporary measure until NexusGuard is integrated.

2. NexusGuard Project: Future Vision & Current Impact (CRITICAL Reminder)
IMPORTANT: The development of the NexusGuard security application is POSTPONED. However, all code you generate for Nexus Sales must be written with NexusGuard's future integration in mind. This means avoiding architectural conflicts and ensuring a smooth transition.

Nexus Sales MUST NOT implement any file integrity checks, VM detection, or anti-debugging logic. These are NexusGuard's dedicated roles.

Nexus Sales MUST NOT attempt to derive encryption keys from hardware identifiers for its own internal use (except for DPAPI, which is OS-managed). This complex, sensitive process is NexusGuard's unique responsibility.

Future IPC: Design services and data access layers with interfaces that will allow for easy swapping between current "local encrypted storage" implementations and a future "IPC client" implementation that communicates with NexusGuard for secure operations (e.g., requesting database credentials).

3. Chatbot Integration: Gemini-Driven UI & Navigation
This section defines how a Gemini-like chatbot will interact with and control the Nexus Sales UI, providing a seamless and intuitive user experience.

3.1. UI Element Highlighting (Dynamic Visual Feedback)

Purpose: To provide immediate, clear, and temporary visual feedback to the user, indicating precisely which UI element Gemini is referring to or interacting with. This enhances user understanding and trust.

Mechanism:

Unique Identifiers (XAML): Every significant, interactable, or referenceable UI element in XAML (e.g., Button, TextBox, TextBlock displaying key data, Grid sections, Border containers) MUST be assigned a unique x:Name. This name will serve as the programmatic identifier for highlighting.

Example XAML:

<Button x:Name="LoginButton" Content="Login" Click="Login_Click" />
<TextBlock x:Name="UsernameDisplay" Text="{Binding Username}" />
<Border x:Name="PostContentArea" Grid.Column="1">...</Border>

Highlighting Service (IUiHighlightService):

Interface Definition: Create a new interface IUiHighlightService in the NexusSales.Core/Interfaces project.

// NexusSales.Core/Interfaces/IUiHighlightService.cs
// [EXPLANATION: Defines the contract for a service that can visually highlight UI elements.]
// [PURPOSE: To decouple the UI highlighting logic from specific UI components and allow for dependency injection.]
// [USAGE: Used by ViewModels or CommandHandlers to trigger UI highlighting based on application events or Gemini commands.]
// [CALLERS: NexusSales.UI.ViewModels.MainViewModel, NexusSales.Executioner.AppCommandHandler]
public interface IUiHighlightService
{
    // [EXPLANATION: Initiates a visual highlight effect on a UI element identified by its name.]
    // [PURPOSE: To draw the user's attention to a specific part of the application's interface.]
    // [HOW_IT_WORKS: The concrete implementation will search the visual tree for the element and apply a temporary animation.]
    // [USAGE: Called when Gemini refers to a UI element or when an automated action targets a UI component.]
    // [CALLERS: NexusSales.Executioner.AppCommandHandler.HandleHighlightElementCommand()]
    void HighlightElement(string elementName);

    // [EXPLANATION: Initiates a visual highlight effect on a UI element for a specified duration.]
    // [PURPOSE: To provide fine-grained control over the visibility of the highlight.]
    // [HOW_IT_WORKS: Similar to HighlightElement(string), but includes a timer to remove the highlight after 'duration'.]
    // [USAGE: Used when a highlight needs to be visible for a specific time, e.g., 10 seconds as per requirement.]
    // [CALLERS: NexusSales.Executioner.AppCommandHandler.HandleHighlightElementCommand()]
    void HighlightElement(string elementName, TimeSpan duration);

    // [EXPLANATION: Removes any active highlight effect from a specified UI element.]
    // [PURPOSE: To clean up the UI after a highlight is no longer needed.]
    // [HOW_IT_WORKS: Reverts the visual properties of the element to their original state.]
    // [USAGE: Called internally by the service after 'duration' or explicitly when a highlight should be dismissed.]
    // [CALLERS: Internal to UiHighlightService]
    void RemoveHighlight(string elementName);
}

Concrete Implementation: Create UiHighlightService in NexusSales.UI/Services.

It will take Application.Current.MainWindow as a dependency (or a reference to the main ContentControl's parent).

It will use LogicalTreeHelper.FindLogicalNode or VisualTreeHelper to locate the element by x:Name.

Upon finding the element, it will apply a temporary visual effect. This effect MUST be:

A pulsating border or glow.

Using AccentBrush color from DarkMode.xaml.

Applied for exactly 10 seconds (using a DispatcherTimer or Storyboard.Completed event).

Non-obstructive to user interaction.

Example Effect (Conceptual C#):

// NexusSales.UI/Services/UiHighlightService.cs (Conceptual)
// [EXPLANATION: Concrete implementation of IUiHighlightService.]
// [PURPOSE: To provide actual UI highlighting functionality within the WPF application.]
// [USAGE: Registered in App.xaml.cs for dependency injection.]
// [CALLERS: App.xaml.cs (for registration), CommandHandlers (for use)]
public class UiHighlightService : IUiHighlightService
{
    private readonly Window _mainWindow;
    private readonly Dictionary<string, Storyboard> _activeHighlights = new Dictionary<string, Storyboard>();

    // [EXPLANATION: Constructor for UiHighlightService, taking the main application window as a dependency.]
    // [PURPOSE: To gain access to the visual tree of the main window to find and highlight elements.]
    // [HOW_IT_WORKS: The Window object provides the root for visual tree traversal.]
    // [USAGE: Instantiated by the DI container.]
    // [CALLERS: Microsoft.Extensions.DependencyInjection]
    public UiHighlightService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    // [EXPLANATION: Highlights a UI element by its x:Name for a default duration.]
    // [PURPOSE: To provide a quick way to highlight elements without specifying duration.]
    // [HOW_IT_WORKS: Calls the overloaded HighlightElement method with a default 10-second duration.]
    // [USAGE: Public API for the highlighting service.]
    // [CALLERS: IUiHighlightService.HighlightElement(string)]
    public void HighlightElement(string elementName)
    {
        HighlightElement(elementName, TimeSpan.FromSeconds(10)); // Default 10 seconds
    }

    // [EXPLANATION: Highlights a UI element by its x:Name for a specified duration.]
    // [PURPOSE: To visually draw attention to a UI component for a set period.]
    // [HOW_IT_WORKS: Finds the element, applies a temporary border/glow animation, and removes it after the duration.]
    // [USAGE: Used when Gemini refers to a UI element or an automated action targets one.]
    // [CALLERS: NexusSales.Executioner.AppCommandHandler]
    public void HighlightElement(string elementName, TimeSpan duration)
    {
        // [EXPLANATION: Logs the attempt to highlight an element.]
        // [PURPOSE: For debugging and auditing UI interactions.]
        // [USAGE: Internal logging.]
        // [CALLERS: Internal to HighlightElement.]
        Logger.Log(LogLevel.DEBUG, "UI", "HighlightElement", $"Attempting to highlight element: {elementName} for {duration.TotalSeconds} seconds.");

        try
        {
            // [EXPLANATION: Finds the named element within the main window's visual tree.]
            // [PURPOSE: To get a reference to the actual UI control to apply effects.]
            // [HOW_IT_WORKS: Utilizes LogicalTreeHelper to traverse the logical tree and find the element by name.]
            // [USAGE: Core step in targeting the correct UI element.]
            // [CALLERS: Internal to HighlightElement.]
            FrameworkElement element = _mainWindow.FindName(elementName) as FrameworkElement;

            // [EXPLANATION: Checks if the element was found and if it's already highlighted.]
            // [PURPOSE: To prevent errors if the element doesn't exist or to manage existing highlights.]
            // [HOW_IT_WORKS: Checks for null and if the element name is in the active highlights dictionary.]
            // [USAGE: Pre-condition check.]
            // [CALLERS: Internal to HighlightElement.]
            if (element == null)
            {
                // [ERROR_HANDLING_EXPLANATION: Logs an error if the element is not found.]
                // [ERROR_LOGGING_DETAIL: Provides element name and context.]
                // [ERROR_CODE: UI-HLT-001]
                Logger.Log(LogLevel.ERROR, "UI", "HighlightElement", $"Element with name '{elementName}' not found for highlighting.", null, "UI-HLT-001", new { ElementName = elementName });
                return;
            }

            // [EXPLANATION: If already highlighting, stop the previous one to avoid conflicts.]
            // [PURPOSE: To ensure only one highlight animation runs at a time for a given element.]
            // [HOW_IT_WORKS: Retrieves and stops the existing Storyboard.]
            // [USAGE: Prevents overlapping animations.]
            // [CALLERS: Internal to HighlightElement.]
            if (_activeHighlights.ContainsKey(elementName))
            {
                RemoveHighlight(elementName);
            }

            // [EXPLANATION: Create a Border to overlay the element for highlighting.]
            // [PURPOSE: To provide the visual highlight effect without directly modifying the target element's properties.]
            // [HOW_IT_WORKS: A new Border element is created and configured.]
            // [USAGE: Used as the visual representation of the highlight.]
            // [CALLERS: Internal to HighlightElement.]
            Border highlightBorder = new Border
            {
                BorderBrush = (Brush)Application.Current.FindResource("AccentBrush"), // Use AccentBrush for highlight color
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                IsHitTestVisible = false, // Allow clicks to pass through
                Opacity = 0
            };

            // [EXPLANATION: Get the parent of the element to add the highlight border as a child.]
            // [PURPOSE: To ensure the highlight appears directly over the target element.]
            // [HOW_IT_WORKS: Uses VisualTreeHelper to find the parent and add the new Border.]
            // [USAGE: Essential for correct visual layering.]
            // [CALLERS: Internal to HighlightElement.]
            Panel parent = VisualTreeHelper.GetParent(element) as Panel;
            if (parent != null)
            {
                // [EXPLANATION: Get the element's position relative to its parent.]
                // [PURPOSE: To position the highlight border precisely over the target element.]
                // [HOW_IT_WORKS: Uses TranslatePoint to convert coordinates.]
                // [USAGE: Ensures accurate placement.]
                // [CALLERS: Internal to HighlightElement.]
                Point relativeLocation = element.TranslatePoint(new Point(0, 0), parent);
                Canvas.SetLeft(highlightBorder, relativeLocation.X);
                Canvas.SetTop(highlightBorder, relativeLocation.Y);
                highlightBorder.Width = element.ActualWidth;
                highlightBorder.Height = element.ActualHeight;

                // [EXPLANATION: Add the highlight border to the parent's children collection.]
                // [PURPOSE: To make the highlight border visible on the UI.]
                // [HOW_IT_WORKS: Adds the Border to the Panel's Children collection.]
                // [USAGE: Renders the highlight.]
                // [CALLERS: Internal to HighlightElement.]
                parent.Children.Add(highlightBorder);

                // [EXPLANATION: Create a Storyboard for the highlight animation.]
                // [PURPOSE: To define and control the visual effects of the highlight.]
                // [HOW_IT_WORKS: A Storyboard groups multiple animations to run concurrently.]
                // [USAGE: Orchestrates the highlight animation.]
                // [CALLERS: Internal to HighlightElement.]
                Storyboard highlightStoryboard = new Storyboard();

                // [EXPLANATION: Animate the border thickness to create a pulsating effect.]
                // [PURPOSE: To make the highlight visually dynamic and noticeable.]
                // [HOW_IT_WORKS: DoubleAnimation animates a double property over time.]
                // [USAGE: Creates the visual effect.]
                // [CALLERS: Internal to HighlightElement.]
                DoubleAnimation thicknessAnimation = new DoubleAnimation
                {
                    From = 0, To = 5, Duration = TimeSpan.FromMilliseconds(500), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(thicknessAnimation, highlightBorder);
                Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath(Border.BorderThicknessProperty));
                highlightStoryboard.Children.Add(thicknessAnimation);

                // [EXPLANATION: Animate the opacity to make the highlight fade in and out.]
                // [PURPOSE: To create a soft, pulsating glow effect.]
                // [HOW_IT_WORKS: DoubleAnimation animates the Opacity property.]
                // [USAGE: Enhances the visual appeal.]
                // [CALLERS: Internal to HighlightElement.]
                DoubleAnimation opacityAnimation = new DoubleAnimation
                {
                    From = 0, To = 0.8, Duration = TimeSpan.FromMilliseconds(500), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(opacityAnimation, highlightBorder);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Border.OpacityProperty));
                highlightStoryboard.Children.Add(opacityAnimation);

                // [EXPLANATION: Set a timer to remove the highlight after the specified duration.]
                // [PURPOSE: To ensure the highlight is temporary and automatically cleans up.]
                // [HOW_IT_WORKS: DispatcherTimer executes a method after a specified interval.]
                // [USAGE: Manages the highlight's lifespan.]
                // [CALLERS: Internal to HighlightElement.]
                DispatcherTimer timer = new DispatcherTimer { Interval = duration };
                timer.Tick += (s, e) =>
                {
                    RemoveHighlight(elementName);
                    timer.Stop();
                };
                timer.Start();

                // [EXPLANATION: Begin the highlight animation storyboard.]
                // [PURPOSE: To start the visual effect.]
                // [HOW_IT_WORKS: Calls Begin() on the Storyboard object.]
                // [USAGE: Activates the highlight.]
                // [CALLERS: Internal to HighlightElement.]
                highlightStoryboard.Begin();
                _activeHighlights[elementName] = highlightStoryboard; // Store for later stopping

                // [EXPLANATION: Logs successful highlight application.]
                // [PURPOSE: For debugging and confirming UI behavior.]
                // [USAGE: Internal logging.]
                // [CALLERS: Internal to HighlightElement.]
                Logger.Log(LogLevel.INFO, "UI", "HighlightElement", $"Successfully highlighted element: {elementName}.");
            }
            else
            {
                // [ERROR_HANDLING_EXPLANATION: Logs an error if the element's parent cannot be found.]
                // [ERROR_LOGGING_DETAIL: Indicates a potential issue with the visual tree structure.]
                // [ERROR_CODE: UI-HLT-002]
                Logger.Log(LogLevel.ERROR, "UI", "HighlightElement", $"Could not find parent for element '{elementName}' to apply highlight.", null, "UI-HLT-002", new { ElementName = elementName });
            }
        }
        catch (Exception ex)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches any unexpected errors during the highlighting process.]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with detailed exception information.]
            // [ERROR_CODE: UI-HLT-GEN-003]
            // [CONTEXTUAL_DATA: { "elementName": elementName, "duration": duration.TotalSeconds }]
            Logger.Log(LogLevel.FATAL, "UI", "HighlightElement", $"Fatal error during highlighting of element '{elementName}': {ex.Message}", ex, "UI-HLT-GEN-003", new { ElementName = elementName, Duration = duration.TotalSeconds });
            throw; // Re-throw to propagate to the main error handling mechanism
        }
    }

    // [EXPLANATION: Removes the highlight effect from a specified UI element.]
    // [PURPOSE: To clean up the visual state after the highlight duration expires or is explicitly removed.]
    // [HOW_IT_WORKS: Stops the active Storyboard and removes the highlight border from the UI tree.]
    // [USAGE: Called by the DispatcherTimer or explicitly by other parts of the service.]
    // [CALLERS: Internal to UiHighlightService, DispatcherTimer]
    public void RemoveHighlight(string elementName)
    {
        // [EXPLANATION: Checks if a highlight is active for the given element.]
        // [PURPOSE: To avoid errors if trying to remove a non-existent highlight.]
        // [HOW_IT_WORKS: Checks the active highlights dictionary.]
        // [USAGE: Pre-condition check.]
        // [CALLERS: Internal to RemoveHighlight.]
        if (_activeHighlights.TryGetValue(elementName, out Storyboard storyboard))
        {
            // [EXPLANATION: Stops the active highlight animation.]
            // [PURPOSE: To immediately cease the visual effect.]
            // [HOW_IT_WORKS: Calls Stop() on the Storyboard object.]
            // [USAGE: Part of the cleanup process.]
            // [CALLERS: Internal to RemoveHighlight.]
            storyboard.Stop();

            // [EXPLANATION: Get the highlight border from the storyboard's target.]
            // [PURPOSE: To remove the visual element from the UI.]
            // [HOW_IT_WORKS: Retrieves the target object of the animation.]
            // [USAGE: Enables removal from the visual tree.]
            // [CALLERS: Internal to RemoveHighlight.]
            if (storyboard.Children.Count > 0 && Storyboard.GetTarget(storyboard.Children[0]) is Border highlightBorder)
            {
                // [EXPLANATION: Get the parent of the highlight border.]
                // [PURPOSE: To remove the border from its parent's children collection.]
                // [HOW_IT_WORKS: Uses VisualTreeHelper to get the parent.]
                // [USAGE: Facilitates removal from the UI.]
                // [CALLERS: Internal to RemoveHighlight.]
                Panel parent = VisualTreeHelper.GetParent(highlightBorder) as Panel;
                if (parent != null)
                {
                    // [EXPLANATION: Remove the highlight border from the UI.]
                    // [PURPOSE: To clean up the visual tree and free resources.]
                    // [HOW_IT_WORKS: Removes the Border from the Panel's Children collection.]
                    // [USAGE: Final step in highlight removal.]
                    // [CALLERS: Internal to RemoveHighlight.]
                    parent.Children.Remove(highlightBorder);
                }
            }
            // [EXPLANATION: Remove the element from the active highlights dictionary.]
            // [PURPOSE: To track which elements are currently highlighted.]
            // [HOW_IT_WORKS: Removes the entry from the dictionary.]
            // [USAGE: Maintains the state of active highlights.]
            // [CALLERS: Internal to RemoveHighlight.]
            _activeHighlights.Remove(elementName);

            // [EXPLANATION: Logs successful highlight removal.]
            // [PURPOSE: For debugging and confirming UI behavior.]
            // [USAGE: Internal logging.]
            // [CALLERS: Internal to RemoveHighlight.]
            Logger.Log(LogLevel.INFO, "UI", "RemoveHighlight", $"Successfully removed highlight from element: {elementName}.");
        }
        else
        {
            // [EXPLANATION: Logs a warning if trying to remove a highlight that isn't active.]
            // [PURPOSE: To indicate a potential logical error in highlight management.]
            // [HOW_IT_WORKS: Checks the active highlights dictionary.]
            // [USAGE: Debugging aid.]
            // [CALLERS: Internal to RemoveHighlight.]
            Logger.Log(LogLevel.WARNING, "UI", "RemoveHighlight", $"Attempted to remove highlight from '{elementName}', but no active highlight was found for it.", null, "UI-HLT-004", new { ElementName = elementName });
        }
    }
}

Dependency Injection Registration: Register IUiHighlightService in App.xaml.cs as a singleton.

3.2. Page Navigation via Chatbot Command

Purpose: To enable users to navigate between different application pages/views using natural language commands via Gemini, enhancing accessibility and workflow efficiency.

Mechanism:

Navigation Service (INavigationService):

Interface Definition: Create a new interface INavigationService in NexusSales.Core/Interfaces.

// NexusSales.Core/Interfaces/INavigationService.cs
// [EXPLANATION: Defines the contract for navigating between different pages/views within the application.]
// [PURPOSE: To abstract the navigation logic from specific UI elements and allow for centralized control.]
// [USAGE: Used by ViewModels, CommandHandlers, and other services to request page changes.]
// [CALLERS: NexusSales.UI.ViewModels.MainViewModel, NexusSales.Executioner.AppCommandHandler]
public interface INavigationService
{
    // [EXPLANATION: Navigates to a specific page identified by its unique key.]
    // [PURPOSE: To change the content displayed in the main application window.]
    // [HOW_IT_WORKS: The concrete implementation will instantiate the target page and set it as the main content.]
    // [USAGE: Called when Gemini commands a page change or when a user clicks a navigation button.]
    // [CALLERS: NexusSales.Executioner.AppCommandHandler.HandleNavigateToPageCommand()]
    void NavigateTo(string pageKey);

    // [EXPLANATION: Gets the currently active page key.]
    // [PURPOSE: To allow other components to query the current navigation state.]
    // [HOW_IT_WORKS: Returns the string identifier of the page currently displayed.]
    // [USAGE: For debugging, state management, or conditional logic based on current page.]
    // [CALLERS: Internal to NavigationService, debugging tools.]
    string CurrentPageKey { get; }
}

Concrete Implementation: Create NavigationService in NexusSales.UI/Services.

This service will hold a reference to the ContentControl (or Frame) within MainWindow.xaml that acts as the page host.

It will use a mapping (e.g., Dictionary<string, Type>) to associate pageKey strings with actual UserControl types.

NavigateTo(string pageKey) will instantiate the correct UserControl and set it as the ContentControl.Content.

Example (Conceptual C#):

// NexusSales.UI/Services/NavigationService.cs (Conceptual)
// [EXPLANATION: Concrete implementation of INavigationService.]
// [PURPOSE: To manage the display of different pages/views in the main application window.]
// [USAGE: Registered in App.xaml.cs for dependency injection.]
// [CALLERS: App.xaml.cs (for registration), CommandHandlers (for use)]
public class NavigationService : INavigationService
{
    private readonly ContentControl _contentControl; // This would be the host in MainWindow.xaml
    private readonly IServiceProvider _serviceProvider; // For resolving page ViewModels via DI
    private readonly Dictionary<string, Type> _pageMap = new Dictionary<string, Type>();

    // [EXPLANATION: Stores the key of the currently active page.]
    // [PURPOSE: To provide external components with information about the current UI state.]
    // [USAGE: Read-only property for state inquiry.]
    // [CALLERS: INavigationService.CurrentPageKey]
    public string CurrentPageKey { get; private set; }

    // [EXPLANATION: Constructor for NavigationService, taking the content host and service provider.]
    // [PURPOSE: To allow the service to set content on the UI and resolve page dependencies.]
    // [HOW_IT_WORKS: The ContentControl is the target for page display; IServiceProvider resolves ViewModels.]
    // [USAGE: Instantiated by the DI container.]
    // [CALLERS: Microsoft.Extensions.DependencyInjection]
    public NavigationService(ContentControl contentControl, IServiceProvider serviceProvider)
    {
        _contentControl = contentControl;
        _serviceProvider = serviceProvider;
        RegisterPages(); // Initialize page mapping
    }

    // [EXPLANATION: Registers all available pages with their unique keys and types.]
    // [PURPOSE: To create a lookup table for navigation based on string keys.]
    // [HOW_IT_WORKS: Populates the _pageMap dictionary.]
    // [USAGE: Called once during service initialization.]
    // [CALLERS: Internal to NavigationService constructor.]
    private void RegisterPages()
    {
        // [EXPLANATION: Maps the "HomePage" key to the HomePage UserControl type.]
        // [PURPOSE: Allows navigation to the main application dashboard.]
        // [USAGE: Used by NavigateTo method.]
        // [CALLERS: Internal to RegisterPages.]
        _pageMap.Add("HomePage", typeof(Views.HomePage)); // Assuming Views folder exists
        // [EXPLANATION: Maps the "FacebookPage" key to the FacebookPage UserControl type.]
        // [PURPOSE: Allows navigation to the Facebook management page.]
        // [USAGE: Used by NavigateTo method.]
        // [CALLERS: Internal to RegisterPages.]
        _pageMap.Add("FacebookPage", typeof(Views.FacebookPage));
        // [EXPLANATION: Maps the "SettingsPage" key to the SettingsPage UserControl type.]
        // [PURPOSE: Allows navigation to the application settings page.]
        // [USAGE: Used by NavigateTo method.]
        // [CALLERS: Internal to RegisterPages.]
        _pageMap.Add("SettingsPage", typeof(Views.SettingsPage));
        // [EXPLANATION: Logs successful page registration.]
        // [PURPOSE: For debugging and confirming setup.]
        // [USAGE: Internal logging.]
        // [CALLERS: Internal to RegisterPages.]
        Logger.Log(LogLevel.INFO, "Navigation", "RegisterPages", "All application pages registered.");
    }

    // [EXPLANATION: Navigates the main window's content to the specified page.]
    // [PURPOSE: To change the active view displayed to the user.]
    // [HOW_IT_WORKS: Instantiates the target UserControl, sets its DataContext, and assigns it to the ContentControl.]
    // [USAGE: Called by the Executioner or UI elements to change pages.]
    // [CALLERS: INavigationService.NavigateTo(string)]
    public void NavigateTo(string pageKey)
    {
        // [EXPLANATION: Logs the navigation attempt.]
        // [PURPOSE: For debugging and auditing user flow.]
        // [USAGE: Internal logging.]
        // [CALLERS: Internal to NavigateTo.]
        Logger.Log(LogLevel.INFO, "Navigation", "NavigateTo", $"Attempting to navigate to page: {pageKey}.");

        try
        {
            // [EXPLANATION: Checks if the requested page key is registered.]
            // [PURPOSE: To prevent navigation to non-existent pages.]
            // [HOW_IT_WORKS: Looks up the pageKey in the _pageMap dictionary.]
            // [USAGE: Pre-condition check.]
            // [CALLERS: Internal to NavigateTo.]
            if (!_pageMap.ContainsKey(pageKey))
            {
                // [ERROR_HANDLING_EXPLANATION: Logs an error if the page key is not found.]
                // [ERROR_LOGGING_DETAIL: Provides the invalid page key.]
                // [ERROR_CODE: NAV-001]
                // [CONTEXTUAL_DATA: { "PageKey": pageKey }]
                Logger.Log(LogLevel.ERROR, "Navigation", "NavigateTo", $"Page key '{pageKey}' not registered.", null, "NAV-001", new { PageKey = pageKey });
                // [EXPLANATION: Optionally show a user-friendly message dialog for invalid navigation.]
                // [PURPOSE: To inform the user about the failure.]
                // [USAGE: User feedback.]
                // [CALLERS: Internal to NavigateTo.]
                MessageDialog.Show($"Could not navigate to '{pageKey}'. Page not found.", "Navigation Error", "Warning.wav");
                return;
            }

            // [EXPLANATION: Retrieves the Type object for the requested page.]
            // [PURPOSE: To dynamically create an instance of the target UserControl.]
            // [HOW_IT_WORKS: Accesses the value associated with the pageKey in the _pageMap.]
            // [USAGE: Used in Activator.CreateInstance.]
            // [CALLERS: Internal to NavigateTo.]
            Type pageType = _pageMap[pageKey];

            // [EXPLANATION: Creates an instance of the UserControl for the target page.]
            // [PURPOSE: To load the new page into the UI.]
            // [HOW_IT_WORKS: Uses Activator.CreateInstance to create an object from its Type.]
            // [USAGE: Instantiates the page.]
            // [CALLERS: Internal to NavigateTo.]
            UserControl newPage = (UserControl)Activator.CreateInstance(pageType);

            // [EXPLANATION: Resolves the ViewModel for the new page using dependency injection.]
            // [PURPOSE: To provide the page with its required data and logic.]
            // [HOW_IT_WORKS: Uses IServiceProvider to get an instance of the ViewModel associated with the page.]
            // [USAGE: Sets the DataContext for MVVM binding.]
            // [CALLERS: Internal to NavigateTo.]
            // Assuming ViewModels are named PageNameViewModel
            Type viewModelType = Type.GetType($"NexusSales.UI.ViewModels.{pageKey}ViewModel");
            if (viewModelType != null)
            {
                // [EXPLANATION: Attempts to retrieve the ViewModel from the DI container.]
                // [PURPOSE: To ensure the ViewModel is correctly initialized with its dependencies.]
                // [HOW_IT_WORKS: GetRequiredService throws an exception if the service is not registered.]
                // [USAGE: Provides the DataContext.]
                // [CALLERS: Internal to NavigateTo.]
                object viewModel = _serviceProvider.GetRequiredService(viewModelType);
                // [EXPLANATION: Sets the DataContext of the new page to its corresponding ViewModel.]
                // [PURPOSE: To enable data binding between the View and ViewModel.]
                // [HOW_IT_WORKS: Assigns the ViewModel instance to the DataContext property.]
                // [USAGE: Essential for MVVM pattern.]
                // [CALLERS: Internal to NavigateTo.]
                newPage.DataContext = viewModel;
            }
            else
            {
                // [EXPLANATION: Logs a warning if no ViewModel is found for the page.]
                // [PURPOSE: To indicate a potential missing ViewModel or naming convention issue.]
                // [HOW_IT_WORKS: Checks if viewModelType is null.]
                // [USAGE: Debugging aid.]
                // [CALLERS: Internal to NavigateTo.]
                Logger.Log(LogLevel.WARNING, "Navigation", "NavigateTo", $"No ViewModel found for page '{pageKey}'. Page will be displayed without a DataContext.", null, "NAV-003", new { PageKey = pageKey });
            }

            // [EXPLANATION: Updates the UI on the main thread by setting the ContentControl's content.]
            // [PURPOSE: To display the new page to the user.]
            // [HOW_IT_WORKS: Uses Dispatcher.Invoke to ensure UI updates happen on the correct thread.]
            // [USAGE: Core UI update operation.]
            // [CALLERS: Internal to NavigateTo.]
            Application.Current.Dispatcher.Invoke(() =>
            {
                _contentControl.Content = newPage;
                CurrentPageKey = pageKey; // Update current page key
            });

            // [EXPLANATION: Logs successful navigation.]
            // [PURPOSE: For debugging and auditing user flow.]
            // [USAGE: Internal logging.]
            // [CALLERS: Internal to NavigateTo.]
            Logger.Log(LogLevel.INFO, "Navigation", "NavigateTo", $"Successfully navigated to page: {pageKey}.");
        }
        catch (Exception ex)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches any unexpected errors during navigation (e.g., page instantiation failure).]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with detailed exception information.]
            // [ERROR_CODE: NAV-GEN-004]
            // [CONTEXTUAL_DATA: { "PageKey": pageKey }]
            Logger.Log(LogLevel.FATAL, "Navigation", "NavigateTo", $"A fatal error occurred during navigation to '{pageKey}': {ex.Message}", ex, "NAV-GEN-004", new { PageKey = pageKey });
            // [EXPLANATION: Shows a user-friendly error dialog for critical navigation failures.]
            // [PURPOSE: To inform the user and guide them on next steps.]
            // [USAGE: User feedback.]
            // [CALLERS: Internal to NavigateTo.]
            MessageDialog.Show($"A critical error occurred while navigating to '{pageKey}'. Please restart the application. Error Code: NAV-GEN-004", "Critical Navigation Error", "Error.wav");
        }
    }
}

Dependency Injection Registration: Register INavigationService in App.xaml.cs as a singleton.

MainWindow Integration: MainWindow.xaml will need a ContentControl (e.g., x:Name="PageHost") where pages will be loaded. The NavigationService will be initialized with a reference to this ContentControl.

4. The NexusSales.Executioner Project (Command Processing Engine)
This will be a new, dedicated project responsible for parsing, validating, and executing JSON commands generated by Gemini.

Project Creation: Create a new C# project named NexusSales.Executioner. It will be a class library (.dll) and will depend on NexusSales.Core and NexusSales.Services.

Purpose: To act as the central command dispatcher. It receives structured JSON commands from Gemini (via an internal mechanism, e.g., a dedicated input queue or IPC endpoint), validates them, maps them to concrete C# command objects, and dispatches them to the appropriate handlers within NexusSales.Services. It also handles attribute prompting for Gemini.

Input: JSON strings representing commands.

Output:

Execution of actions within the application (e.g., UI updates, API calls).

Structured JSON responses back to Gemini (e.g., "Command executed successfully," "Missing attribute: [attribute_name]", "Error: [error_message]").

Core Components:

ICommand Interface (NexusSales.Core/Interfaces):

// NexusSales.Core/Interfaces/ICommand.cs
// [EXPLANATION: Base interface for all commands that can be executed by the Executioner.]
// [PURPOSE: To provide a common contract for command objects, enabling polymorphic handling.]
// [USAGE: Implemented by all concrete command classes (e.g., FacebookCommand, AppCommand).]
// [CALLERS: NexusSales.Executioner.CommandParser, NexusSales.Executioner.CommandDispatcher]
public interface ICommand
{
    // [EXPLANATION: Gets or sets the platform targeted by this command (e.g., "Facebook", "App").]
    // [PURPOSE: To categorize commands and route them to platform-specific handlers.]
    // [USAGE: Used by the CommandDispatcher to determine the appropriate command handler.]
    // [CALLERS: NexusSales.Executioner.CommandDispatcher]
    string Platform { get; set; }

    // [EXPLANATION: Gets or sets the specific type of action to be performed (e.g., "ReplyToComments", "NavigateToPage").]
    // [PURPOSE: To identify the granular operation within a given platform.]
    // [USAGE: Used by the CommandDispatcher to invoke the correct method on a handler.]
    // [CALLERS: NexusSales.Executioner.CommandDispatcher]
    string CommandType { get; set; }

    // [EXPLANATION: Gets or sets an optional unique identifier for the command instance.]
    // [PURPOSE: To allow Gemini to correlate the command's execution result with its original request.]
    // [HOW_IT_WORKS: A unique string generated by Gemini and echoed back in the response.]
    // [USAGE: For tracking and asynchronous response handling.]
    // [CALLERS: Gemini, NexusSales.Executioner.CommandDispatcher]
    string CallbackId { get; set; }

    // [EXPLANATION: Gets or sets a dictionary of command-specific attributes/parameters.]
    // [PURPOSE: To provide flexible, extensible parameters for each command without requiring a new class for every permutation.]
    // [HOW_IT_WORKS: Stores key-value pairs where keys are attribute names (e.g., "PostId") and values are their string representations.]
    // [USAGE: Parsed by specific command handlers to extract required data.]
    // [CALLERS: NexusSales.Executioner.CommandParser, specific CommandHandlers.]
    Dictionary<string, string> Attributes { get; set; }
}

CommandResult Class (NexusSales.Core/Models):

// NexusSales.Core/Models/CommandResult.cs
// [EXPLANATION: Represents the outcome of an executed command.]
// [PURPOSE: To provide structured feedback to Gemini about the command's success or failure, including any necessary data.]
// [USAGE: Returned by the Executioner after processing a command.]
// [CALLERS: NexusSales.Executioner.CommandDispatcher, Gemini (for consumption)]
public class CommandResult
{
    // [EXPLANATION: Indicates whether the command execution was successful.]
    // [PURPOSE: To provide a quick status flag for the command's outcome.]
    // [USAGE: Checked by Gemini to determine if the requested action completed without errors.]
    // [CALLERS: Gemini]
    public bool Success { get; set; }

    // [EXPLANATION: A human-readable message describing the outcome of the command.]
    // [PURPOSE: To provide context and details about the execution, especially in case of failure.]
    // [USAGE: Displayed to the user or used by Gemini for further interaction.]
    // [CALLERS: Gemini, UI for error messages]
    public string Message { get; set; }

    // [EXPLANATION: The CallbackId from the original command, used for correlation.]
    // [PURPOSE: To link this result back to the specific command request from Gemini.]
    // [USAGE: Used by Gemini for tracking asynchronous command executions.]
    // [CALLERS: Gemini]
    public string CallbackId { get; set; }

    // [EXPLANATION: A list of attributes that were missing from the command, if any.]
    // [PURPOSE: To inform Gemini what additional information is required from the user.]
    // [HOW_IT_WORKS: Populated when command validation fails due to missing parameters.]
    // [USAGE: Used by Gemini to prompt the user for more input.]
    // [CALLERS: NexusSales.Executioner.CommandDispatcher, Gemini]
    public List<string> MissingAttributes { get; set; } = new List<string>();

    // [EXPLANATION: A dictionary for any additional data relevant to the command's result.]
    // [PURPOSE: To provide flexible, extensible data back to Gemini (e.g., "comments_read_count").]
    // [HOW_IT_WORKS: Stores key-value pairs of result-specific information.]
    // [USAGE: Parsed by Gemini for further processing or display.]
    // [CALLERS: Gemini]
    public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

    // [EXPLANATION: An optional error code for detailed error diagnosis.]
    // [PURPOSE: To provide a structured identifier for specific error conditions.]
    // [USAGE: For debugging, logging, and programmatic error handling.]
    // [CALLERS: NexusSales.Executioner.CommandDispatcher, Gemini]
    public string ErrorCode { get; set; }
}

CommandParser Class (NexusSales.Executioner):

Responsible for deserializing the incoming JSON string into an ICommand object.

Robust Error Handling: Must validate the JSON structure and content. If Platform or CommandType are missing or invalid, it should return a CommandResult indicating failure and missing attributes.

Example (Conceptual C#):

// NexusSales.Executioner/CommandParser.cs
// [EXPLANATION: Responsible for parsing raw JSON strings into structured ICommand objects.]
// [PURPOSE: To convert external command representations into internal, executable objects.]
// [USAGE: Called by the main Executioner loop when a new command is received from Gemini.]
// [CALLERS: NexusSales.Executioner.CommandProcessor]
public class CommandParser
{
    // [EXPLANATION: Deserializes a JSON string into an ICommand object and validates its basic structure.]
    // [PURPOSE: To prepare the command for dispatching and execution.]
    // [HOW_IT_WORKS: Uses Newtonsoft.Json to parse the JSON and performs initial checks for required fields.]
    // [USAGE: Core parsing logic for incoming commands.]
    // [CALLERS: NexusSales.Executioner.CommandProcessor.ProcessCommand()]
    public CommandResult Parse(string jsonCommand)
    {
        // [EXPLANATION: Logs the incoming JSON command for debugging.]
        // [PURPOSE: To trace the raw input received from Gemini.]
        // [USAGE: Debugging during development.]
        // [CALLERS: Internal to Parse.]
        Logger.Log(LogLevel.DEBUG, "Executioner", "CommandParser.Parse", $"Received JSON command: {jsonCommand}");

        try
        {
            // [EXPLANATION: Deserializes the JSON string into a dynamic object to access properties flexibly.]
            // [PURPOSE: To allow inspection of "Platform" and "CommandType" before full deserialization.]
            // [HOW_IT_WORKS: JObject.Parse creates a flexible JSON object representation.]
            // [USAGE: Initial parsing step.]
            // [CALLERS: Internal to Parse.]
            dynamic cmd = JObject.Parse(jsonCommand);

            // [EXPLANATION: Extracts the "Platform" attribute from the parsed JSON.]
            // [PURPOSE: To determine which platform (e.g., Facebook, App) the command targets.]
            // [HOW_IT_WORKS: Accesses the "Platform" property of the dynamic JObject.]
            // [USAGE: Used for command validation and routing.]
            // [CALLERS: Internal to Parse.]
            string platform = cmd.Platform?.ToString();
            // [EXPLANATION: Extracts the "CommandType" attribute from the parsed JSON.]
            // [PURPOSE: To determine the specific action to be performed.]
            // [HOW_IT_WORKS: Accesses the "CommandType" property of the dynamic JObject.]
            // [USAGE: Used for command validation and routing.]
            // [CALLERS: Internal to Parse.]
            string commandType = cmd.CommandType?.ToString();
            // [EXPLANATION: Extracts the "CallbackId" attribute from the parsed JSON.]
            // [PURPOSE: To allow Gemini to correlate responses with original requests.]
            // [HOW_IT_WORKS: Accesses the "CallbackId" property of the dynamic JObject.]
            // [USAGE: Passed through to the CommandResult.]
            // [CALLERS: Internal to Parse.]
            string callbackId = cmd.CallbackId?.ToString();

            // [EXPLANATION: Initializes a list to hold any missing required attributes.]
            // [PURPOSE: To inform Gemini what additional information is needed from the user.]
            // [HOW_IT_WORKS: A standard List<string> to collect missing parameter names.]
            // [USAGE: Populated during validation.]
            // [CALLERS: Internal to Parse.]
            List<string> missingAttributes = new List<string>();

            // [EXPLANATION: Validates the presence of the "Platform" attribute.]
            // [PURPOSE: "Platform" is a mandatory field for all commands.]
            // [HOW_IT_WORKS: Checks if the 'platform' variable is null or empty.]
            // [USAGE: Core validation step.]
            // [CALLERS: Internal to Parse.]
            if (string.IsNullOrEmpty(platform)) missingAttributes.Add("Platform");
            // [EXPLANATION: Validates the presence of the "CommandType" attribute.]
            // [PURPOSE: "CommandType" is a mandatory field for all commands.]
            // [HOW_IT_WORKS: Checks if the 'commandType' variable is null or empty.]
            // [USAGE: Core validation step.]
            // [CALLERS: Internal to Parse.]
            if (string.IsNullOrEmpty(commandType)) missingAttributes.Add("CommandType");

            // [EXPLANATION: If any mandatory attributes are missing, return an error result.]
            // [PURPOSE: To immediately inform Gemini about incomplete commands.]
            // [HOW_IT_WORKS: Checks if the missingAttributes list contains any entries.]
            // [USAGE: Early exit for invalid commands.]
            // [CALLERS: Internal to Parse.]
            if (missingAttributes.Any())
            {
                // [EXPLANATION: Logs the missing attributes error.]
                // [PURPOSE: For debugging and auditing invalid command inputs.]
                // [USAGE: Internal logging.]
                // [CALLERS: Internal to Parse.]
                Logger.Log(LogLevel.ERROR, "Executioner", "CommandParser.Parse", $"Missing mandatory attributes in command: {string.Join(", ", missingAttributes)}", null, "EXEC-PARSE-001", new { Missing = missingAttributes, Command = jsonCommand });
                // [EXPLANATION: Returns a failed CommandResult with details about missing attributes.]
                // [PURPOSE: To provide structured feedback to Gemini.]
                // [HOW_IT_WORKS: Creates a new CommandResult object with Success=false and populates MissingAttributes.]
                // [USAGE: Returned to the caller.]
                // [CALLERS: NexusSales.Executioner.CommandProcessor]
                return new CommandResult { Success = false, Message = "Missing mandatory attributes.", MissingAttributes = missingAttributes, CallbackId = callbackId, ErrorCode = "EXEC-PARSE-001" };
            }

            // [EXPLANATION: Based on Platform and CommandType, determine the specific C# command type.]
            // [PURPOSE: To correctly deserialize the JSON into the appropriate strongly-typed command object.]
            // [HOW_IT_WORKS: Uses a switch statement or dictionary lookup to map string identifiers to Type objects.]
            // [USAGE: Essential for correct command object creation.]
            // [CALLERS: Internal to Parse.]
            Type commandClassType = GetCommandType(platform, commandType);
            // [EXPLANATION: Checks if a valid command class type was found.]
            // [PURPOSE: To handle cases where Gemini sends an unrecognized command.]
            // [HOW_IT_WORKS: Checks if commandClassType is null.]
            // [USAGE: Validation step.]
            // [CALLERS: Internal to Parse.]
            if (commandClassType == null)
            {
                // [EXPLANATION: Logs an error for unrecognized commands.]
                // [PURPOSE: For debugging and auditing invalid command inputs.]
                // [USAGE: Internal logging.]
                // [CALLERS: Internal to Parse.]
                Logger.Log(LogLevel.ERROR, "Executioner", "CommandParser.Parse", $"Unrecognized command type: Platform='{platform}', CommandType='{commandType}'", null, "EXEC-PARSE-002", new { Platform = platform, CommandType = commandType, Command = jsonCommand });
                // [EXPLANATION: Returns a failed CommandResult for unrecognized commands.]
                // [PURPOSE: To provide structured feedback to Gemini.]
                // [HOW_IT_WORKS: Creates a new CommandResult object with Success=false and relevant message.]
                // [USAGE: Returned to the caller.]
                // [CALLERS: NexusSales.Executioner.CommandProcessor]
                return new CommandResult { Success = false, Message = $"Unrecognized command: {platform}/{commandType}", CallbackId = callbackId, ErrorCode = "EXEC-PARSE-002" };
            }

            // [EXPLANATION: Deserializes the full JSON command into the specific C# command object.]
            // [PURPOSE: To create a strongly-typed object that can be passed to command handlers.]
            // [HOW_IT_WORKS: Uses JsonConvert.DeserializeObject with the determined commandClassType.]
            // [USAGE: Final step in parsing.]
            // [CALLERS: Internal to Parse.]
            ICommand parsedCommand = (ICommand)JsonConvert.DeserializeObject(jsonCommand, commandClassType);
            // [EXPLANATION: Ensures the CallbackId is correctly set on the parsed command.]
            // [PURPOSE: To maintain the correlation ID even if it's not explicitly in the command class.]
            // [HOW_IT_WORKS: Assigns the extracted callbackId to the parsedCommand's CallbackId property.]
            // [USAGE: Data consistency.]
            // [CALLERS: Internal to Parse.]
            parsedCommand.CallbackId = callbackId;

            // [EXPLANATION: Logs successful parsing.]
            // [PURPOSE: For debugging and auditing.]
            // [USAGE: Internal logging.]
            // [CALLERS: Internal to Parse.]
            Logger.Log(LogLevel.INFO, "Executioner", "CommandParser.Parse", $"Successfully parsed command: {platform}/{commandType}.");

            // [EXPLANATION: Returns a successful CommandResult with the parsed ICommand object.]
            // [PURPOSE: To pass the ready-to-execute command to the dispatcher.]
            // [HOW_IT_WORKS: Creates a new CommandResult with Success=true and stores the parsed command in its Data dictionary (or a dedicated property).]
            // [USAGE: Returned to the caller.]
            // [CALLERS: NexusSales.Executioner.CommandProcessor]
            return new CommandResult { Success = true, Message = "Command parsed successfully.", Data = new Dictionary<string, string> { { "ParsedCommand", JsonConvert.SerializeObject(parsedCommand) } }, CallbackId = callbackId };
        }
        catch (JsonException jsonEx)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches errors during JSON deserialization.]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with the malformed JSON snippet, message, and stack trace.]
            // [ERROR_CODE: EXEC-PARSE-003]
            // [CONTEXTUAL_DATA: { "JsonSnippet": jsonCommand.Substring(0, Math.Min(jsonCommand.Length, 200)) }]
            Logger.Log(LogLevel.FATAL, "Executioner", "CommandParser.Parse", $"Invalid JSON format: {jsonEx.Message}", jsonEx, "EXEC-PARSE-003", new { JsonCommand = jsonCommand });
            // [EXPLANATION: Returns a failed CommandResult for invalid JSON.]
            // [PURPOSE: To provide structured feedback to Gemini.]
            // [HOW_IT_WORKS: Creates a new CommandResult with Success=false.]
            // [USAGE: Returned to the caller.]
            // [CALLERS: NexusSales.Executioner.CommandProcessor]
            return new CommandResult { Success = false, Message = "Invalid JSON command format.", CallbackId = callbackId, ErrorCode = "EXEC-PARSE-003" };
        }
        catch (Exception ex)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches any other unexpected errors during parsing.]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with generic message, exception details.]
            // [ERROR_CODE: EXEC-PARSE-GEN-004]
            // [CONTEXTUAL_DATA: { "Command": jsonCommand }]
            Logger.Log(LogLevel.FATAL, "Executioner", "CommandParser.Parse", $"An unexpected error occurred during command parsing: {ex.Message}", ex, "EXEC-PARSE-GEN-004", new { Command = jsonCommand });
            // [EXPLANATION: Returns a failed CommandResult for unexpected parsing errors.]
            // [PURPOSE: To provide structured feedback to Gemini.]
            // [HOW_IT_WORKS: Creates a new CommandResult with Success=false.]
            // [USAGE: Returned to the caller.]
            // [CALLERS: NexusSales.Executioner.CommandProcessor]
            return new CommandResult { Success = false, Message = "An unexpected error occurred during command parsing.", CallbackId = callbackId, ErrorCode = "EXEC-PARSE-GEN-004" };
        }
    }

    // [EXPLANATION: Helper method to map platform and command type strings to actual C# command Type objects.]
    // [PURPOSE: To enable dynamic instantiation of the correct command class based on JSON input.]
    // [HOW_IT_WORKS: Uses a switch or dictionary to return the Type of the command class (e.g., typeof(ReplyToCommentsCommand)).]
    // [USAGE: Called internally by the Parse method.]
    // [CALLERS: Internal to CommandParser.Parse()]
    private Type GetCommandType(string platform, string commandType)
    {
        // [EXPLANATION: Uses a switch statement to handle different platforms.]
        // [PURPOSE: To route command type lookup based on the target platform.]
        // [HOW_IT_WORKS: Evaluates the 'platform' string.]
        // [USAGE: Command routing.]
        // [CALLERS: Internal to GetCommandType.]
        switch (platform.ToLowerInvariant())
        {
            // [EXPLANATION: Handles commands for the "Facebook" platform.]
            // [PURPOSE: To map Facebook-specific command types to their C# classes.]
            // [HOW_IT_WORKS: Contains a nested switch for Facebook command types.]
            // [USAGE: Specific platform handling.]
            // [CALLERS: Internal to GetCommandType.]
            case "facebook":
                // [EXPLANATION: Nested switch for Facebook command types.]
                // [PURPOSE: To provide granular mapping for Facebook actions.]
                // [HOW_IT_WORKS: Evaluates the 'commandType' string for Facebook commands.]
                // [USAGE: Specific command type mapping.]
                // [CALLERS: Internal to GetCommandType.]
                switch (commandType.ToLowerInvariant())
                {
                    // [EXPLANATION: Maps "replytocomments" to the ReplyToCommentsCommand class.]
                    // [PURPOSE: To correctly identify the C# class for this command.]
                    // [HOW_IT_WORKS: Returns the Type object for ReplyToCommentsCommand.]
                    // [USAGE: Command class resolution.]
                    // [CALLERS: Internal to GetCommandType.]
                    case "replytocomments": return typeof(Commands.Facebook.ReplyToCommentsCommand);
                    // [EXPLANATION: Maps "reacttopost" to the ReactToPostCommand class.]
                    // [PURPOSE: To correctly identify the C# class for this command.]
                    // [HOW_IT_WORKS: Returns the Type object for ReactToPostCommand.]
                    // [USAGE: Command class resolution.]
                    // [CALLERS: Internal to GetCommandType.]
                    case "reacttopost": return typeof(Commands.Facebook.ReactToPostCommand);
                    // [EXPLANATION: Maps "readcomments" to the ReadCommentsCommand class.]
                    // [PURPOSE: To correctly identify the C# class for this command.]
                    // [HOW_IT_WORKS: Returns the Type object for ReadCommentsCommand.]
                    // [USAGE: Command class resolution.]
                    // [CALLERS: Internal to GetCommandType.]
                    case "readcomments": return typeof(Commands.Facebook.ReadCommentsCommand);
                    // [EXPLANATION: Maps "reacttocomment" to the ReactToCommentCommand class.]
                    // [PURPOSE: To correctly identify the C# class for this command.]
                    // [HOW_IT_WORKS: Returns the Type object for ReactToCommentCommand.]
                    // [USAGE: Command class resolution.]
                    // [CALLERS: Internal to GetCommandType.]
                    case "reacttocomment": return typeof(Commands.Facebook.ReactToCommentCommand);
                    // [EXPLANATION: Default case for unrecognized Facebook command types.]
                    // [PURPOSE: To handle commands that are not explicitly mapped.]
                    // [HOW_IT_WORKS: Returns null, indicating an unknown command.]
                    // [USAGE: Error handling.]
                    // [CALLERS: Internal to GetCommandType.]
                    default: return null;
                }
            // [EXPLANATION: Handles commands for the "App" platform (internal application commands).]
            // [PURPOSE: To map App-specific command types to their C# classes.]
            // [HOW_IT_WORKS: Contains a nested switch for App command types.]
            // [USAGE: Specific platform handling.]
            // [CALLERS: Internal to GetCommandType.]
            case "app":
                // [EXPLANATION: Nested switch for App command types.]
                // [PURPOSE: To provide granular mapping for internal application actions.]
                // [HOW_IT_WORKS: Evaluates the 'commandType' string for App commands.]
                // [USAGE: Specific command type mapping.]
                // [CALLERS: Internal to GetCommandType.]
                switch (commandType.ToLowerInvariant())
                {
                    // [EXPLANATION: Maps "navigatetopage" to the NavigateToPageCommand class.]
                    // [PURPOSE: To correctly identify the C# class for this command.]
                    // [HOW_IT_WORKS: Returns the Type object for NavigateToPageCommand.]
                    // [USAGE: Command class resolution.]
                    // [CALLERS: Internal to GetCommandType.]
                    case "navigatetopage": return typeof(Commands.App.NavigateToPageCommand);
                    // [EXPLANATION: Maps "highlightelement" to the HighlightElementCommand class.]
                    // [PURPOSE: To correctly identify the C# class for this command.]
                    // [HOW_IT_WORKS: Returns the Type object for HighlightElementCommand.]
                    // [USAGE: Command class resolution.]
                    // [CALLERS: Internal to GetCommandType.]
                    case "highlightelement": return typeof(Commands.App.HighlightElementCommand);
                    // [EXPLANATION: Default case for unrecognized App command types.]
                    // [PURPOSE: To handle commands that are not explicitly mapped.]
                    // [HOW_IT_WORKS: Returns null, indicating an unknown command.]
                    // [USAGE: Error handling.]
                    // [CALLERS: Internal to GetCommandType.]
                    default: return null;
                }
            // [EXPLANATION: Default case for unrecognized platforms.]
            // [PURPOSE: To handle commands that target platforms not explicitly supported.]
            // [HOW_IT_WORKS: Returns null, indicating an unknown platform.]
            // [USAGE: Error handling.]
            // [CALLERS: Internal to GetCommandType.]
            default: return null;
        }
    }
}

CommandDispatcher Class (NexusSales.Executioner):

Receives the parsed ICommand object.

Uses Dependency Injection (IServiceProvider) to resolve the correct ICommandHandler (e.g., IFacebookCommandHandler, IAppCommandHandler).

Dispatches the command to the handler's appropriate method.

Validation: Before dispatching, validate that all required attributes for the specific CommandType are present. If not, return a CommandResult indicating missing attributes.

Example (Conceptual C#):

// NexusSales.Executioner/CommandDispatcher.cs
// [EXPLANATION: Responsible for routing parsed commands to their respective handlers.]
// [PURPOSE: To centralize command execution and decouple the command parsing from the actual business logic.]
// [USAGE: Called by the CommandProcessor after a command has been successfully parsed.]
// [CALLERS: NexusSales.Executioner.CommandProcessor]
public class CommandDispatcher
{
    private readonly IServiceProvider _serviceProvider; // For resolving command handlers via DI

    // [EXPLANATION: Constructor for CommandDispatcher, taking the service provider as a dependency.]
    // [PURPOSE: To allow the dispatcher to retrieve instances of command handlers from the DI container.]
    // [HOW_IT_WORKS: The IServiceProvider enables runtime resolution of registered services.]
    // [USAGE: Instantiated by the DI container.]
    // [CALLERS: Microsoft.Extensions.DependencyInjection]
    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // [EXPLANATION: Dispatches the given command to the appropriate handler for execution.]
    // [PURPOSE: To execute the action defined by the command object.]
    // [HOW_IT_WORKS: Resolves the correct handler based on command.Platform and command.CommandType, then calls the handler's method.]
    // [USAGE: Core execution flow for all commands.]
    // [CALLERS: NexusSales.Executioner.CommandProcessor.ProcessCommand()]
    public async Task<CommandResult> DispatchAsync(ICommand command)
    {
        // [EXPLANATION: Logs the command being dispatched.]
        // [PURPOSE: For debugging and auditing command execution flow.]
        // [USAGE: Internal logging.]
        // [CALLERS: Internal to DispatchAsync.]
        Logger.Log(LogLevel.INFO, "Executioner", "CommandDispatcher.DispatchAsync", $"Dispatching command: {command.Platform}/{command.CommandType} (CallbackId: {command.CallbackId}).");

        try
        {
            // [EXPLANATION: Validates the command's attributes before dispatching.]
            // [PURPOSE: To ensure all necessary parameters for the command's execution are present.]
            // [HOW_IT_WORKS: Calls a helper method to check for required attributes based on command type.]
            // [USAGE: Pre-execution validation.]
            // [CALLERS: Internal to DispatchAsync.]
            CommandResult validationResult = ValidateCommandAttributes(command);
            // [EXPLANATION: If attribute validation fails, return the error result immediately.]
            // [PURPOSE: To prevent execution of incomplete commands and provide feedback to Gemini.]
            // [HOW_IT_WORKS: Checks the Success property of the validationResult.]
            // [USAGE: Early exit for invalid commands.]
            // [CALLERS: Internal to DispatchAsync.]
            if (!validationResult.Success)
            {
                // [EXPLANATION: Logs the attribute validation failure.]
                // [PURPOSE: For debugging and auditing invalid command inputs.]
                // [USAGE: Internal logging.]
                // [CALLERS: Internal to DispatchAsync.]
                Logger.Log(LogLevel.ERROR, "Executioner", "CommandDispatcher.DispatchAsync", $"Command validation failed: {validationResult.Message} (Missing: {string.Join(", ", validationResult.MissingAttributes)})", null, validationResult.ErrorCode, new { Command = command });
                return validationResult;
            }

            // [EXPLANATION: Uses a switch statement to route commands based on their platform.]
            // [PURPOSE: To direct the command to the correct platform-specific handler.]
            // [HOW_IT_WORKS: Evaluates the 'command.Platform' string.]
            // [USAGE: Top-level command routing.]
            // [CALLERS: Internal to DispatchAsync.]
            switch (command.Platform.ToLowerInvariant())
            {
                // [EXPLANATION: Handles commands for the "Facebook" platform.]
                // [PURPOSE: To execute Facebook-specific actions.]
                // [HOW_IT_WORKS: Resolves IFacebookCommandHandler and calls its appropriate method.]
                // [USAGE: Platform-specific command execution.]
                // [CALLERS: Internal to DispatchAsync.]
                case "facebook":
                    // [EXPLANATION: Resolves the Facebook command handler from the DI container.]
                    // [PURPOSE: To get an instance of the service responsible for Facebook operations.]
                    // [HOW_IT_WORKS: GetRequiredService throws if the service is not registered.]
                    // [USAGE: Dependency resolution.]
                    // [CALLERS: Internal to DispatchAsync.]
                    var facebookHandler = _serviceProvider.GetRequiredService<IFacebookCommandHandler>();
                    // [EXPLANATION: Uses a nested switch to call the specific method on the Facebook handler.]
                    // [PURPOSE: To execute the exact Facebook action requested by the command.]
                    // [HOW_IT_WORKS: Evaluates command.CommandType and invokes the corresponding handler method.]
                    // [USAGE: Granular command execution.]
                    // [CALLERS: Internal to DispatchAsync.]
                    switch (command.CommandType.ToLowerInvariant())
                    {
                        // [EXPLANATION: Handles "replytocomments" command.]
                        // [PURPOSE: To initiate the bulk reply operation on Facebook.]
                        // [HOW_IT_WORKS: Casts the command to ReplyToCommentsCommand and passes it to the handler.]
                        // [USAGE: Specific command execution.]
                        // [CALLERS: Internal to DispatchAsync.]
                        case "replytocomments":
                            return await facebookHandler.HandleReplyToCommentsAsync((Commands.Facebook.ReplyToCommentsCommand)command);
                        // [EXPLANATION: Handles "reacttopost" command.]
                        // [PURPOSE: To initiate a reaction to a Facebook post.]
                        // [HOW_IT_WORKS: Casts the command to ReactToPostCommand and passes it to the handler.]
                        // [USAGE: Specific command execution.]
                        // [CALLERS: Internal to DispatchAsync.]
                        case "reacttopost":
                            return await facebookHandler.HandleReactToPostAsync((Commands.Facebook.ReactToPostCommand)command);
                        // [EXPLANATION: Handles "readcomments" command.]
                        // [PURPOSE: To initiate reading comments from a Facebook post.]
                        // [HOW_IT_WORKS: Casts the command to ReadCommentsCommand and passes it to the handler.]
                        // [USAGE: Specific command execution.]
                        // [CALLERS: Internal to DispatchAsync.]
                        case "readcomments":
                            return await facebookHandler.HandleReadCommentsAsync((Commands.Facebook.ReadCommentsCommand)command);
                        // [EXPLANATION: Handles "reacttocomment" command.]
                        // [PURPOSE: To initiate a reaction to a specific Facebook comment.]
                        // [HOW_IT_WORKS: Casts the command to ReactToCommentCommand and passes it to the handler.]
                        // [USAGE: Specific command execution.]
                        // [CALLERS: Internal to DispatchAsync.]
                        case "reacttocomment":
                            return await facebookHandler.HandleReactToCommentAsync((Commands.Facebook.ReactToCommentCommand)command);
                        // [EXPLANATION: Default case for unrecognized Facebook command types.]
                        // [PURPOSE: To handle commands that are not explicitly supported within the Facebook platform.]
                        // [HOW_IT_WORKS: Returns a failed result indicating an unknown command.]
                        // [USAGE: Error handling.]
                        // [CALLERS: Internal to DispatchAsync.]
                        default:
                            // [EXPLANATION: Logs an error for an unsupported command type within the Facebook platform.]
                            // [PURPOSE: For debugging and auditing invalid command inputs.]
                            // [USAGE: Internal logging.]
                            // [CALLERS: Internal to DispatchAsync.]
                            Logger.Log(LogLevel.ERROR, "Executioner", "CommandDispatcher.DispatchAsync", $"Unsupported Facebook command type: {command.CommandType}", null, "EXEC-DISP-002", new { Command = command });
                            return new CommandResult { Success = false, Message = $"Unsupported Facebook command: {command.CommandType}", CallbackId = command.CallbackId, ErrorCode = "EXEC-DISP-002" };
                    }

                // [EXPLANATION: Handles commands for the "App" platform (internal application commands).]
                // [PURPOSE: To execute internal application actions like UI navigation or highlighting.]
                // [HOW_IT_WORKS: Resolves IAppCommandHandler and calls its appropriate method.]
                // [USAGE: Platform-specific command execution.]
                // [CALLERS: Internal to DispatchAsync.]
                case "app":
                    // [EXPLANATION: Resolves the App command handler from the DI container.]
                    // [PURPOSE: To get an instance of the service responsible for internal app operations.]
                    // [HOW_IT_WORKS: GetRequiredService throws if the service is not registered.]
                    // [USAGE: Dependency resolution.]
                    // [CALLERS: Internal to DispatchAsync.]
                    var appHandler = _serviceProvider.GetRequiredService<IAppCommandHandler>();
                    // [EXPLANATION: Uses a nested switch to call the specific method on the App handler.]
                    // [PURPOSE: To execute the exact internal application action requested by the command.]
                    // [HOW_IT_WORKS: Evaluates command.CommandType and invokes the corresponding handler method.]
                    // [USAGE: Granular command execution.]
                    // [CALLERS: Internal to DispatchAsync.]
                    switch (command.CommandType.ToLowerInvariant())
                    {
                        // [EXPLANATION: Handles "navigatetopage" command.]
                        // [PURPOSE: To initiate navigation to a different UI page.]
                        // [HOW_IT_WORKS: Casts the command to NavigateToPageCommand and passes it to the handler.]
                        // [USAGE: Specific command execution.]
                        // [CALLERS: Internal to DispatchAsync.]
                        case "navigatetopage":
                            return await appHandler.HandleNavigateToPageAsync((Commands.App.NavigateToPageCommand)command);
                        // [EXPLANATION: Handles "highlightelement" command.]
                        // [PURPOSE: To initiate highlighting of a UI element.]
                        // [HOW_IT_WORKS: Casts the command to HighlightElementCommand and passes it to the handler.]
                        // [USAGE: Specific command execution.]
                        // [CALLERS: Internal to DispatchAsync.]
                        case "highlightelement":
                            return await appHandler.HandleHighlightElementAsync((Commands.App.HighlightElementCommand)command);
                        // [EXPLANATION: Default case for unrecognized App command types.]
                        // [PURPOSE: To handle commands that are not explicitly supported within the App platform.]
                        // [HOW_IT_WORKS: Returns a failed result indicating an unknown command.]
                        // [USAGE: Error handling.]
                        // [CALLERS: Internal to DispatchAsync.]
                        default:
                            // [EXPLANATION: Logs an error for an unsupported command type within the App platform.]
                            // [PURPOSE: For debugging and auditing invalid command inputs.]
                            // [USAGE: Internal logging.]
                            // [CALLERS: Internal to DispatchAsync.]
                            Logger.Log(LogLevel.ERROR, "Executioner", "CommandDispatcher.DispatchAsync", $"Unsupported App command type: {command.CommandType}", null, "EXEC-DISP-003", new { Command = command });
                            return new CommandResult { Success = false, Message = $"Unsupported app command: {command.CommandType}", CallbackId = command.CallbackId, ErrorCode = "EXEC-DISP-003" };
                    }
                // [EXPLANATION: Default case for unrecognized platforms.]
                // [PURPOSE: To handle commands that target platforms not explicitly supported by the dispatcher.]
                // [HOW_IT_WORKS: Returns a failed result indicating an unknown platform.]
                // [USAGE: Error handling.]
                // [CALLERS: Internal to DispatchAsync.]
                default:
                    // [EXPLANATION: Logs an error for an unsupported platform.]
                    // [PURPOSE: For debugging and auditing invalid command inputs.]
                    // [USAGE: Internal logging.]
                    // [CALLERS: Internal to DispatchAsync.]
                    Logger.Log(LogLevel.ERROR, "Executioner", "CommandDispatcher.DispatchAsync", $"Unsupported platform: {command.Platform}", null, "EXEC-DISP-001", new { Command = command });
                    return new CommandResult { Success = false, Message = $"Unsupported platform: {command.Platform}", CallbackId = command.CallbackId, ErrorCode = "EXEC-DISP-001" };
            }
        }
        catch (Exception ex)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches any unexpected errors during command dispatching.]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with detailed exception information.]
            // [ERROR_CODE: EXEC-DISP-GEN-004]
            // [CONTEXTUAL_DATA: { "Command": command }]
            Logger.Log(LogLevel.FATAL, "Executioner", "CommandDispatcher.DispatchAsync", $"A fatal error occurred during command dispatch: {ex.Message}", ex, "EXEC-DISP-GEN-004", new { Command = command });
            return new CommandResult { Success = false, Message = "An unexpected error occurred during command dispatch.", CallbackId = command.CallbackId, ErrorCode = "EXEC-DISP-GEN-004" };
        }
    }

    // [EXPLANATION: Validates the attributes of a given command based on its type.]
    // [PURPOSE: To ensure that all required parameters for a command's execution are present before dispatching.]
    // [HOW_IT_WORKS: Contains a switch statement that defines required attributes for each command type.]
    // [USAGE: Called by DispatchAsync before calling the specific command handler.]
    // [CALLERS: CommandDispatcher.DispatchAsync()]
    private CommandResult ValidateCommandAttributes(ICommand command)
    {
        // [EXPLANATION: Initializes a list to store any missing attribute names.]
        // [PURPOSE: To collect and report all missing parameters to Gemini.]
        // [HOW_IT_WORKS: A standard List<string>.]
        // [USAGE: Populated during validation checks.]
        // [CALLERS: Internal to ValidateCommandAttributes.]
        List<string> missingAttributes = new List<string>();

        // [EXPLANATION: Determines required attributes based on the command's platform and type.]
        // [PURPOSE: To apply specific validation rules for each command.]
        // [HOW_IT_WORKS: Uses nested switch statements to define required attributes.]
        // [USAGE: Core validation logic.]
        // [CALLERS: Internal to ValidateCommandAttributes.]
        switch (command.Platform.ToLowerInvariant())
        {
            // [EXPLANATION: Defines required attributes for Facebook commands.]
            // [PURPOSE: To ensure Facebook-specific commands have all necessary parameters.]
            // [HOW_IT_WORKS: Contains a nested switch for Facebook command types.]
            // [USAGE: Platform-specific validation.]
            // [CALLERS: Internal to ValidateCommandAttributes.]
            case "facebook":
                // [EXPLANATION: Nested switch for Facebook command types.]
                // [PURPOSE: To provide granular validation for each Facebook action.]
                // [HOW_IT_WORKS: Evaluates command.CommandType.]
                // [USAGE: Specific command validation.]
                // [CALLERS: Internal to ValidateCommandAttributes.]
                switch (command.CommandType.ToLowerInvariant())
                {
                    // [EXPLANATION: Defines required attributes for "replytocomments" command.]
                    // [PURPOSE: Ensures PostId and ReplyContent are present.]
                    // [HOW_IT_WORKS: Checks command.Attributes for key existence and non-empty values.]
                    // [USAGE: Command-specific validation.]
                    // [CALLERS: Internal to ValidateCommandAttributes.]
                    case "replytocomments":
                        if (!command.Attributes.ContainsKey("PostId") || string.IsNullOrEmpty(command.Attributes["PostId"])) missingAttributes.Add("PostId");
                        if (!command.Attributes.ContainsKey("ReplyContent") || string.IsNullOrEmpty(command.Attributes["ReplyContent"])) missingAttributes.Add("ReplyContent");
                        break;
                    // [EXPLANATION: Defines required attributes for "reacttopost" command.]
                    // [PURPOSE: Ensures PostId and ReactionType are present.]
                    // [HOW_IT_WORKS: Checks command.Attributes for key existence and non-empty values.]
                    // [USAGE: Command-specific validation.]
                    // [CALLERS: Internal to ValidateCommandAttributes.]
                    case "reacttopost":
                        if (!command.Attributes.ContainsKey("PostId") || string.IsNullOrEmpty(command.Attributes["PostId"])) missingAttributes.Add("PostId");
                        if (!command.Attributes.ContainsKey("ReactionType") || string.IsNullOrEmpty(command.Attributes["ReactionType"])) missingAttributes.Add("ReactionType");
                        break;
                    // [EXPLANATION: Defines required attributes for "readcomments" command.]
                    // [PURPOSE: Ensures PostId is present.]
                    // [HOW_IT_WORKS: Checks command.Attributes for key existence and non-empty values.]
                    // [USAGE: Command-specific validation.]
                    // [CALLERS: Internal to ValidateCommandAttributes.]
                    case "readcomments":
                        if (!command.Attributes.ContainsKey("PostId") || string.IsNullOrEmpty(command.Attributes["PostId"])) missingAttributes.Add("PostId");
                        break;
                    // [EXPLANATION: Defines required attributes for "reacttocomment" command.]
                    // [PURPOSE: Ensures CommentId and ReactionType are present.]
                    // [HOW_IT_WORKS: Checks command.Attributes for key existence and non-empty values.]
                    // [USAGE: Command-specific validation.]
                    // [CALLERS: Internal to ValidateCommandAttributes.]
                    case "reacttocomment":
                        if (!command.Attributes.ContainsKey("CommentId") || string.IsNullOrEmpty(command.Attributes["CommentId"])) missingAttributes.Add("CommentId");
                        if (!command.Attributes.ContainsKey("ReactionType") || string.IsNullOrEmpty(command.Attributes["ReactionType"])) missingAttributes.Add("ReactionType");
                        break;
                }
                break;
            // [EXPLANATION: Defines required attributes for App commands.]
            // [PURPOSE: To ensure internal application commands have all necessary parameters.]
            // [HOW_IT_WORKS: Contains a nested switch for App command types.]
            // [USAGE: Platform-specific validation.]
            // [CALLERS: Internal to ValidateCommandAttributes.]
            case "app":
                // [EXPLANATION: Nested switch for App command types.]
                // [PURPOSE: To provide granular validation for each internal application action.]
                // [HOW_IT_WORKS: Evaluates command.CommandType.]
                // [USAGE: Specific command validation.]
                // [CALLERS: Internal to ValidateCommandAttributes.]
                switch (command.CommandType.ToLowerInvariant())
                {
                    // [EXPLANATION: Defines required attributes for "navigatetopage" command.]
                    // [PURPOSE: Ensures TargetPageKey is present.]
                    // [HOW_IT_WORKS: Checks command.Attributes for key existence and non-empty values.]
                    // [USAGE: Command-specific validation.]
                    // [CALLERS: Internal to ValidateCommandAttributes.]
                    case "navigatetopage":
                        if (!command.Attributes.ContainsKey("TargetPageKey") || string.IsNullOrEmpty(command.Attributes["TargetPageKey"])) missingAttributes.Add("TargetPageKey");
                        break;
                    // [EXPLANATION: Defines required attributes for "highlightelement" command.]
                    // [PURPOSE: Ensures ElementName is present.]
                    // [HOW_IT_WORKS: Checks command.Attributes for key existence and non-empty values.]
                    // [USAGE: Command-specific validation.]
                    // [CALLERS: Internal to ValidateCommandAttributes.]
                    case "highlightelement":
                        if (!command.Attributes.ContainsKey("ElementName") || string.IsNullOrEmpty(command.Attributes["ElementName"])) missingAttributes.Add("ElementName");
                        break;
                }
                break;
        }

        // [EXPLANATION: Returns a CommandResult indicating success or failure based on missing attributes.]
        // [PURPOSE: To provide structured feedback to the caller (CommandDispatcher).]
        // [HOW_IT_WORKS: If missingAttributes list is empty, Success=true; otherwise, Success=false and MissingAttributes is populated.]
        // [USAGE: Final result of attribute validation.]
        // [CALLERS: CommandDispatcher.DispatchAsync()]
        if (missingAttributes.Any())
        {
            return new CommandResult
            {
                Success = false,
                Message = "Missing required attributes for command.",
                MissingAttributes = missingAttributes,
                CallbackId = command.CallbackId,
                ErrorCode = "EXEC-VALID-001"
            };
        }
        return new CommandResult { Success = true, CallbackId = command.CallbackId };
    }
}

CommandProcessor Class (NexusSales.Executioner):

The main entry point for NexusSales.Executioner.

Receives raw JSON commands (simulated for now, but future IPC target).

Uses CommandParser to parse the command.

Uses CommandDispatcher to dispatch the command.

Returns a CommandResult back to the caller (simulated for now, but future Gemini interface).

5. Command Design & Templates (Structured Communication)
This section defines the precise structure for commands, ensuring clarity, extensibility, and ease of use for Gemini.

5.1. Professional JSON Command Template (Gemini's Output Format):

Gemini will generate commands in this exact JSON format. The Attributes dictionary provides flexibility for command-specific parameters.

{
  "Platform": "Facebook",             // MANDATORY: e.g., "Facebook", "Messenger", "App" (for internal app commands)
  "CommandType": "ReplyToComments",   // MANDATORY: e.g., "ReplyToComments", "ReactToPost", "NavigateToPage", "HighlightElement"
  "Attributes": {                     // MANDATORY: Dictionary of command-specific parameters
    "PostId": "1234567890",           // Required for ReplyToComments, ReactToPost, ReadComments
    "ReplyContent": "Thanks for your support!", // Required for ReplyToComments
    "ReactionType": "Love",           // Required for ReactToPost, ReactToComment (e.g., "Like", "Love", "Haha", "Wow", "Sad", "Angry")
    "CommentId": "987654321",         // Required for ReactToComment
    "TargetPageKey": "FacebookPage",  // Required for NavigateToPage
    "ElementName": "LoginButton"      // Required for HighlightElement
  },
  "CallbackId": "unique_request_id_123" // OPTIONAL: For Gemini to track responses and asynchronous operations.
}

5.2. Professional C# Command Object Model (NexusSales.Core/Commands)

Base Interface: Define a base interface ICommand (or IAppCommand) in NexusSales.Core/Interfaces with properties like Platform, CommandType, CallbackId, and Attributes. This is crucial for polymorphic handling.

Concrete Command Classes: For each Platform and CommandType combination, create a specific C# class that implements ICommand. These classes will have strongly-typed properties for their specific Attributes. This enforces type safety and makes commands easier to work with in C#.

Folder Structure: Organize commands by platform (e.g., NexusSales.Core/Commands/Facebook/, NexusSales.Core/Commands/App/).

Example: NexusSales.Core/Commands/Facebook/ReplyToCommentsCommand.cs

// NexusSales.Core/Commands/Facebook/ReplyToCommentsCommand.cs
// [EXPLANATION: Represents a command to reply to all comments on a specific Facebook post.]
// [PURPOSE: To encapsulate the data needed for the 'reply to comments' action, ensuring type safety and clarity.]
// [USAGE: Deserialized from JSON input by CommandParser, passed to the FacebookCommandHandler.]
// [CALLERS: NexusSales.Executioner.CommandParser, NexusSales.Services.FacebookCommandHandler]
public class ReplyToCommentsCommand : ICommand // Implements the base ICommand interface
{
    // [EXPLANATION: Gets or sets the platform for this command. Fixed to "Facebook" for this class.]
    // [PURPOSE: To identify which platform's API/service should handle this command. Ensures correct routing.]
    // [HOW_IT_WORKS: Set during JSON deserialization or object creation. Used by CommandDispatcher.]
    // [USAGE: Routing commands, logging.]
    // [CALLERS: Newtonsoft.Json, NexusSales.Executioner.CommandDispatcher]
    public string Platform { get; set; } = "Facebook"; // Default value for this specific command

    // [EXPLANATION: Gets or sets the type of command. Fixed to "ReplyToComments" for this class.]
    // [PURPOSE: To identify the specific action to be performed within the Facebook platform.]
    // [HOW_IT_WORKS: Set during JSON deserialization or object creation. Used by CommandDispatcher.]
    // [USAGE: Routing commands, logging.]
    // [CALLERS: Newtonsoft.Json, NexusSales.Executioner.CommandDispatcher]
    public string CommandType { get; set; } = "ReplyToComments"; // Default value for this specific command

    // [EXPLANATION: Gets or sets the ID of the Facebook post to which comments will be replied.]
    // [PURPOSE: To specify the target post for the reply action. This is a mandatory attribute.]
    // [HOW_IT_WORKS: Populated from the "PostId" attribute in the incoming JSON command.]
    // [USAGE: Passed as a parameter to the Facebook Graph API call.]
    // [CALLERS: NexusSales.Executioner.CommandParser, NexusSales.Services.FacebookCommandHandler]
    public string PostId { get; set; }

    // [EXPLANATION: Gets or sets the content of the reply message to be sent.]
    // [PURPOSE: To provide the actual text that will be used for the bulk reply.]
    // [HOW_IT_WORKS: Populated from the "ReplyContent" attribute in the incoming JSON command.]
    // [USAGE: Used as a parameter in the Facebook Graph API call.]
    // [CALLERS: NexusSales.Executioner.CommandParser, NexusSales.Services.FacebookCommandHandler]
    public string ReplyContent { get; set; }

    // [EXPLANATION: Gets or sets an optional callback ID for tracking the command's execution.]
    // [PURPOSE: To allow Gemini to correlate responses with original requests, especially for asynchronous operations.]
    // [HOW_IT_WORKS: Populated from the "CallbackId" attribute in the incoming JSON command.]
    // [USAGE: Included in the CommandResult returned by the Executioner.]
    // [CALLERS: Newtonsoft.Json, NexusSales.Executioner.CommandDispatcher]
    public string CallbackId { get; set; }

    // [EXPLANATION: Gets or sets a dictionary of all raw attributes from the JSON, for flexible access.]
    // [PURPOSE: To provide access to any attributes not explicitly mapped to strongly-typed properties, or for debugging.]
    // [HOW_IT_WORKS: Populated directly from the "Attributes" object in the incoming JSON.]
    // [USAGE: Used by CommandParser for initial validation, and by handlers for generic attribute access.]
    // [CALLERS: Newtonsoft.Json, NexusSales.Executioner.CommandParser]
    public Dictionary<string, string> Attributes { get; set; } // Required by ICommand interface
}

Other Command Examples (Create similar classes for these):

NexusSales.Core/Commands/Facebook/ReactToPostCommand.cs (Attributes: PostId, ReactionType)

NexusSales.Core/Commands/Facebook/ReadCommentsCommand.cs (Attributes: PostId)

NexusSales.Core/Commands/Facebook/ReactToCommentCommand.cs (Attributes: CommentId, ReactionType)

NexusSales.Core/Commands/App/NavigateToPageCommand.cs (Attributes: TargetPageKey)

NexusSales.Core/Commands/App/HighlightElementCommand.cs (Attributes: ElementName, DurationSeconds (optional, default 10))

5.3. Gemini's Attribute Prompting Protocol:

Executioner's Role: If the CommandDispatcher (after CommandParser has done its job) receives an ICommand object that is missing required attributes for its specific CommandType (as defined in CommandDispatcher.ValidateCommandAttributes), the Executioner will generate a structured CommandResult back to Gemini.

This CommandResult will have Success = false, a clear Message, and its MissingAttributes list will be populated with the names of the missing parameters.

Gemini's Response: Gemini will interpret this CommandResult and then prompt the user for the missing information in a natural language format (e.g., "I need a Post ID to reply to comments. Which post are you referring to?").

User Input: The user's response will be processed by Gemini, which will then generate a new JSON command (or an updated version of the previous one) that includes the now-provided attributes. This new command will be sent back to the Executioner for re-processing.

6. Rigorous Debugging, Logging, and Code Explanation Protocol (ABSOLUTE MANDATE)
This phase is paramount for ensuring the Nexus Sales application's reliability and maintainability. Every line of code must be defensively written and transparent about its state.

100% Try-Catch Coverage: Every single method, every critical block of code, and ideally every single line of code that performs an operation that could fail (file I/O, network requests, JSON parsing, API calls, process manipulation, UI updates, reflection, dependency injection resolution) MUST be wrapped in a try...catch block.

Nested Try-Catch: For complex operations, use nested try...catch blocks to pinpoint the exact failure point.

Hyper-Detailed Error Logging: When an exception is caught, log it comprehensively to a dedicated log file (e.g., NexusSales.log in the application's data directory) and, if in debug mode, to the console.

DateTime: Exact timestamp (yyyy-MM-dd HH:mm:ss.fff).

LogLevel: INFO, WARNING, ERROR, FATAL.

ModuleName: The specific project/class (e.g., NexusSales.Executioner, NexusSales.Services.Facebook).

MethodName: The precise method where the error originated.

ErrorMessage: The full Exception.Message.

StackTrace: The complete Exception.StackTrace.

InnerException: If present, log details of ex.InnerException.

ErrorCode: A unique, descriptive error code or identifier (e.g., EXEC-CMD-001 for unknown command, FB-API-002 for Facebook API error, UI-HLT-003 for UI element not found).

ContextualData: Any relevant variables or state that led to the error (e.g., jsonCommand, elementName, postId).

Verbose Debug Logging: Implement a configurable logging system (e.g., via App.config). In debug mode, log every significant step, every successful operation, and the values of key variables. This is crucial for tracing execution flow and identifying subtle bugs.

Example: "INFO: Starting command execution for 'ReplyToComments'...", "DEBUG: Validating PostId: '12345'...", "SUCCESS: Reply sent to comments on post '12345'."

Post-Error State: After logging a critical error, the application must:

Display a user-friendly MessageDialog with the error code and a brief, actionable explanation (e.g., "Facebook API error. Please check your internet connection or try again later. Error Code: FB-API-002").

Attempt to gracefully recover or, if recovery is impossible, guide the user to restart the application.

100% Line-by-Line Code Comments & Explanation (ABSOLUTE MANDATE):

Every single line of code you generate must be accompanied by a comment.

Purpose: Explain what that line of code does.

How it Works: Explain how it achieves its purpose.

Usage: Describe what it is used for in the broader context.

Callers/Users: List other functions, methods, or modules that might call or use this specific line of code or its result.

Example (Reiterating and expanding for clarity):

// [EXPLANATION: This line declares a new instance of HttpClient for making HTTP requests.]
// [PURPOSE: To provide a mechanism for sending HTTP requests and receiving HTTP responses from web resources.]
// [HOW_IT_WORKS: HttpClient is a class in the System.Net.Http namespace that sends requests and receives responses from a URI identified resource.]
// [USAGE: This client instance will be used to interact with the Facebook Graph API or other web services.]
// [CALLERS: Internal to this method, but the method itself is called by FacebookService.GetPostDetails().]
using (HttpClient client = new HttpClient())
{
    try
    {
        // [EXPLANATION: This line sends an asynchronous GET request to the specified URL.]
        // [PURPOSE: To retrieve data from the Facebook Graph API endpoint for a specific post.]
        // [HOW_IT_WORKS: The GetAsync method sends an HTTP GET request and returns an HttpResponseMessage upon completion.]
        // [USAGE: Fetches the raw JSON data of a Facebook post.]
        // [CALLERS: Internal to this method.]
        HttpResponseMessage response = await client.GetAsync(postApiUrl);

        // [EXPLANATION: This line checks if the HTTP response indicates a successful status code (2xx).]
        // [PURPOSE: To ensure that the API request was successful before attempting to read the content.]
        // [HOW_IT_WORKS: If the status code is not in the 200-299 range, it throws an HttpRequestException.]
        // [USAGE: Critical validation step after any network request.]
        // [CALLERS: Internal to this method.]
        response.EnsureSuccessStatusCode();

        // [EXPLANATION: This line reads the content of the HTTP response as a string asynchronously.]
        // [PURPOSE: To obtain the JSON payload returned by the Facebook Graph API.]
        // [HOW_IT_WORKS: ReadAsStringAsync reads the entire content of the HTTP response body as a string.]
        // [USAGE: The resulting JSON string will then be deserialized into a C# object.]
        // [CALLERS: Internal to this method.]
        string jsonResponse = await response.Content.ReadAsStringAsync();

        // [EXPLANATION: This line logs the successful API call and the received JSON response.]
        // [PURPOSE: For debugging and auditing API interactions, especially in verbose debug mode.]
        // [HOW_IT_WORKS: Calls a custom Logger utility to write the message to a log file and console.]
        // [USAGE: Debugging during development, auditing in production (if enabled).]
        // [CALLERS: Internal to this method.]
        Logger.Log(LogLevel.DEBUG, "FacebookAPI", "GetPostDetails", $"API call successful. Response: {jsonResponse}");

        // [EXPLANATION: This line deserializes the JSON response string into a FacebookPost object.]
        // [PURPOSE: To convert the raw JSON data into a structured C# object for easier manipulation.]
        // [HOW_IT_WORKS: Uses Newtonsoft.Json.JsonConvert.DeserializeObject to map JSON properties to C# object properties.]
        // [USAGE: The resulting object is then used to extract post details, comments, etc.]
        // [CALLERS: Internal to this method.]
        FacebookPost post = JsonConvert.DeserializeObject<FacebookPost>(jsonResponse);

        // [EXPLANATION: This line returns the deserialized FacebookPost object.]
        // [PURPOSE: To provide the structured post data to the calling method.]
        // [HOW_IT_WORKS: Passes the object back to the caller.]
        // [USAGE: Used by ViewModels or other services to display post information.]
        // [CALLERS: FacebookPageViewModel.LoadPostData(), FacebookCommandHandler.HandleReadComments()]
        return post;
    }
    catch (HttpRequestException httpEx)
    {
        // [ERROR_HANDLING_EXPLANATION: Catches exceptions specific to HTTP requests (e.g., network connectivity issues, DNS resolution failures, HTTP status codes indicating errors).]
        // [ERROR_LOGGING_DETAIL: Logs a FATAL error including the URL, specific HTTP error message, and full stack trace.]
        // [ERROR_CODE: FB-API-HTTP-001]
        // [CONTEXTUAL_DATA: { "url": postApiUrl }]
        Logger.Log(LogLevel.FATAL, "FacebookAPI", "GetPostDetails", $"FB API HTTP request failed for {postApiUrl}: {httpEx.Message}", httpEx, "FB-API-HTTP-001");
        throw; // Re-throw to propagate to the main error handling mechanism
    }
    catch (JsonException jsonEx)
    {
        // [ERROR_HANDLING_EXPLANATION: Catches exceptions specific to JSON parsing (e.g., malformed JSON, unexpected data structure).]
        // [ERROR_LOGGING_DETAIL: Logs a FATAL error including the JSON content that caused the error, message, and stack trace.]
        // [ERROR_CODE: FB-API-JSON-002]
        // [CONTEXTUAL_DATA: { "jsonResponseSnippet": jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 200)) }]
        Logger.Log(LogLevel.FATAL, "FacebookAPI", "GetPostDetails", $"Failed to parse FB API JSON response: {jsonEx.Message}", jsonEx, "FB-API-JSON-002");
        throw;
    }
    catch (Exception ex)
    {
        // [ERROR_HANDLING_EXPLANATION: Catches any other unexpected exceptions that might occur during the process.]
        // [ERROR_LOGGING_DETAIL: Logs a FATAL error with a generic message, the exception message, and stack trace.]
        // [ERROR_CODE: FB-API-GEN-003]
        // [CONTEXTUAL_DATA: { "url": postApiUrl }]
        Logger.Log(LogLevel.FATAL, "FacebookAPI", "GetPostDetails", $"An unexpected error occurred during FB API call to {postApiUrl}: {ex.Message}", ex, "FB-API-GEN-003");
        throw;
    }
}

7. Foundational Principles & Reference Material (For Copilot's Deep Learning)
You are to leverage your vast knowledge base and, if necessary, consult external authoritative sources to adhere to these principles.

Secure Coding Best Practices (General):

OWASP Top 10: https://owasp.org/www-project-top-ten/

Microsoft Security Development Lifecycle (SDL) practices.

Principle of Least Privilege.

Cryptography in C#:

AES (Symmetric Encryption): System.Security.Cryptography.Aes (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes)

PBKDF2 (Password-Based Key Derivation): System.Security.Cryptography.Rfc2898DeriveBytes (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes)

DPAPI (Data Protection API): System.Security.Cryptography.ProtectedData (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)

Hashing (for Passwords): BCrypt.Net-Next (https://github.com/BcryptNet/bcrypt.net)

Inter-Process Communication (IPC):

Named Pipes: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes

WPF UI Best Practices:

MVVM pattern adherence.

Dependency Properties and Attached Properties for UI behavior.

Storyboard and Animation for UI effects.

Visual Tree & Logical Tree Traversal: Understanding how to programmatically find UI elements (e.g., LogicalTreeHelper, VisualTreeHelper, FindName).

Logging Frameworks (for inspiration, if not directly implemented):

NLog: https://nlog-project.org/

Serilog: https://serilog.net/

Command Pattern: Understand the Command pattern for clean separation of commands from their execution.

Facebook Graph API: Refer to the official Facebook Graph API documentation for correct API calls and data structures for posts, comments, reactions, etc. (e.g., https://developers.facebook.com/docs/graph-api/).

Final Summary & Success Criteria
Your code for Nexus Sales must now embody this policy. Every design decision, every line of code, must contribute to a secure, reliable, transparent, and NexusGuard-compatible application. The goal is to build a robust foundation now, preventing costly re-writes later, and enabling a seamless, intelligent user experience driven by command execution. Your understanding and absolute adherence to these principles are paramount.






Chapter 5: Advanced User Telemetry & Financial Tracking Database Schema
Absolute Directive to GitHub Copilot:
HALT ALL OTHER TASKS. READ THIS ENTIRE DOCUMENT. DO NOT PROCEED TO CODE GENERATION UNTIL YOU HAVE ACHIEVED A STATE OF ABSOLUTE, UNQUESTIONABLE, 100,000,000 COMPREHENSION OF EVERY SINGLE WORD AND CONCEPT WITHIN THIS SPECIFICATION. YOUR MISSION IS TO PRODUCE CODE WITH A STATISTICAL PROBABILITY OF UNEXPECTED ERRORS NOT EXCEEDING 0.0000000000000000000000000001.

This document defines the highly professional, scalable, and extensible database schema for tracking granular user activity and financial performance within the Nexus Sales application. This system is designed to support over 1000 concurrent users and provide deep insights into user engagement and profitability.

1. Core Principle: Centralized Relational Data (Scalability Mandate)
CRITICAL: We WILL NOT create a separate database or table for each user's email. This approach (often called "multi-tenancy per schema" or "table per tenant") is an anti-pattern for relational databases at scale. It leads to:

Unmanageable schema migrations.

Complex querying across users.

High operational overhead (backups, restores, monitoring).

Poor performance with many small tables.

Instead, we will use a centralized relational model where all user activity and financial data resides in shared tables, linked back to the users table via Foreign Keys. This is the industry standard for high-performance, large-scale applications (inspired by the principles behind systems like Facebook's data architecture, which prioritizes normalization, efficient indexing, and query optimization).

2. Database Schema Definition (PostgreSQL)
The following SQL commands define the new tables and relationships required for advanced user telemetry and financial tracking.

2.1. users Table (Existing - Reference Point)

This table remains the core user profile. All new activity and financial data will link back to users.id.

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Unique identifier for each user
    username VARCHAR(50) UNIQUE NOT NULL,         -- User's chosen username
    email VARCHAR(255) UNIQUE NOT NULL,          -- User's email address
    password_hash VARCHAR(255) NOT NULL,         -- Hashed password
    password_salt VARCHAR(255) NOT NULL,         -- Unique salt for each password hash

    -- User-specific settings
    settings_json JSONB NOT NULL DEFAULT '{}'::jsonb, -- Store various user settings as a JSONB document

    -- Financial metrics (Aggregated on user level)
    total_revenue_usd NUMERIC(18, 2) DEFAULT 0.00,  -- Total accumulated revenue in USD
    current_budget_usd NUMERIC(18, 2) DEFAULT 0.00, -- Current allocated budget in USD
    monthly_profit_usd NUMERIC(15, 2) DEFAULT 0.00, -- User's monthly profit in USD
    average_order_value_usd NUMERIC(15, 2) DEFAULT 0.00, -- User's Average Order Value in USD

    -- Performance metrics (Aggregated on user level)
    overall_performance_score DECIMAL(5, 2) DEFAULT 0.00, -- A calculated performance score (e.g., 0.00 to 100.00)
    conversion_rate DECIMAL(5, 4) DEFAULT 0.0000, -- Percentage as a decimal (e.g., 0.1234 for 12.34%)
    engagement_rate DECIMAL(5, 4) DEFAULT 0.0000, -- Another percentage as a decimal

    -- Social media metrics (Aggregated on user level)
    followers_count BIGINT DEFAULT 0,             -- Number of followers (can be large)
    likes_count BIGINT DEFAULT 0,                 -- Total likes on their content
    following_count BIGINT DEFAULT 0,             -- Number of accounts they are following

    -- Audit and Status columns
    last_login_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE,
    is_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP -- KEY FOR DATA SYNCHRONIZATION
);

2.2. platforms Table (Lookup for Social Media Platforms)

This table normalizes platform names, preventing string duplication in activity logs.

-- [EXPLANATION: Creates the 'platforms' table to store unique social media platform names.]
-- [PURPOSE: To normalize platform data, ensuring consistency and efficient storage by using integer IDs instead of repeated strings in other tables.]
-- [HOW_IT_WORKS: Each row represents a distinct social media platform (e.g., Facebook, Messenger, WhatsApp).]
-- [USAGE: Referenced by 'user_activity_logs' and 'user_financial_transactions' tables via 'platform_id' foreign key.]
-- [CALLERS: Database initialization scripts, application's PlatformService (for lookup).]
CREATE TABLE platforms (
    platform_id SERIAL PRIMARY KEY, -- [EXPLANATION: Unique integer identifier for each platform.]
                                    -- [PURPOSE: Primary key for fast lookups and efficient foreign key relationships.]
                                    -- [HOW_IT_WORKS: Automatically increments for new entries.]
                                    -- [USAGE: Foreign key in other tables.]
    name VARCHAR(50) UNIQUE NOT NULL, -- [EXPLANATION: The human-readable name of the platform (e.g., 'Facebook', 'Messenger', 'WhatsApp').]
                                       -- [PURPOSE: To provide a unique and descriptive name for each platform.]
                                       -- [HOW_IT_WORKS: Enforced as unique to prevent duplicate platform entries.]
                                       -- [USAGE: Displayed in UI, used for internal lookup.]
    description TEXT,                  -- [EXPLANATION: A brief description of the platform.]
                                       -- [PURPOSE: To provide additional context or details about the platform.]
                                       -- [HOW_IT_WORKS: Stores variable-length text.]
                                       -- [USAGE: Internal documentation, potential tooltip in UI.]
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP, -- [EXPLANATION: Timestamp when the platform record was created.]
                                                                   -- [PURPOSE: Audit trail.]
                                                                   -- [HOW_IT_WORKS: Automatically set to the current time upon insertion.]
                                                                   -- [USAGE: Internal auditing.]
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP  -- [EXPLANATION: Timestamp of the last modification to the platform record.]
                                                                   -- [PURPOSE: Audit trail, data synchronization (if platform details change).]
                                                                   -- [HOW_IT_WORKS: Automatically updated by a trigger (similar to 'users' table).]
                                                                   -- [USAGE: Internal auditing.]
);

-- [EXPLANATION: Inserts initial, mandatory platform data into the 'platforms' table.]
-- [PURPOSE: To pre-populate the lookup table with the platforms Nexus Sales supports.]
-- [HOW_IT_WORKS: Standard SQL INSERT statements.]
-- [USAGE: Database initialization script.]
-- [CALLERS: Database administrator, deployment scripts.]
INSERT INTO platforms (name, description) VALUES ('Facebook', 'Facebook social media platform for posts, comments, reactions.');
INSERT INTO platforms (name, description) VALUES ('Messenger', 'Facebook Messenger for direct messages and chat interactions.');
INSERT INTO platforms (name, description) VALUES ('WhatsApp', 'WhatsApp messaging platform integration.');
-- Add more platforms as needed in the future

2.3. command_types Table (Lookup for Executable Commands)

This table normalizes the types of commands that can be executed via the Executioner.

-- [EXPLANATION: Creates the 'command_types' table to store unique command names executed by the application.]
-- [PURPOSE: To normalize command data, ensuring consistency and efficient storage by using integer IDs instead of repeated strings in activity logs.]
-- [HOW_IT_WORKS: Each row represents a distinct command type (e.g., ReplyToComments, ReactToPost, NavigateToPage).]
-- [USAGE: Referenced by 'user_activity_logs' table via 'command_type_id' foreign key.]
-- [CALLERS: Database initialization scripts, application's CommandService (for lookup).]
CREATE TABLE command_types (
    command_type_id SERIAL PRIMARY KEY, -- [EXPLANATION: Unique integer identifier for each command type.]
                                        -- [PURPOSE: Primary key for fast lookups and efficient foreign key relationships.]
                                        -- [HOW_IT_WORKS: Automatically increments for new entries.]
                                        -- [USAGE: Foreign key in other tables.]
    name VARCHAR(50) UNIQUE NOT NULL,   -- [EXPLANATION: The human-readable name of the command type (e.g., 'ReplyToComments', 'ReactToPost').]
                                        -- [PURPOSE: To provide a unique and descriptive name for each command.]
                                        -- [HOW_IT_WORKS: Enforced as unique to prevent duplicate command entries.]
                                        -- [USAGE: Displayed in UI (for reports), used for internal lookup.]
    description TEXT,                   -- [EXPLANATION: A brief description of what the command does.]
                                        -- [PURPOSE: To provide additional context or details about the command.]
                                        -- [HOW_IT_WORKS: Stores variable-length text.]
                                        -- [USAGE: Internal documentation, potential tooltip in UI.]
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP, -- [EXPLANATION: Timestamp when the command type record was created.]
                                                                   -- [PURPOSE: Audit trail.]
                                                                   -- [HOW_IT_WORKS: Automatically set to the current time upon insertion.]
                                                                   -- [USAGE: Internal auditing.]
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP  -- [EXPLANATION: Timestamp of the last modification to the command type record.]
                                                                   -- [PURPOSE: Audit trail, data synchronization (if command details change).]
                                                                   -- [HOW_IT_WORKS: Automatically updated by a trigger (similar to 'users' table).]
                                                                   -- [USAGE: Internal auditing.]
);

-- [EXPLANATION: Inserts initial, mandatory command type data into the 'command_types' table.]
-- [PURPOSE: To pre-populate the lookup table with the command types Nexus Sales supports.]
-- [HOW_IT_WORKS: Standard SQL INSERT statements.]
-- [USAGE: Database initialization script.]
-- [CALLERS: Database administrator, deployment scripts.]
INSERT INTO command_types (name, description) VALUES ('ReplyToComments', 'Replies to comments on a social media post.');
INSERT INTO command_types (name, description) VALUES ('ReactToPost', 'Applies a reaction (e.g., Like, Love) to a social media post.');
INSERT INTO command_types (name, description) VALUES ('ReadComments', 'Reads and retrieves comments from a social media post.');
INSERT INTO command_types (name, description) VALUES ('SharePost', 'Shares a social media post.');
INSERT INTO command_types (name, description) VALUES ('ReactToComment', 'Applies a reaction to a specific comment.');
INSERT INTO command_types (name, description) VALUES ('NavigateToPage', 'Navigates the application UI to a specific internal page.');
INSERT INTO command_types (name, description) VALUES ('HighlightElement', 'Visually highlights a UI element for a short duration.');
-- Add more command types as needed in the future

2.4. user_activity_logs Table (Granular User Usage Tracking)

This table stores every single relevant action performed by a user through the application. This is the core telemetry data.

-- [EXPLANATION: Creates the 'user_activity_logs' table to record every significant action performed by users.]
-- [PURPOSE: To provide granular telemetry data for user behavior analysis, feature usage, and debugging.]
-- [HOW_IT_WORKS: Each row represents a single user action, linked to the user, platform, and command type via foreign keys.]
-- [USAGE: Populated by the NexusSales.Executioner after successful command execution. Used for reporting and analytics.]
-- [CALLERS: NexusSales.Executioner.CommandDispatcher, ActivityLoggingService.]
CREATE TABLE user_activity_logs (
    log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- [EXPLANATION: Unique identifier for each activity log entry.]
                                                       -- [PURPOSE: Primary key for individual log retrieval.]
                                                       -- [HOW_IT_WORKS: Automatically generated UUID.]
                                                       -- [USAGE: Referenced by 'user_financial_transactions' for linking profit to specific actions.]
    user_id UUID NOT NULL REFERENCES users(id),        -- [EXPLANATION: Foreign key linking to the 'users' table, identifying which user performed the action.]
                                                       -- [PURPOSE: To attribute actions to specific users.]
                                                       -- [HOW_IT_WORKS: Ensures referential integrity with the 'users' table.]
                                                       -- [USAGE: Joins for user-specific activity reports.]
    platform_id INTEGER NOT NULL REFERENCES platforms(platform_id), -- [EXPLANATION: Foreign key linking to the 'platforms' table, identifying the social media platform used.]
                                                                    -- [PURPOSE: To categorize actions by platform for reporting.]
                                                                    -- [HOW_IT_WORKS: Ensures referential integrity with the 'platforms' table.]
                                                                    -- [USAGE: Joins for platform-specific activity reports.]
    command_type_id INTEGER NOT NULL REFERENCES command_types(command_type_id), -- [EXPLANATION: Foreign key linking to the 'command_types' table, identifying the specific command executed.]
                                                                                -- [PURPOSE: To categorize actions by command type for reporting.]
                                                                                -- [HOW_IT_WORKS: Ensures referential integrity with the 'command_types' table.]
                                                                                -- [USAGE: Joins for command-specific activity reports.]
    action_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL, -- [EXPLANATION: The exact timestamp when the action occurred.]
                                                                                  -- [PURPOSE: For time-series analysis, chronological ordering, and monthly/daily aggregation.]
                                                                                  -- [HOW_IT_WORKS: Automatically set to the current time upon insertion.]
                                                                                  -- [USAGE: Filtering by date range, calculating usage frequency.]
    target_entity_id VARCHAR(255),                                  -- [EXPLANATION: Identifier of the entity the action was performed on (e.g., Facebook Post ID, Comment ID, Page ID).]
                                                                    -- [PURPOSE: To link actions to specific content or targets.]
                                                                    -- [HOW_IT_WORKS: Stores a string ID, nullable if the command doesn't target a specific entity (e.g., 'NavigateToPage').]
                                                                    -- [USAGE: Filtering actions by specific posts/comments.]
    details JSONB,                                                  -- [EXPLANATION: A JSONB column to store command-specific details that are not suitable for dedicated columns.]
                                                                    -- [PURPOSE: To provide flexible, schema-less storage for dynamic attributes (e.g., 'reaction_type' for React commands, 'reply_content_length' for Reply commands, 'page_key' for navigation).]
                                                                    -- [HOW_IT_WORKS: Stores JSON objects efficiently, allowing for indexing on JSON keys.]
                                                                    -- [USAGE: For detailed drill-down into specific action parameters.]
    success BOOLEAN NOT NULL,                                       -- [EXPLANATION: Indicates whether the command execution was successful.]
                                                                    -- [PURPOSE: To track successful vs. failed operations for reliability monitoring.]
                                                                    -- [HOW_IT_WORKS: Set to TRUE or FALSE based on the CommandResult from Executioner.]
                                                                    -- [USAGE: Error rate calculation, debugging.]
    error_message TEXT,                                             -- [EXPLANATION: Stores the error message if the command execution failed.]
                                                                    -- [PURPOSE: To provide context for failed operations, aiding in debugging and support.]
                                                                    -- [HOW_IT_WORKS: Null if success is TRUE, otherwise contains the error details.]
                                                                    -- [USAGE: Debugging, error reporting.]
    duration_ms INTEGER,                                            -- [EXPLANATION: The duration of the action in milliseconds, if measurable.]
                                                                    -- [PURPOSE: To track performance of commands and identify bottlenecks.]
                                                                    -- [HOW_IT_WORKS: Stores an integer representing milliseconds, nullable if not applicable.]
                                                                    -- [USAGE: Performance analytics.]
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,  -- [EXPLANATION: Timestamp when the log entry was created.]
                                                                    -- [PURPOSE: Audit trail.]
                                                                    -- [HOW_IT_WORKS: Automatically set to the current time upon insertion.]
                                                                    -- [USAGE: Internal auditing.]
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP   -- [EXPLANATION: Timestamp of the last modification to the log entry.]
                                                                    -- [PURPOSE: Audit trail, data synchronization.]
                                                                    -- [HOW_IT_WORKS: Automatically updated by a trigger.]
                                                                    -- [USAGE: Internal auditing.]
);

-- [EXPLANATION: Creates an index on 'user_id' in 'user_activity_logs'.]
-- [PURPOSE: To significantly speed up queries that retrieve all activities for a specific user.]
-- [HOW_IT_WORKS: Creates a B-tree index on the 'user_id' column.]
-- [USAGE: Essential for user-specific dashboards and reports.]
-- [CALLERS: PostgreSQL query planner.]
CREATE INDEX idx_user_activity_logs_user_id ON user_activity_logs (user_id);

-- [EXPLANATION: Creates a composite index on 'user_id' and 'action_timestamp' in 'user_activity_logs'.]
-- [PURPOSE: To optimize queries that fetch a user's activities within a specific time range (e.g., "show last month's activity").]
-- [HOW_IT_WORKS: Creates a B-tree index on both columns, allowing efficient filtering and sorting.]
-- [USAGE: Common for time-series user reports.]
-- [CALLERS: PostgreSQL query planner.]
CREATE INDEX idx_user_activity_logs_user_id_timestamp ON user_activity_logs (user_id, action_timestamp DESC);

-- [EXPLANATION: Creates a composite index on 'platform_id' and 'command_type_id' in 'user_activity_logs'.]
-- [PURPOSE: To optimize queries that aggregate usage by platform and command type (e.g., "how many Facebook replies were made").]
-- [HOW_IT_WORKS: Creates a B-tree index on both columns.]
-- [USAGE: For overall application usage statistics.]
-- [CALLERS: PostgreSQL query planner.]
CREATE INDEX idx_user_activity_logs_platform_command ON user_activity_logs (platform_id, command_type_id);

2.5. user_financial_transactions Table (Detailed Profit Tracking)

This table records all financial events (e.g., profit generated), linking them to users and potentially to specific actions.

-- [EXPLANATION: Creates the 'user_financial_transactions' table to record all financial events related to user activity.]
-- [PURPOSE: To track revenue, profit, and other monetary metrics generated by users through the application's tools.]
-- [HOW_IT_WORKS: Each row represents a single financial transaction, linked to the user and optionally to a specific activity log entry.]
-- [USAGE: Populated when profit-generating actions occur. Used for financial reporting and user profitability analysis.]
-- [CALLERS: FinancialTrackingService, specific command handlers that generate profit.]
CREATE TABLE user_financial_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- [EXPLANATION: Unique identifier for each financial transaction.]
                                                               -- [PURPOSE: Primary key for individual transaction retrieval.]
                                                               -- [HOW_IT_WORKS: Automatically generated UUID.]
                                                               -- [USAGE: Internal referencing.]
    user_id UUID NOT NULL REFERENCES users(id),                -- [EXPLANATION: Foreign key linking to the 'users' table, identifying the user associated with the transaction.]
                                                               -- [PURPOSE: To attribute financial performance to specific users.]
                                                               -- [HOW_IT_WORKS: Ensures referential integrity with the 'users' table.]
                                                               -- [USAGE: Joins for user-specific financial reports.]
    transaction_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL, -- [EXPLANATION: The exact timestamp when the transaction occurred.]
                                                                                  -- [PURPOSE: For time-series financial analysis and monthly/daily aggregation.]
                                                                                  -- [HOW_IT_WORKS: Automatically set to the current time upon insertion.]
                                                                                  -- [USAGE: Filtering by date range, calculating monthly profit.]
    amount NUMERIC(18, 2) NOT NULL,                            -- [EXPLANATION: The monetary amount of the transaction (e.g., profit generated, revenue).]
                                                               -- [PURPOSE: To quantify financial impact.]
                                                               -- [HOW_IT_WORKS: Stores a fixed-point decimal number with 18 digits total, 2 after decimal (e.g., 1234567890123456.78).]
                                                               -- [USAGE: Summing for total profit, calculating averages.]
    currency VARCHAR(3) DEFAULT 'USD' NOT NULL,                -- [EXPLANATION: The currency code for the transaction amount (e.g., 'USD', 'EUR').]
                                                               -- [PURPOSE: To ensure clarity and consistency in financial reporting for multi-currency support.]
                                                               -- [HOW_IT_WORKS: Stores a 3-character string, defaults to 'USD'.]
                                                               -- [USAGE: Currency conversion, display.]
    source_platform_id INTEGER REFERENCES platforms(platform_id), -- [EXPLANATION: Optional foreign key linking to the 'platforms' table, indicating which platform generated the profit.]
                                                                  -- [PURPOSE: To analyze profitability by platform (e.g., "Facebook generated X profit").]
                                                                  -- [HOW_IT_WORKS: Nullable, as some transactions might not be directly tied to a platform.]
                                                                  -- [USAGE: Joins for platform-specific profit reports.]
    related_log_id UUID REFERENCES user_activity_logs(log_id),    -- [EXPLANATION: Optional foreign key linking to a specific entry in 'user_activity_logs'.]
                                                                  -- [PURPOSE: To directly associate a financial transaction with a particular user action (e.g., a sale resulting from a specific 'SharePost' command).]
                                                                  -- [HOW_IT_WORKS: Nullable, as not all transactions will have a direct activity log link.]
                                                                  -- [USAGE: Detailed attribution analysis.]
    description TEXT,                                             -- [EXPLANATION: A human-readable description of the transaction.]
                                                                  -- [PURPOSE: To provide context for the financial entry (e.g., "Sale from Facebook ad campaign", "Subscription renewal").]
                                                                  -- [HOW_IT_WORKS: Stores variable-length text.]
                                                                  -- [USAGE: Reporting, auditing.]
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP, -- [EXPLANATION: Timestamp when the transaction record was created.]
                                                                   -- [PURPOSE: Audit trail.]
                                                                   -- [HOW_IT_WORKS: Automatically set to the current time upon insertion.]
                                                                   -- [USAGE: Internal auditing.]
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP  -- [EXPLANATION: Timestamp of the last modification to the transaction record.]
                                                                   -- [PURPOSE: Audit trail, data synchronization.]
                                                                   -- [HOW_IT_WORKS: Automatically updated by a trigger.]
                                                                   -- [USAGE: Internal auditing.]
);

-- [EXPLANATION: Creates an index on 'user_id' in 'user_financial_transactions'.]
-- [PURPOSE: To significantly speed up queries that retrieve all financial transactions for a specific user.]
-- [HOW_IT_WORKS: Creates a B-tree index on the 'user_id' column.]
-- [USAGE: Essential for user-specific financial dashboards and reports.]
-- [CALLERS: PostgreSQL query planner.]
CREATE INDEX idx_user_financial_transactions_user_id ON user_financial_transactions (user_id);

-- [EXPLANATION: Creates a composite index on 'user_id' and 'transaction_date' in 'user_financial_transactions'.]
-- [PURPOSE: To optimize queries that fetch a user's financial transactions within a specific time range (e.g., "show last month's profit").]
-- [HOW_IT_WORKS: Creates a B-tree index on both columns, allowing efficient filtering and sorting.]
-- [USAGE: Common for time-series financial reports.]
-- [CALLERS: PostgreSQL query planner.]
CREATE INDEX idx_user_financial_transactions_user_id_date ON user_financial_transactions (user_id, transaction_date DESC);

2.6. Trigger for updated_at (Reused)

The existing update_updated_at_column() function can be reused for the new tables.

-- [EXPLANATION: Creates or replaces a PostgreSQL function to automatically update the 'updated_at' column.]
-- [PURPOSE: To maintain accurate audit trails and facilitate data synchronization by automatically setting the 'updated_at' timestamp on record modification.]
-- [HOW_IT_WORKS: A PL/pgSQL function that sets the 'updated_at' column of the NEW row to the current timestamp (NOW()).]
-- [USAGE: Called by BEFORE UPDATE triggers on tables that have an 'updated_at' column.]
-- [CALLERS: PostgreSQL trigger system.]
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW(); -- Sets the updated_at timestamp to the current time of the update operation
    RETURN NEW;
END;
$$ language 'plpgsql';

-- [EXPLANATION: Creates a trigger to execute 'update_updated_at_column()' before updates on the 'platforms' table.]
-- [PURPOSE: To automatically manage the 'updated_at' timestamp for platform records.]
-- [HOW_IT_WORKS: Fires before each row is updated in the 'platforms' table.]
-- [USAGE: Ensures 'updated_at' is always current for platform data.]
-- [CALLERS: PostgreSQL trigger system.]
CREATE TRIGGER update_platforms_updated_at
BEFORE UPDATE ON platforms
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- [EXPLANATION: Creates a trigger to execute 'update_updated_at_column()' before updates on the 'command_types' table.]
-- [PURPOSE: To automatically manage the 'updated_at' timestamp for command type records.]
-- [HOW_IT_WORKS: Fires before each row is updated in the 'command_types' table.]
-- [USAGE: Ensures 'updated_at' is always current for command type data.]
-- [CALLERS: PostgreSQL trigger system.]
CREATE TRIGGER update_command_types_updated_at
BEFORE UPDATE ON command_types
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- [EXPLANATION: Creates a trigger to execute 'update_updated_at_column()' before updates on the 'user_activity_logs' table.]
-- [PURPOSE: To automatically manage the 'updated_at' timestamp for user activity records.]
-- [HOW_IT_WORKS: Fires before each row is updated in the 'user_activity_logs' table.]
-- [USAGE: Ensures 'updated_at' is always current for activity data.]
-- [CALLERS: PostgreSQL trigger system.]
CREATE TRIGGER update_user_activity_logs_updated_at
BEFORE UPDATE ON user_activity_logs
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- [EXPLANATION: Creates a trigger to execute 'update_updated_at_column()' before updates on the 'user_financial_transactions' table.]
-- [PURPOSE: To automatically manage the 'updated_at' timestamp for financial transaction records.]
-- [HOW_IT_WORKS: Fires before each row is updated in the 'user_financial_transactions' table.]
-- [USAGE: Ensures 'updated_at' is always current for financial data.]
-- [CALLERS: PostgreSQL trigger system.]
CREATE TRIGGER update_user_financial_transactions_updated_at
BEFORE UPDATE ON user_financial_transactions
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

3. C# Data Models (NexusSales.Core/Models)
Create corresponding C# classes for each new database table. These will be Plain Old C# Objects (POCOs).

3.1. Platform.cs

// NexusSales.Core/Models/Platform.cs
using System;
using System.ComponentModel.DataAnnotations.Schema; // For [Column] attribute if using EF Core

namespace NexusSales.Core.Models
{
    // [EXPLANATION: Represents a social media platform in the database.]
    // [PURPOSE: To provide a structured C# object for interacting with the 'platforms' table.]
    // [USAGE: Used for mapping database records to C# objects and vice-versa, especially for foreign key lookups.]
    // [CALLERS: Data access layer (e.g., PlatformRepository), reporting services.]
    public class Platform
    {
        // [EXPLANATION: Gets or sets the unique integer ID of the platform.]
        // [PURPOSE: Maps to 'platform_id' in the 'platforms' table, serving as the primary key.]
        // [HOW_IT_WORKS: Auto-incremented by the database.]
        // [USAGE: Foreign key in 'UserActivityLog' and 'UserFinancialTransaction'.]
        public int PlatformId { get; set; }

        // [EXPLANATION: Gets or sets the human-readable name of the platform (e.g., "Facebook").]
        // [PURPOSE: Maps to 'name' in the 'platforms' table, must be unique.]
        // [HOW_IT_WORKS: String representation of the platform.]
        // [USAGE: Display in UI, lookup by name.]
        public string Name { get; set; }

        // [EXPLANATION: Gets or sets a description for the platform.]
        // [PURPOSE: Maps to 'description' in the 'platforms' table, provides additional context.]
        // [HOW_IT_WORKS: Textual description.]
        // [USAGE: Internal documentation, potential UI tooltips.]
        public string Description { get; set; }

        // [EXPLANATION: Gets or sets the creation timestamp of the platform record.]
        // [PURPOSE: Maps to 'created_at' in the 'platforms' table, for auditing.]
        // [HOW_IT_WORKS: Set automatically by the database.]
        // [USAGE: Auditing, data integrity checks.]
        public DateTimeOffset CreatedAt { get; set; }

        // [EXPLANATION: Gets or sets the last update timestamp of the platform record.]
        // [PURPOSE: Maps to 'updated_at' in the 'platforms' table, for auditing and potential synchronization.]
        // [HOW_IT_WORKS: Updated automatically by a database trigger.]
        // [USAGE: Auditing, synchronization.]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}

3.2. CommandType.cs

// NexusSales.Core/Models/CommandType.cs
using System;
using System.ComponentModel.DataAnnotations.Schema; // For [Column] attribute if using EF Core

namespace NexusSales.Core.Models
{
    // [EXPLANATION: Represents a type of command executed within the application.]
    // [PURPOSE: To provide a structured C# object for interacting with the 'command_types' table.]
    // [USAGE: Used for mapping database records to C# objects, especially for foreign key lookups in activity logs.]
    // [CALLERS: Data access layer (e.g., CommandTypeRepository), reporting services.]
    public class CommandType
    {
        // [EXPLANATION: Gets or sets the unique integer ID of the command type.]
        // [PURPOSE: Maps to 'command_type_id' in the 'command_types' table, serving as the primary key.]
        // [HOW_IT_WORKS: Auto-incremented by the database.]
        // [USAGE: Foreign key in 'UserActivityLog'.]
        public int CommandTypeId { get; set; }

        // [EXPLANATION: Gets or sets the human-readable name of the command type (e.g., "ReplyToComments").]
        // [PURPOSE: Maps to 'name' in the 'command_types' table, must be unique.]
        // [HOW_IT_WORKS: String representation of the command.]
        // [USAGE: Display in UI (for reports), lookup by name.]
        public string Name { get; set; }

        // [EXPLANATION: Gets or sets a description for the command type.]
        // [PURPOSE: Maps to 'description' in the 'command_types' table, provides additional context.]
        // [HOW_IT_WORKS: Textual description.]
        // [USAGE: Internal documentation, potential UI tooltips.]
        public string Description { get; set; }

        // [EXPLANATION: Gets or sets the creation timestamp of the command type record.]
        // [PURPOSE: Maps to 'created_at' in the 'command_types' table, for auditing.]
        // [HOW_IT_WORKS: Set automatically by the database.]
        // [USAGE: Auditing, data integrity checks.]
        public DateTimeOffset CreatedAt { get; set; }

        // [EXPLANATION: Gets or sets the last update timestamp of the command type record.]
        // [PURPOSE: Maps to 'updated_at' in the 'command_types' table, for auditing and potential synchronization.]
        // [HOW_IT_WORKS: Updated automatically by a database trigger.]
        // [USAGE: Auditing, synchronization.]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}

3.3. UserActivityLog.cs

// NexusSales.Core/Models/UserActivityLog.cs
using System;
using System.ComponentModel.DataAnnotations.Schema; // For [Column] attribute if using EF Core
using Newtonsoft.Json.Linq; // For JSONB mapping

namespace NexusSales.Core.Models
{
    // [EXPLANATION: Represents a single log entry of a user's activity within the application.]
    // [PURPOSE: To store granular telemetry data for user behavior analysis and feature usage tracking.]
    // [USAGE: Populated by the Executioner and ActivityLoggingService. Used for generating usage reports and dashboards.]
    // [CALLERS: NexusSales.Executioner.CommandDispatcher (after command execution), ActivityLoggingService.]
    public class UserActivityLog
    {
        // [EXPLANATION: Gets or sets the unique identifier for this activity log entry.]
        // [PURPOSE: Maps to 'log_id' in the 'user_activity_logs' table, serving as the primary key.]
        // [HOW_IT_WORKS: Automatically generated UUID by the database.]
        // [USAGE: Unique identification, potential foreign key in financial transactions.]
        public Guid LogId { get; set; }

        // [EXPLANATION: Gets or sets the ID of the user who performed this action.]
        // [PURPOSE: Maps to 'user_id' in the 'user_activity_logs' table, foreign key to the 'users' table.]
        // [HOW_IT_WORKS: Links the activity to a specific user.]
        // [USAGE: Filtering activities by user, joining with user data.]
        public Guid UserId { get; set; }

        // [EXPLANATION: Gets or sets the ID of the platform related to this activity.]
        // [PURPOSE: Maps to 'platform_id' in the 'user_activity_logs' table, foreign key to the 'platforms' table.]
        // [HOW_IT_WORKS: Links the activity to a specific social media platform.]
        // [USAGE: Filtering activities by platform, joining with platform data.]
        public int PlatformId { get; set; }

        // [EXPLANATION: Gets or sets the ID of the command type related to this activity.]
        // [PURPOSE: Maps to 'command_type_id' in the 'user_activity_logs' table, foreign key to the 'command_types' table.]
        // [HOW_IT_WORKS: Links the activity to a specific command executed.]
        // [USAGE: Filtering activities by command type, joining with command type data.]
        public int CommandTypeId { get; set; }

        // [EXPLANATION: Gets or sets the exact timestamp when the action occurred.]
        // [PURPOSE: Maps to 'action_timestamp' in the 'user_activity_logs' table.]
        // [HOW_IT_WORKS: Set automatically by the database or explicitly by the application.]
        // [USAGE: Time-series analysis, chronological ordering, aggregation.]
        public DateTimeOffset ActionTimestamp { get; set; }

        // [EXPLANATION: Gets or sets the ID of the target entity (e.g., Post ID, Comment ID).]
        // [PURPOSE: Maps to 'target_entity_id' in the 'user_activity_logs' table.]
        // [HOW_IT_WORKS: A string identifier for the specific item the command acted upon. Can be null if not applicable.]
        // [USAGE: Detailed filtering, auditing specific content interactions.]
        public string TargetEntityId { get; set; }

        // [EXPLANATION: Gets or sets a JSON object containing additional command-specific details.]
        // [PURPOSE: Maps to 'details' in the 'user_activity_logs' table (JSONB type in PostgreSQL).]
        // [HOW_IT_WORKS: Stores flexible, unstructured data like 'reaction_type', 'reply_content_length', 'page_key'.]
        // [USAGE: For deep-dive analysis into command parameters without rigid schema changes.]
        public JObject Details { get; set; }

        // [EXPLANATION: Gets or sets a boolean indicating if the command execution was successful.]
        // [PURPOSE: Maps to 'success' in the 'user_activity_logs' table.]
        // [HOW_IT_WORKS: Set to true if the command completed without errors, false otherwise.]
        // [USAGE: Error rate calculation, reliability monitoring.]
        public bool Success { get; set; }

        // [EXPLANATION: Gets or sets the error message if the command failed.]
        // [PURPOSE: Maps to 'error_message' in the 'user_activity_logs' table.]
        // [HOW_IT_WORKS: Contains details of the error, null if successful.]
        // [USAGE: Debugging, problem diagnosis.]
        public string ErrorMessage { get; set; }

        // [EXPLANATION: Gets or sets the duration of the action in milliseconds.]
        // [PURPOSE: Maps to 'duration_ms' in the 'user_activity_logs' table.]
        // [HOW_IT_WORKS: An integer representing the time taken for the command to execute. Nullable.]
        // [USAGE: Performance monitoring, identifying slow operations.]
        public int? DurationMs { get; set; }

        // [EXPLANATION: Gets or sets the creation timestamp of the log entry.]
        // [PURPOSE: Maps to 'created_at' in the 'user_activity_logs' table, for auditing.]
        // [HOW_IT_WORKS: Set automatically by the database.]
        // [USAGE: Auditing.]
        public DateTimeOffset CreatedAt { get; set; }

        // [EXPLANATION: Gets or sets the last update timestamp of the log entry.]
        // [PURPOSE: Maps to 'updated_at' in the 'user_activity_logs' table, for auditing and synchronization.]
        // [HOW_IT_WORKS: Updated automatically by a database trigger.]
        // [USAGE: Auditing, synchronization.]
        public DateTimeOffset UpdatedAt { get; set; }

        // [EXPLANATION: Navigation property for the User related to this activity log.]
        // [PURPOSE: To allow easy access to the associated User object when querying activity logs (e.g., using EF Core).]
        // [HOW_IT_WORKS: Represents the one-to-many relationship where one User can have many UserActivityLogs.]
        // [USAGE: ORM-specific feature for simplified data retrieval.]
        [ForeignKey("UserId")]
        public virtual User User { get; set; } // Assuming User model exists in NexusSales.Core.Models

        // [EXPLANATION: Navigation property for the Platform related to this activity log.]
        // [PURPOSE: To allow easy access to the associated Platform object when querying activity logs.]
        // [HOW_IT_WORKS: Represents the one-to-many relationship where one Platform can have many UserActivityLogs.]
        // [USAGE: ORM-specific feature for simplified data retrieval.]
        [ForeignKey("PlatformId")]
        public virtual Platform Platform { get; set; }

        // [EXPLANATION: Navigation property for the CommandType related to this activity log.]
        // [PURPOSE: To allow easy access to the associated CommandType object when querying activity logs.]
        // [HOW_IT_WORKS: Represents the one-to-many relationship where one CommandType can have many UserActivityLogs.]
        // [USAGE: ORM-specific feature for simplified data retrieval.]
        [ForeignKey("CommandTypeId")]
        public virtual CommandType CommandType { get; set; }
    }
}

3.4. UserFinancialTransaction.cs

// NexusSales.Core/Models/UserFinancialTransaction.cs
using System;
using System.ComponentModel.DataAnnotations.Schema; // For [Column] attribute if using EF Core

namespace NexusSales.Core.Models
{
    // [EXPLANATION: Represents a single financial transaction or profit event related to a user's activity.]
    // [PURPOSE: To store detailed monetary data generated by users through the application's features.]
    // [USAGE: Populated when profit-generating actions occur. Used for financial reporting and user profitability analysis.]
    // [CALLERS: FinancialTrackingService, specific command handlers that result in profit.]
    public class UserFinancialTransaction
    {
        // [EXPLANATION: Gets or sets the unique identifier for this financial transaction.]
        // [PURPOSE: Maps to 'transaction_id' in the 'user_financial_transactions' table, serving as the primary key.]
        // [HOW_IT_WORKS: Automatically generated UUID by the database.]
        // [USAGE: Unique identification, internal referencing.]
        public Guid TransactionId { get; set; }

        // [EXPLANATION: Gets or sets the ID of the user associated with this transaction.]
        // [PURPOSE: Maps to 'user_id' in the 'user_financial_transactions' table, foreign key to the 'users' table.]
        // [HOW_IT_WORKS: Links the financial event to a specific user.]
        // [USAGE: Filtering transactions by user, joining with user data.]
        public Guid UserId { get; set; }

        // [EXPLANATION: Gets or sets the exact timestamp when the transaction occurred.]
        // [PURPOSE: Maps to 'transaction_date' in the 'user_financial_transactions' table.]
        // [HOW_IT_WORKS: Set automatically by the database or explicitly by the application.]
        // [USAGE: Time-series financial analysis, chronological ordering, aggregation.]
        public DateTimeOffset TransactionDate { get; set; }

        // [EXPLANATION: Gets or sets the monetary amount of the transaction (e.g., profit, revenue).]
        // [PURPOSE: Maps to 'amount' in the 'user_financial_transactions' table.]
        // [HOW_IT_WORKS: Stores a decimal value with high precision (NUMERIC(18, 2) in PostgreSQL).]
        // [USAGE: Summing for total profit, calculating averages.]
        public decimal Amount { get; set; }

        // [EXPLANATION: Gets or sets the currency code for the transaction amount (e.g., "USD").]
        // [PURPOSE: Maps to 'currency' in the 'user_financial_transactions' table.]
        // [HOW_IT_WORKS: A 3-character string representing the currency, defaults to 'USD'.]
        // [USAGE: Displaying currency, potential for multi-currency conversion.]
        public string Currency { get; set; } = "USD"; // Default to USD

        // [EXPLANATION: Gets or sets the ID of the platform from which this profit was generated (optional).]
        // [PURPOSE: Maps to 'source_platform_id' in the 'user_financial_transactions' table, foreign key to 'platforms'.]
        // [HOW_IT_WORKS: Links the financial event to a specific social media platform. Nullable.]
        // [USAGE: Analyzing profitability by platform.]
        public int? SourcePlatformId { get; set; }

        // [EXPLANATION: Gets or sets the ID of a related user activity log entry (optional).]
        // [PURPOSE: Maps to 'related_log_id' in the 'user_financial_transactions' table, foreign key to 'user_activity_logs'.]
        // [HOW_IT_WORKS: Links a financial event directly to a specific action that triggered it. Nullable.]
        // [USAGE: Detailed attribution analysis (e.g., "this sale came from that specific post share").]
        public Guid? RelatedLogId { get; set; }

        // [EXPLANATION: Gets or sets a human-readable description of the transaction.]
        // [PURPOSE: Maps to 'description' in the 'user_financial_transactions' table.]
        // [HOW_IT_WORKS: Textual context for the financial entry.]
        // [USAGE: Reporting, auditing.]
        public string Description { get; set; }

        // [EXPLANATION: Gets or sets the creation timestamp of the transaction record.]
        // [PURPOSE: Maps to 'created_at' in the 'user_financial_transactions' table, for auditing.]
        // [HOW_IT_WORKS: Set automatically by the database.]
        // [USAGE: Auditing.]
        public DateTimeOffset CreatedAt { get; set; }

        // [EXPLANATION: Gets or sets the last update timestamp of the transaction record.]
        // [PURPOSE: Maps to 'updated_at' in the 'user_financial_transactions' table, for auditing and synchronization.]
        // [HOW_IT_WORKS: Updated automatically by a database trigger.]
        // [USAGE: Auditing, synchronization.]
        public DateTimeOffset UpdatedAt { get; set; }

        // [EXPLANATION: Navigation property for the User related to this financial transaction.]
        // [PURPOSE: To allow easy access to the associated User object when querying transactions.]
        // [HOW_IT_WORKS: Represents the one-to-many relationship (one User has many transactions).]
        // [USAGE: ORM-specific feature for simplified data retrieval.]
        [ForeignKey("UserId")]
        public virtual User User { get; set; } // Assuming User model exists in NexusSales.Core.Models

        // [EXPLANATION: Navigation property for the Source Platform related to this transaction.]
        // [PURPOSE: To allow easy access to the associated Platform object when querying transactions.]
        // [HOW_IT_WORKS: Represents the one-to-many relationship (one Platform can be source for many transactions).]
        // [USAGE: ORM-specific feature for simplified data retrieval.]
        [ForeignKey("SourcePlatformId")]
        public virtual Platform SourcePlatform { get; set; }

        // [EXPLANATION: Navigation property for the User Activity Log entry related to this transaction.]
        // [PURPOSE: To allow easy access to the associated UserActivityLog object when querying transactions.]
        // [HOW_IT_WORKS: Represents the one-to-one or one-to-many relationship (one log entry can lead to one transaction, or vice-versa).]
        // [USAGE: ORM-specific feature for simplified data retrieval.]
        [ForeignKey("RelatedLogId")]
        public virtual UserActivityLog RelatedActivity { get; set; }
    }
}

4. C# Service & Repository Integration Points
These services will be responsible for interacting with the new database tables. They will be integrated into the existing Dependency Injection framework.

4.1. ActivityLoggingService (NexusSales.Services)

Purpose: To provide a centralized service for logging user activities to the user_activity_logs table.

Integration: This service will be injected into NexusSales.Executioner's CommandDispatcher or directly into the command handlers (FacebookCommandHandler, AppCommandHandler).

Method: LogActivityAsync(Guid userId, string platformName, string commandTypeName, string targetEntityId, JObject details, bool success, string errorMessage = null, int? durationMs = null)

This method will resolve platform_id and command_type_id from the platforms and command_types tables based on their name strings. This ensures data consistency.

4.2. FinancialTrackingService (NexusSales.Services)

Purpose: To provide a centralized service for recording financial transactions to the user_financial_transactions table.

Integration: This service will be injected into relevant command handlers or business logic that determines profit generation (e.g., after a successful SharePost that leads to a conversion).

Method: RecordTransactionAsync(Guid userId, decimal amount, string currency, string description, string sourcePlatformName = null, Guid? relatedLogId = null)

4.3. Data Access Layer (Repositories - NexusSales.Data)

Create concrete repository implementations (e.g., PlatformRepository, CommandTypeRepository, UserActivityLogRepository, UserFinancialTransactionRepository) in NexusSales.Data.

These repositories will handle the direct ADO.NET (or ORM, if introduced) interactions with the PostgreSQL database for their respective tables.

They will implement interfaces defined in NexusSales.Core/Interfaces.

5. Reporting & Analytics (Future Consideration)
While not part of the immediate coding task, this schema is designed to support powerful reporting:

Monthly Usage Summary: Query user_activity_logs grouped by user_id, platform_id, command_type_id, and action_timestamp (truncated to month).

Most Used Actions: Aggregate user_activity_logs by command_type_id.

Profit by Tool/App: Join user_financial_transactions with platforms and users tables, aggregating amount by source_platform_id over time.

User Performance Dashboards: Combine data from users (e.g., total_revenue_usd) with aggregated data from user_activity_logs and user_financial_transactions.

6. Rigorous Debugging, Logging, and Code Explanation Protocol (ABSOLUTE MANDATE)
This phase is paramount for ensuring the Nexus Sales application's reliability and maintainability. Every line of code must be defensively written and transparent about its state.

100% Try-Catch Coverage: Every single method, every critical block of code, and ideally every single line of code that performs an operation that could fail (database connections, queries, data mapping, JSON serialization/deserialization) MUST be wrapped in a try...catch block.

Nested Try-Catch: For complex operations, use nested try...catch blocks to pinpoint the exact failure point.

Hyper-Detailed Error Logging: When an exception is caught, log it comprehensively to a dedicated log file (e.g., NexusSales.log in the application's data directory) and, if in debug mode, to the console.

DateTime: Exact timestamp (yyyy-MM-dd HH:mm:ss.fff).

LogLevel: INFO, WARNING, ERROR, FATAL.

ModuleName: The specific project/class (e.g., NexusSales.Data.UserActivityLogRepository, NexusSales.Services.ActivityLoggingService).

MethodName: The precise method where the error originated.

ErrorMessage: The full Exception.Message.

StackTrace: The complete Exception.StackTrace.

InnerException: If present, log details of ex.InnerException.

ErrorCode: A unique, descriptive error code or identifier (e.g., DB-ACT-001 for activity log insertion failure, DB-FIN-002 for financial transaction error).

ContextualData: Any relevant variables or state that led to the error (e.g., userId, platformId, commandTypeId, transactionAmount).

Verbose Debug Logging: Implement a configurable logging system. In debug mode, log every significant step, every successful operation, and the values of key variables. This is crucial for tracing execution flow and identifying subtle bugs.

Post-Error State: After logging a critical error, the application must:

Display a user-friendly MessageDialog with the error code and a brief, actionable explanation.

Attempt to gracefully recover or, if recovery is impossible, guide the user to restart the application.

100% Line-by-Line Code Comments & Explanation (ABSOLUTE MANDATE):

Every single line of code you generate must be accompanied by a comment.

Purpose: Explain what that line of code does.

How it Works: Explain how it achieves its purpose.

Usage: Describe what it is used for in the broader context.

Callers/Users: List other functions, methods, or modules that might call or use this specific line of code or its result.

Example (Reiterating and expanding for clarity, specifically for database interaction):

// C# Pseudo-code for Copilot's reference - Logging Activity to DB
// [EXPLANATION: This method logs a user's activity to the database asynchronously.]
// [PURPOSE: To persist user interaction data for telemetry, reporting, and analytics.]
// [HOW_IT_WORKS: It constructs a SQL INSERT statement, prepares parameters, and executes the command against the PostgreSQL database.]
// [USAGE: Called by command handlers (e.g., FacebookCommandHandler) after a command has been processed.]
// [CALLERS: FacebookCommandHandler.HandleReplyToCommentsAsync(), AppCommandHandler.HandleNavigateToPageAsync()]
public async Task LogActivityAsync(Guid userId, int platformId, int commandTypeId, string targetEntityId, JObject details, bool success, string errorMessage = null, int? durationMs = null)
{
    // [EXPLANATION: Defines the SQL INSERT statement for the user_activity_logs table.]
    // [PURPOSE: To insert a new row representing a user action.]
    // [HOW_IT_WORKS: Uses parameterized queries to prevent SQL injection and map C# parameters to database columns.]
    // [USAGE: Executed by NpgsqlCommand.]
    // [CALLERS: Internal to LogActivityAsync.]
    string sql = @"INSERT INTO user_activity_logs (
                        user_id, platform_id, command_type_id, action_timestamp,
                        target_entity_id, details, success, error_message, duration_ms
                    ) VALUES (
                        @userId, @platformId, @commandTypeId, @actionTimestamp,
                        @targetEntityId, @details::jsonb, @success, @errorMessage, @durationMs
                    )";

    // [EXPLANATION: Establishes a new connection to the PostgreSQL database.]
    // [PURPOSE: To communicate with the database server to perform the insert operation.]
    // [HOW_IT_WORKS: Uses NpgsqlConnection, which implements IDisposable, ensuring proper resource management.]
    // [USAGE: Provides the conduit for database commands.]
    // [CALLERS: Internal to LogActivityAsync.]
    using (var conn = new NpgsqlConnection(_connectionString)) // _connectionString would be securely managed
    {
        try
        {
            // [EXPLANATION: Opens the database connection asynchronously.]
            // [PURPOSE: To establish a live link to the database before executing commands.]
            // [HOW_IT_WORKS: Asynchronously connects to the database, freeing up the calling thread.]
            // [USAGE: Pre-requisite for any database operation.]
            // [CALLERS: Internal to LogActivityAsync.]
            await conn.OpenAsync();

            // [EXPLANATION: Creates a new NpgsqlCommand object with the SQL query and connection.]
            // [PURPOSE: To encapsulate the SQL command to be executed.]
            // [HOW_IT_WORKS: Takes the SQL string and the active NpgsqlConnection.]
            // [USAGE: Used to add parameters and execute the query.]
            // [CALLERS: Internal to LogActivityAsync.]
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                // [EXPLANATION: Adds the 'userId' parameter to the SQL command.]
                // [PURPOSE: To bind the C# Guid 'userId' to the '@userId' placeholder in the SQL query.]
                // [HOW_IT_WORKS: NpgsqlParameter handles type conversion and prevents SQL injection.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("userId", userId);
                // [EXPLANATION: Adds the 'platformId' parameter to the SQL command.]
                // [PURPOSE: To bind the C# int 'platformId' to the '@platformId' placeholder in the SQL query.]
                // [HOW_IT_WORKS: NpgsqlParameter handles type conversion.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("platformId", platformId);
                // [EXPLANATION: Adds the 'commandTypeId' parameter to the SQL command.]
                // [PURPOSE: To bind the C# int 'commandTypeId' to the '@commandTypeId' placeholder in the SQL query.]
                // [HOW_IT_WORKS: NpgsqlParameter handles type conversion.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("commandTypeId", commandTypeId);
                // [EXPLANATION: Adds the 'actionTimestamp' parameter to the SQL command.]
                // [PURPOSE: To bind the C# DateTimeOffset 'ActionTimestamp' to the '@actionTimestamp' placeholder.]
                // [HOW_IT_WORKS: Uses DateTimeOffset.UtcNow for consistency, Npgsql handles conversion to TIMESTAMP WITH TIME ZONE.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("actionTimestamp", DateTimeOffset.UtcNow);
                // [EXPLANATION: Adds the 'targetEntityId' parameter to the SQL command.]
                // [PURPOSE: To bind the C# string 'targetEntityId' to the '@targetEntityId' placeholder.]
                // [HOW_IT_WORKS: NpgsqlParameter handles null values for nullable columns.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("targetEntityId", (object)targetEntityId ?? DBNull.Value); // Handle null
                // [EXPLANATION: Adds the 'details' JSONB parameter to the SQL command.]
                // [PURPOSE: To bind the C# JObject 'details' to the '@details' placeholder, casting it to JSONB in SQL.]
                // [HOW_IT_WORKS: Converts JObject to string, Npgsql handles the ::jsonb cast in SQL.]
                // [USAGE: Parameterization of the INSERT statement for flexible data.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("details", details?.ToString() ?? "{}"); // Default to empty JSON object if null
                // [EXPLANATION: Adds the 'success' boolean parameter to the SQL command.]
                // [PURPOSE: To bind the C# bool 'success' to the '@success' placeholder.]
                // [HOW_IT_WORKS: NpgsqlParameter handles type conversion.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("success", success);
                // [EXPLANATION: Adds the 'errorMessage' parameter to the SQL command.]
                // [PURPOSE: To bind the C# string 'errorMessage' to the '@errorMessage' placeholder.]
                // [HOW_IT_WORKS: NpgsqlParameter handles null values for nullable columns.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("errorMessage", (object)errorMessage ?? DBNull.Value); // Handle null
                // [EXPLANATION: Adds the 'durationMs' integer parameter to the SQL command.]
                // [PURPOSE: To bind the C# nullable int 'durationMs' to the '@durationMs' placeholder.]
                // [HOW_IT_WORKS: NpgsqlParameter handles null values for nullable columns.]
                // [USAGE: Parameterization of the INSERT statement.]
                // [CALLERS: Internal to LogActivityAsync.]
                cmd.Parameters.AddWithValue("durationMs", (object)durationMs ?? DBNull.Value); // Handle null

                // [EXPLANATION: Executes the SQL command asynchronously, returning the number of rows affected.]
                // [PURPOSE: To insert the new activity log entry into the database.]
                // [HOW_IT_WORKS: ExecuteNonQueryAsync is used for commands that do not return data (INSERT, UPDATE, DELETE).]
                // [USAGE: Core database write operation.]
                // [CALLERS: Internal to LogActivityAsync.]
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                // [EXPLANATION: Logs successful activity logging.]
                // [PURPOSE: For debugging and auditing data persistence.]
                // [USAGE: Internal logging.]
                // [CALLERS: Internal to LogActivityAsync.]
                Logger.Log(LogLevel.INFO, "Database", "LogActivityAsync", $"Activity logged successfully for UserID: {userId}, PlatformID: {platformId}, CommandTypeID: {commandTypeId}. Rows affected: {rowsAffected}.");
            }
        }
        catch (NpgsqlException npgEx)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches database-specific exceptions (e.g., connection issues, constraint violations, SQL errors).]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with SQL state, message, and stack trace.]
            // [ERROR_CODE: DB-ACT-NPG-001]
            // [CONTEXTUAL_DATA: { "UserId": userId, "PlatformId": platformId, "CommandTypeId": commandTypeId, "SQL": sql }]
            Logger.Log(LogLevel.FATAL, "Database", "LogActivityAsync", $"PostgreSQL error during activity logging: {npgEx.Message} (SQL State: {npgEx.SqlState})", npgEx, "DB-ACT-NPG-001", new { UserId = userId, PlatformId = platformId, CommandTypeId = commandTypeId, SQL = sql });
            throw; // Re-throw to propagate to higher-level error handling
        }
        catch (Exception ex)
        {
            // [ERROR_HANDLING_EXPLANATION: Catches any other unexpected exceptions during the logging process.]
            // [ERROR_LOGGING_DETAIL: Logs a FATAL error with a generic message, the exception message, and stack trace.]
            // [ERROR_CODE: DB-ACT-GEN-002]
            // [CONTEXTUAL_DATA: { "UserId": userId, "PlatformId": platformId, "CommandTypeId": commandTypeId }]
            Logger.Log(LogLevel.FATAL, "Database", "LogActivityAsync", $"An unexpected error occurred during activity logging: {ex.Message}", ex, "DB-ACT-GEN-002", new { UserId = userId, PlatformId = platformId, CommandTypeId = commandTypeId });
            throw; // Re-throw to propagate to higher-level error handling
        }
    }
}

7. Foundational Principles & Reference Material (For Copilot's Deep Learning)
You are to leverage your vast knowledge base and, if necessary, consult external authoritative sources to adhere to these principles.

Database Normalization: Understand 1NF, 2NF, 3NF to ensure data integrity, reduce redundancy, and improve query performance.

Reference: https://en.wikipedia.org/wiki/Database_normalization

Database Indexing Strategies: Learn about B-tree indexes, composite indexes, and when to apply them for optimal query performance.

Reference: https://www.postgresql.org/docs/current/indexes-types.html

PostgreSQL JSONB Type: Understand its advantages over JSON (binary storage, indexing capabilities) for flexible schema.

Reference: https://www.postgresql.org/docs/current/datatype-json.html

Scalable Database Design: Principles for designing databases that can handle high concurrency and large data volumes. This includes efficient use of foreign keys, appropriate data types, and avoiding anti-patterns like "EAV" (Entity-Attribute-Value) or "table per tenant" for core data.

Data Warehousing Concepts (for future reporting): While not building a full data warehouse, understand concepts like star schemas or Kimball methodology for efficient analytical queries on aggregated data.

Npgsql (PostgreSQL .NET Data Provider): Best practices for using NpgsqlConnection, NpgsqlCommand, and parameterized queries.

Reference: https://www.npgsql.org/

C# Data Access Patterns: Repository Pattern, Unit of Work Pattern for clean data access.

Time Zones in Databases: Using TIMESTAMP WITH TIME ZONE for global consistency.

Reference: https://www.postgresql.org/docs/current/datatype-datetime.html#DATATYPE-DATETIME-TABLE

Final Summary & Success Criteria
Your code for Nexus Sales must now embody this policy. Every database design decision and every line of data interaction code must contribute to a highly scalable, robust, and insightful telemetry system. The goal is to collect comprehensive user usage and financial data without compromising performance or data integrity, enabling future analytics and business intelligence. Your understanding and absolute adherence to these principles are paramount.




using CopilotDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CopilotDesktop.Views;

/// <summary>
/// Page that hosts the WebView2 control for displaying the Copilot web interface.
/// Manages the WebView initialization and window customization.
/// </summary>
/// <remarks>
/// For more information about WebView2, see https://docs.microsoft.com/microsoft-edge/webview2/.
/// </remarks>
public sealed partial class WebViewPage : Page
{
    /// <summary>
    /// Gets the ViewModel that provides data and commands for this page.
    /// </summary>
    public WebViewViewModel ViewModel
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebViewPage"/> class.
    /// Sets up the ViewModel, initializes the WebView service, and subscribes to the Loaded event.
    /// </summary>
    public WebViewPage()
    {
        try
        {
            ViewModel = App.GetService<WebViewViewModel>();
            InitializeComponent();

            // Initialize the WebView service with the WebView2 control
            if (CopilotView != null)
            {
                ViewModel.WebViewService.Initialize(CopilotView);
            }

            this.Loaded += MainWindow_Loaded;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] WebViewPage constructor: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Handles the Loaded event for the page.
    /// Configures the window to extend content into the title bar and initializes the WebView2 core.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    /// <remarks>
    /// This method performs two key operations:
    /// 1. Extends content into the title bar for a custom window appearance
    /// 2. Ensures the WebView2 runtime is initialized before the control is used
    /// Includes defensive error handling to prevent crashes during initialization.
    /// </remarks>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[INFO] WebViewPage: MainWindow_Loaded started");

            // Small delay to ensure UI thread is fully ready
            await Task.Delay(100);

            var window = App.MainWindow;
            if (window != null)
            {
                // Enable custom title bar by extending content into the title bar area
                window.ExtendsContentIntoTitleBar = true;
                // Set the draggable title bar region
                if (AppTitleBar != null)
                {
                    window.SetTitleBar(AppTitleBar);
                }
            }

            // Ensure WebView2 runtime is initialized before navigation
            if (CopilotView != null)
            {
                //System.Diagnostics.Debug.WriteLine("[INFO] WebViewPage: Initializing WebView2...");
                await CopilotView.EnsureCoreWebView2Async();
                //System.Diagnostics.Debug.WriteLine("[INFO] WebViewPage: WebView2 initialized successfully");
            }
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"[ERROR] WebViewPage MainWindow_Loaded: {ex}");
            // Don't rethrow - allow page to load even if WebView fails
        }
    }
}
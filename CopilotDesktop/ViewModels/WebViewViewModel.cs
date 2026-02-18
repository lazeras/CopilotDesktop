using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CopilotDesktop.Contracts.Services;
using CopilotDesktop.Contracts.ViewModels;

using Microsoft.Web.WebView2.Core;

namespace CopilotDesktop.ViewModels;

/// <summary>
/// ViewModel for the WebView page that displays the Copilot web interface.
/// Manages navigation, loading states, and browser controls for the WebView2 component.
/// </summary>
public partial class WebViewViewModel : ObservableRecipient, INavigationAware
{
    /// <summary>
    /// Gets or sets the current URI being displayed in the WebView.
    /// Defaults to https://copilot.microsoft.com.
    /// </summary>
    [ObservableProperty]
    private Uri source = new("https://copilot.microsoft.com");

    /// <summary>
    /// Gets or sets a value indicating whether the WebView is currently loading content.
    /// </summary>
    [ObservableProperty]
    private bool isLoading = true;

    /// <summary>
    /// Gets or sets a value indicating whether navigation failures have occurred.
    /// Used to display error UI to the user.
    /// </summary>
    [ObservableProperty]
    private bool hasFailures;

    /// <summary>
    /// Gets the WebView service that provides browser functionality.
    /// </summary>
    public IWebViewService WebViewService
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebViewViewModel"/> class.
    /// </summary>
    /// <param name="webViewService">The WebView service for browser operations.</param>
    public WebViewViewModel(IWebViewService webViewService)
    {
        WebViewService = webViewService;
    }

    /// <summary>
    /// Opens the current WebView source URL in the system's default browser.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task OpenInBrowser()
    {
        if (WebViewService.Source != null)
        {
            await Windows.System.Launcher.LaunchUriAsync(WebViewService.Source);
        }
    }

    /// <summary>
    /// Reloads the current page in the WebView.
    /// </summary>
    [RelayCommand]
    private void Reload()
    {
        WebViewService.Reload();
    }

    /// <summary>
    /// Navigates forward in the browser history if possible.
    /// </summary>
    /// <remarks>
    /// The command's CanExecute state is controlled by <see cref="BrowserCanGoForward"/>.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(BrowserCanGoForward))]
    private void BrowserForward()
    {
        if (WebViewService.CanGoForward)
        {
            WebViewService.GoForward();
        }
    }

    /// <summary>
    /// Determines whether the browser can navigate forward in the history.
    /// </summary>
    /// <returns><c>true</c> if forward navigation is available; otherwise, <c>false</c>.</returns>
    private bool BrowserCanGoForward()
    {
        return WebViewService.CanGoForward;
    }

    /// <summary>
    /// Navigates backward in the browser history if possible.
    /// </summary>
    /// <remarks>
    /// The command's CanExecute state is controlled by <see cref="BrowserCanGoBack"/>.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(BrowserCanGoBack))]
    private void BrowserBack()
    {
        if (WebViewService.CanGoBack)
        {
            WebViewService.GoBack();
        }
    }

    /// <summary>
    /// Determines whether the browser can navigate backward in the history.
    /// </summary>
    /// <returns><c>true</c> if backward navigation is available; otherwise, <c>false</c>.</returns>
    private bool BrowserCanGoBack()
    {
        return WebViewService.CanGoBack;
    }

    /// <summary>
    /// Called when the view is navigated to.
    /// Subscribes to the NavigationCompleted event.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    public void OnNavigatedTo(object parameter)
    {
        WebViewService.NavigationCompleted += OnNavigationCompleted;
    }

    /// <summary>
    /// Called when the view is navigated away from.
    /// Unsubscribes from events and cleans up resources.
    /// </summary>
    public void OnNavigatedFrom()
    {
        WebViewService.UnregisterEvents();
        WebViewService.NavigationCompleted -= OnNavigationCompleted;
    }

    /// <summary>
    /// Handles the navigation completed event from the WebView.
    /// Updates loading state, command availability, and error status.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="webErrorStatus">The error status of the navigation, or default if successful.</param>
    private void OnNavigationCompleted(object? sender, CoreWebView2WebErrorStatus webErrorStatus)
    {
        IsLoading = false;
        BrowserBackCommand.NotifyCanExecuteChanged();
        BrowserForwardCommand.NotifyCanExecuteChanged();

        if (webErrorStatus != default)
        {
            HasFailures = true;
        }
    }

    /// <summary>
    /// Retries loading the current page after a navigation failure.
    /// Resets the failure state and reloads the WebView.
    /// </summary>
    [RelayCommand]
    private void OnRetry()
    {
        HasFailures = false;
        IsLoading = true;
        WebViewService?.Reload();
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CopilotDesktop.Contracts.Services;
using CopilotDesktop.Contracts.ViewModels;
using CopilotDesktop.Models;

using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace CopilotDesktop.ViewModels;

/// <summary>
/// ViewModel for the WebView page that displays the Copilot web interface.
/// Manages navigation, loading states, and browser controls for the WebView2 component.
/// </summary>
public partial class WebViewViewModel : ObservableRecipient, INavigationAware
{
    private const string _sourceKey = "WebViewSource";

    /// 

    /// Gets or sets the current URI being displayed in the WebView.
    /// Loaded from local settings and can be saved back.
    /// 

    [ObservableProperty]
    private Uri source;

    /// 

    /// Gets or sets a value indicating whether the WebView is currently loading content.
    /// 

    [ObservableProperty]
    private bool isLoading = true;

    /// 

    /// Gets or sets a value indicating whether navigation failures have occurred.
    /// Used to display error UI to the user.
    /// 

    [ObservableProperty]
    private bool hasFailures;

    /// 

    /// Gets the WebView service that provides browser functionality.
    /// 

    public IWebViewService WebViewService
    {
        get;
    }

    private readonly ILocalSettingsService _localSettingsService;
    private readonly CopilotDesktop.Services.IProviderService _providerService;

    /// 

    /// Initializes a new instance of the  class.
    /// 

    /// The WebView service for browser operations.
    /// The local settings service for persisting settings.
    public Task Initialization { get; }

    public WebViewViewModel(IWebViewService webViewService, ILocalSettingsService localSettingsService, CopilotDesktop.Services.IProviderService providerService)
    {
        WebViewService = webViewService;
        _localSettingsService = localSettingsService;
        _providerService = providerService;

        Initialization = InitializeSourceAsync();

        // react to provider changes
        _providerService.SelectedProviderChanged += (provider) =>
        {
            if (provider != null)
            {
                Source = new System.Uri(provider.Url);
            }
        };
    }

    private async Task InitializeSourceAsync()
    {
        if (_providerService != null)
        {
            await _providerService.InitializeAsync();
        }

        // Prefer the configured/default provider URL on app start (first-run/default behavior).
        // Fall back to a previously saved WebViewSource only if no selected provider URL exists.
        var selectedProviderUrl = _providerService?.SelectedProviderUrl;
        var savedUriString = await _localSettingsService.ReadSettingAsync<string>(_sourceKey);

        if (!string.IsNullOrEmpty(selectedProviderUrl))
        {
            Source = new Uri(selectedProviderUrl);
        }
        else if (!string.IsNullOrEmpty(savedUriString))
        {
            Source = new Uri(savedUriString);
        }
        else
        {
            Source = new Uri("https://copilot.microsoft.com");
        }
    }

    /// 

    /// Opens the current WebView source URL in the system's default browser.
    /// 

    /// A task representing the asynchronous operation.
    [RelayCommand]
    private async Task OpenInBrowser()
    {
        if (WebViewService.Source != null)
        {
            await Windows.System.Launcher.LaunchUriAsync(WebViewService.Source);
        }
    }

    /// 

    /// Reloads the current page in the WebView.
    /// 

    [RelayCommand]
    private void Reload() => WebViewService.Reload();

    /// 

    /// Navigates forward in the browser history if possible.
    /// 

    /// 
    /// The command's CanExecute state is controlled by .
    /// 
    [RelayCommand(CanExecute = nameof(BrowserCanGoForward))]
    private void BrowserForward()
    {
        if (WebViewService.CanGoForward) WebViewService.GoForward();
    }

    /// 

    /// Determines whether the browser can navigate forward in the history.
    /// 

    /// true if forward navigation is available; otherwise, false.
    private bool BrowserCanGoForward() => WebViewService.CanGoForward;

    /// 

    /// Navigates backward in the browser history if possible.
    /// 

    /// 
    /// The command's CanExecute state is controlled by .
    /// 
    [RelayCommand(CanExecute = nameof(BrowserCanGoBack))]
    private void BrowserBack()
    {
        if (WebViewService.CanGoBack) WebViewService.GoBack();
    }

    /// 

    /// Determines whether the browser can navigate backward in the history.
    /// 

    /// true if backward navigation is available; otherwise, false.
    private bool BrowserCanGoBack() => WebViewService.CanGoBack;

    /// 

    /// Called when the view is navigated to.
    /// Subscribes to the NavigationCompleted event.
    /// 

    /// Optional navigation parameter.
    public void OnNavigatedTo(object parameter)
    {
        WebViewService.NavigationCompleted += OnNavigationCompleted;
    }

    /// 

    /// Called when the view is navigated away from.
    /// Unsubscribes from events and cleans up resources.
    /// 

    public void OnNavigatedFrom()
    {
        WebViewService.UnregisterEvents();
        WebViewService.NavigationCompleted -= OnNavigationCompleted;

        _ = SaveSourceAsync(Source);
    }

    private async Task SaveSourceAsync(Uri uri)
    {
        if (uri == null) return;
        await _localSettingsService.SaveSettingAsync(_sourceKey, uri.AbsoluteUri);
    }

    /// 

    /// Handles the navigation completed event from the WebView.
    /// Updates loading state, command availability, and error status.
    /// 

    /// The event sender.
    /// The error status of the navigation, or default if successful.
    private void OnNavigationCompleted(object? sender, CoreWebView2WebErrorStatus webErrorStatus)
    {
        IsLoading = false;
        BrowserBackCommand.NotifyCanExecuteChanged();
        BrowserForwardCommand.NotifyCanExecuteChanged();
        // set HasFailures based on the reported web error status; clear failures on success
        HasFailures = webErrorStatus != default;
    }

    /// 

    /// Retries loading the current page after a navigation failure.
    /// Resets the failure state and reloads the WebView.
    /// 

    [RelayCommand]
    private void OnRetry()
    {
        HasFailures = false;
        IsLoading = true;
        WebViewService?.Reload();
    }
}
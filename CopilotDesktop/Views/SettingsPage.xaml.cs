using CopilotDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using CopilotDesktop.Models;
using System.Linq;
using CopilotDesktop.Services;

namespace CopilotDesktop.Views;

public sealed partial class SettingsPage : Page
{
    private IProviderService _providerService;

    public SettingsViewModel ViewModel
    {
        get;
    }

    private async void DefaultProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DefaultProviderCombo == null) return;
        if (DefaultProviderCombo.SelectedItem is ProviderItem provider)
        {
            await _providerService.SetSelectedProviderAsync(provider);
        }
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        _providerService = App.GetService<IProviderService>();

        // start provider service initialization and wire up UI after load
        _ = InitializeProvidersAsync();
    }

    private async Task InitializeProvidersAsync()
    {
        try
        {
            await _providerService.InitializeAsync();

            if (EntriesList != null)
            {
                EntriesList.ItemsSource = _providerService.UserProviders;
            }

            if (DefaultProviderCombo != null)
            {
                DefaultProviderCombo.ItemsSource = _providerService.CombinedProviders;
                // select current
                var selected = _providerService.CombinedProviders.FirstOrDefault(p => p.Url == _providerService.SelectedProviderUrl);
                if (selected != null)
                    DefaultProviderCombo.SelectedItem = selected;
            }

            _providerService.SelectedProviderChanged += (provider) =>
            {
                // nothing to do in settings page for now
            };
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeProviders failed: {ex}");
        }
    }

    private async void AddEntry_Click(object sender, RoutedEventArgs e)
    {
        if (NameInput == null || UrlInput == null)
        {
            System.Diagnostics.Debug.WriteLine("NameInput or UrlInput control is missing in XAML.");
            return;
        }

        string name = NameInput.Text?.Trim();
        string url = UrlInput.Text?.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
        {
            ShowValidation("Name and URL are required.");
            return;
        }

        // If scheme missing, assume https
        if (!url.Contains("://"))
        {
            url = "https://" + url;
        }

        if (!IsValidUrl(url))
        {
            ShowValidation("Please enter a valid URL (http or https). Example: https://example.com");
            return;
        }

        HideValidation();
        var provider = new ProviderItem { Name = name, Url = url };
        await _providerService.AddUserProviderAsync(provider);
        NameInput.Text = string.Empty;
        UrlInput.Text = string.Empty;
    }

    private async void RemoveEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var entry = btn.Tag as ProviderItem;
            if (entry != null)
            {
                await _providerService.RemoveUserProviderAsync(entry);
            }
        }
    }

    private bool IsValidUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }
        return false;
    }

    private void ShowValidation(string message)
    {
        try
        {
            if (ValidationMessage != null)
            {
                ValidationMessage.Text = message;
                ValidationMessage.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    private void HideValidation()
    {
        try
        {
            if (ValidationMessage != null)
            {
                ValidationMessage.Text = string.Empty;
                ValidationMessage.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    // persistence and provider management handled by ProviderService
}
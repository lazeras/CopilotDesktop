using CopilotDesktop.Contracts.Services;
using CopilotDesktop.Helpers;
using CopilotDesktop.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using CopilotDesktop.Models;

using Windows.System;

namespace CopilotDesktop.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // initialize provider combo
        _ = InitializeProviderComboAsync();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = "AppDisplayName".GetLocalized();
    }

    private async Task InitializeProviderComboAsync()
    {
        try
        {
            var providerService = App.GetService<CopilotDesktop.Services.IProviderService>();
            await providerService.InitializeAsync();
                if (ProviderCombo != null)
                {
                    ProviderCombo.ItemsSource = providerService.CombinedProviders;
                    var selected = providerService.CombinedProviders.FirstOrDefault(p => string.Equals(p.Url?.Trim(), providerService.SelectedProviderUrl?.Trim(), System.StringComparison.OrdinalIgnoreCase));
                    if (selected != null) ProviderCombo.SelectedItem = selected;
                }

            providerService.SelectedProviderChanged += (provider) =>
            {
                try
                {
                    if (ProviderCombo != null && provider != null)
                    {
                        ProviderCombo.SelectedItem = provider;
                    }
                }
                catch { }
            };
        }
        catch { }
    }

    private async void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProviderCombo == null) return;
        if (ProviderCombo.SelectedItem is CopilotDesktop.Models.ProviderItem provider)
        {
            var providerService = App.GetService<CopilotDesktop.Services.IProviderService>();
            // Select provider for the current session only (do not change the persisted default)
            await providerService.SelectProviderTransientAsync(provider);
        }
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText as UIElement;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }
}
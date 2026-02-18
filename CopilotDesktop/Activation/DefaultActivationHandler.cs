using CopilotDesktop.Contracts.Services;
using CopilotDesktop.ViewModels;

using Microsoft.UI.Xaml;

namespace CopilotDesktop.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        _navigationService.NavigateTo(typeof(WebViewViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
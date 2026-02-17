using CopilotDesktop.Activation;
using CopilotDesktop.Contracts.Services;
using CopilotDesktop.Core.Contracts.Services;
using CopilotDesktop.Core.Services;
using CopilotDesktop.Helpers;
using CopilotDesktop.Models;
using CopilotDesktop.Notifications;
using CopilotDesktop.Services;
using CopilotDesktop.ViewModels;
using CopilotDesktop.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace CopilotDesktop;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<IWebViewService, WebViewService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<HotkeyService>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<WebViewViewModel>();
            services.AddTransient<WebViewPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        //App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, e.Message);

        e.Handled = true;
    }

    private static void LogException(Exception? exception, string message)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CopilotDesktop", "Logs");
            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, $"errors_{DateTime.Now:yyyy-MM-dd}.log");

            var exceptionDetails = exception != null
                ? $"Exception Type: {exception.GetType().FullName}\n" +
                  $"Message: {exception.Message}\n" +
                  $"Stack Trace:\n{exception.StackTrace}\n" +
                  $"Inner Exception: {exception.InnerException?.Message ?? "None"}"
                : "No exception details available";

            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Unhandled Exception\n" +
                            $"Error Message: {message}\n" +
                            $"{exceptionDetails}\n" +
                            $"{new string('-', 80)}\n\n";

            File.AppendAllText(logFile, logMessage);

            TryLogToWindowsEventLog(message, exceptionDetails);
        }
        catch
        {
        }
    }

    private static void TryLogToWindowsEventLog(string message, string exceptionDetails)
    {
        try
        {
            const string source = "CopilotDesktop";
            const string logName = "Application";

            if (!System.Diagnostics.EventLog.SourceExists(source))
            {
                System.Diagnostics.EventLog.CreateEventSource(source, logName);
            }

            var logMessage = $"Unhandled exception in CopilotDesktop\n\n" +
                            $"Error Message: {message}\n\n" +
                            $"{exceptionDetails}";

            System.Diagnostics.EventLog.WriteEntry(source, logMessage, System.Diagnostics.EventLogEntryType.Error, 1001);
        }
        catch
        {
        }
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        //App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}

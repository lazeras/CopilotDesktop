using CopilotDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CopilotDesktop.Views;

// To learn more about WebView2, see https://docs.microsoft.com/microsoft-edge/webview2/.
public sealed partial class WebViewPage : Page
{
    public WebViewViewModel ViewModel
    {
        get;
    }

    public WebViewPage()
    {
        ViewModel = App.GetService<WebViewViewModel>();
        InitializeComponent();

        ViewModel.WebViewService.Initialize(CopilotView); 
        this.Loaded += MainWindow_Loaded;
    }


    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var window = App.MainWindow;
        if (window != null)
        {
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(AppTitleBar);
        }

        await CopilotView.EnsureCoreWebView2Async(); 
        CopilotView.Source = new Uri("https://copilot.microsoft.com");
    }

/*    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CopilotView.EnsureCoreWebView2Async(); CopilotView.Source = new Uri("https://copilot.microsoft.com");
    }*/
}

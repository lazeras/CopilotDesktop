using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CopilotDesktop.Views
{
    /// <summary>
    /// A compact mini-window for quick Copilot access.
    /// Features always-on-top behavior and custom title bar.
    /// </summary>
    public sealed partial class MiniChatWindow : Window
    {
        private bool _isInitialized = false;

        public MiniChatWindow()
        {
            try
            {
                this.InitializeComponent();

                // Custom title bar
                this.ExtendsContentIntoTitleBar = true;
                this.SetTitleBar(TitleBarGrid);

                // Set window size
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 660));

                // Always on top
                if (this.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsAlwaysOnTop = true;
                }

                this.Activated += MiniChatWindow_Activated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] MiniChatWindow constructor: {ex}");
                throw;
            }
        }

        private async void MiniChatWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (!_isInitialized && e.WindowActivationState != WindowActivationState.Deactivated)
            {
                _isInitialized = true;

                try
                {
                    Debug.WriteLine("[INFO] MiniChatWindow: Initializing WebView2...");

                    // Small delay to ensure window is fully ready
                    await Task.Delay(100);

                    if (MiniWebView != null)
                    {
                        await MiniWebView.EnsureCoreWebView2Async();
                        MiniWebView.Source = new Uri("https://copilot.microsoft.com");
                        Debug.WriteLine("[INFO] MiniChatWindow: WebView2 initialized successfully");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] MiniChatWindow WebView2 initialization: {ex}");
                    // Don't crash - window can still be used
                }
            }
        }
    }
}
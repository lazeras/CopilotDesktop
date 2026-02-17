using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CopilotDesktop.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MiniChatWindow : Window
    {
        private bool _isInitialized = false;

        public MiniChatWindow()
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
        private async void MiniChatWindow_Activated(object sender, WindowActivatedEventArgs e) 
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                await MiniWebView.EnsureCoreWebView2Async(); 
                MiniWebView.Source = new Uri("https://copilot.microsoft.com");
            }
        }
    }
}

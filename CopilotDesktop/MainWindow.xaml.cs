using CopilotDesktop.Helpers;
using CopilotDesktop.Services;
using CopilotDesktop.Views;
using Windows.UI.ViewManagement;
using System.Runtime.InteropServices;

namespace CopilotDesktop;

public sealed partial class MainWindow : WindowEx
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    private HotkeyService? _hotkey; 
    private MiniChatWindow? _miniWindow;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        // Set explicit background to prevent transparency
        this.SystemBackdrop = null;

        // Set border color to dark color (BGR format: #151a28 -> 0x00281a15)
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        int borderColor = 0x00281a15; // Dark blue-gray to match background
        DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        Closed += MainWindow_Closed;

        _hotkey = new HotkeyService(this); 
        _hotkey.HotkeyPressed += ToggleMiniWindow;

    }

    private void ToggleMiniWindow()
    {
        if (_miniWindow == null) 
        { 
            _miniWindow = new MiniChatWindow(); 
            _miniWindow.Closed += (s, e) => 
            { 
                _miniWindow = null;
                this.AppWindow.Show();
                this.Activate();
            }; 
            _miniWindow.Activate();
            this.AppWindow.Hide();
        } else { 
            _miniWindow.Close(); 
            _miniWindow = null; 
        }
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        if (settings != null)
        {
            settings.ColorValuesChanged -= Settings_ColorValuesChanged;
        }

        if (_hotkey != null)
        {
            _hotkey.Dispose();
            _hotkey = null;
        }
    }

    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
}

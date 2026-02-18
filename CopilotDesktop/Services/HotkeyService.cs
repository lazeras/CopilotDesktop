using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace CopilotDesktop.Services
{
    /// <summary>
    /// Service that registers and manages global system hotkeys for the application.
    /// Uses Win32 APIs to intercept keyboard input system-wide and trigger actions when the hotkey is pressed.
    /// </summary>
    /// <remarks>
    /// Currently registers Ctrl+Shift+C as the global hotkey.
    /// Implements window procedure hooking to receive and process WM_HOTKEY messages.
    /// </remarks>
    public class HotkeyService : IDisposable
    {
        #region Win32 API Imports

        /// <summary>
        /// Registers a system-wide hotkey.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        /// <summary>
        /// Unregisters a previously registered hotkey.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Sets the window procedure for 32-bit applications.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        /// <summary>
        /// Sets the window procedure for 64-bit applications.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        /// <summary>
        /// Calls the default window procedure for message processing.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Delegate for custom window procedure callback.
        /// </summary>
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        #endregion Win32 API Imports

        #region Constants

        /// <summary>
        /// Window procedure index for SetWindowLong/SetWindowLongPtr.
        /// </summary>
        private const int GWL_WNDPROC = -4;

        /// <summary>
        /// Windows message ID for hotkey notifications.
        /// </summary>
        private const uint WM_HOTKEY = 0x0312;

        /// <summary>
        /// Modifier key flag for the Control key.
        /// </summary>
        private const uint MOD_CONTROL = 0x0002;

        /// <summary>
        /// Modifier key flag for the Shift key.
        /// </summary>
        private const uint MOD_SHIFT = 0x0004;

        /// <summary>
        /// Unique identifier for the registered hotkey.
        /// </summary>
        private const int HOTKEY_ID = 9000;

        #endregion Constants

        #region Fields

        private readonly Window _window;
        private readonly IntPtr _hwnd;
        private IntPtr _oldWndProc;
        private WndProcDelegate _newWndProc;
        private bool _disposed;

        #endregion Fields

        #region Events

        /// <summary>
        /// Event raised when the registered hotkey (Ctrl+Shift+C) is pressed.
        /// </summary>
        public event Action? HotkeyPressed;

        #endregion Events

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyService"/> class and registers the global hotkey.
        /// </summary>
        /// <param name="window">The WinUI 3 window that will receive hotkey messages.</param>
        /// <remarks>
        /// Automatically registers Ctrl+Shift+C as the global hotkey upon instantiation.
        /// The window procedure is hooked to intercept WM_HOTKEY messages.
        /// </remarks>
        public HotkeyService(Window window)
        {
            _window = window;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Create new window procedure delegate (must be stored to prevent garbage collection)
            _newWndProc = new WndProcDelegate(WndProc);

            // Hook the window procedure (use different APIs for 32-bit vs 64-bit)
            if (IntPtr.Size == 8)
            {
                // 64-bit: Use SetWindowLongPtr
                _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, _newWndProc);
            }
            else
            {
                // 32-bit: Use SetWindowLong
                _oldWndProc = new IntPtr(SetWindowLong(_hwnd, GWL_WNDPROC, _newWndProc));
            }

            // Register the global hotkey: Ctrl+Shift+C
            RegisterHotKey(_hwnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Windows.System.VirtualKey.C);
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Custom window procedure that intercepts and processes window messages.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">Additional message information (message-specific).</param>
        /// <param name="lParam">Additional message information (message-specific).</param>
        /// <returns>The result of message processing.</returns>
        /// <remarks>
        /// Listens for WM_HOTKEY messages and raises the <see cref="HotkeyPressed"/> event when detected.
        /// All other messages are passed to the original window procedure.
        /// </remarks>
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // Check if this is a hotkey message with our registered ID
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Raise the event to notify subscribers
                HotkeyPressed?.Invoke();
                return IntPtr.Zero;
            }

            // Pass all other messages to the original window procedure
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Unregisters the hotkey and restores the original window procedure.
        /// </summary>
        /// <remarks>
        /// This method should be called when the service is no longer needed to free system resources.
        /// It's important to unregister hotkeys to prevent conflicts with other applications.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // Unregister the hotkey
            UnregisterHotKey(_hwnd, HOTKEY_ID);

            // Restore the original window procedure if it was saved
            if (_oldWndProc != IntPtr.Zero)
            {
                if (IntPtr.Size == 8)
                {
                    // 64-bit: Restore using SetWindowLongPtr
                    SetWindowLongPtr(_hwnd, GWL_WNDPROC, new WndProcDelegate((h, m, w, l) => CallWindowProc(_oldWndProc, h, m, w, l)));
                }
                else
                {
                    // 32-bit: Restore using SetWindowLong
                    SetWindowLong(_hwnd, GWL_WNDPROC, new WndProcDelegate((h, m, w, l) => CallWindowProc(_oldWndProc, h, m, w, l)));
                }
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
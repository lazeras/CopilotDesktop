using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CopilotDesktop.Services
{
    class HotkeyService
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWL_WNDPROC = -4;
        private const uint WM_HOTKEY = 0x0312;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const int HOTKEY_ID = 9000;

        private readonly Window _window;
        private readonly IntPtr _hwnd;
        private IntPtr _oldWndProc;
        private WndProcDelegate _newWndProc;

        public event Action? HotkeyPressed;

        public HotkeyService(Window window)
        {
            _window = window;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            _newWndProc = new WndProcDelegate(WndProc);

            if (IntPtr.Size == 8)
            {
                _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, _newWndProc);
            }
            else
            {
                _oldWndProc = new IntPtr(SetWindowLong(_hwnd, GWL_WNDPROC, _newWndProc));
            }

            RegisterHotKey(_hwnd, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Windows.System.VirtualKey.C);
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                return IntPtr.Zero;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);

            if (_oldWndProc != IntPtr.Zero)
            {
                if (IntPtr.Size == 8)
                {
                    SetWindowLongPtr(_hwnd, GWL_WNDPROC, new WndProcDelegate((h, m, w, l) => CallWindowProc(_oldWndProc, h, m, w, l)));
                }
                else
                {
                    SetWindowLong(_hwnd, GWL_WNDPROC, new WndProcDelegate((h, m, w, l) => CallWindowProc(_oldWndProc, h, m, w, l)));
                }
            }
        }
    }
}

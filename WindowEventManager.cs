using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using TrueReplayer.Interop;
using TrueReplayer.Services;
using WinRT.Interop;

namespace TrueReplayer.Managers
{
    public class WindowEventManager
    {
        private readonly Window window;
        private readonly IntPtr hwnd;

        private const int WM_USER = 0x0400;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int SC_MINIMIZE = 0xF020;
        private const int SW_RESTORE = 9;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public WindowEventManager(Window window)
        {
            this.window = window;
            hwnd = WindowNative.GetWindowHandle(window);
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam)!;
                mmi.ptMinTrackSize.x = 850;
                mmi.ptMinTrackSize.y = 510;
                Marshal.StructureToPtr(mmi, lParam, true);
                return IntPtr.Zero;
            }

            if (msg == WM_USER + 1)
            {
                if ((int)lParam == WM_LBUTTONDBLCLK)
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                }
                else if ((int)lParam == WM_RBUTTONUP)
                {
                    TrayIconService.ShowContextMenu();
                }
            }
            else if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;

                if (command == SC_MINIMIZE && ((MainWindow)window).MinimizeToTraySwitch.IsOn)
                {
                    TrayIconService.Initialize((MainWindow)window, hwnd);
                    TrayIconService.ShowMinimizeBalloon();

                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    AppWindow.GetFromWindowId(windowId).Hide();

                    return IntPtr.Zero;
                }
            }

            return HwndHookManager.CallOriginalWndProc(hwnd, msg, wParam, lParam);
        }

        public void UpdateAlwaysOnTop(bool isAlwaysOnTop)
        {
            SetWindowPos(hwnd,
                isAlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        #region PInvoke

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        #endregion
    }

    // Structs usadas para WM_GETMINMAXINFO
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
}

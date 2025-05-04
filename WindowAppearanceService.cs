using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;
using TrueReplayer.Models;

namespace TrueReplayer.Services
{
    public static class WindowAppearanceService
    {
        public static void Configure(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Define tamanho inicial
            appWindow.Resize(new SizeInt32(850, 510));

            CustomizeTitleBar(appWindow);
            CenterWindow(appWindow, windowId);
        }

        public static void CustomizeTitleBar(AppWindow appWindow)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        public static void CenterWindow(AppWindow appWindow, WindowId windowId)
        {
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var centerPosition = new PointInt32
            {
                X = displayArea.WorkArea.X + (displayArea.WorkArea.Width - appWindow.Size.Width) / 2,
                Y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - appWindow.Size.Height) / 2
            };

            appWindow.Move(centerPosition);
        }

        public static void ApplyWindowState(Window window, UserProfile profile)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            SetWindowPos(hwnd, profile.AlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            if (profile.IsMaximized)
            {
                ShowWindow(hwnd, SW_MAXIMIZE);
            }
            else
            {
                ShowWindow(hwnd, SW_RESTORE);

                if (profile.WindowX >= 0 && profile.WindowY >= 0)
                {
                    appWindow.Move(new PointInt32(profile.WindowX, profile.WindowY));
                }
                else
                {
                    CenterWindow(appWindow, windowId);
                }

                appWindow.Resize(new SizeInt32(
                    profile.WindowWidth > 0 ? profile.WindowWidth : 790,
                    profile.WindowHeight > 0 ? profile.WindowHeight : 510
                ));
            }
        }

        public static void CaptureWindowState(Window window, UserProfile profile)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var placement = GetWindowPlacement(hwnd);
            profile.IsMaximized = placement.showCmd == SW_MAXIMIZE;

            if (!profile.IsMaximized)
            {
                profile.WindowX = appWindow.Position.X;
                profile.WindowY = appWindow.Position.Y;
                profile.WindowWidth = appWindow.Size.Width;
                profile.WindowHeight = appWindow.Size.Height;
            }
            else
            {
                profile.WindowX = placement.rcNormalPosition.X;
                profile.WindowY = placement.rcNormalPosition.Y;
                profile.WindowWidth = placement.rcNormalPosition.Width;
                profile.WindowHeight = placement.rcNormalPosition.Height;
            }
        }

        #region Win32

        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static WINDOWPLACEMENT GetWindowPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new()
            {
                length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWPLACEMENT))
            };
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion
    }
}

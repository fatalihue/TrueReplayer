using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using TrueReplayer.Models;
using TrueReplayer.Interop;
using System.Threading.Tasks;

namespace TrueReplayer.Services
{
    public static class TrayIconService
    {
        private const int WM_USER = 0x0400;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        private static IntPtr hwnd;
        private static NotifyIconData notifyIcon;
        private static bool isInitialized = false;

        public static void Initialize(object window, IntPtr windowHandle, bool showNotification = false)
        {
            if (isInitialized) return;
            hwnd = windowHandle;
            CreateTrayIcon(showNotification);
            isInitialized = true;
        }

        public static void CreateTrayIcon(bool showNotification = false)
        {
            notifyIcon = new NotifyIconData
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NotifyIconData)),
                hWnd = hwnd,
                uID = 1,
                uFlags = 0x01 | 0x02 | 0x04,
                uCallbackMessage = WM_USER + 1,
                hIcon = LoadImage(IntPtr.Zero, Path.Combine(AppContext.BaseDirectory, "TrueReplayer.ico"), 1, 0, 0, 0x00000010),
                szTip = "TrueReplayer"
            };

            Shell_NotifyIcon(0x00000000, ref notifyIcon);

            if (showNotification)
            {
                var trayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "TrueReplayer.ico")),
                    Visible = true,
                    BalloonTipTitle = "TrueReplayer está em segundo plano",
                    BalloonTipText = "Clique no ícone da bandeja para restaurar a janela."
                };

                trayIcon.ShowBalloonTip(3000);
                Task.Delay(5000).ContinueWith(_ => trayIcon.Dispose());
            }
        }

        public static void ShowMinimizeBalloon()
        {
            NotifyIconData data = notifyIcon;
            data.uFlags |= 0x10;
            data.szInfoTitle = "TrueReplayer is running in the background";
            data.szInfo = "Click the tray icon to restore the window.";
            data.dwInfoFlags = 0x01;
            Shell_NotifyIcon(0x00000001, ref data);
        }

        public static void RemoveTrayIcon()
        {
            Shell_NotifyIcon(0x00000002, ref notifyIcon);
        }

        public static void ShowContextMenu()
        {
            IntPtr hMenu = CreatePopupMenu();
            AppendMenu(hMenu, 0x0000, 1, "Restaurar");
            AppendMenu(hMenu, 0x0000, 2, "Sair");

            GetCursorPos(out POINT pt);
            SetForegroundWindow(hwnd);
            int cmd = TrackPopupMenu(hMenu, 0x0100, pt.x, pt.y, 0, hwnd, IntPtr.Zero);

            if (cmd == 1) ShowWindow(hwnd, 9);
            else if (cmd == 2) Microsoft.UI.Xaml.Application.Current.Exit();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NotifyIconData
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll")] private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);
        [DllImport("user32.dll")] private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpdata);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }

    public static class WindowAppearanceService
    {
        public static void Configure(Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(885, 510));
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
            var centerPosition = new Windows.Graphics.PointInt32
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
                if (profile.WindowX != -1 && profile.WindowY != -1)
                    appWindow.Move(new Windows.Graphics.PointInt32(profile.WindowX, profile.WindowY));
                else
                    CenterWindow(appWindow, windowId);

                appWindow.Resize(new Windows.Graphics.SizeInt32(
                    profile.WindowWidth > 0 ? profile.WindowWidth : 885,
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

        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length, flags, showCmd;
            public System.Drawing.Point ptMinPosition, ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        [DllImport("user32.dll")] private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public static WINDOWPLACEMENT GetWindowPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new() { length = Marshal.SizeOf(typeof(WINDOWPLACEMENT)) };
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }
    }

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
            hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam)!;
                mmi.ptMinTrackSize.x = 885;
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

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}

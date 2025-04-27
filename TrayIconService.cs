using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            notifyIcon = new NotifyIconData();
            notifyIcon.cbSize = (uint)Marshal.SizeOf(typeof(NotifyIconData));
            notifyIcon.hWnd = hwnd;
            notifyIcon.uID = 1;
            notifyIcon.uFlags = 0x01 | 0x02 | 0x04;
            notifyIcon.uCallbackMessage = WM_USER + 1;

            string iconPath = Path.Combine(AppContext.BaseDirectory, "TrueReplayer.ico");
            notifyIcon.hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x00000010);
            notifyIcon.szTip = "TrueReplayer";

            Shell_NotifyIcon(0x00000000, ref notifyIcon);

            if (showNotification)
            {
                var trayIcon = new NotifyIcon
                {
                    Icon = new System.Drawing.Icon(iconPath),
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

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;

            public uint dwState;
            public uint dwStateMask;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;

            public uint uTimeoutOrVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;

            public uint dwInfoFlags;

            public Guid guidItem;

            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x, y;
        }

        #region P/Invoke

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll")] private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);
        [DllImport("user32.dll")] private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpdata);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        #endregion
    }
}

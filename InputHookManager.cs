using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TrueReplayer.Helpers;
using TrueReplayer.Interop;

namespace TrueReplayer
{
    public static class InputHookManager
    {
        public static event Action<string, int, int, bool, int>? OnMouseEvent;
        public static event Action<string, bool>? OnKeyEvent;
        public static event Action<string>? OnHotkeyPressed;

        private static IntPtr _mouseHookId = IntPtr.Zero;
        private static IntPtr _keyboardHookId = IntPtr.Zero;

        private static NativeMethods.LowLevelMouseProc _mouseProc = MouseHookCallback;
        private static NativeMethods.LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;

        private static string _recordingHotkey = "F9";
        private static string _replayHotkey = "F10";

        private static DateTime? lastAltRightPressTime = null;


        public static void Start()
        {
            if (_mouseHookId == IntPtr.Zero)
                _mouseHookId = NativeMethods.SetMouseHook(_mouseProc);
            if (_keyboardHookId == IntPtr.Zero)
                _keyboardHookId = NativeMethods.SetKeyboardHook(_keyboardProc);
            System.Diagnostics.Debug.WriteLine($"Hooks iniciados: Mouse={_mouseHookId != IntPtr.Zero}, Keyboard={_keyboardHookId != IntPtr.Zero}");
        }

        public static void UpdateHotkeys(string recordingKey, string replayKey)
        {
            _recordingHotkey = recordingKey;
            _replayHotkey = replayKey;
            System.Diagnostics.Debug.WriteLine($"Hotkeys atualizados: Recording={_recordingHotkey}, Replay={_replayHotkey}");
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                string? button = null;
                bool isDown = false;
                int scrollDelta = 0;

                switch ((int)wParam)
                {
                    case NativeMethods.WM_LBUTTONDOWN:
                        button = "Left"; isDown = true; break;
                    case NativeMethods.WM_LBUTTONUP:
                        button = "Left"; isDown = false; break;
                    case NativeMethods.WM_RBUTTONDOWN:
                        button = "Right"; isDown = true; break;
                    case NativeMethods.WM_RBUTTONUP:
                        button = "Right"; isDown = false; break;
                    case NativeMethods.WM_MBUTTONDOWN:
                        button = "Middle"; isDown = true; break;
                    case NativeMethods.WM_MBUTTONUP:
                        button = "Middle"; isDown = false; break;
                    case NativeMethods.WM_MOUSEWHEEL:
                        button = "Scroll";
                        scrollDelta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                        break;
                }

                if (button != null)
                {
                    OnMouseEvent?.Invoke(button, hookStruct.pt.x, hookStruct.pt.y, isDown, scrollDelta);
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isDown = wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)0x0104;

                if (vkCode == 165 && isDown)
                {
                    lastAltRightPressTime = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine("Alt Direito detectado. Iniciando supressão de Ctrl.");
                }

                if (vkCode == 162 && isDown && lastAltRightPressTime != null)
                {
                    var elapsed = DateTime.Now - lastAltRightPressTime.Value;
                    if (elapsed.TotalMilliseconds < 100)
                    {
                        System.Diagnostics.Debug.WriteLine("Ctrl suprimido por AltGr.");
                        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                    }
                }

                string key;

                if (vkCode == 164 || vkCode == 165)
                {
                    key = "Alt";
                }
                else
                {
                    key = KeyUtils.NormalizeKeyName(vkCode);
                }

                if (key == _recordingHotkey || key == _replayHotkey)
                {
                    if (isDown)
                    {
                        OnHotkeyPressed?.Invoke(key);
                        System.Diagnostics.Debug.WriteLine($"Hotkey {key} disparada e suprimida.");
                    }
                    return (IntPtr)1;
                }

                OnKeyEvent?.Invoke(key, isDown);
            }

            return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}

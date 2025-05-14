using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using TrueReplayer.Interop;
using TrueReplayer.Models;

namespace TrueReplayer.Services
{
    public class ActionReplayer
    {
        private readonly ObservableCollection<ActionItem> _actions;
        private readonly DispatcherQueue dispatcherQueue;
        private CancellationTokenSource? _cts;
        private int _loopCount = 0;
        private int _loopInterval = 0;

        public event Action<ActionItem>? OnActionExecuting;

        public ActionReplayer(ObservableCollection<ActionItem> actions, DispatcherQueue dispatcherQueue)
        {
            _actions = actions;
            this.dispatcherQueue = dispatcherQueue;
        }

        public void SetLoopOptions(int loopCount, int loopInterval)
        {
            _loopCount = loopCount >= 0 ? loopCount : 0;
            _loopInterval = loopInterval >= 0 ? loopInterval : 0;
            System.Diagnostics.Debug.WriteLine($"Loop options set: Count={_loopCount} (0 = infinito), Interval={_loopInterval}ms");
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            try
            {
                int iteration = 0;
                bool isInfinite = _loopCount == 0;

                while (!_cts.IsCancellationRequested && (isInfinite || iteration < _loopCount))
                {
                    iteration++;
                    System.Diagnostics.Debug.WriteLine($"Iniciando loop {iteration} de {(isInfinite ? "infinito" : _loopCount.ToString())}");

                    foreach (var action in _actions)
                    {
                        if (_cts.IsCancellationRequested) break;

                        int safeDelay = Math.Max(0, action.Delay);
                        await Task.Delay(safeDelay, _cts.Token);

                        dispatcherQueue.TryEnqueue(() =>
                        {
                            OnActionExecuting?.Invoke(action);
                        });

                        switch (action.ActionType)
                        {
                            case "KeyDown": SimulateKey(action.Key, true); break;
                            case "KeyUp": SimulateKey(action.Key, false); break;
                            case "LeftClickDown": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_LEFTDOWN); break;
                            case "LeftClickUp": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_LEFTUP); break;
                            case "RightClickDown": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_RIGHTDOWN); break;
                            case "RightClickUp": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_RIGHTUP); break;
                            case "MiddleClickDown": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_MIDDLEDOWN); break;
                            case "MiddleClickUp": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_MIDDLEUP); break;
                            case "ScrollUp": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_WHEEL, 120); break;
                            case "ScrollDown": SimulateMouse(action.X, action.Y, NativeMethods.MOUSEEVENTF_WHEEL, -120); break;
                        }
                    }

                    if (!_cts.IsCancellationRequested && (isInfinite || iteration < _loopCount) && _loopInterval > 0)
                    {
                        await Task.Delay(_loopInterval, _cts.Token);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Replay cancelado.");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no delay: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            ResetMouseState();
        }

        private void ResetMouseState()
        {
            var pos = GetCurrentMousePosition();
            SimulateMouse(pos.X, pos.Y, NativeMethods.MOUSEEVENTF_LEFTUP);
            SimulateMouse(pos.X, pos.Y, NativeMethods.MOUSEEVENTF_RIGHTUP);
            SimulateMouse(pos.X, pos.Y, NativeMethods.MOUSEEVENTF_MIDDLEUP);
        }

        private (int X, int Y) GetCurrentMousePosition()
        {
            NativeMethods.POINT point;
            NativeMethods.GetCursorPos(out point);
            return (point.x, point.y);
        }

        private void SimulateMouse(int x, int y, uint mouseEvent, int mouseData = 0)
        {
            NativeMethods.SetCursorPos(x, y);

            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                U = new NativeMethods.InputUnion
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)mouseData,
                        dwFlags = mouseEvent,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private void SimulateKey(string key, bool isDown)
        {
            if (!Helpers.KeyUtils.TryResolveVirtualKeyCode(key, out ushort vk))
            {
                System.Diagnostics.Debug.WriteLine($"Tecla desconhecida: {key}");
                return;
            }

            ushort scan = (ushort)NativeMethods.MapVirtualKey(vk, 0);

            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = scan,
                        dwFlags = isDown ? 0 : NativeMethods.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}

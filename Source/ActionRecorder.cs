using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TrueReplayer.Models;

namespace TrueReplayer.Services
{
    public class ActionRecorder
    {
        private readonly ObservableCollection<ActionItem> _actions;
        private readonly Func<int> _getDelay;
        private readonly Action? _onActionAdded;
        private bool _isRecording = false;
        private readonly HashSet<string> _pressedKeys = new();
        private int? insertIndex = null;

        public bool RecordMouse { get; set; } = true;
        public bool RecordScroll { get; set; } = true;
        public bool RecordKeyboard { get; set; } = true;

        public ActionRecorder(ObservableCollection<ActionItem> actions, Func<int>? getDelay = null, Action? onActionAdded = null)
        {
            _actions = actions;
            _getDelay = getDelay ?? (() => 100);
            _onActionAdded = onActionAdded;
        }

        public void SetInsertIndex(int? index)
        {
            if (index.HasValue && (index < 0 || index > _actions.Count))
                insertIndex = null;
            else
                insertIndex = index;
        }

        public void Start() => _isRecording = true;

        public void Stop()
        {
            _isRecording = false;
            _pressedKeys.Clear();
            insertIndex = null;

            foreach (var action in _actions)
                action.IsInsertionPoint = false;
        }

        public bool IsRecording => _isRecording;

        public void RecordKeyboardAction(string key, bool isDown)
        {
            System.Diagnostics.Debug.WriteLine($"[Recorder] RecordKeyboardAction chamado. Gravando? {_isRecording}, Keyboard: {RecordKeyboard}, Key: {key}");

            if (!_isRecording || !RecordKeyboard) return;

            var actionType = isDown ? "KeyDown" : "KeyUp";
            int delay = _getDelay();

            if (isDown)
            {
                if (!_pressedKeys.Contains(key))
                {
                    var action = new ActionItem
                    {
                        ActionType = actionType,
                        Key = key,
                        Delay = delay
                    };

                    System.Diagnostics.Debug.WriteLine($"[Recorder] Adicionando tecla: {action.Key}, Ação: {action.ActionType}, Delay={delay}");

                    AddAction(action);
                    _pressedKeys.Add(key);
                }
            }
            else
            {
                var action = new ActionItem
                {
                    ActionType = actionType,
                    Key = key,
                    Delay = delay
                };

                System.Diagnostics.Debug.WriteLine($"[Recorder] Adicionando tecla: {action.Key}, Ação: {action.ActionType}, Delay={delay}");

                AddAction(action);
                _pressedKeys.Remove(key);
            }
        }

        public void RecordMouseAction(string button, int x, int y, bool isDown, int scrollDelta = 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Recorder] RecordMouseAction chamado. Gravando? {_isRecording}, Mouse: {RecordMouse}, Scroll: {RecordScroll}, Button: {button}");

            if (!_isRecording) return;

            string actionType = button switch
            {
                "Left" => isDown ? "LeftClickDown" : "LeftClickUp",
                "Right" => isDown ? "RightClickDown" : "RightClickUp",
                "Middle" => isDown ? "MiddleClickDown" : "MiddleClickUp",
                "Scroll" => scrollDelta > 0 ? "ScrollUp" : "ScrollDown",
                _ => ""
            };

            if (string.IsNullOrEmpty(actionType)) return;

            if (button == "Scroll" && !RecordScroll) return;
            if (button != "Scroll" && !RecordMouse) return;

            int delay = _getDelay();

            var action = new ActionItem
            {
                ActionType = actionType,
                X = x,
                Y = y,
                Delay = delay
            };

            System.Diagnostics.Debug.WriteLine($"[Recorder] Adicionando ação: {action.ActionType} X={x}, Y={y}, Delay={delay}");

            AddAction(action);
        }

        private void AddAction(ActionItem action)
        {
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= _actions.Count)
            {
                _actions.Insert(insertIndex.Value, action);
                insertIndex++;
            }
            else
            {
                _actions.Add(action);
            }

            _onActionAdded?.Invoke();
        }
    }
}
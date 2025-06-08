﻿using System;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using TrueReplayer.Models;
using TrueReplayer.Services;

namespace TrueReplayer.Controllers
{
    public class MainController
    {
        public static MainController? Instance { get; private set; }

        private readonly ObservableCollection<ActionItem> actions;
        private readonly ActionRecorder recorder;
        private readonly RecordingService recordingService;
        private readonly ReplayService replayService;
        private readonly Button recordingButton;
        private readonly Button replayButton;
        private readonly TextBox delayTextBox;
        private readonly ToggleSwitch customDelaySwitch;
        private readonly DataGrid actionsGrid;

        private DateTime lastActionTime;

        public MainController(
            ObservableCollection<ActionItem> actions,
            ActionRecorder recorder,
            RecordingService recordingService,
            ReplayService replayService,
            Button recordingButton,
            Button replayButton,
            TextBox delayTextBox,
            ToggleSwitch customDelaySwitch,
            DataGrid actionsGrid)
        {
            this.actions = actions;
            this.recorder = recorder;
            this.recordingService = recordingService;
            this.replayService = replayService;
            this.recordingButton = recordingButton;
            this.replayButton = replayButton;
            this.delayTextBox = delayTextBox;
            this.customDelaySwitch = customDelaySwitch;
            this.actionsGrid = actionsGrid;

            Instance = this;
        }

        public void ToggleRecording()
        {
            recordingService.ToggleRecording();
        }

        public void ToggleReplay(bool loopEnabled, string loopCountText, bool intervalEnabled, string intervalText)
        {
            replayService.ToggleReplay(loopEnabled, loopCountText, intervalEnabled, intervalText);
        }

        public void CancelInsertMode()
        {
            recorder.SetInsertIndex(null);
        }

        public void EnableInsertMode(int? index)
        {
            recorder.SetInsertIndex(index);
        }

        public bool IsRecording() => recordingService.IsRecording;

        public bool IsReplayInProgress() => replayService.IsReplaying;

        public void UpdateButtonStates()
        {
            recordingButton.IsEnabled = true;
            replayButton.IsEnabled = actions.Count > 0;
        }

        public void SetLastActionTime(DateTime time)
        {
            lastActionTime = time;
        }

        public int GetDelay()
        {
            return customDelaySwitch.IsOn && int.TryParse(delayTextBox.Text, out int delay) ? delay : 100;
        }

        public void ScrollToLastAction()
        {
            if (actions.Count > 0)
            {
                var lastAction = actions[^1];
                actionsGrid.ScrollIntoView(lastAction, null);
            }
        }

        private string? hotkeyJustPressed;
        private DateTime hotkeyPressTime;

        private DateTime lastRecordingToggleTime = DateTime.MinValue;
        private DateTime lastReplayToggleTime = DateTime.MinValue;

        public void SetLastHotkeyPressed(string key)
        {
            hotkeyJustPressed = key;
            hotkeyPressTime = DateTime.Now;
        }

        public bool IsHotkeyKeyUpSuppressed(string key)
        {
            return hotkeyJustPressed == key && (DateTime.Now - hotkeyPressTime).TotalMilliseconds < 300;
        }

        public bool ShouldSuppressDuplicateRecordingHotkey()
        {
            var now = DateTime.Now;
            if ((now - lastRecordingToggleTime).TotalMilliseconds < 500)
                return true;

            lastRecordingToggleTime = now;
            return false;
        }

        public bool ShouldSuppressDuplicateReplayHotkey()
        {
            var now = DateTime.Now;
            if ((now - lastReplayToggleTime).TotalMilliseconds < 500)
                return true;

            lastReplayToggleTime = now;
            return false;
        }
    }
}

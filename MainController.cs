using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TrueReplayer.Models;
using TrueReplayer.Services;

namespace TrueReplayer.Controllers
{
    public class MainController
    {
        private readonly ObservableCollection<ActionItem> actions;
        private readonly ActionRecorder recorder;
        private readonly RecordingService recordingService;
        private readonly ReplayService replayService;
        private readonly Button recordingButton;
        private readonly Button replayButton;
        private readonly TextBox customDelayTextBox;
        private readonly ToggleSwitch useCustomDelaySwitch;
        private readonly DataGrid actionsGrid;

        private DateTime lastActionTime;
        public Action? OnActionsUpdated;

        public MainController(
            ObservableCollection<ActionItem> actions,
            ActionRecorder recorder,
            RecordingService recordingService,
            ReplayService replayService,
            Button recordingButton,
            Button replayButton,
            TextBox customDelayTextBox,
            ToggleSwitch useCustomDelaySwitch,
            DataGrid actionsGrid)
        {
            this.actions = actions;
            this.recorder = recorder;
            this.recordingService = recordingService;
            this.replayService = replayService;
            this.recordingButton = recordingButton;
            this.replayButton = replayButton;
            this.customDelayTextBox = customDelayTextBox;
            this.useCustomDelaySwitch = useCustomDelaySwitch;
            this.actionsGrid = actionsGrid;
        }

        public bool IsRecording() => recordingService.IsRecording;

        public void ToggleRecording()
        {
            recordingService.ToggleRecording();
            UpdateButtonStates();
        }

        public void ToggleReplay(bool loopEnabled, string loopCountText, bool intervalEnabled, string intervalText)
        {
            replayService.ToggleReplay(loopEnabled, loopCountText, intervalEnabled, intervalText);
            UpdateButtonStates();
        }

        public void UpdateButtonStates()
        {
            recordingButton.IsEnabled = !replayService.IsReplaying;
            replayButton.IsEnabled = !recordingService.IsRecording && actions.Count > 0;

            recordingButton.Background = recordingService.IsRecording
                ? new SolidColorBrush(Colors.LightGreen)
                : null;

            replayButton.Background = replayService.IsReplaying
                ? new SolidColorBrush(Colors.LightCoral)
                : null;
        }

        public int GetDelay()
        {
            if (useCustomDelaySwitch.IsOn)
            {
                if (int.TryParse(customDelayTextBox.Text, out int delay) && delay >= 0)
                    return delay;

                customDelayTextBox.Text = "100";
                return 100;
            }
            else
            {
                DateTime now = DateTime.Now;
                int realDelay = actions.Any() ? (int)(now - lastActionTime).TotalMilliseconds : 0;
                lastActionTime = now;
                return Math.Max(0, realDelay);
            }
        }

        public void ScrollToLastAction()
        {
            if (actions.Any())
            {
                var lastItem = actions.Last();
                actionsGrid.ScrollIntoView(lastItem, null);
                OnActionsUpdated?.Invoke();
            }
        }

        public void SetLastActionTime(DateTime time)
        {
            lastActionTime = time;
        }
    }
} 

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using TrueReplayer.Models;
using CommunityToolkit.WinUI.UI.Controls;

namespace TrueReplayer.Services
{
    public class ReplayService
    {
        private readonly DataGrid actionsGrid;
        private readonly ObservableCollection<ActionItem> actions;
        private readonly ActionReplayer replayer;
        private readonly Button replayButton;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly Action updateButtonStates;

        public bool IsReplaying { get; private set; }

        public ReplayService(
            ObservableCollection<ActionItem> actions,
            Button replayButton,
            DispatcherQueue dispatcherQueue,
            Action updateButtonStates,
            DataGrid actionsGrid)
        {
            this.actions = actions;
            this.replayer = new ActionReplayer(actions, dispatcherQueue);
            this.replayButton = replayButton;
            this.dispatcherQueue = dispatcherQueue;
            this.updateButtonStates = updateButtonStates;
            this.actionsGrid = actionsGrid;

            replayer.OnActionExecuting += (action) =>
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    actionsGrid.SelectedItem = action;
                    actionsGrid.ScrollIntoView(action, null);
                });
            };
        }


        public void ToggleReplay(bool loopEnabled, string loopCountText, bool intervalEnabled, string intervalText)
        {
            if (!IsReplaying && actions.Count > 0)
            {
                StartReplay(loopEnabled, loopCountText, intervalEnabled, intervalText);
            }
            else if (IsReplaying)
            {
                StopReplay();
            }
        }

        private void StartReplay(bool loopEnabled, string loopCountText, bool intervalEnabled, string intervalText)
        {
            IsReplaying = true;
            replayButton.Content = "Stop";
            replayButton.Background = new SolidColorBrush(Colors.LightGreen);

            int loopCount = loopEnabled && int.TryParse(loopCountText, out int count) && count >= 0 ? count : 1;
            int loopInterval = intervalEnabled && int.TryParse(intervalText, out int interval) && interval >= 0 ? interval : 0;

            replayer.SetLoopOptions(loopCount, loopInterval);

            _ = replayer.StartAsync().ContinueWith(_ =>
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    IsReplaying = false;
                    replayButton.Content = "Replay";
                    replayButton.ClearValue(Button.BackgroundProperty);
                    updateButtonStates();
                });
            });
        }

        private void StopReplay()
        {
            replayer.Stop();
            IsReplaying = false;
            replayButton.Content = "Replay";
            replayButton.ClearValue(Button.BackgroundProperty);
            updateButtonStates();
        }
    }
}
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using TrueReplayer.Controllers;
using TrueReplayer.Helpers;
using TrueReplayer.Models;
using TrueReplayer.Services;

namespace TrueReplayer.Managers
{
    public class UIInteractionHandler
    {
        private readonly ObservableCollection<ActionItem> actions;
        private readonly MainController mainController;
        private readonly DataGrid actionsDataGrid;

        public UIInteractionHandler(ObservableCollection<ActionItem> actions, MainController mainController, DataGrid actionsDataGrid)
        {
            this.actions = actions;
            this.mainController = mainController;
            this.actionsDataGrid = actionsDataGrid;
        }

        public void HandleRecordingButtonClick()
        {
            mainController.ToggleRecording();
            actionsDataGrid.Focus(FocusState.Programmatic);
        }

        public void HandleReplayButtonClick(bool loopEnabled, string loopCountText, bool intervalEnabled, string intervalText)
        {
            mainController.ToggleReplay(loopEnabled, loopCountText, intervalEnabled, intervalText);
        }

        public void HandleClearButtonClick()
        {
            actions.Clear();
            mainController.UpdateButtonStates();
        }

        public void HandleCopyButtonClick()
        {
            ClipboardService.CopyActions(actions);
        }

        public void HandleTextBoxSelectAll(object sender)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        public void HandleKeyEditTextBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (actionsDataGrid.SelectedItem is not ActionItem item) return;

            e.Handled = true;

            string? newKey = KeyUtils.NormalizeKeyName((int)e.Key);

            if (string.IsNullOrEmpty(newKey))
            {
                newKey = e.Key switch
                {
                    Windows.System.VirtualKey.Control => "Ctrl",
                    Windows.System.VirtualKey.LeftControl => "LeftCtrl",
                    Windows.System.VirtualKey.RightControl => "RightCtrl",
                    Windows.System.VirtualKey.Shift => "Shift",
                    Windows.System.VirtualKey.LeftShift => "LeftShift",
                    Windows.System.VirtualKey.RightShift => "RightShift",
                    Windows.System.VirtualKey.Menu => "Alt",
                    Windows.System.VirtualKey.LeftMenu => "LeftAlt",
                    Windows.System.VirtualKey.RightMenu => "RightAlt",
                    _ => null
                };
            }

            if (string.IsNullOrEmpty(newKey)) return;

            item.Key = newKey;

            var selectedIndex = actionsDataGrid.SelectedIndex;
            actionsDataGrid.SelectedItem = null;
            actionsDataGrid.SelectedIndex = selectedIndex;
        }
    }
} //upd

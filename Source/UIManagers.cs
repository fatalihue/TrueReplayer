using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TrueReplayer.Controllers;
using TrueReplayer.Helpers;
using TrueReplayer.Models;
using TrueReplayer.Services;
using Windows.System;
using Windows.UI.Core;
using TrueReplayer.Interop; // Added namespace

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

        public void HandleKeyEditTextBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            if (actionsDataGrid.SelectedItem is not ActionItem item) return;

            e.Handled = true;

            // Use NativeMethods to check key states
            bool ctrl = (NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
            bool alt = (NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU (Alt)
            bool shift = (NativeMethods.GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT

            string? mainKey = KeyUtils.NormalizeKeyName((int)e.Key) ?? e.Key.ToString();

            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            if (!string.IsNullOrEmpty(mainKey) && !parts.Contains(mainKey))
                parts.Add(mainKey);

            string newKey = string.Join("+", parts);

            // Only update if the key is valid and not empty
            if (!string.IsNullOrEmpty(newKey))
            {
                item.Key = newKey;
                var selectedIndex = actionsDataGrid.SelectedIndex;
                actionsDataGrid.SelectedItem = null;
                actionsDataGrid.SelectedIndex = selectedIndex;
            }
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
                textBox.SelectAll();
        }
    }

    public class UISettingsManager
    {
        public static UserProfile CreateFromUI(MainWindow window)
        {
            var profile = new UserProfile
            {
                Actions = window.Actions,
                RecordingHotkey = window.ToggleRecordingTextBox.Text,
                ReplayHotkey = window.ToggleReplayTextBox.Text,
                RecordMouse = window.RecordMouseSwitch.IsOn,
                RecordScroll = window.RecordScrollSwitch.IsOn,
                RecordKeyboard = window.RecordKeyboardSwitch.IsOn,
                UseCustomDelay = window.UseCustomDelaySwitch.IsOn,
                CustomDelay = int.TryParse(window.CustomDelayTextBox.Text, out var d) ? d : 100,
                EnableLoop = window.EnableLoopSwitch.IsOn,
                LoopCount = int.TryParse(window.LoopCountTextBox.Text, out var c) ? c : 0,
                LoopIntervalEnabled = window.LoopIntervalSwitch.IsOn,
                LoopInterval = int.TryParse(window.LoopIntervalTextBox.Text, out var i) ? i : 1000,
                ProfileKeyEnabled = window.ProfileKeySwitch.IsOn,
                CustomHotkey = UserProfile.Current.CustomHotkey
            };

            return profile;
        }

        public static void ApplyToUI(MainWindow window, UserProfile profile)
        {
            window.Actions.Clear();
            foreach (var action in profile.Actions)
                window.Actions.Add(action);

            window.ToggleRecordingTextBox.Text = profile.RecordingHotkey;
            window.ToggleReplayTextBox.Text = profile.ReplayHotkey;
            window.RecordMouseSwitch.IsOn = profile.RecordMouse;
            window.RecordScrollSwitch.IsOn = profile.RecordScroll;
            window.RecordKeyboardSwitch.IsOn = profile.RecordKeyboard;
            window.UseCustomDelaySwitch.IsOn = profile.UseCustomDelay;
            window.CustomDelayTextBox.Text = profile.CustomDelay.ToString();
            window.EnableLoopSwitch.IsOn = profile.EnableLoop;
            window.LoopCountTextBox.Text = profile.LoopCount.ToString();
            window.LoopIntervalSwitch.IsOn = profile.LoopIntervalEnabled;
            window.LoopIntervalTextBox.Text = profile.LoopInterval.ToString();

            InputHookManager.UpdateHotkeys(profile.RecordingHotkey, profile.ReplayHotkey);

            window.AlwaysOnTopSwitch_Toggled(null, null);
            window.UpdateButtonStates();
        }
    }

    public class HotkeyManager
    {
        private readonly TextBox recordingTextBox;
        private readonly TextBox replayTextBox;

        public string RecordingHotkey => UserProfile.Current.RecordingHotkey;
        public string ReplayHotkey => UserProfile.Current.ReplayHotkey;

        public event Action<string>? OnHotkeyChanged;

        public HotkeyManager(TextBox recordingTextBox, TextBox replayTextBox)
        {
            this.recordingTextBox = recordingTextBox;
            this.replayTextBox = replayTextBox;

            recordingTextBox.Text = RecordingHotkey;
            replayTextBox.Text = ReplayHotkey;
        }

        public void HandlePreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not TextBox textBox) return;
            textBox.Focus(FocusState.Keyboard);
            e.Handled = true;

            string? newKey = KeyUtils.NormalizeKeyName((int)e.Key);
            if (string.IsNullOrEmpty(newKey)) return;

            var profileHotkeys = InputHookManager.ProfileHotkeys.Values;

            if (profileHotkeys.Contains(newKey, StringComparer.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"Hotkey '{newKey}' está em uso como Profile Key e não pode ser atribuída.");
                return;
            }

            if (newKey == RecordingHotkey || newKey == ReplayHotkey) return;

            if (textBox == recordingTextBox)
                UserProfile.Current.RecordingHotkey = newKey;
            else if (textBox == replayTextBox)
                UserProfile.Current.ReplayHotkey = newKey;
            else return;

            textBox.Text = newKey;
            textBox.SelectionStart = newKey.Length;

            InputHookManager.UpdateHotkeys(
                UserProfile.Current.RecordingHotkey,
                UserProfile.Current.ReplayHotkey);

            OnHotkeyChanged?.Invoke(newKey);
        }
    }

    public class DelayManager
    {
        private readonly TextBox delayTextBox;
        private readonly ObservableCollection<ActionItem> actions;
        private readonly DataGrid dataGrid;

        public DelayManager(TextBox delayTextBox, ObservableCollection<ActionItem> actions, DataGrid dataGrid)
        {
            this.delayTextBox = delayTextBox;
            this.actions = actions;
            this.dataGrid = dataGrid;
        }

        public void HandleKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool isDigit = (e.Key >= Windows.System.VirtualKey.Number0 && e.Key <= Windows.System.VirtualKey.Number9) ||
                           (e.Key >= Windows.System.VirtualKey.NumberPad0 && e.Key <= Windows.System.VirtualKey.NumberPad9);

            bool isControlKey = e.Key == Windows.System.VirtualKey.Back ||
                                e.Key == Windows.System.VirtualKey.Delete ||
                                e.Key == Windows.System.VirtualKey.Left ||
                                e.Key == Windows.System.VirtualKey.Right ||
                                e.Key == Windows.System.VirtualKey.Enter;

            if (!isDigit && !isControlKey)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!int.TryParse(delayTextBox.Text, out int newDelay) || newDelay < 0)
                {
                    delayTextBox.Text = "100";
                    delayTextBox.SelectionStart = delayTextBox.Text.Length;
                    return;
                }

                var selectedItems = dataGrid.SelectedItems.Cast<ActionItem>().ToList();
                if (selectedItems.Any())
                {
                    foreach (var item in selectedItems)
                        item.Delay = newDelay;

                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = actions;
                }
            }
        }

        public void HandleTextChanging(object sender, TextBoxTextChangingEventArgs args)
        {
            if (sender is not TextBox textBox) return;
            string newText = textBox.Text;

            if (string.IsNullOrWhiteSpace(newText) || !newText.All(char.IsDigit) ||
                !int.TryParse(newText, out int delay) || delay < 0)
            {
                textBox.Text = "100";
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
    }

    public class LoopControlManager
    {
        private readonly TextBox loopCountTextBox;
        private readonly ToggleSwitch enableLoopSwitch;
        private readonly TextBox loopIntervalTextBox;
        private readonly ToggleSwitch loopIntervalSwitch;

        public LoopControlManager(TextBox loopCountTextBox, ToggleSwitch enableLoopSwitch, TextBox loopIntervalTextBox, ToggleSwitch loopIntervalSwitch)
        {
            this.loopCountTextBox = loopCountTextBox;
            this.enableLoopSwitch = enableLoopSwitch;
            this.loopIntervalTextBox = loopIntervalTextBox;
            this.loopIntervalSwitch = loopIntervalSwitch;
        }

        public void HandleLoopCountKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (int.TryParse(loopCountTextBox.Text, out int loopCount) && loopCount >= 0)
                    enableLoopSwitch.IsOn = true;
                else
                {
                    loopCountTextBox.Text = "0";
                    enableLoopSwitch.IsOn = false;
                }
            }
        }

        public void HandleLoopCountTextChanging(object sender, TextBoxTextChangingEventArgs args)
        {
            if (sender is not TextBox textBox) return;
            string newText = textBox.Text;
            if (string.IsNullOrEmpty(newText) || !newText.All(char.IsDigit))
            {
                string validText = new string(newText.Where(char.IsDigit).ToArray());
                textBox.Text = string.IsNullOrEmpty(validText) ? "0" : validText;
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        public void HandleLoopIntervalKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (int.TryParse(loopIntervalTextBox.Text, out int loopInterval) && loopInterval >= 0)
                    loopIntervalSwitch.IsOn = true;
                else
                {
                    loopIntervalTextBox.Text = "1000";
                    loopIntervalSwitch.IsOn = false;
                }
            }
        }

        public void HandleLoopIntervalTextChanging(object sender, TextBoxTextChangingEventArgs args)
        {
            if (sender is not TextBox textBox) return;
            string newText = textBox.Text;
            if (string.IsNullOrEmpty(newText) || !newText.All(char.IsDigit))
            {
                string validText = new string(newText.Where(char.IsDigit).ToArray());
                textBox.Text = string.IsNullOrEmpty(validText) ? "1000" : validText;
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
    }
}
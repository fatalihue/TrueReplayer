using System;
using System.Linq;
using TrueReplayer.Models;

namespace TrueReplayer.Services
{
    public static class UISettingsManager
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
                AlwaysOnTop = window.AlwaysOnTopSwitch.IsOn,
                MinimizeToTray = window.MinimizeToTraySwitch.IsOn,
            };

            window.CaptureWindowState(profile);
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
            window.AlwaysOnTopSwitch.IsOn = profile.AlwaysOnTop;
            window.MinimizeToTraySwitch.IsOn = profile.MinimizeToTray;

            InputHookManager.UpdateHotkeys(profile.RecordingHotkey, profile.ReplayHotkey);

            window.AlwaysOnTopSwitch_Toggled(null, null);
            window.UpdateButtonStates();
        }
    }
}
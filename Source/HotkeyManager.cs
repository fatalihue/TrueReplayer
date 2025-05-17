using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using TrueReplayer.Helpers;
using TrueReplayer.Models;

namespace TrueReplayer.Controllers
{
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
                UserProfile.Current.ReplayHotkey
            );

            OnHotkeyChanged?.Invoke(newKey);

            System.Diagnostics.Debug.WriteLine($"Hotkey atualizada: Gravar={RecordingHotkey}, Reproduzir={ReplayHotkey}");
        }
    }
}

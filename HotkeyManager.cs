using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using TrueReplayer.Helpers;

namespace TrueReplayer.Controllers
{
    public class HotkeyManager
    {
        private readonly TextBox recordingTextBox;
        private readonly TextBox replayTextBox;
        public string RecordingHotkey { get; private set; }
        public string ReplayHotkey { get; private set; }

        public event Action<string>? OnHotkeyChanged;

        public HotkeyManager(TextBox recordingTextBox, TextBox replayTextBox, string initialRecordingHotkey = "F2", string initialReplayHotkey = "F3")
        {
            this.recordingTextBox = recordingTextBox;
            this.replayTextBox = replayTextBox;
            RecordingHotkey = initialRecordingHotkey;
            ReplayHotkey = initialReplayHotkey;

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
                RecordingHotkey = newKey;
            else if (textBox == replayTextBox)
                ReplayHotkey = newKey;
            else return;

            textBox.Text = newKey;
            textBox.SelectionStart = newKey.Length;

            InputHookManager.UpdateHotkeys(RecordingHotkey, ReplayHotkey);
            OnHotkeyChanged?.Invoke(newKey);

            System.Diagnostics.Debug.WriteLine($"Hotkey atualizada: Gravar={RecordingHotkey}, Reproduzir={ReplayHotkey}");
        }
    }
}

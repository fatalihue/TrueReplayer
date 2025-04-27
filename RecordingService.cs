using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using TrueReplayer.Models;

namespace TrueReplayer.Services
{
    public class RecordingService
    {
        private readonly ActionRecorder recorder;
        private readonly Button recordingButton;
        private readonly Func<bool> getMouse;
        private readonly Func<bool> getScroll;
        private readonly Func<bool> getKeyboard;
        private readonly Action<DateTime> setLastActionTime;

        public bool IsRecording { get; private set; } = false;

        public RecordingService(
            ActionRecorder recorder,
            Button recordingButton,
            Func<bool> getMouse,
            Func<bool> getScroll,
            Func<bool> getKeyboard,
            Action<DateTime> setLastActionTime)
        {
            this.recorder = recorder;
            this.recordingButton = recordingButton;
            this.getMouse = getMouse;
            this.getScroll = getScroll;
            this.getKeyboard = getKeyboard;
            this.setLastActionTime = setLastActionTime;
        }

        public void ToggleRecording()
        {
            if (!IsRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            IsRecording = true;
            recordingButton.Content = "Pause";
            recordingButton.Background = new SolidColorBrush(Colors.Orange);

            recorder.RecordMouse = getMouse();
            recorder.RecordScroll = getScroll();
            recorder.RecordKeyboard = getKeyboard();
            recorder.Start();
            setLastActionTime(DateTime.Now);

            System.Diagnostics.Debug.WriteLine($"Gravação iniciada. Filtros: Mouse={recorder.RecordMouse}, Scroll={recorder.RecordScroll}, Keyboard={recorder.RecordKeyboard}");
        }

        private void StopRecording()
        {
            IsRecording = false;
            recordingButton.Content = "Recording";
            recordingButton.ClearValue(Button.BackgroundProperty);
            recorder.Stop();
            System.Diagnostics.Debug.WriteLine("Gravação pausada.");
        }
    }
}

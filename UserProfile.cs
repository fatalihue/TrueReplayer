using System.Collections.ObjectModel;
using TrueReplayer.Models;

namespace TrueReplayer.Models
{
    public class UserProfile
    {
        public static UserProfile Current { get; set; } = Default;
        public ObservableCollection<ActionItem> Actions { get; set; } = new();
        public string RecordingHotkey { get; set; } = "F2";
        public string ReplayHotkey { get; set; } = "F3";
        public bool RecordMouse { get; set; } = true;
        public bool RecordScroll { get; set; } = true;
        public bool RecordKeyboard { get; set; } = true;
        public bool UseCustomDelay { get; set; } = true;
        public int CustomDelay { get; set; } = 100;
        public bool EnableLoop { get; set; } = false;
        public int LoopCount { get; set; } = 0;
        public bool LoopIntervalEnabled { get; set; } = false;
        public int LoopInterval { get; set; } = 1000;
        public bool AlwaysOnTop { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public int WindowX { get; set; } = -1;
        public int WindowY { get; set; } = -1;
        public int WindowWidth { get; set; } = 790;
        public int WindowHeight { get; set; } = 510;
        public string BatchDelay { get; set; } = "Delay (ms)";
        public string? LastProfileDirectory { get; set; }
        public bool IsMaximized { get; set; }

        public static UserProfile Default => new UserProfile
        {
            RecordingHotkey = "F2",
            ReplayHotkey = "F3",
            RecordMouse = true,
            RecordScroll = true,
            RecordKeyboard = true,
            UseCustomDelay = true,
            CustomDelay = 100,
            EnableLoop = false,
            LoopCount = 0,
            LoopIntervalEnabled = false,
            LoopInterval = 1000,
            AlwaysOnTop = false,
            MinimizeToTray = false,
            WindowX = -1,
            WindowY = -1,
            WindowWidth = 790,
            WindowHeight = 510,
            Actions = new ObservableCollection<ActionItem>(),
            BatchDelay = "Delay (ms)"
        };
    }
}

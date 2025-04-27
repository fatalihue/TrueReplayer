using CommunityToolkit.WinUI.UI.Controls;
using Controls = CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using TrueReplayer.Controllers;
using TrueReplayer.Helpers;
using TrueReplayer.Interop;
using TrueReplayer.Models;
using TrueReplayer.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinForms = System.Windows.Forms;
using WinRT.Interop;
using WinUIControls = Microsoft.UI.Xaml.Controls;

namespace TrueReplayer
{
    public sealed partial class MainWindow : Window
    {
        #region Fields and Properties

        public ObservableCollection<ActionItem> Actions { get; } = new();
        public List<string> ActionTypes { get; } = new()
        {
            "LeftClickDown", "LeftClickUp", "RightClickDown", "RightClickUp",
            "MiddleClickDown", "MiddleClickUp", "ScrollDown", "ScrollUp", "KeyDown", "KeyUp"
        };

        private ActionRecorder actionRecorder;
        private ReplayService replayService;
        private RecordingService recordingService;
        private MainController mainController;
        private HotkeyManager hotkeyManager;
        private DelayManager delayManager;
        private LoopControlManager loopControlManager;
        private ProfileController profileController;
        private ActionEditorController actionEditorController;
        private List<string> profileFilePaths = new();
        private string selectedProfileName = "";

        public string recordingHotkey = "F2";
        public string replayHotkey = "F3";

        private IntPtr hwnd;

        #endregion

        #region Constants

        private const int WM_USER = 0x0400;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_SIZE = 0x0005;
        private const int SIZE_MINIMIZED = 1;
        private const int SW_HIDE = 0;
        private const int SW_RESTORE = 9;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        #endregion

        #region Initialization

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "TrueReplayer";

            hwnd = WindowNative.GetWindowHandle(this);

            HwndHookManager.SetupHook(hwnd, WndProc);
            TrayIconService.Initialize(this, hwnd);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "TrueReplayer.ico");
            IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x00000010);
            const int WM_SETICON = 0x80;
            SendMessage(hwnd, WM_SETICON, (IntPtr)1, hIcon);
            SendMessage(hwnd, WM_SETICON, (IntPtr)0, hIcon);

            recordingHotkey = "F2";
            replayHotkey = "F3";

            mainController = null!;

            actionRecorder = new ActionRecorder(
                Actions,
                () => mainController.GetDelay(),
                () => mainController.ScrollToLastAction()
            );

            recordingService = new RecordingService(
                actionRecorder,
                RecordingButton,
                () => RecordMouseSwitch.IsOn,
                () => RecordScrollSwitch.IsOn,
                () => RecordKeyboardSwitch.IsOn,
                time => mainController.SetLastActionTime(time)
            );

            replayService = new ReplayService(
                Actions,
                ReplayButton,
                DispatcherQueue,
                () => mainController.UpdateButtonStates()
            );

            mainController = new MainController(
                Actions,
                actionRecorder,
                recordingService,
                replayService,
                RecordingButton,
                ReplayButton,
                CustomDelayTextBox,
                UseCustomDelaySwitch,
                ActionsDataGrid
            );

            hotkeyManager = new HotkeyManager(
                ToggleRecordingTextBox,
                ToggleReplayTextBox,
                recordingHotkey,
                replayHotkey
            );

            hotkeyManager.OnHotkeyChanged += (key) =>
            {
                recordingHotkey = hotkeyManager.RecordingHotkey;
                replayHotkey = hotkeyManager.ReplayHotkey;
            };

            InitializeUIControls();

            WindowAppearanceService.Configure(this);

            SetupInputHooks();

            profileController = new ProfileController(this);

            this.Closed += (_, _) => TrayIconService.RemoveTrayIcon();

            mainController.UpdateButtonStates();

            LoadProfileList();
        }

        private void InitializeUIControls()
        {
            ToggleRecordingTextBox.Text = recordingHotkey;
            ToggleReplayTextBox.Text = replayHotkey;

            CustomDelayTextBox.Text = "100";
            UseCustomDelaySwitch.IsOn = true;

            LoopCountTextBox.Text = "0";
            LoopIntervalTextBox.Text = "1000";
            EnableLoopSwitch.IsOn = false;
            LoopIntervalSwitch.IsOn = false;

            RecordMouseSwitch.IsOn = true;
            RecordScrollSwitch.IsOn = true;
            RecordKeyboardSwitch.IsOn = true;

            AlwaysOnTopSwitch_Toggled(null, null);

            delayManager = new DelayManager(CustomDelayTextBox, Actions, ActionsDataGrid);
            CustomDelayTextBox.KeyDown += delayManager.HandleKeyDown;
            CustomDelayTextBox.TextChanging += delayManager.HandleTextChanging;

            loopControlManager = new LoopControlManager(
                LoopCountTextBox,
                EnableLoopSwitch,
                LoopIntervalTextBox,
                LoopIntervalSwitch
            );
            LoopCountTextBox.KeyDown += loopControlManager.HandleLoopCountKeyDown;
            LoopCountTextBox.TextChanging += loopControlManager.HandleLoopCountTextChanging;
            LoopIntervalTextBox.KeyDown += loopControlManager.HandleLoopIntervalKeyDown;
            LoopIntervalTextBox.TextChanging += loopControlManager.HandleLoopIntervalTextChanging;

            ToggleRecordingTextBox.PreviewKeyDown += hotkeyManager.HandlePreviewKeyDown;
            ToggleReplayTextBox.PreviewKeyDown += hotkeyManager.HandlePreviewKeyDown;

            actionEditorController = new ActionEditorController(
                Actions,
                ActionsDataGrid,
                () => mainController.UpdateButtonStates()
            );

            ActionsDataGrid.KeyDown += actionEditorController.HandleKeyDown;
            ActionsDataGrid.PreparingCellForEdit += actionEditorController.HandlePreparingCellForEdit;
            ActionsDataGrid.Tapped += actionEditorController.HandleTapped;
        }

        private void SetupInputHooks()
        {
            InputHookManager.Start();
            System.Diagnostics.Debug.WriteLine("Hooks iniciados no construtor.");

            InputHookManager.OnHotkeyPressed += (key) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (key == recordingHotkey)
                    {
                        mainController.ToggleRecording();
                    }
                    else if (key == replayHotkey)
                    {
                        mainController.ToggleReplay(
                            EnableLoopSwitch.IsOn,
                            LoopCountTextBox.Text,
                            LoopIntervalSwitch.IsOn,
                            LoopIntervalTextBox.Text);
                    }
                });
            };

            InputHookManager.OnMouseEvent += (button, x, y, isDown, scrollDelta) =>
            {
                System.Diagnostics.Debug.WriteLine($"MouseEvent: {button} at ({x},{y}), down={isDown}");

                if (!mainController.IsRecording()) return;

                actionRecorder.RecordMouseAction(button, x, y, isDown, scrollDelta);
            };

            InputHookManager.OnKeyEvent += (key, isDown) =>
            {
                System.Diagnostics.Debug.WriteLine($"KeyEvent: {key}, down={isDown}");

                if (!mainController.IsRecording()) return;

                actionRecorder.RecordKeyboardAction(key, isDown);
            };
        }

        #endregion

        #region Window Management

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;
            const int SC_MAXIMIZE = 0xF030;

            if (msg == WM_USER + 1)
            {
                if ((int)lParam == WM_LBUTTONDBLCLK)
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);
                }
                else if ((int)lParam == WM_RBUTTONUP)
                {
                    TrayIconService.ShowContextMenu();
                }
            }
            else if (msg == WM_SYSCOMMAND)
            {
                int command = wParam.ToInt32() & 0xFFF0;

                if (command == SC_MINIMIZE && MinimizeToTraySwitch.IsOn)
                {
                    TrayIconService.Initialize(this, hwnd);
                    TrayIconService.ShowMinimizeBalloon();

                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    AppWindow.GetFromWindowId(windowId).Hide();

                    return IntPtr.Zero;
                }
            }

            return HwndHookManager.CallOriginalWndProc(hwnd, msg, wParam, lParam);
        }

        public void AlwaysOnTopSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            SetWindowPos(hwnd, AlwaysOnTopSwitch.IsOn ? HWND_TOPMOST : HWND_NOTOPMOST,
                         0, 0, 0, 0,
                         SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        #endregion

        #region UI Event Handlers

        private void RecordingButton_Click(object sender, RoutedEventArgs e) => mainController.ToggleRecording();

        private void ReplayButton_Click(object sender, RoutedEventArgs e) => mainController.ToggleReplay(
            EnableLoopSwitch.IsOn,
            LoopCountTextBox.Text,
            LoopIntervalSwitch.IsOn,
            LoopIntervalTextBox.Text
        );

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Actions.Clear();
            mainController.UpdateButtonStates();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            ClipboardService.CopyActions(Actions);

        }

        #endregion

        #region Profile Management

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await profileController.SaveProfileAsync();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            await profileController.LoadProfileAsync();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            profileController.ResetProfile();
            UpdateProfileColors(null);
        }

        public void UpdateButtonStates() => mainController.UpdateButtonStates();

        public void CaptureWindowState(UserProfile profile) => profileController.CaptureWindowState(profile);

        #endregion

        #region Utility Methods

        private void LoadProfileList()
        {
            string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueReplayerProfiles");
            Directory.CreateDirectory(profileDir);

            var files = Directory.GetFiles(profileDir, "*.json");
            profileFilePaths = files.ToList();
            ProfilesListBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        }

        private async void ProfilesListBox_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is string selectedProfile)
            {
                int index = ProfilesListBox.Items.IndexOf(selectedProfile);
                if (index >= 0 && index < profileFilePaths.Count)
                {
                    string path = profileFilePaths[index];

                    var profile = await SettingsManager.LoadProfileAsync(path);

                    if (profile != null)
                    {
                        UserProfile.Current = profile;
                        UISettingsManager.ApplyToUI(this, profile);
                        WindowAppearanceService.ApplyWindowState(this, profile);

                        selectedProfileName = selectedProfile;
                        UpdateProfileColors(selectedProfileName);
                    }
                }
            }
        }

        private void UpdateProfileColors(string selectedProfileName)
        {
            if (ProfilesListBox?.Items == null)
                return;

            foreach (var item in ProfilesListBox.Items)
            {
                var container = ProfilesListBox.ContainerFromItem(item) as Microsoft.UI.Xaml.Controls.ListViewItem;
                if (container == null)
                    continue;

                var contentPresenter = FindVisualChild<ContentPresenter>(container);
                if (contentPresenter == null)
                    continue;

                var stackPanel = FindVisualChild<StackPanel>(contentPresenter);
                if (stackPanel == null)
                    continue;

                var textBlock = stackPanel.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock == null)
                    continue;

                bool isSelected = selectedProfileName != null && item?.ToString() == selectedProfileName;
                textBlock.Foreground = new SolidColorBrush(isSelected ? Colors.LimeGreen : Colors.White);
            }
        }


        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T foundChild)
                    return foundChild;

                var foundDescendant = FindVisualChild<T>(child);
                if (foundDescendant != null)
                    return foundDescendant;
            }
            return null;
        }

        public void RefreshProfileList()
        {
            LoadProfileList();
        }

        private void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is WinUIControls.TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void KeyEditTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is not Microsoft.UI.Xaml.Controls.TextBox textBox) return;
            if (ActionsDataGrid.SelectedItem is not ActionItem item) return;

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

            var selectedIndex = ActionsDataGrid.SelectedIndex;
            ActionsDataGrid.SelectedItem = null;
            ActionsDataGrid.SelectedIndex = selectedIndex;
        }

        #endregion

        #region P/Invoke and Structs

        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        #endregion
    }
}
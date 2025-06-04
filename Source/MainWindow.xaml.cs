using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrueReplayer.Controllers;
using TrueReplayer.Helpers;
using TrueReplayer.Interop;
using TrueReplayer.Managers;
using TrueReplayer.Models;
using TrueReplayer.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Controls = CommunityToolkit.WinUI.UI.Controls;
using WinForms = System.Windows.Forms;
using WinUIControls = Microsoft.UI.Xaml.Controls;

namespace TrueReplayer
{
    public sealed partial class MainWindow : Window
    {
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
        private WindowEventManager windowEventManager;
        private UIInteractionHandler uiInteractionHandler;

        private IntPtr hwnd;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "TrueReplayer";

            hwnd = WindowNative.GetWindowHandle(this);

            windowEventManager = new WindowEventManager(this);
            HwndHookManager.SetupHook(hwnd, windowEventManager.WndProc);

            TrayIconService.Initialize(this, hwnd);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "TrueReplayer.ico");
            IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x00000010);
            const int WM_SETICON = 0x80;
            SendMessage(hwnd, WM_SETICON, (IntPtr)1, hIcon);
            SendMessage(hwnd, WM_SETICON, (IntPtr)0, hIcon);

            mainController = null!;

            actionRecorder = new ActionRecorder(
                Actions,
                () => mainController.GetDelay(),
                () => UseCustomDelaySwitch.IsOn,
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
                () => mainController.UpdateButtonStates(),
                ActionsDataGrid
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

            uiInteractionHandler = new UIInteractionHandler(
                Actions,
                mainController,
                ActionsDataGrid
            );

            InitializeUIControls();

            CustomDelayTextBox.GotFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = true;
            CustomDelayTextBox.LostFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = false;

            LoopCountTextBox.GotFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = true;
            LoopCountTextBox.LostFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = false;

            LoopIntervalTextBox.GotFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = true;
            LoopIntervalTextBox.LostFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = false;

            ToggleRecordingTextBox.GotFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = true;
            ToggleRecordingTextBox.LostFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = false;

            ToggleReplayTextBox.GotFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = true;
            ToggleReplayTextBox.LostFocus += (s, e) => InputHookManager.IgnoreProfileHotkeys = false;

            WindowAppearanceService.Configure(this);

            SetupInputHooks();

            mainController.UpdateButtonStates();

            profileController = new ProfileController(this);
            ProfilesListBox.ItemsSource = profileController.ProfileEntries;

            DispatcherQueue.TryEnqueue(async () =>
            {
                profileController.RefreshProfileList(true);

                var hotkeys = await profileController.GetProfileHotkeys();
                InputHookManager.RegisterProfileHotkeys(hotkeys);
            });

            ProfileKeySwitch.Toggled += (sender, args) =>
            {
                UserProfile.Current.ProfileKeyEnabled = ProfileKeySwitch.IsOn;
            };
        }

        private void InitializeUIControls()
        {
            ToggleRecordingTextBox.Text = UserProfile.Current.RecordingHotkey;
            ToggleReplayTextBox.Text = UserProfile.Current.ReplayHotkey;

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

            hotkeyManager = new HotkeyManager(ToggleRecordingTextBox, ToggleReplayTextBox);

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

            InputHookManager.OnHotkeyPressed += (key) =>
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (key.StartsWith("PROFILE::") && !ProfileKeySwitch.IsOn)
                    {
                        return;
                    }

                    if (key.StartsWith("PROFILE::") && ProfileKeySwitch.IsOn && mainController.IsRecording())
                    {
                        return;
                    }

                    if (key == UserProfile.Current.RecordingHotkey)
                    {
                        if (mainController.ShouldSuppressDuplicateRecordingHotkey())
                            return;

                        int index = ActionsDataGrid.SelectedIndex;

                        foreach (var action in Actions)
                        {
                            action.IsInsertionPoint = false;
                            action.IsVisuallyDeselected = false;
                        }

                        if (index >= 0 && index < Actions.Count - 1)
                        {
                            Actions[index].IsInsertionPoint = true;
                            mainController.EnableInsertMode(index);
                        }
                        else
                        {
                            mainController.EnableInsertMode(null);
                        }

                        mainController.SetLastHotkeyPressed(key);
                        uiInteractionHandler.HandleRecordingButtonClick();
                    }
                    else if (key == UserProfile.Current.ReplayHotkey)
                    {
                        if (mainController.ShouldSuppressDuplicateReplayHotkey())
                            return;

                        mainController.SetLastHotkeyPressed(key);
                        mainController.ToggleReplay(
                            EnableLoopSwitch.IsOn,
                            LoopCountTextBox.Text,
                            LoopIntervalSwitch.IsOn,
                            LoopIntervalTextBox.Text);
                    }
                    else if (key.StartsWith("PROFILE::"))
                    {
                        string profileName = key.Substring("PROFILE::".Length);
                        var profile = await profileController.LoadProfileByNameAsync(profileName);

                        if (profile != null)
                        {
                            mainController.SetLastHotkeyPressed(key);
                            UserProfile.Current = profile;
                            UISettingsManager.ApplyToUI(this, profile);

                            mainController.ToggleReplay(
                                profile.EnableLoop,
                                profile.LoopCount.ToString(),
                                profile.LoopIntervalEnabled,
                                profile.LoopInterval.ToString());

                            profileController.UpdateProfileColors(profileName);
                        }
                    }
                });
            };

            InputHookManager.OnMouseEvent += (button, x, y, isDown, scrollDelta) =>
            {
                if (!mainController.IsRecording()) return;

                actionRecorder.RecordMouseAction(button, x, y, isDown, scrollDelta);
            };

            InputHookManager.OnKeyEvent += async (key, isDown) =>
            {
                if (isDown && key == "Escape")
                {
                    mainController.CancelInsertMode();
                    foreach (var action in Actions)
                    {
                        action.IsInsertionPoint = false;
                        action.IsVisuallyDeselected = true;
                    }
                    return;
                }

                if (!mainController.IsRecording()) return;

                actionRecorder.RecordKeyboardAction(key, isDown);
            };
        }

        public void AlwaysOnTopSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            windowEventManager.UpdateAlwaysOnTop(AlwaysOnTopSwitch.IsOn);
        }

        private void RecordingButton_Click(object sender, RoutedEventArgs e)
        {
            uiInteractionHandler.HandleRecordingButtonClick();
        }

        private void ReplayButton_Click(object sender, RoutedEventArgs e)
        {
            uiInteractionHandler.HandleReplayButtonClick(
                EnableLoopSwitch.IsOn,
                LoopCountTextBox.Text,
                LoopIntervalSwitch.IsOn,
                LoopIntervalTextBox.Text
            );
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            uiInteractionHandler.HandleClearButtonClick();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            uiInteractionHandler.HandleCopyButtonClick();
        }

        private void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            uiInteractionHandler.HandleTextBoxSelectAll(sender);
        }

        private void KeyEditTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            uiInteractionHandler.HandleKeyEditTextBoxPreviewKeyDown(sender, e);
        }

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
            UISettingsManager.ApplyToUI(this, UserProfile.Current);
            profileController.UpdateProfileColors(null);
        }

        private async void ProfilesListBox_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProfileEntry selectedProfile)
            {
                await profileController.HandleProfileItemClick(selectedProfile.Name);
            }
        }

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            await profileController.DeleteSelectedProfileAsync();
        }

        private void OpenProfileFolder_Click(object sender, RoutedEventArgs e)
        {
            profileController.OpenSelectedProfileFolder();
        }

        private void ProfilesListBox_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is ProfileEntry profile)
            {
                profileController.HandleProfileRightTapped(profile.Name);
            }
        }

        private async void RenameProfile_Click(object sender, RoutedEventArgs e)
        {
            await profileController.RenameSelectedProfileAsync();
        }

        private async void CreateNewProfile_Click(object sender, RoutedEventArgs e)
        {
            var inputTextBox = new Microsoft.UI.Xaml.Controls.TextBox
            {
                PlaceholderText = "Profile name...",
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Colors.DimGray),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Gray)
            };

            var dialog = new ContentDialog
            {
                Title = "Create New Profile",
                Content = inputTextBox,
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 43, 43)),
                Foreground = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8)
            };

            inputTextBox.Loaded += (_, _) => inputTextBox.Focus(FocusState.Programmatic);

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string name = inputTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;

                string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueReplayerProfiles");
                Directory.CreateDirectory(profileDir);

                if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    name += ".json";

                string fullPath = Path.Combine(profileDir, name);

                if (File.Exists(fullPath))
                {
                    WinForms.MessageBox.Show("A profile with this name already exists.", "Warning", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                    return;
                }

                var profile = UserProfile.Default;
                WindowAppearanceService.CaptureWindowState(this, profile);
                await SettingsManager.SaveProfileAsync(fullPath, profile);

                profileController.RefreshProfileList(true);

                var loaded = await SettingsManager.LoadProfileAsync(fullPath);
                if (loaded != null)
                {
                    UserProfile.Current = loaded;
                    UISettingsManager.ApplyToUI(this, loaded);
                    WindowAppearanceService.ApplyWindowState(this, loaded);
                }
            }
        }

        private async void AssignProfileHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is not ProfileEntry selectedProfileEntry)
                return;

            string selectedProfile = selectedProfileEntry.Name;

            if (string.IsNullOrEmpty(selectedProfile))
            {
                System.Diagnostics.Debug.WriteLine("selectedProfile está vazio ou nulo no AssignProfileHotkey_Click.");
                return;
            }

            var hotkeyTextBox = new Microsoft.UI.Xaml.Controls.TextBox
            {
                PlaceholderText = "Press a key...",
                IsReadOnly = true,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Colors.DimGray),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Gray)
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = $"Assign Profile Key to \"{selectedProfile}\"",
                Content = hotkeyTextBox,
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark,
                CloseButtonText = "Cancel",
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 43, 43)),
                Foreground = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8)
            };

            hotkeyTextBox.PreviewKeyDown += async (s, args) =>
            {
                args.Handled = true;

                try
                {
                    bool ctrl = (NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
                    bool alt = (NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU (Alt)
                    bool shift = (NativeMethods.GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT

                    int vkCode = (int)args.Key;
                    var parts = new List<string>(); // Declaração única de 'parts'

                    if (vkCode == 0x10 || vkCode == 0x11 || vkCode == 0x12 || // Shift, Ctrl, Alt
                        vkCode == 0xA0 || vkCode == 0xA1 || // LeftShift, RightShift
                        vkCode == 0xA2 || vkCode == 0xA3 || // LeftCtrl, RightCtrl
                        vkCode == 0xA4 || vkCode == 0xA5)   // LeftAlt, RightAlt
                    {
                        if (ctrl) parts.Add("Ctrl");
                        if (alt) parts.Add("Alt");
                        if (shift) parts.Add("Shift");
                        hotkeyTextBox.Text = string.Join("+", parts);
                        return;
                    }

                    string? mainKey = KeyUtils.NormalizeKeyName(vkCode) ?? args.Key.ToString();

                    parts.Clear();
                    if (ctrl) parts.Add("Ctrl");
                    if (alt) parts.Add("Alt");
                    if (shift) parts.Add("Shift");
                    if (!string.IsNullOrEmpty(mainKey) && !parts.Contains(mainKey, StringComparer.OrdinalIgnoreCase))
                        parts.Add(mainKey);

                    string newHotkey = string.Join("+", parts);

                    if (string.IsNullOrEmpty(newHotkey) || parts.Count == 0 || (parts.Count == 1 && (parts[0] == "Ctrl" || parts[0] == "Alt" || parts[0] == "Shift")))
                    {
                        System.Diagnostics.Debug.WriteLine("Hotkey inválida ou apenas modificador.");
                        return;
                    }

                    if (string.IsNullOrEmpty(selectedProfile) || profileController == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"selectedProfile ({selectedProfile}) ou profileController é nulo.");
                        return;
                    }

                    hotkeyTextBox.Text = newHotkey;

                    var profile = await profileController.LoadProfileByNameAsync(selectedProfile);
                    if (profile == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Não foi possível carregar o perfil: {selectedProfile}");
                        return;
                    }

                    profile.CustomHotkey = newHotkey;
                    await profileController.SaveProfileByNameAsync(selectedProfile, profile);

                    profileController.RefreshProfileList(true);
                    var map = await profileController.GetProfileHotkeys();
                    InputHookManager.RegisterProfileHotkeys(map);

                    dialog.Hide();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro no PreviewKeyDown: {ex}");
                }
            };

            hotkeyTextBox.Loaded += (_, _) => hotkeyTextBox.Focus(FocusState.Programmatic);

            await dialog.ShowAsync();
        }

        private async void RemoveProfileHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesListBox.SelectedItem is not ProfileEntry selectedProfileEntry)
                return;

            string selectedProfile = selectedProfileEntry.Name;

            var profile = await profileController.LoadProfileByNameAsync(selectedProfile);
            if (profile != null)
            {
                profile.CustomHotkey = null;
                await profileController.SaveProfileByNameAsync(selectedProfile, profile);

                profileController.RefreshProfileList(true);

                var map = await profileController.GetProfileHotkeys();
                InputHookManager.RegisterProfileHotkeys(map);
            }
        }

        public void UpdateButtonStates() => mainController.UpdateButtonStates();

        public void CaptureWindowState(UserProfile profile) => profileController.CaptureWindowState(profile);

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

        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}
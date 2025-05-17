using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using CommunityToolkit.WinUI.UI.Controls;
using Controls = CommunityToolkit.WinUI.UI.Controls;
using WinForms = System.Windows.Forms;
using WinUIControls = Microsoft.UI.Xaml.Controls;
using TrueReplayer.Controllers;
using TrueReplayer.Helpers;
using TrueReplayer.Interop;
using TrueReplayer.Managers;
using TrueReplayer.Models;
using TrueReplayer.Services;

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
        private WindowEventManager windowEventManager;
        private UIInteractionHandler uiInteractionHandler;

        private IntPtr hwnd;

        #endregion

        #region Initialization

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

            WindowAppearanceService.Configure(this);

            SetupInputHooks();

            mainController.UpdateButtonStates();

            profileController = new ProfileController(this);

            ProfilesListBox.ItemsSource = profileController.ProfileNames;
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
            System.Diagnostics.Debug.WriteLine("Hooks iniciados no construtor.");

            InputHookManager.OnHotkeyPressed += (key) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (key == UserProfile.Current.RecordingHotkey)
                    {
                        int index = ActionsDataGrid.SelectedIndex;

                        foreach (var action in Actions)
                        {
                            action.IsInsertionPoint = false;
                            action.IsVisuallyDeselected = false;
                        }

                        if (index >= 0 && index < Actions.Count)
                        {
                            Actions[index].IsInsertionPoint = true;
                            mainController.EnableInsertMode(index);
                        }
                        else
                        {
                            mainController.EnableInsertMode(null);
                        }

                        uiInteractionHandler.HandleRecordingButtonClick();
                    }
                    else if (key == UserProfile.Current.ReplayHotkey)
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

            InputHookManager.OnKeyEvent += async (key, isDown) =>
            {
                System.Diagnostics.Debug.WriteLine($"KeyEvent: {key}, down={isDown}");

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

        #endregion

        #region Window Management

        public void AlwaysOnTopSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            windowEventManager.UpdateAlwaysOnTop(AlwaysOnTopSwitch.IsOn);
        }

        #endregion

        #region UI Event Handlers

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
            profileController.UpdateProfileColors(null);
        }

        public void UpdateButtonStates() => mainController.UpdateButtonStates();

        public void CaptureWindowState(UserProfile profile) => profileController.CaptureWindowState(profile);

        private async void ProfilesListBox_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is string selectedProfile)
            {
                await profileController.HandleProfileItemClick(selectedProfile);
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
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is string profile)
            {
                profileController.HandleProfileRightTapped(profile);
            }
        }

        private async void RenameProfile_Click(object sender, RoutedEventArgs e)
        {
            await profileController.RenameSelectedProfileAsync();
        }

        #endregion

        #region Utility Methods

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

        #endregion

        #region P/Invoke and Structs

        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
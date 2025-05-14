using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TrueReplayer.Models;
using TrueReplayer.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinForms = System.Windows.Forms;
using Microsoft.UI;
using System.Collections.ObjectModel;

namespace TrueReplayer.Controllers
{
    public class ProfileController
    {
        private readonly MainWindow window;
        private FileSystemWatcher? profileWatcher;
        private CancellationTokenSource? debounceCts;
        private List<string> profileFilePaths = new();
        private bool suppressWatcherRefresh = false;
        private string? selectedProfileName;

        public ObservableCollection<string> ProfileNames { get; } = new();

        public ProfileController(MainWindow window)
        {
            this.window = window;
            LoadProfileList();
            SetupProfileWatcher();
        }

        #region Profile CRUD

        public async Task SaveProfileAsync()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string profileDir = Path.Combine(documentsPath, "TrueReplayerProfiles");

            Directory.CreateDirectory(profileDir);

            var dialog = new WinForms.SaveFileDialog
            {
                Filter = "JSON file (*.json)|*.json",
                FileName = "profile.json",
                InitialDirectory = profileDir
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                var profile = UISettingsManager.CreateFromUI(window);
                profile.LastProfileDirectory = Path.GetDirectoryName(dialog.FileName)!;
                CaptureWindowState(profile);

                try
                {
                    if (File.Exists(dialog.FileName))
                    {
                        File.Delete(dialog.FileName);
                    }

                    await SettingsManager.SaveProfileAsync(dialog.FileName, profile);
                    RefreshProfileList(true);
                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show($"Error saving profile:\n{ex.Message}", "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                }
            }
        }

        public async Task LoadProfileAsync()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string profileDir = Path.Combine(documentsPath, "TrueReplayerProfiles");

            var dialog = new WinForms.OpenFileDialog
            {
                Filter = "JSON file (*.json)|*.json",
                InitialDirectory = Directory.Exists(profileDir) ? profileDir : documentsPath
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                string path = dialog.FileName;
                var profile = await SettingsManager.LoadProfileAsync(path);

                if (profile != null)
                {
                    UserProfile.Current = profile;
                    UISettingsManager.ApplyToUI(window, profile);
                    WindowAppearanceService.ApplyWindowState(window, profile);
                    UserProfile.Current.LastProfileDirectory = Path.GetDirectoryName(path)!;
                }
            }
        }

        public void ResetProfile()
        {
            UserProfile.Current = UserProfile.Default;
            UISettingsManager.ApplyToUI(window, UserProfile.Default);
            WindowAppearanceService.Configure(window);
            System.Diagnostics.Debug.WriteLine("Settings reset to default.");
        }

        public void CaptureWindowState(UserProfile profile)
        {
            WindowAppearanceService.CaptureWindowState(window, profile);
        }

        #endregion

        #region Profile List Management

        private void LoadProfileList()
        {
            string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueReplayerProfiles");
            Directory.CreateDirectory(profileDir);

            var files = Directory.GetFiles(profileDir, "*.json");

            ProfileNames.Clear();
            profileFilePaths.Clear();

            foreach (var file in files)
            {
                profileFilePaths.Add(file);
                ProfileNames.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        public void RefreshProfileList(bool suppressWatcher = false)
        {
            if (suppressWatcher)
                suppressWatcherRefresh = true;

            var oldItemsSource = window.ProfilesListBox.ItemsSource;

            window.ProfilesListBox.ItemsSource = null;

            LoadProfileList();

            selectedProfileName = null;
            UpdateProfileColors(null);

            window.ProfilesListBox.ItemsSource = ProfileNames;
        }

        private void SetupProfileWatcher()
        {
            string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TrueReplayerProfiles");

            if (!Directory.Exists(profileDir))
                Directory.CreateDirectory(profileDir);

            profileWatcher = new FileSystemWatcher(profileDir, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            profileWatcher.Created += OnProfileFolderChanged;
            profileWatcher.Deleted += OnProfileFolderChanged;
            profileWatcher.Renamed += OnProfileFolderChanged;
            profileWatcher.EnableRaisingEvents = true;
        }

        private async void OnProfileFolderChanged(object sender, FileSystemEventArgs e)
        {
            debounceCts?.Cancel();
            debounceCts?.Dispose();

            debounceCts = new CancellationTokenSource();
            var token = debounceCts.Token;

            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    window.DispatcherQueue.TryEnqueue(() =>
                    {
                        if (suppressWatcherRefresh)
                        {
                            suppressWatcherRefresh = false;
                            return;
                        }
                        RefreshProfileList();
                    });
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        #endregion

        #region Profile Actions (UI Events)

        public async Task HandleProfileItemClick(string selectedProfile)
        {
            int index = ProfileNames.IndexOf(selectedProfile);
            if (index >= 0 && index < profileFilePaths.Count)
            {
                string path = profileFilePaths[index];
                var profile = await SettingsManager.LoadProfileAsync(path);

                if (profile != null)
                {
                    UserProfile.Current = profile;
                    UISettingsManager.ApplyToUI(window, profile);
                    WindowAppearanceService.ApplyWindowState(window, profile);

                    selectedProfileName = selectedProfile;
                    UpdateProfileColors(selectedProfileName);
                }
            }
        }

        public void HandleProfileRightTapped(string profile)
        {
            if (ProfileNames.Contains(profile))
            {
                window.ProfilesListBox.SelectedItem = profile;
            }
        }

        public async Task DeleteSelectedProfileAsync()
        {
            if (window.ProfilesListBox.SelectedItem is string selectedProfile)
            {
                int index = ProfileNames.IndexOf(selectedProfile);
                if (index >= 0 && index < profileFilePaths.Count)
                {
                    string filePath = profileFilePaths[index];

                    var confirmResult = WinForms.MessageBox.Show($"Delete profile '{selectedProfile}'?", "Confirm Delete", WinForms.MessageBoxButtons.YesNo, WinForms.MessageBoxIcon.Warning);
                    if (confirmResult == WinForms.DialogResult.Yes)
                    {
                        try
                        {
                            if (File.Exists(filePath))
                                File.Delete(filePath);

                            RefreshProfileList(true);
                        }
                        catch (Exception ex)
                        {
                            WinForms.MessageBox.Show($"Error deleting profile:\n{ex.Message}", "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        public void OpenSelectedProfileFolder()
        {
            if (window.ProfilesListBox.SelectedItem is string selectedProfile)
            {
                int index = ProfileNames.IndexOf(selectedProfile);
                if (index >= 0 && index < profileFilePaths.Count)
                {
                    string filePath = profileFilePaths[index];
                    string? folderPath = Path.GetDirectoryName(filePath);

                    if (folderPath != null && Directory.Exists(folderPath))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = folderPath,
                                UseShellExecute = true,
                                Verb = "open"
                            });
                        }
                        catch (Exception ex)
                        {
                            WinForms.MessageBox.Show($"Error opening folder:\n{ex.Message}", "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        public async Task RenameSelectedProfileAsync()
        {
            if (window.ProfilesListBox.SelectedItem is string selectedProfile)
            {
                int index = ProfileNames.IndexOf(selectedProfile);
                if (index >= 0 && index < profileFilePaths.Count)
                {
                    string oldFilePath = profileFilePaths[index];
                    string? folderPath = Path.GetDirectoryName(oldFilePath);

                    if (folderPath != null)
                    {
                        string? newName = await ShowRenameDialogAsync(selectedProfile);

                        if (!string.IsNullOrWhiteSpace(newName))
                        {
                            if (!newName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                newName += ".json";

                            string newFilePath = Path.Combine(folderPath, newName);

                            try
                            {
                                if (File.Exists(newFilePath))
                                {
                                    WinForms.MessageBox.Show($"A profile named '{newName}' already exists.", "Rename Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                                }
                                else
                                {
                                    File.Move(oldFilePath, newFilePath);
                                    RefreshProfileList(true);
                                }
                            }
                            catch (Exception ex)
                            {
                                WinForms.MessageBox.Show($"Error renaming profile:\n{ex.Message}", "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
        }

        private async Task<string?> ShowRenameDialogAsync(string currentName)
        {
            var inputTextBox = new TextBox
            {
                PlaceholderText = "New profile name...",
                Text = currentName,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Colors.DimGray),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Gray)
            };

            var dialog = new ContentDialog
            {
                Title = "Rename Profile",
                XamlRoot = window.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 43, 43)),
                Foreground = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                Content = inputTextBox
            };

            dialog.Loaded += (_, _) =>
            {
                inputTextBox.Focus(FocusState.Programmatic);
                inputTextBox.SelectAll();
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string newName = inputTextBox.Text.Trim();
                return string.IsNullOrEmpty(newName) ? null : newName;
            }

            return null;
        }

        public void UpdateProfileColors(string? selectedProfileName)
        {
            if (window.ProfilesListBox?.Items == null)
                return;

            foreach (var item in window.ProfilesListBox.Items)
            {
                var container = window.ProfilesListBox.ContainerFromItem(item) as ListViewItem;
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

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

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
    }
}
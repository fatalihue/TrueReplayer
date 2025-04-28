using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using TrueReplayer.Models;
using TrueReplayer.Services;
using Windows.Storage.Pickers;
using WinForms = System.Windows.Forms;

namespace TrueReplayer.Controllers
{
    public class ProfileController
    {
        private readonly MainWindow window;

        public ProfileController(MainWindow window)
        {
            this.window = window;
        }

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
                        var result = WinForms.MessageBox.Show(
                            $"A file named '{Path.GetFileName(dialog.FileName)}' already exists.\nDo you want to overwrite it?",
                            "Confirm Overwrite",
                            WinForms.MessageBoxButtons.YesNo,
                            WinForms.MessageBoxIcon.Question
                        );

                        if (result != WinForms.DialogResult.Yes)
                            return;
                    }

                    await SettingsManager.SaveProfileAsync(dialog.FileName, profile);
                    window.RefreshProfileList(); // Atualiza lista após salvar
                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show($"Erro ao salvar perfil:\n{ex.Message}", "Erro", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
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
            System.Diagnostics.Debug.WriteLine("Configurações resetadas para os valores padrão.");
        }

        public void CaptureWindowState(UserProfile profile)
        {
            WindowAppearanceService.CaptureWindowState(window, profile);
        }
    }
}
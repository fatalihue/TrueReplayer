using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using TrueReplayer.Models;

namespace TrueReplayer.Services
{
    public static class SettingsManager
    {
        private static string GetDefaultProfilePath()
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string appFolder = Path.Combine(documentsPath, "TrueReplayer");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "profile.json");
        }

        public static async Task SaveProfileAsync(string? filePath, UserProfile profile)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = GetDefaultProfilePath();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            var json = JsonSerializer.Serialize(profile, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task<UserProfile?> LoadProfileAsync(string? filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = GetDefaultProfilePath();

            if (!File.Exists(filePath)) return null;

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<UserProfile>(json, options);
        }
    }
}

using System;
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
            string appFolder = AppContext.BaseDirectory;  // Obtém a pasta raiz do aplicativo
            string profileDir = Path.Combine(appFolder, "Profiles");  // Alterado para "Profiles"

            Directory.CreateDirectory(profileDir);  // Cria a pasta de perfis, se não existir
            return Path.Combine(profileDir, "profile.json");  // Caminho completo para o perfil padrão
        }

        public static async Task SaveProfileAsync(string? filePath, UserProfile profile)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = GetDefaultProfilePath();  // Usa o caminho padrão, se não for especificado

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            var json = JsonSerializer.Serialize(profile, options);
            await File.WriteAllTextAsync(filePath, json);  // Salva o perfil no arquivo
        }

        public static async Task<UserProfile?> LoadProfileAsync(string? filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = GetDefaultProfilePath();  // Usa o caminho padrão, se não for especificado

            if (!File.Exists(filePath)) return null;  // Verifica se o arquivo existe

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            var json = await File.ReadAllTextAsync(filePath);  // Lê o arquivo de perfil
            return JsonSerializer.Deserialize<UserProfile>(json, options);  // Deserializa o perfil
        }
    }
}

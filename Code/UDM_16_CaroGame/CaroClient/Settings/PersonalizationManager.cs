using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaroClient.Settings
{
    public class PersonalizationManager
    {
        private static PersonalizationManager? _instance;
        public static PersonalizationManager Instance => _instance ??= new PersonalizationManager();

        public PlayerPersonalizationSettings Settings { get; private set; } = new PlayerPersonalizationSettings();

        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        private PersonalizationManager()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaroClient");
            Directory.CreateDirectory(appDataPath);
            _settingsFilePath = Path.Combine(appDataPath, "personalization.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new ColorJsonConverter() }
            };

            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    Settings = JsonSerializer.Deserialize<PlayerPersonalizationSettings>(json, _jsonOptions) ?? new PlayerPersonalizationSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PersonalizationManager] Failed to load settings, using defaults. Error: {ex.Message}");
                    Settings = new PlayerPersonalizationSettings();
                }
            }
            else
            {
                Settings = new PlayerPersonalizationSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(Settings, _jsonOptions);
                string tempFilePath = _settingsFilePath + ".tmp";
                
                // Atomic save pattern
                File.WriteAllText(tempFilePath, json);
                File.Move(tempFilePath, _settingsFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PersonalizationManager] Failed to save settings. Error: {ex.Message}");
            }
        }
        
        public void CommitTemporarySettings(PlayerPersonalizationSettings tempSettings)
        {
            Settings = tempSettings;
            SaveSettings();
        }
    }
}

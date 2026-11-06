
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FokusApp.Services
{
    public static class PrayerAlarmSettingsStore
    {
        private static readonly string FolderPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "FokusApp");

        private static readonly string FilePath =
            Path.Combine(FolderPath, "prayer-alarm-settings.json");

        public static Dictionary<string, bool> Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return CreateDefaultSettings();

                string json = File.ReadAllText(FilePath);

                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                       ?? CreateDefaultSettings();
            }
            catch
            {
                return CreateDefaultSettings();
            }
        }

        public static void Save(Dictionary<string, bool> settings)
        {
            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(FilePath, json);
        }

        private static Dictionary<string, bool> CreateDefaultSettings()
        {
            return new Dictionary<string, bool>
            {
                ["Subuh"] = false,
                ["Dzuhur"] = false,
                ["Ashar"] = false,
                ["Maghrib"] = false,
                ["Isya"] = false
            };
        }
    }
}

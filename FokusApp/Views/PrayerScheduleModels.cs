
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FokusApp.Views
{
    // Penyimpanan jadwal salat
    public static class PrayerScheduleStore
    {
        private static readonly string FolderPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "FokusApp");

        private static readonly string FilePath =
            Path.Combine(FolderPath, "prayer-schedule.json");

        public static void Save(PrayerScheduleData data)
        {
            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(FilePath, json);
        }

        public static PrayerScheduleData? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;

                string json = File.ReadAllText(FilePath);

                return JsonSerializer.Deserialize<PrayerScheduleData>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    // Model jadwal salat
    public class PrayerScheduleData
    {
        public string ProvinceCode { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;

        public string RegencyCode { get; set; } = string.Empty;
        public string RegencyName { get; set; } = string.Empty;

        public string DistrictCode { get; set; } = string.Empty;
        public string DistrictName { get; set; } = string.Empty;

        public string ScheduleDate { get; set; } = string.Empty;

        public string Fajr { get; set; } = "--:--";
        public string Dhuhr { get; set; } = "--:--";
        public string Asr { get; set; } = "--:--";
        public string Maghrib { get; set; } = "--:--";
        public string Isha { get; set; } = "--:--";
    }

    public class ApiResponse<T>
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new List<T>();
    }

    public class WilayahItem
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class AlAdhanResponse
    {
        [JsonPropertyName("data")]
        public AlAdhanData? Data { get; set; }
    }

    public class AlAdhanData
    {
        [JsonPropertyName("timings")]
        public PrayerTimings? Timings { get; set; }
    }

    public class PrayerTimings
    {
        [JsonPropertyName("Fajr")]
        public string Fajr { get; set; } = string.Empty;

        [JsonPropertyName("Dhuhr")]
        public string Dhuhr { get; set; } = string.Empty;

        [JsonPropertyName("Asr")]
        public string Asr { get; set; } = string.Empty;

        [JsonPropertyName("Maghrib")]
        public string Maghrib { get; set; } = string.Empty;

        [JsonPropertyName("Isha")]
        public string Isha { get; set; } = string.Empty;
    }
}

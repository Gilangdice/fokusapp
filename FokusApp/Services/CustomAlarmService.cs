using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FokusApp.Services
{
    public class CustomAlarmItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Time { get; set; } = "07:00"; // Format HH:mm
        public string Label { get; set; } = "Alarm";
        public bool IsEnabled { get; set; } = true;
        public string LastTriggeredDate { get; set; } = "";
        public string SoundPath { get; set; } = ""; // Kosong = suara default Windows
    }

    public class CustomAlarmService
    {
        public static CustomAlarmService Instance { get; } = new();

        private readonly string _filePath;
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly MediaPlayer _player = new();

        public List<CustomAlarmItem> Alarms { get; private set; } = new();
        public event Action? AlarmsChanged;

        private CustomAlarmService()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FokusApp");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "custom_alarms.json");

            Load();

            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            string currentTime = now.ToString("HH:mm");
            string today = now.ToString("yyyy-MM-dd");

            bool updated = false;

            foreach (var alarm in Alarms.Where(a => a.IsEnabled))
            {
                if (alarm.Time == currentTime && alarm.LastTriggeredDate != today)
                {
                    alarm.LastTriggeredDate = today;
                    updated = true;

                    PlaySound(alarm.SoundPath);

                    MessageBox.Show(
                        $"Waktunya: {alarm.Label}\nPukul {alarm.Time}",
                        "Alarm FokusApp",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Stop audio begitu user klik OK
                    _player.Stop();
                }
            }

            if (updated) Save();
        }

        private void PlaySound(string soundPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(soundPath) && File.Exists(soundPath))
                {
                    _player.Stop();
                    _player.Open(new Uri(soundPath));
                    _player.Play();
                    return;
                }
            }
            catch { }

            // Fallback kalau file belum dipilih atau terhapus
            SystemSounds.Exclamation.Play();
        }

        public void AddAlarm(string time, string label)
        {
            Alarms.Add(new CustomAlarmItem
            {
                Time = time,
                Label = string.IsNullOrWhiteSpace(label) ? "Alarm" : label.Trim(),
                IsEnabled = true
            });

            Alarms = Alarms.OrderBy(a => a.Time).ToList();
            Save();
            AlarmsChanged?.Invoke();
        }

        public void UpdateAlarmSound(string id, string soundPath)
        {
            var item = Alarms.FirstOrDefault(a => a.Id == id);
            if (item == null) return;

            item.SoundPath = soundPath;
            Save();
            AlarmsChanged?.Invoke();
        }

        public void ToggleAlarm(string id, bool isEnabled)
        {
            var item = Alarms.FirstOrDefault(a => a.Id == id);
            if (item == null) return;

            item.IsEnabled = isEnabled;
            if (isEnabled) item.LastTriggeredDate = "";
            Save();
        }

        public void DeleteAlarm(string id)
        {
            Alarms.RemoveAll(a => a.Id == id);
            Save();
            AlarmsChanged?.Invoke();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                string json = File.ReadAllText(_filePath);
                Alarms = JsonSerializer.Deserialize<List<CustomAlarmItem>>(json) ?? new();
            }
            catch
            {
                Alarms = new();
            }
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Alarms, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}
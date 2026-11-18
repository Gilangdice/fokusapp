using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FokusApp.Services;

namespace FokusApp.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly string _username;
        private readonly DispatcherTimer _timer;
        private DateTime? _nextPrayerDateTime;
        private string _nextPrayerName = string.Empty;

        public DashboardView(string username)
        {
            InitializeComponent();
            _username = username;

            // Load semua data awal
            UpdateClockAndGreeting();
            UpdatePomodoroCard();
            LoadPrayerSchedule();
            LoadInitialTasks();

            // Timer 1 detik untuk jam, countdown sholat, dan sinkronisasi Pomodoro
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Stop timer saat pindah menu biar hemat RAM
            Unloaded += (_, _) => _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateClockAndGreeting();
            UpdatePomodoroCard();
            UpdatePrayerCountdown();
        }

        private void UpdateClockAndGreeting()
        {
            var now = DateTime.Now;
            ClockText.Text = now.ToString("HH:mm:ss");
            DateText.Text = now.ToString("dddd, d MMMM yyyy", new CultureInfo("id-ID"));

            string greeting = now.Hour switch
            {
                >= 4 and < 11 => "Selamat pagi",
                >= 11 and < 15 => "Selamat siang",
                >= 15 and < 18 => "Selamat sore",
                _ => "Selamat malam"
            };

            GreetText.Text = $"{greeting}, {_username}";
        }

        private void UpdatePomodoroCard()
        {
            var p = PomodoroService.Instance;
            PomodoroTimeText.Text = p.FormattedTime;
            PomodoroStatusText.Text = $"{p.StatusText} • Sesi {p.CompletedSessions}/{p.TargetSessions}";
            PomodoroProgressBar.Value = p.ProgressPercent;
        }

        private void LoadPrayerSchedule()
        {
            var schedule = PrayerScheduleStore.Load();

            if (schedule == null || string.IsNullOrEmpty(schedule.DistrictName))
            {
                NextPrayerText.Text = "Belum ada jadwal";
                PrayerCountdownText.Text = "";
                FollowingPrayerText.Text = "Pilih lokasi di menu Alarm";
                PrayerLocationText.Text = "Lokasi belum dipilih";
                _nextPrayerDateTime = null;
                return;
            }

            PrayerLocationText.Text = $"{schedule.DistrictName}, {schedule.RegencyName}";

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (schedule.ScheduleDate != today)
            {
                NextPrayerText.Text = "Jadwal perlu diperbarui";
                PrayerCountdownText.Text = "";
                FollowingPrayerText.Text = "Buka menu Alarm";
                _nextPrayerDateTime = null;
                return;
            }

            var prayers = new List<(string Name, string Time)>
            {
                ("Subuh", schedule.Fajr),
                ("Dzuhur", schedule.Dhuhr),
                ("Ashar", schedule.Asr),
                ("Maghrib", schedule.Maghrib),
                ("Isya", schedule.Isha)
            };

            var validPrayers = prayers
                .Select(p => new
                {
                    p.Name,
                    p.Time,
                    ParsedTime = TimeSpan.TryParse(p.Time, out TimeSpan parsed) ? parsed : (TimeSpan?)null
                })
                .Where(p => p.ParsedTime.HasValue)
                .ToList();

            if (validPrayers.Count == 0)
            {
                NextPrayerText.Text = "Jadwal tidak tersedia";
                PrayerCountdownText.Text = "";
                FollowingPrayerText.Text = "";
                _nextPrayerDateTime = null;
                return;
            }

            var now = DateTime.Now;
            var upcoming = validPrayers
                .Where(p => p.ParsedTime!.Value >= now.TimeOfDay)
                .ToList();

            if (upcoming.Count > 0)
            {
                _nextPrayerName = upcoming[0].Name;
                _nextPrayerDateTime = now.Date.Add(upcoming[0].ParsedTime!.Value);
                NextPrayerText.Text = $"{upcoming[0].Name}  {upcoming[0].Time}";

                FollowingPrayerText.Text = upcoming.Count > 1
                    ? $"{upcoming[1].Name}  {upcoming[1].Time}"
                    : "Berikutnya: Subuh besok";
            }
            else
            {
                _nextPrayerName = "Subuh besok";
                _nextPrayerDateTime = now.Date.AddDays(1).Add(validPrayers[0].ParsedTime!.Value);
                NextPrayerText.Text = $"Subuh besok  {validPrayers[0].Time}";

                if (validPrayers.Count > 1)
                {
                    FollowingPrayerText.Text = $"Dzuhur  {validPrayers[1].Time}";
                }
            }

            UpdatePrayerCountdown();
        }

        private void UpdatePrayerCountdown()
        {
            if (!_nextPrayerDateTime.HasValue) return;

            var remaining = _nextPrayerDateTime.Value - DateTime.Now;
            if (remaining <= TimeSpan.Zero)
            {
                LoadPrayerSchedule();
                return;
            }

            PrayerCountdownText.Text =
                $"Menuju {_nextPrayerName}: {(int)remaining.TotalHours} jam {remaining.Minutes} menit {remaining.Seconds} detik";
        }

        // --- Fitur To-Do List Interaktif ---

        private void LoadInitialTasks()
        {
            AddTaskItem("Revisi laporan praktikum", isChecked: true);
            AddTaskItem("Siapkan materi presentasi", isChecked: false);
            AddTaskItem("Olahraga sore", isChecked: false);
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            SubmitNewTask();
        }

        private void NewTaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SubmitNewTask();
            }
        }

        private void SubmitNewTask()
        {
            string text = NewTaskInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddTaskItem(text, isChecked: false);
            NewTaskInput.Clear();
        }

        private void AddTaskItem(string title, bool isChecked)
        {
            var cb = new CheckBox
            {
                Content = title,
                IsChecked = isChecked,
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            cb.Checked += (_, _) => UpdateTaskProgress();
            cb.Unchecked += (_, _) => UpdateTaskProgress();

            TaskListPanel.Children.Add(cb);
            UpdateTaskProgress();
        }

        private void UpdateTaskProgress()
        {
            var checkboxes = TaskListPanel.Children.OfType<CheckBox>().ToList();
            int total = checkboxes.Count;
            int done = checkboxes.Count(c => c.IsChecked == true);

            TaskProgressText.Text = $"{done}/{total} selesai";
            TaskProgressBar.Value = total == 0 ? 0 : (double)done / total * 100;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace FokusApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView(string username)
        {
            InitializeComponent();

            GreetText.Text = $"Selamat datang, {username}";

            DateText.Text = DateTime.Now.ToString(
                "dddd, d MMMM yyyy",
                new CultureInfo("id-ID"));

            LoadPrayerSchedule();
        }

        private void LoadPrayerSchedule()
        {
            var schedule = PrayerScheduleStore.Load();

            if (schedule == null ||
                string.IsNullOrEmpty(schedule.DistrictName))
            {
                NextPrayerText.Text = "Belum ada jadwal";
                FollowingPrayerText.Text = "Pilih lokasi di menu Alarm";
                PrayerLocationText.Text = "Lokasi belum dipilih";
                return;
            }

            PrayerLocationText.Text =
                $"{schedule.DistrictName}, {schedule.RegencyName}";

            // Pastikan jadwal merupakan jadwal hari ini
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (schedule.ScheduleDate != today)
            {
                NextPrayerText.Text = "Jadwal perlu diperbarui";
                FollowingPrayerText.Text = "Buka menu Alarm";
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
                    ParsedTime = TimeSpan.TryParse(
                        p.Time,
                        out TimeSpan parsed) ? parsed : (TimeSpan?)null
                })
                .Where(p => p.ParsedTime.HasValue)
                .ToList();

            if (validPrayers.Count == 0)
            {
                NextPrayerText.Text = "Jadwal tidak tersedia";
                FollowingPrayerText.Text = "";
                return;
            }

            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            var upcoming = validPrayers
                .Where(p => p.ParsedTime!.Value >= currentTime)
                .ToList();

            if (upcoming.Count > 0)
            {
                NextPrayerText.Text =
                    $"{upcoming[0].Name}  {upcoming[0].Time}";

                if (upcoming.Count > 1)
                {
                    FollowingPrayerText.Text =
                        $"{upcoming[1].Name}  {upcoming[1].Time}";
                }
                else
                {
                    FollowingPrayerText.Text =
                        "Berikutnya: Subuh besok";
                }
            }
            else
            {
                NextPrayerText.Text =
                    $"Subuh besok  {validPrayers[0].Time}";

                if (validPrayers.Count > 1)
                {
                    FollowingPrayerText.Text =
                        $"Dzuhur  {validPrayers[1].Time}";
                }
            }
        }
    }
}

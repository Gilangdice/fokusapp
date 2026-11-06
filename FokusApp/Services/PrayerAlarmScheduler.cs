
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using FokusApp.Views;

namespace FokusApp.Services
{
    public static class PrayerAlarmScheduler
    {
        private static readonly string[] PrayerNames =
        {
            "Subuh",
            "Dzuhur",
            "Ashar",
            "Maghrib",
            "Isya"
        };

        public static void SyncPrayerAlarms(
            PrayerScheduleData schedule,
            IReadOnlyDictionary<string, bool> enabledAlarms)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (schedule.ScheduleDate != today)
            {
                throw new InvalidOperationException(
                    "Jadwal salat belum diperbarui untuk hari ini.");
            }

            foreach (string prayerName in PrayerNames)
            {
                bool isEnabled =
                    enabledAlarms.TryGetValue(
                        prayerName, out bool enabled) && enabled;

                string taskName = $"FokusApp - Alarm {prayerName}";

                if (!isEnabled)
                {
                    DeleteTask(taskName);
                    continue;
                }

                string time = GetPrayerTime(schedule, prayerName);

                CreateTask(
                    taskName,
                    time,
                    $"--alarm {prayerName}");
            }
        }

        public static void RegisterDailyRefresh()
{
    CreateTask(
        "FokusApp - Refresh Jadwal Salat",
        "00:05",
        "--refresh");
}

        private static string GetPrayerTime(
            PrayerScheduleData schedule,
            string prayerName)
        {
            string time = prayerName switch
            {
                "Subuh" => schedule.Fajr,
                "Dzuhur" => schedule.Dhuhr,
                "Ashar" => schedule.Asr,
                "Maghrib" => schedule.Maghrib,
                "Isya" => schedule.Isha,
                _ => throw new ArgumentException(
                    "Nama salat tidak dikenal.")
            };

            if (!TimeSpan.TryParseExact(
                time,
                @"hh\:mm",
                CultureInfo.InvariantCulture,
                out TimeSpan parsedTime))
            {
                throw new FormatException(
                    $"Format waktu {prayerName} tidak valid: {time}");
            }

            return parsedTime.ToString(
                @"hh\:mm",
                CultureInfo.InvariantCulture);
        }

        // Menentukan perintah untuk menjalankan FokusApp
        private static string GetApplicationCommand(string arguments)
        {
            string? assemblyPath =
                Assembly.GetEntryAssembly()?.Location;

            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                assemblyPath = Path.GetFullPath(assemblyPath);

                // Utamakan file apphost FokusApp.exe
                string appHostPath =
                    Path.ChangeExtension(assemblyPath, ".exe");

                if (File.Exists(appHostPath))
                {
                    return $"\"{appHostPath}\" {arguments}";
                }

                // Jika apphost tidak tersedia, jalankan melalui dotnet
                string? dotnetPath = Environment.ProcessPath;

                if (!string.IsNullOrWhiteSpace(dotnetPath))
                {
                    return $"\"{dotnetPath}\" \"{assemblyPath}\" {arguments}";
                }
            }

            throw new InvalidOperationException(
                "File aplikasi FokusApp tidak ditemukan.");
        }

        private static void CreateTask(
            string taskName,
            string time,
            string arguments)
        {
            string taskCommand =
                GetApplicationCommand(arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            startInfo.ArgumentList.Add("/Create");
            startInfo.ArgumentList.Add("/SC");
            startInfo.ArgumentList.Add("DAILY");

            startInfo.ArgumentList.Add("/ST");
            startInfo.ArgumentList.Add(time);

            startInfo.ArgumentList.Add("/TN");
            startInfo.ArgumentList.Add(taskName);

            startInfo.ArgumentList.Add("/TR");
            startInfo.ArgumentList.Add(taskCommand);

            startInfo.ArgumentList.Add("/F");
            startInfo.ArgumentList.Add("/IT");

            using Process? process = Process.Start(startInfo);

            if (process == null)
            {
                throw new InvalidOperationException(
                    "Windows Task Scheduler gagal dijalankan.");
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Gagal mendaftarkan tugas: {taskName}. " +
                    "Periksa izin Windows Task Scheduler.");
            }
        }

        private static void DeleteTask(string taskName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                startInfo.ArgumentList.Add("/Delete");
                startInfo.ArgumentList.Add("/TN");
                startInfo.ArgumentList.Add(taskName);
                startInfo.ArgumentList.Add("/F");

                using Process? process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch
            {
                // Tugas yang belum tersedia boleh diabaikan.
            }
        }
    }
}

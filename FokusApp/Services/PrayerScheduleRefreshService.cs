
using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FokusApp.Views;

namespace FokusApp.Services
{
    public static class PrayerScheduleRefreshService
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<PrayerScheduleData?> RefreshAsync()
        {
            // Ambil lokasi terakhir yang tersimpan
            PrayerScheduleData? savedSchedule =
                PrayerScheduleStore.Load();

            if (savedSchedule == null)
                return null;

            string address =
                $"{savedSchedule.DistrictName}, " +
                $"{savedSchedule.RegencyName}, " +
                $"{savedSchedule.ProvinceName}, Indonesia";

            string encodedAddress =
                Uri.EscapeDataString(address);

            string date =
                DateTime.Now.ToString("dd-MM-yyyy");

            string url =
                $"https://api.aladhan.com/v1/timingsByAddress/" +
                $"{date}?address={encodedAddress}&method=20";

            string response =
                await client.GetStringAsync(url);

            var result =
                JsonSerializer.Deserialize<AlAdhanResponse>(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result?.Data?.Timings == null)
                throw new Exception(
                    "Jadwal salat tidak ditemukan dari API.");

            var timings = result.Data.Timings;

            // Perbarui jadwal tanpa mengubah lokasi tersimpan
            savedSchedule.ScheduleDate =
                DateTime.Now.ToString("yyyy-MM-dd");

            savedSchedule.Fajr = FormatTime(timings.Fajr);
            savedSchedule.Dhuhr = FormatTime(timings.Dhuhr);
            savedSchedule.Asr = FormatTime(timings.Asr);
            savedSchedule.Maghrib = FormatTime(timings.Maghrib);
            savedSchedule.Isha = FormatTime(timings.Isha);

            PrayerScheduleStore.Save(savedSchedule);

            return savedSchedule;
        }

        private static string FormatTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException(
                    "Waktu salat dari API kosong.");

            string time = value.Split(' ')[0];

            if (!TimeSpan.TryParseExact(
                time,
                @"hh\:mm",
                CultureInfo.InvariantCulture,
                out TimeSpan parsedTime))
            {
                throw new FormatException(
                    $"Format waktu dari API tidak valid: {value}");
            }

            return parsedTime.ToString(
                @"hh\:mm",
                CultureInfo.InvariantCulture);
        }
    }
}

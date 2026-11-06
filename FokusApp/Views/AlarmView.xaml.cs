
using FokusApp.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FokusApp.Views
{
    public partial class AlarmView : UserControl
    {
        private static readonly HttpClient client = new HttpClient();
        private bool _isRestoring = false;

        public AlarmView()
        {
            InitializeComponent();

            Loaded += AlarmView_Loaded;

            ProvinceComboBox.SelectionChanged += ProvinceComboBox_SelectionChanged;
            RegencyComboBox.SelectionChanged += RegencyComboBox_SelectionChanged;
            DistrictComboBox.SelectionChanged += DistrictComboBox_SelectionChanged;

            LoadPrayerTimesButton.Click += LoadPrayerTimesButton_Click;
            SaveAlarmSettingsButton.Click += SaveAlarmSettingsButton_Click;
        }

        private async void AlarmView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProvinces();
            await RestoreSavedLocation();
            LoadAlarmSettings();
        }

        private async Task LoadProvinces()
        {
            try
            {
                ProvinceComboBox.IsEnabled = false;

                string url = "https://wilayah.web.id/api/provinces?limit=100";
                string response = await client.GetStringAsync(url);

                var result = JsonSerializer.Deserialize<ApiResponse<WilayahItem>>(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result?.Data == null || result.Data.Count == 0)
                {
                    MessageBox.Show("Data provinsi tidak ditemukan.");
                    return;
                }

                ProvinceComboBox.ItemsSource = result.Data;
                ProvinceComboBox.DisplayMemberPath = nameof(WilayahItem.Name);
                ProvinceComboBox.SelectedValuePath = nameof(WilayahItem.Code);
                ProvinceComboBox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal mengambil data provinsi.\n\n" + ex.Message,
                    "Kesalahan API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task<bool> LoadRegencies(string provinceCode)
        {
            try
            {
                string url = $"https://wilayah.web.id/api/regencies/{provinceCode}";
                string response = await client.GetStringAsync(url);

                var result = JsonSerializer.Deserialize<ApiResponse<WilayahItem>>(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result?.Data == null || result.Data.Count == 0)
                    return false;

                RegencyComboBox.ItemsSource = result.Data;
                RegencyComboBox.DisplayMemberPath = nameof(WilayahItem.Name);
                RegencyComboBox.SelectedValuePath = nameof(WilayahItem.Code);
                RegencyComboBox.IsEnabled = true;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal mengambil data kabupaten/kota.\n\n" + ex.Message,
                    "Kesalahan API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        private async Task<bool> LoadDistricts(string regencyCode)
        {
            try
            {
                string url = $"https://wilayah.web.id/api/districts/{regencyCode}";
                string response = await client.GetStringAsync(url);

                var result = JsonSerializer.Deserialize<ApiResponse<WilayahItem>>(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result?.Data == null || result.Data.Count == 0)
                    return false;

                DistrictComboBox.ItemsSource = result.Data;
                DistrictComboBox.DisplayMemberPath = nameof(WilayahItem.Name);
                DistrictComboBox.SelectedValuePath = nameof(WilayahItem.Code);
                DistrictComboBox.IsEnabled = true;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal mengambil data kecamatan.\n\n" + ex.Message,
                    "Kesalahan API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        private async void ProvinceComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isRestoring || ProvinceComboBox.SelectedValue == null)
                return;

            string provinceCode = ProvinceComboBox.SelectedValue.ToString()!;

            RegencyComboBox.ItemsSource = null;
            DistrictComboBox.ItemsSource = null;

            RegencyComboBox.IsEnabled = false;
            DistrictComboBox.IsEnabled = false;
            LoadPrayerTimesButton.IsEnabled = false;

            await LoadRegencies(provinceCode);
        }

        private async void RegencyComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isRestoring || RegencyComboBox.SelectedValue == null)
                return;

            string regencyCode = RegencyComboBox.SelectedValue.ToString()!;

            DistrictComboBox.ItemsSource = null;
            DistrictComboBox.IsEnabled = false;
            LoadPrayerTimesButton.IsEnabled = false;

            await LoadDistricts(regencyCode);
        }

        private void DistrictComboBox_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isRestoring)
                return;

            LoadPrayerTimesButton.IsEnabled =
                DistrictComboBox.SelectedValue != null;
        }

        private async Task RestoreSavedLocation()
        {
            var saved = PrayerScheduleStore.Load();

            if (saved == null || string.IsNullOrEmpty(saved.ProvinceCode))
                return;

            try
            {
                _isRestoring = true;

                ProvinceComboBox.SelectedValue = saved.ProvinceCode;

                bool regenciesLoaded = await LoadRegencies(saved.ProvinceCode);

                if (!regenciesLoaded)
                    return;

                RegencyComboBox.SelectedValue = saved.RegencyCode;

                bool districtsLoaded = await LoadDistricts(saved.RegencyCode);

                if (!districtsLoaded)
                    return;

                DistrictComboBox.SelectedValue = saved.DistrictCode;

                LoadPrayerTimesButton.IsEnabled =
                    DistrictComboBox.SelectedValue != null;

                if (ProvinceComboBox.SelectedItem is WilayahItem province &&
                    RegencyComboBox.SelectedItem is WilayahItem regency &&
                    DistrictComboBox.SelectedItem is WilayahItem district)
                {
                    SelectedLocationText.Text =
                        $"{district.Name}, {regency.Name}, {province.Name}";
                }

                ShowSavedTimings(saved);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal mengembalikan lokasi sebelumnya.\n\n" + ex.Message,
                    "Penyimpanan Lokasi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _isRestoring = false;
            }

            if (saved.ScheduleDate != DateTime.Now.ToString("yyyy-MM-dd"))
                await LoadPrayerTimesAsync();
        }

        private void ShowSavedTimings(PrayerScheduleData saved)
        {
            FajrText.Text = FormatTime(saved.Fajr);
            DhuhrText.Text = FormatTime(saved.Dhuhr);
            AsrText.Text = FormatTime(saved.Asr);
            MaghribText.Text = FormatTime(saved.Maghrib);
            IshaText.Text = FormatTime(saved.Isha);
        }

        private async void LoadPrayerTimesButton_Click(
            object sender, RoutedEventArgs e)
        {
            await LoadPrayerTimesAsync();
        }

        private async Task LoadPrayerTimesAsync()
        {
            var province = ProvinceComboBox.SelectedItem as WilayahItem;
            var regency = RegencyComboBox.SelectedItem as WilayahItem;
            var district = DistrictComboBox.SelectedItem as WilayahItem;

            if (province == null || regency == null || district == null)
                return;

            try
            {
                LoadPrayerTimesButton.IsEnabled = false;
                LoadPrayerTimesButton.Content = "Memuat...";

                string address =
                    $"{district.Name}, {regency.Name}, {province.Name}, Indonesia";

                string encodedAddress = Uri.EscapeDataString(address);
                string date = DateTime.Now.ToString("dd-MM-yyyy");

                string url =
                    $"https://api.aladhan.com/v1/timingsByAddress/{date}?address={encodedAddress}&method=20";

                string response = await client.GetStringAsync(url);

                var result = JsonSerializer.Deserialize<AlAdhanResponse>(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result?.Data?.Timings == null)
                {
                    MessageBox.Show("Jadwal salat tidak ditemukan.");
                    return;
                }

                var timings = result.Data.Timings;

                var schedule = new PrayerScheduleData
                {
                    ProvinceCode = province.Code,
                    ProvinceName = province.Name,
                    RegencyCode = regency.Code,
                    RegencyName = regency.Name,
                    DistrictCode = district.Code,
                    DistrictName = district.Name,
                    ScheduleDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Fajr = FormatTime(timings.Fajr),
                    Dhuhr = FormatTime(timings.Dhuhr),
                    Asr = FormatTime(timings.Asr),
                    Maghrib = FormatTime(timings.Maghrib),
                    Isha = FormatTime(timings.Isha)
                };

                PrayerScheduleStore.Save(schedule);

                ShowSavedTimings(schedule);

                SelectedLocationText.Text =
                    $"{district.Name}, {regency.Name}, {province.Name}";

                MessageBox.Show(
                    "Jadwal salat berhasil diperbarui dan disimpan.",
                    "Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Gagal mengambil jadwal salat.\n\n" + ex.Message,
                    "Kesalahan API",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                LoadPrayerTimesButton.Content = "Tampilkan Jadwal";
                LoadPrayerTimesButton.IsEnabled =
                    DistrictComboBox.SelectedValue != null;
            }
        }

        private void LoadAlarmSettings()
        {
            var settings = PrayerAlarmSettingsStore.Load();

            FajrAlarmCheckBox.IsChecked =
                settings.GetValueOrDefault("Subuh", false);

            DhuhrAlarmCheckBox.IsChecked =
                settings.GetValueOrDefault("Dzuhur", false);

            AsrAlarmCheckBox.IsChecked =
                settings.GetValueOrDefault("Ashar", false);

            MaghribAlarmCheckBox.IsChecked =
                settings.GetValueOrDefault("Maghrib", false);

            IshaAlarmCheckBox.IsChecked =
                settings.GetValueOrDefault("Isya", false);
        }

        private Dictionary<string, bool> GetEnabledAlarms()
        {
            return new Dictionary<string, bool>
            {
                ["Subuh"] = FajrAlarmCheckBox.IsChecked == true,
                ["Dzuhur"] = DhuhrAlarmCheckBox.IsChecked == true,
                ["Ashar"] = AsrAlarmCheckBox.IsChecked == true,
                ["Maghrib"] = MaghribAlarmCheckBox.IsChecked == true,
                ["Isya"] = IshaAlarmCheckBox.IsChecked == true
            };
        }

        private void SaveAlarmSettingsButton_Click(
            object sender, RoutedEventArgs e)
        {
            var settings = GetEnabledAlarms();

            try
            {
                PrayerAlarmSettingsStore.Save(settings);

                PrayerScheduleData? schedule =
                    PrayerScheduleStore.Load();

                if (schedule == null)
                {
                    MessageBox.Show(
                        "Pilihan alarm berhasil disimpan, tetapi jadwal salat belum tersedia.\n\nSilakan tampilkan jadwal salat terlebih dahulu.",
                        "Jadwal Belum Tersedia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                PrayerAlarmScheduler.SyncPrayerAlarms(schedule, settings);

                PrayerAlarmScheduler.RegisterDailyRefresh();

                MessageBox.Show(
                    "Pengaturan alarm berhasil disimpan!\n\n" +
                    "Alarm yang dipilih sudah didaftarkan ke Windows.\n" +
                    "Jadwal salat akan diperbarui otomatis setiap hari pukul 00.05.",
                    "Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Pengaturan tersimpan, tetapi pendaftaran alarm mengalami kendala.\n\n" +
                    ex.Message,
                    "Kesalahan Alarm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private string FormatTime(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return "--:--";

            return time.Split(' ')[0];
        }
    }
}

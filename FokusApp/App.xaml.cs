
using System;
using System.Windows;
using FokusApp.Services;
using FokusApp.Views;

namespace FokusApp
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Jalankan notifikasi alarm salat
            if (e.Args.Length >= 2 &&
                e.Args[0].Equals("--alarm", StringComparison.OrdinalIgnoreCase))
            {
                string prayerName = e.Args[1];

                var alarmWindow = new AlarmNotificationWindow(prayerName);

                MainWindow = alarmWindow;
                alarmWindow.ShowDialog();

                return;
            }

            // Perbarui jadwal salat secara otomatis
            if (e.Args.Length >= 1 &&
                e.Args[0].Equals("--refresh", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var schedule =
                        await PrayerScheduleRefreshService.RefreshAsync();

                    if (schedule != null)
                    {
                        var settings = PrayerAlarmSettingsStore.Load();

                        PrayerAlarmScheduler.SyncPrayerAlarms(
                            schedule,
                            settings);

                        PrayerAlarmScheduler.RegisterDailyRefresh();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Gagal memperbarui jadwal salat: " + ex.Message);
                }

                Shutdown();
                return;
            }

            // Jalankan aplikasi seperti biasa
            Database.Init();

            var loginWindow = new LoginWindow();

            MainWindow = loginWindow;
            loginWindow.Show();
        }
    }
}

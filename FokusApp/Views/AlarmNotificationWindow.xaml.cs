
using System;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace FokusApp.Views
{
    public partial class AlarmNotificationWindow : Window
    {
        private readonly DispatcherTimer _soundTimer;
        private int _soundCount = 0;

        public AlarmNotificationWindow(string prayerName)
        {
            InitializeComponent();

            PrayerNameText.Text = prayerName;

            _soundTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            _soundTimer.Tick += SoundTimer_Tick;

            Loaded += (sender, e) =>
            {
                PlayAlarmSound();
                _soundTimer.Start();
            };

            Closed += (sender, e) =>
            {
                _soundTimer.Stop();
            };
        }

        private void PlayAlarmSound()
        {
            SystemSounds.Exclamation.Play();
            _soundCount++;
        }

        private void SoundTimer_Tick(object? sender, EventArgs e)
        {
            if (_soundCount >= 5)
            {
                _soundTimer.Stop();
                return;
            }

            PlayAlarmSound();
        }

        private void CloseButton_Click(
            object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

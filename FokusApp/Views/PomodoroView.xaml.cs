using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FokusApp.Views
{
    public partial class PomodoroView : UserControl
    {
        // Diameter garis tengah cincin = 300 - 14 (tebal garis); satuan dash = tebal garis
        private const double Units = Math.PI * (300 - 14) / 14;

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly int _total = 25 * 60;
        private int _left = 25 * 60;

        public PomodoroView()
        {
            InitializeComponent();
            _timer.Tick += Timer_Tick;
            Render();
            Unloaded += (_, _) => _timer.Stop();
        }

        private void Render()
        {
            ClockText.Text = $"{_left / 60:00}:{_left % 60:00}";
            double fraction = (double)_left / _total;
            ProgressRing.Visibility = _left <= 0 ? Visibility.Hidden : Visibility.Visible;
            ProgressRing.StrokeDashArray = new DoubleCollection { fraction * Units, 100 };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _left--;
            Render();
            if (_left <= 0)
            {
                _timer.Stop();
                StartButton.Content = "Mulai";
                StatusText.Text = "Sesi selesai";
                SystemSounds.Asterisk.Play();
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                StartButton.Content = "Lanjut";
                StatusText.Text = "Dijeda";
            }
            else
            {
                if (_left <= 0) { _left = _total; Render(); }
                _timer.Start();
                StartButton.Content = "Jeda";
                StatusText.Text = "Sedang fokus";
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _left = _total;
            StartButton.Content = "Mulai";
            StatusText.Text = "Siap memulai";
            Render();
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FokusApp.Services;

namespace FokusApp.Views
{
    public partial class PomodoroView : UserControl
    {
        private const double Units = Math.PI * (300 - 14) / 14;
        private readonly PomodoroService _pomodoro = PomodoroService.Instance;

        public PomodoroView()
        {
            InitializeComponent();
            _pomodoro.StateChanged += Render;
            Unloaded += (_, _) => _pomodoro.StateChanged -= Render;
            Render();
        }

        private void Render()
        {
            ClockText.Text = _pomodoro.FormattedTime;
            StatusText.Text = _pomodoro.StatusText;

            if (_pomodoro.IsRunning)
                StartButton.Content = "Jeda";
            else if (_pomodoro.SecondsLeft < _pomodoro.TotalSeconds && _pomodoro.SecondsLeft > 0)
                StartButton.Content = "Lanjut";
            else
                StartButton.Content = "Mulai";

            double fraction = (double)_pomodoro.SecondsLeft / _pomodoro.TotalSeconds;
            ProgressRing.Visibility = _pomodoro.SecondsLeft <= 0 ? Visibility.Hidden : Visibility.Visible;
            ProgressRing.StrokeDashArray = new DoubleCollection { fraction * Units, 100 };
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _pomodoro.ToggleStartPause();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _pomodoro.Reset();
        }
    }
}
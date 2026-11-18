using System;
using System.Media;
using System.Windows.Threading;

namespace FokusApp.Services
{
    public class PomodoroService
    {
        public static PomodoroService Instance { get; } = new();

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

        public int TotalSeconds { get; } = 25 * 60;
        public int SecondsLeft { get; private set; } = 25 * 60;
        public int CompletedSessions { get; private set; } = 0;
        public int TargetSessions { get; } = 4;
        public bool IsRunning => _timer.IsEnabled;
        public string StatusText { get; private set; } = "Siap memulai";

        public event Action? StateChanged;

        private PomodoroService()
        {
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            SecondsLeft--;

            if (SecondsLeft <= 0)
            {
                SecondsLeft = 0;
                _timer.Stop();
                CompletedSessions++;
                StatusText = "Sesi selesai";
                SystemSounds.Asterisk.Play();
            }

            StateChanged?.Invoke();
        }

        public void ToggleStartPause()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                StatusText = "Dijeda";
            }
            else
            {
                if (SecondsLeft <= 0)
                {
                    SecondsLeft = TotalSeconds;
                }
                _timer.Start();
                StatusText = "Sedang fokus";
            }

            StateChanged?.Invoke();
        }

        public void Reset()
        {
            _timer.Stop();
            SecondsLeft = TotalSeconds;
            StatusText = "Siap memulai";
            StateChanged?.Invoke();
        }

        public string FormattedTime => $"{SecondsLeft / 60:00}:{SecondsLeft % 60:00}";
        public double ProgressPercent => (double)(TotalSeconds - SecondsLeft) / TotalSeconds * 100;
    }
}
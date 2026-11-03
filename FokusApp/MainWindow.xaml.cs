using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FokusApp.Views;

namespace FokusApp
{
    public partial class MainWindow : Window
    {
        private readonly string _user;
        private Button? _aktif;   // tombol menu yang sedang dipilih

        public MainWindow(string user)
        {
            InitializeComponent();
            _user = user;
            Title = "Fokus - " + _user;
            MainContent.Content = new DashboardView(_user);
            SetAktif(BtnDashboard);
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            MainContent.Content = btn.Tag?.ToString() switch
            {
                "Pomodoro" => new PomodoroView(),
                "Notes" => new NotesView(),
                "Alarm" => new AlarmView(),
                "Settings" => new SettingsView(),
                _ => new DashboardView(_user)
            };
            SetAktif(btn);
        }

        // Tombol yang aktif berwarna merah, yang lain kembali normal
        private void SetAktif(Button btn)
        {
            if (_aktif != null)
                _aktif.Foreground = (Brush)FindResource("TextBrush");

            btn.Foreground = (Brush)FindResource("AccentBrush");
            _aktif = btn;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var jawab = MessageBox.Show("Yakin ingin keluar dari akun?", "Keluar",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (jawab != MessageBoxResult.Yes) return;

            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }
    }
}
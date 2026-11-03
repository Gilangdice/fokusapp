using System.Windows;
using FokusApp.Services;

namespace FokusApp.Views
{
    public partial class RegisterWindow : Window
    {
        public string CreatedUsername { get; private set; } = "";

        public RegisterWindow()
        {
            InitializeComponent();
            UserBox.Focus();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string user = UserBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string pass = PassBox.Password;

            // 1. Validasi dulu sebelum menyentuh database
            if (user.Length < 3) { ErrorText.Text = "Username minimal 3 karakter."; return; }
            if (!email.Contains('@') || !email.Contains('.')) { ErrorText.Text = "Format email tidak valid."; return; }
            if (pass.Length < 6) { ErrorText.Text = "Password minimal 6 karakter."; return; }
            if (pass != Pass2Box.Password) { ErrorText.Text = "Password dan pengulangannya tidak sama."; return; }

            // 2. Simpan ke database
            var (ok, message) = Database.Register(user, email, pass);
            if (!ok) { ErrorText.Text = message; return; }

            // 3. Beri tahu jendela login bahwa pendaftaran berhasil, lalu tutup
            CreatedUsername = user;
            DialogResult = true;
        }
    }
}
using System;
using System.Windows;
using FokusApp.Models;
using FokusApp.Services;

namespace FokusApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            UserBox.Focus();
        }

        // Login lokal: cek username/email + password ke database
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string identity = UserBox.Text.Trim();
            string pass = PassBox.Password;

            if (identity.Length == 0 || pass.Length == 0)
            {
                ErrorText.Text = "Username/email dan password wajib diisi.";
                return;
            }

            User? user = Database.Login(identity, pass);
            if (user == null)
            {
                ErrorText.Text = "Username/email atau password salah.";
                PassBox.Clear();
                return;
            }

            OpenMain(user);
        }

        // Login Google: buka browser, ambil email, simpan/ambil akun di database
        private async void Google_Click(object sender, RoutedEventArgs e)
        {
            if (!GoogleAuthService.IsConfigured)
            {
                MessageBox.Show(
                    "Client ID Google belum diisi.\n\nBuka Services/GoogleConfig.cs lalu isi ClientId dan " +
                    "ClientSecret dari Google Cloud Console.",
                    "Login Google belum dikonfigurasi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            GoogleButton.IsEnabled = false;
            ErrorText.Text = "";
            try
            {
                var (email, name) = await GoogleAuthService.SignInAsync();
                User user = Database.LoginWithGoogle(email, name);
                OpenMain(user);
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Login Google gagal: " + ex.Message;
            }
            finally
            {
                GoogleButton.IsEnabled = true;
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var reg = new RegisterWindow { Owner = this };
            if (reg.ShowDialog() == true)
            {
                UserBox.Text = reg.CreatedUsername;
                ErrorText.Text = "Akun dibuat. Silakan masuk.";
                PassBox.Focus();
            }
        }

        // MainWindow masih menerima teks nama, jadi kita kirim DisplayName
        private void OpenMain(User user)
        {
            var main = new MainWindow(user.DisplayName);
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
    }
}
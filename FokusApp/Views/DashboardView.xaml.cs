using System;
using System.Globalization;
using System.Windows.Controls;

namespace FokusApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView(string username)
        {
            InitializeComponent();
            GreetText.Text = $"Selamat datang, {username}";
            DateText.Text = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("id-ID"));
        }
    }
}

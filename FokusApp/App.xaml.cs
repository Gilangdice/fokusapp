using System.Windows;
using FokusApp.Services;

namespace FokusApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Siapkan database sebelum jendela login muncul
            Database.Init();
            base.OnStartup(e);
        }
    }
}
using System.Windows.Controls;
using FokusApp.Services;

namespace FokusApp.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DbText.Text = Database.Description;
        }
    }
}
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FokusApp.Views
{
    public class TaskItem
    {
        public string Title { get; set; } = "";
        public bool IsDone { get; set; }
    }

    public partial class NotesView : UserControl
    {
        private readonly ObservableCollection<TaskItem> _tasks = new()
        {
            new TaskItem { Title = "Revisi laporan praktikum", IsDone = true },
            new TaskItem { Title = "Siapkan materi presentasi" },
            new TaskItem { Title = "Olahraga sore" }
        };

        public NotesView()
        {
            InitializeComponent();
            TaskList.ItemsSource = _tasks;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string title = NewTask.Text.Trim();
            if (title.Length == 0) return;
            _tasks.Add(new TaskItem { Title = title });
            NewTask.Clear();
        }
    }
}

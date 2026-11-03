namespace FokusApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string LoginMethod { get; set; } = "lokal";   // "lokal" atau "google"
        public string CreatedAt { get; set; } = "";
    }
}
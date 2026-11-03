using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace FokusApp.Services
{
    // Alur: buka browser -> pengguna pilih akun Google -> Google kirim kode ke
    // localhost -> library menukar kode jadi access token -> kita ambil email & nama.
    public static class GoogleAuthService
    {
        public static bool IsConfigured => !GoogleConfig.ClientId.StartsWith("ISI_");

        public static async Task<(string Email, string Name)> SignInAsync()
        {
            // Folder sementara unik supaya Google selalu menampilkan pemilihan akun
            string store = Path.Combine(Path.GetTempPath(), "fokus-google-" + Guid.NewGuid());

            try
            {
                var secrets = new ClientSecrets
                {
                    ClientId = GoogleConfig.ClientId,
                    ClientSecret = GoogleConfig.ClientSecret
                };

                // Batas waktu 2 menit supaya tidak menunggu selamanya kalau browser ditutup
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

                UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    new[] { "openid", "email", "profile" },
                    "user",
                    cts.Token,
                    new FileDataStore(store, true));

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", credential.Token.AccessToken);

                string json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string email = root.GetProperty("email").GetString() ?? "";
                string name = root.TryGetProperty("name", out JsonElement n) ? (n.GetString() ?? email) : email;
                return (email, name);
            }
            catch (OperationCanceledException)
            {
                throw new Exception("Waktu login Google habis (2 menit). Silakan coba lagi.");
            }
            finally
            {
                try { if (Directory.Exists(store)) Directory.Delete(store, true); } catch { }
            }
        }
    }
}
using FokusApp.Models;
using Microsoft.Data.SqlClient;

namespace FokusApp.Services
{
    // Semua akses ke SQL Server ada di file ini
    public static class Database
    {
        // ===== Nama server (sesuai komputermu: LocalDB) =====
        private const string Server = @"(localdb)\MSSQLLocalDB";
        private const string DbName = "FokusDB";
        // ====================================================

        private static string ConnStr => new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = DbName,
            IntegratedSecurity = true,        // login pakai akun Windows
            TrustServerCertificate = true
        }.ConnectionString;

        public static string Description => $"SQL Server {Server} / {DbName}";

        // Membuat database dan tabel kalau belum ada, lalu mengisi akun demo
        public static void Init()
        {
            // 1. Buat database FokusDB (terhubung dulu ke database sistem 'master')
            string masterConn = new SqlConnectionStringBuilder(ConnStr) { InitialCatalog = "master" }.ConnectionString;
            using (var conn = new SqlConnection(masterConn))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"IF DB_ID('{DbName}') IS NULL CREATE DATABASE [{DbName}]";
                cmd.ExecuteNonQuery();
            }

            // 2. Buat tabel users
            int count;
            using (var conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    IF OBJECT_ID('dbo.users', 'U') IS NULL
                    CREATE TABLE dbo.users (
                        id            INT IDENTITY(1,1) PRIMARY KEY,
                        username      NVARCHAR(50)  NOT NULL UNIQUE,
                        email         NVARCHAR(150) NOT NULL UNIQUE,
                        password_hash NVARCHAR(200) NULL,
                        display_name  NVARCHAR(100) NOT NULL,
                        login_method  NVARCHAR(20)  NOT NULL DEFAULT 'lokal',
                        created_at    DATETIME2     NOT NULL DEFAULT SYSDATETIME()
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT COUNT(*) FROM dbo.users";
                count = (int)cmd.ExecuteScalar()!;
            }

            // 3. Akun demo kalau tabel masih kosong
            if (count == 0)
                Register("admin", "admin@fokus.local", "admin123", "Administrator");
        }

        // Daftar akun baru (login lokal)
        public static (bool Ok, string Message) Register(string username, string email, string password, string? displayName = null)
        {
            using var conn = new SqlConnection(ConnStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO dbo.users (username, email, password_hash, display_name, login_method)
                                VALUES (@u, @e, @p, @d, 'lokal')";
            cmd.Parameters.AddWithValue("@u", username.Trim());
            cmd.Parameters.AddWithValue("@e", email.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@p", PasswordHasher.Hash(password));
            cmd.Parameters.AddWithValue("@d", string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim());

            try
            {
                cmd.ExecuteNonQuery();
                return (true, "Pendaftaran berhasil.");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)   // pelanggaran UNIQUE
            {
                return (false, "Username atau email sudah terdaftar.");
            }
        }

        // Login lokal: identity boleh username atau email
        public static User? Login(string identity, string password)
        {
            using var conn = new SqlConnection(ConnStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TOP 1 id, username, email, display_name, login_method, created_at, password_hash
                                FROM dbo.users
                                WHERE username = @i OR email = @i";
            cmd.Parameters.AddWithValue("@i", identity.Trim());

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            if (r.IsDBNull(6)) return null;                        // akun Google tidak punya password
            if (!PasswordHasher.Verify(password, r.GetString(6))) return null;

            return ReadUser(r);
        }

        // Login Google: kalau email sudah ada pakai akun itu, kalau belum buat akun baru
        public static User LoginWithGoogle(string email, string name)
        {
            email = email.Trim().ToLowerInvariant();

            using var conn = new SqlConnection(ConnStr);
            conn.Open();

            // 1. Cari akun dengan email itu
            using (var find = conn.CreateCommand())
            {
                find.CommandText = @"SELECT TOP 1 id, username, email, display_name, login_method, created_at
                                     FROM dbo.users WHERE email = @e";
                find.Parameters.AddWithValue("@e", email);
                using var r = find.ExecuteReader();
                if (r.Read()) return ReadUser(r);
            }

            // 2. Belum ada: buat username unik dari bagian depan email
            string baseName = email.Split('@')[0];
            if (baseName.Length > 40) baseName = baseName.Substring(0, 40);
            string username = baseName;
            int n = 1;
            while (UsernameExists(conn, username))
                username = baseName + (++n);

            string display = string.IsNullOrWhiteSpace(name) ? username : name.Trim();

            using var ins = conn.CreateCommand();
            ins.CommandText = @"INSERT INTO dbo.users (username, email, password_hash, display_name, login_method)
                                OUTPUT INSERTED.id, INSERTED.username, INSERTED.email,
                                       INSERTED.display_name, INSERTED.login_method, INSERTED.created_at
                                VALUES (@u, @e, NULL, @d, 'google')";
            ins.Parameters.AddWithValue("@u", username);
            ins.Parameters.AddWithValue("@e", email);
            ins.Parameters.AddWithValue("@d", display);

            using var created = ins.ExecuteReader();
            created.Read();
            return ReadUser(created);
        }

        private static bool UsernameExists(SqlConnection conn, string username)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM dbo.users WHERE username = @u";
            cmd.Parameters.AddWithValue("@u", username);
            return (int)cmd.ExecuteScalar()! > 0;
        }

        // Membaca 6 kolom pertama: id, username, email, display_name, login_method, created_at
        private static User ReadUser(SqlDataReader r) => new()
        {
            Id = r.GetInt32(0),
            Username = r.GetString(1),
            Email = r.GetString(2),
            DisplayName = r.GetString(3),
            LoginMethod = r.GetString(4),
            CreatedAt = r.GetDateTime(5).ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
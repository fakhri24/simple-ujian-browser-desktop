using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Mengambil config.json dari Firebase Hosting saat aplikasi start.
    /// Operator mengganti pengaturan cukup dengan mengedit satu file di server,
    /// dan semua laptop ikut berubah tanpa membagikan ulang aplikasi.
    /// </summary>
    internal static class ConfigService
    {
        // Alamat config.json bawaan (di Firebase Hosting yang sudah dipakai).
        // Bisa di-override lewat baris pertama config.txt untuk uji coba / staging.
        public const string DefaultConfigUrl = "https://simple-ujian.web.app/lockdown-config.json";

        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Mengambil & mengurai config.json. Melempar exception jika gagal jaringan
        /// atau JSON tidak valid; pemanggil menampilkan pesan + tombol "Coba lagi".
        /// </summary>
        public static async Task<AppConfig> FetchAsync(string url)
        {
            // Cache-buster agar selalu mengambil versi terbaru, bukan cache lama.
            string sep = url.Contains('?') ? "&" : "?";
            string busted = url + sep + "t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json = await Http.GetStringAsync(busted);

            AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            if (config is null || string.IsNullOrWhiteSpace(config.ExamUrl))
                throw new InvalidOperationException("config.json tidak memuat 'examUrl' yang valid.");

            return config;
        }

        /// <summary>
        /// Menentukan alamat config.json: baris pertama config.txt (jika ada &amp; tidak
        /// dikomentari), atau alamat bawaan. config.txt kini hanya menunjuk LOKASI
        /// config.json, bukan menyimpan pengaturan itu sendiri.
        /// </summary>
        public static string ResolveConfigUrl()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "config.txt");
                if (File.Exists(path))
                {
                    foreach (string raw in File.ReadAllLines(path))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#"))
                            continue;
                        return line; // baris valid pertama = override alamat config.json
                    }
                }
            }
            catch
            {
                // abaikan; pakai alamat bawaan
            }
            return DefaultConfigUrl;
        }
    }
}

using System.Text.Json.Serialization;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Bentuk data config.json yang diambil dari Firebase Hosting.
    /// Sumber kebenaran tunggal untuk URL ujian, URL exit, dan password admin.
    /// </summary>
    internal sealed class AppConfig
    {
        [JsonPropertyName("examUrl")]
        public string? ExamUrl { get; set; }

        [JsonPropertyName("exitUrl")]
        public string? ExitUrl { get; set; }

        [JsonPropertyName("adminPassword")]
        public PasswordSpec? AdminPassword { get; set; }
    }

    /// <summary>
    /// Parameter verifikasi password admin (PBKDF2). Tidak memuat password polos --
    /// hanya salt + hash, sehingga aman walau config.json bisa dibaca publik.
    /// </summary>
    internal sealed class PasswordSpec
    {
        [JsonPropertyName("algo")]
        public string? Algo { get; set; }

        [JsonPropertyName("iterations")]
        public int Iterations { get; set; }

        [JsonPropertyName("salt")]
        public string? Salt { get; set; }

        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
    }
}

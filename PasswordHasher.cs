using System.Security.Cryptography;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Verifikasi password admin memakai PBKDF2-SHA256 (hash satu-arah bergaram).
    /// Ini BUKAN enkripsi: aplikasi tidak pernah membuka kembali password asli,
    /// hanya mencocokkan hasil hash. Karena itu tak ada "kunci rahasia" yang perlu
    /// disembunyikan -- yang disimpan di config.json hanyalah salt + hash.
    /// </summary>
    internal static class PasswordHasher
    {
        public const string Algorithm = "PBKDF2-SHA256";
        public const int DefaultIterations = 100_000; // makin tinggi makin lambat di-brute-force
        private const int SaltSize = 16;              // byte
        private const int HashSize = 32;              // byte (ukuran SHA-256)

        /// <summary>
        /// Membuat salt acak + hash dari sebuah password. Dipakai oleh mode
        /// --make-hash untuk menghasilkan nilai yang ditempel ke config.json.
        /// </summary>
        public static (string Salt, string Hash) Create(string password, int iterations = DefaultIterations)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
            return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        /// <summary>
        /// Mencocokkan password yang diketik dengan salt+hash dari config.
        /// Memakai perbandingan waktu-tetap (FixedTimeEquals) untuk mencegah timing attack.
        /// Mengembalikan false jika salt/hash rusak.
        /// </summary>
        public static bool Verify(string password, string saltBase64, string hashBase64, int iterations)
        {
            try
            {
                byte[] salt = Convert.FromBase64String(saltBase64);
                byte[] expected = Convert.FromBase64String(hashBase64);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }
    }
}

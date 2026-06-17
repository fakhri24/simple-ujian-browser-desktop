using System.Windows;
using System.Windows.Input;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Dialog modal untuk verifikasi password admin sebelum keluar aplikasi.
    /// Memverifikasi password di dalam dialog sendiri, dan baru mengembalikan
    /// DialogResult = true bila password BENAR.
    /// </summary>
    public partial class PasswordDialog : Window
    {
        // Password admin yang benar (lihat AGENTS.md). Untuk produksi nyata,
        // sebaiknya disimpan ter-hash, bukan plain-text. Cukup untuk tahap ini.
        private const string AdminPassword = "Admin123!";

        public PasswordDialog()
        {
            InitializeComponent();

            // Fokuskan kotak password begitu dialog tampil agar admin langsung mengetik.
            Loaded += (_, _) => PasswordInput.Focus();

            // Dukungan keyboard: Enter = konfirmasi, Esc = batal.
            PasswordInput.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter) TryConfirm();
                else if (e.Key == Key.Escape) { DialogResult = false; }
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => TryConfirm();

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        /// <summary>
        /// Cek password. Jika benar -> tutup dialog dengan hasil sukses.
        /// Jika salah -> tampilkan pesan error dan kosongkan kotak input.
        /// </summary>
        private void TryConfirm()
        {
            if (PasswordInput.Password == AdminPassword)
            {
                DialogResult = true; // sukses -> menutup dialog & memberi sinyal "boleh keluar"
            }
            else
            {
                ErrorText.Text = "Password salah. Coba lagi.";
                ErrorText.Visibility = Visibility.Visible;
                PasswordInput.Clear();
                PasswordInput.Focus();
            }
        }
    }
}

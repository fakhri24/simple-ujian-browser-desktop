using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace SecureExamBrowser
{
    /// <summary>
    /// Code-behind untuk MainWindow.
    /// Phase 2: integrasi WebView2 + baca URL dari config.txt + konfigurasi keamanan browser.
    /// </summary>
    public partial class MainWindow : Window
    {
        // URL cadangan yang dipakai jika config.txt tidak ditemukan atau kosong.
        private const string DefaultUrl = "https://simple-ujian.web.app/";

        // Keyboard hook untuk memblokir tombol sistem (Phase 3).
        private KeyboardHook? _keyboardHook;

        // Phase 4: penanda bahwa keluar sudah disetujui (password admin benar).
        // Selama false, setiap upaya menutup jendela akan dibatalkan.
        private bool _allowClose;

        // Mencegah dialog password terbuka berkali-kali (mis. Ctrl+Shift+Q ditekan berulang).
        private bool _exitDialogOpen;

        public MainWindow()
        {
            InitializeComponent();

            // Konfigurasi kiosk dari Phase 1: rebut kembali posisi teratas saat fokus hilang.
            // Kecuali saat dialog password sedang terbuka -> jangan curi fokusnya.
            Deactivated += (_, _) =>
            {
                if (_exitDialogOpen) return;
                Topmost = true;
                Activate();
            };

            // Mulai proses inisialisasi WebView2 segera setelah jendela dimuat.
            // "async void" pada event handler adalah pola yang lazim & aman di WPF.
            Loaded += async (_, _) =>
            {
                Activate();
                Focus();

                // Pasang keyboard hook (memblokir Alt+Tab, Win, Alt+F4, Ctrl+Esc).
                // Ctrl+Shift+Q kini memicu prompt password admin (bukan langsung tutup).
                _keyboardHook = new KeyboardHook(onExitRequested: RequestAdminExit);
                _keyboardHook.Install();

                await InitializeWebViewAsync();
            };

            // Phase 4: cegat upaya menutup. Selama belum disetujui admin, batalkan.
            Closing += (_, e) =>
            {
                if (!_allowClose)
                    e.Cancel = true;
            };

            // Saat jendela benar-benar ditutup, WAJIB lepas hook agar tidak terjadi memory leak / lag OS.
            Closed += (_, _) => _keyboardHook?.Dispose();
        }

        /// <summary>
        /// Menampilkan dialog password admin. Jika password benar, izinkan aplikasi keluar.
        /// </summary>
        private void RequestAdminExit()
        {
            if (_exitDialogOpen) return; // sudah ada dialog yang terbuka
            _exitDialogOpen = true;
            try
            {
                var dialog = new PasswordDialog { Owner = this };
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    _allowClose = true; // password sudah diverifikasi benar di dalam dialog
                    Close();
                }
            }
            finally
            {
                _exitDialogOpen = false;
            }
        }

        /// <summary>
        /// Menyiapkan WebView2: cek Runtime, inisialisasi, atur keamanan, lalu navigasi.
        /// </summary>
        private async Task InitializeWebViewAsync()
        {
            // --- 1. Deteksi otomatis WebView2 Runtime ---------------------------------
            // GetAvailableBrowserVersionString() melempar exception / mengembalikan null
            // jika Runtime belum terpasang di laptop. Kita tangkap untuk beri pesan jelas.
            try
            {
                string? version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrEmpty(version))
                {
                    ShowRuntimeMissingMessage();
                    return;
                }
            }
            catch (WebView2RuntimeNotFoundException)
            {
                ShowRuntimeMissingMessage();
                return;
            }

            // --- 2. Inisialisasi inti WebView2 (asynchronous) -------------------------
            // EnsureCoreWebView2Async WAJIB di-await sebelum mengakses properti CoreWebView2.
            try
            {
                await WebView.EnsureCoreWebView2Async();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Gagal memuat komponen browser.\n\n" + ex.Message;
                return;
            }

            // --- 3. Konfigurasi keamanan browser -------------------------------------
            var settings = WebView.CoreWebView2.Settings;

            // Matikan menu klik-kanan bawaan (Reload, Save As, Inspect, dll).
            settings.AreDefaultContextMenusEnabled = false;

            // Matikan DevTools sepenuhnya (F12 / Ctrl+Shift+I tidak akan membuka inspector).
            settings.AreDevToolsEnabled = false;

            // Matikan kontrol zoom (Ctrl+'+' / Ctrl+'-' / Ctrl+scroll).
            settings.IsZoomControlEnabled = false;

            // Matikan tombol pintas akselerator browser (mis. Ctrl+P print, Ctrl+F find bawaan).
            settings.AreBrowserAcceleratorKeysEnabled = false;

            // Matikan status bar (teks URL kecil di pojok kiri-bawah saat hover link).
            settings.IsStatusBarEnabled = false;

            // --- 4. Sembunyikan overlay status saat halaman selesai dimuat -----------
            WebView.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (args.IsSuccess)
                {
                    StatusText.Visibility = Visibility.Collapsed;
                    WebView.Visibility = Visibility.Visible;
                }
                else
                {
                    StatusText.Text =
                        "Gagal terhubung ke server ujian.\n" +
                        "Periksa koneksi internet, lalu coba lagi.";
                    WebView.Visibility = Visibility.Collapsed;
                    StatusText.Visibility = Visibility.Visible;
                }
            };

            // --- 5. Tentukan URL & navigasi ------------------------------------------
            string url = ReadExamUrl();
            WebView.CoreWebView2.Navigate(url);
        }

        /// <summary>
        /// Membaca URL ujian dari config.txt yang berada di samping file .exe.
        /// Mengambil baris pertama yang bukan komentar (#) dan tidak kosong.
        /// Jika file tidak ada / kosong, kembalikan DefaultUrl.
        /// </summary>
        private static string ReadExamUrl()
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.txt");
                if (!File.Exists(configPath))
                    return DefaultUrl;

                foreach (string raw in File.ReadAllLines(configPath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue; // lewati baris kosong & komentar

                    return line; // baris valid pertama = URL ujian
                }
            }
            catch
            {
                // Abaikan error baca file; pakai URL cadangan agar aplikasi tetap jalan.
            }

            return DefaultUrl;
        }

        /// <summary>
        /// Menampilkan pesan jelas ketika WebView2 Runtime belum terpasang,
        /// menggantikan layar blank yang membingungkan.
        /// </summary>
        private void ShowRuntimeMissingMessage()
        {
            WebView.Visibility = Visibility.Collapsed;
            StatusText.Visibility = Visibility.Visible;
            StatusText.Text =
                "Komponen 'Microsoft Edge WebView2 Runtime' belum terpasang di laptop ini.\n\n" +
                "Mohon hubungi pengawas/operator untuk memasangnya terlebih dahulu, " +
                "lalu jalankan kembali aplikasi ujian.";
        }
    }
}

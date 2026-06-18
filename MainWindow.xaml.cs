using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace SimpleUjianBrowser
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

        // URL "exit" opsional dari config.txt. Jika website ujian menavigasi ke URL ini
        // (mis. setelah siswa submit), aplikasi keluar otomatis TANPA minta password.
        // null/kosong = fitur dimatikan.
        private string? _exitUrl;

        // Timer toolbar: memperbarui jam & info baterai setiap detik.
        private DispatcherTimer? _statusTimer;

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

                // Mulai timer toolbar: perbarui jam & baterai tiap detik.
                _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _statusTimer.Tick += (_, _) => UpdateStatusBar();
                _statusTimer.Start();
                UpdateStatusBar(); // tampilkan langsung tanpa menunggu 1 detik

                await InitializeWebViewAsync();
            };

            // Phase 4: cegat upaya menutup. Selama belum disetujui admin, batalkan.
            Closing += (_, e) =>
            {
                if (!_allowClose)
                    e.Cancel = true;
            };

            // Saat jendela benar-benar ditutup, WAJIB lepas hook agar tidak terjadi memory leak / lag OS.
            Closed += (_, _) =>
            {
                _statusTimer?.Stop();
                _keyboardHook?.Dispose();
            };
        }

        /// <summary>
        /// Memperbarui teks jam & baterai di toolbar. Dipanggil dari _statusTimer.
        /// </summary>
        private void UpdateStatusBar()
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            BatteryText.Text = BatteryStatus.GetDisplayText();
        }

        /// <summary>
        /// Handler tombol "Muat ulang": memuat ulang halaman ujian saat ini.
        /// Aman jika CoreWebView2 belum siap (operator menekan terlalu dini).
        /// </summary>
        private void ReloadButton_Click(object sender, RoutedEventArgs e)
            => WebView.CoreWebView2?.Reload();

        /// <summary>
        /// Handler tombol "Keluar" di pojok kanan-atas. Memakai jalur keluar yang
        /// SAMA dengan Ctrl+Shift+Q, jadi password admin tetap wajib dimasukkan.
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e) => RequestAdminExit();

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
            ExamConfig config = ReadConfig();
            _exitUrl = config.ExitUrl;

            // Jika exit_url diset, pantau setiap awal navigasi untuk keluar otomatis.
            if (!string.IsNullOrEmpty(_exitUrl))
                WebView.CoreWebView2.NavigationStarting += OnNavigationStarting;

            WebView.CoreWebView2.Navigate(config.ExamUrl);
        }

        /// <summary>
        /// Dipanggil setiap kali halaman akan bernavigasi. Jika tujuannya cocok dengan
        /// URL exit dari config (mis. website ujian mengarahkan ke sana setelah submit),
        /// batalkan navigasi tersebut lalu tutup aplikasi TANPA meminta password admin.
        /// Pencocokan memakai awalan (prefix) agar query string tambahan tetap terdeteksi.
        /// </summary>
        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (string.IsNullOrEmpty(_exitUrl))
                return;

            if (e.Uri.StartsWith(_exitUrl, StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;     // jangan render halaman sentinel
                _allowClose = true;  // izinkan Closing tanpa dialog password
                // Tutup setelah event handler selesai, bukan di tengah navigasi.
                Dispatcher.BeginInvoke(new Action(Close));
            }
        }

        /// <summary>
        /// Hasil pembacaan config.txt: URL ujian (wajib) + URL exit opsional.
        /// </summary>
        private sealed record ExamConfig(string ExamUrl, string? ExitUrl);

        /// <summary>
        /// Membaca config.txt yang berada di samping file .exe.
        /// Aturan per baris (baris '#' atau kosong diabaikan):
        ///   - "exit_url=&lt;url&gt;"  -> URL exit opsional (keluar otomatis tanpa password).
        ///   - baris valid pertama lainnya -> URL ujian.
        /// Jika file tidak ada / tidak ada URL ujian, pakai DefaultUrl.
        /// </summary>
        private static ExamConfig ReadConfig()
        {
            string? examUrl = null;
            string? exitUrl = null;

            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.txt");
                if (File.Exists(configPath))
                {
                    foreach (string raw in File.ReadAllLines(configPath))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#"))
                            continue; // lewati baris kosong & komentar

                        const string exitPrefix = "exit_url=";
                        if (line.StartsWith(exitPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string value = line.Substring(exitPrefix.Length).Trim();
                            if (value.Length > 0)
                                exitUrl = value;
                            continue;
                        }

                        // Baris valid pertama yang bukan setting = URL ujian.
                        examUrl ??= line;
                    }
                }
            }
            catch
            {
                // Abaikan error baca file; pakai URL cadangan agar aplikasi tetap jalan.
            }

            return new ExamConfig(examUrl ?? DefaultUrl, exitUrl);
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

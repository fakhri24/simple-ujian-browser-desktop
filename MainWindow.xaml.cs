using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Code-behind untuk MainWindow.
    /// Mengambil konfigurasi (URL ujian, URL exit, password admin) dari config.json
    /// di Firebase Hosting saat start, lalu menerapkan lockdown + keamanan browser.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Keyboard hook untuk memblokir tombol sistem (Phase 3).
        private KeyboardHook? _keyboardHook;

        // Penanda bahwa keluar sudah disetujui. Selama false, setiap upaya menutup dibatalkan.
        private bool _allowClose;

        // Mencegah dialog password terbuka berkali-kali.
        private bool _exitDialogOpen;

        // URL "exit" opsional dari config. Jika website ujian menavigasi ke sini,
        // aplikasi keluar otomatis TANPA password. null/kosong = fitur dimatikan.
        private string? _exitUrl;

        // Timer toolbar: memperbarui jam & info baterai setiap detik.
        private DispatcherTimer? _statusTimer;

        // Verifikator password admin. Default: password bawaan (cadangan offline),
        // diganti oleh konfigurasi dari Firebase setelah berhasil diambil.
        private Func<string, bool> _adminVerifier = DefaultVerify;

        public MainWindow()
        {
            InitializeComponent();

            // Rebut kembali posisi teratas saat fokus hilang, kecuali saat dialog password terbuka.
            Deactivated += (_, _) =>
            {
                if (_exitDialogOpen) return;
                Topmost = true;
                Activate();
            };

            Loaded += async (_, _) =>
            {
                Activate();
                Focus();

                // Pasang keyboard hook (blokir Alt+Tab, Win, Alt+F4, Ctrl+Esc).
                // Ctrl+Shift+Q memicu prompt password admin.
                _keyboardHook = new KeyboardHook(onExitRequested: RequestAdminExit);
                _keyboardHook.Install();

                // Mulai timer toolbar: perbarui jam & baterai tiap detik.
                _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _statusTimer.Tick += (_, _) => UpdateStatusBar();
                _statusTimer.Start();
                UpdateStatusBar();

                await InitializeWebViewAsync();
            };

            // Cegat upaya menutup. Selama belum disetujui, batalkan.
            Closing += (_, e) =>
            {
                if (!_allowClose)
                    e.Cancel = true;
            };

            // Saat ditutup, WAJIB lepas hook & hentikan timer.
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
        /// </summary>
        private void ReloadButton_Click(object sender, RoutedEventArgs e)
            => WebView.CoreWebView2?.Reload();

        /// <summary>
        /// Handler tombol "Keluar": memakai jalur keluar yang SAMA dengan
        /// Ctrl+Shift+Q, jadi password admin tetap wajib dimasukkan.
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e) => RequestAdminExit();

        /// <summary>
        /// Menampilkan dialog password admin. Jika password benar, izinkan keluar.
        /// </summary>
        private void RequestAdminExit()
        {
            if (_exitDialogOpen) return;
            _exitDialogOpen = true;
            try
            {
                var dialog = new PasswordDialog(_adminVerifier) { Owner = this };
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    _allowClose = true;
                    Close();
                }
            }
            finally
            {
                _exitDialogOpen = false;
            }
        }

        /// <summary>
        /// Menyiapkan WebView2: cek Runtime, inisialisasi, atur keamanan,
        /// lalu ambil konfigurasi & navigasi.
        /// </summary>
        private async Task InitializeWebViewAsync()
        {
            // --- 1. Deteksi WebView2 Runtime -----------------------------------------
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

            // --- 2. Inisialisasi inti WebView2 ---------------------------------------
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
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.IsStatusBarEnabled = false;

            // --- 3b. Marker "Simple Ujian Browser" -----------------------------------
            // Agar web app mengenali bahwa halaman dibuka di dalam SUB (bukan browser
            // biasa) dan menerapkan alur lockdown (mis. logout -> navigasi ke exitUrl
            // yang memicu auto-quit di OnNavigationStarting). Lihat js/lockdown.js di
            // repo web (deteksi via UA token ATAU window.SimpleUjianBrowser).
            //
            // Bersifat BEST-EFFORT: dibungkus try/catch agar kegagalan menyetel marker
            // (mis. properti tak didukung di runtime tertentu) TIDAK menghentikan
            // pemuatan konfigurasi & navigasi di langkah berikutnya.
            try
            {
                settings.UserAgent += " SimpleUjianBrowser/1.0";
                await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    "window.SimpleUjianBrowser = { version: '1.0', platform: 'webview2' };");
            }
            catch
            {
                // Marker gagal dipasang -> abaikan; aplikasi tetap lanjut memuat ujian.
            }

            // --- 4. Sembunyikan overlay saat halaman selesai dimuat ------------------
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

            // Pantau navigasi untuk exit otomatis (handler memeriksa _exitUrl sendiri).
            WebView.CoreWebView2.NavigationStarting += OnNavigationStarting;

            // --- 5. Ambil konfigurasi dari Firebase, lalu navigasi -------------------
            await LoadConfigAndNavigateAsync();
        }

        /// <summary>
        /// Mengambil config.json dari Firebase Hosting lalu menerapkannya: URL ujian,
        /// URL exit, dan verifikator password admin. Jika gagal (mis. tidak ada internet),
        /// tampilkan pesan + tombol "Coba lagi" — tanpa fallback diam-diam.
        /// </summary>
        private async Task LoadConfigAndNavigateAsync()
        {
            RetryButton.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Collapsed;
            StatusText.Visibility = Visibility.Visible;
            StatusText.Text = "Memuat konfigurasi ujian...";

            try
            {
                AppConfig config = await ConfigService.FetchAsync(ConfigService.ResolveConfigUrl());

                _exitUrl = config.ExitUrl;
                _adminVerifier = BuildVerifier(config);

                StatusText.Text = "Menyiapkan ujian...";
                WebView.CoreWebView2.Navigate(config.ExamUrl!); // dijamin non-null oleh FetchAsync
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Gagal memuat konfigurasi ujian.\n" +
                    "Pastikan laptop terhubung internet, lalu tekan \"Coba lagi\".\n\n" +
                    "(" + ex.Message + ")";
                RetryButton.Visibility = Visibility.Visible;
            }
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
            => await LoadConfigAndNavigateAsync();

        /// <summary>
        /// Jika website ujian menavigasi ke URL exit, batalkan navigasi tersebut
        /// dan tutup aplikasi TANPA meminta password. Pencocokan berbasis awalan (prefix).
        /// </summary>
        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (string.IsNullOrEmpty(_exitUrl))
                return;

            if (e.Uri.StartsWith(_exitUrl, StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;     // jangan render halaman sentinel
                _allowClose = true;  // izinkan Closing tanpa dialog password
                Dispatcher.BeginInvoke(new Action(Close));
            }
        }

        /// <summary>
        /// Membuat fungsi verifikasi password dari config. Jika config memuat
        /// adminPassword (PBKDF2) yang lengkap, pakai itu; jika tidak, pakai password bawaan.
        /// </summary>
        private static Func<string, bool> BuildVerifier(AppConfig config)
        {
            PasswordSpec? spec = config.AdminPassword;
            if (spec is not null &&
                !string.IsNullOrEmpty(spec.Salt) &&
                !string.IsNullOrEmpty(spec.Hash) &&
                spec.Iterations > 0)
            {
                return pwd => PasswordHasher.Verify(pwd, spec.Salt!, spec.Hash!, spec.Iterations);
            }
            return DefaultVerify;
        }

        // Password bawaan (cadangan) bila config belum memuat adminPassword.
        private static bool DefaultVerify(string password) => password == "Admin123!";

        /// <summary>
        /// Menampilkan pesan jelas ketika WebView2 Runtime belum terpasang.
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

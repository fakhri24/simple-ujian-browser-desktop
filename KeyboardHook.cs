using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace SecureExamBrowser
{
    /// <summary>
    /// Memasang Low-Level Keyboard Hook (WH_KEYBOARD_LL) lewat Win32 API.
    /// Hook ini menangkap SETIAP penekanan tombol di seluruh sistem SEBELUM
    /// sampai ke Windows, sehingga kita bisa "menelan" (memblokir) kombinasi
    /// berbahaya seperti Alt+Tab, Win, Alt+F4, dan Ctrl+Esc.
    ///
    /// Implementasi IDisposable agar hook dilepas (UnhookWindowsHookEx) saat
    /// aplikasi ditutup -> mencegah memory leak & lag pada OS.
    /// </summary>
    public sealed class KeyboardHook : IDisposable
    {
        // --- Konstanta Win32 ------------------------------------------------------
        private const int WH_KEYBOARD_LL = 13;     // Jenis hook: keyboard low-level.
        private const int WM_KEYDOWN = 0x0100;     // Pesan: tombol ditekan.
        private const int WM_SYSKEYDOWN = 0x0104;  // Pesan: tombol ditekan bersama Alt.
        private const uint LLKHF_ALTDOWN = 0x20;   // Flag: tombol Alt sedang ditekan.

        // --- Virtual-Key codes (kode tombol) -------------------------------------
        private const int VK_TAB = 0x09;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_F4 = 0x73;
        private const int VK_Q = 0x51;

        // Handle ke hook yang terpasang (dipakai saat melepas hook).
        private IntPtr _hookId = IntPtr.Zero;

        // Simpan delegate sebagai field agar TIDAK dikumpulkan Garbage Collector.
        // Jika delegate ter-GC saat hook masih aktif -> aplikasi crash.
        private readonly LowLevelKeyboardProc _proc;

        // Aksi yang dipanggil saat hotkey keluar (Ctrl+Shift+Q) ditekan.
        private readonly Action _onExitRequested;

        public KeyboardHook(Action onExitRequested)
        {
            _onExitRequested = onExitRequested;
            _proc = HookCallback; // ikat callback ke field permanen
        }

        /// <summary>Memasang hook ke sistem.</summary>
        public void Install()
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule!;
            _hookId = SetWindowsHookEx(
                WH_KEYBOARD_LL,
                _proc,
                GetModuleHandle(curModule.ModuleName),
                0);
        }

        /// <summary>
        /// Fungsi yang dipanggil Windows untuk SETIAP event keyboard.
        /// Mengembalikan (IntPtr)1 = "telan" tombol (blokir).
        /// Memanggil CallNextHookEx = teruskan tombol ke aplikasi lain (izinkan).
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    int vk = (int)data.vkCode;

                    // Status tombol modifier saat ini.
                    bool alt = (data.flags & LLKHF_ALTDOWN) != 0;
                    bool ctrl = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                    bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

                    // ===================================================================
                    // Hotkey KELUAR admin: Ctrl + Shift + Q
                    // Memicu prompt password admin (lihat MainWindow.RequestAdminExit).
                    // Aplikasi hanya benar-benar keluar bila password 'Admin123!' benar.
                    // ===================================================================
                    if (ctrl && shift && vk == VK_Q)
                    {
                        // Jalankan aksi keluar di UI thread.
                        Application.Current?.Dispatcher.Invoke(_onExitRequested);
                        return (IntPtr)1; // telan supaya 'Q' tidak ikut terketik
                    }
                    // ===================================================================

                    // --- Daftar kombinasi yang DIBLOKIR ---
                    bool block =
                        vk == VK_LWIN || vk == VK_RWIN ||   // Win key (Start Menu)
                        (alt && vk == VK_TAB) ||            // Alt+Tab (pindah app)
                        (alt && vk == VK_F4) ||             // Alt+F4 (paksa tutup)
                        (ctrl && vk == VK_ESCAPE);          // Ctrl+Esc (Start Menu)

                    if (block)
                        return (IntPtr)1; // telan -> tombol tidak diteruskan ke OS
                }
            }

            // Selain yang diblok, teruskan normal ke hook/aplikasi berikutnya.
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // --- Pembersihan: lepas hook --------------------------------------------
        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        // --- Struktur & deklarasi P/Invoke (jembatan ke Win32 user32.dll) -------
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

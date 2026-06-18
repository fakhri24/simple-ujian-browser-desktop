using System.Runtime.InteropServices;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Helper untuk membaca kondisi baterai laptop lewat Win32 API
    /// <c>GetSystemPowerStatus</c> (kernel32.dll), tanpa perlu menambah
    /// referensi Windows Forms. Aman dipanggil berulang dari timer UI.
    /// </summary>
    internal static class BatteryStatus
    {
        // Struktur data yang diisi oleh Windows: cerminan SYSTEM_POWER_STATUS.
        // Urutan & tipe field WAJIB sama persis dengan definisi Win32.
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;        // 0 = pakai baterai, 1 = colok listrik, 255 = tidak diketahui
            public byte BatteryFlag;         // bitmask; 128 = tidak ada baterai sistem (PC desktop)
            public byte BatteryLifePercent;  // 0-100, atau 255 jika tidak diketahui
            public byte SystemStatusFlag;    // tidak dipakai di sini
            public int BatteryLifeTime;      // detik tersisa, atau -1
            public int BatteryFullLifeTime;  // tidak dipakai di sini
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

        /// <summary>
        /// Mengembalikan teks ringkas kondisi baterai, mis. "85%" atau
        /// "85% (mengisi)". Jika tidak ada baterai (PC desktop) -> "AC";
        /// jika gagal membaca -> "Baterai -".
        /// </summary>
        public static string GetDisplayText()
        {
            if (!GetSystemPowerStatus(out SYSTEM_POWER_STATUS s))
                return "Baterai -";

            const byte NoSystemBattery = 128; // bit penanda "tidak ada baterai"
            if ((s.BatteryFlag & NoSystemBattery) != 0)
                return "AC";

            string percent = s.BatteryLifePercent == 255
                ? "?"
                : s.BatteryLifePercent + "%";

            bool charging = s.ACLineStatus == 1; // terhubung ke listrik
            return charging ? percent + " (mengisi)" : percent;
        }
    }
}

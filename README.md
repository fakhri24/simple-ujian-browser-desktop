# SimpleUjianBrowser

Lockdown browser ringan untuk Windows yang membungkus platform ujian berbasis web.
Aplikasi ini berjalan dalam mode kiosk fullscreen dan mencegah siswa berpindah
aplikasi, membuka aplikasi lain, atau memakai pintasan keyboard tak sah selama ujian.

Dibangun dengan **C# / .NET 8 (WPF)** + **Microsoft Edge WebView2**, lalu didistribusikan
sebagai **satu file `.exe` self-contained** (runtime .NET ikut dibundel, jadi laptop
siswa tidak perlu menginstal .NET).

---

## Fitur Utama

- **Mode kiosk fullscreen** — `WindowStyle=None`, `Maximized`, `Topmost`, tanpa resize.
- **WebView2 terkunci** — klik kanan, DevTools (`F12` / `Ctrl+Shift+I`), dan zoom dinonaktifkan.
- **Blokir pintasan sistem** via low-level keyboard hook: `Alt+Tab`, `Win`, `Alt+F4`, `Ctrl+Esc`.
- **Konfigurasi terpusat via server** — URL ujian, URL exit, dan password admin diambil
  dari `config.json` di Firebase Hosting saat start. Ganti pengaturan = edit 1 file di
  server, semua laptop ikut, tanpa membagikan ulang `.exe`.
- **Keluar butuh password admin** melalui dialog modal — lewat tombol `Keluar` di pojok
  kanan-atas atau pintasan `Ctrl+Shift+Q`. Password diverifikasi dengan **hash PBKDF2**
  (bukan teks polos).
- **Exit otomatis via URL** — jika website ujian redirect ke `exitUrl`, aplikasi keluar
  otomatis tanpa password (mis. setelah submit).
- **Toolbar kanan-atas** — info baterai, jam berjalan, dan tombol `Muat ulang` halaman,
  di samping tombol `Keluar`.

> ⚠️ **Catatan keamanan (BYOD):** pada laptop milik siswa, lockdown ini bersifat
> **penghalang (deterrence)**, bukan jaminan — `Ctrl+Alt+Del` tidak bisa diblok dan
> rahasia di klien bisa diekstrak. Untuk keamanan kuat, andalkan deteksi sisi server.
> Lihat [plan/PLAN2.md](plan/PLAN2.md).

---

## Konfigurasi

Pengaturan disimpan di **`config.json`** yang di-host di Firebase Hosting (bawaan:
`https://simple-ujian.web.app/lockdown-config.json`):

```json
{
  "version": 1,
  "examUrl": "https://simple-ujian.web.app/",
  "exitUrl": "https://simple-ujian.web.app/selesai",
  "adminPassword": {
    "algo": "PBKDF2-SHA256",
    "iterations": 100000,
    "salt": "<base64>",
    "hash": "<base64>"
  }
}
```

- `examUrl` (wajib) — halaman ujian yang dibuka.
- `exitUrl` (opsional) — jika website menavigasi ke sini, aplikasi keluar otomatis
  tanpa password. Pencocokan berdasarkan awalan.
- `adminPassword` (opsional) — salt + hash PBKDF2 untuk password keluar. Jika tidak ada,
  aplikasi memakai password bawaan `Admin123!`.

**Mengganti password admin:** jalankan `SimpleUjianBrowser.exe --make-hash`, ketik
password baru, salin blok `adminPassword` yang dihasilkan, lalu tempel ke `config.json`
di Firebase. Karena `config.json` bersifat publik, **gunakan password yang kuat.**

**File `config.txt`** (opsional, di samping `.exe`) kini hanya untuk **mengganti alamat
`config.json`** saat uji coba/staging — bukan menyimpan pengaturan.

> Aplikasi **wajib online** saat start untuk mengambil `config.json`. Jika gagal, akan
> tampil pesan + tombol **Coba lagi**.

---

## Build

> Build & uji dilakukan di **Windows** (PowerShell) dengan **.NET 8 SDK**.
> Pengembangan kode bisa di macOS/VS Code, tetapi WPF tidak dapat dijalankan di macOS.

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

Hasil: `bin\Release\net8.0-windows\win-x64\publish\SimpleUjianBrowser.exe`

Panduan build & deployment lengkap ada di [BUILD.md](BUILD.md).

---

## Cara Keluar

- Klik tombol **`Keluar`** di pojok kanan-atas → masukkan password admin.
- Alternatif: tekan **`Ctrl + Shift + Q`** → masukkan password admin yang sama.
- **Otomatis (tanpa password):** website ujian redirect ke `exitUrl` (lihat Konfigurasi).
- Jaring pengaman darurat: **`Ctrl + Alt + Del` → Sign out**.

---

## Dokumentasi Lain

- [BUILD.md](BUILD.md) — panduan build & distribusi ke laptop siswa.
- [AGENTS.md](AGENTS.md) — instruksi & konteks teknis untuk AI agent.
- [plan/PLAN.md](plan/PLAN.md) — rencana pengembangan.

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
- **URL ujian dapat dikonfigurasi** lewat `config.txt` tanpa perlu build ulang.
- **Keluar butuh password admin** melalui dialog modal (default: `Admin123!`).

---

## Konfigurasi

Edit `config.txt` (diletakkan di samping `.exe`):

- Baris diawali `#` adalah komentar.
- Baris pertama yang bukan komentar & tidak kosong dipakai sebagai URL ujian.
- Jika `config.txt` tidak ada, aplikasi memakai URL bawaan: `https://simple-ujian.web.app/`.

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

- Tekan **`Ctrl + Shift + Q`** → masukkan password admin (`Admin123!`).
- Jaring pengaman darurat: **`Ctrl + Alt + Del` → Sign out**.

---

## Dokumentasi Lain

- [BUILD.md](BUILD.md) — panduan build & distribusi ke laptop siswa.
- [AGENTS.md](AGENTS.md) — instruksi & konteks teknis untuk AI agent.
- [plan/PLAN.md](plan/PLAN.md) — rencana pengembangan.

# Panduan Build & Deployment (Phase 5)

Dokumen ini menjelaskan cara meng-compile Simple Ujian Browser menjadi **satu
file `.exe` self-contained** yang bisa langsung dijalankan di laptop siswa
**tanpa perlu menginstal .NET** terlebih dahulu.

> Semua perintah dijalankan di **Windows** (PowerShell), di dalam folder proyek.
> Mesin build perlu **.NET 8 SDK**. Laptop siswa **tidak** perlu apa pun.

---

## 1. Perintah Build (Self-Contained Single-File)

Jalankan di PowerShell, dari dalam folder `sub`:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

### Arti tiap bagian

| Argumen | Fungsi |
| :------ | :----- |
| `-c Release` | Build mode rilis (teroptimasi, bukan Debug). |
| `-r win-x64` | Target Windows 64-bit. |
| `--self-contained true` | **Bundel .NET 8 runtime ke dalam .exe** -> laptop tujuan tak perlu install .NET. |
| `/p:PublishSingleFile=true` | Gabungkan semua DLL jadi satu file `.exe`. |
| `/p:IncludeNativeLibrariesForSelfExtract=true` | Ikutkan library native (mis. `WebView2Loader.dll`) ke dalam bundle. |
| `/p:EnableCompressionInSingleFile=true` | Kompres isi bundle -> ukuran file lebih kecil. |

> ⚠️ **Jangan** menambahkan `/p:PublishTrimmed=true`. Trimming TIDAK didukung
> penuh oleh WPF dan bisa membuat aplikasi crash saat runtime.

---

## 2. Lokasi Hasil Build

Setelah selesai, file ada di:

```
bin\Release\net8.0-windows\win-x64\publish\
```

Isi folder itu:

- `SimpleUjianBrowser.exe`  <- aplikasi utama (single-file, ~70-150 MB)
- `config.txt`             <- URL ujian yang bisa diedit

---

## 3. Cara Distribusi ke Laptop Siswa

1. Salin **`SimpleUjianBrowser.exe`** ke laptop siswa.
2. (Opsional) Salin juga **`config.txt`** di folder yang sama bila ingin
   mengganti URL ujian tanpa build ulang. Jika `config.txt` tidak ada,
   aplikasi otomatis memakai URL bawaan (`https://simple-ujian.web.app/`).
3. Dobel-klik `SimpleUjianBrowser.exe` untuk menjalankan ujian.

> Syarat di laptop siswa: hanya **WebView2 Runtime** (umumnya sudah ada di
> Windows 10/11 modern). Jika belum ada, aplikasi menampilkan pesan jelas
> dan tidak akan blank.

---

## 4. Keluar dari Aplikasi

- Tekan **`Ctrl + Shift + Q`** -> masukkan password admin **`Admin123!`**.
- Jaring pengaman darurat: **`Ctrl + Alt + Del` -> Sign out**.

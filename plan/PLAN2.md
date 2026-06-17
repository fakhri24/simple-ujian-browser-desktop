# Development Plan 2 (OPSIONAL): Pengerasan Keamanan Lanjutan

> ⚠️ **STATUS: USULAN — BELUM DISETUJUI UNTUK DIKERJAKAN.**
> Dokumen ini berisi langkah-langkah pengerasan tambahan **di luar** PLAN.md
> (Fase 1-5 yang sudah selesai). Setiap langkah bersifat **independen** dan
> akan **diaudit lebih dulu** sebelum diputuskan dikerjakan atau tidak.
> Mengerjakan salah satu TIDAK mewajibkan mengerjakan yang lain.

---

## 📋 Ringkasan Roadmap Opsional

| Langkah   | Judul                          | Tujuan Inti                                              | Tingkat Risiko/Effort | Lokasi Implementasi        |
| :-------- | :----------------------------- | :------------------------------------------------------ | :-------------------- | :------------------------- |
| **Opt-1** | Pengerasan OS (Ctrl+Alt+Del)   | Batasi Sign out / Task Manager / Switch User.           | Tinggi (ubah OS)      | Group Policy / Registry    |
| **Opt-2** | Password Admin via Config      | Ganti password tanpa build ulang, disimpan ter-hash.    | Sedang                | `config.txt` + kode C#     |
| **Opt-3** | Logging & Audit                | Catat upaya keluar & password salah untuk pengawas.     | Rendah                | Kode C# (file log)         |
| **Opt-4** | Deteksi Multi-Monitor & Fokus  | Cegah layar kedua / hilang fokus saat ujian.            | Sedang-Tinggi         | Kode C# + WMI/Win32        |

---

## 🔍 Detail Tiap Langkah

### 🟧 Opt-1: Pengerasan OS — Membatasi Ctrl+Alt+Del

- **Masalah:** `Ctrl+Alt+Del` adalah Secure Attention Sequence level OS yang TIDAK
  bisa diblok dari dalam aplikasi. Dari sana siswa masih bisa **Sign out**,
  **Switch User**, **Lock**, atau membuka **Task Manager**.
- **Tujuan:** Menonaktifkan opsi-opsi tersebut lewat kebijakan Windows, bukan kode aplikasi.
- **Pendekatan (pilih salah satu):**
  - **A. Group Policy Editor (`gpedit.msc`)** — hanya tersedia di Windows Pro/Edu/Enterprise.
    - `User Configuration > Administrative Templates > System > Ctrl+Alt+Del Options`:
      - "Remove Task Manager" -> Enabled
      - "Remove Lock Computer" -> Enabled
      - "Remove Change Password" -> Enabled
      - "Remove Logoff" -> Enabled
  - **B. Registry** (untuk Windows Home yang tak punya gpedit) — di bawah
    `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System`:
    - `DisableTaskMgr = 1`
    - `DisableLockWorkstation = 1`
    - `DisableChangePassword = 1`
    - `NoLogoff = 1`
- **Langkah kerja:**
  1. Buat skrip `.reg` atau `.bat` terpisah (bukan bagian dari .exe utama).
  2. Jalankan sebagai Administrator **sebelum** ujian dimulai.
  3. Siapkan skrip kebalikan (restore) untuk **mengembalikan** setting setelah ujian.
- **Risiko & Catatan:**
  - Mengubah OS lebih invasif; **wajib** ada skrip restore agar laptop normal kembali.
  - Perlu hak Administrator di tiap laptop.
  - Uji di 1 laptop dulu sebelum massal.
- **Kriteria audit:** Apakah institusi mengizinkan ubah Group Policy/Registry pada
  laptop? Apakah laptop milik sekolah (seragam) atau milik siswa (beragam)?

---

### 🟧 Opt-2: Password Admin via `config.txt` (Ter-hash)

- **Masalah:** Saat ini password `Admin123!` di-*hardcode* di `PasswordDialog.xaml.cs`.
  Mengganti password berarti harus build ulang `.exe`.
- **Tujuan:** Operator bisa mengganti password admin tanpa build ulang, dan password
  TIDAK tersimpan sebagai teks polos.
- **Pendekatan:**
  1. Tambah baris konfigurasi di `config.txt`, mis. berformat `admin_hash=<SHA256 hex>`.
  2. Saat verifikasi, aplikasi meng-hash input pengguna (SHA-256) lalu membandingkan
     dengan hash dari config — bukan membandingkan teks polos.
  3. Sediakan utilitas kecil (atau perintah PowerShell) untuk menghasilkan hash dari
     password baru, lalu operator menempelkannya ke `config.txt`.
- **Langkah kerja:**
  - Perluas `ReadExamUrl()` menjadi parser config yang membaca beberapa kunci (URL + hash).
  - Ganti perbandingan `== AdminPassword` di `PasswordDialog` dengan perbandingan hash.
  - Fallback: jika `admin_hash` tidak ada di config, pakai hash default bawaan.
- **Risiko & Catatan:**
  - SHA-256 polos masih rentan brute-force untuk password lemah; pertimbangkan salt
    atau algoritma lebih kuat (PBKDF2) bila perlu.
  - Jangan menaruh password polos di repo/commit.
- **Kriteria audit:** Seberapa sering password perlu diganti? Apakah perlu per-sekolah
  berbeda? Apakah model ancamannya mencakup siswa yang membaca `config.txt`?

---

### 🟧 Opt-3: Logging & Audit

- **Tujuan:** Memberi pengawas jejak aktivitas penting untuk deteksi kecurangan/insiden.
- **Yang dicatat (usulan):**
  - Waktu aplikasi mulai & URL ujian yang dimuat.
  - Setiap pemicu `Ctrl+Shift+Q` (permintaan keluar).
  - Setiap percobaan password admin **salah** (beserta timestamp).
  - Keluar berhasil (password benar) + timestamp.
  - Opsional: kehilangan fokus / percobaan tombol terblokir (bisa "berisik").
- **Pendekatan:**
  1. Buat kelas `Logger` sederhana yang menulis ke file teks
     (mis. `logs\exam-YYYYMMDD.log`) di samping `.exe`.
  2. Tulis dengan format: `[timestamp] [level] pesan`.
  3. Pastikan penulisan aman-thread dan tidak menghambat UI (append ringan).
- **Langkah kerja:**
  - Sisipkan pemanggilan log di titik kunci: start, navigate, request exit,
    wrong password, exit success.
  - Pertimbangkan rotasi/penghapusan log lama agar tidak menumpuk.
- **Risiko & Catatan:**
  - **Privasi:** jangan pernah mencatat isi jawaban atau data pribadi siswa.
  - Log lokal bisa dihapus siswa jika punya akses file; untuk audit kuat, kirim ke server.
- **Kriteria audit:** Cukup log lokal, atau perlu terkirim ke server pusat? Berapa lama
  log disimpan? Siapa yang berhak membacanya?

---

### 🟧 Opt-4: Deteksi Multi-Monitor & Kehilangan Fokus

- **Tujuan:** Mencegah modus kecurangan memakai layar kedua atau memindahkan fokus
  ke aplikasi lain.
- **Sub-fitur (usulan):**
  - **a. Deteksi monitor ganda:** Jika terdeteksi >1 layar aktif, tampilkan peringatan
    atau blokir mulai ujian sampai layar kedua dicabut.
    - Implementasi: `System.Windows.Forms.Screen.AllScreens` atau Win32
      `EnumDisplayMonitors`.
  - **b. Pemantauan fokus:** Jika jendela ujian kehilangan fokus secara tak wajar,
    catat ke log (Opt-3) atau tampilkan peringatan.
    - Catatan: kode kita SUDAH merebut fokus via `Deactivated -> Activate()`;
      langkah ini menambah pencatatan/peringatan, bukan sekadar merebut.
  - **c. (Lanjutan, hati-hati) Deteksi screen-sharing / rekam layar:** sangat sulit &
    rawan false-positive; umumnya butuh pendekatan level kernel. **Tidak disarankan**
    tanpa kebutuhan kuat.
- **Risiko & Catatan:**
  - Multi-monitor sah dipakai sebagian siswa (mis. laptop + proyektor) -> butuh kebijakan jelas.
  - Fitur ini bisa menimbulkan false-positive dan keluhan; uji menyeluruh.
- **Kriteria audit:** Apakah laptop siswa memang berpotensi multi-monitor? Seberapa
  agresif respons yang diinginkan (peringatan vs blokir total)?

---

## 🤖 Catatan untuk AI Agent

1. **Jangan mengerjakan langkah mana pun dari dokumen ini sampai diminta eksplisit.**
2. Saat diaudit, bahas **satu langkah pada satu waktu**; konfirmasi keputusan
   (kerjakan / tunda / batalkan) sebelum menulis kode.
3. Untuk langkah yang mengubah OS (Opt-1), selalu siapkan **skrip restore** dan
   tegaskan kebutuhan hak Administrator.
4. Jaga konsistensi gaya: penjelasan beginner-friendly, perintah berbasis CLI,
   dan tidak merusak fungsi Fase 1-5 yang sudah berjalan.

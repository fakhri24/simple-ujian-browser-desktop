# Development Plan 2 (OPSIONAL): Pengerasan Keamanan Lanjutan

> ⚠️ **STATUS: SEBAGIAN BESAR MASIH USULAN.**
> Dokumen ini berisi langkah-langkah pengerasan tambahan **di luar** PLAN.md
> (Fase 1-5 yang sudah selesai). Kecuali **Opt-2** (sudah dipilih untuk dikerjakan),
> tiap langkah bersifat **independen** dan **diaudit lebih dulu** sebelum diputuskan.
> Mengerjakan salah satu TIDAK mewajibkan mengerjakan yang lain.

---

## 🎯 Realita & Model Ancaman (BYOD)

Penting dipahami sebelum membaca langkah-langkah di bawah, karena ini mengubah
apa yang **mungkin** dicapai:

- **Perangkat milik siswa (BYOD).** Siswa adalah **administrator mesinnya sendiri**.
  Tidak ada aplikasi *user-mode* (termasuk aplikasi ini) yang bisa benar-benar
  mengunci mesin yang **pemiliknya adalah lawannya**.
- **`Ctrl+Alt+Del` tidak bisa diblokir.** Ini *Secure Attention Sequence* yang
  ditangani Windows di level **di bawah** semua aplikasi (sengaja, demi keamanan).
  Low-level keyboard hook kita TIDAK akan pernah bisa mencegatnya. Dari layar itu
  siswa bisa membuka **Task Manager**, **Sign out**, **Switch user**, **Lock**.
- **`Topmost` BUKAN batas keamanan.** Ia hanya menaruh jendela di atas jendela
  *non-topmost*. Saat `Ctrl+Alt+Del` ditekan, layar pindah ke **secure desktop**
  yang berbeda — jendela kita bahkan tidak ada di sana. Begitu proses kita
  di-**End Task**, jendela Topmost-nya ikut lenyap; tidak ada yang "menutupi" apa pun.
  Trik `Deactivated -> Activate()` pun *best-effort* (dibatasi aturan foreground-lock),
  bukan jaminan.
- **Rahasia di klien bisa diekstrak.** `.exe` .NET sangat mudah di-*decompile*
  (dnSpy/ILSpy). Password yang di-*hardcode*, garam, kunci, semuanya terlihat.
  Self-contained single-file kita TIDAK ter-obfuscate.

**Konsekuensi strategis:** di BYOD, jangan bertaruh pada *"mencegah keluar"*
(mustahil dijamin). Bertaruhlah pada **deterrence** (menghalau 95% siswa yang
tidak akan repot membobol) **+ deteksi/pembuktian sisi server** (menangkap jejak
yang sisanya). Penegakan kuat hanya mungkin pada **mesin terkelola** (lihat Opt-1),
bukan BYOD.

---

## 📋 Ringkasan Roadmap Opsional

| Langkah   | Judul                              | Tujuan Inti                                              | Berlaku di BYOD?                | Status            |
| :-------- | :--------------------------------- | :------------------------------------------------------ | :------------------------------ | :---------------- |
| **Opt-1** | Pengerasan OS (Ctrl+Alt+Del)       | Batasi Sign out / Task Manager / Switch User.           | ❌ hanya mesin terkelola         | Usulan            |
| **Opt-2** | Config & Password via Firebase     | Ganti URL & password terpusat tanpa sebar ulang `.exe`. | ⚠️ deterrence (tetap berguna)   | **DIPILIH**       |
| **Opt-3** | Logging & Audit                    | Catat upaya keluar & password salah untuk pengawas.     | ⚠️ lemah jika lokal             | Usulan            |
| **Opt-4** | Deteksi Multi-Monitor & Fokus      | Cegah layar kedua / hilang fokus saat ujian.            | ⚠️ deterrence                   | Usulan            |
| **Opt-5** | Deteksi Sisi Server (heartbeat)    | Tandai sesi janggal; validasi submit dengan token.      | ✅ paling efektif di BYOD        | Usulan (disarankan) |

---

## 🔍 Detail Tiap Langkah

### 🟧 Opt-1: Pengerasan OS — Membatasi Ctrl+Alt+Del

- **Realita:** Seperti dijelaskan di atas, `Ctrl+Alt+Del` TIDAK bisa diblok dari
  aplikasi. Satu-satunya cara membatasi Task Manager / Sign out / Switch user /
  Lock adalah lewat kebijakan **level OS** — dan itu **hanya berlaku di mesin
  terkelola** (milik sekolah), **bukan BYOD**.
- **Pendekatan (hanya untuk mesin terkelola):**
  - **A. Group Policy (`gpedit.msc`)** — Windows Pro/Edu/Enterprise:
    `User Configuration > Administrative Templates > System > Ctrl+Alt+Del Options`
    → Remove Task Manager / Lock Computer / Change Password / Logoff = Enabled.
  - **B. Registry** (Windows Home) di
    `HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System`:
    `DisableTaskMgr=1`, `DisableLockWorkstation=1`, `DisableChangePassword=1`, `NoLogoff=1`.
  - **C. (Paling kuat) Assigned Access / Kiosk Mode / MDM** — Windows menjalankan
    aplikasi dalam mode kiosk sungguhan. Butuh provisioning perangkat.
- **Langkah kerja:** skrip `.reg`/`.bat` terpisah (bukan bagian `.exe`), dijalankan
  sebagai Administrator **sebelum** ujian, plus **skrip restore** untuk mengembalikan.
- **Kesimpulan untuk proyek ini:** Karena target kita **BYOD**, Opt-1 **tidak dapat
  ditegakkan** dan **bukan prioritas**. Dicatat di sini hanya untuk skenario lab/
  mesin sekolah di masa depan.

---

### 🟧 Opt-2: Konfigurasi & Password via Firebase Hosting — **DIPILIH**

- **Masalah yang dipecahkan:** (1) Password `Admin123!` saat ini di-*hardcode* →
  ganti = build & sebar ulang `.exe` besar; (2) URL ujian/exit di `config.txt`
  lokal → per-device, tidak terpusat.
- **Keputusan:** Pindahkan **tiga pengaturan** — URL awal, URL exit, password admin —
  ke sebuah `config.json` yang **di-host di Firebase Hosting** (infrastruktur yang
  SUDAH dipakai: `simple-ujian.web.app`). Aplikasi mengambilnya saat start.
  Ganti pengaturan = edit 1 file di Firebase → **semua laptop ikut berubah**, tanpa
  sebar file apa pun.
- **Penting (kejujuran keamanan):** Ini menyelesaikan **distribusi & kontrol terpusat**,
  BUKAN ketahanan terhadap pemilik mesin. Verifikasi password tetap di sisi klien,
  jadi tetap bisa di-*patch* lewat decompile. Levelnya **deterrence** — tetap sangat
  berguna untuk siswa kasual.
- **Bentuk data (usulan `config.json`):**
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
- **Verifikasi password:** Gunakan **hash satu-arah bergaram (PBKDF2)**, BUKAN enkripsi.
  Saat siswa/operator mengetik password, aplikasi meng-hash input dengan salt+iterasi
  yang sama lalu membandingkan dengan `hash`. Aplikasi TIDAK pernah menyimpan/membaca
  password polos.
- **Catatan:** `config.json` di Hosting bersifat **publik** (siapa pun yang tahu URL
  bisa membacanya). Karena itu password admin **wajib kuat** (hash publik bisa
  di-*brute-force* offline untuk password lemah). PBKDF2 iterasi tinggi memperlambat
  serangan, tapi tidak menggantikan password yang kuat.
- **Alur saat aplikasi start:**
  1. `HttpClient` fetch `config.json` (timeout pendek, mis. 5 detik).
  2. **Sukses** → pakai config itu, lalu simpan salinan sebagai *last-known-good* lokal.
  3. **Gagal/offline** → pakai *last-known-good* lokal; jika belum ada, pakai default
     bawaan (compiled-in) agar ujian tetap bisa jalan.
- **Membuat hash:** sediakan utilitas kecil (mode tersembunyi `--make-hash` di aplikasi,
  atau skrip PowerShell/Node) untuk menghasilkan `salt`+`hash` dari password baru, lalu
  operator menempelkannya ke `config.json` di Firebase.
- **Menu config dalam aplikasi (opsional):** karena sumber kebenaran kini di Firebase,
  menu cukup berupa **panel status (read-only)** ber-password yang menampilkan config
  efektif + tombol "muat ulang config". Pengeditan tetap dilakukan di Firebase.

---

### 🟧 Opt-3: Logging & Audit

- **Tujuan:** Memberi pengawas jejak aktivitas penting.
- **Yang dicatat (usulan):** waktu mulai & URL dimuat; tiap pemicu keluar
  (`Ctrl+Shift+Q`/tombol Keluar); tiap percobaan password **salah**; keluar berhasil;
  pemicu `exitUrl`.
- **Realita BYOD:** Log **lokal bisa dihapus** siswa. Agar berguna untuk audit,
  log sebaiknya **dikirim ke server** (lihat Opt-5). Log lokal hanya cocok untuk
  diagnosa, bukan bukti.
- **Privasi:** jangan pernah mencatat isi jawaban atau data pribadi siswa.

---

### 🟧 Opt-4: Deteksi Multi-Monitor & Kehilangan Fokus

- **Tujuan:** Mengurangi modus layar kedua / fokus pindah.
- **Sub-fitur:** deteksi >1 layar (`EnumDisplayMonitors`); pemantauan fokus
  (kita sudah merebut fokus via `Deactivated -> Activate()`, langkah ini menambah
  **pencatatan/peringatan**).
- **Realita BYOD:** tetap level **deterrence** & rawan *false-positive* (mis. laptop
  + proyektor sah). Butuh kebijakan jelas. Bukan prioritas.

---

### 🟧 Opt-5: Deteksi Sisi Server (heartbeat + token submit) — DISARANKAN untuk BYOD

- **Mengapa:** Inilah satu-satunya lapisan yang **benar-benar efektif di BYOD**,
  karena keputusan terjadi di server (di luar kendali siswa).
- **Sub-fitur (usulan):**
  - **a. Token sesi:** submit jawaban hanya sah bila menyertakan token dari server;
    sesi tanpa token / ganda / kedaluwarsa ditandai.
  - **b. Heartbeat:** aplikasi mengirim sinyal tiap X detik. Bila berhenti mendadak
    (mis. di-End Task), server menandai sesi **mencurigakan** untuk ditinjau pengawas.
  - **c. Anomali:** durasi janggal, sesi dibuka di dua tempat, dll.
- **Catatan:** ini sebagian besar pekerjaan di **sisi web ujian (Firebase)**, bukan
  di aplikasi desktop. Aplikasi desktop cukup mengirim heartbeat. Pergeseran pola pikir:
  **dari "mencegah keluar" → "mendeteksi & membuktikan".**

---

## 🤖 Catatan untuk AI Agent

1. **Opt-2 sudah dipilih** untuk dikerjakan; langkah lain tetap usulan — jangan kerjakan
   tanpa diminta eksplisit.
2. Saat diaudit, bahas **satu langkah pada satu waktu**; konfirmasi keputusan sebelum
   menulis kode.
3. Untuk langkah yang mengubah OS (Opt-1), selalu siapkan **skrip restore** + tegaskan
   kebutuhan hak Administrator — namun ingat Opt-1 **tidak berlaku di BYOD**.
4. Jaga konsistensi gaya: penjelasan beginner-friendly, perintah berbasis CLI, dan
   tidak merusak fungsi Fase 1-5 yang sudah berjalan.
5. Untuk password, gunakan **hash bergaram (PBKDF2)**, BUKAN enkripsi/cipher.

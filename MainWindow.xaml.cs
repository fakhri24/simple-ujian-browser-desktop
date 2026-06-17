using System.Windows;
using System.Windows.Input;

namespace SecureExamBrowser
{
    /// <summary>
    /// Code-behind untuk MainWindow.
    /// Phase 1 fokus pada konfigurasi jendela kiosk yang "tidak bisa diganggu".
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // InitializeComponent() membaca MainWindow.xaml dan membangun UI-nya.
            InitializeComponent();

            // Saat jendela selesai dimuat, kita paksa fokus kembali ke jendela ini
            // supaya elemen di dalamnya bisa langsung menerima input keyboard.
            Loaded += (_, _) =>
            {
                Activate();   // Jadikan jendela ini sebagai jendela aktif.
                Focus();      // Arahkan fokus keyboard ke jendela ini.
            };

            // Jika jendela kehilangan posisi "topmost" (mis. aplikasi lain memaksa naik),
            // kita kembalikan lagi ke atas setiap kali jendela ini ter-deaktivasi.
            Deactivated += (_, _) =>
            {
                Topmost = true;
                Activate();
            };
        }
    }
}

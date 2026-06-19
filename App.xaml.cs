using System.Windows;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Code-behind untuk App.xaml. Menentukan jendela awal:
    /// - Normal           -> MainWindow (browser ujian).
    /// - "--make-hash"    -> HashToolWindow (utilitas operator membuat hash password).
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool makeHash = e.Args.Any(a =>
                string.Equals(a, "--make-hash", StringComparison.OrdinalIgnoreCase));

            Window window = makeHash ? new HashToolWindow() : new MainWindow();
            window.Show();
        }
    }
}

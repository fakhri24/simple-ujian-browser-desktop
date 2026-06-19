using System.Text.Json;
using System.Windows;

namespace SimpleUjianBrowser
{
    /// <summary>
    /// Code-behind utilitas pembuat hash password admin (mode --make-hash).
    /// </summary>
    public partial class HashToolWindow : Window
    {
        public HashToolWindow()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string password = PasswordInput.Password;
            if (string.IsNullOrEmpty(password))
            {
                OutputBox.Text = "Password tidak boleh kosong.";
                return;
            }

            var (salt, hash) = PasswordHasher.Create(password);
            var spec = new PasswordSpec
            {
                Algo = PasswordHasher.Algorithm,
                Iterations = PasswordHasher.DefaultIterations,
                Salt = salt,
                Hash = hash
            };

            // Bungkus dalam properti "adminPassword" supaya siap tempel ke config.json.
            var wrapper = new { adminPassword = spec };
            OutputBox.Text = JsonSerializer.Serialize(
                wrapper, new JsonSerializerOptions { WriteIndented = true });
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(OutputBox.Text))
                Clipboard.SetText(OutputBox.Text);
        }
    }
}

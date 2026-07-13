using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace EarTrumpet.UI.Views
{
    public partial class ChangelogWindow : Window
    {
        private readonly string _version;

        public ChangelogWindow()
        {
            InitializeComponent();

            _version = App.PackageVersion?.ToString() ?? string.Empty;
            VersionMessage.Text = string.Format(
                CultureInfo.CurrentCulture,
                EarTrumpet.Properties.Resources.ChangelogSubtitle,
                _version);
        }

        private void ViewReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
            var releaseUrl = string.IsNullOrWhiteSpace(_version)
                ? "https://github.com/xammen/BetterTrumpet/releases/latest"
                : $"https://github.com/xammen/BetterTrumpet/releases/tag/v{_version}";

            Process.Start(new ProcessStartInfo
            {
                FileName = releaseUrl,
                UseShellExecute = true
            });
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}

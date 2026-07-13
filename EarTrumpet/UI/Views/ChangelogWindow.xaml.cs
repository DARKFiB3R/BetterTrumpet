using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace EarTrumpet.UI.Views
{
    public partial class ChangelogWindow : Window
    {
        private const string ChangelogUrl = "https://bettertrumpet.com/changelog";
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
            Process.Start(new ProcessStartInfo
            {
                FileName = ChangelogUrl,
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

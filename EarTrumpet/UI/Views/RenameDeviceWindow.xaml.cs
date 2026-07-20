using System.Windows;

namespace EarTrumpet.UI.Views
{
    public partial class RenameDeviceWindow : Window
    {
        public RenameDeviceWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RenameDeviceTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            RenameDeviceTextBox.Focus();
            RenameDeviceTextBox.SelectAll();
        }
    }
}

using EarTrumpet.UI.Helpers;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    public class RenameDeviceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action RequestClose;

        public string Title { get; }

        private string _deviceName;
        public string DeviceName
        {
            get => _deviceName;
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeviceName)));
                }
            }
        }

        public ICommand Save { get; }
        public ICommand Cancel { get; }

        public RenameDeviceViewModel(DeviceCollectionViewModel mainViewModel, DeviceViewModel device)
        {
            Title = Properties.Resources.RenameDeviceDialogTitle;
            DeviceName = device.DisplayName;

            Save = new RelayCommand(() =>
            {
                mainViewModel.RenameDevice(device, DeviceName);
                RequestClose?.Invoke();
            });

            Cancel = new RelayCommand(() => RequestClose?.Invoke());
        }
    }
}

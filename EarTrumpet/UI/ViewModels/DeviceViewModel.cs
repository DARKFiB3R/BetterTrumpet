using EarTrumpet.DataModel.Audio;
using EarTrumpet.DataModel.WindowsAudio;
using EarTrumpet.Extensions;
using EarTrumpet.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    public class DeviceViewModel : AudioSessionViewModel, IDeviceViewModel
    {
        public class DisplayNameComparer : IComparer<DeviceViewModel>
        {
            public int Compare(DeviceViewModel one, DeviceViewModel two)
            {
                return string.Compare(one.DisplayName, two.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        public static readonly DisplayNameComparer CompareByDisplayName = new DisplayNameComparer();

        public enum DeviceIconKind
        {
            Mute,
            Bar0,
            Bar1,
            Bar2,
            Bar3,
            Microphone,
        }

        public override string DisplayName => _device.DisplayName;
        protected override bool IsDevice => true;
        public string AccessibleName => IsMuted ? Properties.Resources.AppOrDeviceMutedFormatAccessibleText.Replace("{Name}", DisplayName) :
            Properties.Resources.AppOrDeviceFormatAccessibleText.Replace("{Name}", DisplayName).Replace("{Volume}", Volume.ToString());
        public string DeviceDescription => ((IAudioDeviceWindowsAudio)_device).DeviceDescription;
        public string EnumeratorName => ((IAudioDeviceWindowsAudio)_device).EnumeratorName;
        public string InterfaceName => ((IAudioDeviceWindowsAudio)_device).InterfaceName;
        public ObservableCollection<IAppItemViewModel> Apps { get; }
        public int HiddenAppsCount
        {
            get => _hiddenAppsCount;
            private set
            {
                if (_hiddenAppsCount != value)
                {
                    _hiddenAppsCount = value;
                    RaisePropertyChanged(nameof(HiddenAppsCount));
                    RaisePropertyChanged(nameof(HasHiddenApps));
                }
            }
        }
        public bool HasHiddenApps => HiddenAppsCount > 0;

        public bool IsDisplayNameVisible
        {
            get => _isDisplayNameVisible;
            set
            {
                if (_isDisplayNameVisible != value)
                {
                    _isDisplayNameVisible = value;
                    RaisePropertyChanged(nameof(IsDisplayNameVisible));
                }
            }
        }

        public DeviceIconKind IconKind
        {
            get => _iconKind;
            set
            {
                if (_iconKind != value)
                {
                    _iconKind = value;
                    RaisePropertyChanged(nameof(IconKind));
                }
            }
        }

        protected readonly IAudioDevice _device;
        protected readonly IAudioDeviceManager _deviceManager;
        protected readonly WeakReference<DeviceCollectionViewModel> _parent;
        private readonly AppSettings _settings;
        private bool _isDisplayNameVisible;
        private DeviceIconKind _iconKind;
        private int _hiddenAppsCount;
        private readonly List<IAudioDeviceChannel> _balanceChannels;
        private bool _isApplyingBalance;
        private double _lastAppliedBalance;

        public DeviceViewModel(DeviceCollectionViewModel parent, IAudioDeviceManager deviceManager, AppSettings settings, IAudioDevice device) : base(device)
        {
            _deviceManager = deviceManager;
            _settings = settings;
            _device = device;
            _parent = new WeakReference<DeviceCollectionViewModel>(parent);
            Apps = new ObservableCollection<IAppItemViewModel>();
            ResetBalance = new RelayCommand(() => Balance = 0);

            _device.PropertyChanged += OnPropertyChanged;
            _device.Groups.CollectionChanged += OnCollectionChanged;

            // Balance only makes sense for a conventional stereo pair. Devices with a
            // different channel count (mono mics, 5.1/7.1 speaker sets, etc.) simply
            // don't expose the control.
            if (device is IAudioDeviceWindowsAudio windowsAudioDevice)
            {
                var channels = windowsAudioDevice.Channels?.ToList();
                if (channels != null && channels.Count == 2)
                {
                    _balanceChannels = channels;
                    foreach (var channel in _balanceChannels)
                    {
                        channel.PropertyChanged += OnBalanceChannelPropertyChanged;
                    }

                    ApplyPersistedBalance();
                }
            }

            RebuildAppsCollection();
            RefreshHiddenCount();

            UpdateMasterVolumeIcon();
        }

        ~DeviceViewModel()
        {
            _device.PropertyChanged -= OnPropertyChanged;
            _device.Groups.CollectionChanged -= OnCollectionChanged;

            if (_balanceChannels != null)
            {
                foreach (var channel in _balanceChannels)
                {
                    channel.PropertyChanged -= OnBalanceChannelPropertyChanged;
                }
            }
        }

        public bool IsBalanceSupported => _balanceChannels != null;

        public ICommand ResetBalance { get; }

        // -100 (full left) .. 0 (centered) .. +100 (full right).
        // At balance 0 both channels sit at the same level; skewing towards one side
        // proportionally pulls the *other* channel down while keeping the louder
        // channel at whatever the current volume is - not hardcoded to 100% - so
        // adjusting balance never causes a sudden jump in loudness.
        public double Balance
        {
            get
            {
                if (_balanceChannels == null)
                {
                    return 0;
                }

                var left = _balanceChannels[0].Level;
                var right = _balanceChannels[1].Level;

                if (Math.Abs(left - right) < 0.0001f)
                {
                    return 0;
                }

                return left > right
                    ? -(1.0 - (right / Math.Max(left, 0.0001f))) * 100.0   // skewed left
                    : (1.0 - (left / Math.Max(right, 0.0001f))) * 100.0;   // skewed right
            }
            set
            {
                if (_balanceChannels == null)
                {
                    return;
                }

                var clamped = Math.Max(-100.0, Math.Min(100.0, value));
                ApplyChannels(clamped, CurrentVolumeScalar);
                _settings?.SetBalanceForDevice(_device.Id, clamped);
            }
        }

        private float CurrentVolumeScalar => _balanceChannels == null
            ? 0f
            : Math.Max(_balanceChannels[0].Level, _balanceChannels[1].Level);

        private void ApplyChannels(double balance, float volume)
        {
            float left, right;
            if (balance >= 0)
            {
                right = volume;
                left = volume * (1f - (float)(balance / 100.0));
            }
            else
            {
                left = volume;
                right = volume * (1f - (float)(-balance / 100.0));
            }

            _lastAppliedBalance = balance;
            _isApplyingBalance = true;
            try
            {
                _balanceChannels[0].Level = left;
                _balanceChannels[1].Level = right;
            }
            finally
            {
                _isApplyingBalance = false;
            }

            RaisePropertyChanged(nameof(Balance));
        }

        private void ApplyPersistedBalance()
        {
            var saved = _settings?.GetBalanceForDevice(_device.Id) ?? 0;
            if (Math.Abs(saved) > 0.0001)
            {
                Balance = saved;
            }
        }

        private void OnBalanceChannelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Reflect changes made outside of this control (e.g. the Windows volume mixer's
            // own Levels tab, or another instance of the app) without re-persisting them as
            // if the user had just dragged the slider here.
            if (_isApplyingBalance || e.PropertyName != nameof(IAudioDeviceChannel.Level))
            {
                return;
            }

            // On some drivers, changing the device's own master volume isn't
            // independent of the channels - it can flatten them back level with each
            // other, silently discarding whatever balance was set. If the channels no
            // longer reflect the balance we last intentionally applied, restore that
            // same ratio at the new overall level instead of losing it. This only
            // ever runs for a device where balance has actually been set away from
            // center - it doesn't affect anything until then.
            if (Math.Abs(_lastAppliedBalance) > 0.0001 && Math.Abs(Balance - _lastAppliedBalance) > 0.5)
            {
                ApplyChannels(_lastAppliedBalance, CurrentVolumeScalar);
                return;
            }

            RaisePropertyChanged(nameof(Balance));
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_device.IsMuted) ||
                e.PropertyName == nameof(_device.Volume))
            {
                UpdateMasterVolumeIcon();
                RaisePropertyChanged(nameof(AccessibleName));
            }
            else if (e.PropertyName == nameof(_device.DisplayName))
            {
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(AccessibleName));
            }
        }

        public override void UpdatePeakValueForeground()
        {
            base.UpdatePeakValueForeground();

            foreach (var app in Apps)
            {
                app.UpdatePeakValueForeground();
            }
        }

        private void UpdateMasterVolumeIcon()
        {
            if (_device.Parent.Kind == AudioDeviceKind.Recording.ToString())
            {
                IconKind = DeviceIconKind.Microphone;
            }
            else
            {
                var isOnWindows11 = Environment.OSVersion.IsAtLeast(OSVersions.Windows11);
                if (_device.IsMuted)
                {
                    IconKind = DeviceIconKind.Mute;
                }
                else if (isOnWindows11 && _device.Volume > 0.66f)
                {
                    IconKind = DeviceIconKind.Bar3;
                }
                else if (!isOnWindows11 && _device.Volume >= 0.66f)
                {
                    IconKind = DeviceIconKind.Bar3;
                }
                else if (isOnWindows11 && _device.Volume > 0.33f)
                {
                    IconKind = DeviceIconKind.Bar2;
                }
                else if (!isOnWindows11 && _device.Volume >= 0.33f)
                {
                    IconKind = DeviceIconKind.Bar2;
                }
                else if (_device.Volume > 0.00f)
                {
                    IconKind = DeviceIconKind.Bar1;
                }
                else
                {
                    IconKind = DeviceIconKind.Bar0;
                }
            }
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    AddSession((IAudioDeviceSession)e.NewItems[0], true);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    var existing = Apps.FirstOrDefault(x => x.Id == ((IAudioDeviceSession)e.OldItems[0]).Id);
                    if (existing != null)
                    {
                        Apps.Remove(existing);
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void AddSession(IAudioDeviceSession session, bool animateOnLoad)
        {
            if (_settings != null && _settings.IsAppHiddenForDevice(_device.Id, session.AppId, session.ExeName))
            {
                return;
            }

            var newSession = new AppItemViewModel(this, session, animateOnLoad: animateOnLoad);

            // Hard-muted apps are forced muted whenever a session appears (launch, relaunch, reboot).
            if (_settings != null && _settings.IsAppHardMuted(session.ExeName))
            {
                session.IsMuted = true;
            }

            foreach (var app in Apps)
            {
                if (app.DoesGroupWith(newSession))
                {
                    newSession.Volume = app.Volume;
                    newSession.IsMuted = app.IsMuted;
                    Apps.Remove(app);
                    break;
                }
            }

            Apps.AddSorted(newSession, AppItemViewModel.CompareByExeName);
        }

        private void RebuildAppsCollection()
        {
            Apps.Clear();
            foreach (var session in _device.Groups)
            {
                AddSession(session, false);
            }
        }

        private void ReconcileAppsWithHiddenState()
        {
            foreach (var app in Apps.ToArray())
            {
                if (_settings != null && _settings.IsAppHiddenForDevice(_device.Id, app.AppId, app.ExeName))
                {
                    if (app is TemporaryAppItemViewModel temporaryApp)
                    {
                        temporaryApp.Expired -= OnAppExpired;
                    }

                    Apps.Remove(app);
                }
            }

            foreach (var session in _device.Groups)
            {
                if (_settings != null && _settings.IsAppHiddenForDevice(_device.Id, session.AppId, session.ExeName))
                {
                    continue;
                }

                if (!Apps.Any(app => AppMatchesSession(app, session)))
                {
                    AddSession(session, true);
                }
            }
        }

        private static bool AppMatchesSession(IAppItemViewModel app, IAudioDeviceSession session)
        {
            return string.Equals(app.Id, session.Id, StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(app.AppId) && string.Equals(app.AppId, session.AppId, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshHiddenCount()
        {
            HiddenAppsCount = _settings?.GetHiddenAppCountForDevice(_device.Id) ?? 0;
        }

        internal void RefreshHiddenApps()
        {
            ReconcileAppsWithHiddenState();
            RefreshHiddenCount();
        }

        internal void ApplyHardMuteState()
        {
            if (_settings == null)
            {
                return;
            }

            foreach (var app in Apps)
            {
                if (!app.IsMuted && _settings.IsAppHardMuted(app.ExeName))
                {
                    app.IsMuted = true;
                }
            }
        }

        public void AppMovingToThisDevice(TemporaryAppItemViewModel app)
        {
            app.Expired += OnAppExpired;

            foreach (var childApp in app.ChildApps)
            {
                ((IAudioDeviceManagerWindowsAudio)_deviceManager).UnhideSessionsForProcessId(_device.Id, childApp.ProcessId);
            }

            bool hasExistingAppGroup = false;
            foreach (var a in Apps)
            {
                if (a.DoesGroupWith(app))
                {
                    hasExistingAppGroup = true;
                    break;
                }
            }

            var isHiddenOnThisDevice = _settings != null && _settings.IsAppHiddenForDevice(_device.Id, app.AppId, app.ExeName);
            if (!hasExistingAppGroup && !isHiddenOnThisDevice)
            {
                Apps.AddSorted(app, AppItemViewModel.CompareByExeName);
            }
        }

        private void OnAppExpired(object sender, EventArgs e)
        {
            var app = (TemporaryAppItemViewModel)sender;
            if (Apps.Contains(app))
            {
                app.Expired -= OnAppExpired;
                Apps.Remove(app);
            }
        }

        internal void AppLeavingFromThisDevice(IAppItemViewModel app)
        {
            if (app is TemporaryAppItemViewModel)
            {
                Apps.Remove(app);
            }
        }

        public void MakeDefaultDevice() => _deviceManager.Default = _device;
        public void IncrementVolume(int delta) => Volume += delta;
        public override string ToString() => AccessibleName;
    }
}

using System;
using System.ComponentModel;
using EarTrumpet.Interop.MMDeviceAPI;

namespace EarTrumpet.DataModel.WindowsAudio.Internal
{
    class AudioDeviceChannel : BindableBase, INotifyPropertyChanged, IAudioDeviceChannel
    {
        private float _level;
        private uint _index;
        private IAudioEndpointVolume _deviceVolume;
        private readonly Action _onBeforeWrite;

        public AudioDeviceChannel(IAudioEndpointVolume deviceVolume, uint index, Action onBeforeWrite = null)
        {
            _index = index;
            _deviceVolume = deviceVolume;
            _onBeforeWrite = onBeforeWrite;
            _level = _deviceVolume.GetChannelVolumeLevelScalar(index);
        }

        public float Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    // Setting a channel scalar still triggers the device's normal
                    // volume-change notification (it reports full current state, not
                    // just the field that changed). On some endpoints that snapshot's
                    // reported master volume isn't independent of the channel values,
                    // so give the owning device a chance to avoid treating that as a
                    // genuine master-volume change.
                    _onBeforeWrite?.Invoke();

                    Guid dummy = Guid.Empty;
                    _deviceVolume.SetChannelVolumeLevelScalar(_index, value, ref dummy);

                    _level = value;
                    RaisePropertyChanged(nameof(Level));
                }
            }
        }

        internal void OnNotify(float newLevel)
        {
            if (newLevel != _level)
            {
                _level = newLevel;
                RaisePropertyChanged(nameof(Level));
            }
        }
    }
}

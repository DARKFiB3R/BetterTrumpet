using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using EarTrumpet.Interop.MMDeviceAPI;

namespace EarTrumpet.DataModel.WindowsAudio.Internal
{
    class AudioDeviceChannel : BindableBase, INotifyPropertyChanged, IAudioDeviceChannel
    {
        private float _level;
        private uint _index;
        private IAudioEndpointVolume _deviceVolume;

        public AudioDeviceChannel(IAudioEndpointVolume deviceVolume, uint index)
        {
            _index = index;
            _deviceVolume = deviceVolume;
            _level = _deviceVolume.GetChannelVolumeLevelScalar(index);
        }

        public float Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    Guid dummy = Guid.Empty;
                    _deviceVolume.SetChannelVolumeLevelScalar(_index, value, ref dummy);

                    _level = value;
                    RaisePropertyChanged(nameof(Level));
                }
            }
        }

        // Returns whether the change is large enough to be a genuine external
        // change rather than float round-trip noise from the OS. Does not raise
        // PropertyChanged - callers batch that separately (see
        // AudioDeviceChannelCollection.OnNotify) so a listener reacting to one
        // channel's change always sees every channel's already-applied value.
        internal bool ApplyNotifiedLevel(float newLevel)
        {
            var changed = Math.Abs(newLevel - _level) > 0.0001f;
            _level = newLevel;
            return changed;
        }

        internal void RaiseLevelChanged()
        {
            RaisePropertyChanged(nameof(Level));
        }
    }
}

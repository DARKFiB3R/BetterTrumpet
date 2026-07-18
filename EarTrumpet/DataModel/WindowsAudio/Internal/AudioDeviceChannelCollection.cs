using EarTrumpet.Interop.MMDeviceAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace EarTrumpet.DataModel.WindowsAudio.Internal
{
    class AudioDeviceChannelCollection
    {
        public List<AudioDeviceChannel> Channels { get; }

        private readonly Dispatcher _dispatcher;
        private readonly int _afChannelVolumesOffset;

        public AudioDeviceChannelCollection(IAudioEndpointVolume deviceVolume, Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            var ret = new List<AudioDeviceChannel>();
            for (uint i = 0; i < deviceVolume.GetChannelCount(); i++)
            {
                ret.Add(new AudioDeviceChannel(deviceVolume, i));
            }
            Channels = ret;

            AUDIO_VOLUME_NOTIFICATION_DATA dummy;
            _afChannelVolumesOffset = Marshal.OffsetOf<AUDIO_VOLUME_NOTIFICATION_DATA>(nameof(dummy.afChannelVolumes)).ToInt32();
        }

        public bool OnNotify(IntPtr pNotify, AUDIO_VOLUME_NOTIFICATION_DATA data)
        {
            var channelVolumesValues = new float[data.nChannels];
            Marshal.Copy(IntPtr.Add(pNotify, _afChannelVolumesOffset), channelVolumesValues, 0, (int)data.nChannels);

            var anyChannelChanged = false;
            for (var i = 0; i < data.nChannels; i++)
            {
                if (Math.Abs(Channels[i].Level - channelVolumesValues[i]) > 0.0001f)
                {
                    anyChannelChanged = true;
                }
            }

            Trace.WriteLine($"[BALTRACE] AudioDeviceChannelCollection.OnNotify nChannels={data.nChannels} anyChanged={anyChannelChanged} vals=[{string.Join(",", channelVolumesValues)}] tid={Thread.CurrentThread.ManagedThreadId}");

            // Apply every channel's new value before raising any PropertyChanged,
            // so a listener reacting to one channel's change (e.g. balance
            // self-correction) always sees every other channel's new value too,
            // instead of one still holding a value from before this notification -
            // which otherwise reads as a bogus balance drift and fights the OS's
            // own update (e.g. when a master-volume change proportionally rescales
            // both channels at once).
            var changed = new bool[data.nChannels];
            for (var i = 0; i < data.nChannels; i++)
            {
                changed[i] = Channels[i].ApplyNotifiedLevel(channelVolumesValues[i]);
            }

            if (Array.Exists(changed, c => c))
            {
                // These notifications arrive on an arbitrary COM callback thread.
                // Raising them there would let a reentrant balance self-correction
                // (DeviceViewModel.OnBalanceChannelPropertyChanged) run concurrently
                // with a UI-thread ApplyChannels call over the same unsynchronized
                // fields, so marshal to the UI thread the same way AudioDevice.OnNotify
                // already does for Volume/IsMuted.
                _dispatcher.Invoke((Action)(() =>
                {
                    for (var i = 0; i < data.nChannels; i++)
                    {
                        if (changed[i])
                        {
                            Channels[i].RaiseLevelChanged();
                        }
                    }
                }));
            }

            return anyChannelChanged;
        }
    }
}

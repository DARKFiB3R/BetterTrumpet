using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EarTrumpet.UI.Controls
{
    // Shared tick-sound playback for VolumeSlider and BalanceSlider drag feedback.
    internal static class TickSoundPlayer
    {
        private static byte[] _tickWavBytes;
        private static int _playbackInProgress;

        public static void Play(double volume)
        {
            if (App.Settings?.UseVolumeTickSound != true)
                return;

            try
            {
                if (_tickWavBytes == null)
                {
                    var streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/tick.wav"));
                    if (streamResourceInfo != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            streamResourceInfo.Stream.CopyTo(memoryStream);
                            _tickWavBytes = memoryStream.ToArray();
                        }
                    }
                }

                if (_tickWavBytes == null)
                    return;

                // Opening the device per tick is slower than reusing a live player, so under
                // fast dragging (ticks arriving every ~50ms) each PlaySync call can outlast
                // the interval between ticks. Queuing every call via Task.Run would let a
                // backlog build up and keep audibly ticking well after the slider is
                // released. Instead, drop a tick if a previous one hasn't finished yet.
                if (Interlocked.CompareExchange(ref _playbackInProgress, 1, 0) != 0)
                    return;

                var scaledWav = ScaleWavVolume(_tickWavBytes, volume);

                // SoundPlayer (winmm PlaySound) opens and releases its audio device per call
                // instead of holding a persistent WASAPI session open like
                // System.Windows.Media.MediaPlayer does — that's what previously made
                // BetterTrumpet stick around in the Windows Volume Mixer for the rest of the
                // app's life once a tick had ever played. Play on a background thread so we
                // don't block the UI, and dispose once playback finishes.
                Task.Run(() =>
                {
                    try
                    {
                        using (var stream = new MemoryStream(scaledWav))
                        using (var player = new System.Media.SoundPlayer(stream))
                        {
                            player.PlaySync();
                        }
                    }
                    catch { /* Ignore sound errors */ }
                    finally
                    {
                        Interlocked.Exchange(ref _playbackInProgress, 0);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"TickSoundPlayer: Failed to play tick sound — {ex.Message}");
            }
        }

        // Scales the 16-bit PCM samples in a WAV byte buffer by the given linear volume
        // factor, returning a new buffer. Leaves the file header/other chunks untouched.
        private static byte[] ScaleWavVolume(byte[] wavBytes, double volume)
        {
            var result = (byte[])wavBytes.Clone();
            var dataIndex = FindChunk(result, "data");
            if (dataIndex < 0)
                return result;

            var dataSize = BitConverter.ToInt32(result, dataIndex + 4);
            var sampleStart = dataIndex + 8;
            var sampleEnd = Math.Min(sampleStart + dataSize, result.Length - 1);

            for (var i = sampleStart; i + 1 <= sampleEnd; i += 2)
            {
                var sample = BitConverter.ToInt16(result, i);
                var scaled = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, sample * volume));
                result[i] = (byte)(scaled & 0xFF);
                result[i + 1] = (byte)((scaled >> 8) & 0xFF);
            }

            return result;
        }

        // Finds the byte offset of a RIFF chunk (e.g. "data") by its 4-character ID.
        private static int FindChunk(byte[] wavBytes, string chunkId)
        {
            var id = System.Text.Encoding.ASCII.GetBytes(chunkId);
            for (var i = 12; i + 4 <= wavBytes.Length; i++)
            {
                if (wavBytes[i] == id[0] && wavBytes[i + 1] == id[1] && wavBytes[i + 2] == id[2] && wavBytes[i + 3] == id[3])
                    return i;
            }
            return -1;
        }
    }
}

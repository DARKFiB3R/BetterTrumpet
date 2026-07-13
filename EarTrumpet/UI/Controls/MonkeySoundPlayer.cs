using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EarTrumpet.UI.Controls
{
    internal static class MonkeySoundPlayer
    {
        private const double RepeatCrossfadeMs = 75;
        private const double RangeCrossfadeMs = 40;
        private const double ActivityWindowMs = 180;

        private static readonly IReadOnlyDictionary<string, double> ClipDurationsMs =
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["Assets/monkeylow.wav"] = 599.896,
                ["Assets/monkeymid.wav"] = 566.917,
                ["Assets/monkeyhigh.wav"] = 525.875
            };

        private static readonly PlaybackChannel[] Channels =
        {
            new PlaybackChannel(),
            new PlaybackChannel()
        };

        private static readonly Dictionary<string, string> ExtractedPaths =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static DispatcherTimer _timer;
        private static int _activeChannelIndex = -1;
        private static string _desiredResourcePath;
        private static double _desiredVolume;
        private static DateTime _lastActivityAt = DateTime.MinValue;

        public static void Play(double volume)
        {
            try
            {
                var now = DateTime.UtcNow;
                var resourcePath = GetResourcePath(volume);

                _desiredResourcePath = resourcePath;
                _desiredVolume = Math.Max(0.1, Math.Min(1.0, volume / 100.0));
                _lastActivityAt = now;

                EnsureTimer();

                var activeChannel = GetActiveChannel();
                if (activeChannel == null ||
                    !string.Equals(activeChannel.ResourcePath, resourcePath, StringComparison.Ordinal))
                {
                    StartOnAlternateChannel(resourcePath, _desiredVolume, now, RangeCrossfadeMs);
                }
                else
                {
                    activeChannel.BaseVolume = _desiredVolume;
                    activeChannel.ApplyVolume(now);
                }

                if (!_timer.IsEnabled)
                {
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"MonkeySoundPlayer: Failed to start playback — {ex.Message}");
                StopAll();
            }
        }

        private static void EnsureTimer()
        {
            if (_timer != null)
            {
                return;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(15)
            };
            _timer.Tick += OnTimerTick;
        }

        private static void OnTimerTick(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;

            foreach (var channel in Channels)
            {
                channel.Update(now);
            }

            var activeChannel = GetActiveChannel();
            var isStillAdjusting = (now - _lastActivityAt).TotalMilliseconds <= ActivityWindowMs;

            if (activeChannel == null &&
                isStillAdjusting &&
                !string.IsNullOrEmpty(_desiredResourcePath))
            {
                StartOnAlternateChannel(_desiredResourcePath, _desiredVolume, now, 0);
                activeChannel = GetActiveChannel();
            }

            if (activeChannel != null)
            {
                activeChannel.BaseVolume = _desiredVolume;
                activeChannel.ApplyVolume(now);

                var remainingMs = (activeChannel.EndAt - now).TotalMilliseconds;

                if (isStillAdjusting &&
                    remainingMs > 0 &&
                    remainingMs <= RepeatCrossfadeMs)
                {
                    StartOnAlternateChannel(
                        _desiredResourcePath,
                        _desiredVolume,
                        now,
                        RepeatCrossfadeMs);
                }
                else if (remainingMs <= 0 && activeChannel.IsPlaying)
                {
                    activeChannel.Stop();
                    _activeChannelIndex = -1;
                }
            }

            if (!Channels[0].IsPlaying && !Channels[1].IsPlaying)
            {
                _activeChannelIndex = -1;
                _timer.Stop();
            }
        }

        private static void StartOnAlternateChannel(
            string resourcePath,
            double baseVolume,
            DateTime now,
            double crossfadeMs)
        {
            var previousChannel = GetActiveChannel();
            var nextChannelIndex = _activeChannelIndex == 0 ? 1 : 0;
            var nextChannel = Channels[nextChannelIndex];

            if (nextChannel.IsPlaying)
            {
                nextChannel.Stop();
            }

            var extractedPath = EnsureExtracted(resourcePath);
            nextChannel.Start(
                resourcePath,
                extractedPath,
                ClipDurationsMs[resourcePath],
                baseVolume,
                now,
                previousChannel == null ? 0 : crossfadeMs);

            if (previousChannel != null)
            {
                previousChannel.FadeTo(0, now, crossfadeMs);
            }

            _activeChannelIndex = nextChannelIndex;
        }

        private static PlaybackChannel GetActiveChannel()
        {
            if (_activeChannelIndex < 0 || _activeChannelIndex >= Channels.Length)
            {
                return null;
            }

            var channel = Channels[_activeChannelIndex];
            return channel.IsPlaying ? channel : null;
        }

        private static string EnsureExtracted(string resourcePath)
        {
            if (ExtractedPaths.TryGetValue(resourcePath, out var existingPath) &&
                File.Exists(existingPath))
            {
                return existingPath;
            }

            var streamResourceInfo = Application.GetResourceStream(
                new Uri($"pack://application:,,,/{resourcePath}"));

            if (streamResourceInfo == null)
            {
                throw new FileNotFoundException($"Embedded monkey sound not found: {resourcePath}");
            }

            var tempDirectory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "BetterTrumpet",
                "audio");
            Directory.CreateDirectory(tempDirectory);

            var tempPath = System.IO.Path.Combine(
                tempDirectory,
                System.IO.Path.GetFileName(resourcePath));

            using (var fileStream = File.Create(tempPath))
            {
                streamResourceInfo.Stream.CopyTo(fileStream);
            }

            ExtractedPaths[resourcePath] = tempPath;
            return tempPath;
        }

        private static string GetResourcePath(double volume)
        {
            if (volume <= 20)
            {
                return "Assets/monkeylow.wav";
            }

            if (volume < 85)
            {
                return "Assets/monkeymid.wav";
            }

            return "Assets/monkeyhigh.wav";
        }

        private static void StopAll()
        {
            foreach (var channel in Channels)
            {
                channel.Stop();
            }

            _activeChannelIndex = -1;
            _timer?.Stop();
        }

        private sealed class PlaybackChannel
        {
            public MediaPlayer Player { get; } = new MediaPlayer();
            public string ResourcePath { get; private set; }
            public bool IsPlaying { get; private set; }
            public DateTime EndAt { get; private set; }
            public double BaseVolume { get; set; }

            private string _openedPath;
            private double _gain;
            private double _fadeFrom;
            private double _fadeTo;
            private DateTime _fadeStartedAt;
            private double _fadeDurationMs;

            public void Start(
                string resourcePath,
                string extractedPath,
                double durationMs,
                double baseVolume,
                DateTime now,
                double fadeInMs)
            {
                if (!string.Equals(_openedPath, extractedPath, StringComparison.OrdinalIgnoreCase))
                {
                    Player.Close();
                    Player.Open(new Uri(extractedPath, UriKind.Absolute));
                    _openedPath = extractedPath;
                }

                ResourcePath = resourcePath;
                BaseVolume = baseVolume;
                EndAt = now.AddMilliseconds(durationMs);
                IsPlaying = true;

                _gain = fadeInMs > 0 ? 0 : 1;
                Player.Volume = BaseVolume * _gain;
                Player.Position = TimeSpan.Zero;
                Player.Play();

                if (fadeInMs > 0)
                {
                    FadeTo(1, now, fadeInMs);
                }
            }

            public void FadeTo(double targetGain, DateTime now, double durationMs)
            {
                UpdateGain(now);
                _fadeFrom = _gain;
                _fadeTo = targetGain;
                _fadeStartedAt = now;
                _fadeDurationMs = Math.Max(1, durationMs);
            }

            public void Update(DateTime now)
            {
                if (!IsPlaying)
                {
                    return;
                }

                UpdateGain(now);
                ApplyVolume(now);

                if (_gain <= 0.001 && _fadeTo <= 0.001)
                {
                    Stop();
                }
                else if (now >= EndAt)
                {
                    Stop();
                }
            }

            public void ApplyVolume(DateTime now)
            {
                if (!IsPlaying)
                {
                    return;
                }

                UpdateGain(now);
                Player.Volume = Math.Max(0, Math.Min(1, BaseVolume * _gain));
            }

            public void Stop()
            {
                if (IsPlaying)
                {
                    Player.Stop();
                }

                IsPlaying = false;
                ResourcePath = null;
                _gain = 0;
                _fadeFrom = 0;
                _fadeTo = 0;
                _fadeDurationMs = 0;
            }

            private void UpdateGain(DateTime now)
            {
                if (_fadeDurationMs <= 0)
                {
                    return;
                }

                var progress = Math.Max(
                    0,
                    Math.Min(1, (now - _fadeStartedAt).TotalMilliseconds / _fadeDurationMs));
                var easedProgress = 1 - Math.Pow(1 - progress, 2);
                _gain = _fadeFrom + ((_fadeTo - _fadeFrom) * easedProgress);

                if (progress >= 1)
                {
                    _gain = _fadeTo;
                    _fadeDurationMs = 0;
                }
            }
        }
    }
}

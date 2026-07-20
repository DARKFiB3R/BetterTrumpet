using EarTrumpet.Interop;
using EarTrumpet.UI.ViewModels;
using System;
using System.Text;
using System.Windows;

namespace EarTrumpet.Interop.Helpers
{
    // Suppresses Windows' native volume OSD specifically when it would be showing a device
    // BetterTrumpet has locally renamed. That OSD always reads the device's real registry
    // name and has no way to know about BT's local-only rename, so instead of showing the
    // wrong name we hide it for renamed devices and let BT's own tray tooltip (which does
    // respect renames) stand in as the feedback. Devices without a custom name are left
    // completely alone.
    //
    // On builds where the OSD is hosted in the legacy NativeHWNDHost/DirectUIHWND pair
    // (what the open-source HideVolumeOSD tool targets), that class name is specific enough
    // to act on directly. On newer builds it's hosted in a generic Windows.UI.Core.CoreWindow
    // instead, which is used by many unrelated UWP/XAML-Islands surfaces (Search, Widgets,
    // Action Center, etc.) - so for that case this also requires the window's size and
    // screen position to roughly match the known OSD card before acting on it.
    //
    // For the CoreWindow case, the hide has to happen the instant EVENT_OBJECT_SHOW fires,
    // before geometry is even checked: its XAML content composes asynchronously, so its rect
    // is a 1x1 placeholder at that point and only settles a frame or two later. Hiding it
    // preemptively (and only undoing that if a short retry loop never confirms OSD-shaped
    // geometry) avoids a visible flash of the wrong name for the couple of frames that
    // would otherwise render before confirmation was possible. DWM cloaking a cross-process
    // window returns E_ACCESSDENIED (only the owning process can cloak its own window), so
    // plain ShowWindow is what's actually doing the work here.
    public sealed class NativeVolumeOsdSuppressor : IDisposable
    {
        private const string LegacyOsdHostClass = "NativeHWNDHost";
        private const string LegacyOsdChildClass = "DirectUIHWND";
        private const string ModernOsdHostClass = "Windows.UI.Core.CoreWindow";
        private const int GeometryRecheckIntervalMs = 40;
        private const int GeometryRecheckMaxAttempts = 8;

        private readonly DeviceCollectionViewModel _collectionViewModel;
        private readonly User32.WinEventDelegate _winEventDelegate;
        private IntPtr _hook = IntPtr.Zero;
        private bool _started;

        // Rooted so an in-flight one-shot geometry-recheck timer can't be GC'd before it fires.
        private readonly System.Collections.Generic.List<System.Threading.Timer> _pendingTimers = new System.Collections.Generic.List<System.Threading.Timer>();
        private readonly object _pendingTimersLock = new object();

        public NativeVolumeOsdSuppressor(DeviceCollectionViewModel collectionViewModel)
        {
            _collectionViewModel = collectionViewModel;
            _winEventDelegate = OnWinEvent; // keep rooted for the hook's lifetime
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            _hook = User32.SetWinEventHook(
                User32.EVENT_OBJECT_SHOW,
                User32.EVENT_OBJECT_SHOW,
                IntPtr.Zero,
                _winEventDelegate,
                0,
                0,
                User32.WINEVENT_OUTOFCONTEXT);
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            _started = false;

            if (_hook != IntPtr.Zero)
            {
                User32.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }

            lock (_pendingTimersLock)
            {
                foreach (var timer in _pendingTimers)
                {
                    timer.Dispose();
                }
                _pendingTimers.Clear();
            }
        }

        private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != User32.OBJID_WINDOW || hwnd == IntPtr.Zero)
            {
                return;
            }

            // Only ever relevant when the current default device has a local rename -
            // otherwise the native OSD showing the real name is correct as-is.
            if (_collectionViewModel.Default?.HasCustomName != true)
            {
                return;
            }

            var sb = new StringBuilder(User32.MAX_CLASSNAME_LENGTH);
            User32.GetClassName(hwnd, sb, User32.MAX_CLASSNAME_LENGTH);
            var className = sb.ToString();

            if (className == LegacyOsdHostClass)
            {
                var child = User32.FindWindowEx(hwnd, IntPtr.Zero, LegacyOsdChildClass, IntPtr.Zero);
                if (child == IntPtr.Zero)
                {
                    return;
                }

                User32.ShowWindow(hwnd, User32.SW_MINIMIZE);
            }
            else if (className == ModernOsdHostClass)
            {
                User32.ShowWindow(hwnd, User32.SW_HIDE);
                RecheckModernOsd(hwnd, GeometryRecheckMaxAttempts);
            }
        }

        private void RecheckModernOsd(IntPtr hwnd, int attemptsLeft)
        {
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer(_ =>
            {
                lock (_pendingTimersLock)
                {
                    _pendingTimers.Remove(timer);
                }
                timer.Dispose();

                if (!User32.IsWindow(hwnd) || LooksLikeVolumeOsd(hwnd))
                {
                    return;
                }

                if (attemptsLeft > 1)
                {
                    RecheckModernOsd(hwnd, attemptsLeft - 1);
                }
                else
                {
                    // Never settled into OSD-shaped geometry - wasn't the volume OSD, undo.
                    User32.ShowWindow(hwnd, User32.SW_SHOWNA);
                }
            }, null, GeometryRecheckIntervalMs, System.Threading.Timeout.Infinite);

            lock (_pendingTimersLock)
            {
                _pendingTimers.Add(timer);
            }
        }

        // Windows.UI.Core.CoreWindow is used by many unrelated flyouts, so this narrows to
        // ones sized and positioned like the small volume/brightness-style OSD card - a
        // few hundred pixels wide, roughly a hundred tall, anchored toward the bottom-right
        // of the virtual screen (where that OSD renders regardless of monitor layout).
        private static bool LooksLikeVolumeOsd(IntPtr hwnd)
        {
            if (!User32.GetWindowRect(hwnd, out RECT rect))
            {
                return false;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width < 150 || width > 800 || height < 50 || height > 300)
            {
                return false;
            }

            double rightHalfStart = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth * 0.5;
            double bottomHalfStart = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight * 0.5;

            return rect.Left >= rightHalfStart && rect.Top >= bottomHalfStart;
        }

        public void Dispose() => Stop();
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EarTrumpet.UI.Controls
{
    // A Slider that behaves exactly like a normal Slider everywhere, except for a small
    // magnetic detent right at its center value: dragging into that zone snaps straight
    // to center, and dragging back out requires passing a slightly wider threshold before
    // it lets go, giving a small "pop" feel on release rather than a mushy, ambiguous edge.
    //
    // Outside of that narrow zone, values pass through completely untouched — full
    // precision and the original feel are preserved across the rest of the range.
    //
    // Keyboard nudges and programmatic sets (e.g. restoring a persisted value on load)
    // are intentionally left alone; the detent only applies to live pointer interaction.
    public class BalanceSlider : Slider
    {
        // How close to center a drag must get to snap in.
        private const double SnapInZone = 4.0;

        // How far a drag must move back out before the snap releases. Wider than
        // SnapInZone on purpose - that gap is what creates the felt "pop" on release
        // instead of the value flickering in and out right at one boundary.
        private const double SnapOutThreshold = 8.0;

        private bool _isInteracting;
        private bool _isSnapped;

        static BalanceSlider()
        {
            // Reuse whatever Style the app already applies to plain Sliders elsewhere,
            // rather than falling back to the unstyled default OS look.
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BalanceSlider), new FrameworkPropertyMetadata(typeof(Slider)));

            ValueProperty.OverrideMetadata(typeof(BalanceSlider),
                new FrameworkPropertyMetadata(0.0, null, CoerceValue));
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var slider = (BalanceSlider)d;
            var raw = (double)baseValue;

            return slider._isInteracting ? slider.ApplyMagneticCenter(raw) : raw;
        }

        private double ApplyMagneticCenter(double raw)
        {
            var center = (Maximum + Minimum) / 2.0;
            var distance = Math.Abs(raw - center);

            if (_isSnapped)
            {
                if (distance >= SnapOutThreshold)
                {
                    _isSnapped = false;
                    return raw; // pop free and track the pointer immediately, no lag
                }
                return center;
            }

            if (distance <= SnapInZone)
            {
                _isSnapped = true;
                return center;
            }

            return raw;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _isInteracting = true;
            _isSnapped = false;
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            _isInteracting = false;
        }

        protected override void OnPreviewTouchDown(TouchEventArgs e)
        {
            _isInteracting = true;
            _isSnapped = false;
            base.OnPreviewTouchDown(e);
        }

        protected override void OnPreviewTouchUp(TouchEventArgs e)
        {
            base.OnPreviewTouchUp(e);
            _isInteracting = false;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            _isInteracting = false;
        }
    }
}

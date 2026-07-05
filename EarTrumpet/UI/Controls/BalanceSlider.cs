using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EarTrumpet.UI.Controls
{
    // A Slider that magnetically resists movement away from its center value while the
    // user is actively dragging/clicking it, without giving up any precision or range:
    // the remap is a smooth, monotonic, fully-invertible cubic curve, so every value from
    // Minimum to Maximum is still reachable and nothing is ever lost or rounded away.
    //
    // Keyboard nudges (arrow keys) and programmatic sets (e.g. restoring a persisted
    // value on load) are intentionally left untouched — the magnet only applies to live
    // pointer interaction, where a "sticky center" is a helpful, expected feel.
    public class BalanceSlider : Slider
    {
        private bool _isInteracting;

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
            var halfRange = Math.Max(0.0001, (Maximum - Minimum) / 2.0);
            var center = (Maximum + Minimum) / 2.0;
            var offset = raw - center;
            var sign = Math.Sign(offset);
            var normalized = Math.Min(1.0, Math.Abs(offset) / halfRange);

            // Cubic ease-in: nearly flat right around center (resistant), steepening back
            // out to full sensitivity by the time you reach either end.
            var eased = normalized * normalized * normalized;

            return center + sign * eased * halfRange;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _isInteracting = true;
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

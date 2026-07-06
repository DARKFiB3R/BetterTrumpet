using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EarTrumpet.UI.ViewModels;

namespace EarTrumpet.UI.Controls
{
    // A Slider with a magnetic center detent: outside a small zone around its center
    // value it behaves like an ordinary slider, but dragging into that zone locks the
    // value at center and holds it there - genuinely nothing changes, no matter how
    // far you keep dragging - until the pointer moves back out past a slightly wider
    // release threshold, at which point it lets go and resumes tracking the pointer
    // immediately from wherever it now is.
    //
    // This computes value directly from absolute pointer position on every move (the
    // same technique the app's own VolumeSlider control uses), rather than relying on
    // the default Slider/Thumb's relative drag-delta accumulation. That accumulation
    // is what makes a true "frozen while held" dead zone impossible with a stock
    // Slider: the internal running total keeps climbing even while the displayed
    // value is held still, so releasing it produces an unpredictable jump instead of
    // a clean pop back to wherever the pointer actually is.
    public class BalanceSlider : Slider
    {
        private const double SnapInZone = 4.0;
        private const double SnapOutThreshold = 10.0;

        private bool _isDragging;
        private bool _isSnapped;
        private Thumb _thumb;
        private Border _centerTick;

        static BalanceSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BalanceSlider), new FrameworkPropertyMetadata(typeof(BalanceSlider)));
        }

        public BalanceSlider() : base()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _thumb = GetTemplateChild("SliderThumb") as Thumb;
            _centerTick = GetTemplateChild("CenterTick") as Border;

            ApplyCustomColors();

            if (App.Settings != null)
            {
                App.Settings.CustomSliderColorsChanged += OnCustomSliderColorsChanged;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (App.Settings != null)
            {
                App.Settings.CustomSliderColorsChanged -= OnCustomSliderColorsChanged;
            }
        }

        private void OnCustomSliderColorsChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ApplyCustomColors));
                return;
            }
            ApplyCustomColors();
        }

        // Mirrors VolumeSlider.ApplyCustomColors so the balance slider stays visually
        // consistent with every other slider, whether the person is using the default
        // theme accent or has picked custom slider colors in Settings.
        private void ApplyCustomColors()
        {
            var settings = App.Settings;
            if (settings == null || _thumb == null)
            {
                return;
            }

            if (settings.UseCustomSliderColors)
            {
                var thumbColor = settings.SliderThumbColor;
                var brush = new SolidColorBrush(thumbColor != Colors.Transparent ? thumbColor : ThemeRegistry.DefaultAccentColor);

                // Local values win over the Theme:Brush system's own local-value writes
                // (see the identical note in VolumeSlider.ApplyColorsToVisualElements) -
                // setting these directly, last, is what makes the override actually stick.
                _thumb.Foreground = brush;
                if (_centerTick != null)
                {
                    _centerTick.Background = brush;
                }
            }
            else
            {
                _thumb.ClearValue(ForegroundProperty);
                _centerTick?.ClearValue(Border.BackgroundProperty);
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _isDragging = true;
            _isSnapped = false;
            CaptureMouse();
            UpdateValueFromPoint(e.GetPosition(this));
            Focus();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                UpdateValueFromPoint(e.GetPosition(this));
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnPreviewTouchDown(TouchEventArgs e)
        {
            _isDragging = true;
            _isSnapped = false;
            CaptureTouch(e.TouchDevice);
            UpdateValueFromPoint(e.GetTouchPoint(this).Position);
            e.Handled = true;
        }

        protected override void OnPreviewTouchUp(TouchEventArgs e)
        {
            _isDragging = false;
            ReleaseTouchCapture(e.TouchDevice);
            e.Handled = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            _isDragging = false;
        }

        private void UpdateValueFromPoint(Point point)
        {
            if (ActualWidth <= 0)
            {
                return;
            }

            var percent = Math.Max(0.0, Math.Min(1.0, point.X / ActualWidth));
            var raw = Minimum + percent * (Maximum - Minimum);
            var center = (Maximum + Minimum) / 2.0;
            var distance = Math.Abs(raw - center);

            if (_isSnapped)
            {
                if (distance >= SnapOutThreshold)
                {
                    _isSnapped = false;
                    Value = raw; // pop free, track the pointer immediately
                }
                // Still held by the center - the pointer can move freely within the
                // zone and nothing happens, exactly as if the slider were locked.
                return;
            }

            if (distance <= SnapInZone)
            {
                _isSnapped = true;
                Value = center;
                return;
            }

            Value = raw;
        }
    }
}

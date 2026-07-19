using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private Border _trackBg;
        private Border _fill;
        private Border _peakLeft;
        private Border _peakRight;

        // Live per-channel meter levels (0-1), same source VolumeSlider's own peak meter
        // uses (Device.PeakValue1/2, backed by the real Windows per-channel audio peak
        // API) - bound from DeviceView.xaml, not user-set.
        public float PeakValue1
        {
            get { return (float)GetValue(PeakValue1Property); }
            set { SetValue(PeakValue1Property, value); }
        }
        public static readonly DependencyProperty PeakValue1Property = DependencyProperty.Register(
            "PeakValue1", typeof(float), typeof(BalanceSlider), new PropertyMetadata(0f, OnPeakValueChanged));

        public float PeakValue2
        {
            get { return (float)GetValue(PeakValue2Property); }
            set { SetValue(PeakValue2Property, value); }
        }
        public static readonly DependencyProperty PeakValue2Property = DependencyProperty.Register(
            "PeakValue2", typeof(float), typeof(BalanceSlider), new PropertyMetadata(0f, OnPeakValueChanged));

        // How much Balance is attenuating each channel right now, relative to the louder
        // one (1.0 = not attenuated). The raw PeakValue1/2 meter reading doesn't reliably
        // reflect this on every driver (see DeviceViewModel.BalanceGainRatioLeft/Right for
        // why), so these are used to scale the raw reading ourselves instead of trusting
        // the meter to already show it.
        public double GainRatioLeft
        {
            get { return (double)GetValue(GainRatioLeftProperty); }
            set { SetValue(GainRatioLeftProperty, value); }
        }
        public static readonly DependencyProperty GainRatioLeftProperty = DependencyProperty.Register(
            "GainRatioLeft", typeof(double), typeof(BalanceSlider), new PropertyMetadata(1.0, OnPeakValueChanged));

        public double GainRatioRight
        {
            get { return (double)GetValue(GainRatioRightProperty); }
            set { SetValue(GainRatioRightProperty, value); }
        }
        public static readonly DependencyProperty GainRatioRightProperty = DependencyProperty.Register(
            "GainRatioRight", typeof(double), typeof(BalanceSlider), new PropertyMetadata(1.0, OnPeakValueChanged));

        private static void OnPeakValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BalanceSlider)d).UpdatePeakMeterGeometry();
        }

        static BalanceSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BalanceSlider), new FrameworkPropertyMetadata(typeof(BalanceSlider)));
        }

        public BalanceSlider() : base()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += (_, __) => { UpdateFillGeometry(); UpdatePeakMeterGeometry(); };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _thumb = GetTemplateChild("SliderThumb") as Thumb;
            _centerTick = GetTemplateChild("CenterTick") as Border;
            _trackBg = GetTemplateChild("TrackBg") as Border;
            _fill = GetTemplateChild("Fill") as Border;
            _peakLeft = GetTemplateChild("PeakLeft") as Border;
            _peakRight = GetTemplateChild("PeakRight") as Border;

            ApplyCustomColors();
            ApplyPeakMeterStyle();
            UpdateFillGeometry();
            UpdatePeakMeterGeometry();

            // The generic Theme:Brush system (used for the default, non-custom accent
            // color) also writes a local value to Foreground on this same Loaded event,
            // and can fire after us depending on handler order - silently overwriting
            // our custom color right after we set it. Re-applying once more, deferred,
            // guarantees we go last and actually stick. Unlike VolumeSlider (always
            // visible), this row starts Visibility=Collapsed until IsBalanceSupported
            // flips it - that visibility change can trigger its own later restyling
            // pass, so ContextIdle (rather than Loaded priority) to land after it too.
            Dispatcher.BeginInvoke(new Action(ApplyCustomColors), System.Windows.Threading.DispatcherPriority.ContextIdle);

            if (App.Settings != null)
            {
                App.Settings.CustomSliderColorsChanged += OnCustomSliderColorsChanged;
                App.Settings.PeakMeterStyleChanged += OnPeakMeterStyleChanged;
            }

            // Re-apply custom colors after any theme change (the theme system re-sets
            // Foreground via local values, so we must re-override) - matches VolumeSlider.
            // The balance row is conditionally shown via a Visibility DataTrigger rather
            // than always present like VolumeSlider, which means the theme system can run
            // an extra restyling pass tied to that visibility flip, after our own
            // Loaded-priority re-apply already ran. Without this subscription that later
            // pass has nothing to catch it, leaving the slider unthemed until something
            // else (e.g. a settings change) happens to fire CustomSliderColorsChanged.
            if (UI.Themes.Manager.Current != null)
            {
                UI.Themes.Manager.Current.ThemeChanged += OnThemeChangedReapplyColors;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (App.Settings != null)
            {
                App.Settings.CustomSliderColorsChanged -= OnCustomSliderColorsChanged;
                App.Settings.PeakMeterStyleChanged -= OnPeakMeterStyleChanged;
            }
            if (UI.Themes.Manager.Current != null)
            {
                UI.Themes.Manager.Current.ThemeChanged -= OnThemeChangedReapplyColors;
            }
        }

        private void OnPeakMeterStyleChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyPeakMeterStyle);
                return;
            }
            ApplyPeakMeterStyle();
        }

        // Mirrors VolumeSlider's per-style Height/CornerRadius/OpacityMask so the balance
        // meter matches whatever Peak Meter Style is selected in Settings. Unlike
        // VolumeSlider (which hides its second bar for single-bar styles), both PeakLeft
        // and PeakRight always stay visible here - they're not a stereo-depth pair, they're
        // two genuinely different channels.
        private void ApplyPeakMeterStyle()
        {
            if (App.Settings == null || _peakLeft == null || _peakRight == null)
            {
                return;
            }

            double height;
            CornerRadius cornerRadius;
            double opacity;
            Brush mask;

            switch (App.Settings.PeakMeterStyle)
            {
                case PeakMeterStyle.Dotted:
                    height = 3;
                    cornerRadius = new CornerRadius(0);
                    opacity = 0.7;
                    mask = VolumeSlider.CreateDottedBrush(3, 2);
                    break;
                case PeakMeterStyle.Blocks:
                    height = 4;
                    cornerRadius = new CornerRadius(0);
                    opacity = 0.7;
                    mask = VolumeSlider.CreateDottedBrush(6, 2);
                    break;
                case PeakMeterStyle.Bars:
                    height = 2;
                    cornerRadius = new CornerRadius(1);
                    opacity = 0.55;
                    mask = null;
                    break;
                case PeakMeterStyle.Wave:
                    height = 3;
                    cornerRadius = new CornerRadius(1);
                    opacity = 0.65;
                    mask = VolumeSlider.CreateDottedBrush(5, 4);
                    break;
                default: // Classic
                    height = 4;
                    cornerRadius = new CornerRadius(2);
                    opacity = 0.6;
                    mask = null;
                    break;
            }

            _peakLeft.Height = height;
            _peakLeft.CornerRadius = cornerRadius;
            _peakLeft.Opacity = opacity;
            _peakLeft.OpacityMask = mask;

            _peakRight.Height = height;
            _peakRight.CornerRadius = cornerRadius;
            _peakRight.Opacity = opacity;
            _peakRight.OpacityMask = mask;
        }

        private void OnThemeChangedReapplyColors()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(OnThemeChangedReapplyColors));
                return;
            }

            ApplyCustomColors();
            Dispatcher.BeginInvoke(new Action(ApplyCustomColors), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void OnCustomSliderColorsChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(OnCustomSliderColorsChanged));
                return;
            }

            ApplyCustomColors();

            // Same race as in OnLoaded: the generic Theme:Brush system also reacts to
            // a theme change and can overwrite our color right after we set it if it
            // runs second. Going again, deferred - and later in the queue than Loaded
            // priority, since the conditional-visibility restyling pass can land there -
            // guarantees we're the one that sticks.
            Dispatcher.BeginInvoke(new Action(ApplyCustomColors), System.Windows.Threading.DispatcherPriority.ContextIdle);
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

                var trackFillColor = settings.SliderTrackFillColor;
                var fillBrush = new SolidColorBrush(trackFillColor != Colors.Transparent ? trackFillColor : ThemeRegistry.DefaultAccentColor);
                if (_fill != null)
                {
                    _fill.Background = fillBrush;
                }

                var trackBackgroundColor = settings.SliderTrackBackgroundColor;
                if (_trackBg != null && trackBackgroundColor != Colors.Transparent)
                {
                    _trackBg.Background = new SolidColorBrush(trackBackgroundColor);
                    _trackBg.Opacity = 1;
                }

                var peakMeterColor = settings.PeakMeterColor;
                var peakBrush = new SolidColorBrush(peakMeterColor != Colors.Transparent ? peakMeterColor : ThemeRegistry.DefaultPeakMeter);
                if (_peakLeft != null)
                {
                    _peakLeft.Background = peakBrush;
                }
                if (_peakRight != null)
                {
                    _peakRight.Background = peakBrush;
                }
            }
            else
            {
                _thumb.ClearValue(ForegroundProperty);
                _centerTick?.ClearValue(Border.BackgroundProperty);
                _fill?.ClearValue(Border.BackgroundProperty);
                _trackBg?.ClearValue(Border.BackgroundProperty);
                _trackBg?.ClearValue(OpacityProperty);
                _peakLeft?.ClearValue(Border.BackgroundProperty);
                _peakRight?.ClearValue(Border.BackgroundProperty);
            }
        }

        // Fill grows from center (Value == 0) toward the thumb, proportional to how far
        // off-center the balance is - the visual equivalent of VolumeSlider's 0-to-value
        // fill, just anchored at the middle instead of the left edge.
        private void UpdateFillGeometry()
        {
            if (_fill == null || ActualWidth <= 0)
            {
                return;
            }

            var center = (Maximum + Minimum) / 2.0;
            var halfRange = (Maximum - Minimum) / 2.0;
            var percent = halfRange > 0 ? Math.Abs(Value - center) / halfRange : 0;
            var halfWidth = ActualWidth / 2.0;
            var fillWidth = Math.Max(0, percent * halfWidth);
            var marginLeft = Value >= center ? halfWidth : halfWidth - fillWidth;

            _fill.Width = fillWidth;
            _fill.Margin = new Thickness(marginLeft, 0, 0, 0);
        }

        // Real stereo meter: left channel level grows leftward from center, right channel
        // grows rightward - independent of Value/Fill, driven by the live PeakValue1/2
        // the same way VolumeSlider's own peak meter is, just anchored at the middle
        // instead of the left edge. Each raw value is further scaled by GainRatioLeft/
        // Right, since the raw meter reading alone doesn't reliably reflect the balance
        // skew on every driver - see the property doc comments for why.
        private void UpdatePeakMeterGeometry()
        {
            if (ActualWidth <= 0)
            {
                return;
            }

            var halfWidth = ActualWidth / 2.0;

            if (_peakLeft != null)
            {
                var rawLeft = Math.Max(0, Math.Min(1, PeakValue1));
                var leftWidth = rawLeft * GainRatioLeft * halfWidth;
                _peakLeft.Width = leftWidth;
                _peakLeft.Margin = new Thickness(halfWidth - leftWidth, 0, 0, 0);
            }

            if (_peakRight != null)
            {
                var rawRight = Math.Max(0, Math.Min(1, PeakValue2));
                var rightWidth = rawRight * GainRatioRight * halfWidth;
                _peakRight.Width = rightWidth;
                _peakRight.Margin = new Thickness(halfWidth, 0, 0, 0);
            }
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            UpdateFillGeometry();
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

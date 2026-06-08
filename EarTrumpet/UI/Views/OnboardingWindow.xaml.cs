using EarTrumpet.UI.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EarTrumpet.UI.Views
{
    public partial class OnboardingWindow : Window
    {
        private readonly Duration _slideDuration = new Duration(TimeSpan.FromMilliseconds(320));
        private readonly Duration _fastDuration = new Duration(TimeSpan.FromMilliseconds(200));
        private readonly IEasingFunction _easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        private readonly IEasingFunction _backEaseOut = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
        private int _lastPage = -1;
        private bool _confettiTriggered;
        private bool _reducedMotion;

        public OnboardingWindow()
        {
            InitializeComponent();
            VersionText.Text = $"v{App.PackageVersion}";

            // Detect reduced motion preference
            _reducedMotion = SystemParameters.ClientAreaAnimation == false;

            Loaded += OnLoaded;

            // Keyboard shortcuts
            PreviewKeyDown += OnboardingWindow_KeyDown;
        }

        private void OnboardingWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is OnboardingViewModel vm)
            {
                // Escape = Skip
                if (e.Key == Key.Escape)
                {
                    Skip_Click(null, null);
                    e.Handled = true;
                }
                // Enter = Next (when CTA button is focused or no focus)
                else if (e.Key == Key.Enter && !IsDeviceCardFocused() && !IsThemeCardFocused())
                {
                    Next_Click(null, null);
                    e.Handled = true;
                }
            }
        }

        private bool IsDeviceCardFocused()
        {
            var focused = FocusManager.GetFocusedElement(this) as FrameworkElement;
            return focused?.Tag is AudioDeviceChoice;
        }

        private bool IsThemeCardFocused()
        {
            var focused = FocusManager.GetFocusedElement(this) as FrameworkElement;
            return focused?.Name == "Theme0Card" || focused?.Name == "Theme1Card";
        }

        private void CtaButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                Next_Click(sender, null);
                e.Handled = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Start with splash screen
            ShowSplashScreen();

            if (DataContext is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += OnViewModelPropertyChanged;
            }

            // Cleanup on close to avoid memory leaks
            Closed += (s, ev) =>
            {
                if (DataContext is INotifyPropertyChanged n)
                    n.PropertyChanged -= OnViewModelPropertyChanged;
                ConfettiCanvas.Children.Clear();
            };
        }

        private void ShowSplashScreen()
        {
            // Skip splash if reduced motion
            if (_reducedMotion)
            {
                SplashContainer.Visibility = Visibility.Collapsed;
                MainContent.Opacity = 1;
                AnimatePageEntrance(Page0, Page0Translate);
                UpdateProgressBar();
                UpdateStepDots(0);
                StartGradientShimmer();
                AnimateWelcomePage();
                return;
            }

            // Hide main content
            MainContent.Opacity = 0;
            SplashContainer.Visibility = Visibility.Visible;
            SplashContainer.Opacity = 0;

            // Fade in splash
            var splashFadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                EasingFunction = _easeOut
            };
            SplashContainer.BeginAnimation(OpacityProperty, splashFadeIn);

            // Logo burst animation
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                AnimateLogoBurst();
            };
            timer.Start();

            // Transition to main content after splash
            var transitionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
            transitionTimer.Tick += (s, ev) =>
            {
                transitionTimer.Stop();
                TransitionToMainContent();
            };
            transitionTimer.Start();
        }

        private void AnimateLogoBurst()
        {
            // Logo pop with back ease
            var scaleAnim = new DoubleAnimation(0.5, 1.0, new Duration(TimeSpan.FromMilliseconds(600)))
            {
                EasingFunction = _backEaseOut
            };
            SplashLogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            SplashLogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // Particle burst
            LaunchSplashParticles();

            // Breathing effect after burst
            var breathTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            breathTimer.Tick += (s, ev) =>
            {
                breathTimer.Stop();
                StartLogoBreathing();
            };
            breathTimer.Start();
        }

        private void LaunchSplashParticles()
        {
            var rand = new Random();
            var centerX = SplashContainer.ActualWidth / 2;
            var centerY = SplashContainer.ActualHeight / 2;

            for (int i = 0; i < 20; i++)
            {
                var angle = (360.0 / 20) * i;
                var radians = angle * Math.PI / 180.0;
                var distance = 60 + rand.NextDouble() * 40;

                var particle = new Ellipse
                {
                    Width = 4 + rand.NextDouble() * 3,
                    Height = 4 + rand.NextDouble() * 3,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 59, 158, 255)),
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                Canvas.SetLeft(particle, centerX);
                Canvas.SetTop(particle, centerY);
                SplashCanvas.Children.Add(particle);

                var endX = centerX + Math.Cos(radians) * distance;
                var endY = centerY + Math.Sin(radians) * distance;

                var moveX = new DoubleAnimation(centerX, endX, new Duration(TimeSpan.FromMilliseconds(400)))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var moveY = new DoubleAnimation(centerY, endY, new Duration(TimeSpan.FromMilliseconds(400)))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(400)))
                {
                    BeginTime = TimeSpan.FromMilliseconds(200)
                };

                fadeOut.Completed += (s, ev) => SplashCanvas.Children.Remove(particle);

                particle.BeginAnimation(Canvas.LeftProperty, moveX);
                particle.BeginAnimation(Canvas.TopProperty, moveY);
                particle.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void StartLogoBreathing()
        {
            var breathe = new DoubleAnimation(1.0, 1.03, new Duration(TimeSpan.FromMilliseconds(1200)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            SplashLogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, breathe);
            SplashLogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, breathe);
        }

        private void TransitionToMainContent()
        {
            // Fade out splash
            var splashFadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(400)))
            {
                EasingFunction = _easeOut
            };
            splashFadeOut.Completed += (s, ev) =>
            {
                SplashContainer.Visibility = Visibility.Collapsed;
                SplashCanvas.Children.Clear();
            };
            SplashContainer.BeginAnimation(OpacityProperty, splashFadeOut);

            // Fade in main content with slight delay
            var delay = TimeSpan.FromMilliseconds(200);
            var mainFadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(500)))
            {
                BeginTime = delay,
                EasingFunction = _easeOut
            };
            MainContent.BeginAnimation(OpacityProperty, mainFadeIn);

            // Animate first page entrance
            var pageDelay = TimeSpan.FromMilliseconds(300);
            var pageTimer = new DispatcherTimer { Interval = pageDelay };
            pageTimer.Tick += (s, ev) =>
            {
                pageTimer.Stop();
                AnimatePageEntrance(Page0, Page0Translate);
                UpdateProgressBar();
                UpdateStepDots(0);
                StartGradientShimmer();
                AnimateWelcomePage();
            };
            pageTimer.Start();
        }

        private void AnimateWelcomePage()
        {
            // Typing effect on subtitle
            StartTypingEffect();

            // Floating preview animation (skip if reduced motion)
            if (!_reducedMotion)
            {
                var floatAnim = new DoubleAnimation(-8, 8, new Duration(TimeSpan.FromMilliseconds(2500)))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                PreviewFloat.BeginAnimation(TranslateTransform.YProperty, floatAnim);
            }

            // Animate preview bars
            AnimatePreviewBars();

            // Counter animation
            AnimateUserCounter();
        }

        private void StartTypingEffect()
        {
            if (DataContext is OnboardingViewModel vm)
            {
                var fullText = vm.SubtitleText;
                var currentText = "";
                var charIndex = 0;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                timer.Tick += (s, ev) =>
                {
                    if (charIndex < fullText.Length)
                    {
                        currentText += fullText[charIndex];
                        WelcomeSubtitle.Text = currentText;
                        charIndex++;
                    }
                    else
                    {
                        timer.Stop();
                    }
                };
                timer.Start();
            }
        }

        private void AnimatePreviewBars()
        {
            var bars = new[] { PreviewBar1, PreviewBar2, PreviewBar3, PreviewBar4 };
            var heights = new[] { 40.0, 60.0, 35.0, 50.0 };
            var targetHeights = new[] { 55.0, 45.0, 50.0, 38.0 };

            for (int i = 0; i < bars.Length; i++)
            {
                var bar = bars[i];
                var delay = TimeSpan.FromMilliseconds(500 + i * 150);

                var heightAnim = new DoubleAnimation(heights[i], targetHeights[i],
                    new Duration(TimeSpan.FromMilliseconds(1200)))
                {
                    BeginTime = delay,
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                bar.BeginAnimation(HeightProperty, heightAnim);
            }
        }

        private void AnimateUserCounter()
        {
            var targetCount = 4000;
            var currentCount = 0;
            var increment = targetCount / 40; // 40 steps

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
            timer.Tick += (s, ev) =>
            {
                if (currentCount < targetCount)
                {
                    currentCount += increment;
                    if (currentCount > targetCount) currentCount = targetCount;
                    UserCountText.Text = $"{currentCount:N0}+";
                }
                else
                {
                    timer.Stop();
                    UserCountText.Text = "4,000+";
                }
            };

            // Start after typing effect
            var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            delayTimer.Tick += (s, ev) =>
            {
                delayTimer.Stop();
                timer.Start();
            };
            delayTimer.Start();
        }

        private void WelcomeLogo_MouseEnter(object sender, MouseEventArgs e)
        {
            // Subtle 3D tilt on hover
            var scaleAnim = new DoubleAnimation(1.0, 1.05, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = _easeOut
            };
            WelcomeLogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            WelcomeLogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            var rotateAnim = new DoubleAnimation(0, 5, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = _easeOut
            };
            WelcomeLogoRotateY.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
        }

        private void WelcomeLogo_MouseLeave(object sender, MouseEventArgs e)
        {
            var scaleAnim = new DoubleAnimation(1.05, 1.0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = _easeOut
            };
            WelcomeLogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            WelcomeLogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            var rotateAnim = new DoubleAnimation(5, 0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = _easeOut
            };
            WelcomeLogoRotateY.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
        }

        private void StartGradientShimmer()
        {
            // Subtle gradient animation for premium feel
            var shimmer = new DoubleAnimation(0.3, 0.6, new Duration(TimeSpan.FromMilliseconds(3000)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            GradientMesh.BeginAnimation(OpacityProperty, shimmer);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OnboardingViewModel.CurrentPage) && DataContext is OnboardingViewModel vm)
            {
                int newPage = vm.CurrentPage;
                bool forward = newPage > _lastPage;
                AnimatePageTransition(newPage, forward);
                UpdateProgressBar();
                UpdateStepDots(newPage);
                _lastPage = newPage;

                // Announce page change to screen readers
                AnnouncePageChange(newPage);

                if (newPage == 2)
                {
                    AnimateAppearancePage();
                }
                else if (newPage == 3)
                {
                    AnimatePrivacyPage();
                }
                else if (newPage == 4)
                {
                    AnimateReadyPage();
                }
                else if (newPage == 5)
                {
                    AnimateTrayPinPage();
                }
            }
            else if (e.PropertyName == nameof(OnboardingViewModel.IsTelemetryEnabled) && DataContext is OnboardingViewModel vm2)
            {
                // Show confirmation dialog when user disables telemetry
                if (!vm2.IsTelemetryEnabled)
                {
                    ShowTelemetryDisableConfirmation(vm2);
                }
            }
        }

        private void AnnouncePageChange(int pageIndex)
        {
            var pageNames = new[] { "Welcome", "Audio Output", "Appearance", "Privacy", "Ready", "Tray Pin" };
            if (pageIndex >= 0 && pageIndex < pageNames.Length)
            {
                var announcement = $"Page {pageIndex + 1} of {pageNames.Length}: {pageNames[pageIndex]}";
                AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
                AutomationProperties.SetName(this, announcement);
            }
        }

        private void ShowTelemetryDisableConfirmation(OnboardingViewModel vm)
        {
            var result = MessageBox.Show(
                "Disabling crash reports makes it much harder for us to fix stability issues and bugs that affect you.\n\n" +
                "Crash reports are anonymous and help us improve BetterTrumpet for everyone.\n\n" +
                "Are you sure you want to disable crash reports?",
                "Help Us Improve BetterTrumpet",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                // Re-enable telemetry
                vm.IsTelemetryEnabled = true;
            }
        }

        private void AnimateAppearancePage()
        {
            // Animate theme preview bars
            var theme0Bars = new[] { Theme0Bar1, Theme0Bar2, Theme0Bar3, Theme0Bar4 };
            var theme1Bars = new[] { Theme1Bar1, Theme1Bar2, Theme1Bar3, Theme1Bar4 };

            for (int i = 0; i < theme0Bars.Length; i++)
            {
                var delay = TimeSpan.FromMilliseconds(300 + i * 100);

                var pulseAnim = new DoubleAnimation
                {
                    From = 0.3,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(800)),
                    BeginTime = delay,
                    EasingFunction = _easeOut
                };
                theme0Bars[i].BeginAnimation(OpacityProperty, pulseAnim);
                theme1Bars[i].BeginAnimation(OpacityProperty, pulseAnim);
            }
        }

        private void AnimatePrivacyPage()
        {
            // Page Privacy loaded - no specific animations needed
        }

        private void UpdateProgressBar()
        {
            if (DataContext is OnboardingViewModel vm)
            {
                var anim = new DoubleAnimation(vm.Progress, new Duration(TimeSpan.FromMilliseconds(350)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                ProgressScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            }
        }

        private void UpdateStepDots(int currentPage)
        {
            var dots = new[] { Dot0, Dot1, Dot2, Dot3, Dot4, Dot5 };
            var accentBrush = (SolidColorBrush)FindResource("AccentBrush");
            var dimBrush = new SolidColorBrush(Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF));

            for (int i = 0; i < dots.Length; i++)
            {
                var targetBrush = i == currentPage ? accentBrush : dimBrush;
                var anim = new ColorAnimation(((SolidColorBrush)targetBrush).Color,
                    new Duration(TimeSpan.FromMilliseconds(250)))
                {
                    EasingFunction = _easeOut
                };
                ((SolidColorBrush)dots[i].Fill).BeginAnimation(SolidColorBrush.ColorProperty, anim);

                // Active dot slightly bigger
                var sizeAnim = new DoubleAnimation(i == currentPage ? 7 : 6,
                    new Duration(TimeSpan.FromMilliseconds(200)))
                {
                    EasingFunction = _easeOut
                };
                dots[i].BeginAnimation(WidthProperty, sizeAnim);
                dots[i].BeginAnimation(HeightProperty, sizeAnim);
            }
        }

        private void AnimatePageTransition(int newPage, bool forward)
        {
            FrameworkElement page;
            TranslateTransform translate;

            switch (newPage)
            {
                case 0: page = Page0; translate = Page0Translate; break;
                case 1: page = Page1; translate = Page1Translate; break;
                case 2: page = Page2; translate = Page2Translate; break;
                case 3: page = Page3; translate = Page3Translate; break;
                case 4: page = Page4; translate = Page4Translate; break;
                case 5: page = Page5; translate = Page5Translate; break;
                default: return;
            }

            AnimatePageEntrance(page, translate, forward);
        }

        private void AnimatePageEntrance(FrameworkElement page, TranslateTransform translate, bool forward = true)
        {
            double startX = forward ? 50 : -50;

            var slideAnim = new DoubleAnimation(startX, 0, _slideDuration)
            {
                EasingFunction = _easeOut
            };
            translate.BeginAnimation(TranslateTransform.XProperty, slideAnim);

            var fadeAnim = new DoubleAnimation(0, 1, _slideDuration)
            {
                EasingFunction = _easeOut
            };
            page.BeginAnimation(OpacityProperty, fadeAnim);
        }

        private void AnimateReadyPage()
        {
            // Launch confetti IMMEDIATELY on page 4
            LaunchConfetti();

            var items = new[] { CheckItem0, CheckItem1, CheckItem2 };
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                item.Opacity = 0;
                item.RenderTransform = new TranslateTransform(0, 12);

                var delay = TimeSpan.FromMilliseconds(200 + i * 120);

                var fadeIn = new DoubleAnimation(0, 1, _fastDuration)
                {
                    BeginTime = delay,
                    EasingFunction = _easeOut
                };
                item.BeginAnimation(OpacityProperty, fadeIn);

                var slideUp = new DoubleAnimation(12, 0, _fastDuration)
                {
                    BeginTime = delay,
                    EasingFunction = _easeOut
                };
                ((TranslateTransform)item.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideUp);
            }

            var btnDelay = TimeSpan.FromMilliseconds(700);
            var pulse = new DoubleAnimation(1.0, 1.06, new Duration(TimeSpan.FromMilliseconds(400)))
            {
                BeginTime = btnDelay,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            CtaButtonScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            CtaButtonScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        private void AnimateTrayPinPage()
        {
            // Load the GIF programmatically (more reliable than pack URI in non-packaged mode)
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/TrayPin.gif", UriKind.Absolute);
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(TrayPinGif, uri);
                XamlAnimatedGif.AnimationBehavior.SetAutoStart(TrayPinGif, true);
                XamlAnimatedGif.AnimationBehavior.SetRepeatBehavior(TrayPinGif, RepeatBehavior.Forever);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"OnboardingWindow: Failed to load TrayPin.gif — {ex.Message}");
            }

            // CTA button pulse
            var btnDelay = TimeSpan.FromMilliseconds(400);
            var pulse = new DoubleAnimation(1.0, 1.04, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                BeginTime = btnDelay,
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            CtaButtonScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            CtaButtonScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        // ═══ CONFETTI SYSTEM ═══

        private static readonly Color[] ConfettiColors = new[]
        {
            Color.FromRgb(0x3B, 0x9E, 0xFF), // blue (accent)
            Color.FromRgb(0x00, 0xBC, 0xD4), // cyan
            Color.FromRgb(0x7C, 0x4D, 0xFF), // purple
            Color.FromRgb(0xFF, 0x6D, 0x3B), // orange
            Color.FromRgb(0x4C, 0xAF, 0x50), // green
            Color.FromRgb(0xFF, 0x45, 0x6B), // pink-red
            Color.FromRgb(0xFF, 0xC8, 0x57), // gold
        };

        private void LaunchConfetti()
        {
            // Skip confetti if reduced motion
            if (_reducedMotion || _confettiTriggered) return;

            _confettiTriggered = true;

            var rand = new Random();
            double canvasW = ConfettiCanvas.ActualWidth > 0 ? ConfettiCanvas.ActualWidth : 520;
            double canvasH = ConfettiCanvas.ActualHeight > 0 ? ConfettiCanvas.ActualHeight : 640;

            // Spawn confetti in 3 bursts for a more natural effect
            for (int burst = 0; burst < 3; burst++)
            {
                int burstDelay = burst * 150; // ms between bursts
                int piecesInBurst = burst == 0 ? 28 : (burst == 1 ? 20 : 12);

                for (int i = 0; i < piecesInBurst; i++)
                {
                    SpawnConfettiPiece(rand, canvasW, canvasH, burstDelay, i);
                }
            }
        }

        private void SpawnConfettiPiece(Random rand, double canvasW, double canvasH, int burstDelayMs, int index)
        {
            var color = ConfettiColors[rand.Next(ConfettiColors.Length)];

            // Mix of rectangles and small squares
            bool isSquare = rand.NextDouble() > 0.6;
            double w = isSquare ? 6 + rand.NextDouble() * 3 : 4 + rand.NextDouble() * 3;
            double h = isSquare ? w : 8 + rand.NextDouble() * 6;

            var piece = new Rectangle
            {
                Width = w,
                Height = h,
                RadiusX = 1,
                RadiusY = 1,
                Fill = new SolidColorBrush(color),
                Opacity = 0.85 + rand.NextDouble() * 0.15,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };

            // Start position: spread across the top, some from edges
            double startX = rand.NextDouble() * canvasW;
            double startY = -10 - rand.NextDouble() * 40;

            Canvas.SetLeft(piece, startX);
            Canvas.SetTop(piece, startY);

            var transformGroup = new TransformGroup();
            var rotate = new RotateTransform(rand.NextDouble() * 360);
            var scale = new ScaleTransform(1, 1);
            transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(scale);
            piece.RenderTransform = transformGroup;

            ConfettiCanvas.Children.Add(piece);

            // Animation timing
            var delay = TimeSpan.FromMilliseconds(burstDelayMs + rand.Next(100));
            double fallDuration = 2800 + rand.NextDouble() * 2200; // 2.8s to 5s — slow gentle fall

            // Fall down (Y)
            double endY = canvasH + 20;
            var fallAnim = new DoubleAnimation(startY, endY, new Duration(TimeSpan.FromMilliseconds(fallDuration)))
            {
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fallAnim.Completed += (s, e) =>
            {
                ConfettiCanvas.Children.Remove(piece);
            };

            // Horizontal sway (X) — gentle sine-like drift
            double swayAmount = 30 + rand.NextDouble() * 50;
            double swayDir = rand.NextDouble() > 0.5 ? 1 : -1;
            var swayAnim = new DoubleAnimation(startX, startX + swayAmount * swayDir,
                new Duration(TimeSpan.FromMilliseconds(fallDuration)))
            {
                BeginTime = delay,
                AutoReverse = false,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            // Rotation — continuous spin
            double rotEnd = rotate.Angle + (200 + rand.NextDouble() * 400) * (rand.NextDouble() > 0.5 ? 1 : -1);
            var rotAnim = new DoubleAnimation(rotate.Angle, rotEnd,
                new Duration(TimeSpan.FromMilliseconds(fallDuration)))
            {
                BeginTime = delay
            };

            // Flip effect via ScaleX oscillation (simulates 3D tumble)
            var flipAnim = new DoubleAnimation(1, -1,
                new Duration(TimeSpan.FromMilliseconds(300 + rand.Next(400))))
            {
                BeginTime = delay,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(fallDuration))
            };

            // Fade out near the bottom
            var fadeAnim = new DoubleAnimation(piece.Opacity, 0,
                new Duration(TimeSpan.FromMilliseconds(600)))
            {
                BeginTime = delay + TimeSpan.FromMilliseconds(fallDuration - 600)
            };

            piece.BeginAnimation(Canvas.TopProperty, fallAnim);
            piece.BeginAnimation(Canvas.LeftProperty, swayAnim);
            rotate.BeginAnimation(RotateTransform.AngleProperty, rotAnim);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, flipAnim);
            piece.BeginAnimation(OpacityProperty, fadeAnim);
        }

        // ═══ EVENT HANDLERS ═══

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        private void Next_Click(object sender, MouseButtonEventArgs e)
        {
            // Tactile squish effect
            var squish = new DoubleAnimation(1.0, 0.95, new Duration(TimeSpan.FromMilliseconds(80)))
            {
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            CtaButtonScale.BeginAnimation(ScaleTransform.ScaleXProperty, squish);
            CtaButtonScale.BeginAnimation(ScaleTransform.ScaleYProperty, squish);

            if (DataContext is OnboardingViewModel vm)
            {
                if (vm.IsLastPage)
                {
                    // Launch confetti, then close after a short delay
                    LaunchConfetti();

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
                    timer.Tick += (s, ev) =>
                    {
                        timer.Stop();
                        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(400)))
                        {
                            EasingFunction = _easeOut
                        };
                        fadeOut.Completed += (s2, ev2) => vm.SkipCommand.Execute(null);
                        BeginAnimation(OpacityProperty, fadeOut);
                    };
                    timer.Start();
                }
                else
                {
                    vm.NextCommand.Execute(null);
                }
            }
            e.Handled = true;
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is OnboardingViewModel vm)
                vm.BackCommand.Execute(null);
            e.Handled = true;
        }

        private void Skip_Click(object sender, MouseButtonEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, _fastDuration);
            fadeOut.Completed += (s, ev) =>
            {
                if (DataContext is OnboardingViewModel vm)
                    vm.SkipCommand.Execute(null);
            };
            BeginAnimation(OpacityProperty, fadeOut);
            e.Handled = true;
        }

        private void DeviceCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is OnboardingViewModel vm && sender is FrameworkElement fe && fe.Tag is AudioDeviceChoice choice)
            {
                // Bounce effect on click
                var bounce = new DoubleAnimation(1.0, 0.96, new Duration(TimeSpan.FromMilliseconds(100)))
                {
                    AutoReverse = true,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                if (fe.RenderTransform == null || !(fe.RenderTransform is ScaleTransform))
                {
                    fe.RenderTransform = new ScaleTransform(1, 1);
                    fe.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                ((ScaleTransform)fe.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
                ((ScaleTransform)fe.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, bounce);

                foreach (var dev in vm.AudioDevices)
                    dev.IsDefault = false;
                choice.IsDefault = true;
                vm.SelectedDevice = choice;
            }
            if (e != null) e.Handled = true;
        }

        private void DeviceCard_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                DeviceCard_Click(sender, null);
                e.Handled = true;
            }
        }

        private void Theme0_Click(object sender, MouseButtonEventArgs e)
        {
            AddClickBounce(sender);
            if (DataContext is OnboardingViewModel vm) vm.SelectedThemeIndex = 0;
            e.Handled = true;
        }

        private void Theme1_Click(object sender, MouseButtonEventArgs e)
        {
            AddClickBounce(sender);
            if (DataContext is OnboardingViewModel vm) vm.SelectedThemeIndex = 1;
            e.Handled = true;
        }

        private void ThemeCard_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                if (sender is FrameworkElement fe)
                {
                    if (fe.Name == "Theme0Card")
                        Theme0_Click(sender, null);
                    else if (fe.Name == "Theme1Card")
                        Theme1_Click(sender, null);
                }
                e.Handled = true;
            }
        }

        private void AddClickBounce(object sender)
        {
            if (sender is FrameworkElement fe)
            {
                var bounce = new DoubleAnimation(1.0, 0.97, new Duration(TimeSpan.FromMilliseconds(100)))
                {
                    AutoReverse = true,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                if (fe.RenderTransform == null || !(fe.RenderTransform is ScaleTransform))
                {
                    fe.RenderTransform = new ScaleTransform(1, 1);
                    fe.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                ((ScaleTransform)fe.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
                ((ScaleTransform)fe.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
            }
        }

        private void OnboardingUpdateChannel0_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is OnboardingViewModel vm) vm.UpdateChannelIndex = 0;
            e.Handled = true;
        }

        private void OnboardingUpdateChannel1_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is OnboardingViewModel vm) vm.UpdateChannelIndex = 1;
            e.Handled = true;
        }

        private void OnboardingUpdateChannel3_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is OnboardingViewModel vm) vm.UpdateChannelIndex = 3;
            e.Handled = true;
        }
    }
}

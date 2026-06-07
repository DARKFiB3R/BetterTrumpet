using EarTrumpet.DataModel;
using EarTrumpet.Extensions;
using EarTrumpet.Interop;
using EarTrumpet.Interop.Helpers;
using EarTrumpet.UI.Themes;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EarTrumpet.UI.Helpers
{
    public class WindowAnimationLibrary
    {
        // Windows 11 style: short, directional movement that feels tied to the taskbar.
        const int _animationOffset = 8;
        const int _entranceDurationMs = 165;
        const int _exitDurationMs = 105;

        // Premium "pop" entrance: content scales up from the taskbar edge with a slight
        // elastic overshoot (BackEase). Tuned to read as snappy, not bouncy.
        const int _scaleEntranceDurationMs = 220;
        const double _entranceFromScale = 0.92;
        const double _backEaseAmplitude = 0.35;

        // Staggered row reveal: each row fades + slides up, offset by a growing delay.
        const int _staggerPerItemMs = 28;
        const int _staggerRowDurationMs = 260;
        const double _staggerSlideFromPx = 10;
        const int _staggerMaxItems = 24; // cap so a huge app list can't make the tail crawl in

        /// <summary>
        /// Shows the flyout without the scale "pop" — just uncloak + foreground/focus. Used
        /// when re-opening after an expand/collapse, where the whole-window pop feels wrong
        /// and the staggered row cascade should carry the transition instead.
        /// </summary>
        public static void BeginFlyoutInstantShow(Window window, WindowsTaskbar.State taskbar, Action completed)
        {
            window.Topmost = false;
            window.Activate();
            BringTaskbarToFront();
            window.Opacity = 1;
            window.Cloak(false);
            window.Topmost = true;
            window.Focus();
            User32.SetForegroundWindow(window.GetHandle());
            completed();
        }

        /// <summary>
        /// Reveals rows one-by-one with a short, growing delay (fade + slide-up), producing
        /// the cascade effect on open. Pass the rows in visual (top-to-bottom) order. No-op
        /// when animations are disabled. Each row gets its own TranslateTransform that is
        /// cleared on completion so layout/hit-testing is unaffected afterwards.
        /// </summary>
        public static void BeginStaggeredRowReveal(System.Collections.Generic.IReadOnlyList<FrameworkElement> rows)
        {
            if (!Manager.Current.AnimationsEnabled || rows == null || rows.Count == 0)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                {
                    continue;
                }

                var delayMs = System.Math.Min(i, _staggerMaxItems) * _staggerPerItemMs;
                var beginTime = TimeSpan.FromMilliseconds(delayMs);
                var duration = new Duration(TimeSpan.FromMilliseconds(_staggerRowDurationMs));
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

                var translate = new TranslateTransform(0, _staggerSlideFromPx);
                row.RenderTransform = translate;

                // Start hidden so the pre-delay frames don't flash the row at its final spot.
                row.Opacity = 0;

                var fade = new DoubleAnimation
                {
                    From = 0, To = 1,
                    BeginTime = beginTime,
                    Duration = duration,
                    EasingFunction = ease,
                    FillBehavior = FillBehavior.Stop,
                };
                fade.Completed += (s, e) => row.Opacity = 1;

                var slide = new DoubleAnimation
                {
                    From = _staggerSlideFromPx, To = 0,
                    BeginTime = beginTime,
                    Duration = duration,
                    EasingFunction = ease,
                    FillBehavior = FillBehavior.Stop,
                };
                slide.Completed += (s, e) => row.RenderTransform = null;

                row.BeginAnimation(UIElement.OpacityProperty, fade);
                translate.BeginAnimation(TranslateTransform.YProperty, slide);
            }
        }

        /// <summary>
        /// Premium entrance: scales the flyout content up from the taskbar-anchored edge
        /// with an elastic overshoot, while the window fades in. Runs on the WPF content
        /// (a ScaleTransform on <paramref name="content"/>), not the Win32 window, so it
        /// stays GPU-cheap. Falls back to an instant show when animations are disabled.
        /// </summary>
        public static void BeginFlyoutScaleEntranceAnimation(Window window, FrameworkElement content, WindowsTaskbar.State taskbar, Action completed)
        {
            var onCompleted = new EventHandler((s, e) =>
            {
                window.Topmost = true;
                window.Focus();
                User32.SetForegroundWindow(window.GetHandle());
                completed();
            });

            window.Topmost = false;
            window.Activate();
            BringTaskbarToFront();

            if (!Manager.Current.AnimationsEnabled)
            {
                window.Cloak(false);
                onCompleted(null, null);
                return;
            }

            // Anchor the scale origin to the edge nearest the taskbar so the flyout
            // appears to grow out of the tray, mirroring native Win11 flyouts.
            content.RenderTransformOrigin = GetScaleOriginForTaskbar(taskbar);
            var scale = new ScaleTransform(_entranceFromScale, _entranceFromScale);
            content.RenderTransform = scale;

            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = _backEaseAmplitude };
            var duration = new Duration(TimeSpan.FromMilliseconds(_scaleEntranceDurationMs));

            var scaleAnim = new DoubleAnimation
            {
                From = _entranceFromScale,
                To = 1.0,
                Duration = duration,
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop,
            };
            // Completion fires once (hooked on the Y animation only) to clear the transform
            // and hand control back to the caller.
            scaleAnim.Completed += (s, e) =>
            {
                content.RenderTransform = null;
                content.RenderTransformOrigin = new Point(0, 0);
                onCompleted(null, null);
            };

            if (SystemSettings.IsTransparencyEnabled)
            {
                window.Opacity = 0;
                var fade = new DoubleAnimation
                {
                    From = 0, To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(_entranceDurationMs)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop,
                };
                fade.Completed += (s, e) => window.Opacity = 1;
                window.BeginAnimation(UIElement.OpacityProperty, fade);
            }

            window.Cloak(false);

            // Bind X without a completion handler; Y carries the single completion callback.
            var scaleAnimX = scaleAnim.Clone();
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }

        private static Point GetScaleOriginForTaskbar(WindowsTaskbar.State taskbar)
        {
            switch (taskbar.Location)
            {
                case WindowsTaskbar.Position.Left: return new Point(0, 1);
                case WindowsTaskbar.Position.Right: return new Point(1, 1);
                case WindowsTaskbar.Position.Top: return new Point(0.5, 0);
                case WindowsTaskbar.Position.Bottom:
                default: return new Point(0.5, 1);
            }
        }

        public static void BeginFlyoutEntranceAnimation(Window window, WindowsTaskbar.State taskbar, Action completed)
        {
            var onCompleted = new EventHandler((s, e) =>
            {
                window.Topmost = true;
                window.Focus();
                User32.SetForegroundWindow(window.GetHandle());
                completed();
            });

            window.Topmost = false;
            window.Activate();
            BringTaskbarToFront();

            if (!Manager.Current.AnimationsEnabled)
            {
                window.Cloak(false);
                onCompleted(null, null);
                return;
            }

            var easingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

            var moveAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(_entranceDurationMs)),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = easingFunction
            };

            var fadeAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(_entranceDurationMs)),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = easingFunction,
                From = 0,
                To = 1
            };
            fadeAnimation.Completed += (s, e) => { window.Opacity = 1; };
            Storyboard.SetTarget(fadeAnimation, window);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(Window.OpacityProperty));

            double moveAnimationTo;
            switch (taskbar.Location)
            {
                case WindowsTaskbar.Position.Left:
                    moveAnimationTo = window.Left;
                    window.Left -= _animationOffset;
                    break;
                case WindowsTaskbar.Position.Right:
                    moveAnimationTo = window.Left;
                    window.Left += _animationOffset;
                    break;
                case WindowsTaskbar.Position.Top:
                    moveAnimationTo = window.Top;
                    window.Top -= _animationOffset;
                    break;
                case WindowsTaskbar.Position.Bottom:
                default:
                    moveAnimationTo = window.Top;
                    window.Top += _animationOffset;
                    break;
            }
            moveAnimation.To = moveAnimationTo;

            if (taskbar.Location == WindowsTaskbar.Position.Left || taskbar.Location == WindowsTaskbar.Position.Right)
            {
                Storyboard.SetTarget(moveAnimation, window);
                Storyboard.SetTargetProperty(moveAnimation, new PropertyPath(Window.LeftProperty));
                moveAnimation.From = window.Left;
            }
            else
            {
                Storyboard.SetTarget(moveAnimation, window);
                Storyboard.SetTargetProperty(moveAnimation, new PropertyPath(Window.TopProperty));
                moveAnimation.From = window.Top;
            }

            if (SystemSettings.IsTransparencyEnabled)
            {
                window.Opacity = 0;
            }

            window.Cloak(false);

            var storyboard = new Storyboard();
            storyboard.FillBehavior = FillBehavior.Stop;
            storyboard.Children.Add(moveAnimation);

            if (SystemSettings.IsTransparencyEnabled)
            {
                storyboard.Children.Add(fadeAnimation);
            }

            storyboard.Completed += (s, e) =>
            {
                if (taskbar.IsHorizontal)
                {
                    window.Top = moveAnimationTo;
                }
                else
                {
                    window.Left = moveAnimationTo;
                }
            };
            storyboard.Completed += onCompleted;

            storyboard.Begin(window);
        }

        public static void BeginFlyoutExitanimation(Window window, Action completed)
        {
            var onCompleted = new EventHandler((s, e) =>
            {
                window.Cloak();
                completed();
            });

            window.Topmost = false;

            if (!Manager.Current.AnimationsEnabled)
            {
                onCompleted(null, null);
                return;
            }

            var easingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };

            var moveAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(_exitDurationMs)),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = easingFunction
            };

            var fadeAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(_exitDurationMs)),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = easingFunction,
                From = 1,
                To = 0,
            };
            fadeAnimation.Completed += (s, e) => { window.Opacity = 0; };

            Storyboard.SetTarget(fadeAnimation, window);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(Window.OpacityProperty));

            var taskbarPosition = WindowsTaskbar.Current.Location;

            switch (taskbarPosition)
            {
                case WindowsTaskbar.Position.Left:
                    moveAnimation.To = window.Left - _animationOffset;
                    break;
                case WindowsTaskbar.Position.Right:
                    moveAnimation.To = window.Left + _animationOffset;
                    break;
                case WindowsTaskbar.Position.Top:
                    moveAnimation.To = window.Top - _animationOffset;
                    break;
                case WindowsTaskbar.Position.Bottom:
                default:
                    moveAnimation.To = window.Top + _animationOffset;
                    break;
            }

            if (taskbarPosition == WindowsTaskbar.Position.Left || taskbarPosition == WindowsTaskbar.Position.Right)
            {
                Storyboard.SetTarget(moveAnimation, window);
                Storyboard.SetTargetProperty(moveAnimation, new PropertyPath(Window.LeftProperty));
                moveAnimation.From = window.Left;
            }
            else
            {
                Storyboard.SetTarget(moveAnimation, window);
                Storyboard.SetTargetProperty(moveAnimation, new PropertyPath(Window.TopProperty));
                moveAnimation.From = window.Top;
            }

            var storyboard = new Storyboard();
            storyboard.FillBehavior = FillBehavior.Stop;
            storyboard.Children.Add(moveAnimation);

            if (SystemSettings.IsTransparencyEnabled)
            {
                storyboard.Children.Add(fadeAnimation);
            }

            storyboard.Completed += onCompleted;
            storyboard.Begin(window);
        }

        public static void BeginWindowExitAnimation(Window window, Action completed)
        {
            var onCompleted = new EventHandler((s, e) =>
            {
                window.Cloak();
                completed();
            });

            if (!Manager.Current.AnimationsEnabled || !SystemSettings.IsTransparencyEnabled)
            {
                window.Dispatcher.BeginInvoke((Action)(() =>
                {
                    onCompleted(null, null);
                }));
            }
            else
            {
                var fadeAnimation = new DoubleAnimation
                {
                    Duration = new Duration(TimeSpan.FromMilliseconds(_exitDurationMs)),
                    FillBehavior = FillBehavior.Stop,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
                    From = 1,
                    To = 0,
                };
                fadeAnimation.Completed += (s, e) => { window.Opacity = 0; };

                Storyboard.SetTarget(fadeAnimation, window);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(Window.OpacityProperty));

                var storyboard = new Storyboard();
                storyboard.FillBehavior = FillBehavior.Stop;
                storyboard.Children.Add(fadeAnimation);
                storyboard.Completed += onCompleted;
                storyboard.Begin(window);
            }
        }

        public static void BeginWindowEntranceAnimation(Window window, Action completed, double fromOpacity = 0)
        {
            var onCompleted = new EventHandler((s, e) =>
            {
                completed();
            });

            if (!Manager.Current.AnimationsEnabled)
            {
                window.Cloak(false);
                onCompleted(null, null);
                return;
            }

            var fadeAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(_entranceDurationMs)),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                From = fromOpacity,
                To = 1,
            };
            fadeAnimation.Completed += (s, e) => { window.Opacity = 1; };

            Storyboard.SetTarget(fadeAnimation, window);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(Window.OpacityProperty));

            var storyboard = new Storyboard();
            storyboard.FillBehavior = FillBehavior.Stop;
            storyboard.Children.Add(fadeAnimation);
            storyboard.Completed += onCompleted;

            window.Cloak(false);

            storyboard.Begin(window);
        }

        public static void BringTaskbarToFront()
        {
            User32.SetForegroundWindow(WindowsTaskbar.GetHwnd());
        }
    }
}

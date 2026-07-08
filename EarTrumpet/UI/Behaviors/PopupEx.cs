using EarTrumpet.DataModel;
using EarTrumpet.Interop;
using EarTrumpet.Interop.Helpers;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace EarTrumpet.UI.Behaviors
{
    public static class PopupEx
    {
        public static bool GetEnableAcrylic(DependencyObject obj) => (bool)obj.GetValue(EnableAcrylicProperty);
        public static void SetEnableAcrylic(DependencyObject obj, bool value) => obj.SetValue(EnableAcrylicProperty, value);

        public static readonly DependencyProperty EnableAcrylicProperty =
            DependencyProperty.RegisterAttached("EnableAcrylic", typeof(bool), typeof(PopupEx), new PropertyMetadata(false, EnableAcrylicChanged));

        private static void EnableAcrylicChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (!(dependencyObject is Popup popup))
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                popup.Opened += Popup_Opened;
            }
            else
            {
                popup.Opened -= Popup_Opened;
            }
        }

        private static void Popup_Opened(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            popup.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!SystemSettings.IsTransparencyEnabled || SystemParameters.HighContrast)
                {
                    return;
                }

                var themeTarget = popup.Child ?? (DependencyObject)popup;
                AccentPolicyLibrary.EnableAcrylic(
                    popup,
                    UI.Themes.Manager.Current.ResolveRef(themeTarget, "AcrylicColor_Flyout"),
                    User32.AccentFlags.None);
            }), DispatcherPriority.Loaded);
        }
    }
}

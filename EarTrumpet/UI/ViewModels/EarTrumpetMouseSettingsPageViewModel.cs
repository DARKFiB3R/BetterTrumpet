

namespace EarTrumpet.UI.ViewModels
{
    public class EarTrumpetMouseSettingsPageViewModel : SettingsPageViewModel
    {
        public bool UseScrollWheelInTray
        {
            get => _settings.UseScrollWheelInTray;
            set => _settings.UseScrollWheelInTray = value;
        }

        public bool UseGlobalMouseWheelHook
        {
            get => _settings.UseGlobalMouseWheelHook;
            set => _settings.UseGlobalMouseWheelHook = value;
        }

        // Logarithmic volume scaling (perceptual loudness). Merged here from the
        // former standalone "Community" page so all volume-adjustment behavior
        // lives in one place.
        public bool UseLogarithmicVolume
        {
            get => _settings.UseLogarithmicVolume;
            set => _settings.UseLogarithmicVolume = value;
        }

        // Volume tick sound effect
        public bool UseVolumeTickSound
        {
            get => _settings.UseVolumeTickSound;
            set => _settings.UseVolumeTickSound = value;
        }

        // Per-device L/R balance slider (only ever shows for 2-channel devices regardless)
        public bool ShowBalanceSlider
        {
            get => _settings.ShowBalanceSlider;
            set => _settings.ShowBalanceSlider = value;
        }

        // Hide Windows' native volume OSD when it would show a renamed device's real name
        public bool SuppressNativeOsdForRenamedDevices
        {
            get => _settings.SuppressNativeOsdForRenamedDevices;
            set => _settings.SuppressNativeOsdForRenamedDevices = value;
        }

        private readonly AppSettings _settings;

        public EarTrumpetMouseSettingsPageViewModel(AppSettings settings) : base(null)
        {
            _settings = settings;
            Title = Properties.Resources.VolumeMouseSettingsPageText;
            Subtitle = Properties.Resources.VolumeMouseSettingsPageSubtitle;
            Glyph = "\xE962";
        }
    }
}

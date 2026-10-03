using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace jnHodiny.Settings
{
    /// <summary>
    /// Nastavení aplikace jako obyčejná datová třída s výchozími hodnotami.
    /// Neví nic o registru ani o oknu - ukládání řeší <see cref="RegistrySettingsStore"/>.
    /// </summary>
    public class AppSettings
    {
        public const double MinFontSize = 10;
        public const double MaxFontSize = 350;
        public const double MinOpacity = 0.01;
        public const double MaxOpacity = 1.0;
        public const double MinCornerRadius = 0;
        public const double MaxCornerRadius = 300;

        // Poloha okna (v jednotkách WPF); omezení na viditelnou oblast monitorů řeší okno
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;

        public double FontSize { get; set; } = 36;
        public Color TextColor { get; set; } = Colors.Black;
        public Color BackgroundColor { get; set; } = Colors.White;
        public double CornerRadius { get; set; } = 20;
        public double Opacity { get; set; } = 1.0;

        /// <summary>Krátký formát času bez sekund (HH:mm).</summary>
        public bool ShortTimeFormat { get; set; } = false;
        public bool Use24HourFormat { get; set; } = true;

        public bool TransparentBackground { get; set; } = false;
        public bool TransparentText { get; set; } = false;

        public bool BellEnabled { get; set; } = false;
        public bool CountdownEnabled { get; set; } = false;
        public bool FiveMinuteWarning { get; set; } = false;

        /// <summary>Omezí číselné hodnoty na povolený rozsah (např. po ručně upraveném registru).</summary>
        public void Normalize()
        {
            FontSize = Math.Clamp(FontSize, MinFontSize, MaxFontSize);
            Opacity = Math.Clamp(Opacity, MinOpacity, MaxOpacity);
            CornerRadius = Math.Clamp(CornerRadius, MinCornerRadius, MaxCornerRadius);
        }
    }
}

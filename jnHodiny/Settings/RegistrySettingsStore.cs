using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace jnHodiny.Settings
{
    /// <summary>
    /// Čtení a ukládání <see cref="AppSettings"/> do HKCU\Software\jnHodiny.
    /// Názvy hodnot zůstávají stejné jako ve starších verzích aplikace (včetně překlepu
    /// "CoundownRinging"), aby si nové verze přečetly nastavení ze starých.
    /// </summary>
    public class RegistrySettingsStore
    {
        private const string KeyPath = @"Software\jnHodiny";

        private const string LeftName = "X";
        private const string TopName = "Y";
        private const string FontSizeName = "TextSize";
        private const string TextColorName = "TextColor";
        private const string BackgroundColorName = "BackgroundColor";
        private const string CornerRadiusName = "CornerRadius";
        private const string OpacityName = "Opacity";
        private const string ShortTimeFormatName = "TimeFormat";
        private const string Use24HourFormatName = "24 Format";
        private const string TransparentBackgroundName = "BackgroundTransparent";
        private const string TransparentTextName = "TextTransparent";
        private const string BellEnabledName = "Ringing";
        private const string CountdownEnabledName = "CoundownRinging";
        private const string FiveMinuteWarningName = "FiveMinuteWarning";

        /// <summary>
        /// Načte nastavení. Každá hodnota se čte samostatně, takže jedna chybná hodnota
        /// nezahodí ostatní. Při chybě nebo chybějícím klíči zůstanou výchozí hodnoty.
        /// </summary>
        public AppSettings Load()
        {
            var settings = new AppSettings();

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
                if (key == null) return settings;

                settings.Left = ReadDouble(key, LeftName, settings.Left);
                settings.Top = ReadDouble(key, TopName, settings.Top);
                settings.FontSize = ReadDouble(key, FontSizeName, settings.FontSize);
                settings.TextColor = ReadColor(key, TextColorName, settings.TextColor);
                settings.BackgroundColor = ReadColor(key, BackgroundColorName, settings.BackgroundColor);
                settings.CornerRadius = ReadDouble(key, CornerRadiusName, settings.CornerRadius);
                settings.Opacity = ReadDouble(key, OpacityName, settings.Opacity);

                settings.ShortTimeFormat = ReadBool(key, ShortTimeFormatName, settings.ShortTimeFormat);
                settings.Use24HourFormat = ReadBool(key, Use24HourFormatName, settings.Use24HourFormat);
                settings.TransparentBackground = ReadBool(key, TransparentBackgroundName, settings.TransparentBackground);
                settings.TransparentText = ReadBool(key, TransparentTextName, settings.TransparentText);
                settings.BellEnabled = ReadBool(key, BellEnabledName, settings.BellEnabled);
                settings.CountdownEnabled = ReadBool(key, CountdownEnabledName, settings.CountdownEnabled);
                settings.FiveMinuteWarning = ReadBool(key, FiveMinuteWarningName, settings.FiveMinuteWarning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při čtení nastavení z registru: {ex.Message}");
            }

            settings.Normalize();
            return settings;
        }

        /// <summary>
        /// Uloží nastavení. Při chybě (např. zakázaný zápis do registru) vyhodí výjimku -
        /// co s ní, rozhodne volající, třída sama nezobrazuje žádná okna.
        /// </summary>
        public void Save(AppSettings settings)
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);

            key.SetValue(LeftName, (int)settings.Left);
            key.SetValue(TopName, (int)settings.Top);

            // Desetinná čísla invariantně (s tečkou), aby nezáleželo na kultuře systému
            key.SetValue(FontSizeName, settings.FontSize.ToString(CultureInfo.InvariantCulture));
            key.SetValue(CornerRadiusName, settings.CornerRadius.ToString(CultureInfo.InvariantCulture));
            key.SetValue(OpacityName, settings.Opacity.ToString(CultureInfo.InvariantCulture));

            key.SetValue(TextColorName, ColorToInt(settings.TextColor));
            key.SetValue(BackgroundColorName, ColorToInt(settings.BackgroundColor));

            key.SetValue(ShortTimeFormatName, settings.ShortTimeFormat);
            key.SetValue(Use24HourFormatName, settings.Use24HourFormat);
            key.SetValue(TransparentBackgroundName, settings.TransparentBackground);
            key.SetValue(TransparentTextName, settings.TransparentText);
            key.SetValue(BellEnabledName, settings.BellEnabled);
            key.SetValue(CountdownEnabledName, settings.CountdownEnabled);
            key.SetValue(FiveMinuteWarningName, settings.FiveMinuteWarning);
        }

        // Hodnota z registru jako text (REG_DWORD i REG_SZ), null pokud neexistuje
        private static string? ReadString(RegistryKey key, string name)
        {
            return key.GetValue(name)?.ToString();
        }

        // Desetinné číslo: zkouší invariantní zápis (tečka) i zápis podle aktuální kultury (čárka),
        // starší verze ukládaly čísla podle kultury systému
        private static double ReadDouble(RegistryKey key, string name, double defaultValue)
        {
            string? text = ReadString(key, name);
            if (text == null) return defaultValue;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }
            return defaultValue;
        }

        // Logická hodnota: True/False, 1/0, a navíc formát času z WinForms verze ("HH:mm" = krátký formát) -
        // všechny verze aplikace sdílejí klíč HKCU\Software\jnHodiny a hodnota TimeFormat má v nich jiný význam
        private static bool ReadBool(RegistryKey key, string name, bool defaultValue)
        {
            string? text = ReadString(key, name);
            if (text == null) return defaultValue;

            if (bool.TryParse(text, out bool value)) return value;
            if (int.TryParse(text, out int number)) return number != 0;
            if (text == "HH:mm") return true;
            if (text == "HH:mm:ss") return false;
            return defaultValue;
        }

        // Barva uložená jako 32bitové číslo ARGB (REG_DWORD se může načíst jako záporné int)
        private static Color ReadColor(RegistryKey key, string name, Color defaultValue)
        {
            string? text = ReadString(key, name);
            if (text == null || !long.TryParse(text, out long number)) return defaultValue;

            uint argb = unchecked((uint)number);
            return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
        }

        private static int ColorToInt(Color color)
        {
            return unchecked((int)(((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B));
        }
    }
}

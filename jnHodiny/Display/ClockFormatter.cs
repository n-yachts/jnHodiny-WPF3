using System.Globalization;

namespace jnHodiny.Display
{
    /// <summary>Převod času a odpočtu na text zobrazený v hodinách.</summary>
    public static class ClockFormatter
    {
        public static string FormatTime(DateTime now, bool use24HourFormat, bool shortFormat)
        {
            string format = (use24HourFormat, shortFormat) switch
            {
                (true, true) => "HH:mm",
                (true, false) => "HH:mm:ss",
                (false, true) => "hh:mm tt",
                (false, false) => "hh:mm:ss tt"
            };
            return now.ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>Odpočet ve tvaru h:mm nebo h:mm:ss (hodiny bez úvodní nuly).</summary>
        public static string FormatCountdown(TimeSpan remaining, bool shortFormat)
        {
            string text = $"{remaining.Hours}:{remaining.Minutes:00}";
            return shortFormat ? text : $"{text}:{remaining.Seconds:00}";
        }

        /// <summary>Text s dvojtečkami nahrazenými mezerou - střídáním s původním textem dvojtečky blikají.</summary>
        public static string WithoutColons(string text) => text.Replace(':', ' ');
    }
}

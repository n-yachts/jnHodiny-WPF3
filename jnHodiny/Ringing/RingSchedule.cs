using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace jnHodiny.Ringing
{
    /// <summary>
    /// Rozvrh zvonění: seřazené časy během dne a z nich odvozené konce vyučovacích hodin.
    /// Čistá logika bez závislosti na oknu, zvuku nebo aktuálním čase - volající předává čas sám.
    /// </summary>
    public class RingSchedule
    {
        private static readonly TimeSpan Day = TimeSpan.FromHours(24);

        private readonly List<TimeSpan> _times;
        private readonly List<TimeSpan> _warningTimes;

        /// <param name="times">Časy zvonění (jako doba od půlnoci); duplicity a neplatné hodnoty se vynechají.</param>
        /// <param name="lessonLength">Délka vyučovací hodiny - určuje, které zvonění je konec hodiny.</param>
        /// <param name="warningLead">Jak dlouho před koncem hodiny se má upozornit.</param>
        public RingSchedule(IEnumerable<TimeSpan> times, TimeSpan lessonLength, TimeSpan warningLead)
        {
            _times = times
                .Where(t => t >= TimeSpan.Zero && t < Day)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            _warningTimes = BuildWarningTimes(_times, lessonLength, warningLead);
        }

        /// <summary>Rozvrh s hodinou 45 minut a upozorněním 5 minut před koncem.</summary>
        public RingSchedule(IEnumerable<TimeSpan> times)
            : this(times, TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(5))
        {
        }

        /// <summary>Načte časy ze souboru (jeden čas na řádek, např. 7:15). Chybí-li soubor, rozvrh je prázdný.</summary>
        public static RingSchedule LoadFromFile(string path)
        {
            var times = new List<TimeSpan>();

            try
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (TimeSpan.TryParse(line.Trim(), CultureInfo.InvariantCulture, out var time))
                        {
                            times.Add(time);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"Chyba při čtení rozvrhu zvonění: {ex.Message}");
            }

            return new RingSchedule(times);
        }

        public bool IsEmpty => _times.Count == 0;

        /// <summary>Časy zvonění seřazené vzestupně.</summary>
        public IReadOnlyList<TimeSpan> Times => _times;

        /// <summary>Časy upozornění (konec každé vyučovací hodiny minus předstih).</summary>
        public IReadOnlyList<TimeSpan> WarningTimes => _warningTimes;

        /// <summary>
        /// Doba do nejbližšího zvonění po zadaném čase; když už dnes žádné nezbývá,
        /// počítá se první zvonění zítřka. Rozvrh nesmí být prázdný.
        /// </summary>
        public TimeSpan TimeUntilNext(TimeSpan now)
        {
            if (IsEmpty) throw new InvalidOperationException("Rozvrh zvonění je prázdný.");

            foreach (var time in _times)
            {
                if (time > now) return time - now;
            }
            return Day - now + _times[0];
        }

        /// <summary>
        /// Zda mezi dvěma tiky (previous, current] padl čas zvonění.
        /// Nezávisí na tom, zda tik padl přesně do dané sekundy; zvládá i přechod přes půlnoc.
        /// </summary>
        public bool RingCrossed(TimeSpan previous, TimeSpan current) => Crossed(_times, previous, current);

        /// <summary>Zda mezi dvěma tiky (previous, current] padl čas upozornění.</summary>
        public bool WarningCrossed(TimeSpan previous, TimeSpan current) => Crossed(_warningTimes, previous, current);

        // Konec hodiny = zvonění, kterému o délku hodiny dřív předchází jiné zvonění (začátek hodiny)
        private static List<TimeSpan> BuildWarningTimes(List<TimeSpan> times, TimeSpan lessonLength, TimeSpan warningLead)
        {
            var warnings = new List<TimeSpan>();
            foreach (var end in times)
            {
                if (times.Contains(Wrap(end - lessonLength)))
                {
                    warnings.Add(Wrap(end - warningLead));
                }
            }
            warnings.Sort();
            return warnings;
        }

        private static bool Crossed(List<TimeSpan> times, TimeSpan previous, TimeSpan current)
        {
            foreach (var t in times)
            {
                bool crossed = current >= previous
                    ? (t > previous && t <= current)
                    : (t > previous || t <= current); // tik přes půlnoc
                if (crossed) return true;
            }
            return false;
        }

        // Převede čas do rozsahu jednoho dne (po odečtení dojde k záporné hodnotě kolem půlnoci)
        private static TimeSpan Wrap(TimeSpan time) => time < TimeSpan.Zero ? time + Day : time;
    }
}

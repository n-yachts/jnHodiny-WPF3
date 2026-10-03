using System.Diagnostics;
using System.IO;
using System.Media;

namespace jnHodiny.Audio
{
    /// <summary>
    /// Přehrávání zvuku zvonění (bell.wav) a upozornění 5 minut před koncem hodiny (ding.wav).
    /// Chybí-li soubor nebo se přehrání nepodaří, zazní vestavěný zvuk Windows, takže se nikdy nezvoní naprázdno.
    /// </summary>
    public sealed class BellPlayer : IDisposable
    {
        private readonly string _bellPath;
        private readonly string _warningPath;
        private SoundPlayer? _bellPlayer;
        private SoundPlayer? _warningPlayer;

        /// <param name="directory">Složka se zvuky, obvykle složka s aplikací.</param>
        public BellPlayer(string directory)
        {
            _bellPath = Path.Combine(directory, "bell.wav");
            if (!File.Exists(_bellPath))
            {
                // Náhradní systémový zvuk, když vedle aplikace žádný zvonek není
                _bellPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "Alarm01.wav");
            }

            _warningPath = Path.Combine(directory, "ding.wav");
        }

        public void PlayBell() => Play(ref _bellPlayer, _bellPath, SystemSounds.Exclamation);

        public void PlayWarning() => Play(ref _warningPlayer, _warningPath, SystemSounds.Asterisk);

        private static void Play(ref SoundPlayer? player, string path, SystemSound fallback)
        {
            try
            {
                if (File.Exists(path))
                {
                    player ??= new SoundPlayer(path);
                    player.Play();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při přehrávání zvuku: {ex.Message}");
            }

            fallback.Play();
        }

        public void Dispose()
        {
            _bellPlayer?.Dispose();
            _warningPlayer?.Dispose();
        }
    }
}

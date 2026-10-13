using Microsoft.Win32;

namespace jnHodiny.Startup
{
    /// <summary>
    /// Spuštění aplikace při přihlášení do Windows (HKCU\...\Run). Stav se čte přímo z registru,
    /// takže zůstane správný i po přesunutí nebo ruční úpravě.
    /// </summary>
    public static class AutostartManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "jnHodiny";

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            string? value = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(value) &&
                   string.Equals(value.Trim('"'), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }

        public static void SetEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
            {
                string? path = Environment.ProcessPath;
                if (path != null) key.SetValue(ValueName, $"\"{path}\"");
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }
        }
    }
}

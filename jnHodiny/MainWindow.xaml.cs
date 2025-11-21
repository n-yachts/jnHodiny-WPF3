using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Forms;

namespace jnHodiny
{
    public partial class MainWindow : Window
    {
    //    private DispatcherTimer _timer;
    //    private NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
        }

        /* 
            Obsluha událostí kliknutí v menu
        */
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Místo zavření pouze skryjeme okno
            e.Cancel = true;
            this.Hide();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseApplication();
        }

        private void ToggleWindowVisibility_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementovat skrytí/zobrazení okna
        }

        private void TransparentBackground_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementovat průhlednost pozadí
        }

        private void TransparentText_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementovat průhlednost textu
        }

        private void DefaultView_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementovat obnovení výchozího nastavení
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementovat zobrazení nápovědy
            MessageBox.Show(
                "Nápověda k aplikaci jnHodiny\n\n" +
                "• Přesouvání: Klikněte a táhněte levým tlačítkem myši\n" +
                "• kolečko myši nastavuje velikost textu\n" +
                "• Shift + kolečko myši nastavuje barvu textu\n" +
                "• Ctrl + kolečko myši nastavuje barvu pozadí\n" +
                "• Alt + kolečko myši nastavuje průhlednost aplikace\n" +
                "• Ctrl + Shift + kolečko myši nastavuje zaoblení rohů\n" +
                "• Kontextové menu: Pravé tlačítko myši\n" +
                "• Automatické přichycení k okrajům obrazovky",
                "Nápověda",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /* 
            Nastavení, načtení a uložení nastavení
        */

        private void LoadWindowSettings()
        {
            // TODO: Implementovat načtení nastavení
        }

        private void SaveWindowSettings()
        {
            // TODO: Implementovat ukládání nastavení
        }

        private void SetDefaultValues()
        {
            // TODO: Implementovat výchozí nastavení
        }

        /* 
            Jednotlivé funkce
        */
        private void CloseApplication()
        {
            SaveWindowSettings();
        //    _notifyIcon.Visible = false;
        //    _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }


    }
}
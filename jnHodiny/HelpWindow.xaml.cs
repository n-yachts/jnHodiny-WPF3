using System.Windows;

namespace jnHodiny
{
    /// <summary>Jedna položka nápovědy: ovládací prvek (nebo kombinace kláves) a jeho popis.</summary>
    public record HelpItem(string Key, string Description);

    /// <summary>Skupina souvisejících položek nápovědy.</summary>
    public record HelpSection(string Title, string Intro, IReadOnlyList<HelpItem> Items);

    /// <summary>Okno nápovědy; obsah je data (seznam sekcí), vzhled určují šablony v XAML.</summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            SectionsList.ItemsSource = CreateSections();
        }

        private static HelpSection[] CreateSections() =>
        [
            new("Ovládání myší", "Hodiny nemají žádná tlačítka – vše se nastavuje přímo myší.",
            [
                new("Tažení levým tlačítkem", "Přesune hodiny. Poblíž okraje obrazovky se samy přichytí; poloha se pamatuje."),
                new("Kolečko", "Mění velikost textu (a tím celého okna)."),
                new("Shift + kolečko", "Mění barvu textu (světlejší / tmavší)."),
                new("Ctrl + kolečko", "Mění barvu pozadí (světlejší / tmavší)."),
                new("Alt + kolečko", "Mění průhlednost celého okna."),
                new("Ctrl + Shift + kolečko", "Mění zaoblení rohů."),
                new("Pravé tlačítko", "Otevře menu s nastavením (viz níže)."),
            ]),

            new("Menu", "Totéž menu najdete i u ikony v oznamovací oblasti Windows.",
            [
                new("Zavřít", "Uloží nastavení a ukončí aplikaci."),
                new("Zobrazit/Skrýt", "Schová hodiny nebo je znovu zobrazí; aplikace běží dál. Stejně funguje dvojklik na ikonu v oznamovací oblasti."),
                new("Formát času HH:MM", "Zobrazí čas bez sekund; dvojtečka přitom v sekundovém rytmu bliká."),
                new("24h formát", "Přepíná mezi 24hodinovým a 12hodinovým zobrazením."),
                new("Průhledné pozadí", "Zruší barvu pozadí, zůstane jen text. Vylučuje se s průhledným textem."),
                new("Průhledný text", "Text se z barevného pozadí „vykousne“ a prosvítá jím plocha. Vylučuje se s průhledným pozadím."),
                new("Výchozí zobrazení", "Vrátí velikost, barvy, průhlednost, zaoblení, formát i polohu na původní hodnoty."),
                new("Nápověda", "Zobrazí toto okno."),
            ]),

            new("Zvonění", "Tyto položky se v menu objeví, jen když je vedle aplikace soubor ringing.txt s rozvrhem.",
            [
                new("Zvonění", "V zadaných časech zazní zvonění. Zapnuté zvonění je poznat podle ikony zvonku za časem."),
                new("5 minut", "Pět minut před koncem vyučovací hodiny (45 min) zazní upozornění a pod zvonkem se objeví „-5“. Nabízí se jen při zapnutém zvonění."),
                new("Odpočet zvonění", "Místo aktuálního času ukazuje, za jak dlouho zazvoní příště (po posledním zvonění dne počítá k prvnímu zítřejšímu)."),
            ]),

            new("Soubory vedle aplikace", "Všechny jsou nepovinné.",
            [
                new("ringing.txt", "Rozvrh zvonění – jeden čas na řádek, např. 7:15 nebo 8:05. Načítá se při spuštění."),
                new("bell.wav", "Zvuk zvonění. Chybí-li, zazní systémový zvuk Windows."),
                new("ding.wav", "Zvuk upozornění 5 minut před koncem hodiny. Chybí-li, zazní systémový zvuk."),
            ]),

            new("Tipy", "",
            [
                new("Zavření okna", "Zavření okna hodiny jen skryje; naplno se aplikace ukončuje volbou Zavřít v menu."),
                new("Ukládání", "Nastavení i poloha okna se ukládají do registru a při dalším spuštění se obnoví."),
                new("Více monitorů", "Hodiny se přichytávají k okrajům monitoru, na kterém právě leží."),
            ]),
        ];

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

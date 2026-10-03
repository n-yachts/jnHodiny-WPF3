using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Separator = System.Windows.Controls.Separator;

namespace jnHodiny.Tray
{
    /// <summary>
    /// Ikona v oznamovací oblasti s menu, které zrcadlí kontextové menu okna (WPF ContextMenu).
    /// Menu se postaví jednou; zaškrtnutí, viditelnost a povolení se před každým otevřením načtou z WPF menu.
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly List<(MenuItem Source, ToolStripMenuItem Item)> _items = new();

        public TrayIcon(string text, ContextMenu source, Action toggleVisibility)
        {
            var strip = new ContextMenuStrip();

            foreach (var entry in source.Items)
            {
                if (entry is Separator)
                {
                    strip.Items.Add(new ToolStripSeparator());
                }
                else if (entry is MenuItem menuItem)
                {
                    var item = CreateItem(menuItem);
                    _items.Add((menuItem, item));
                    strip.Items.Add(item);
                }
            }

            // Menu se zobrazí samo při kliknutí pravým tlačítkem; stav se aktualizuje těsně před otevřením
            strip.Opening += (s, e) => Refresh();

            _notifyIcon = new NotifyIcon
            {
                Icon = LoadIcon(),
                Text = text,
                ContextMenuStrip = strip,
                Visible = true
            };
            _notifyIcon.DoubleClick += (s, e) => toggleVisibility();
        }

        private static ToolStripMenuItem CreateItem(MenuItem source)
        {
            var item = new ToolStripMenuItem(source.Header?.ToString());

            if (source.IsCheckable)
            {
                item.CheckOnClick = true;
                // Změna IsChecked vyvolá v okně stejné události jako kliknutí v jeho vlastním menu
                item.Click += (s, e) => source.IsChecked = item.Checked;
            }
            else
            {
                item.Click += (s, e) => source.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }

            return item;
        }

        private void Refresh()
        {
            foreach (var (source, item) in _items)
            {
                if (source.IsCheckable) item.Checked = source.IsChecked;
                item.Enabled = source.IsEnabled;
                item.Visible = source.Visibility == Visibility.Visible;
            }
        }

        private static System.Drawing.Icon LoadIcon()
        {
            string? path = Environment.ProcessPath;
            if (path != null)
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon != null) return icon;
            }
            return System.Drawing.SystemIcons.Application;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}

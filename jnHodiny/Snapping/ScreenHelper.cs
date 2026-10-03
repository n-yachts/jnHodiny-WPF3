using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using Rect = System.Windows.Rect;

namespace jnHodiny.Snapping
{
    /// <summary>Zjištění pracovní plochy monitoru, na kterém okno leží (v jednotkách WPF).</summary>
    public static class ScreenHelper
    {
        /// <summary>
        /// Pracovní plocha (monitor bez hlavního panelu) pro zadané okno. Třída Screen pracuje v pixelech,
        /// WPF v jednotkách nezávislých na DPI, proto se výsledek převede podle DPI okna.
        /// Před zobrazením okna (bez systémového handle) se monitor určí podle středu okna.
        /// </summary>
        public static Rect GetWorkArea(Window window)
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(window);
            IntPtr handle = new WindowInteropHelper(window).Handle;

            Screen screen;
            if (handle != IntPtr.Zero)
            {
                screen = Screen.FromHandle(handle);
            }
            else
            {
                // Pro bod mimo všechny monitory vrátí Screen nejbližší monitor
                int centerX = (int)Math.Round((window.Left + window.ActualWidth / 2) * dpi.DpiScaleX);
                int centerY = (int)Math.Round((window.Top + window.ActualHeight / 2) * dpi.DpiScaleY);
                screen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
            }

            var area = screen.WorkingArea;
            return new Rect(
                area.Left / dpi.DpiScaleX,
                area.Top / dpi.DpiScaleY,
                area.Width / dpi.DpiScaleX,
                area.Height / dpi.DpiScaleY);
        }
    }
}

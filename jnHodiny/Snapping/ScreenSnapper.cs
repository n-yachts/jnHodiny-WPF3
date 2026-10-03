using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Size = System.Windows.Size;

namespace jnHodiny.Snapping
{
    /// <summary>Nová poloha okna po přichycení a okraje, ke kterým se přichytilo.</summary>
    public readonly record struct SnapResult(Point Position, SnapState State);

    /// <summary>
    /// Přichytávání okna k okrajům pracovní plochy monitoru. Čistá logika nad obdélníky
    /// (v jednotkách WPF) - nezná okno ani monitor, takže se dá snadno testovat.
    /// </summary>
    public static class ScreenSnapper
    {
        /// <summary>
        /// Přichytí okno k okraji, který je blíž než <paramref name="distance"/>.
        /// Vodorovně má přednost levý okraj, svisle horní (jako ve starších verzích).
        /// </summary>
        public static SnapResult Snap(Rect window, Rect workArea, double distance)
        {
            double left = window.Left;
            double top = window.Top;
            bool snapLeft = false, snapRight = false, snapTop = false, snapBottom = false;

            if (Math.Abs(window.Left - workArea.Left) <= distance)
            {
                left = workArea.Left;
                snapLeft = true;
            }
            else if (Math.Abs(workArea.Right - window.Right) <= distance)
            {
                left = workArea.Right - window.Width;
                snapRight = true;
            }

            if (Math.Abs(window.Top - workArea.Top) <= distance)
            {
                top = workArea.Top;
                snapTop = true;
            }
            else if (Math.Abs(workArea.Bottom - window.Bottom) <= distance)
            {
                top = workArea.Bottom - window.Height;
                snapBottom = true;
            }

            return new SnapResult(new Point(left, top), new SnapState(snapLeft, snapRight, snapTop, snapBottom));
        }

        /// <summary>
        /// Poloha a velikost okna po změně velikosti (např. jiný text nebo písmo) tak, aby zůstalo
        /// přichycené: u pravého a dolního okraje se okno roztahuje směrem dovnitř obrazovky.
        /// Je-li přichycené k oběma protilehlým okrajům, zachová se aktuální velikost v dané ose.
        /// </summary>
        public static Rect Resize(SnapState state, Rect workArea, Rect current, Size desiredSize)
        {
            double width = state.Left && state.Right ? current.Width : desiredSize.Width;
            double height = state.Top && state.Bottom ? current.Height : desiredSize.Height;

            double left = current.Left;
            double top = current.Top;

            if (state.Right && !state.Left) left = workArea.Right - width;
            if (state.Bottom && !state.Top) top = workArea.Bottom - height;

            return new Rect(left, top, width, height);
        }
    }
}

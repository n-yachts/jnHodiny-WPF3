using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace jnHodiny.Display
{
    /// <summary>
    /// Maska průhlednosti pro režim "průhledný text": celá plocha okna s vyříznutým tvarem písmen,
    /// takže text propouští to, co je za oknem.
    /// </summary>
    public static class TextMask
    {
        /// <summary>Jeden text masky: obsah, písmo, velikost a levý horní roh v souřadnicích okna.</summary>
        public record Item(string Text, Typeface Typeface, double FontSize, Point Origin);

        public static Brush Create(IEnumerable<Item> items, Size windowSize, double cornerRadius, double pixelsPerDip)
        {
            var windowGeometry = new RectangleGeometry(
                new Rect(0, 0, windowSize.Width, windowSize.Height), cornerRadius, cornerRadius);

            Geometry textGeometry = Geometry.Empty;
            foreach (var item in items)
            {
                var formattedText = new FormattedText(
                    item.Text,
                    CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    item.Typeface,
                    item.FontSize,
                    Brushes.Black,
                    pixelsPerDip);

                textGeometry = new CombinedGeometry(GeometryCombineMode.Union, textGeometry,
                    formattedText.BuildGeometry(item.Origin));
            }

            var combined = new CombinedGeometry(GeometryCombineMode.Exclude, windowGeometry, textGeometry);

            // Pro OpacityMask rozhoduje jen průhlednost: plná plocha okna s "vyříznutým" textem
            var brush = new DrawingBrush(new GeometryDrawing(Brushes.Black, null, combined));
            brush.Freeze();
            return brush;
        }
    }
}

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
        public static Brush Create(string text, Typeface typeface, double fontSize, Size windowSize,
            double cornerRadius, double pixelsPerDip)
        {
            var windowGeometry = new RectangleGeometry(
                new Rect(0, 0, windowSize.Width, windowSize.Height), cornerRadius, cornerRadius);

            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            // Text je v okně vystředěný
            Geometry textGeometry = formattedText.BuildGeometry(new Point(
                (windowSize.Width - formattedText.Width) / 2,
                (windowSize.Height - formattedText.Height) / 2));

            var combined = new CombinedGeometry(GeometryCombineMode.Exclude, windowGeometry, textGeometry);

            // Pro OpacityMask rozhoduje jen průhlednost: plná plocha okna s "vyříznutým" textem
            var brush = new DrawingBrush(new GeometryDrawing(Brushes.Black, null, combined));
            brush.Freeze();
            return brush;
        }
    }
}

using System.Windows;
using System.Windows.Media;

namespace Drawing2D
{
    class ElementHostVisual : DrawingVisual
    {
        public DrawingElement Element { get; set; }
        public Geometry RawGeometry { get; set; }
        public Geometry Geometry { get; set; }
        public Rect RowBound { get; internal set; }
    }
}

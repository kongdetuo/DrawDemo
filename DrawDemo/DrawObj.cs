using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DrawDemo
{
    public abstract class DrawObj
    {
        /// <summary>
        /// 填充色
        /// </summary>
        public Brush FillColor { get; set; }

        /// <summary>
        /// 描边色
        /// </summary>
        public Brush BorderColor { get; set; }


        public abstract Geometry GetGeometry();

        public void Move()
        {

        }
    }

    public class RectObj : DrawObj
    {
        public RectObj(Point start, Point end)
        {
            this.Start = start;
            this.End = end;
            this.FillColor = Brushes.Gray;
            this.BorderColor = Brushes.Black;
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }

        public override Geometry GetGeometry()
        {
            return new RectangleGeometry(new Rect(Start, End));
        }
    }
    public class LineObj : DrawObj
    {
        public LineObj(Point start, Point end)
        {
            this.Start = start;
            this.End = end;
            this.FillColor = Brushes.Gray;
            this.BorderColor = Brushes.Black;
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }

        public override Geometry GetGeometry()
        {
            return new LineGeometry(Start, End);
        }
    }
    internal class DrawObjVisual : DrawingVisual
    {
        public DrawObjVisual()
        {
        }

        public DrawObjVisual(DrawObj draw)
        {
            this.Object = draw;
        }

        public DrawObj Object { get; set; }
    }
}

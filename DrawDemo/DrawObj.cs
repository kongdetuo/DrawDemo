using System.Collections.Generic;
using System.Linq;
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
        /// 边线
        /// </summary>
        public Pen Pen { get; set; }

        public double PenThickness { get; } = 1;

        public List<DrawObj> Childs = new List<DrawObj>();

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
            this.Pen = new Pen(Brushes.Black, 1);
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }
        public RectangleGeometry Geometry { get; private set; }

        public override Geometry GetGeometry()
        {
            if (this.Geometry == null)
            {
                this.Geometry = new RectangleGeometry(new Rect(Start, End));
            }
            return Geometry;
        }
    }

    public class CycleObj : DrawObj
    {
        public CycleObj(Point center, double r)
        {
            this.Center = center;
            this.R = r;
   
            //this.FillColor = Brushes.Gray;
            this.Pen = new Pen(Brushes.Black, 1);
        }

        public Point Center { get; private set; }
        public double R { get; private set; }
       
        public EllipseGeometry Geometry { get; private set; }

        public override Geometry GetGeometry()
        {
            if (this.Geometry == null)
            {
                this.Geometry = new EllipseGeometry(Center, R,R);
                this.Geometry.Freeze();
            }
            return Geometry;
        }
    }
    public class LineObj : DrawObj
    {
        public LineObj(Point start, Point end)
        {
            this.Start = start;
            this.End = end;
            this.FillColor = Brushes.Gray;
            this.Pen = new Pen(Brushes.Black, PenThickness);
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }

        public override Geometry GetGeometry()
        {
            return new LineGeometry(Start, End);
        }
    }

    public sealed class TextObj : DrawObj
    {
        public TextObj(Point point, string text)
        {
            this.Text = text;
            this.Location = point;
            this.FillColor = Brushes.Black;
            this.FormatText = new FormattedText(Text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                16,
                this.FillColor,
                1);
        }

        public string Text { get; }

        public Point Location { get; set; }
        public FormattedText FormatText { get; private set; }

        private Geometry geometry;

        private static Typeface typeface = new Typeface(
                    fontFamily: new FontFamily("宋体"),
                    style: FontStyles.Normal,
                    weight: FontWeights.Normal,
                    stretch: FontStretches.Normal);

        private Geometry getGeometry()
        {
            var formatText = new FormattedText(Text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                16,
                this.FillColor,
                1);
            var geo = formatText.BuildGeometry(Location);
            //geo.Transform = new RotateTransform(45, Location.X, Location.Y);
            return geo;
        }

        public override Geometry GetGeometry()
        {
            if (geometry == null)
                geometry = getGeometry();
            return geometry;
        }
    }

    internal class SelectionRectObj : DrawObj
    {
        public SelectionRectObj(Point start, Point end)
        {
            this.Start = start;
            this.End = end;
            this.Pen = new Pen(Brushes.Black, PenThickness);
            if (end.X < start.X)
            {
                Pen.DashStyle = DashStyles.Dash;
            }
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }

        public override Geometry GetGeometry()
        {
            return new RectangleGeometry(new Rect(Start, End));
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
            this.Brush = draw.FillColor;
            this.Pen = draw.Pen;
        }

        public DrawObj Object { get; set; }

        public Geometry Geometry { get; set; }

        public Brush Brush { get; set; }

        public Pen Pen { get; set; }
    }
    internal class MultiDrawObjVisual : DrawObjVisual
    {
        private GeometryGroup Group = new GeometryGroup();
        public MultiDrawObjVisual()
        {
            Group = new GeometryGroup();
            this.Geometry = Group;
        }

        public void UpdateObjs(IEnumerable<Geometry> geometries)
        {
            //var set = geometries.ToHashSet();
            //var oldSet = Group.Children.ToHashSet();
            //for (int i = Group.Children.Count - 1; i >= 0; i--)
            //{
            //    if (!set.Contains(Group.Children[i]))
            //        Group.Children.RemoveAt(i);
            //}

            //foreach (var item in set)
            //{
            //    if (!oldSet.Contains(item))
            //        Group.Children.Add(item);
            //}
            Group.Children = new GeometryCollection(geometries);
        }
    }

    public static class Ex
    {
        public static T MakeFreeze<T>(this T freezable)
            where T : Freezable
        {
            freezable.Freeze();
            return freezable;
        }

        public static Geometry GetTransformed(this Geometry geometry, Transform transform)
        {
            Transform transform2 = geometry.Transform;
            geometry = geometry.Clone();
            if (transform != null && !transform.Value.IsIdentity)
            {
                if (transform2 == null || transform2.Value.IsIdentity)
                {
                    geometry.Transform = transform;
                }
                else
                {
                    geometry.Transform = new MatrixTransform(transform2.Value * transform.Value);
                }
            }

            return geometry;
            //return Geometry.Combine(Geometry.Empty, geometry, GeometryCombineMode.Union, transform);
        }

        public static Matrix GetInverse(this Matrix matrix)
        {
            var m = Matrix.Identity * matrix;
            m.Invert();
            return m;
        }

        public static GeometryDrawing CreateDrawing(this DrawObj obj)
        {
            var geo = new GeometryGroup();
            geo.Children.Add(obj.GetGeometry().MakeFreeze());
            return new GeometryDrawing(obj.FillColor, obj.Pen, geo);
        }
    }
}

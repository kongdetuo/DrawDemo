using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Drawing2D
{
    public abstract class DrawingElement
    {
        public DrawingElement(DrawingDocument document)
        {
            this.Document = document;
            this.Id = document.IDManager.NewID();
            document.Add(this);
        }

        private List<int> childs = new List<int>();

        public DrawingDocument Document { get; }

        public int Id { get; }

        public Brush? FillBrush { get; set; }

        public Pen? Pen { get; set; }

        public int ParentId { get; private set; } = -1;

        public IReadOnlyList<int> ChildIds => this.childs;

        public void AddChild(DrawingElement child)
        {
            child.ParentId = this.Id;
            this.childs.Add(child.Id);
            Document.Update(this);
            Document.Add(child);
        }

        public void RemoveChild(DrawingElement child)
        {
            this.childs.Remove(child.Id);
            Document.Update(this);
            Document.Remove(child);
        }

        public virtual string GetTip()
        {
            return "";
        }

        public abstract Geometry GetGeometry();

        public DrawingElement Clone()
        {
            var instance = this.MemberwiseClone() as DrawingElement;
            instance.childs = new List<int>(childs);
            return instance;
        }
    }



    public class RectObj : DrawingElement
    {
        public RectObj(DrawingDocument document, Point start, Point end):base(document)
        {
            this.Start = start;
            this.End = end;
            this.FillBrush = Brushes.Gray;
            this.Pen = new Pen(Brushes.Black, 1);
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }
        public RectangleGeometry? Geometry { get; private set; }

        public override Geometry GetGeometry()
        {
            if (this.Geometry == null)
            {
                this.Geometry = new RectangleGeometry(new Rect(Start, End));
            }
            return Geometry;
        }
    }

    public class CycleObj : DrawingElement
    {
        public CycleObj(DrawingDocument document, Point center, double r) : base(document)
        {
            this.Center = center;
            this.R = r;

            //this.FillColor = Brushes.Gray;
            this.Pen = new Pen(Brushes.Black, 1);
        }

        public Point Center { get; private set; }
        public double R { get; private set; }

        public Geometry Geometry { get; private set; }

        public override Geometry GetGeometry()
        {
            if (this.Geometry == null)
            {
                //this.Geometry = new EllipseGeometry(Center, R, R);
                //this.Geometry.Freeze();
                var g1 = new EllipseGeometry(Center, R, R);
                var g2 = new EllipseGeometry(Center, R - 2, R - 2);
                this.Geometry = Geometry.Combine(g1, g2, GeometryCombineMode.Exclude, null);
            }




            return Geometry;
        }
    }
    public class LineObj : DrawingElement
    {
        public LineObj(DrawingDocument document, Point start, Point end) : base(document)
        {
            this.Start = start;
            this.End = end;
            this.FillBrush = Brushes.Gray;
            this.Pen = new Pen(Brushes.Black, 1);
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }

        public override Geometry GetGeometry()
        {
            return new LineGeometry(Start, End);
        }
    }

    public sealed class TextObj : DrawingElement
    {
        public TextObj(DrawingDocument document, Point point, string text) : base(document)
        {
            this.Text = text;
            this.Location = point;
            this.FillBrush = Brushes.Black;
            this.FormatText = new FormattedText(Text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                16,
                this.FillBrush,
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
                this.FillBrush,
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

    internal class SelectionRectObj : DrawingElement
    {
        public SelectionRectObj(DrawingDocument document, Point start, Point end) : base(document)
        {
            this.Start = start;
            this.End = end;
            this.Pen = new Pen(Brushes.Black, 1);
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

        public DrawObjVisual(DrawingElement draw)
        {
            this.Object = draw;
            this.Brush = draw.FillBrush;
            this.Pen = draw.Pen;
            this.RawGeometry = draw.GetGeometry();
        }

        public DrawingElement Object { get; set; }

        public Geometry Geometry { get; set; }

        public Geometry RawGeometry { get; set; }

        public Brush Brush { get; set; }

        public Pen Pen { get; set; }

        internal bool Changed { get; set; }

        private Rect rect;

        internal Rect Rect
        {
            get
            {
                if (this.Changed)
                {
                    rect = this.Geometry.Bounds;
                    Changed = false;
                }
                return rect;
            }
        }
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

        public static GeometryDrawing CreateDrawing(this DrawingElement obj)
        {
            var geo = new GeometryGroup();
            geo.Children.Add(obj.GetGeometry().MakeFreeze());
            return new GeometryDrawing(obj.FillBrush, obj.Pen, geo);
        }
    }
}

﻿using System.Collections.Generic;
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
            this.Pen = new Pen(Brushes.Black, PenThickness);
        }

        public Point Start { get; private set; }
        public Point End { get; private set; }
        public RectangleGeometry Geometry { get; private set; }

        public override Geometry GetGeometry()
        {
            if (this.Geometry == null)
            {
                this.Geometry = new RectangleGeometry(new Rect(Start, End));
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
            this.FormatText= new FormattedText(Text,
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
        }

        public DrawObj Object { get; set; }
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
            return Geometry.Combine(Geometry.Empty, geometry, GeometryCombineMode.Union, transform);
        }
    }
}

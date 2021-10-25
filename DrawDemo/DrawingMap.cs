using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace DrawDemo
{
    public class DrawingMap : UserControl
    {
        private TextBlock text = new TextBlock()
        {
            Background = Brushes.White
        };

        public DrawingMap()
        {
            var drawingHost = new DrawingCanvas();
            var grid = new Grid();
            this.Content = grid;
            grid.Children.Add(drawingHost);
            this.DrawingHost = drawingHost;
            this. topCanvas = new Canvas();
            grid.Children.Add(topCanvas);
            topCanvas.Children.Add(text);
            this.DrawingHost.SnapsToDevicePixels = true;

            this.ScaleTransform = new ScaleTransform(1, -1);
            this.TranslateTransform = new TranslateTransform();
            this.Transform = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    ScaleTransform,
                    TranslateTransform
                }
            };


            this.ElementLayer = drawingHost.AddDrawingLayer();
            this.TagLayer = drawingHost.AddDrawingLayer();
            this.PreviewSelectLayer = drawingHost.AddDrawingLayer();
            this.PreviewLayer = drawingHost.AddDrawingLayer();

            SelectObjectVisual = new MultiDrawObjVisual()
            {
                Effect = new DropShadowEffect()
                {
                    Color = Colors.Black,
                    ShadowDepth = -1,
                },
                Pen = new Pen(Brushes.Red, 2)
                //Transform = TranslateTransform
            };
            SelectionRectVisual = new DrawObjVisual();
            PreviewVisual = new DrawObjVisual();
            testVisual = new DrawingVisual();
            //SelectionRectVisual.Transform = TranslateTransform;
            //PreviewVisual.Transform = TranslateTransform;
            //testVisual.Transform = TranslateTransform;
            drawingHost.AddVisual(this.PreviewLayer, testVisual);
            drawingHost.AddVisual(this.PreviewLayer, PreviewVisual);
            drawingHost.AddVisual(this.PreviewSelectLayer, SelectObjectVisual);
            drawingHost.AddVisual(this.PreviewSelectLayer, SelectionRectVisual);

            this.PreviewGeometry = new GeometryGroup();
            this.PreviewGeometry.Transform = this.Transform;
            PreviewVisual.Geometry = this.PreviewGeometry;
            Draw(PreviewVisual);
            Draw(SelectObjectVisual);

            new TestPerformance(this, this.DrawingHost, this.Transform);

            this.Loaded += DrawingMap_Loaded;

        }

        private void DrawingMap_Loaded(object sender, RoutedEventArgs e)
        {
            //this.Matrix.Translate(0, this.ActualHeight);
            this.Translate += new Vector(0, this.ActualHeight);
        }

        public DrawingLayer ElementLayer { get; private set; }
        public DrawingLayer TagLayer { get; private set; }
        public DrawingLayer PreviewSelectLayer { get; private set; }
        public DrawingLayer PreviewLayer { get; private set; }


        private List<DrawObjVisual> Selection = new List<DrawObjVisual>();
        private List<DrawObjVisual> visuals = new List<DrawObjVisual>();
        private Vector translate;

        private Point Start;
        private Point End;
        private bool CanvasMoving;
        private bool IsMouseDown;
        private bool ObjectMoving;
        private bool Selecting;

        private bool CtrlDown;

        private MultiDrawObjVisual SelectObjectVisual;
        private DrawObjVisual SelectionRectVisual;
        private DrawObjVisual PreviewVisual;

        public ScaleTransform ScaleTransform { get; private set; }
        GuidelineSet guidelineSet = new GuidelineSet();

        public double Scale
        {
            get => scale;
            set
            {
                scale = value;
                this.ScaleTransform.ScaleX = value;
                this.ScaleTransform.ScaleY = -value;


                //var xs = new HashSet<double>();
                //var ys = new HashSet<double>();
                //foreach (var item in this.visuals)
                //{
                //    if (item.Drawing != null)
                //    {
                //        var rect = this.Transform.TransformBounds(item.Object.GetGeometry().Bounds);
                //        xs.Add((rect.Left));
                //        xs.Add((rect.Right));
                //        ys.Add((rect.Top));
                //        ys.Add((rect.Bottom));
                //    }
                //}
                //guidelineSet.GuidelinesX = new DoubleCollection(xs.Select(p => p + 0.5));
                //guidelineSet.GuidelinesY = new DoubleCollection(ys.Select(p => p + 0.5));

            }
        }


        private TranslateTransform TranslateTransform;

        public TransformGroup Transform { get; }

        public Vector Translate
        {
            get => translate;
            set
            {
                translate = value;
                this.TranslateTransform.X = value.X;
                this.TranslateTransform.Y = value.Y;
            }
        }


        public DrawingVisual testVisual { get; private set; }

        private Point SelectionStart; // 用的是元素坐标

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                CtrlDown = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                CtrlDown = false;
            }
            base.OnKeyUp(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                this.CaptureMouse();
                if (e.ChangedButton == MouseButton.Middle)
                {
                    CanvasMoving = true;
                }
            }
            else if (e.ClickCount == 2)
            {
                if (e.ChangedButton == MouseButton.Middle)
                {
                    Zoom();
                }

            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
            if (e.ChangedButton == MouseButton.Middle)
            {
                CanvasMoving = false;
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            lock (lockObj)
            {
                var p = e.GetPosition(DrawingHost);                          //当前视图坐标点
                var scaleCenter = this.Transform.Inverse.Transform(p);// 当前真实坐标点

                var scale = this.Scale + e.Delta * 0.001;
                if (scale < 0.1)
                    return;

                Scale1(p, scaleCenter, scale);
            }
            base.OnMouseWheel(e);
        }



        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            End = e.GetPosition(this.DrawingHost);
            if (Action != null)
            {
                Action?.MouseLeftButtonUp(GetMousePosition(e));
            }
            else if (CanvasMoving)    // 可能是在移动画布，什么都不用做
            {
                CanvasMoving = false;
            }
            else if (ObjectMoving)// 可能是在移动图元，需要更新图元信息
            {
                ObjectMoving = false;
            }
            else if (Selecting)// 可能是在选择元素
            {
                Selecting = false;
                var selection = this.Select(Start, End);
                if (this.CtrlDown)
                    this.Selection.AddRange(selection);
                else
                    this.Selection = selection;
                DrawSelectionObject(Selection);
            }

            using (var dc = this.SelectionRectVisual.RenderOpen()) ;
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            IsMouseDown = true;
            Start = e.GetPosition(this.DrawingHost);
            PreviousPoint = Start;

            if (Action != null)
            {
                Action?.MouseLeftButtonDown(GetMousePosition(e));
            }
            else if (this.Selection != null && this.Selection.Count > 0 && this.Selection.Contains(this.DrawingHost.GetVisual(Start, CanSelect)))
            {
                ObjectMoving = true;
            }
            else
            {
                Selecting = true;
                SelectionStart = GetMousePosition(e);
            }
            base.OnMouseLeftButtonDown(e);
        }


        private Point PreviousPoint;
        private Random Random = new Random();

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var point = e.GetPosition(this.DrawingHost);
            var vector = point - PreviousPoint;
            PreviousPoint = point;

            if (CanvasMoving)
            {
                this.Translate += vector;
            }
            this.text.Text = GetMousePosition(e).ToString();
            if (Action != null)
            {
                Action.MouseMove(GetMousePosition(e));
            }
            else if (IsMouseDown)
            {
                if (ObjectMoving)
                {
                    PreviewMove(this.Selection, vector);
                }
                else if (Selecting)
                {
                    //SelectionRectVisual.Object = new SelectionRectObj(SelectionStart, GetMousePosition(e));
                    //Draw(SelectionRectVisual);
                    //DrawSelectionRect(Start, point,this.transform);
                    var selection = Select(Start, point);
                    if (this.CtrlDown)
                        this.Selection.AddRange(selection);
                    else
                        this.Selection = selection;
                    DrawSelectionObject(Selection);
                }
            }
            else
            {
                var selection = Select(point, point);

                DrawSelectionObject(Select(point, point).Concat(this.Selection).ToList());
                if (selection.Count > 0)
                {

                }
            }
            base.OnMouseMove(e);
        }

        private void PreviewMove(List<DrawObjVisual> selection, Vector vector)
        {
            var transform = new TranslateTransform(vector.X, vector.Y);

            foreach (var item in selection)
            {
                var drawing = item.Drawing;
                using (var dc = item.RenderOpen())
                {

                    dc.PushTransform(transform);
                    dc.DrawDrawing(drawing);
                }
            }

            DrawSelectionObject(selection);
        }

        private double scale = 1;

        private readonly object lockObj = new object();

        public DrawingCanvas DrawingHost { get; private set; }
        public Canvas topCanvas { get; }
        public DrawActionBase Action { get; private set; }


        internal void DoAction(DrawActionBase drawRectAction)
        {
            this.ClearPreview();
            this.Action = drawRectAction;
            if (drawRectAction != null)
                this.Action.Complated += Action_Complated;
        }

        private void Action_Complated(object sender, EventArgs e)
        {
            this.ClearPreview();
            this.Action = null;
        }

        public Point GetMousePosition(MouseEventArgs e)
        {
            var point = e.GetPosition(this.DrawingHost);
            return this.Transform.Inverse.Transform(point);
        }

        public void AddObject(DrawObj obj)
        {
            AddObject(this.ElementLayer, obj);
        }

        public void AddObject(DrawingLayer layer, DrawObj obj)
        {
            var visual = new DrawObjVisual(obj);
            var geo = obj.GetGeometry().Clone();
            var transform = new TransformGroup();
            if (geo.Transform != null)
                transform.Children.Add(geo.Transform);
            transform.Children.Add(ScaleTransform);
            transform.Children.Add(TranslateTransform);

            geo.Transform = transform;
            visual.Geometry = geo;
            var rect = obj.GetGeometry().Bounds;
            visual.XSnappingGuidelines = new DoubleCollection(new[] { 0.5 + (rect.Left), 0.5 + rect.Right });
            visual.YSnappingGuidelines = new DoubleCollection(new[] { 0.5 + rect.Top, 0.5 + rect.Bottom });



            Draw(visual);
            this.DrawingHost.AddVisual(layer, visual);
            this.visuals.Add(visual);


            //var visual1 = new DrawObjVisual(obj, true);
            //Draw(visual1);
            //this.Canvas.AddVisual(layer, visual1);
            //this.visuals.Add(visual1);

        }

        public void Remove(DrawObj obj)
        {
            var visual = visuals.Find(p => p.Object == obj);
            this.DrawingHost.RemoveVisual(visual);
            this.visuals.Remove(visual);
        }

        public void MoveAll()
        {

        }

        public void Update(DrawObj obj)
        {

        }

        private GeometryGroup PreviewGeometry = new GeometryGroup();

        public void DrawPreview(DrawObj obj)
        {
            PreviewGeometry.Children = new GeometryCollection()
            {
                obj.GetGeometry()
            };

            PreviewVisual.Object = obj;
            Draw(PreviewVisual);
            //= new GeometryCollection()
            //{
            //    obj.GetGeometry()
            //};
        }
        public void ClearPreview()
        {
            PreviewVisual.Object = null;
            using (var dc = PreviewVisual.RenderOpen()) ;
        }

        private void Draw(DrawObjVisual visual)
        {

            Draw(visual, visual.Geometry, visual.Brush, visual.Pen);
            //Draw(visual, visual.Object, visual.Object.FillColor, visual.Object.Pen, new ScaleTransform(this.Scale, -this.Scale));



        }




        private void Draw(DrawingVisual visual, Geometry geo, Brush FillBrush, Pen pen)
        {
            if (geo == null)
                return;

            using (var dc = visual.RenderOpen())
            {
                dc.DrawGeometry(FillBrush, pen, geo);
            }
        }


        private List<DrawObjVisual> Select(Point start, Point end)
        {
            if (IsSingle(start, end))
            {
                var result = new List<DrawObjVisual>();
                var r = this.DrawingHost.GetVisual(start, CanSelect) as DrawObjVisual;
                if (r != null)
                {
                    result.Add(r);
                }
                return result;
            }

            var stop = Stopwatch.StartNew();

            var geo = new RectangleGeometry(new Rect(start, end));

            var selectBox = new Rect(start, end);
            //var list = this.visuals.Select(visual => (visual, CheckBox(visual))).ToList();
            //var results = list.Where(p => p.Item2 == IntersectionDetail.FullyInside).Select(p => p.visual).ToList();

            //if (end.X < start.X)
            //{
            //    var set = list.Where(p => p.Item2 == IntersectionDetail.Intersects).Select(p => p.visual).ToList();
            //    var r = this.Canvas.GetVisuals(geo,p=> p!=null && p is DrawObjVisual && set.Contains(p as DrawObjVisual), true, true, true).OfType<DrawObjVisual>().ToList();
            //    results.AddRange(r);
            //}
            var results = this.visuals.Where(p => filter(p)).ToList();
            Debug.WriteLine(stop.ElapsedMilliseconds);

            return results;

            bool filter(DrawObjVisual visual)
            {
                var detail = CheckBox(visual);
                if (detail == IntersectionDetail.FullyInside)
                    return true;
                if (detail == IntersectionDetail.Intersects && end.X < start.X)
                {
                    detail = visual.Geometry.FillContainsWithDetail(geo);
                    return detail == IntersectionDetail.Intersects;
                }
                return false;
            }

            IntersectionDetail CheckBox(DrawObjVisual visual)
            {
                var rect = visual.Geometry.Bounds;
                if (selectBox.IntersectsWith(rect))
                {
                    if (selectBox.Contains(rect))
                    {
                        return IntersectionDetail.FullyInside;
                    }
                    return IntersectionDetail.Intersects;
                }
                return IntersectionDetail.Empty;
            }



        }

        internal static bool IsSingle(Point start, Point end)
        {
            return (end - start).Length < 2;
        }

        private void DrawSelectionObject() => DrawSelectionObject(this.Selection);

        private void DrawSelectionObject(List<DrawObjVisual> visuals)
        {
            SelectObjectVisual.UpdateObjs(visuals.Select(p => p.Geometry));
        }

        private bool CanSelect(Visual visual)
        {
            return visual != this.SelectionRectVisual
                && visual != this.SelectObjectVisual
                && visual != this.PreviewVisual;
        }

        private void Zoom()
        {
            var rects = this.visuals.Select(p => p.Object.GetGeometry().Bounds).ToList();
            var top = rects.Max(p => p.Y + p.Height);
            var bottom = rects.Min(p => p.Y);
            var left = rects.Min(p => p.X);
            var right = rects.Max(p => p.X + p.Width);

            var center = new Point((left + right) / 2, (top + bottom) / 2);

            var width = right - left;
            var height = top - bottom;

            var actualWidth = this.ActualWidth;
            var actualHeight = this.ActualHeight;

            if (actualWidth > 200)
                actualWidth -= 40;
            if (actualHeight > 200)
                actualHeight -= 40;

            var scale1 = actualWidth / width;
            var scale2 = actualHeight / height;


            Scale1(new Point(this.ActualWidth / 2, this.ActualHeight / 2), center, Math.Min(scale1, scale2));

        }
        private void Scale1(Point drawingPoint, Point scaleCenter, double scale)
        {
            var scaleTransform = new ScaleTransform(scale, -scale);
            var translateVector = -(Vector)scaleTransform.Transform(scaleCenter) + new Vector(drawingPoint.X, drawingPoint.Y); // 先移动到0，0点，再移动到缩放中心

            this.Scale = scale;
            this.Translate = translateVector;

            //foreach (var item in this.visuals)
            //{
            //    var rect = this.Transform.TransformBounds(item.Object.GetGeometry().Bounds);
            //    item.XSnappingGuidelines = new DoubleCollection(new[] { 0.5 + (rect.Left), 0.5 + rect.Right });
            //    item.YSnappingGuidelines = new DoubleCollection(new[] { 0.5 + rect.Top, 0.5 + rect.Bottom });
            //}
            //foreach (var item in this.visuals)
            //{
            //    var rect = this.Transform.TransformBounds(item.Object.GetGeometry().Bounds);
            //    item.XSnappingGuidelines = new DoubleCollection(new[] { 0.5 });
            //    item.YSnappingGuidelines = new DoubleCollection(new[] { 0.5 });
            //}

            foreach (var item in this.visuals)
            {
                var rect = this.Transform.TransformBounds(item.Object.GetGeometry().Bounds);
                item.XSnappingGuidelines[0] = 0.5 + (rect.Left);
                item.XSnappingGuidelines[1] = 0.5 + (rect.Right);
                item.YSnappingGuidelines[0] = 0.5 + (rect.Top);
                item.YSnappingGuidelines[1] = 0.5 + (rect.Bottom);

                //item.XSnappingGuidelines = new DoubleCollection(new[] { 0.5 + (rect.Left), 0.5 + rect.Right });
                //item.YSnappingGuidelines = new DoubleCollection(new[] { 0.5 + rect.Top, 0.5 + rect.Bottom });
            }
        }

        private void SetGuideLines()
        {

        }
    }

    public class SelectedVisualsChangedArgs : EventArgs
    {
        public SelectedVisualsChangedArgs(List<DrawingVisual> visuals)
        {
            this.SelectedVisuals = visuals;
        }

        public List<DrawingVisual> SelectedVisuals { get; private set; }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

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
            var canvas = new DrawingCanvas();
            var grid = new Grid();
            this.Content = grid;
            grid.Children.Add(canvas);
            this.Canvas = canvas;
            var topCanvas = new Canvas();
            grid.Children.Add(topCanvas);
            topCanvas.Children.Add(text);
            this.Canvas.SnapsToDevicePixels = true;

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


            this.ElementLayer = canvas.AddDrawingLayer();
            this.TagLayer = canvas.AddDrawingLayer();
            this.PreviewSelectLayer = canvas.AddDrawingLayer();
            this.PreviewLayer = canvas.AddDrawingLayer();

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
            canvas.AddVisual(this.PreviewLayer, testVisual);
            canvas.AddVisual(this.PreviewLayer, PreviewVisual);
            canvas.AddVisual(this.PreviewSelectLayer, SelectObjectVisual);
            canvas.AddVisual(this.PreviewSelectLayer, SelectionRectVisual);

            this.PreviewGeometry = new GeometryGroup();
            this.PreviewGeometry.Transform = this.Transform;
            PreviewVisual.Geometry = this.PreviewGeometry;
            Draw(PreviewVisual);
            Draw(SelectObjectVisual);

            //DrawingBrush drawingBrush = new DrawingBrush();
            //RenderOptions.SetCachingHint(drawingBrush, CachingHint.Cache);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    AddObject(new RectObj(new Point(i * 20, j * 20), new Point(i * 20 + 16, j * 20 + 16)));
                }
            }
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

        public double Scale
        {
            get => scale;
            set
            {
                scale = value;
                this.ScaleTransform.ScaleX = value;
                this.ScaleTransform.ScaleY = -value;
            }
        }


        private TranslateTransform TranslateTransform;

        public TransformGroup Transform { get; }
        private GuidelineSet GuidelineSet { get; set; } = new GuidelineSet();
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
            this.CaptureMouse();
            if (e.ChangedButton == MouseButton.Middle)
            {
                CanvasMoving = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.Zoom();
            }
            base.OnMouseDoubleClick(e);
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
                var p = e.GetPosition(Canvas);                          //当前视图坐标点
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
            End = e.GetPosition(this.Canvas);
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
            Start = e.GetPosition(this.Canvas);
            PreviousPoint = Start;

            if (Action != null)
            {
                Action?.MouseLeftButtonDown(GetMousePosition(e));
            }
            else if (this.Selection != null && this.Selection.Count > 0 && this.Selection.Contains(this.Canvas.GetVisual(Start, CanSelect)))
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
            var point = e.GetPosition(this.Canvas);
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
                    SelectionRectVisual.Object = new SelectionRectObj(SelectionStart, GetMousePosition(e));
                    Draw(SelectionRectVisual);
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

        public DrawingCanvas Canvas { get; private set; }
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
            var point = e.GetPosition(this.Canvas);
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
            //visual.XSnappingGuidelines = new DoubleCollection(new[] { 0.5 });
            //visual.YSnappingGuidelines = new DoubleCollection(new[] { 0.5 });

            Draw(visual);
            this.Canvas.AddVisual(layer, visual);
            this.visuals.Add(visual);


            //var visual1 = new DrawObjVisual(obj, true);
            //Draw(visual1);
            //this.Canvas.AddVisual(layer, visual1);
            //this.visuals.Add(visual1);

        }

        public void Remove(DrawObj obj)
        {
            var visual = visuals.Find(p => p.Object == obj);
            this.Canvas.RemoveVisual(visual);
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

        private void Draw(DrawObjVisual visual, Transform transform)
        {
            if (visual.Geometry != null)
            {
                Draw(visual, visual.Brush, visual.Pen, visual.Geometry);
            }
        }

        private void Draw(DrawingVisual visual, Brush FillBrush, Pen pen, Geometry geometry)
        {
            //var guideLines = new GuidelineSet();
            //guideLines.GuidelinesX = new DoubleCollection();
            //var rect = geometry.Bounds;
            //guideLines.GuidelinesX.Add(rect.X + 0.5);
            //guideLines.GuidelinesX.Add(rect.X + rect.Width + 0.5);
            //guideLines.GuidelinesY.Add(rect.Y + 0.5);
            //guideLines.GuidelinesY.Add(rect.Y + rect.Height + 0.5);

            using (var dc = visual.RenderOpen())
            {
                dc.PushGuidelineSet(GuidelineSet);
                dc.DrawGeometry(FillBrush, null, geometry);
                dc.DrawGeometry(null, pen, geometry);
            }
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
        private void Draw(DrawingContext dc, Geometry geo, Brush FillBrush, Pen pen)
        {
            //var guideLines = new GuidelineSet();
            //guideLines.GuidelinesX = new DoubleCollection();
            //guideLines.GuidelinesX.Add(rect.X + 0.5);
            //guideLines.GuidelinesX.Add(rect.X + rect.Width + 0.5);
            //guideLines.GuidelinesY.Add(rect.Y + 0.5);
            //guideLines.GuidelinesY.Add(rect.Y + rect.Height + 0.5);

            //dc.PushGuidelineSet(guideLines);

            dc.DrawGeometry(FillBrush, pen, geo);
        }
        private List<DrawObjVisual> Select(Point start, Point end)
        {
            if (IsSingle(start, end))
            {
                var result = new List<DrawObjVisual>();
                var r = this.Canvas.GetVisual(start, CanSelect) as DrawObjVisual;
                if (r != null)
                {
                    result.Add(r);
                }
                return result;
            }

            var geo = new RectangleGeometry(new Rect(start, end));
            if (end.X < start.X)
            {
                return this.Canvas.GetVisuals(geo, CanSelect, true, true, true).OfType<DrawObjVisual>().ToList();
            }
            else
            {
                return this.Canvas.GetVisuals(geo, CanSelect, true, false, false).OfType<DrawObjVisual>().ToList();
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


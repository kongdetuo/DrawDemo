using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DrawDemo
{
    public class DrawingMap
    {
        public DrawingMap(DrawingCanvas canvas)
        {
            this.Canvas = canvas;

            this.Canvas.MouseMove += Canvas_MouseMove;
            this.Canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            this.Canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            this.Canvas.MouseWheel += Canvas_MouseWheel;
            this.Canvas.MouseDown += Canvas_MouseDown;
            this.Canvas.MouseUp += Canvas_MouseUp;
            Window.GetWindow(this.Canvas).PreviewKeyDown += Canvas_PreviewKeyDown;
            Window.GetWindow(this.Canvas).PreviewKeyUp += Canvas_PreviewKeyUp;

            Transform = Transform.Identity;

            this.ElementLayer = canvas.AddDrawingLayer();
            this.TagLayer = canvas.AddDrawingLayer();
            this.PreviewSelectLayer = canvas.AddDrawingLayer();
            this.PreviewLayer = canvas.AddDrawingLayer();

            SelectObjectVisual = new DrawingVisual()
            {
                Effect = new DropShadowEffect()
                {
                    Color = Colors.Black,
                    ShadowDepth = -1,
                }
            };
            SelectionRectVisual = new DrawObjVisual();
            PreviewVisual = new DrawObjVisual();

            canvas.AddVisual(this.PreviewLayer, PreviewVisual);
            canvas.AddVisual(this.PreviewSelectLayer, SelectObjectVisual);
            canvas.AddVisual(this.PreviewSelectLayer, SelectionRectVisual);
        }

        public DrawingLayer ElementLayer { get; private set; }
        public DrawingLayer TagLayer { get; private set; }
        public DrawingLayer PreviewSelectLayer { get; private set; }
        public DrawingLayer PreviewLayer { get; private set; }


        private List<DrawObjVisual> Selection = new List<DrawObjVisual>();
        private List<DrawObjVisual> visuals = new List<DrawObjVisual>();

        private double Scale = 1;
        private Vector Translate;
        private volatile Transform transform;

        private Point Start;
        private Point End;
        private bool CanvasMoving;
        private bool IsMouseDown;
        private bool ObjectMoving;
        private bool Selecting;

        private bool CtrlDown;

        private DrawingVisual SelectObjectVisual;
        private DrawObjVisual SelectionRectVisual;
        private DrawObjVisual PreviewVisual;

        private Point SelectionStart; // 用的是元素坐标

        private void Canvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                CtrlDown = true;
            }
        }
        private void Canvas_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                CtrlDown = false;
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                CanvasMoving = true;
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                CanvasMoving = false;
            }
        }
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            lock (lockObj)
            {
                var p = e.GetPosition(Canvas);                          //当前视图坐标点
                var scaleCenter = this.Transform.Inverse.Transform(p);  // 当前真实坐标点

                var scale = this.Scale + e.Delta * 0.001;
                if (scale < 0.1)
                    return;

                var scaleTransform = new ScaleTransform(scale, scale);
                var translateVector = -(Vector)scaleTransform.Transform(scaleCenter) + (Vector)p; // 先移动到0，0点，再移动到缩放中心
                UpdateTransform(scale, translateVector);
            }
            DrawAll();
        }

        private void Canvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            End = e.GetPosition(this.Canvas);
            if (Action != null)
            {
                Action?.MouseLeftButtonUp(GetMousePosition(e));
            }
            else if (CanvasMoving)    // 可能是在移动画布，什么都不用做
            {

            }
            else if (ObjectMoving)// 可能是在移动图元，需要更新图元信息
            {

            }
            else if (Selecting)// 可能是在选择元素
            {
                var selection = this.Select(Start, End);
                if (this.CtrlDown)
                    this.Selection.AddRange(selection);
                else
                    this.Selection = selection;
                DrawSelectionObject(Selection);
            }

            IsMouseDown = false;
            ObjectMoving = false;
            Selecting = false;

            using var dc = this.SelectionRectVisual.RenderOpen();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
        }

        private Point PreviousPoint;
        private Random Random = new Random();
        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var point = e.GetPosition(this.Canvas);
            var vector = point - PreviousPoint;
            PreviousPoint = point;

            if (CanvasMoving)
            {
                MoveAll(vector);
            }

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
                    Draw(SelectionRectVisual, this.Transform);
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
        }

        private void PreviewMove(List<DrawObjVisual> selection, Vector vector)
        {
            var transform = new TranslateTransform(vector.X, vector.Y);

            foreach (var item in selection)
            {
                var drawing = item.Drawing;
                using var dc = item.RenderOpen();
                dc.PushTransform(transform);
                dc.DrawDrawing(drawing);
            }

            DrawSelectionObject(selection);
        }
        private void Move(List<DrawObjVisual> selection, Vector vector)
        {
            var transform = new TranslateTransform(vector.X, vector.Y);

            foreach (var item in selection)
            {
                var drawing = item.Drawing;
                using var dc = item.RenderOpen();
                dc.PushTransform(transform);
                dc.DrawDrawing(drawing);
            }

        }
        private void MoveAll(Vector vector)
        {
            lock (this.lockObj)
            {
                UpdateTransform(this.Scale, this.Translate + vector);
                DrawAll();
            }
        }

        private void UpdateTransform(double scale, Vector translate)
        {
            this.Scale = scale;
            this.Translate = translate;

            Transform = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    new ScaleTransform(scale,scale),
                    new TranslateTransform(this.Translate.X,this.Translate.Y)
                }
            };
        }

        private readonly object lockObj = new object();

        public DrawingCanvas Canvas { get; private set; }
        public DrawActionBase Action { get; private set; }

        public Transform Transform { get => transform; set => transform = value; }

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
            lock (this.lockObj)
            {
                var point = e.GetPosition(this.Canvas);
                return this.Transform.Inverse.Transform(point);
            }
        }

        public void AddObject(DrawObj obj)
        {
            AddObject(this.ElementLayer, obj);
        }

        public void AddObject(DrawingLayer layer, DrawObj obj)
        {
            var visual = new DrawObjVisual(obj);
            Draw(visual, this.Transform);
            this.Canvas.AddVisual(layer, visual);
            this.visuals.Add(visual);
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

        public void DrawPreview(DrawObj obj)
        {
            PreviewVisual.Object = obj;
            this.Draw(PreviewVisual, this.Transform);
        }
        public void ClearPreview()
        {
            PreviewVisual.Object = null;
            using var dc = PreviewVisual.RenderOpen();
        }

        private void DrawAll()
        {
            var transform = this.Transform;
            foreach (var visual in visuals)
            {
                Draw(visual, transform);
            }

            Draw(PreviewVisual, transform);
            Draw(SelectionRectVisual, transform);
            DrawSelectionObject();
        }

        private void Draw(DrawObjVisual visual, Transform transform)
        {
            if (visual.Object != null)
            {
                Draw(visual, visual.Object, visual.Object.FillColor, visual.Object.Pen, transform);
            }
        }

        private void Draw(DrawingVisual visual, DrawObj obj, Brush FillBrush, Pen pen, Transform transform)
        {
            var geo = obj?.GetGeometry()?.Clone();
            if (geo != null)
            {
                if (geo.Transform == null)
                    geo.Transform = transform;
                else
                    geo.Transform = new TransformGroup()
                    {
                        Children = new TransformCollection()
                        {
                            geo.Transform,
                            transform,
                        }
                    };

                var guideLines = new GuidelineSet();
                guideLines.GuidelinesX = new DoubleCollection();
                var rect = geo.Bounds;
                guideLines.GuidelinesX.Add(rect.X + 0.5);
                guideLines.GuidelinesX.Add(rect.X + rect.Width + 0.5);
                guideLines.GuidelinesY.Add(rect.Y + 0.5);
                guideLines.GuidelinesY.Add(rect.Y + rect.Height + 0.5);

                using var dc = visual.RenderOpen();


                dc.PushGuidelineSet(guideLines);
                dc.DrawGeometry(FillBrush, pen, geo);
            }
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
            if (this.SelectObjectVisual == null)
            {
                this.SelectObjectVisual = new DrawingVisual();
                this.SelectObjectVisual.Effect = new DropShadowEffect()
                {
                    Color = Colors.Black,
                    ShadowDepth = -1,
                };
                this.Canvas.AddVisual(this.PreviewLayer, this.SelectObjectVisual);
            }

            using var dc = this.SelectObjectVisual.RenderOpen();
            foreach (var item in visuals)
            {
                dc.DrawDrawing(item.Drawing);
            }
        }

        private bool CanSelect(Visual visual)
        {
            return visual != this.SelectionRectVisual
                && visual != this.SelectObjectVisual
                && visual != this.PreviewVisual;
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


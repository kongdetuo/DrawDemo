using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

            Transform = Transform.Identity;
        }



        private Point moveStart;
        private bool CanvasMoving;

        private List<DrawObjVisual> Selection = new List<DrawObjVisual>();
        private List<DrawObjVisual> visuals = new List<DrawObjVisual>();


        private double Scale = 1;
        private Vector Translate;

        private volatile Transform transform;
        private DrawingVisual drawSelectObjectVisual;

        private Point Start;
        private Point End;
        private bool IsMouseDown;
        private bool NormalMode = true;
        private bool ObjectMoving;

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                moveStart = e.GetPosition(this.Canvas);
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
            if (NormalMode)
            {
                if (CanvasMoving)    // 可能是在移动画布，什么都不用做
                {

                }
                else if (ObjectMoving)// 可能是在移动图元，需要更新图元信息
                {

                }
                else // 可能是在选择元素
                {
                    var selection = this.Select(Start, End);
                    this.Selection = selection;
                    DrawSelectionObject(selection);
                    ClearPreview();
                }
            }
            else
            {
                if (Action == null)
                {
                    End = e.GetPosition(this.Canvas);
                }
                else
                {
                    Action.MouseLeftButtonUp(GetMousePosition(e));
                }
            }
            IsMouseDown = false;
            ObjectMoving = false;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Start = e.GetPosition(this.Canvas);
            PreviousPoint = Start;
            if (NormalMode)
            {
                IsMouseDown = true;
                if (this.Selection != null && this.Selection.Count > 0 && this.Selection.Contains(this.Canvas.GetVisual(Start)))
                {
                    ObjectMoving = true;
                }
                else
                {
                    this.Selection.Clear();
                    DrawSelectionObject(new List<DrawObjVisual>());
                }
            }
            else
            {
                Action?.MouseLeftButtonDown(GetMousePosition(e));
            }
        }

        private Point PreviousPoint;
        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var point = e.GetPosition(this.Canvas);
            var vector = point - PreviousPoint;
            PreviousPoint = point;

            if (NormalMode)
            {
                if (IsMouseDown)
                {
                    if (ObjectMoving)
                    {
                        PreviewMove(this.Selection, vector);
                    }
                    else
                    {
                        DrawSelectionRect(Start, point);
                        var selection = Select(Start, point);
                        DrawSelectionObject(selection);
                        this.Selection = selection;
                    }
                }
                else
                {
                    DrawSelectionObject(Select(point, point).Concat(this.Selection).ToList());
                }
            }
            else
            {
                Action?.MouseMove(GetMousePosition(e));
            }

            if (CanvasMoving)
            {
                MoveAll(vector);
            }
        }

        private void DrawSelectionRect(Point start, Point end)
        {
            if ((end - start).LengthSquared > 1)
            {
                using var dc = this.PreviewVisual.RenderOpen();
                dc.PushGuidelineSet(new GuidelineSet(new[] { 0.5 }, new[] { 0.5 }));
                var pen = new Pen(Brushes.Black, 1);

                if (start.X > end.X)
                {
                    pen.DashStyle = DashStyles.Dot;
                }

                dc.DrawRectangle(null, pen, new Rect(start, end));
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
        internal DrawObjVisual PreviewVisual { get; private set; }


        public double PenThickness { get; private set; } = 1;
        public Transform Transform { get => transform; set => transform = value; }

        internal void DoAction(DrawActionBase drawRectAction)
        {
            this.ClearPreview();
            this.Action = drawRectAction;
            if(drawRectAction!=null)
            this.Action.Complated += Action_Complated;
            NormalMode = drawRectAction == null;
                
        }

        private void Action_Complated(object sender, EventArgs e)
        {
            //if(sender is not SelectionAction)
            //{
            //    this.Action = new SelectionAction(this.Canvas);
            //}
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
            var visual = new DrawObjVisual(obj);
            Draw(visual, this.Transform);
            this.Canvas.AddVisual(visual);
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
            if (this.PreviewVisual == null)
            {
                this.PreviewVisual = new DrawObjVisual(obj);
                this.Canvas.GetAdorner().AddVisual(PreviewVisual);
            }
            PreviewVisual.Object = obj;
            this.Draw(PreviewVisual, this.Transform);
        }
        public void ClearPreview()
        {
            if (this.PreviewVisual == null)
                return;
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

            if (PreviewVisual != null)
                Draw(PreviewVisual, transform);
            DrawSelectionObject();
        }

        private void Draw(DrawObjVisual visual, Transform transform)
        {
            if (visual.Object != null)
            {
                var geo = visual.Object.GetGeometry().Clone();
                geo.Transform = transform;

                var guideLines = new GuidelineSet();
                guideLines.GuidelinesX = new DoubleCollection();
                var rect = geo.Bounds;
                guideLines.GuidelinesX.Add(rect.X + 0.5);
                guideLines.GuidelinesX.Add(rect.X + rect.Width + 0.5);
                guideLines.GuidelinesY.Add(rect.Y + 0.5);
                guideLines.GuidelinesY.Add(rect.Y + rect.Height + 0.5);

                using var dc = visual.RenderOpen();
                dc.PushGuidelineSet(guideLines);
                dc.DrawGeometry(visual.Object.FillColor, new Pen(visual.Object.BorderColor, PenThickness), geo);
            }
        }

        private List<DrawObjVisual> Select(Point start, Point end)
        {
            if (IsSingle(start, end))
            {
                var result = new List<DrawObjVisual>();
                var r = this.Canvas.GetVisual(start) as DrawObjVisual;
                if (r != null)
                {
                    result.Add(r);
                }
                return result;
            }

            var geo = new RectangleGeometry(new Rect(start, end));
            if (end.X < start.X)
            {
                return this.Canvas.GetVisuals(geo, true, true, true).OfType<DrawObjVisual>().ToList();
            }
            else
            {
                return this.Canvas.GetVisuals(geo, true, false, false).OfType<DrawObjVisual>().ToList();
            }
        }

        internal static bool IsSingle(Point start, Point end)
        {
            return (end - start).Length < 2;
        }

        private void DrawSelectionObject() => DrawSelectionObject(this.Selection);

        private void DrawSelectionObject(List<DrawObjVisual> visuals)
        {
            if (this.drawSelectObjectVisual == null)
            {
                this.drawSelectObjectVisual = new DrawingVisual();
                this.drawSelectObjectVisual.Effect = new System.Windows.Media.Effects.DropShadowEffect()
                {
                    Color = Colors.Black,
                    ShadowDepth = -1,
                };
                this.Canvas.GetAdorner().AddVisual(this.drawSelectObjectVisual);
            }

            using var dc = this.drawSelectObjectVisual.RenderOpen();
            foreach (var item in visuals)
            {
                dc.DrawDrawing(item.Drawing);
            }
        }
    }
}


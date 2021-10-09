using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DrawDemo
{
    public abstract class DrawBase : ActionBase
    {
        protected DrawingVisual previewVisual { get; private set; }

        public DrawBase(DrawingCanvas canvas) : base(canvas)
        {
            previewVisual = new DrawingVisual();
            canvas.GetAdorner().AddVisual(previewVisual);
        }




        public virtual void Dispose()
        {
            base.Dispose();

            Canvas.GetAdorner().RemoveVisual(previewVisual);
        }

        protected DrawingContext RenderOpen(DrawingVisual visual)
        {
            var dc = visual.RenderOpen();
            dc.PushGuidelineSet(new GuidelineSet(new[] { 0.5 }, new[] { 0.5 }));
            return dc;
        }
    }

    public abstract class ActionBase
    {
        public DrawingCanvas Canvas { get; }


        public ActionBase(DrawingCanvas canvas)
        {
            this.Canvas = canvas;

            canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            canvas.MouseMove += Canvas_MouseMove;
        }

        protected virtual void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }

        protected virtual void Canvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        protected virtual void Canvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        protected Point GetCurrentPoint(MouseEventArgs e)
        {
            return e.GetPosition(this.Canvas);
        }

        public virtual void Dispose()
        {
            Canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            Canvas.MouseLeftButtonUp -= Canvas_MouseLeftButtonUp;
            Canvas.MouseMove -= Canvas_MouseMove;
        }
    }

    public abstract class DrawActionBase : DrawBase
    {

        public DrawActionBase(DrawingCanvas canvas):base(canvas)
        {

        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    public abstract class SingClickDrawAction : DrawActionBase
    {
        public SingClickDrawAction(DrawingCanvas canvas) : base(canvas)
        {

        }

        public abstract void DrawPreview(DrawingContext context, Point currentPoint);
        public abstract void Draw(DrawingContext context, Point currentPoint);

        protected override void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var visual = new DrawingVisual();
            using var dc = base.RenderOpen(visual);
            Draw(dc, GetCurrentPoint(e));
            this.Canvas.AddVisual(visual);
        }


        protected override void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            using var dc = base.RenderOpen(previewVisual);
            Draw(dc, GetCurrentPoint(e));
        }
    }

    public abstract class MultiClickDrawAction : DrawActionBase
    {
        protected MultiClickDrawAction(DrawingCanvas canvas) : base(canvas)
        {
        }

        public abstract void DrawPreview(Point privious, Point currentPrivew);
        public abstract void Draw(Point privious, Point current);
    }

    public abstract class DragDrawAction : DrawActionBase
    {
        public DragDrawAction(DrawingCanvas canvas) : base(canvas)
        {

        }
        private Point start;
        protected bool MouseLeftDown;

        public abstract void DrawPreview(DrawingContext context, Point start, Point privewEnd);

        public abstract void Draw(DrawingContext context, Point start, Point end);

        protected override void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas.CaptureMouse();
            start = e.GetPosition(Canvas);
            MouseLeftDown = true;
        }
        protected override void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseLeftDown = false;
            (sender as DrawingCanvas).ReleaseMouseCapture();
            MouseLeftDown = false;
            var visual = new DrawingVisual();
            var end = e.GetPosition(Canvas);
            using var dc = visual.RenderOpen();
            dc.PushGuidelineSet(new GuidelineSet(new[] { 0.5 }, new[] { 0.5 }));
            Draw(dc, start, end);
            Canvas.AddVisual(visual);
        }

        protected override void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseLeftDown)
            {
                var end = e.GetPosition(Canvas);
                using var dc = previewVisual.RenderOpen();
                dc.PushGuidelineSet(new GuidelineSet(new[] { 0.5 }, new[] { 0.5 }));
                DrawPreview(dc, start, end);
            }
        }
    }

    public class DrawRectAction : SingClickDrawAction
    {
        public DrawRectAction(DrawingCanvas canvas) : base(canvas)
        {
        }

        public override void Draw(DrawingContext context, Point currentPoint)
        {
            var pen = new Pen(Brushes.Black, 1);
            context.DrawRectangle(null, pen, new Rect(currentPoint, currentPoint + new Vector(100, 100)));
        }

        public override void DrawPreview(DrawingContext context, Point currentPoint)
        {

        }
    }

    internal class DrawLineAction : DragDrawAction
    {
        public DrawLineAction(DrawingCanvas canvas) : base(canvas)
        {
        }

        public override void Draw(DrawingContext context, Point start, Point end)
        {
            var pen = new Pen(Brushes.Black, 1);
            context.DrawLine(pen, start, end);
        }

        public override void DrawPreview(DrawingContext context, Point start, Point privewEnd)
        {
            var pen = new Pen(Brushes.Black, 1);
            context.DrawLine(pen, start, privewEnd);
        }
    }

    internal class SelectionAction : ActionBase
    {
        private DrawSelectionRectAction drawSelectionRect;
        private DrawSelectionObjectAction drawSelectionObject;

        public List<DrawingVisual> Selection { get; private set; }
        private bool Selected;

        public SelectionAction(DrawingCanvas canvas) : base(canvas)
        {
            drawSelectionRect = new DrawSelectionRectAction(canvas);
            drawSelectionObject = new DrawSelectionObjectAction(canvas);
        }

        protected override void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.Canvas_MouseLeftButtonDown(sender, e);
        }

        protected override void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.Canvas_MouseLeftButtonUp(sender, e);
            
        }

        protected override void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            base.Canvas_MouseMove(sender, e);
        }

        private void Select()
    }

    internal class DrawMoveAction : DrawActionBase
    {
        public DrawMoveAction(DrawingCanvas canvas) : base(canvas)
        {
        }

        public void Draw(Point start, Point end)
        {

        }
    }

    internal class DrawSelectionRectAction : DrawBase
    {
        private bool MouseLeftDown;


        public DrawSelectionRectAction(DrawingCanvas canvas) : base(canvas)
        {
        }

        protected override void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Selection = new List<DrawingVisual>();
            base.Canvas_MouseLeftButtonDown(sender, e);
        }
        protected override void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseLeftDown = false;
            using var dc = this.previewVisual.RenderOpen();
            base.Canvas_MouseLeftButtonUp(sender, e);
        }

        public void Draw(Point start, Point end)
        {
            using var dc = base.RenderOpen(this.previewVisual);
            var pen = new Pen(Brushes.Black, 1);
            if (start.X > end.X)
            {
                pen.DashStyle = DashStyles.Dot;
            }
            dc.DrawRectangle(null, pen, new Rect(start, end));
        }
    }

    internal class DrawSelectionObjectAction : DrawBase
    {
        public DrawSelectionObjectAction(DrawingCanvas canvas) : base(canvas)
        {
            this.previewVisual.Effect = new DropShadowEffect()
            {
                ShadowDepth = 0
            };
        }

        public void Draw(List<DrawingVisual> visuals)
        {
            using var dc = this.previewVisual.RenderOpen();
            foreach (var item in visuals)
            {
                dc.DrawDrawing(item.Drawing);
            }
        }
    }
}
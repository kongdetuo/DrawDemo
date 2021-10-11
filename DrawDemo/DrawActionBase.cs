using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawDemo
{
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

    public abstract class DrawActionBase
    {
        public DrawingMap Map { get; private set; }


        public DrawActionBase(DrawingMap map)
        {
            this.Map = map;
            map.Canvas.KeyDown += Canvas_KeyDown;
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                OnDrawComplated();
            }
        }

        public event EventHandler Complated;

        public virtual void MouseLeftButtonDown(Point point)
        {

        }

        public virtual void MouseLeftButtonUp(Point point)
        {

        }

        public virtual void MouseMove(Point point)
        {

        }

        protected void OnDrawComplated()
        {
            this.Complated?.Invoke(this, EventArgs.Empty);
        }
    }

    public abstract class SingClickDrawAction : DrawActionBase
    {
        public SingClickDrawAction(DrawingMap map) : base(map)
        {

        }

        public abstract DrawObj CreatePreviewObj(Point point);
        public abstract DrawObj CreateObj(Point point);

        public override void MouseLeftButtonUp(Point point)
        {
            var obj = CreateObj(point);
            this.Map.AddObject(Map.ElementLayer, obj);
        }

        public override void MouseMove(Point point)
        {
            this.Map.DrawPreview(CreatePreviewObj(point));
        }
    }

    //public abstract class MultiClickDrawAction : DrawActionBase
    //{
    //    protected MultiClickDrawAction(DrawingCanvas canvas) : base(canvas)
    //    {
    //    }

    //    public abstract void DrawPreview(Point privious, Point currentPrivew);
    //    public abstract void Draw(Point privious, Point current);
    //}

    public abstract class DragDrawAction : DrawActionBase
    {
        public DragDrawAction(DrawingMap canvas) : base(canvas)
        {

        }
        private Point start;
        protected bool MouseLeftDown;

        public abstract DrawObj CreatePreviewObj(Point start, Point end);
        public abstract DrawObj CreateObj(Point start, Point end);

        public override void MouseLeftButtonDown(Point point)
        {
            MouseLeftDown = true;
            start = point;
        }

        public override void MouseLeftButtonUp(Point point)
        {
            MouseLeftDown = false;
            this.Map.ClearPreview();
            var obj = CreateObj(start, point);
            this.Map.AddObject(Map.TagLayer, obj);
        }

        public override void MouseMove(Point point)
        {
            if (MouseLeftDown)
            {
                this.Map.DrawPreview(CreatePreviewObj(start, point));
            }
        }
    }

    public class DrawRectAction : SingClickDrawAction
    {
        public DrawRectAction(DrawingMap canvas) : base(canvas)
        {
        }

        public override DrawObj CreateObj(Point point)
        {
            return new RectObj(point, point + new Vector(20, 20));
        }

        public override DrawObj CreatePreviewObj(Point point)
        {
            return new RectObj(point, point + new Vector(20, 20));
        }
    }

    internal class DrawLineAction : DragDrawAction
    {
        public DrawLineAction(DrawingMap canvas) : base(canvas)
        {
        }

        public override DrawObj CreateObj(Point start, Point end)
        {
            return new LineObj(start, end);
        }

        public override DrawObj CreatePreviewObj(Point start, Point end)
        {
            return new LineObj(start, end);
        }
    }

    class NormalAction : DrawActionBase
    {
        public NormalAction(DrawingMap map) : base(map)
        {
            this.Selections = new List<DrawObjVisual>();
        }

        public List<DrawObjVisual> Selections;


    }

    //internal class DrawMoveAction : DrawActionBase
    //{
    //    public DrawMoveAction(DrawingCanvas canvas) : base(canvas)
    //    {
    //    }

    //    public void Draw(Point start, Point end)
    //    {

    //    }
    //}
}
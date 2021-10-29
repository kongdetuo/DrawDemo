using System.Windows;
using System.Windows.Input;

namespace Drawing2D
{
    /// <summary>
    /// 手动绘图基类
    /// </summary>
    public abstract class DrawingAction : IDrawingAction
    {
        public bool IsDrawing { get; internal set; }
    

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
        }

        protected virtual void OnKeyUp(KeyEventArgs e)
        {
        }

        protected virtual void OnMouseDown(Point point)
        {
        }

        protected virtual void OnMouseMove(Point point)
        {
        }

        protected virtual void OnMouseUp(Point point)
        {
        }
        #region 实现接口

        void IDrawingAction.OnKeyDown(KeyEventArgs e)
        {
            this.OnKeyDown(e);
        }

        void IDrawingAction.OnKeyUp(KeyEventArgs e)
        {
            this.OnKeyUp(e);
        }

        void IDrawingAction.OnMouseDown(Point point)
        {
            this.OnMouseDown(point);
        }

        void IDrawingAction.OnMouseMove(Point point)
        {
            OnMouseMove(point);
        }

        void IDrawingAction.OnMouseUp(Point point)
        {
            OnMouseUp(point);
        }
        #endregion
    }

    public class PreviewGeometryChangedEventArgs
    {

    }


}

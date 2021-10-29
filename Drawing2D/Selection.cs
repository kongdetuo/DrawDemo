using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Drawing2D
{
    public class Selection : IDrawingAction
    {
        void IDrawingAction.OnKeyDown(KeyEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        void IDrawingAction.OnKeyUp(KeyEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        void IDrawingAction.OnMouseDown(Point point)
        {
            throw new System.NotImplementedException();
        }

        void IDrawingAction.OnMouseMove(Point point)
        {
            throw new System.NotImplementedException();
        }

        void IDrawingAction.OnMouseUp(Point point)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SelectionFilter
    {
        public void SetPreviewSelection(List<int> ids)
        {

        }

        public virtual void OnTabKeyDown()
        {

        }

        public virtual bool CanSelect(DrawingElement element)
        {
            return true;
        }

    }
}

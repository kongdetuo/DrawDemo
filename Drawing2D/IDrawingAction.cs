using System.Windows;
using System.Windows.Input;

namespace Drawing2D
{
    internal interface IDrawingAction
    {
        void OnKeyDown(KeyEventArgs e);
        void OnKeyUp(KeyEventArgs e);

        void OnMouseDown(Point point);
        void OnMouseUp(Point point);
        void OnMouseMove(Point point);
    }

}

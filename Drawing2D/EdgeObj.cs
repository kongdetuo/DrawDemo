using System;
using System.Windows;

namespace Drawing2D
{
    public class EdgeObj
    {
        public Point Start { get; set; }

        public Point End { get; set; }

        public Reference GetReference()
        {
            return new Reference();
        }

        public Reference GetEndPointReference(int index)
        {
            if (index == 0)
                return new Reference();
            if (index == 1)
                return new Reference();

            throw new ArgumentOutOfRangeException();
        }
    }

    public abstract class DrawingState
    {
        public DrawingHost Canvas { get; private set; }

        public DrawingState(DrawingHost drawingCanvas)
        {
            this.Canvas = drawingCanvas;
            Canvas.MouseLeftButtonDown += DrawingCanvas_MouseLeftButtonDown;
            Canvas.MouseLeftButtonUp += DrawingCanvas_MouseLeftButtonUp;
        }

        public void Dispose()
        {
            Canvas.MouseLeftButtonDown -= DrawingCanvas_MouseLeftButtonDown;
            Canvas.MouseLeftButtonUp -= DrawingCanvas_MouseLeftButtonUp;
        }

        protected virtual void OnMouseLeftButtonDown()
        {
        }

        protected virtual void OnMouseLeftButtonUp()
        {
        }

        private void DrawingCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnMouseLeftButtonUp();
        }

        private void DrawingCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnMouseLeftButtonDown();
        }
    }
}
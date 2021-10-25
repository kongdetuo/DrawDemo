using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DrawDemo
{
    class Foo : UIElement
    {
        public Foo()
        {
            var drawingVisual = new DrawingVisual();
            var translateTransform = new TranslateTransform();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var rectangleGeometry = new RectangleGeometry(new Rect(0, 0, 10, 10));

                for (int i = 0; i < 10; i++)
                {
                    translateTransform.X = i * 15;

                    drawingContext.PushTransform(translateTransform);

                    drawingContext.DrawGeometry(Brushes.Red, null, rectangleGeometry);

                    drawingContext.Pop();
                }
            }

            translateTransform.X = 500;

            Visual = drawingVisual;

            SetTranslateTransform(translateTransform);
        }

        private async void SetTranslateTransform(TranslateTransform translateTransform)
        {
            while (true)
            {
                translateTransform.X++;

                if (translateTransform.X > 700)
                {
                    translateTransform.X = 0;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(20));
            }
        }

        protected override Visual GetVisualChild(int index) => Visual;
        protected override int VisualChildrenCount => 1;

        private Visual Visual { get; }
    }
}

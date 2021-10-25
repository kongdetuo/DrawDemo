using System.Windows;
using System.Windows.Media;

namespace DrawDemo
{
    class TestPerformance
    {
        const string debug = "DEBUG";
        private const int rowCount = 100;
        private const int colCount = 100;

        public TestPerformance(DrawingMap map, DrawingCanvas canvas, Transform transform)
        {
            this.Map = map;
            this.DrawingHost = canvas;
            this.Transform = transform;

            //this.TestDrawing();
            this.TestGeometry();
            //this.TestPath();
        }

        public DrawingMap Map { get; }
        public DrawingCanvas DrawingHost { get; private set; }
        public Transform Transform { get; private set; }

        [System.Diagnostics.Conditional(debug)]
        public void TestDrawing()
        {
            // 一万元素可以接受
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    var visual = new DrawingVisual();
                    using (var dc = visual.RenderOpen())
                    {
                        var drawing = new CycleObj(new Point(i * 20, j * 20), 8).CreateDrawing();
                        drawing.Geometry.Transform = this.Transform;
                        dc.DrawDrawing(drawing);
                    }
                    this.DrawingHost.AddVisual(visual);
                }
            }
        }

        [System.Diagnostics.Conditional(debug)]
        public void TestGeometry()
        {
            // 一万元素可以接受
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    var visual = new DrawingVisual();
                    using (var dc = visual.RenderOpen())
                    {
                        var obj = new CycleObj(new Point(i * 20, j * 20), 8);
                        var geometry = obj.GetGeometry();
                        var group = new GeometryGroup();
                        group.Children.Add(geometry);
                        group.Transform = this.Transform;

                        dc.DrawGeometry(obj.FillColor, obj.Pen, group);
                    }
                    this.DrawingHost.AddVisual(visual);
                }
            }
        }

        [System.Diagnostics.Conditional(debug)]
        public void TestDrawingGroup()
        {
            // 显示无问题，缩放、平移过慢 
            var group = new DrawingGroup();
            group.Transform = this.Transform;
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    var drawing = new CycleObj(new Point(i * 20, j * 20), 8).CreateDrawing();
                    group.Children.Add(drawing);
                }
            }
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawDrawing(group);
            }
            this.DrawingHost.AddVisual(visual);
        }

        [System.Diagnostics.Conditional(debug)]
        public void TestPath()
        {
            // 使用Path直接承载Geometry，显示无问题，缩放、平移过慢
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    var obj = new CycleObj(new Point(i * 20, j * 20), 8);
                    var geometry = obj.GetGeometry();
                    var group = new GeometryGroup();
                    group.Children.Add(geometry);
                    group.Transform = this.Transform;

                    var path = new System.Windows.Shapes.Path();
                    path.Data = group;
                    path.Fill = obj.FillColor;
                    path.Stroke = obj.Pen.Brush;
                    path.StrokeThickness = obj.Pen.Thickness;

                    this.Map.topCanvas.Children.Add(path);
                }
            }
        }

    }
}


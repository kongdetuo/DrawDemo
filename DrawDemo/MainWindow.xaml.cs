using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrawDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var dpi = VisualTreeHelper.GetDpi(this);
            label1.Content = dpi.DpiScaleX;
        }

        private DrawActionBase DrawAction;

        private void DrawRectagle_Checked(object sender, RoutedEventArgs e)
        {
            DrawAction?.Dispose();
            DrawAction = new DrawRectAction(this.drawingSurface);
        }

        private void MultiSelect_Checked(object sender, RoutedEventArgs e)
        {
            DrawAction?.Dispose();
            DrawAction = new SelectionAction(this.drawingSurface);
        }

        private void Delete_Checked(object sender, RoutedEventArgs e)
        {
            DrawAction?.Dispose();
        }

        private void Move_Checked(object sender, RoutedEventArgs e)
        {







            var isSelect = false;
            Drawing drawing = null;
            Point startPoint = new Point();
            DrawingVisual selectedVisual = null;
            DrawingVisual fakeVisual = null;    // 用来高亮显示在最上层的元素
            var adorner = drawingSurface.GetAdorner();
            adorner.IsHitTestVisible = false;
            MouseLeftButtonDownAction = (s, arg) =>
            {
                if (selectedVisual != null)
                {
                    isSelect = true;
                    startPoint = arg.GetPosition(drawingSurface);
                    drawing = selectedVisual.Drawing;
                    drawingSurface.CaptureMouse();
                }
            };
            MouseLeftButtonUpAction = (s, arg) =>
            {
                selectedVisual = null;
                fakeVisual = null;
                isSelect = false;
                drawingSurface.ReleaseMouseCapture();
            };
            MouseMoveAction = (s, arg) =>
            {
                var pointClicked = arg.GetPosition(drawingSurface);
                if (isSelect)
                {
                    var offsetX = pointClicked.X - startPoint.X;
                    var offsetY = pointClicked.Y - startPoint.Y;

                    var drawingGroup = new DrawingGroup();
                    using (var dc = drawingGroup.Open())
                    {
                        var trans = new TranslateTransform(offsetX, offsetY);
                        dc.PushTransform(trans);
                        dc.DrawDrawing(drawing);
                        dc.Pop();
                    }
                    selectedVisual.SetDrawing(drawingGroup);
                    fakeVisual.SetDrawing(drawingGroup);
                    return;
                }
                else
                {
                    var visual = drawingSurface.GetVisual(pointClicked);
                    if (visual == fakeVisual)
                        return;
                    if (visual == null)
                    {
                        selectedVisual = null;
                        adorner.RemoveVisual(fakeVisual);
                        return;
                    }
                    if (fakeVisual != null && fakeVisual.Drawing == visual.Drawing)
                        return;

                    adorner.ClearVisuals();
                    selectedVisual = visual;
                    fakeVisual = new DrawingVisual();
                    using (var dc = fakeVisual.RenderOpen())
                    {
                        dc.PushOpacity(0.5);
                        dc.DrawDrawing(selectedVisual.Drawing);
                        dc.Pop();
                    }
                    fakeVisual.Effect = new DropShadowEffect()
                    {
                        Color = Colors.CadetBlue,
                        ShadowDepth = 0,
                        BlurRadius = 7
                    };
                    adorner.AddVisual(fakeVisual);
                }

            };

        }

        private DrawingMap AllObjs = new DrawingMap();

        private double PenThickness = 1;

        

        private void DrawLine_Checked(object sender, RoutedEventArgs e)
        {
            this.DrawAction?.Dispose();
            this.DrawAction = new DrawLineAction(this.drawingSurface);
        }

        double Scale = 1;
        private object lockObj = new object();

        private Point LeftTop = new Point(0, 0);
        private Point RightBottom = new Point(0, 0);


        private void DoScale(Point centerPoint, double scale)
        {

        }

        private void drawingSurface_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scale = 0d;
            Point leftTop;
            lock (lockObj)
            {
                scale = this.Scale;
                leftTop = this.LeftTop;
                var rightBottom = this.RightBottom;
                //var p = e.GetPosition(drawingSurface);
                var p = new Point(100, 100);
                var scaleCenter = leftTop + new Vector(p.X * scale, p.Y * scale);
                //scale += e.Delta * 0.001;
               scale += 0.1;
                if (scale < 0.1)
                    return;
                scaleCenter = new Point(scaleCenter.X * scale, scaleCenter.Y * scale);
                leftTop = scaleCenter - new Vector(p.X, p.Y) ;
                this.Scale = scale;
                this.LeftTop = leftTop;
                aaa.Text = scaleCenter.ToString();
            }

            var transform = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    new ScaleTransform(scale, scale),
                    new TranslateTransform(-LeftTop.X, -LeftTop.Y)
                }
            };
            var guide = GetGuidelineSet(leftTop, scale);
            foreach (var item in AllObjs.Values)
            {
                var visual = item.Item1;
                var drawObj = item.Item2;

                using (var dc = visual.RenderOpen())
                {
                    var dg = new GeometryGroup();
                    dg.Children.Add(drawObj.Geometry);
                    dg.Transform = (Transform)transform;
                    guide.GuidelinesX = new DoubleCollection();
                    var rect = dg.Bounds;
                    guide.GuidelinesX.Add(rect.X + 0.5);
                    guide.GuidelinesX.Add(rect.X + rect.Width + 0.5);
                    guide.GuidelinesY.Add(rect.Y + 0.5);
                    guide.GuidelinesY.Add(rect.Y + rect.Height + 0.5);
                    dc.PushGuidelineSet(guide);
                    dc.DrawGeometry(drawObj.FillColor, new Pen(drawObj.BorderColor, PenThickness), dg);
                    dc.Pop();
                }
            }
        }

        public GuidelineSet GetGuidelineSet(Point leftTop, double scale)
        {
            var guide = new GuidelineSet();
            //var height = this.drawingSurface.RenderSize.Height;
            //var width = this.drawingSurface.RenderSize.Width;
            //for (int i = 0; i < width; i++)
            //{
            //    var value = leftTop.X + i / scale;
            //    guide.GuidelinesX.Add(value + 0.5);
            //}
            //for (int i = 0; i < height; i++)
            //{
            //    var value = leftTop.Y + i / scale;
            //    guide.GuidelinesY.Add(value + 0.5);
            //}
            return guide;
        }


        private void DrawingSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Middle)
            {

            }
        }

        private void Unchecked(object sender, RoutedEventArgs e)
        {

        }
    }
}

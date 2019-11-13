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

        private bool MouseLeftDown;
        private Point MouseLeftDownPoint;
        private Action<object, MouseButtonEventArgs> MouseLeftButtonDownAction;
        private Action<object, MouseButtonEventArgs> MouseLeftButtonUpAction;
        private Action<object, MouseEventArgs> MouseMoveAction;
        private Action<object, MouseEventArgs> MouseMoveAction2;

        private void clearAction()
        {
            MouseLeftButtonDownAction = null;
            MouseLeftButtonUpAction = null;
            MouseMoveAction = null;
            MouseMoveAction2 = null;
        }


        private void DrawingSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var canvas = sender as DrawingCanvas;
                canvas.CaptureMouse();
                MouseLeftDownPoint = e.GetPosition(canvas);
                MouseLeftDown = true;
                MouseLeftButtonDownAction?.Invoke(sender, e);
            }
            catch (Exception)
            {
            }
        }

        private void DrawingSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MouseLeftDown = false;
                (sender as DrawingCanvas).ReleaseMouseCapture();
                MouseLeftButtonUpAction?.Invoke(sender, e);
                //MouseLeftDownPoint = new Point();
            }
            catch (Exception)
            {
            }
        }

        private void DrawingSurface_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (!MouseLeftDown)
                    MouseMoveAction?.Invoke(sender, e);
                else
                    MouseMoveAction2?.Invoke(sender, e);
            }
            catch (Exception)
            {
            }
        }

        private void DrawRectagle_Checked(object sender, RoutedEventArgs e)
        {
            MouseLeftButtonDownAction = (s, arg) =>
            {
                var geometry = new RectangleGeometry(new Rect(drawingSurface.RenderSize));
                //var pointClicked = arg.GetPosition(drawingSurface);
                //var visual = new DrawingVisual();
                //using (var dc = visual.RenderOpen())
                //{

                //    var g = new GuidelineSet(new[] { 0.5 }, new[] { 0.5 });
                //    dc.PushGuidelineSet(g);
                //    var brush = Brushes.AliceBlue;
                //    dc.DrawRectangle(brush, new Pen(Brushes.SteelBlue, 1), new Rect(pointClicked, new Size(30, 30)));
                //    dc.Pop();
                //    var formattedText = new FormattedText("MC", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("微软雅黑"), 24, Brushes.Black, 1);
                //    dc.DrawText(formattedText, pointClicked);
                //}

                var drawObj = new DrawObj()
                {
                    Geometry = new RectangleGeometry(new Rect(MouseLeftDownPoint, new Size(20, 20))),
                    FillColor = Brushes.AliceBlue,
                    BorderColor = Brushes.Black
                };

                var visual = new DrawingVisual();
                DrawObj(visual, drawObj);
                this.AllObjs.Add(drawObj, visual);
                drawingSurface.AddVisual(visual);
            };
            MouseLeftButtonUpAction = null;
            MouseMoveAction = null;
        }

        private void MultiSelect_Checked(object sender, RoutedEventArgs e)
        {
            List<DrawingVisual> visuals = new List<DrawingVisual>();
            if (!(sender as RadioButton).IsChecked.Value)
            {
                foreach (var item in visuals)
                {
                    item.Effect = null;
                }
                MouseLeftButtonDownAction = null;
                MouseMoveAction = null;
                MouseLeftButtonUpAction = null;
            }
            else
            {
                var visual = new DrawingVisual();
                var isMultiSelecting = false;
                var pointStart = new Point();
                MouseLeftButtonDownAction = (s, arg) =>
                {
                    isMultiSelecting = true;
                    visual = new DrawingVisual();
                    pointStart = arg.GetPosition(drawingSurface);
                    drawingSurface.AddVisual(visual);
                    drawingSurface.CaptureMouse();
                };
                MouseMoveAction = (s, arg) =>
                {
                    if (isMultiSelecting)
                    {
                        var pointCurrent = arg.GetPosition(drawingSurface);
                        pointCurrent.X = Math.Max(pointCurrent.X, 0);
                        pointCurrent.X = Math.Min(pointCurrent.X, drawingSurface.RenderSize.Width);
                        pointCurrent.Y = Math.Max(pointCurrent.Y, 0);
                        pointCurrent.Y = Math.Min(pointCurrent.Y, drawingSurface.RenderSize.Height);

                        using (var dc = visual.RenderOpen())
                        {
                            var pen = new Pen(Brushes.DarkGray, 2)
                            {
                                DashStyle = DashStyles.Dash
                            };

                            dc.DrawRectangle(Brushes.Transparent, pen, new Rect(pointStart, pointCurrent));
                        }
                    }
                };
                MouseLeftButtonUpAction = (s, arg) =>
                {
                    if (isMultiSelecting)
                    {
                        isMultiSelecting = false;

                        visuals = drawingSurface.GetVisuals(new RectangleGeometry(visual.Drawing.Bounds), true, true);
                        foreach (var item in visuals)
                        {
                            item.Effect = new DropShadowEffect()
                            {
                                Color = Colors.Black,
                                ShadowDepth = 0,
                                BlurRadius = 7
                            };
                        }

                        drawingSurface.RemoveVisual(visual);
                        drawingSurface.ReleaseMouseCapture();
                    }
                };
            }
        }

        private void Delete_Checked(object sender, RoutedEventArgs e)
        {
            DrawingVisual selectedVisual = null;
            DrawingVisual fakeVisual = null;    // 用来高亮显示在最上层的元素


            MouseLeftButtonDownAction = null;
            MouseLeftButtonUpAction = (s, arg) =>
            {
                drawingSurface.RemoveVisual(selectedVisual);
                drawingSurface.RemoveVisual(fakeVisual);
                selectedVisual = null;
                fakeVisual = null;
            };
            MouseMoveAction = (s, arg) =>
            {
                var pointClicked = arg.GetPosition(drawingSurface);
                var visual = drawingSurface.GetVisual(pointClicked);
                if (visual == fakeVisual)
                    return;
                if (visual == null)
                {
                    selectedVisual = null;
                    drawingSurface.RemoveVisual(fakeVisual);
                    return;
                }

                selectedVisual = visual;
                drawingSurface.RemoveVisual(fakeVisual);
                fakeVisual = new DrawingVisual();
                using (var dc = fakeVisual.RenderOpen())
                {
                    dc.DrawDrawing(selectedVisual.Drawing);
                }
                fakeVisual.Effect = new DropShadowEffect()
                {
                    Color = Colors.Black,
                    ShadowDepth = 0,
                    BlurRadius = 7
                };
                drawingSurface.AddVisual(fakeVisual);
            };
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

        private void DrawObj(DrawingVisual visual, DrawObj obj)
        {
            using (var dc = visual.RenderOpen())
            {
                dc.DrawGeometry(obj.FillColor, new Pen(obj.BorderColor, PenThickness), obj.Geometry);
            }
        }

        private void DrawLine_Checked(object sender, RoutedEventArgs e)
        {
            clearAction();




            var canvas = drawingSurface;
            var previewVisual = new DrawingVisual();
            var drawObj = new DrawObj();
            drawObj.BorderColor = Brushes.Black;
            drawObj.FillColor = Brushes.Black;
            MouseMoveAction2 = (s, arg) =>
            {
                if(canvas.GetAdorner().VisualCount == 0)
                    canvas.GetAdorner().AddVisual(previewVisual);

                var p1 = MouseLeftDownPoint;
                var p2 = arg.GetPosition(canvas);
                drawObj.Geometry = new LineGeometry(p1, p2);
                DrawObj(previewVisual, drawObj);
            };

            MouseLeftButtonUpAction = (s, arg) =>
            {
                canvas.GetAdorner().ClearVisuals();

                var p1 = MouseLeftDownPoint;
                var p2 = arg.GetPosition(canvas);

                drawObj = new DrawObj
                {
                    BorderColor = Brushes.Black,
                    FillColor = Brushes.Black,
                    Geometry = new LineGeometry(p1, p2)
                };

                var newVisual = new DrawingVisual();
                DrawObj(newVisual, drawObj);
                this.AllObjs.Add(newVisual, drawObj);
                canvas.AddVisual(newVisual);
            };
        }
    }
}

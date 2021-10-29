using Drawing2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Drawing2D
{
    public class DrawingCanvas : UserControl
    {

        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document",
                typeof(DrawingDocument),
                typeof(DrawingCanvas),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(DocumentChanged)));

        private static void DocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawingCanvas = d as DrawingCanvas;

            var oldDocument = e.OldValue as DrawingDocument;
            if (oldDocument != null)
            {
                oldDocument.Changed -= drawingCanvas.Document_Changed;
            }

            var document = e.NewValue as DrawingDocument;
            drawingCanvas.Init(document);
        }

        private void Init(DrawingDocument document)
        {
            visuals.Clear();
            IsSelecting = false;
            IsCanvasMoving = false;
            IsElementsMoveing = false;
            IsMouseLeftButtonDown = false;

            this.ElementLayer.ClearVisuals();
            this.PreviewSelectLayer.ClearVisuals();
            this.Canvas.Children.Clear();

            if (document != null)
            {
                document.Changed += Document_Changed;

                foreach (var item in document.GetElements())
                {
                    DrawElement(item);
                }
                if (this.IsLoaded)
                {
                    ZoomToCenter();
                }
            }
        }

        private void Document_Changed(object sender, DocumentChangedEventArgs e)
        {
            var document = sender as DrawingDocument;

            foreach (var item in e.RemovedElements)
            {
                RemoveElement(item);
            }

            foreach (var item in e.ModifiedElements)
            {
                UpdateElement(document.GetElement(item));
            }

            foreach (var item in e.AddedElements)
            {
                DrawElement(document.GetElement(item));
            }
        }

        public static readonly DependencyProperty DrawActionProperty =
                DependencyProperty.Register(
                    "DrawAction",
                    typeof(DrawingAction),
                    typeof(DrawingCanvas),
                    new FrameworkPropertyMetadata());


        private Canvas Canvas = new Canvas();
        private DrawingHost ElementLayer = new DrawingHost();
        private DrawingHost PreviewSelectLayer = new DrawingHost();
        private List<int> Selection = new List<int>();
        private Dictionary<int, ElementHostVisual> visuals = new Dictionary<int, ElementHostVisual>();

        private bool IsSelecting = false;
        private bool IsCanvasMoving = false;
        private bool IsElementsMoveing = false;
        private bool IsMouseLeftButtonDown = false;

        public DrawingDocument Document
        {
            get { return (DrawingDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        public DrawingAction DrawAction
        {
            get { return (DrawingAction)GetValue(DrawActionProperty); }
            set { SetValue(DrawActionProperty, value); }
        }


        public event EventHandler SelectionChanged;

        private double ZoomScale;
        private TranslateTransform TranslateTransform;
        private ScaleTransform ScaleTransform;
        public Transform Transform { get; set; }

        private TipService TipService;

        public DrawingCanvas()
        {
            this.Background = Brushes.White;

            var grid = new Grid();
            grid.Children.Add(ElementLayer);
            grid.Children.Add(PreviewSelectLayer);
            grid.Children.Add(Canvas);
            this.Content = grid;

            this.TipService = new TipService(this);

            TranslateTransform = new TranslateTransform();
            ScaleTransform = new ScaleTransform(1, -1);
            Transform = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    ScaleTransform,
                    TranslateTransform
                }
            };

            this.Loaded += DrawingCanvas_Loaded;
#if DEBUG
            var stackPanel = new StackPanel();
            stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
            stackPanel.VerticalAlignment = VerticalAlignment.Bottom;
            grid.Children.Add(stackPanel);

            stackPanel.Children.Add(new TextBlock() { Text = "调试模式", Foreground = Brushes.Red });
            stackPanel.Children.Add(scaleTextBlock);
            stackPanel.Children.Add(positionBlock);

            debugTransform = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    ScaleTransform,
                    TranslateTransform
                }
            };
#endif
        }
#if DEBUG
        TextBlock scaleTextBlock = new TextBlock()
        {
            Foreground = Brushes.Red,
        };
        TextBlock positionBlock = new TextBlock()
        {
            Foreground = Brushes.Red
        };

        Pen debugBoxPen = new Pen(Brushes.Red, 1);

        Transform debugTransform;
#endif
        private void DrawingCanvas_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            this.BorderBrush = Brushes.Green;
            this.BorderThickness = new Thickness(1);
#endif
            if (this.Document != null)
            {
                //foreach (var item in Document.GetElements())
                //{
                //    DrawElement(item);
                //}

                ZoomToCenter();
            }
        }

        protected virtual void OnSelectionChanged()
        {
            this.SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Events

        protected override void OnKeyDown(KeyEventArgs e)
        {

            if (e.Key == Key.Escape)
            {
                if (IsSelecting)
                {
                    IsSelecting = false;
                }
                else if (Selection.Count > 0)
                {
                    Selection.Clear();
                    OnSelectionChanged();
                }

                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
        }

        Point MouseDownPoint;
        Point PreviousMousePoint;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                PreviousMousePoint = MouseDownPoint = e.GetPosition(this);
                this.CaptureMouse();

                if (e.ChangedButton == MouseButton.Middle)
                {
                    this.IsCanvasMoving = true;
                }
            }
            else if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Middle)
            {
                ZoomToCenter();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
            if (DrawAction != null && DrawAction.IsDrawing)     // 可能是在绘制元素
            {
                DrawAction.IsDrawing = false;
            }
            else if (IsCanvasMoving)      // 可能是在移动画布，什么都不用做
            {
                IsCanvasMoving = false;
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(this);
            var moveOffset = currentPoint - PreviousMousePoint;
            PreviousMousePoint = currentPoint;

            // 必须优先处理画布移动事件, 否则坐标不正确
            if (IsCanvasMoving)
            {
                this.TranslateTransform.X += moveOffset.X;
                this.TranslateTransform.Y += moveOffset.Y;
            }
#if DEBUG
            var position = this.Transform.Inverse.Transform(currentPoint);
            this.positionBlock.Text = $"当前位置：{position}";
#endif

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var p = e.GetPosition(this);                          //当前视图坐标点
            var scaleCenter = this.Transform.Inverse.Transform(p);// 当前真实坐标点

            var ratio = e.Delta > 0 ? 1.1 : 1 / 1.1;
            var power = Math.Max(1, Math.Abs(e.Delta / 100));

            var newScale = this.ScaleTransform.ScaleX * Math.Pow(ratio, power);
            if ((newScale / ZoomScale) < 0.0001 || (newScale / ZoomScale) > 10000) // 缩放一万倍
                return;
            ChangeScale(p, scaleCenter, newScale);
            base.OnMouseWheel(e);
        }
        #endregion

        private void DrawElement(DrawingElement element)
        {
            var visual = new ElementHostVisual();
            this.visuals[element.Id] = visual;
            this.ElementLayer.AddVisual(visual);

            DrawElement(visual, element);
        }

        private void UpdateElement(DrawingElement element)
        {
            var visual = visuals[element.Id];
            DrawElement(visual, element);
        }

        private void DrawElement(ElementHostVisual visual, DrawingElement element)
        {
            var rect = Rect.Empty;
            visual.Element = element;
            using (var dc = visual.RenderOpen())
            {
                foreach (var item in GetChildAndSelf(element))
                {
                    var geometry = item.GetGeometry();
                    var geometryGroup = new GeometryGroup();
                    geometryGroup.Children.Add(geometry);


                    geometryGroup.Transform = this.Transform;

                    dc.DrawGeometry(item.FillBrush, item.Pen, geometryGroup);

                    rect.Union(geometry.Bounds);
                }

#if DEBUG
                var testBound = new RectangleGeometry(rect, 0, 0, this.Transform);
                dc.DrawGeometry(null, this.debugBoxPen, testBound);
#endif
            }
            visual.RowBound = rect;
        }
        /// <summary>
        /// 所有图形
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private IEnumerable<DrawingElement> GetChildAndSelf(DrawingElement element)
        {
            IEnumerable<DrawingElement> elements = new List<DrawingElement>() { element };
            if (element.ChildIds != null && element.ChildIds.Count > 0)
            {
                foreach (var item in element.ChildIds)
                {
                    elements = elements.Concat(GetChildAndSelf(element.Document.GetElement(item)));
                }
            }
            return elements;
        }

        private void RemoveElement(int id)
        {
            if (visuals.ContainsKey(id))
            {
                this.ElementLayer.RemoveVisual(visuals[id]);
                this.visuals.Remove(id);
            }
        }

        /// <summary>
        /// 修改缩放比例
        /// </summary>
        /// <param name="screenPoint">控件上的点</param>
        /// <param name="rawPoint">控件点对应的真实坐标点</param>
        /// <param name="scale">新的缩放比例</param>
        private void ChangeScale(Point screenPoint, Point rawPoint, double scale)
        {
            var scaleTransform = new ScaleTransform(scale, -scale);
            var translateVector = -(Vector)scaleTransform.Transform(rawPoint) + new Vector(screenPoint.X, screenPoint.Y); // 先移动到0，0点，再移动到缩放中心

            this.ScaleTransform.ScaleX = scale;
            this.ScaleTransform.ScaleY = -scale;

            this.TranslateTransform.X = translateVector.X;
            this.TranslateTransform.Y = translateVector.Y;

#if DEBUG
            this.scaleTextBlock.Text = $"缩放比例：{scale}";
#endif
        }

        /// <summary>
        /// 缩放到屏幕中心
        /// </summary>
        private void ZoomToCenter()
        {
            if (this.visuals.Count == 0)
            {
                return;
            }

            var rects = this.visuals.Values.Select(p => p.RowBound).ToList();
            var top = rects.Max(p => p.Y + p.Height);
            var bottom = rects.Min(p => p.Y);
            var left = rects.Min(p => p.X);
            var right = rects.Max(p => p.X + p.Width);

            var center = new Point((left + right) / 2, (top + bottom) / 2);

            var width = right - left;
            var height = top - bottom;

            var actualWidth = this.ActualWidth;
            var actualHeight = this.ActualHeight;

            // 边缘稍微留白，防止显得太过拥挤
            if (actualWidth > 200)
                actualWidth -= 40;
            if (actualHeight > 200)
                actualHeight -= 40;

            var scale1 = actualWidth / width;
            var scale2 = actualHeight / height;
            this.ZoomScale = Math.Min(scale1, scale2);
            ChangeScale(new Point(this.ActualWidth / 2, this.ActualHeight / 2), center, ZoomScale);
        }

        /// <summary>
        /// 尝试选中元素
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private List<ElementHostVisual> TrySelect(Point start, Point end)
        {
            return new List<ElementHostVisual>();
        }
    }

}

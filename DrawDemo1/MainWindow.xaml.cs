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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrawDemo1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            PathVMs = new List<PathVM>();

            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    PathVMs.Add(new PathVM()
                    {
                        Geometry = new RectangleGeometry(new Rect(i * 20, j * 20, 16, 16))
                    });
                }
            }

            canvas.ItemsSource = PathVMs;



        }
        List<PathVM> PathVMs;
        bool isss = false;
        Point previous;
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            isss = true;
            previous = e.GetPosition(this.canvas);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            isss = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var p = e.GetPosition(this.canvas);
            var v = p - previous;
            previous = p;
            if (isss)
            {
                var tr = new TranslateTransform(v.X, v.Y);
                foreach (var item in PathVMs)
                {
                    var transform = item.Geometry.Transform;
                    if (transform == null)
                        transform = new TransformGroup();

                    var group = new TransformGroup();
                    if (transform != null)
                        group.Children.Add(tr);
                    group.Children.Add(tr);
                    item.Geometry.Transform = group;
                }
            }

            base.OnMouseMove(e);
        }
    }

    class PathVM
    {
        public Geometry Geometry { get; set; }
    }
}

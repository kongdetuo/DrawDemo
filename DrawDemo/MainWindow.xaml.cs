using Drawing2D;
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

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.drawingCanvas.Document = new TestDocument();



        }

        private void DrawRectagle_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void MultiSelect_Checked(object sender, RoutedEventArgs e)
        {
           
        }
        private void DrawLine_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void drawText_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new Window1().ShowDialog();
        }
    }
}

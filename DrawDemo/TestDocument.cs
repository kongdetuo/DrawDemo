using Drawing2D;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawDemo
{
    public class TestDocument : DrawingDocument
    {
        public override IEnumerable<DrawingElement> LoadElements()
        {
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    yield return new CycleObj(this, new Point(i * 20, j * 20), 8);
                }
            }
        }
    }

}

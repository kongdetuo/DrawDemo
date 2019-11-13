using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DrawDemo
{
    public class DrawObj
    {
        /// <summary>
        /// 填充色
        /// </summary>
        public Brush FillColor { get; set; }

        /// <summary>
        /// 描边色
        /// </summary>
        public Brush BorderColor { get; set; }

        public List<EdgeObj> Edges { get; private set; }

        public Geometry Geometry { get; set; }
        public Geometry GetGeometry()
        {
            return Geometry;
        }
    }
}

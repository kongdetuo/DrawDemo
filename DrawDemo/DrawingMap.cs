using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DrawDemo
{
    public class DrawingMap : Map<DrawingVisual, DrawObj>
    {
        public List<DrawingVisual> GetAllVisuals() => map1.Keys.ToList();

        public List<DrawObj> GetDrawObjs() => map1.Values.ToList();
    }
}

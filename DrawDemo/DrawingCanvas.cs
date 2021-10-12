using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DrawDemo
{
    public class DrawingCanvas : Canvas
    {

        public DrawingCanvas()
        {
            Background = Brushes.White;
        }
        private readonly List<DrawingLayer> drawingLayers = new List<DrawingLayer>();
        private readonly List<int> layerVisualCounts = new List<int>();
        private readonly List<Visual> visuals = new List<Visual>();

        protected override int VisualChildrenCount => visuals.Count;

        public int VisualCount => visuals.Count;
        protected override Visual GetVisualChild(int index)
        {
            return visuals[index];
        }

        public DrawingVisual GetVisual(Point point, Func<Visual, bool> filter = null, int tol = 3)
        {
            //var hitTestResult = VisualTreeHelper.HitTest(this, point);
            //var result = hitTestResult.VisualHit as DrawingVisual;
            //if (result == null)
            //{
            DrawingVisual result = null;
            var vec = new Vector(tol, tol);
            var rect = new Rect(point - vec, point + vec);
            var geo = new RectangleGeometry(rect);
            VisualTreeHelper.HitTest(this, null, hitResult =>
             {
                 var geometryResult = hitResult as GeometryHitTestResult;
                 result = geometryResult.VisualHit as DrawingVisual;

                 if (result != null && filter?.Invoke(result) == true)
                 {
                     return HitTestResultBehavior.Stop;
                 }
                 return HitTestResultBehavior.Continue;
             }, new GeometryHitTestParameters(geo));
            //}
            return result;
        }

        public List<DrawingVisual> GetVisuals(Geometry geometry, Func<Visual, bool> filter, bool fullyInside = false, bool intersets = false, bool fullyContains = false)
        {
            var hits = new List<DrawingVisual>();
            VisualTreeHelper.HitTest(this, null, hitResult =>
            {
                var tem = this.visuals;
                var geometryResult = hitResult as GeometryHitTestResult;
                var visual = geometryResult.VisualHit as DrawingVisual;
                if (visual != null && filter?.Invoke(visual) == true)
                {
                    if (fullyInside && geometryResult.IntersectionDetail == IntersectionDetail.FullyInside)
                        hits.Add(visual);
                    else if (intersets && geometryResult.IntersectionDetail == IntersectionDetail.Intersects)
                        hits.Add(visual);
                    else if (fullyContains && geometryResult.IntersectionDetail == IntersectionDetail.FullyContains)
                        hits.Add(visual);
                }
                return HitTestResultBehavior.Continue;
            },
            new GeometryHitTestParameters(geometry));
            return hits;
        }

        public void AddVisual(DrawingLayer layer, Visual visual)
        {
            if (visual == null)
                return;

            var index = drawingLayers.TakeWhile(p => p != layer).Append(layer).Sum(p => layerVisualCounts[p.ID]);
            visuals.Insert(index, visual);
            layerVisualCounts[layer.ID]++;
            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }

        public void RemoveVisual(Visual visual)
        {
            if (visual == null)
                return;
            visuals.Remove(visual);
            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }

        public void ClearVisuals()
        {
            var list = new List<Visual>(this.visuals);
            this.visuals.Clear();
            foreach (var item in list)
            {
                base.RemoveVisualChild(item);
                base.RemoveLogicalChild(item);
            }
        }

        public DrawingLayer AddDrawingLayer()
        {
            var layer = new DrawingLayer(drawingLayers.Count);
            drawingLayers.Add(layer);
            layerVisualCounts.Add(0);
            return layer;
        }

        public void RemoveDrawingLayer(DrawingLayer layer)
        {
            if (drawingLayers.Contains(layer))
            {
                int i = 0;
                for (; i < layer.ID; i++)
                {
                    i += layerVisualCounts[i];
                }

                if(layerVisualCounts[i] > 0)
                {
                    for (int j = layerVisualCounts[i] - 1; j >= 0; j--)
                    {
                        RemoveVisual(visuals[j]);
                    }
                }

                layerVisualCounts[i] = 0;
                // 删除所有Visual就行了，layer本身不用删掉
            }
        }

    }

    public class DrawingLayer
    {
        public DrawingLayer(int id)
        {
            this.ID = id;
            this.CanHitTest = true;
        }

        public int ID { get; private set; }

        public bool CanHitTest { get; set; }
    }
}

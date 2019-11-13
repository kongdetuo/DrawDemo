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
        private readonly List<Visual> visuals = new List<Visual>();

        protected override int VisualChildrenCount => visuals.Count;

        public int VisualCount => visuals.Count;
        protected override Visual GetVisualChild(int index)
        {
            return visuals[index];
        }

        public DrawingConvasAdorner GetAdorner()
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(this);

            var adorner = adornerLayer.GetAdorners(this)
                ?.OfType<DrawingConvasAdorner>()
                .FirstOrDefault();
            if(adorner == null)
            {
                adorner = new DrawingConvasAdorner(this);
                adorner.IsHitTestVisible = false;
                adorner.ClipToBounds = true;
                adornerLayer.Add(adorner);
            }
            return adorner;
        }
        public DrawingVisual GetVisual(Point point)
        {
            var hitTestResult = VisualTreeHelper.HitTest(this, point);
            return hitTestResult.VisualHit as DrawingVisual;
        }

        public List<DrawingVisual> GetVisuals(Geometry geometry, bool fullyInside = false, bool intersets = false, bool fullyContains = false)
        {
            var hits = new List<DrawingVisual>();
            VisualTreeHelper.HitTest(this, null, hitResult =>
            {
                var geometryResult = hitResult as GeometryHitTestResult;
                var visual = geometryResult.VisualHit as DrawingVisual;
                if (visual != null)
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

        public void AddVisual(Visual visual)
        {
            if (visual == null)
                return;
            visuals.Add(visual);
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
    }


    public static class DrawingVisualHelper
    {
        public static void SetDrawing(this DrawingVisual drawingVisual, Drawing drawing)
        {
            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawDrawing(drawing);
            }
        }
    }
}

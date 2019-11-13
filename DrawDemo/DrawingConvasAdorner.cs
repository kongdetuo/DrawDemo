using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace DrawDemo
{
    public class DrawingConvasAdorner : Adorner
    {
        private readonly List<Visual> visuals = new List<Visual>();


        public DrawingConvasAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        protected override int VisualChildrenCount => visuals.Count;

        public int VisualCount => visuals.Count;
        protected override Visual GetVisualChild(int index) => visuals[index];

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
    }
}

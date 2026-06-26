using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// ==============================================================================
//  JbRegionMap: Static routing-fabric value types (regions and edge gates) and the debug overlay element.
// ==============================================================================
namespace JuiceBox
{
    internal enum JbEdgeDir { None, In, Out, InOut }

    internal struct JbRegion
    {
        public Rect Rect;
        public bool Channel;
        public JbEdgeDir Top;
        public JbEdgeDir Right;
        public JbEdgeDir Bottom;
        public JbEdgeDir Left;
    }

    internal sealed class JbRegionMap
    {
        internal static bool ShowDebug = false;
        public readonly List<JbRegion> Regions = new List<JbRegion>();
    }

    internal sealed class JbRegionOverlay : VisualElement
    {
        private readonly JbRegionMap _map;

        public JbRegionOverlay(JbRegionMap map)
        {
            _map = map;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.overflow = Overflow.Visible;
            generateVisualContent += OnDraw;
        }

        public void Refresh() => MarkDirtyRepaint();

        private void OnDraw(MeshGenerationContext ctx)
        {
            if (!JbRegionMap.ShowDebug) return;
            Painter2D p = ctx.painter2D;
            var regions = _map.Regions;
            for (int i = 0; i < regions.Count; i++)
            {
                JbRegion r = regions[i];
                Rect rc = r.Rect;

                p.fillColor = r.Channel
                    ? new Color(0.30f, 0.60f, 0.90f, 0.15f)
                    : new Color(0.80f, 0.35f, 0.35f, 0.13f);
                p.BeginPath();
                p.MoveTo(new Vector2(rc.xMin, rc.yMin));
                p.LineTo(new Vector2(rc.xMax, rc.yMin));
                p.LineTo(new Vector2(rc.xMax, rc.yMax));
                p.LineTo(new Vector2(rc.xMin, rc.yMax));
                p.ClosePath();
                p.Fill();

                Edge(p, rc.xMin, rc.yMin, rc.xMax, rc.yMin, r.Top);
                Edge(p, rc.xMax, rc.yMin, rc.xMax, rc.yMax, r.Right);
                Edge(p, rc.xMin, rc.yMax, rc.xMax, rc.yMax, r.Bottom);
                Edge(p, rc.xMin, rc.yMin, rc.xMin, rc.yMax, r.Left);
            }
        }

        private static void Edge(Painter2D p, float x0, float y0, float x1, float y1, JbEdgeDir d)
        {
            switch (d)
            {
                case JbEdgeDir.In: p.strokeColor = new Color(0.37f, 0.81f, 0.44f); p.lineWidth = 2.5f; break;
                case JbEdgeDir.Out: p.strokeColor = new Color(0.88f, 0.57f, 0.18f); p.lineWidth = 2.5f; break;
                case JbEdgeDir.InOut: p.strokeColor = new Color(0.30f, 0.60f, 0.90f, 0.6f); p.lineWidth = 1.25f; break;
                default: p.strokeColor = new Color(0.55f, 0.55f, 0.60f, 0.4f); p.lineWidth = 1f; break;
            }
            p.BeginPath();
            p.MoveTo(new Vector2(x0, y0));
            p.LineTo(new Vector2(x1, y1));
            p.Stroke();
        }
    }
}
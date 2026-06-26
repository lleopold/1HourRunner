using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

// ==============================================================================
//  SequenceGraphView.Routing: Recomputes channel-edge routes from the region map and simplifies the polylines.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView
    {
        private void RouteEdges()
        {
            BuildChannelGraph();
            BeginRoutes();

            var en = edges.GetEnumerator();
            while (en.MoveNext())
            {
                JbChannelEdge e = en.Current as JbChannelEdge;
                if (e == null) continue;
                JbChannelEdgeControl ctrl = e.ChannelControl;
                if (ctrl == null) continue;
                ctrl.SetGraphSpace(contentViewContainer);
                ctrl.StrokeWidth = _theme.EdgeWidth;
                ctrl.CornerRadius = _theme.EdgeCornerRadius;

                Port outp = e.output;
                Port inp = e.input;
                if (outp == null || inp == null) continue;

                Color colOut = outp.portColor;
                colOut.a = 1f;
                Color colIn = inp.portColor;
                colIn.a = 1f;
                ctrl.StrokeColor = colOut;
                ctrl.StrokeColorTo = colIn;

                Vector2 po = contentViewContainer.WorldToLocal(outp.GetGlobalCenter());
                Vector2 pi = contentViewContainer.WorldToLocal(inp.GetGlobalCenter());

                Dir dOut = DirectionFor(outp);
                Dir dIn = DirectionFor(inp);

                var pts = NextRoute(ctrl).Pts;
                if (!RouteThroughChannels(po, pi, dOut, dIn, pts))
                {
                    pts.Clear();
                    pts.Add(po);
                    pts.Add(new Vector2(pi.x, po.y));
                    pts.Add(pi);
                }
                Simplify(pts);
            }

            SeparateLanes();

            for (int i = 0; i < _allRoutes.Count; i++)
                _allRoutes[i].Ctrl.SetRoute(_allRoutes[i].Pts);
        }

        private static void Simplify(List<Vector2> pts)
        {
            for (int i = pts.Count - 2; i >= 1; i--)
            {
                Vector2 a = pts[i - 1], b = pts[i], c = pts[i + 1];
                if ((b - a).sqrMagnitude < 1e-4f) { pts.RemoveAt(i); continue; }
                if ((c - b).sqrMagnitude < 1e-4f) { pts.RemoveAt(i); continue; }
                Vector2 ab = (b - a).normalized;
                Vector2 bc = (c - b).normalized;
                float d = Vector2.Dot(ab, bc);
                if (d > 0.9999f || d < -0.9999f) pts.RemoveAt(i);
            }
            if (pts.Count >= 2 &&
                (pts[pts.Count - 1] - pts[pts.Count - 2]).sqrMagnitude < 1e-4f)
                pts.RemoveAt(pts.Count - 1);
        }
    }
}
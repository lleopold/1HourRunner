using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

// ==============================================================================
//  JbChannelEdge: Channel-routed edge plus its EdgeControl - a rounded, gradient Painter2D polyline with a loose-drag preview.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class JbChannelEdge : Edge
    {
        internal JbChannelEdgeControl ChannelControl { get; private set; }

        protected override EdgeControl CreateEdgeControl()
        {
            ChannelControl = new JbChannelEdgeControl();
            return ChannelControl;
        }

        public override bool UpdateEdgeControl()
        {
            if (ChannelControl != null)
                ChannelControl.Loose = (input == null || output == null);
            return base.UpdateEdgeControl();
        }
    }

    internal sealed class JbChannelEdgeControl : EdgeControl
    {
        private readonly List<Vector2> _graphPts = new List<Vector2>();
        private readonly List<Vector2> _localPts = new List<Vector2>();
        private readonly List<Vector2> _densePts = new List<Vector2>();
        private VisualElement _graphSpace;

        internal float StrokeWidth = 3f;
        internal float CornerRadius = 6f;
        internal Color StrokeColor = Color.white;
        internal Color StrokeColorTo = Color.white;
        internal bool Loose;

        public JbChannelEdgeControl()
        {
            generateVisualContent = OnGenerate;
            style.overflow = Overflow.Visible;
        }

        internal void SetGraphSpace(VisualElement graphSpace)
        {
            _graphSpace = graphSpace;
        }

        internal void SetRoute(List<Vector2> graphPts)
        {
            _graphPts.Clear();
            if (graphPts != null)
                for (int i = 0; i < graphPts.Count; i++)
                    _graphPts.Add(graphPts[i]);
            MarkDirtyRepaint();
        }

        private void RebuildLocal()
        {
            _localPts.Clear();
            if (_graphSpace == null) return;
            for (int i = 0; i < _graphPts.Count; i++)
            {
                Vector2 world = _graphSpace.LocalToWorld(_graphPts[i]);
                _localPts.Add(this.WorldToLocal(world));
            }
        }

        private void OnGenerate(MeshGenerationContext ctx)
        {
            if (Loose)
            {
                Painter2D p = ctx.painter2D;
                p.lineWidth = StrokeWidth;
                p.lineJoin = LineJoin.Round;
                p.lineCap = LineCap.Round;

                Color c = StrokeColor;
                c.a *= 0.5f;
                p.strokeColor = c;

                Vector2 a = from, b = to;
                float dir = 1f;
                Edge owner = GetFirstAncestorOfType<Edge>();
                if (owner != null)
                {
                    Port port = owner.output != null ? owner.output : owner.input;
                    if (port != null)
                    {
                        a = this.WorldToLocal(port.GetGlobalCenter());
                        if (port.direction == Direction.Input) dir = -1f;
                    }
                    b = this.WorldToLocal(owner.candidatePosition);
                }

                float k = Mathf.Max(24f, Mathf.Abs(b.x - a.x) * 0.5f);
                Vector2 c1 = new Vector2(a.x + dir * k, a.y);
                Vector2 c2 = new Vector2(b.x - dir * k, b.y);

                p.BeginPath();
                p.MoveTo(a);
                p.BezierCurveTo(c1, c2, b);
                p.Stroke();
                return;
            }

            RebuildLocal();
            if (_localPts.Count < 2) return;
            BuildRoundedPath(_localPts, _densePts);
            DrawGradientStroke(ctx, _densePts, StrokeColor, StrokeColorTo);
        }

        private void BuildRoundedPath(List<Vector2> src, List<Vector2> dst)
        {
            dst.Clear();
            int n = src.Count;
            if (n == 0) return;
            if (n <= 2)
            {
                for (int i = 0; i < n; i++) dst.Add(src[i]);
                return;
            }

            const int segs = 5;
            dst.Add(src[0]);
            for (int i = 1; i < n - 1; i++)
            {
                Vector2 a = src[i - 1], b = src[i], cc = src[i + 1];
                Vector2 din = b - a, dout = cc - b;
                float lin = din.magnitude, lout = dout.magnitude;
                if (lin < 1e-4f || lout < 1e-4f) { dst.Add(b); continue; }
                din /= lin; dout /= lout;
                float d = Mathf.Min(CornerRadius, lin * 0.5f, lout * 0.5f);
                Vector2 p1 = b - din * d;
                Vector2 p2 = b + dout * d;
                dst.Add(p1);
                for (int s = 1; s < segs; s++)
                {
                    float t = s / (float)segs;
                    float u = 1f - t;
                    dst.Add(u * u * p1 + 2f * u * t * b + t * t * p2);
                }
                dst.Add(p2);
            }
            dst.Add(src[n - 1]);
        }

        private void DrawGradientStroke(MeshGenerationContext ctx, List<Vector2> path,
                                        Color cFrom, Color cTo)
        {
            int n = path.Count;
            if (n < 2) return;

            float total = 0f;
            for (int i = 0; i + 1 < n; i++) total += Vector2.Distance(path[i], path[i + 1]);
            if (total < 1e-4f) return;

            float half = StrokeWidth * 0.5f;
            const float fadeSpan = 0.5f;
            float fadeStart = (1f - fadeSpan) * 0.5f;
            float fadeEnd = 1f - fadeStart;
            MeshWriteData mwd = ctx.Allocate(n * 2, (n - 1) * 6);

            float acc = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 dir;
                if (i == 0) dir = path[1] - path[0];
                else if (i == n - 1) dir = path[n - 1] - path[n - 2];
                else dir = path[i + 1] - path[i - 1];
                if (dir.sqrMagnitude < 1e-8f) dir = new Vector2(1f, 0f);
                dir.Normalize();
                Vector2 nrm = new Vector2(-dir.y, dir.x);

                float blend = Mathf.InverseLerp(fadeStart, fadeEnd, acc / total);
                Color col = Color.Lerp(cFrom, cTo, blend);
                Vector2 lft = path[i] + nrm * half;
                Vector2 rgt = path[i] - nrm * half;

                mwd.SetNextVertex(new Vertex
                { position = new Vector3(lft.x, lft.y, Vertex.nearZ), tint = col });
                mwd.SetNextVertex(new Vertex
                { position = new Vector3(rgt.x, rgt.y, Vertex.nearZ), tint = col });

                if (i + 1 < n) acc += Vector2.Distance(path[i], path[i + 1]);
            }

            for (int i = 0; i + 1 < n; i++)
            {
                ushort l0 = (ushort)(2 * i);
                ushort r0 = (ushort)(2 * i + 1);
                ushort l1 = (ushort)(2 * i + 2);
                ushort r1 = (ushort)(2 * i + 3);
                mwd.SetNextIndex(l0); mwd.SetNextIndex(r0); mwd.SetNextIndex(l1);
                mwd.SetNextIndex(r0); mwd.SetNextIndex(r1); mwd.SetNextIndex(l1);
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (Loose) return false;
            RebuildLocal();
            if (_localPts.Count < 2) return false;
            float tol = StrokeWidth * 0.5f + 4f;
            for (int i = 0; i < _localPts.Count - 1; i++)
                if (PointSegDist(localPoint, _localPts[i], _localPts[i + 1]) <= tol)
                    return true;
            return false;
        }

        public override bool Overlaps(Rect rect)
        {
            if (Loose) return false;
            RebuildLocal();
            for (int i = 0; i < _localPts.Count - 1; i++)
                if (SegHitsRect(rect, _localPts[i], _localPts[i + 1]))
                    return true;
            return false;
        }

        private static float PointSegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        private static bool SegHitsRect(Rect r, Vector2 a, Vector2 b)
        {
            if (r.Contains(a) || r.Contains(b)) return true;
            for (int s = 1; s < 8; s++)
                if (r.Contains(Vector2.Lerp(a, b, s / 8f))) return true;
            return false;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

// ==============================================================================
//  SequenceGraphView.PathRouter: Channel graph, A* routing, lane separation, and per-port exit directions.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView
    {
        private struct PortConn
        {
            public int node;
            public Vector2 junction;
            public float cost;
        }

        private enum Dir { Left, Right, Up, Down }

        private static Dir DirectionFor(Port p)
        {
            Node n = p.GetFirstAncestorOfType<Node>();
            if (n is SmoothingNode)
                return Dir.Right;
            if (n is HookNode)
                return Dir.Up;
            if (n is EffectNode)
                return p.direction == Direction.Input
                    ? Dir.Up : Dir.Down;
            return p.direction == Direction.Output ? Dir.Down : Dir.Up;
        }

        private readonly List<float> _vxs = new List<float>();
        private readonly List<float> _hys = new List<float>();
        private readonly List<Rect> _chan = new List<Rect>();
        private readonly Dictionary<int, float> _vWidth = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _hWidth = new Dictionary<int, float>();

        private sealed class RouteData
        {
            public JbChannelEdgeControl Ctrl;
            public readonly List<Vector2> Pts = new List<Vector2>();
            public readonly Dictionary<int, float> OffV = new Dictionary<int, float>();
            public readonly Dictionary<int, float> OffH = new Dictionary<int, float>();
        }

        private readonly List<RouteData> _allRoutes = new List<RouteData>();
        private readonly List<RouteData> _routePool = new List<RouteData>();
        private int _routeUsed;
        private List<int>[] _gridAdj;
        private int _gV, _gH;

        private const float Bridge = 3f;

        private readonly List<Vector2> _ivals = new List<Vector2>();
        private static readonly System.Comparison<Vector2> ByMin =
            (a, b) => a.x.CompareTo(b.x);

        private readonly List<PortConn> _srcConn = new List<PortConn>();
        private readonly List<PortConn> _dstConn = new List<PortConn>();
        private readonly List<int> _path = new List<int>();

        private float[] _g;
        private int[] _from;
        private bool[] _closed;
        private readonly List<int> _open = new List<int>();
        private Vector2 _routeDst;

        private void BuildChannelGraph()
        {
            _vxs.Clear();
            _hys.Clear();
            _chan.Clear();
            _vWidth.Clear();
            _hWidth.Clear();
            if (_regionMap == null) return;

            var regions = _regionMap.Regions;
            for (int i = 0; i < regions.Count; i++)
            {
                if (!regions[i].Channel) continue;
                Rect r = regions[i].Rect;
                _chan.Add(r);
                if (r.height >= r.width) { AddUnique(_vxs, r.center.x); RegWidth(_vWidth, r.center.x, r.width); }
                else { AddUnique(_hys, r.center.y); RegWidth(_hWidth, r.center.y, r.height); }
            }
            _vxs.Sort();
            _hys.Sort();

            _gV = _vxs.Count;
            _gH = _hys.Count;
            int n = _gV * _gH;
            _gridAdj = new List<int>[n];

            for (int vi = 0; vi < _gV; vi++)
                for (int hi = 0; hi + 1 < _gH; hi++)
                    if (SegClear(true, _vxs[vi], _hys[hi], _hys[hi + 1]))
                        Link(vi * _gH + hi, vi * _gH + hi + 1);

            for (int hi = 0; hi < _gH; hi++)
                for (int vi = 0; vi + 1 < _gV; vi++)
                    if (SegClear(false, _hys[hi], _vxs[vi], _vxs[vi + 1]))
                        Link(vi * _gH + hi, (vi + 1) * _gH + hi);
        }

        private static void AddUnique(List<float> list, float v)
        {
            for (int i = 0; i < list.Count; i++)
                if (Mathf.Abs(list[i] - v) < 0.5f) return;
            list.Add(v);
        }

        private void Link(int a, int b)
        {
            if (_gridAdj[a] == null) _gridAdj[a] = new List<int>();
            if (_gridAdj[b] == null) _gridAdj[b] = new List<int>();
            _gridAdj[a].Add(b);
            _gridAdj[b].Add(a);
        }

        private bool SegClear(bool vertical, float fixedC, float a, float b)
        {
            if (b < a) { float t = a; a = b; b = t; }
            _ivals.Clear();
            for (int i = 0; i < _chan.Count; i++)
            {
                Rect r = _chan[i];
                if (vertical)
                {
                    if (fixedC < r.xMin - 0.5f || fixedC > r.xMax + 0.5f) continue;
                    _ivals.Add(new Vector2(r.yMin, r.yMax));
                }
                else
                {
                    if (fixedC < r.yMin - 0.5f || fixedC > r.yMax + 0.5f) continue;
                    _ivals.Add(new Vector2(r.xMin, r.xMax));
                }
            }
            _ivals.Sort(ByMin);
            float reach = a;
            for (int i = 0; i < _ivals.Count; i++)
            {
                if (_ivals[i].x > reach + Bridge) break;
                if (_ivals[i].y > reach) reach = _ivals[i].y;
                if (reach >= b - 0.5f) return true;
            }
            return reach >= b - 0.5f;
        }

        private Vector2 NodePos(int idx)
        {
            return new Vector2(_vxs[idx / _gH], _hys[idx % _gH]);
        }

        private static int LowerBound(List<float> list, float v)
        {
            int lo = 0, hi = list.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (list[mid] < v) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        private static void AddConn(List<PortConn> list, int node, Vector2 j, float cost)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].node == node)
                {
                    if (cost < list[i].cost)
                    {
                        PortConn c; c.node = node; c.junction = j; c.cost = cost;
                        list[i] = c;
                    }
                    return;
                }
            PortConn nc; nc.node = node; nc.junction = j; nc.cost = cost;
            list.Add(nc);
        }

        private void ConnectPort(Vector2 p, Dir dir, List<PortConn> outList)
        {
            if (dir == Dir.Left || dir == Dir.Right)
            {
                int step = dir == Dir.Right ? 1 : -1;
                int start = LowerBound(_vxs, p.x);
                if (dir == Dir.Right) { while (start < _gV && _vxs[start] <= p.x + 0.5f) start++; }
                else { start--; while (start >= 0 && _vxs[start] >= p.x - 0.5f) start--; }

                int found = -1;
                for (int vi = start; vi >= 0 && vi < _gV; vi += step)
                    if (ChannelCoversAt(true, _vxs[vi], p.y)) { found = vi; break; }
                if (found < 0) found = NearestIndex(_vxs, p.x);

                if (found >= 0)
                {
                    float vx = _vxs[found];
                    Vector2 j = new Vector2(vx, p.y);
                    int up = LowerBound(_hys, p.y);
                    AddVConn(outList, found, vx, p, j, up);
                    AddVConn(outList, found, vx, p, j, up - 1);
                }
            }
            else
            {
                int step = dir == Dir.Down ? 1 : -1;
                int start = LowerBound(_hys, p.y);
                if (dir == Dir.Down) { while (start < _gH && _hys[start] <= p.y + 0.5f) start++; }
                else { start--; while (start >= 0 && _hys[start] >= p.y - 0.5f) start--; }

                int found = -1;
                for (int hi = start; hi >= 0 && hi < _gH; hi += step)
                    if (ChannelCoversAt(false, _hys[hi], p.x)) { found = hi; break; }
                if (found < 0) found = NearestIndex(_hys, p.y);

                if (found >= 0)
                {
                    float hy = _hys[found];
                    Vector2 j = new Vector2(p.x, hy);
                    int right = LowerBound(_vxs, p.x);
                    AddHConn(outList, found, hy, p, j, right);
                    AddHConn(outList, found, hy, p, j, right - 1);
                }
            }
        }

        private bool ChannelCoversAt(bool vertical, float lineCoord, float other)
        {
            float px = vertical ? lineCoord : other;
            float py = vertical ? other : lineCoord;
            for (int i = 0; i < _chan.Count; i++)
            {
                Rect r = _chan[i];
                if (px >= r.xMin - 0.5f && px <= r.xMax + 0.5f &&
                    py >= r.yMin - 0.5f && py <= r.yMax + 0.5f) return true;
            }
            return false;
        }

        private static int NearestIndex(List<float> list, float v)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                float d = Mathf.Abs(list[i] - v);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        private void AddVConn(List<PortConn> outList, int vi, float vx, Vector2 p, Vector2 j, int hi)
        {
            if (hi < 0 || hi >= _gH) return;
            float cost = Mathf.Abs(p.x - vx) + Mathf.Abs(p.y - _hys[hi]);
            AddConn(outList, vi * _gH + hi, j, cost);
        }

        private void AddHConn(List<PortConn> outList, int hi, float hy, Vector2 p, Vector2 j, int vi)
        {
            if (vi < 0 || vi >= _gV) return;
            float cost = Mathf.Abs(p.y - hy) + Mathf.Abs(p.x - _vxs[vi]);
            AddConn(outList, vi * _gH + hi, j, cost);
        }

        private static Vector2 JunctionFor(List<PortConn> conns, int node)
        {
            for (int i = 0; i < conns.Count; i++)
                if (conns[i].node == node) return conns[i].junction;
            return Vector2.zero;
        }

        private bool RouteThroughChannels(Vector2 po, Vector2 pi, Dir dOut, Dir dIn, List<Vector2> outPts)
        {
            int N = _gV * _gH;
            if (N == 0) return false;

            _srcConn.Clear();
            _dstConn.Clear();
            ConnectPort(po, dOut, _srcConn);
            ConnectPort(pi, dIn, _dstConn);
            if (_srcConn.Count == 0 || _dstConn.Count == 0) return false;

            int S = N, T = N + 1, total = N + 2;
            if (_g == null || _g.Length < total)
            {
                _g = new float[total];
                _from = new int[total];
                _closed = new bool[total];
            }
            for (int i = 0; i < total; i++) { _g[i] = float.MaxValue; _from[i] = -1; _closed[i] = false; }
            _routeDst = pi;

            _g[S] = 0f;
            _open.Clear();
            _open.Add(S);

            while (_open.Count > 0)
            {
                int bi = 0;
                float bf = float.MaxValue;
                for (int i = 0; i < _open.Count; i++)
                {
                    int nd = _open[i];
                    float h = (nd == S || nd == T) ? 0f
                        : Mathf.Abs(_vxs[nd / _gH] - _routeDst.x) + Mathf.Abs(_hys[nd % _gH] - _routeDst.y);
                    float f = _g[nd] + h;
                    if (f < bf) { bf = f; bi = i; }
                }
                int cur = _open[bi];
                _open[bi] = _open[_open.Count - 1];
                _open.RemoveAt(_open.Count - 1);
                if (_closed[cur]) continue;
                _closed[cur] = true;
                if (cur == T) break;

                if (cur == S)
                {
                    for (int i = 0; i < _srcConn.Count; i++)
                        Relax(S, _srcConn[i].node, _srcConn[i].cost);
                }
                else
                {
                    List<int> nb = _gridAdj[cur];
                    if (nb != null)
                    {
                        Vector2 cp = NodePos(cur);
                        for (int i = 0; i < nb.Count; i++)
                            Relax(cur, nb[i], (NodePos(nb[i]) - cp).magnitude);
                    }
                    for (int i = 0; i < _dstConn.Count; i++)
                        if (_dstConn[i].node == cur)
                            Relax(cur, T, _dstConn[i].cost);
                }
            }

            if (_g[T] == float.MaxValue) return false;

            _path.Clear();
            int n2 = T;
            while (n2 != -1) { _path.Add(n2); n2 = _from[n2]; }
            _path.Reverse();

            outPts.Add(po);
            for (int i = 1; i < _path.Count; i++)
            {
                int node = _path[i];
                if (node == T)
                {
                    outPts.Add(JunctionFor(_dstConn, _path[i - 1]));
                    outPts.Add(pi);
                }
                else if (_path[i - 1] == S)
                {
                    outPts.Add(JunctionFor(_srcConn, node));
                    outPts.Add(NodePos(node));
                }
                else
                {
                    outPts.Add(NodePos(node));
                }
            }
            return true;
        }

        private void Relax(int from, int to, float cost)
        {
            float ng = _g[from] + cost;
            if (ng < _g[to])
            {
                _g[to] = ng;
                _from[to] = from;
                if (!_closed[to]) _open.Add(to);
            }
        }

        private static void RegWidth(Dictionary<int, float> dict, float center, float w)
        {
            int q = Mathf.RoundToInt(center);
            float cur;
            if (!dict.TryGetValue(q, out cur) || w < cur) dict[q] = w;
        }

        private void BeginRoutes()
        {
            _routeUsed = 0;
            _allRoutes.Clear();
        }

        private RouteData NextRoute(JbChannelEdgeControl ctrl)
        {
            RouteData rd;
            if (_routeUsed < _routePool.Count) rd = _routePool[_routeUsed];
            else { rd = new RouteData(); _routePool.Add(rd); }
            _routeUsed++;
            rd.Ctrl = ctrl;
            rd.Pts.Clear();
            rd.OffV.Clear();
            rd.OffH.Clear();
            _allRoutes.Add(rd);
            return rd;
        }

        private void SeparateLanes()
        {
            var vWires = new Dictionary<int, List<int>>();
            var hWires = new Dictionary<int, List<int>>();

            for (int r = 0; r < _allRoutes.Count; r++)
            {
                var pts = _allRoutes[r].Pts;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1];
                    if (Mathf.Abs(a.x - b.x) < 0.5f)
                    {
                        int q = Mathf.RoundToInt(a.x);
                        if (_vWidth.ContainsKey(q)) Register(vWires, q, r);
                    }
                    else if (Mathf.Abs(a.y - b.y) < 0.5f)
                    {
                        int q = Mathf.RoundToInt(a.y);
                        if (_hWidth.ContainsKey(q)) Register(hWires, q, r);
                    }
                }
            }

            var ev = vWires.GetEnumerator();
            while (ev.MoveNext()) AssignLane(ev.Current.Key, ev.Current.Value, _vWidth[ev.Current.Key], true);
            var eh = hWires.GetEnumerator();
            while (eh.MoveNext()) AssignLane(eh.Current.Key, eh.Current.Value, _hWidth[eh.Current.Key], false);

            for (int r = 0; r < _allRoutes.Count; r++) ApplyOffsets(_allRoutes[r]);
        }

        private static void Register(Dictionary<int, List<int>> dict, int q, int r)
        {
            List<int> list;
            if (!dict.TryGetValue(q, out list)) { list = new List<int>(); dict[q] = list; }
            if (!list.Contains(r)) list.Add(r);
        }

        private struct LaneItem { public int Route; public float Lo; public float Hi; public float Pref; public float Off; }
        private readonly List<LaneItem> _laneItems = new List<LaneItem>();
        private readonly List<float> _laneEnd = new List<float>();

        private void AssignLane(int q, List<int> wires, float width, bool vertical)
        {
            int n = wires.Count;
            if (n == 0) return;

            _laneItems.Clear();
            for (int i = 0; i < n; i++)
            {
                Vector2 iv = LineSpan(wires[i], q, vertical);
                LaneItem li;
                li.Route = wires[i];
                li.Lo = iv.x;
                li.Hi = iv.y;
                li.Pref = PreferredOffset(wires[i], q, vertical);
                li.Off = 0f;
                _laneItems.Add(li);
            }
            _laneItems.Sort(CompareLaneItem);

            _laneEnd.Clear();
            for (int i = 0; i < _laneItems.Count; i++)
            {
                int lane = -1;
                for (int k = 0; k < _laneEnd.Count; k++)
                    if (_laneEnd[k] <= _laneItems[i].Lo + 0.5f) { lane = k; break; }
                if (lane < 0) { lane = _laneEnd.Count; _laneEnd.Add(0f); }
                _laneEnd[lane] = _laneItems[i].Hi;
            }
            int lanes = _laneEnd.Count;
            if (lanes < 1) lanes = 1;

            float span = width - _theme.EdgeWidth;
            if (span < 0f) span = 0f;
            float pitch = lanes <= 1 ? 0f : Mathf.Min(_theme.EdgeLanePitch, span / (lanes - 1));
            // vertical: right-align the lane block so its rightmost lane sits at +span/2 (right usable edge)
            float neutral = vertical ? span * 0.5f - (lanes - 1) * 0.5f * pitch : 0f;

            _laneEnd.Clear();
            for (int k = 0; k < lanes; k++) _laneEnd.Add(float.MinValue);
            for (int i = 0; i < _laneItems.Count; i++)
            {
                LaneItem li = _laneItems[i];
                int best = -1;
                float bestDelta = float.MaxValue;
                for (int k = 0; k < lanes; k++)
                {
                    if (_laneEnd[k] > li.Lo + 0.5f) continue;
                    float off = (k - (lanes - 1) * 0.5f) * pitch;
                    float delta = Mathf.Abs(off - li.Pref);
                    if (delta < bestDelta) { bestDelta = delta; best = k; }
                }
                if (best < 0) best = 0;
                _laneEnd[best] = li.Hi;
                li.Off = (best - (lanes - 1) * 0.5f) * pitch + neutral;
                _laneItems[i] = li;
            }

            float straightEps = _theme.EdgeLanePitch;
            float clearance = _theme.EdgeWidth;
            for (int i = 0; i < _laneItems.Count; i++)
            {
                LaneItem li = _laneItems[i];
                if (Mathf.Abs(li.Pref) >= straightEps) continue;
                if (Mathf.Abs(li.Off - neutral) < 0.01f) continue;
                bool clear = true;
                for (int j = 0; j < _laneItems.Count; j++)
                {
                    if (j == i) continue;
                    LaneItem lj = _laneItems[j];
                    if (li.Lo <= lj.Hi + 0.5f && lj.Lo <= li.Hi + 0.5f &&
                        Mathf.Abs(lj.Off - neutral) < clearance) { clear = false; break; }
                }
                if (clear) { li.Off = neutral; _laneItems[i] = li; }
            }

            for (int i = 0; i < _laneItems.Count; i++)
            {
                LaneItem li = _laneItems[i];
                if (vertical) _allRoutes[li.Route].OffV[q] = li.Off;
                else _allRoutes[li.Route].OffH[q] = li.Off;
            }
        }

        private static int CompareLaneItem(LaneItem a, LaneItem b)
        {
            int c = a.Lo.CompareTo(b.Lo);
            return c != 0 ? c : a.Route.CompareTo(b.Route);
        }

        private Vector2 LineSpan(int route, int q, bool vertical)
        {
            var pts = _allRoutes[route].Pts;
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 p = pts[i];
                bool on = vertical ? Mathf.RoundToInt(p.x) == q : Mathf.RoundToInt(p.y) == q;
                if (!on) continue;
                float v = vertical ? p.y : p.x;
                if (v < lo) lo = v;
                if (v > hi) hi = v;
            }
            if (lo > hi) { lo = 0f; hi = 0f; }
            return new Vector2(lo, hi);
        }

        private float PreferredOffset(int route, int q, bool vertical)
        {
            var pts = _allRoutes[route].Pts;
            int s = -1, e = -1;
            for (int i = 0; i < pts.Count; i++)
            {
                int c = vertical ? Mathf.RoundToInt(pts[i].x) : Mathf.RoundToInt(pts[i].y);
                if (c == q) { if (s < 0) s = i; e = i; }
            }
            if (s < 0) return 0f;
            float left = vertical
                ? (s > 0 ? pts[s - 1].x : pts[s].x)
                : (s > 0 ? pts[s - 1].y : pts[s].y);
            float right = vertical
                ? (e < pts.Count - 1 ? pts[e + 1].x : pts[e].x)
                : (e < pts.Count - 1 ? pts[e + 1].y : pts[e].y);
            return (left + right) * 0.5f - q;
        }

        private static void ApplyOffsets(RouteData rd)
        {
            var pts = rd.Pts;
            float ox, oy;
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector2 p = pts[i];
                if (rd.OffV.TryGetValue(Mathf.RoundToInt(p.x), out ox)) p.x += ox;
                if (rd.OffH.TryGetValue(Mathf.RoundToInt(p.y), out oy)) p.y += oy;
                pts[i] = p;
            }
        }
    }
}
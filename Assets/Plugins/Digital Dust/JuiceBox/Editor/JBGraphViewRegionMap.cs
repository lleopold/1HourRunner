using UnityEngine;
using UnityEngine.UIElements;

// ==============================================================================
//  SequenceGraphView.RegionMap: Builds the static region map of walls and channels from strip geometry.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView
    {
        private JbRegionMap _regionMap;
        private JbRegionOverlay _regionOverlay;

        private void BuildRegionMap()
        {
            if (_regionMap == null) _regionMap = new JbRegionMap();
            _regionMap.Regions.Clear();

            if (_strips != null && _strips.Count > 0)
                for (int i = 0; i < _strips.Count; i++)
                    BuildStripRegions(_strips[i], i);

            if (_regionOverlay == null)
            {
                _regionOverlay = new JbRegionOverlay(_regionMap);
                contentViewContainer.Add(_regionOverlay);
                RegisterCallback<KeyDownEvent>(OnRegionDebugKey);
            }
            else if (_regionOverlay.parent == null)
            {
                contentViewContainer.Add(_regionOverlay);
            }
            _regionOverlay.BringToFront();
            _regionOverlay.Refresh();
        }

        internal void RefreshRegionOverlay() => _regionOverlay?.Refresh();

        private void OnRegionDebugKey(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.F10) return;
            JbRegionMap.ShowDebug = !JbRegionMap.ShowDebug;
            _regionOverlay?.Refresh();
            e.StopPropagation();
        }

        private void BuildStripRegions(FilmStripElement strip, int index)
        {
            Rect pos = strip.GetPosition();
            float x = pos.x;
            float y = pos.y;
            float right = x + pos.width;
            int n = strip.SlotCount;

            float total = FilmStripElement.StripTotalHeight;
            float area = FilmStripElement.NodeAreaH;
            float perf = FilmStripElement.PerfH;
            float inset = FilmStripElement.PerfEdgeInset;
            float pad = FilmStripElement.PadY;
            float cap = FilmStripElement.LeftCapW;

            float dockTop0 = y + inset, dockTop1 = y + inset + perf;
            float corrTop0 = dockTop1, corrTop1 = y + pad;
            float eff0 = corrTop1, eff1 = y + pad + area;
            float corrBot0 = eff1, corrBot1 = y + total - perf - inset;
            float dockBot0 = corrBot1, dockBot1 = y + total - inset;

            float slot0 = strip.GetSlotCanvasX(0);
            float cellsRight = (n > 0)
                ? strip.GetSlotCanvasX(n - 1) + FilmStripElement.NodeSize
                : slot0;
            float chanL = x + cap;
            float rightChanR = right;

            AddChannel(chanL, corrTop0, rightChanR, corrTop1);
            AddChannel(chanL, corrBot0, rightChanR, corrBot1);
            if (index < _strips.Count - 1)
                AddChannel(0f, y + total, rightChanR, y + total + SequenceGap);

            AddWall(x, dockTop0, x + cap, dockBot1);
            AddChannel(0f, dockTop0, x, dockBot1);

            AddChannel(x + cap, eff0, slot0, eff1);
            for (int j = 0; j < n; j++)
            {
                float cl = strip.GetSlotCanvasX(j);
                float cr = cl + FilmStripElement.NodeSize;
                AddWall(cl, eff0, cr, eff1);
                if (j < n - 1)
                    AddChannel(cr, eff0, strip.GetSlotCanvasX(j + 1), eff1);
            }
            if (n > 0)
                AddChannel(cellsRight, eff0, rightChanR, eff1);

            BuildDockRow(strip, dockTop0, dockTop1);
            BuildDockRow(strip, dockBot0, dockBot1);
        }

        private void BuildDockRow(FilmStripElement strip, float y0, float y1)
        {
            int pc = strip.GetPocketCount();
            float half = FilmStripElement.TopPerfW * 0.5f;
            float capRight = strip.GetPosition().x + FilmStripElement.LeftCapW;
            if (pc > 0)
            {
                float firstLeft = strip.GetPocketCentreX(0) - half;
                if (firstLeft > capRight + 0.5f) AddChannel(capRight, y0, firstLeft, y1);
            }
            float prevRight = float.NaN;
            for (int j = 0; j < pc; j++)
            {
                float cx = strip.GetPocketCentreX(j);
                float l = cx - half;
                float r = cx + half;
                AddWall(l, y0, r, y1);
                if (!float.IsNaN(prevRight) && l > prevRight + 0.5f)
                    AddChannel(prevRight, y0, l, y1);
                prevRight = r;
            }
            if (!float.IsNaN(prevRight))
            {
                float rightEdge = strip.GetPosition().x + strip.GetPosition().width;
                if (rightEdge > prevRight + 0.5f) AddChannel(prevRight, y0, rightEdge, y1);
            }
        }

        private void AddChannel(float xMin, float yMin, float xMax, float yMax)
        {
            JbRegion r;
            r.Rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            r.Channel = true;
            r.Top = r.Right = r.Bottom = r.Left = JbEdgeDir.InOut;
            _regionMap.Regions.Add(r);
        }

        private void AddWall(float xMin, float yMin, float xMax, float yMax)
        {
            JbRegion r;
            r.Rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            r.Channel = false;
            r.Top = r.Right = r.Bottom = r.Left = JbEdgeDir.None;
            _regionMap.Regions.Add(r);
        }
    }
}
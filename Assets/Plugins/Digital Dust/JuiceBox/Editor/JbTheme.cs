using UnityEngine;
using UnityEngine.UIElements;

// ==============================================================================
//  JbTheme: All editor graph colours in one place, with USS custom property overrides.
// ==============================================================================
namespace JuiceBox
{
    internal struct JbTheme
    {
        public Color StripBg;
        public Color CapBg;
        public Color CapBorder;
        public Color CapLabel;
        public Color CapName;
        public Color CapValue;
        public Color HRule;
        public Color SlotIdle;
        public Color SlotHi;
        public Color SlotBgHi;
        public Color SlotDenied;
        public Color SlotBgDenied;
        public Color SlotNum;
        public Color Arrow;
        public Color EndArrow;
        public Color RunBg;
        public Color RunBorder;
        public Color PerfBg;
        public Color PerfBorder;
        public Color PocketBg;
        public Color PocketBorder;
        public Color SpaceBg;
        public Color SpaceBorder;
        public Color NodeBg;
        public Color NodeBorder;
        public Color NodeBorderDrag;
        public Color NodeBorderFloat;
        public Color NodeBorderSel;
        public Color NodeBorderFloatSel;
        public Color NodeHeader;
        public Color BadgeTweenBg;
        public Color BadgeTweenText;
        public Color BadgeAdvBg;
        public Color BadgeAdvText;
        public Color BadgeShakeBg;
        public Color BadgeShakeText;
        public Color BadgeTypeBg;
        public Color BadgeTypeText;
        public Color FieldBg;
        public Color FieldBgMiss;
        public Color FieldLbl;
        public Color FieldVal;
        public Color FieldMissVal;
        public Color PortAction;
        public Color PortLabel;
        public Color PortDivider;
        public Color Arc;
        public Color SubnodeBg;
        public Color SubnodeBorder;
        public Color HookText;
        public Color HookRetText;
        public Color HookRetBg;
        public Color MsgBarBg;
        public Color MsgBarBorder;
        public Color MsgBarText;
        public Color MsgWarnBg;
        public Color MsgWarnBorder;
        public Color MsgWarnText;

        public float EdgeWidth;
        public float EdgeCornerRadius;
        public float EdgeLanePitch;

        public static readonly JbTheme Default = new JbTheme
        {
            StripBg = new Color(0.136f, 0.136f, 0.136f),
            CapBg = new Color(0.108f, 0.155f, 0.200f),
            CapBorder = new Color(0.200f, 0.300f, 0.400f),
            CapLabel = new Color(0.345f, 0.500f, 0.610f),
            CapName = new Color(0.775f, 0.875f, 0.960f),
            CapValue = new Color(0.450f, 0.710f, 0.900f),
            HRule = new Color(0.200f, 0.310f, 0.410f),
            SlotIdle = new Color(0.240f, 0.240f, 0.240f),
            SlotHi = new Color(0.350f, 0.610f, 0.920f),
            SlotBgHi = new Color(0.080f, 0.175f, 0.280f),
            SlotDenied = new Color(0.500f, 0.150f, 0.150f),
            SlotBgDenied = new Color(0.200f, 0.060f, 0.060f),
            SlotNum = new Color(0.280f, 0.280f, 0.280f),
            Arrow = new Color(0.280f, 0.280f, 0.280f),
            EndArrow = new Color(0.300f, 0.300f, 0.300f),
            RunBg = new Color(0.100f, 0.245f, 0.700f, 0.14f),
            RunBorder = new Color(0.240f, 0.540f, 0.900f, 0.55f),
            PerfBg = new Color(0.090f, 0.090f, 0.090f),
            PerfBorder = new Color(0.200f, 0.200f, 0.200f),
            PocketBg = new Color(0.075f, 0.115f, 0.155f),
            PocketBorder = new Color(0.165f, 0.305f, 0.445f),
            SpaceBg = new Color(0.180f, 0.130f, 0.040f),
            SpaceBorder = new Color(0.500f, 0.360f, 0.080f),
            NodeBg = new Color(0.108f, 0.165f, 0.224f),
            NodeBorder = new Color(0.230f, 0.355f, 0.484f),
            NodeBorderDrag = new Color(0.340f, 0.520f, 0.700f),
            NodeBorderFloat = new Color(0.400f, 0.085f, 0.085f),
            NodeBorderSel = new Color(0.660f, 0.590f, 0.130f),
            NodeBorderFloatSel = new Color(0.700f, 0.200f, 0.200f),
            NodeHeader = new Color(0.082f, 0.130f, 0.180f),
            BadgeTweenBg = new Color(0.082f, 0.140f, 0.082f),
            BadgeTweenText = new Color(0.360f, 0.620f, 0.360f),
            BadgeAdvBg = new Color(0.140f, 0.082f, 0.082f),
            BadgeAdvText = new Color(0.720f, 0.390f, 0.390f),
            BadgeShakeBg = new Color(0.140f, 0.110f, 0.040f),
            BadgeShakeText = new Color(0.820f, 0.640f, 0.230f),
            BadgeTypeBg = new Color(0.100f, 0.100f, 0.168f),
            BadgeTypeText = new Color(0.390f, 0.390f, 0.700f),
            FieldBg = new Color(0.068f, 0.108f, 0.152f),
            FieldBgMiss = new Color(0.130f, 0.052f, 0.052f),
            FieldLbl = new Color(0.210f, 0.320f, 0.395f),
            FieldVal = new Color(0.385f, 0.600f, 0.740f),
            FieldMissVal = new Color(0.530f, 0.255f, 0.255f),
            PortAction = new Color(0.30f, 0.60f, 0.90f),
            PortLabel = new Color(0.40f, 0.40f, 0.40f),
            PortDivider = new Color(0.120f, 0.200f, 0.280f),
            Arc = new Color(0.230f, 0.370f, 0.460f),
            SubnodeBg = new Color(0.080f, 0.120f, 0.180f),
            SubnodeBorder = new Color(0.200f, 0.380f, 0.600f),
            HookText = new Color(0.620f, 0.780f, 0.920f),
            HookRetText = new Color(0.70f, 0.50f, 0.20f),
            HookRetBg = new Color(0.25f, 0.15f, 0.05f),
            MsgBarBg = new Color(0.13f, 0.13f, 0.13f),
            MsgBarBorder = new Color(0.20f, 0.20f, 0.20f),
            MsgBarText = new Color(0.48f, 0.48f, 0.48f),
            MsgWarnBg = new Color(0.17f, 0.14f, 0.06f),
            MsgWarnBorder = new Color(0.40f, 0.32f, 0.06f),
            MsgWarnText = new Color(0.88f, 0.72f, 0.22f),

            EdgeWidth = 3f,
            EdgeCornerRadius = 6f,
            EdgeLanePitch = 6f,
        };

        public static JbTheme ReadFrom(ICustomStyle s)
        {
            var t = Default;
            R(s, "--jb-strip-bg", ref t.StripBg);
            R(s, "--jb-cap-bg", ref t.CapBg);
            R(s, "--jb-cap-border", ref t.CapBorder);
            R(s, "--jb-cap-label", ref t.CapLabel);
            R(s, "--jb-cap-name", ref t.CapName);
            R(s, "--jb-cap-value", ref t.CapValue);
            R(s, "--jb-hrule", ref t.HRule);
            R(s, "--jb-slot-idle", ref t.SlotIdle);
            R(s, "--jb-slot-hi", ref t.SlotHi);
            R(s, "--jb-slot-bg-hi", ref t.SlotBgHi);
            R(s, "--jb-slot-denied", ref t.SlotDenied);
            R(s, "--jb-slot-bg-denied", ref t.SlotBgDenied);
            R(s, "--jb-slot-num", ref t.SlotNum);
            R(s, "--jb-arrow", ref t.Arrow);
            R(s, "--jb-end-arrow", ref t.EndArrow);
            R(s, "--jb-run-bg", ref t.RunBg);
            R(s, "--jb-run-border", ref t.RunBorder);
            R(s, "--jb-perf-bg", ref t.PerfBg);
            R(s, "--jb-perf-border", ref t.PerfBorder);
            R(s, "--jb-pocket-bg", ref t.PocketBg);
            R(s, "--jb-pocket-border", ref t.PocketBorder);
            R(s, "--jb-space-bg", ref t.SpaceBg);
            R(s, "--jb-space-border", ref t.SpaceBorder);
            R(s, "--jb-node-bg", ref t.NodeBg);
            R(s, "--jb-node-border", ref t.NodeBorder);
            R(s, "--jb-node-border-drag", ref t.NodeBorderDrag);
            R(s, "--jb-node-border-float", ref t.NodeBorderFloat);
            R(s, "--jb-node-border-sel", ref t.NodeBorderSel);
            R(s, "--jb-node-border-float-sel", ref t.NodeBorderFloatSel);
            R(s, "--jb-node-header", ref t.NodeHeader);
            R(s, "--jb-badge-tween-bg", ref t.BadgeTweenBg);
            R(s, "--jb-badge-tween-text", ref t.BadgeTweenText);
            R(s, "--jb-badge-adv-bg", ref t.BadgeAdvBg);
            R(s, "--jb-badge-adv-text", ref t.BadgeAdvText);
            R(s, "--jb-badge-shake-bg", ref t.BadgeShakeBg);
            R(s, "--jb-badge-shake-text", ref t.BadgeShakeText);
            R(s, "--jb-badge-type-bg", ref t.BadgeTypeBg);
            R(s, "--jb-badge-type-text", ref t.BadgeTypeText);
            R(s, "--jb-field-bg", ref t.FieldBg);
            R(s, "--jb-field-bg-miss", ref t.FieldBgMiss);
            R(s, "--jb-field-lbl", ref t.FieldLbl);
            R(s, "--jb-field-val", ref t.FieldVal);
            R(s, "--jb-field-miss-val", ref t.FieldMissVal);
            R(s, "--jb-port-action", ref t.PortAction);
            R(s, "--jb-port-label", ref t.PortLabel);
            R(s, "--jb-port-divider", ref t.PortDivider);
            R(s, "--jb-arc", ref t.Arc);
            R(s, "--jb-subnode-bg", ref t.SubnodeBg);
            R(s, "--jb-subnode-border", ref t.SubnodeBorder);
            R(s, "--jb-hook-text", ref t.HookText);
            R(s, "--jb-hook-ret-text", ref t.HookRetText);
            R(s, "--jb-hook-ret-bg", ref t.HookRetBg);
            R(s, "--jb-msg-bar-bg", ref t.MsgBarBg);
            R(s, "--jb-msg-bar-border", ref t.MsgBarBorder);
            R(s, "--jb-msg-bar-text", ref t.MsgBarText);
            R(s, "--jb-msg-warn-bg", ref t.MsgWarnBg);
            R(s, "--jb-msg-warn-border", ref t.MsgWarnBorder);
            R(s, "--jb-msg-warn-text", ref t.MsgWarnText);
            RF(s, "--jb-edge-width", ref t.EdgeWidth);
            RF(s, "--jb-edge-radius", ref t.EdgeCornerRadius);
            RF(s, "--jb-edge-lane-pitch", ref t.EdgeLanePitch);
            return t;
        }

        private static void R(ICustomStyle s, string name, ref Color c)
        {
            if (s.TryGetValue(new CustomStyleProperty<Color>(name), out var v)) c = v;
        }

        private static void RF(ICustomStyle s, string name, ref float f)
        {
            if (s.TryGetValue(new CustomStyleProperty<float>(name), out var v)) f = v;
        }
    }
}
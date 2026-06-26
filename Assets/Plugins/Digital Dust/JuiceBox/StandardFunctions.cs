using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using UnityEngine.UI;

// ==============================================================================
//  StandardFunctions: ready-made getter and setter helpers for common targets (transform,
//  RectTransform, material and text color) plus the Easing function library, for wiring
//  into effect delegate slots from the graph editor.
// ==============================================================================
namespace JuiceBox
{
    /// <summary>Ready-made getter and setter helpers for common targets (transform position and scale,
    /// RectTransform, material and text color), plus the Easing function library. Wire these
    /// into an effect's GetTargetValue, OnUpdate, and Easing slots from the graph editor.</summary>
    public static class StandardFunctions
    {
        private const float PI = 3.14159265358979f;

        // -- Vector2 -> Vector3 position mapping ------------------------------

        /// <summary>Sets the target's world space position on the X and Y axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetPositionXY(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.position = new Vector3(value.x, value.y, t.position.z);
        }

        /// <summary>Sets the target's world space position on the X and Z axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetPositionXZ(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.position = new Vector3(value.x, t.position.y, value.y);
        }

        /// <summary>Sets the target's world space position on the Y and Z axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetPositionYZ(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.position = new Vector3(t.position.x, value.x, value.y);
        }

        /// <summary>Sets the target's local space position on the X and Y axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetLocalPositionXY(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.localPosition = new Vector3(value.x, value.y, t.localPosition.z);
        }

        /// <summary>Sets the target's local space position on the X and Z axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetLocalPositionXZ(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.localPosition = new Vector3(value.x, t.localPosition.y, value.y);
        }

        /// <summary>Sets the target's local space position on the Y and Z axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetLocalPositionYZ(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.localPosition = new Vector3(t.localPosition.x, value.x, value.y);
        }

        /// <summary>Returns the target's world space position on the X and Y axes as a Vector2.</summary>
        public static Vector2 GetPositionXY(GameObject target)
        {
            return new Vector2(target.transform.position.x, target.transform.position.y);
        }

        /// <summary>Returns the target's world space position on the X and Z axes as a Vector2.</summary>
        public static Vector2 GetPositionXZ(GameObject target)
        {
            return new Vector2(target.transform.position.x, target.transform.position.z);
        }

        /// <summary>Returns the target's world space position on the Y and Z axes as a Vector2.</summary>
        public static Vector2 GetPositionYZ(GameObject target)
        {
            return new Vector2(target.transform.position.y, target.transform.position.z);
        }

        /// <summary>Returns the target's local space position on the X and Y axes as a Vector2.</summary>
        public static Vector2 GetLocalPositionXY(GameObject target)
        {
            return new Vector2(target.transform.localPosition.x, target.transform.localPosition.y);
        }

        /// <summary>Returns the target's local space position on the X and Z axes as a Vector2.</summary>
        public static Vector2 GetLocalPositionXZ(GameObject target)
        {
            return new Vector2(target.transform.localPosition.x, target.transform.localPosition.z);
        }

        /// <summary>Returns the target's local space position on the Y and Z axes as a Vector2.</summary>
        public static Vector2 GetLocalPositionYZ(GameObject target)
        {
            return new Vector2(target.transform.localPosition.y, target.transform.localPosition.z);
        }

        /// <summary>Sets the target's world space position on the X axis, leaving the other axes unchanged.</summary>
        public static void SetPositionX(GameObject target, float value)
        {
            Transform t = target.transform;
            t.position = new Vector3(value, t.position.y, t.position.z);
        }

        /// <summary>Sets the target's world space position on the Y axis, leaving the other axes unchanged.</summary>
        public static void SetPositionY(GameObject target, float value)
        {
            Transform t = target.transform;
            t.position = new Vector3(t.position.x, value, t.position.z);
        }

        /// <summary>Sets the target's world space position on the Z axis, leaving the other axes unchanged.</summary>
        public static void SetPositionZ(GameObject target, float value)
        {
            Transform t = target.transform;
            t.position = new Vector3(t.position.x, t.position.y, value);
        }

        /// <summary>Sets the target's local space position on the X axis, leaving the other axes unchanged.</summary>
        public static void SetLocalPositionX(GameObject target, float value)
        {
            Transform t = target.transform;
            t.localPosition = new Vector3(value, t.localPosition.y, t.localPosition.z);
        }

        /// <summary>Sets the target's local space position on the Y axis, leaving the other axes unchanged.</summary>
        public static void SetLocalPositionY(GameObject target, float value)
        {
            Transform t = target.transform;
            t.localPosition = new Vector3(t.localPosition.x, value, t.localPosition.z);
        }

        /// <summary>Sets the target's local space position on the Z axis, leaving the other axes unchanged.</summary>
        public static void SetLocalPositionZ(GameObject target, float value)
        {
            Transform t = target.transform;
            t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, value);
        }

        /// <summary>Returns the target's world space position on the X axis.</summary>
        public static float GetPositionX(GameObject target)
        {
            return target.transform.position.x;
        }

        /// <summary>Returns the target's world space position on the Y axis.</summary>
        public static float GetPositionY(GameObject target)
        {
            return target.transform.position.y;
        }

        /// <summary>Returns the target's world space position on the Z axis.</summary>
        public static float GetPositionZ(GameObject target)
        {
            return target.transform.position.z;
        }

        /// <summary>Returns the target's local space position on the X axis.</summary>
        public static float GetLocalPositionX(GameObject target)
        {
            return target.transform.localPosition.x;
        }

        /// <summary>Returns the target's local space position on the Y axis.</summary>
        public static float GetLocalPositionY(GameObject target)
        {
            return target.transform.localPosition.y;
        }

        /// <summary>Returns the target's local space position on the Z axis.</summary>
        public static float GetLocalPositionZ(GameObject target)
        {
            return target.transform.localPosition.z;
        }

        // -- Vector2 -> Vector3 scale mapping --------------------------------

        /// <summary>Sets the target's local scale on the X and Y axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetLocalScaleXY(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.localScale = new Vector3(value.x, value.y, t.localScale.z);
        }

        /// <summary>Sets the target's local scale on the X and Z axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetLocalScaleXZ(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.localScale = new Vector3(value.x, t.localScale.y, value.y);
        }

        /// <summary>Sets the target's local scale on the Y and Z axes from value.x and value.y, leaving the third axis unchanged.</summary>
        public static void SetLocalScaleYZ(GameObject target, Vector2 value)
        {
            Transform t = target.transform;
            t.localScale = new Vector3(t.localScale.x, value.x, value.y);
        }

        /// <summary>Returns the target's local scale on the X and Y axes as a Vector2.</summary>
        public static Vector2 GetLocalScaleXY(GameObject target)
        {
            return new Vector2(target.transform.localScale.x, target.transform.localScale.y);
        }

        /// <summary>Returns the target's local scale on the X and Z axes as a Vector2.</summary>
        public static Vector2 GetLocalScaleXZ(GameObject target)
        {
            return new Vector2(target.transform.localScale.x, target.transform.localScale.z);
        }

        /// <summary>Returns the target's local scale on the Y and Z axes as a Vector2.</summary>
        public static Vector2 GetLocalScaleYZ(GameObject target)
        {
            return new Vector2(target.transform.localScale.y, target.transform.localScale.z);
        }

        /// <summary>Sets the target's local scale on the X axis, leaving the other axes unchanged.</summary>
        public static void SetLocalScaleX(GameObject target, float value)
        {
            Transform t = target.transform;
            t.localScale = new Vector3(value, t.localScale.y, t.localScale.z);
        }

        /// <summary>Sets the target's local scale on the Y axis, leaving the other axes unchanged.</summary>
        public static void SetLocalScaleY(GameObject target, float value)
        {
            Transform t = target.transform;
            t.localScale = new Vector3(t.localScale.x, value, t.localScale.y);
        }

        /// <summary>Sets the target's local scale on the Z axis, leaving the other axes unchanged.</summary>
        public static void SetLocalScaleZ(GameObject target, float value)
        {
            Transform t = target.transform;
            t.localScale = new Vector3(t.localScale.x, t.localScale.y, value);
        }

        /// <summary>Returns the target's local scale on the X axis.</summary>
        public static float GetLocalScaleX(GameObject target)
        {
            return target.transform.localScale.x;
        }

        /// <summary>Returns the target's local scale on the Y axis.</summary>
        public static float GetLocalScaleY(GameObject target)
        {
            return target.transform.localScale.y;
        }

        /// <summary>Returns the target's local scale on the Z axis.</summary>
        public static float GetLocalScaleZ(GameObject target)
        {
            return target.transform.localScale.z;
        }

        // -- Physics force (Vector3) -------------------------------------

        /// <summary>Adds a force to the target's Rigidbody. Drive this from an effect on the FixedUpdate segment.</summary>
        public static void AddForce(GameObject target, Vector3 value)
        {
            target.GetComponent<Rigidbody>().AddForce(value);
        }

        /// <summary>Adds a torque to the target's Rigidbody. Drive this from an effect on the FixedUpdate segment.</summary>
        public static void AddTorque(GameObject target, Vector3 value)
        {
            target.GetComponent<Rigidbody>().AddTorque(value);
        }

        // -- RectTransform edge accessors (float) ----------------------------

        /// <summary>Returns the left edge of the target's RectTransform, measured from its anchored position and size.</summary>
        public static float GetRectTransformLeft(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.anchoredPosition.x - rt.sizeDelta.x * rt.pivot.x;
        }

        /// <summary>Returns the right edge of the target's RectTransform, measured from its anchored position and size.</summary>
        public static float GetRectTransformRight(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.anchoredPosition.x + rt.sizeDelta.x * (1f - rt.pivot.x);
        }

        /// <summary>Returns the bottom edge of the target's RectTransform, measured from its anchored position and size.</summary>
        public static float GetRectTransformBottom(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.anchoredPosition.y - rt.sizeDelta.y * rt.pivot.y;
        }

        /// <summary>Returns the top edge of the target's RectTransform, measured from its anchored position and size.</summary>
        public static float GetRectTransformTop(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.anchoredPosition.y + rt.sizeDelta.y * (1f - rt.pivot.y);
        }

        /// <summary>Moves the left edge of the target's RectTransform to value, keeping the opposite edge fixed.</summary>
        public static void SetRectTransformLeft(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            float right = rt.anchoredPosition.x + rt.sizeDelta.x * (1f - rt.pivot.x);
            float newWidth = right - value;
            float newX = value + newWidth * rt.pivot.x;
            rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(newX, rt.anchoredPosition.y);
        }

        /// <summary>Moves the right edge of the target's RectTransform to value, keeping the opposite edge fixed.</summary>
        public static void SetRectTransformRight(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            float left = rt.anchoredPosition.x - rt.sizeDelta.x * rt.pivot.x;
            float newWidth = value - left;
            float newX = left + newWidth * rt.pivot.x;
            rt.sizeDelta = new Vector2(newWidth, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(newX, rt.anchoredPosition.y);
        }

        /// <summary>Moves the bottom edge of the target's RectTransform to value, keeping the opposite edge fixed.</summary>
        public static void SetRectTransformBottom(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            float top = rt.anchoredPosition.y + rt.sizeDelta.y * (1f - rt.pivot.y);
            float newHeight = top - value;
            float newY = value + newHeight * rt.pivot.y;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, newHeight);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newY);
        }

        /// <summary>Moves the top edge of the target's RectTransform to value, keeping the opposite edge fixed.</summary>
        public static void SetRectTransformTop(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            float bottom = rt.anchoredPosition.y - rt.sizeDelta.y * rt.pivot.y;
            float newHeight = value - bottom;
            float newY = bottom + newHeight * rt.pivot.y;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, newHeight);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newY);
        }

        // -- RectTransform size accessors (float) ----------------------------

        /// <summary>Returns the width of the target's RectTransform.</summary>
        public static float GetRectTransformWidth(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.sizeDelta.x;
        }

        /// <summary>Returns the height of the target's RectTransform.</summary>
        public static float GetRectTransformHeight(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.sizeDelta.y;
        }

        /// <summary>Sets the width of the target's RectTransform.</summary>
        public static void SetRectTransformWidth(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(value, rt.sizeDelta.y);
        }

        /// <summary>Sets the height of the target's RectTransform.</summary>
        public static void SetRectTransformHeight(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, value);
        }

        // -- RectTransform Z rotation (float) --------------------------------

        /// <summary>Returns the target's RectTransform rotation around the Z axis in degrees.</summary>
        public static float GetRectTransformZRotation(GameObject target)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            return rt.localEulerAngles.z;
        }

        /// <summary>Sets the target's RectTransform rotation around the Z axis in degrees.</summary>
        public static void SetRectTransformZRotation(GameObject target, float value)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            Vector3 euler = rt.localEulerAngles;
            euler.z = value;
            rt.localEulerAngles = euler;
        }

        // -- Material color (Vector4 RGBA) ---------------------------------

        /// <summary>Sets the target renderer material color from an RGBA vector (x, y, z, w map to r, g, b, a).</summary>
        public static void SetMaterialColor(GameObject target, Vector4 value)
        {
            Renderer r = target.GetComponent<Renderer>();
            r.material.color = new Color(value.x, value.y, value.z, value.w);
        }

        /// <summary>Sets the target renderer material color from an RGB vector, leaving the existing alpha unchanged.</summary>
        public static void SetMaterialColor(GameObject target, Vector3 value)
        {
            Renderer r = target.GetComponent<Renderer>();
            r.material.color = new Color(value.x, value.y, value.z, r.material.color.a);
        }

        /// <summary>Returns the target renderer material color as an RGBA vector.</summary>
        public static Vector4 GetMaterialColor(GameObject target)
        {
            Color c = target.GetComponent<Renderer>().material.color;
            return new Vector4(c.r, c.g, c.b, c.a);
        }

        /// <summary>Returns the target renderer material color as an RGB vector, ignoring alpha.</summary>
        public static Vector3 GetMaterialColorRGB(GameObject target)
        {
            Color c = target.GetComponent<Renderer>().material.color;
            return new Vector3(c.r, c.g, c.b);
        }

        // -- Material alpha (float) ------------------------------------------

        /// <summary>Sets the alpha of the target renderer material color.</summary>
        public static void SetMaterialAlpha(GameObject target, float value)
        {
            Renderer r = target.GetComponent<Renderer>();
            Color c = r.material.color;
            c.a = value;
            r.material.color = c;
        }

        /// <summary>Returns the alpha of the target renderer material color.</summary>
        public static float GetMaterialAlpha(GameObject target)
        {
            return target.GetComponent<Renderer>().material.color.a;
        }

        // -- TextMeshPro color (Vector4 RGBA) ------------------

        /// <summary>Sets the target TextMeshPro color from an RGBA vector (x, y, z, w map to r, g, b, a).</summary>
        public static void SetTextColor(GameObject target, Vector4 value)
        {
            TMP_Text t = target.GetComponent<TMP_Text>();
            t.color = new Color(value.x, value.y, value.z, value.w);
        }

        /// <summary>Sets the target TextMeshPro color from an RGB vector, leaving the existing alpha unchanged.</summary>
        public static void SetTextColor(GameObject target, Vector3 value)
        {
            TMP_Text t = target.GetComponent<TMP_Text>();
            t.color = new Color(value.x, value.y, value.z, t.color.a);
        }

        /// <summary>Returns the target TextMeshPro color as an RGBA vector.</summary>
        public static Vector4 GetTextColor(GameObject target)
        {
            Color c = target.GetComponent<TMP_Text>().color;
            return new Vector4(c.r, c.g, c.b, c.a);
        }

        /// <summary>Returns the target TextMeshPro color as an RGB vector, ignoring alpha.</summary>
        public static Vector3 GetTextColorRGB(GameObject target)
        {
            Color c = target.GetComponent<TMP_Text>().color;
            return new Vector3(c.r, c.g, c.b);
        }

        // -- TextMeshPro alpha (float) --------------------

        /// <summary>Sets the alpha of the target TextMeshPro text.</summary>
        public static void SetTextAlpha(GameObject target, float value)
        {
            target.GetComponent<TMP_Text>().alpha = value;
        }

        /// <summary>Returns the alpha of the target TextMeshPro text.</summary>
        public static float GetTextAlpha(GameObject target)
        {
            return target.GetComponent<TMP_Text>().alpha;
        }

        // -- TextMeshPro font size (float) -----------------

        /// <summary>Sets the font size of the target TextMeshPro text.</summary>
        public static void SetFontSize(GameObject target, float value)
        {
            target.GetComponent<TMP_Text>().fontSize = value;
        }

        /// <summary>Returns the font size of the target TextMeshPro text.</summary>
        public static float GetFontSize(GameObject target)
        {
            return target.GetComponent<TMP_Text>().fontSize;
        }

        // -- UI Scrollbar (float) --------------------------------------------

        /// <summary>Sets the value of the target's Scrollbar (0 to 1).</summary>
        public static void SetScrollbarValue(GameObject target, float value)
        {
            target.GetComponent<Scrollbar>().value = value;
        }

        /// <summary>Returns the value of the target's Scrollbar (0 to 1).</summary>
        public static float GetScrollbarValue(GameObject target)
        {
            return target.GetComponent<Scrollbar>().value;
        }

        /// <summary>Sets the handle size of the target's Scrollbar (0 to 1).</summary>
        public static void SetScrollbarSize(GameObject target, float value)
        {
            target.GetComponent<Scrollbar>().size = value;
        }

        /// <summary>Returns the handle size of the target's Scrollbar (0 to 1).</summary>
        public static float GetScrollbarSize(GameObject target)
        {
            return target.GetComponent<Scrollbar>().size;
        }

        // -- UI Shadow / Outline color (Vector4 RGBA) ------------------------

        /// <summary>Sets the effect color of the target's Shadow (or Outline) from an RGBA vector (x, y, z, w map to r, g, b, a).</summary>
        public static void SetShadowColor(GameObject target, Vector4 value)
        {
            target.GetComponent<Shadow>().effectColor = new Color(value.x, value.y, value.z, value.w);
        }

        /// <summary>Sets the effect color of the target's Shadow (or Outline) from an RGB vector, leaving the existing alpha unchanged.</summary>
        public static void SetShadowColor(GameObject target, Vector3 value)
        {
            Shadow s = target.GetComponent<Shadow>();
            s.effectColor = new Color(value.x, value.y, value.z, s.effectColor.a);
        }

        /// <summary>Returns the effect color of the target's Shadow (or Outline) as an RGBA vector.</summary>
        public static Vector4 GetShadowColor(GameObject target)
        {
            Color c = target.GetComponent<Shadow>().effectColor;
            return new Vector4(c.r, c.g, c.b, c.a);
        }

        /// <summary>Returns the effect color of the target's Shadow (or Outline) as an RGB vector, ignoring alpha.</summary>
        public static Vector3 GetShadowColorRGB(GameObject target)
        {
            Color c = target.GetComponent<Shadow>().effectColor;
            return new Vector3(c.r, c.g, c.b);
        }

        // -- UI Shadow / Outline alpha (float) -------------------------------

        /// <summary>Sets the alpha of the target's Shadow (or Outline) effect color.</summary>
        public static void SetShadowAlpha(GameObject target, float value)
        {
            Shadow s = target.GetComponent<Shadow>();
            Color c = s.effectColor;
            c.a = value;
            s.effectColor = c;
        }

        /// <summary>Returns the alpha of the target's Shadow (or Outline) effect color.</summary>
        public static float GetShadowAlpha(GameObject target)
        {
            return target.GetComponent<Shadow>().effectColor.a;
        }

        // -- Easing ----------------------------------------------------------

        /// <summary>A library of easing functions mapping a normalized time in the 0 to 1 range to an eased value.
        /// Assign any of these to an effect's Easing slot to shape how it moves over time.</summary>
        public static class Easing
        {
            private const float HalfPi = PI / 2f;

            /// <summary>Holds at 0 for the first half, then jumps to 1. A single hard step.</summary>
            public static float Step1(float time) =>
                time < 0.5f ? 0f : 1f;

            /// <summary>Steps up in three flat stages across the duration.</summary>
            public static float Step2(float time) =>
                time < (1f / 3f) ? 0f : time < (2f / 3f) ? 1f / 3f : 2f / 3f;

            /// <summary>Steps up in four flat stages across the duration.</summary>
            public static float Step3(float time) =>
                time < 0.25f ? 0f : time < 0.5f ? 0.25f : time < 0.75f ? 0.5f : 0.75f;

            /// <summary>Eases in on a sine curve (gentle start, faster finish).</summary>
            public static float SineIn(float time) =>
                (float)System.Math.Sin(time * HalfPi - HalfPi) + 1f;

            /// <summary>Eases out on a sine curve (fast start, gentle finish).</summary>
            public static float SineOut(float time) =>
                (float)System.Math.Sin(time * HalfPi);

            /// <summary>Eases in and out on a sine curve (gentle at both ends).</summary>
            public static float SineInOut(float time) =>
                ((float)System.Math.Sin(time * PI - HalfPi) + 1f) / 2f;

            /// <summary>Eases in on a circular arc (slow start that accelerates sharply).</summary>
            public static float CircIn(float time)
            {
                return 1f - (float)System.Math.Sqrt(1f - time * time);
            }

            /// <summary>Eases out on a circular arc (sharp start that decelerates).</summary>
            public static float CircOut(float time)
            {
                time -= 1f;
                return (float)System.Math.Sqrt(1f - time * time);
            }

            /// <summary>Eases in and out on a circular arc.</summary>
            public static float CircInOut(float time)
            {
                time *= 2f;
                if (time < 1f)
                    return (1f - (float)System.Math.Sqrt(1f - time * time)) * 0.5f;
                time -= 2f;
                return ((float)System.Math.Sqrt(1f - time * time) + 1f) * 0.5f;
            }

            /// <summary>Eases in on a quadratic curve (slow start, fast finish).</summary>
            public static float Pow2In(float time) =>
                (float)System.Math.Pow(time, 2f);

            /// <summary>Eases out on a quadratic curve (fast start, slow finish).</summary>
            public static float Pow2Out(float time) =>
                -((float)System.Math.Pow(time - 1f, 2f) - 1f);

            /// <summary>Eases in and out on a quadratic curve.</summary>
            public static float Pow2InOut(float time) =>
                time < 0.5f
                    ? (float)System.Math.Pow(time * 2f, 2f) * 0.5f
                    : (-((float)System.Math.Pow((time * 2f) - 2f, 2f) - 1f) * 0.5f) + 0.5f;

            /// <summary>Eases in on a cubic curve, steeper than quadratic.</summary>
            public static float Pow3In(float time) =>
                (float)System.Math.Pow(time, 3f);

            /// <summary>Eases out on a cubic curve, steeper than quadratic.</summary>
            public static float Pow3Out(float time) =>
                (float)System.Math.Pow(time - 1f, 3f) + 1f;

            /// <summary>Eases in and out on a cubic curve.</summary>
            public static float Pow3InOut(float time) =>
                time < 0.5f
                    ? (float)System.Math.Pow(time * 2f, 3f) * 0.5f
                    : (((float)System.Math.Pow((time * 2f) - 2f, 3f) + 1f) * 0.5f) + 0.5f;

            /// <summary>Eases in on a quartic curve, steeper than cubic.</summary>
            public static float Pow4In(float time) =>
                (float)System.Math.Pow(time, 4f);

            /// <summary>Eases out on a quartic curve, steeper than cubic.</summary>
            public static float Pow4Out(float time) =>
                -((float)System.Math.Pow(time - 1f, 4f) - 1f);

            /// <summary>Eases in and out on a quartic curve.</summary>
            public static float Pow4InOut(float time) =>
                time < 0.5f
                    ? (float)System.Math.Pow(time * 2f, 4f) * 0.5f
                    : (-((float)System.Math.Pow((time * 2f) - 2f, 4f) - 1f) * 0.5f) + 0.5f;

            /// <summary>Eases in on a quintic curve, the steepest of the power eases.</summary>
            public static float Pow5In(float time) =>
                (float)System.Math.Pow(time, 5f);

            /// <summary>Eases out on a quintic curve, the steepest of the power eases.</summary>
            public static float Pow5Out(float time) =>
                (float)System.Math.Pow(time - 1f, 5f) + 1f;

            /// <summary>Eases in and out on a quintic curve.</summary>
            public static float Pow5InOut(float time) =>
                time < 0.5f
                    ? (float)System.Math.Pow(time * 2f, 5f) * 0.5f
                    : (((float)System.Math.Pow((time * 2f) - 2f, 5f) + 1f) * 0.5f) + 0.5f;

            /// <summary>Eases in with an elastic wobble that builds toward the finish.</summary>
            public static float ElasticIn(float time)
            {
                return time * (float)System.Math.Cos(PI * 4f * time);
            }

            /// <summary>Eases out with an elastic wobble that settles at the finish.</summary>
            public static float ElasticOut(float time)
            {
                return 1f - ((1f - time) * (float)System.Math.Cos(PI * 4f * time));
            }

            /// <summary>Eases in and out with an elastic wobble at both ends.</summary>
            public static float ElasticInOut(float time)
            {
                return ((float)System.Math.Sin(time * PI) * (float)System.Math.Cos(PI * time * 3f)) + time;
            }

            /// <summary>Eases in with a bouncing effect before reaching the start of the motion.</summary>
            public static float BounceIn(float time)
            {
                return 1f - BounceOut(1f - time);
            }

            /// <summary>Eases out with a bouncing effect as it settles at the finish.</summary>
            public static float BounceOut(float time)
            {
                if (time < 1f / 2.75f)
                    return 7.5625f * time * time;
                if (time < 2f / 2.75f)
                {
                    time -= 1.5f / 2.75f;
                    return 7.5625f * time * time + 0.75f;
                }
                if (time < 2.5f / 2.75f)
                {
                    time -= 2.25f / 2.75f;
                    return 7.5625f * time * time + 0.9375f;
                }
                time -= 2.625f / 2.75f;
                return 7.5625f * time * time + 0.984375f;
            }

            /// <summary>Eases in and out with a bouncing effect at both ends.</summary>
            public static float BounceInOut(float time)
            {
                if (time < 0.5f)
                    return (1f - BounceOut(1f - time * 2f)) * 0.5f;
                return BounceOut(time * 2f - 1f) * 0.5f + 0.5f;
            }

            /// <summary>Eases in by first pulling slightly backward before moving forward.</summary>
            public static float BackIn(float time)
            {
                return time + ((1f - (float)System.Math.Pow(time, 4f)) * -0.5f * (float)System.Math.Sin(time * PI));
            }

            /// <summary>Eases out by overshooting the finish and settling back.</summary>
            public static float BackOut(float time)
            {
                return time + ((1f - time) * 1.5f * (float)System.Math.Sin(time * 2.5f));
            }

            /// <summary>Eases in and out with a slight overshoot at both ends.</summary>
            public static float BackInOut(float time)
            {
                return time + ((1.25f * (float)System.Math.Sin(time * PI)) * (.5f * (float)System.Math.Sin(-2f * time * PI)));
            }

            /// <summary>Eases in exponentially (very slow start, very fast finish).</summary>
            public static float ExpoIn(float time)
            {
                return time <= 0f ? 0f : (float)System.Math.Pow(2f, 10f * (time - 1f));
            }

            /// <summary>Eases out exponentially (very fast start, very slow finish).</summary>
            public static float ExpoOut(float time)
            {
                return time >= 1f ? 1f : 1f - (float)System.Math.Pow(2f, -10f * time);
            }

            /// <summary>Eases in and out exponentially.</summary>
            public static float ExpoInOut(float time)
            {
                if (time <= 0f) return 0f;
                if (time >= 1f) return 1f;
                if (time < 0.5f)
                    return (float)System.Math.Pow(2f, 10f * (time * 2f - 1f)) * 0.5f;
                return (1f - (float)System.Math.Pow(2f, -10f * (time * 2f - 1f))) * 0.5f + 0.5f;
            }

            /// <summary>Smooth S-curve with zero slope at both ends (Hermite smoothstep).</summary>
            public static float SmoothStep(float time)
            {
                return time * time * (3f - 2f * time);
            }

            /// <summary>Smoother S-curve than SmoothStep, with zero first and second derivatives at both ends.</summary>
            public static float SmootherStep(float time)
            {
                return time * time * time * (time * (time * 6f - 15f) + 10f);
            }

        }
    }
}
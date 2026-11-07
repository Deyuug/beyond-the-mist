using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARR
{
    /// Tiny helpers for building uGUI entirely from code.
    public static class UIKit
    {
        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// Position in a top-left based coordinate system (x right, y down) inside a fixed-size parent.
        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced && sprite != null) { img.type = UnityEngine.UI.Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.6f; }
            return img;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            return Image(parent, name, color, ArrArt.RoundRect, true);
        }

        public static Text Text(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool shadow = false)
        {
            var rt = Node(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = ArrArt.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.1f;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0.08f, 0.1f, 0.55f);
                s.effectDistance = new Vector2(2, -2);
            }
            return t;
        }

        public static Button Button(Transform parent, string name, string label, int size, Color bg, Action onClick, Color? fg = null)
        {
            var img = Panel(parent, name, bg);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var t = Text(img.transform, "Label", label, size, fg ?? ArrArt.Ink, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 6, 2, 6, 2);
            return b;
        }

        /// Shrink the font (never grow it) until the text fits its box: keeps English labels inside their panels.
        public static Text Fit(Text t, int min = 10)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = t.fontSize;
            t.resizeTextMinSize = Mathf.Min(min, t.fontSize);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public static void SetLabel(Button b, string label)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = label;
        }
    }

    /// Button that reports press and release (for steering).
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) { Held = true; }
        public void OnPointerUp(PointerEventData e) { Held = false; }
        public void OnPointerExit(PointerEventData e) { Held = false; }
        void OnDisable() { Held = false; }
    }

    /// Scales a fixed-size design canvas (e.g. 960x1080) to fit its parent while keeping proportions.
    [ExecuteAlways]
    public class FitInside : MonoBehaviour
    {
        public Vector2 designSize = new Vector2(960, 1080);

        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            Vector2 p = parent.rect.size;
            if (p.x <= 0 || p.y <= 0) return;
            float s = Mathf.Min(p.x / designSize.x, p.y / designSize.y);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = designSize;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = new Vector3(s, s, 1);
        }
    }

    /// Billboard that always faces the child camera.
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = OceanView.I != null ? OceanView.I.cam : Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        }
    }
}

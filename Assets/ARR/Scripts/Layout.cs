using UnityEngine;
using UnityEngine.UI;

namespace ARR
{
    public enum LayoutMode { Split, ChildOnly, ParentOnly, DualDisplay }

    /// Arranges the two asymmetric views. Default: split screen (child left, parent right).
    /// With a second monitor in a build, the parent's chart moves to display 2 automatically.
    public class Layout : MonoBehaviour
    {
        public static Layout I;
        public LayoutMode mode = LayoutMode.Split;
        public Canvas childCanvas, parentCanvas;
        public RectTransform childRoot, parentRoot;
        Camera bgCam, bgCam2;

        public void Init(Transform root)
        {
            I = this;
            bgCam = MakeBgCam(root, "BackgroundCamera", 0);
            childCanvas = MakeCanvas(root, "ChildCanvas", 0, out childRoot);
            parentCanvas = MakeCanvas(root, "ParentCanvas", 10, out parentRoot);

            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-dual") >= 0) EnableDualDisplay();
            Apply();
        }

        /// Opt-in (F2 or the -dual flag): child fullscreen on display 1, parent chart on display 2.
        public void EnableDualDisplay()
        {
            if (Application.isEditor || Display.displays.Length < 2 || mode == LayoutMode.DualDisplay) return;
            Display.displays[1].Activate();
            if (bgCam2 == null) bgCam2 = MakeBgCam(transform, "BackgroundCamera2", 1);
            mode = LayoutMode.DualDisplay;
            Apply();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2)) EnableDualDisplay();
        }

        void OnDestroy() { if (I == this) I = null; }

        static Camera MakeBgCam(Transform root, string name, int display)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.04f, 0.09f, 0.11f);
            c.cullingMask = 0;
            c.depth = -10;
            c.targetDisplay = display;
            return c;
        }

        static Canvas MakeCanvas(Transform root, string name, int order, out RectTransform content)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(root, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            content = UIKit.Node(go.transform, "Root");
            return c;
        }

        public void Cycle()
        {
            if (mode == LayoutMode.DualDisplay) return;
            mode = (LayoutMode)(((int)mode + 1) % 3);
            Apply();
        }

        public void Set(LayoutMode m) { mode = m; Apply(); }

        public void Apply()
        {
            var cam = OceanView.I != null ? OceanView.I.cam : null;
            bool child = mode != LayoutMode.ParentOnly;
            bool parent = mode != LayoutMode.ChildOnly;
            Rect childRect = mode == LayoutMode.Split ? new Rect(0, 0, 0.5f, 1) : new Rect(0, 0, 1, 1);
            if (cam != null) { cam.enabled = child; cam.rect = childRect; cam.targetDisplay = 0; }

            childRoot.gameObject.SetActive(child);
            parentRoot.gameObject.SetActive(parent);
            SetAnchors(childRoot, childRect.xMin, childRect.xMax);
            if (mode == LayoutMode.Split) SetAnchors(parentRoot, 0.5f, 1f);
            else SetAnchors(parentRoot, 0f, 1f);
            parentCanvas.targetDisplay = mode == LayoutMode.DualDisplay ? 1 : 0;
            childCanvas.targetDisplay = 0;
        }

        static void SetAnchors(RectTransform rt, float x0, float x1)
        {
            rt.anchorMin = new Vector2(x0, 0);
            rt.anchorMax = new Vector2(x1, 1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// Is a screen point (pixels, bottom-left origin) inside the child's 3D view?
        public bool PointInChildView(Vector3 p)
        {
            if (mode == LayoutMode.ParentOnly) return false;
            if (mode == LayoutMode.Split) return p.x < Screen.width * 0.5f;
            return true;
        }
    }
}

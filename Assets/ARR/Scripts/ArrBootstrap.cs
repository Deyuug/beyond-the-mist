using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARR
{
    /// Builds the whole demo from code. Works in any scene: press Play and it appears.
    ///
    /// Keys
    ///   Child      joystick / ←→ / A D steer the boat
    ///   Parent     Q / E turn the lighthouse beam, or drag on the chart
    ///   Clinician  Enter start · P pause/resume · X end · R reset
    ///   Demo       Space simulated breath · B breath source · F1/Tab layout · F2 dual display · ] fast-forward · M mute · H help
    public class ArrBootstrap : MonoBehaviour
    {
        public static ArrBootstrap I;
        GameObject root;
        GameObject help;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindAnyObjectByType<ArrBootstrap>() == null)
                new GameObject("ARR_Bootstrap").AddComponent<ArrBootstrap>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            Build();
        }

        public void Rebuild()
        {
            if (root != null) { root.SetActive(false); Destroy(root); }
            Build();
        }

        void Build()
        {
            // other cameras / listeners in the scene would fight with ours
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (root == null || !c.transform.IsChildOf(root.transform)) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) l.enabled = false;

            root = new GameObject("ARR");
            var t = root.transform;

            root.AddComponent<Voyage>();
            root.AddComponent<BreathGuide>();
            root.AddComponent<BreathSignal>();

            var world = new GameObject("World").transform;
            world.SetParent(t, false);
            world.gameObject.AddComponent<OceanView>().Init(world);
            world.gameObject.AddComponent<LighthouseView>().Init(world);
            world.gameObject.AddComponent<Surprises>().Init(world);
            world.gameObject.AddComponent<MistField>().Init(world);
            world.gameObject.AddComponent<MistLayers>().Init(world);
            world.gameObject.AddComponent<Stardust>().Init();

            var ui = new GameObject("UI").transform;
            ui.SetParent(t, false);
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(ui, false);
            var layout = ui.gameObject.AddComponent<Layout>();
            layout.Init(ui);
            ui.gameObject.AddComponent<ChildUI>().Init(layout.childRoot);
            ui.gameObject.AddComponent<ParentUI>().Init(layout.parentRoot);
            BuildHelp(layout.parentCanvas.transform);
            layout.Apply();

            root.AddComponent<AmbientAudio>().Init();
        }

        void BuildHelp(Transform canvas)
        {
            var p = UIKit.Panel(canvas, "Help", new Color(0.03f, 0.08f, 0.1f, 0.94f));
            var rt = p.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(860, 640);
            var txt = UIKit.Text(p.transform, "T",
                "Shortcuts\n\n" +
                "Child          joystick / ← → / A D: turn the boat (it sails forward by itself)\n" +
                "Parent        Q / E or drag on the chart: turn the lighthouse beam\n" +
                "Clinician    Enter start · P pause / resume · X end · R reset\n" +
                "Breath        Space = simulate one breath · B switch source (simulated / mic / UDP)\n" +
                "                 G = breathing guide on / off (the clinician decides)\n" +
                "Demo          F1 switch view · F2 dual display · ] speed · M mute\n\n" +
                $"External sensor: send values 0–1 to UDP port {BreathSignal.UdpPort}\n\n" +
                "Music: \"Silver Blue Light\" by Kevin MacLeod (incompetech.com)\n" +
                "Licensed under Creative Commons: By Attribution 4.0 (creativecommons.org/licenses/by/4.0)\n\nPress H to close",
                22, ArrArt.TextMain, TextAnchor.MiddleLeft);
            UIKit.Stretch(txt.rectTransform, 40, 30, 40, 30);
            help = p.gameObject;
            help.SetActive(false);
        }

        void Update()
        {
            var v = Voyage.I;
            if (v == null) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (v.phase == Phase.Ready) v.StartVoyage();
            }
            if (Input.GetKeyDown(KeyCode.P)) v.TogglePause();
            if (Input.GetKeyDown(KeyCode.X)) v.EndTreatment();   // not E: E turns the light right
            if (Input.GetKeyDown(KeyCode.R)) { Rebuild(); return; }
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Tab)) Layout.I?.Cycle();
            if (Input.GetKeyDown(KeyCode.RightBracket)) v.demoSpeed = v.demoSpeed > 1.5f ? 1f : 4f;
            if (Input.GetKeyDown(KeyCode.H) && help != null) help.SetActive(!help.activeSelf);
        }
    }
}

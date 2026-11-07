using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARR
{
    /// The parent's tablet: a treasure chart that shows what the child cannot see (the hidden surprises),
    /// and the lighthouse light the parent turns to guide the child. The parent cannot steer the boat.
    /// Side panels describe the teamwork in words (no scores or percentages), show the breathing waveform and the
    /// hidden surprises, and mirror the child's steering. The clinician console sits at the bottom, separate.
    public class ParentUI : MonoBehaviour
    {
        RectTransform content;
        Text statusText;
        Button speedBtn;

        // ---- chart
        RectTransform mapArea;
        static readonly Vector2 MapSize = new Vector2(560, 660);
        const float MapScale = 1.22f;           // pixels per metre
        RectTransform boatIcon, aimMarker;
        readonly List<Image> beamDots = new List<Image>();
        readonly List<Image> trail = new List<Image>();
        int trailNext;
        Vector3 lastTrail = new Vector3(9999, 0, 9999);
        class Marker { public Surprise s; public RectTransform rt; public Image icon, ring, glow; public Text label; }
        readonly List<Marker> markers = new List<Marker>();

        // ---- cooperation
        Image guidedDot, followDot;
        Text guidedText, followText, progressText, hintText;

        // ---- breath
        readonly Image[] pulseBars = new Image[60];
        Text breathSource;

        // ---- list
        readonly List<Text> listStatus = new List<Text>();

        // ---- beam / child
        HoldButton beamLeft, beamRight;
        Text beamText, childText;
        RectTransform childKnob;

        // ---- clinician
        Button startBtn;
        readonly Button[] durBtns = new Button[3];
        static readonly float[] Durations = { 180f, 300f, 480f };
        Text timerText, timerNote;
        Button guideBtn;

        public void Init(RectTransform parent)
        {
            var bg = UIKit.Image(parent, "ParentBackground", new Color(0.03f, 0.05f, 0.14f, 1f));
            UIKit.Stretch(bg.rectTransform);
            content = UIKit.Node(parent, "ParentContent");
            content.gameObject.AddComponent<FitInside>().designSize = new Vector2(960, 1080);

            BuildHeader();
            BuildMap();
            BuildCooperation();
            BuildBreath();
            BuildList();
            BuildBeamControls();
            BuildChildPanel();
            BuildClinician();
        }

        // ================================================================ helpers
        RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color c)
        {
            var p = UIKit.Panel(parent, name, c);
            return UIKit.Place(p.rectTransform, x, y, w, h);
        }

        Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft)
        {
            var t = UIKit.Text(parent, "Text", text, size, c, a);
            UIKit.Place(t.rectTransform, x, y, w, h);
            return UIKit.Fit(t);
        }

        Button Btn(Transform parent, string label, float x, float y, float w, float h, int size, Color bg, Action a, Color? fg = null)
        {
            var b = UIKit.Button(parent, "Btn", label, size, bg, a, fg);
            UIKit.Place((RectTransform)b.transform, x, y, w, h);
            UIKit.Fit(b.GetComponentInChildren<Text>());
            return b;
        }

        Image Dot(Transform parent, Vector2 pos, float size, Color c, Sprite s)
        {
            var img = UIKit.Image(parent, "Dot", c, s);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            return img;
        }

        Vector2 W2M(Vector3 w)
        {
            Vector3 d = w - Voyage.I.lighthouse;
            return MapSize * 0.5f + new Vector2(d.x, d.z) * MapScale;
        }

        // ================================================================ build
        void BuildHeader()
        {
            Label(content, "Navigator · Treasure Chart", 24, 14, 440, 56, 30, ArrArt.TextMain);
            statusText = Label(content, "", 470, 20, 170, 44, 18, ArrArt.TextDim);
            speedBtn = Btn(content, "Demo speed ×1", 650, 22, 136, 42, 17, ArrArt.PanelLight, ToggleSpeed, ArrArt.TextMain);
            Btn(content, "Switch view F1", 796, 22, 144, 42, 18, ArrArt.PanelLight, () => Layout.I?.Cycle(), ArrArt.TextMain);
        }

        public void ToggleSpeed()
        {
            var v = Voyage.I;
            v.demoSpeed = v.demoSpeed > 1.5f ? 1f : 4f;
        }

        void BuildMap()
        {
            var frame = Box(content, "MapFrame", 20, 84, 580, 680, new Color(0.10f, 0.12f, 0.30f, 1f));
            mapArea = UIKit.Node(frame, "MapArea");
            UIKit.Place(mapArea, 10, 10, MapSize.x, MapSize.y);
            mapArea.gameObject.AddComponent<RectMask2D>();

            var chart = UIKit.Node(mapArea, "Chart").gameObject.AddComponent<RawImage>();
            UIKit.Stretch(chart.rectTransform);
            chart.texture = MakeChartTexture();
            chart.raycastTarget = true;
            chart.gameObject.AddComponent<MapDrag>().onDrag = OnMapDrag;

            for (int i = 0; i < 300; i++) trail.Add(Dot(mapArea, new Vector2(-100, -100), 4, new Color(1, 1, 1, 0), ArrArt.Circle));
            for (int i = 0; i < 26; i++) beamDots.Add(Dot(mapArea, Vector2.zero, 8, ArrArt.Gold, ArrArt.Circle));
            aimMarker = Dot(mapArea, Vector2.zero, 46, new Color(1f, 0.82f, 0.49f, 0.5f), ArrArt.GlowSprite).rectTransform;

            foreach (var s in Voyage.I.surprises)
            {
                var m = new Marker { s = s };
                m.rt = UIKit.Node(mapArea, "Marker" + s.id);
                m.rt.anchorMin = m.rt.anchorMax = Vector2.zero;
                m.rt.sizeDelta = new Vector2(10, 10);
                m.rt.anchoredPosition = W2M(s.pos);
                m.glow = Dot(m.rt, new Vector2(5, 5), 96, new Color(s.color.r, s.color.g, s.color.b, 0.3f), ArrArt.GlowSprite);
                m.ring = Dot(m.rt, new Vector2(5, 5), 44, new Color(1, 1, 1, 0.5f), ArrArt.Ring);
                m.icon = Dot(m.rt, new Vector2(5, 5), 26, s.color, ArrArt.Star);
                m.label = UIKit.Text(m.rt, "Label", s.name, 17, Color.white, TextAnchor.MiddleLeft, true);
                m.label.rectTransform.anchorMin = m.label.rectTransform.anchorMax = Vector2.zero;
                m.label.rectTransform.pivot = new Vector2(0, 0.5f);
                bool leftSide = W2M(s.pos).x > MapSize.x - 180;
                m.label.alignment = leftSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                m.label.rectTransform.pivot = new Vector2(leftSide ? 1 : 0, 0.5f);
                m.label.rectTransform.anchoredPosition = new Vector2(leftSide ? -20 : 30, 5);
                m.label.rectTransform.sizeDelta = new Vector2(170, 26);
                markers.Add(m);
            }

            // lighthouse
            Vector2 lp = W2M(Voyage.I.lighthouse);
            Dot(mapArea, lp, 70, new Color(1f, 0.82f, 0.49f, 0.45f), ArrArt.GlowSprite);
            Dot(mapArea, lp, 22, ArrArt.Gold, ArrArt.Circle);
            var lt = Label(mapArea, "Lighthouse", 0, 0, 120, 24, 16, ArrArt.Gold, TextAnchor.MiddleCenter);
            lt.rectTransform.anchorMin = lt.rectTransform.anchorMax = Vector2.zero;
            lt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lt.rectTransform.anchoredPosition = lp + new Vector2(0, -24);

            boatIcon = Dot(mapArea, Vector2.zero, 28, Color.white, ArrArt.Boat).rectTransform;
            Label(mapArea, "Drag on the chart to turn the lighthouse beam", 14, MapSize.y - 36, 400, 28, 16, new Color(0.85f, 0.85f, 1f, 0.8f));
            Label(mapArea, "N", MapSize.x - 46, 10, 36, 30, 22, new Color(0.85f, 0.85f, 1f, 0.8f), TextAnchor.MiddleCenter);
            Label(mapArea, "Only you can see these", MapSize.x - 210, MapSize.y - 36, 196, 28, 15, new Color(0.85f, 0.85f, 1f, 0.6f), TextAnchor.MiddleRight);
        }

        Texture2D MakeChartTexture()
        {
            const int w = 280, h = 330;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            Color deep = new Color(0.05f, 0.08f, 0.24f), light = new Color(0.09f, 0.14f, 0.34f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.018f + 2f, y * 0.018f + 9f);
                    Color c = Color.Lerp(deep, light, n);
                    float contour = Mathf.Abs(Mathf.Repeat(n * 9f, 1f) - 0.5f);
                    if (contour < 0.04f) c += new Color(0.07f, 0.06f, 0.12f);
                    // range rings around the lighthouse
                    float r = Mathf.Sqrt((x - w / 2f) * (x - w / 2f) + (y - h / 2f) * (y - h / 2f));
                    if (Mathf.Abs(Mathf.Repeat(r, 61f) - 0.5f) < 0.7f) c += new Color(0.05f, 0.05f, 0.1f);
                    c.a = 1;
                    px[y * w + x] = c;
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        void BuildCooperation()
        {
            var p = Box(content, "Coop", 616, 84, 324, 232, ArrArt.Panel);
            Label(p, "Teamwork", 16, 8, 200, 32, 22, ArrArt.TextMain);
            guidedDot = UIKit.Image(p, "G", Color.gray, ArrArt.Circle);
            UIKit.Place(guidedDot.rectTransform, 18, 52, 18, 18);
            guidedText = Label(p, "", 46, 44, 270, 34, 18, ArrArt.TextMain);
            followDot = UIKit.Image(p, "F", Color.gray, ArrArt.Circle);
            UIKit.Place(followDot.rectTransform, 18, 90, 18, 18);
            followText = Label(p, "", 46, 82, 270, 34, 18, ArrArt.TextMain);
            // no bars, no percentages: the state is described in words
            progressText = Label(p, "", 18, 124, 290, 30, 16, ArrArt.TextDim);
            hintText = Label(p, "", 18, 158, 290, 66, 16, ArrArt.Gold, TextAnchor.UpperLeft);
        }

        void BuildBreath()
        {
            var p = Box(content, "Breath", 616, 326, 324, 178, ArrArt.Panel);
            Label(p, "Breath · clearing the mist", 16, 8, 300, 32, 21, ArrArt.TextMain);
            Label(p, "Breathe with the ring · never affects sailing", 16, 38, 300, 24, 14, ArrArt.TextDim);
            for (int i = 0; i < pulseBars.Length; i++)
            {
                var b = UIKit.Image(p, "Bar", new Color(0.7f, 0.75f, 1f, 0.8f), null);
                b.rectTransform.anchorMin = b.rectTransform.anchorMax = new Vector2(0, 1);
                b.rectTransform.pivot = new Vector2(0, 0);
                b.rectTransform.anchoredPosition = new Vector2(16 + i * 4.85f, -118);
                b.rectTransform.sizeDelta = new Vector2(3, 2);
                pulseBars[i] = b;
            }
            breathSource = Label(p, "", 16, 128, 176, 44, 13, ArrArt.TextDim, TextAnchor.UpperLeft);
            Btn(p, "Simulate breath", 192, 128, 120, 38, 15, new Color(0.72f, 0.70f, 1f), () => BreathSignal.I?.TriggerPulse());
        }

        void BuildList()
        {
            var p = Box(content, "List", 616, 514, 324, 250, ArrArt.Panel);
            Label(p, "Hidden surprises", 16, 8, 190, 32, 21, ArrArt.TextMain);
            Label(p, "only you see them", 190, 12, 120, 26, 14, ArrArt.TextDim, TextAnchor.MiddleRight);
            var v = Voyage.I;
            for (int i = 0; i < v.surprises.Count; i++)
            {
                var s = v.surprises[i];
                var icon = UIKit.Image(p, "I", s.color, ArrArt.Star);
                UIKit.Place(icon.rectTransform, 18, 52 + i * 46, 30, 30);
                Label(p, s.name, 58, 48 + i * 46, 150, 38, 18, ArrArt.TextMain);
                listStatus.Add(Label(p, "", 206, 56 + i * 46, 104, 22, 14, ArrArt.TextDim, TextAnchor.MiddleRight));
            }
        }

        void BuildBeamControls()
        {
            var p = Box(content, "Beam", 20, 776, 580, 112, ArrArt.Panel);
            beamLeft = Hold(p, "Beam left", 14, 14, 160, 84, 90f);
            beamRight = Hold(p, "Beam right", 406, 14, 160, 84, -90f);
            beamText = Label(p, "", 180, 14, 220, 44, 26, ArrArt.Gold, TextAnchor.MiddleCenter);
            Label(p, "Q / E keys, or drag the chart", 180, 58, 220, 40, 15, ArrArt.TextDim, TextAnchor.MiddleCenter);
        }

        HoldButton Hold(Transform parent, string label, float x, float y, float w, float h, float arrowRot)
        {
            var img = UIKit.Panel(parent, "Hold" + label, ArrArt.Gold);
            UIKit.Place(img.rectTransform, x, y, w, h);
            img.raycastTarget = true;
            var hb = img.gameObject.AddComponent<HoldButton>();
            var arrow = UIKit.Image(img.transform, "Arrow", ArrArt.Ink, ArrArt.Boat);
            UIKit.Place(arrow.rectTransform, arrowRot > 0 ? 16 : w - 50, h / 2 - 17, 34, 34);
            arrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            arrow.rectTransform.anchoredPosition += new Vector2(17, -17);
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, arrowRot);
            var t = UIKit.Text(img.transform, "L", label, 22, ArrArt.Ink, TextAnchor.MiddleCenter);
            UIKit.Place(t.rectTransform, arrowRot > 0 ? 50 : 8, h / 2 - 16, w - 58, 32);
            UIKit.Fit(t);
            return hb;
        }

        void BuildChildPanel()
        {
            var p = Box(content, "Child", 616, 776, 324, 112, ArrArt.Panel);
            Label(p, "Your child is steering", 14, 8, 300, 30, 16, ArrArt.TextDim);
            childText = Label(p, "", 14, 36, 296, 30, 20, ArrArt.TextMain);
            var track = UIKit.Panel(p, "Track", new Color(1, 1, 1, 0.12f));
            UIKit.Place(track.rectTransform, 40, 78, 244, 16);
            var knob = UIKit.Image(track.transform, "Knob", Color.white, ArrArt.Circle);
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            knob.rectTransform.sizeDelta = new Vector2(24, 24);
            childKnob = knob.rectTransform;
        }

        void BuildClinician()
        {
            var p = Box(content, "Clinician", 20, 900, 920, 164, new Color(0.17f, 0.18f, 0.21f, 0.98f));
            Label(p, "Clinician console", 20, 10, 200, 32, 23, Color.white);
            Label(p, "Clinicians start, pause and end the treatment; the game never extends it. The child can say they feel unwell at any time: pause whenever needed.", 20, 42, 450, 50, 14, new Color(0.75f, 0.77f, 0.8f), TextAnchor.UpperLeft);
            Label(p, "Planned", 20, 98, 90, 44, 17, new Color(0.85f, 0.87f, 0.9f));
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                durBtns[i] = Btn(p, $"{Durations[i] / 60:0} min", 108 + i * 98, 98, 90, 44, 18, new Color(0.85f, 0.87f, 0.9f),
                    () => { if (Voyage.I.phase == Phase.Ready) Voyage.I.plannedDuration = Durations[k]; });
            }
            timerText = Label(p, "", 430, 96, 280, 30, 20, Color.white);
            timerNote = Label(p, "", 430, 126, 280, 30, 14, new Color(1f, 0.85f, 0.6f));
            guideBtn = Btn(p, "Breath guide: on", 490, 14, 210, 42, 16, new Color(0.72f, 0.70f, 1f), () => BreathGuide.I?.Toggle());
            startBtn = Btn(p, "Start", 724, 12, 182, 70, 24, new Color(0.45f, 0.86f, 0.74f), ClinicianPrimary);
            Btn(p, "End", 724, 92, 88, 58, 20, new Color(0.96f, 0.6f, 0.55f), () => Voyage.I.EndTreatment());
            Btn(p, "Reset", 818, 92, 88, 58, 20, new Color(0.75f, 0.78f, 0.8f), () => ArrBootstrap.I.Rebuild());
        }

        public void ClinicianPrimary()
        {
            var v = Voyage.I;
            if (v.phase == Phase.Ready) v.StartVoyage();
            else v.TogglePause();
        }

        // ================================================================ events
        void OnMapDrag(Vector2 local)
        {
            if (Voyage.I.scripted) return;
            Vector2 d = local - W2M(Voyage.I.lighthouse);
            if (d.sqrMagnitude < 100f) return;
            Voyage.I.SetBeamAngle(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
        }

        // ================================================================ update
        void Update()
        {
            var v = Voyage.I;
            var br = BreathSignal.I;
            if (v == null || br == null) return;
            float dt = Time.deltaTime;
            float time = Time.time;

            if (!v.scripted)
            {
                float beam = 0;
                if (beamLeft.Held || Input.GetKey(KeyCode.Q)) beam -= 1;
                if (beamRight.Held || Input.GetKey(KeyCode.E)) beam += 1;
                v.SetBeamInput(beam);
            }

            string phase = v.phase == Phase.Ready ? "Waiting for the clinician" : v.phase == Phase.Ended ? "Voyage ended" : v.paused ? "Paused by the clinician" : "Sailing";
            if (v.phase == Phase.Sailing && !v.paused && v.anchored) phase = "Resting at anchor";
            statusText.text = phase;
            UIKit.SetLabel(speedBtn, v.demoSpeed > 1.5f ? "Demo speed ×4" : "Demo speed ×1");

            // ---- chart: beam, boat, markers
            Vector2 lp = W2M(v.lighthouse);
            float shown = LighthouseView.I != null ? LighthouseView.I.ShownAngle : v.beamAngle;
            Vector3 bd = Voyage.Dir(shown);
            for (int i = 0; i < beamDots.Count; i++)
            {
                float t = (i + 1) / (float)beamDots.Count;
                beamDots[i].rectTransform.anchoredPosition = lp + new Vector2(bd.x, bd.z) * (Voyage.BeamLength * MapScale * t);
                beamDots[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(9, 5, t);
                beamDots[i].color = new Color(1f, 0.82f, 0.49f, Mathf.Lerp(0.95f, 0.35f, t));
            }
            aimMarker.anchoredPosition = W2M(v.BeamAimPoint());
            aimMarker.gameObject.SetActive(v.phase == Phase.Sailing);

            Vector2 bp = W2M(v.boatPos);
            Vector2 clamped = new Vector2(Mathf.Clamp(bp.x, 12, MapSize.x - 12), Mathf.Clamp(bp.y, 12, MapSize.y - 12));
            boatIcon.anchoredPosition = clamped;
            boatIcon.localRotation = Quaternion.Euler(0, 0, -v.heading);
            if (Vector3.Distance(v.boatPos, lastTrail) > 6f)
            {
                lastTrail = v.boatPos;
                var t = trail[trailNext]; trailNext = (trailNext + 1) % trail.Count;
                t.rectTransform.anchoredPosition = bp;
                t.color = new Color(1f, 1f, 1f, 0.35f);
            }

            foreach (var m in markers)
            {
                bool aimed = v.aimed == m.s.id;
                float pulse = 0.75f + 0.25f * Mathf.Sin(time * 3f + m.s.id);
                m.ring.color = m.s.found ? new Color(1, 1, 1, 0.15f) : aimed ? new Color(1f, 0.82f, 0.49f, pulse) : new Color(1, 1, 1, 0.45f);
                m.ring.rectTransform.sizeDelta = Vector2.one * (aimed ? 50 + 6 * pulse : 44);
                m.glow.color = new Color(m.s.color.r, m.s.color.g, m.s.color.b, m.s.found ? 0.55f : 0.22f + (aimed ? 0.2f * pulse : 0));
                m.icon.color = m.s.found ? m.s.color : new Color(m.s.color.r, m.s.color.g, m.s.color.b, 0.85f);
                m.label.text = m.s.found ? m.s.name + " · met" : m.s.name;
            }

            // ---- cooperation
            var target = v.aimed >= 0 ? v.surprises[v.aimed] : null;
            guidedDot.color = target != null ? ArrArt.Gold : new Color(1, 1, 1, 0.25f);
            guidedText.text = target != null ? $"The light is on the {target.name}" : "The light isn't on a surprise yet";
            followDot.color = v.following ? ArrArt.Gold : new Color(1, 1, 1, 0.25f);
            followText.text = v.phase != Phase.Sailing ? "The boat hasn't set off yet" : v.following ? "Your child is following the light" : "Your child hasn't turned to the light yet";
            float prog = target != null ? target.progress : 0f;
            progressText.text = target == null ? "" : prog < 0.05f ? $"The {target.name} is still hidden in the mist" : prog < 0.6f ? $"The mist around the {target.name} is thinning" : $"The {target.name} is about to appear";
            if (v.anchored && v.AllFound) hintText.text = "You've met every surprise. Enjoy the starlit sea together; the clinician decides when to end.";
            else if (v.anchored && !v.CanSetSail) hintText.text = "The boat has stopped. Enjoy the new friend together.";
            else if (v.anchored) hintText.text = "When you're ready, turn the light to the next surprise and the boat will set off again.";
            else if (v.phase == Phase.Ready) hintText.text = "You can aim the light at a surprise while waiting for the clinician to start.";
            else if (target == null) hintText.text = "Drag the golden beam on the chart onto a hidden surprise.";
            else if (!v.following) hintText.text = "The light is on target. Wait for your child to turn toward the golden path.";
            else hintText.text = "Lovely! You're getting closer together.";

            // ---- breath waveform (display only): each bar is the stronger of two samples, so 60 bars cover ~16 s
            int n = pulseBars.Length, len = br.history.Length;
            for (int i = 0; i < n; i++)
            {
                int i0 = (br.historyHead - 2 * (n - i) + len * 2) % len;
                float val = Mathf.Max(br.history[i0], br.history[(i0 + 1) % len]);
                pulseBars[i].rectTransform.sizeDelta = new Vector2(3, 2 + val * 58);
                pulseBars[i].color = new Color(0.72f, 0.74f, 1f, 0.2f + 0.8f * i / n);
            }
            breathSource.text = "Signal: " + br.SourceLabel + "\nB to switch · Space = simulate";

            // ---- beam / child
            beamText.text = $"Beam {v.beamAngle:000}°";
            childText.text = v.phase == Phase.Sailing ? $"Heading {v.heading:000}° · {(Mathf.Abs(v.steer) < 0.1f ? "straight" : v.steer < 0 ? "turning left" : "turning right")}" : "Not sailing yet";
            childKnob.anchoredPosition = new Vector2(Mathf.Lerp(childKnob.anchoredPosition.x, v.steer * 110f, dt * 12f), 0);

            UpdateClinician(v);
        }

        void UpdateClinician(Voyage v)
        {
            string fmt(float s) => $"{(int)(s / 60):00}:{(int)(s % 60):00}";
            timerText.text = $"Time {fmt(v.elapsed)} / {fmt(v.plannedDuration)}";
            if (v.phase == Phase.Ended) timerNote.text = "Ended";
            else if (v.paused) timerNote.text = "Paused: the boat and the timer have stopped";
            else if (v.phase == Phase.Sailing && v.elapsed >= v.plannedDuration) timerNote.text = "Planned time reached. The clinician decides when to end.";
            else if (v.phase == Phase.Ready) timerNote.text = "Enter start · P pause · X end · R reset";
            else timerNote.text = "";

            if (BreathGuide.I != null) UIKit.SetLabel(guideBtn, BreathGuide.I.on ? "Breath guide: on (G)" : "Breath guide: off (G)");
            UIKit.SetLabel(startBtn, v.phase == Phase.Ready ? "Start" : v.phase == Phase.Ended ? "Ended" : v.paused ? "Resume" : "Pause");
            startBtn.interactable = v.phase != Phase.Ended;
            for (int i = 0; i < 3; i++)
            {
                bool sel = Mathf.Approximately(v.plannedDuration, Durations[i]);
                durBtns[i].GetComponent<Image>().color = sel ? new Color(0.45f, 0.86f, 0.74f) : new Color(0.55f, 0.58f, 0.62f);
                durBtns[i].interactable = v.phase == Phase.Ready || sel;
            }
            for (int i = 0; i < listStatus.Count; i++)
            {
                var s = v.surprises[i];
                listStatus[i].text = s.found ? "Met" : v.aimed == i ? (s.progress > 0.05f ? "Mist clearing" : "In the light") : "Hidden in mist";
                listStatus[i].color = s.found ? ArrArt.Gold : ArrArt.TextDim;
            }
        }
    }

    /// Press or drag on the chart to point the lighthouse.
    public class MapDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<Vector2> onDrag;
        public void OnPointerDown(PointerEventData e) => Send(e);
        public void OnDrag(PointerEventData e) => Send(e);

        void Send(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 local))
                onDrag?.Invoke(local + rt.rect.size * rt.pivot);
        }
    }
}

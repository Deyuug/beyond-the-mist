using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARR
{
    /// The child's overlay on the large display. Deliberately almost empty: no buttons, no timers, no scores,
    /// no counters. Exploring is its own reward. The child's only control is the one-handed joystick
    /// (Horizontal axis: gamepad stick, ←/→ or A/D).
    public class ChildUI : MonoBehaviour
    {
        /// Set by the promo recorder so its title card is not covered by the in-world hint.
        public static bool SuppressReadyHint;

        RectTransform root;
        Text caption;
        CanvasGroup captionGroup;
        readonly Queue<string> messages = new Queue<string>();
        float captionTimer;

        CanvasGroup pauseOverlay, endOverlay, readyGroup, stickGroup, lightGroup;
        RectTransform stickKnob, lightHint, lightArrow;

        // "which way is the light?" hint: only in the short window after the light moves to a NEW destination
        int lastAimed = -1, hintFor = -1;
        float aimedSince, hintSailTime, onCourseTime, hintSide = 1f;
        bool hintOpen;
        public const float HintSettle = 0.6f;     // the light must rest on a new surprise this long (no flicker while dragging)
        public const float HintDelay = 1.2f;      // give the child a moment to find the light on their own
        public const float HintMaxSail = 15f;     // after this much sailing, stop hinting and leave it to the parent
        public const float OffCourse = 35f, OnCourse = 20f;
        public bool HintVisible { get; private set; }

        // animated breathing cue (no text): a ring of light that fills on the in-breath and blows forward on the out-breath
        CanvasGroup cueGroup;
        RectTransform cueRoot;
        Image cueGlow, cueRing, knobImg;
        readonly Image[] motes = new Image[12];
        Text endText;
        RectTransform friendsRow;
        Image chevL, chevR;

        public void Init(RectTransform parent)
        {
            root = UIKit.Node(parent, "ChildUI");
            UIKit.Stretch(root);
            var v = Voyage.I;

            // caption
            var capBg = UIKit.Node(root, "Caption");
            capBg.anchorMin = capBg.anchorMax = new Vector2(0.5f, 1);
            capBg.pivot = new Vector2(0.5f, 1);
            capBg.anchoredPosition = new Vector2(0, -50);
            capBg.sizeDelta = new Vector2(900, 100);
            captionGroup = capBg.gameObject.AddComponent<CanvasGroup>();
            var pill = UIKit.Panel(capBg, "Pill", new Color(0.02f, 0.03f, 0.10f, 0.5f));
            UIKit.Stretch(pill.rectTransform, 60, 12, 60, 12);
            caption = UIKit.Text(capBg, "Text", "", 42, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(caption.rectTransform);
            captionGroup.alpha = 0;

            // ready hint
            var ready = UIKit.Node(root, "Ready");
            ready.anchorMin = ready.anchorMax = new Vector2(0.5f, 0.5f);
            ready.sizeDelta = new Vector2(900, 200);
            ready.anchoredPosition = new Vector2(0, 150);
            readyGroup = ready.gameObject.AddComponent<CanvasGroup>();
            var rt = UIKit.Text(ready, "T", "Turn the boat with the joystick\nand follow the golden light", 44, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(rt.rectTransform);

            // a soft golden glow + arrow at the screen edge, pointing the way to turn toward the new light
            lightHint = UIKit.Node(root, "LightHint");
            lightHint.anchorMin = lightHint.anchorMax = new Vector2(0, 0.5f);
            lightHint.sizeDelta = new Vector2(160, 160);
            lightGroup = lightHint.gameObject.AddComponent<CanvasGroup>();
            lightGroup.alpha = 0;
            var lg = UIKit.Image(lightHint, "Glow", new Color(1f, 0.8f, 0.45f, 0.55f), ArrArt.GlowSprite);
            UIKit.Stretch(lg.rectTransform);
            var arrow = UIKit.Image(lightHint, "Arrow", ArrArt.Gold, ArrArt.Boat);
            lightArrow = arrow.rectTransform;
            lightArrow.anchorMin = lightArrow.anchorMax = new Vector2(0.5f, 0.5f);
            lightArrow.sizeDelta = new Vector2(54, 54);

            // a small, translucent sight at the centre of the screen: it keeps the child's eyes on the sea ahead.
            // The thin ring breathes with the guide; the dot slides as the child steers; the chevron on the side
            // being steered lights up. No bar, no numbers.
            var stick = UIKit.Node(root, "Sight");
            stick.anchorMin = stick.anchorMax = new Vector2(0.5f, 0.5f);
            stick.pivot = new Vector2(0.5f, 0.5f);
            // just above the exact centre: the open sea ahead of the boat, clear of the masthead (which reaches the
            // middle of the screen in the chase view)
            stick.anchoredPosition = new Vector2(0f, 110f);
            stick.sizeDelta = new Vector2(240, 140);
            stickGroup = stick.gameObject.AddComponent<CanvasGroup>();
            stickGroup.blocksRaycasts = false;
            stickGroup.alpha = 0f;
            Image Chevron(string name, float x, float rot)
            {
                var im = UIKit.Image(stick, name, new Color(1, 1, 1, 0.25f), ArrArt.Boat);
                var r = im.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(20, 20);
                r.anchoredPosition = new Vector2(x, 0f);
                r.localRotation = Quaternion.Euler(0, 0, rot);
                return im;
            }
            chevL = Chevron("L", -78f, 90f);
            chevR = Chevron("R", 78f, -90f);
            // the breathing ring lives on the dot: glow + thin ring, motes drawn in / drifting forward
            cueRoot = UIKit.Node(stick, "BreathCue");
            cueRoot.anchorMin = cueRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cueRoot.pivot = new Vector2(0.5f, 0.5f);
            cueRoot.sizeDelta = new Vector2(160, 160);
            cueGroup = cueRoot.gameObject.AddComponent<CanvasGroup>();
            cueGroup.alpha = 0;
            cueGroup.blocksRaycasts = false;
            Image CueImg(string name, Sprite sp, float size)
            {
                var im = UIKit.Image(cueRoot, name, Color.white, sp);
                var r = im.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(size, size);
                return im;
            }
            cueGlow = CueImg("Glow", ArrArt.GlowSprite, 110);
            cueRing = CueImg("Ring", ArrArt.Ring, 72);
            for (int i = 0; i < motes.Length; i++) motes[i] = CueImg("Mote", ArrArt.GlowSprite, 14);

            var knob = UIKit.Image(stick, "Dot", ArrArt.Gold, ArrArt.Circle);
            knobImg = knob;
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            knob.rectTransform.sizeDelta = new Vector2(18, 18);
            stickKnob = knob.rectTransform;

            pauseOverlay = Overlay("Pause", new Color(0.01f, 0.02f, 0.07f, 0.45f), "The boat is resting for a moment\nThe doctor is right here", out _);
            endOverlay = Overlay("End", new Color(0.01f, 0.02f, 0.08f, 0.6f), "", out endText);
            friendsRow = UIKit.Node(endOverlay.transform, "Friends");
            friendsRow.anchorMin = friendsRow.anchorMax = new Vector2(0.5f, 0.5f);
            friendsRow.sizeDelta = new Vector2(700, 120);
            friendsRow.anchoredPosition = new Vector2(0, 150);

            v.ChildMessage += Show;
        }

        void OnDestroy()
        {
            if (Voyage.I != null) Voyage.I.ChildMessage -= Show;
        }

        CanvasGroup Overlay(string name, Color bg, string text, out Text t)
        {
            var o = UIKit.Image(root, name, bg);
            UIKit.Stretch(o.rectTransform);
            var g = o.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            g.blocksRaycasts = false;
            t = UIKit.Text(o.transform, "T", text, 48, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(t.rectTransform, 40, 0, 40, 0);
            return g;
        }

        public void Show(string msg)
        {
            if (messages.Count > 2) messages.Dequeue();
            messages.Enqueue(msg);
        }

        void Update()
        {
            var v = Voyage.I;
            if (v == null) return;
            float dt = Time.deltaTime;

            if (!v.scripted) v.SetSteer(Input.GetAxis("Horizontal"));

            captionTimer -= dt;
            if (captionTimer <= 0 && messages.Count > 0)
            {
                caption.text = messages.Dequeue();
                captionTimer = messages.Count > 0 ? 2.4f : 3.4f;
            }
            captionGroup.alpha = Mathf.MoveTowards(captionGroup.alpha, captionTimer > 0.4f ? 1f : 0f, dt * 3f);

            // the dot leans with the steering; the ring follows only a little (and a touch later), so the sight stays
            // a steady point to look at while still showing the turn
            stickKnob.anchoredPosition = new Vector2(Mathf.Lerp(stickKnob.anchoredPosition.x, v.steer * 26f, dt * 12f), 0);
            // translucent while sailing; softer while stopped beside a friend (so it never covers them); gone on the
            // pause and end screens
            float sightA = v.phase == Phase.Ended || v.paused ? 0f : v.phase != Phase.Sailing ? 0.3f : v.anchored ? 0.25f : 0.6f;
            stickGroup.alpha = Mathf.MoveTowards(stickGroup.alpha, sightA, dt * 1.5f);
            chevL.color = new Color(1, 1, 1, 0.22f + 0.6f * Mathf.Clamp01(-v.steer));
            chevR.color = new Color(1, 1, 1, 0.22f + 0.6f * Mathf.Clamp01(v.steer));

            UpdateLightHint(v, dt);
            UpdateBreathCue(v, dt);

            readyGroup.alpha = Mathf.MoveTowards(readyGroup.alpha, v.phase == Phase.Ready && !SuppressReadyHint ? 1f : 0f, dt * 1.5f);
            pauseOverlay.alpha = Mathf.MoveTowards(pauseOverlay.alpha, v.paused ? 1f : 0f, dt * 2f);

            bool ended = v.phase == Phase.Ended;
            if (ended && endOverlay.alpha < 0.01f) BuildEnd(v);
            endOverlay.alpha = Mathf.MoveTowards(endOverlay.alpha, ended ? 1f : 0f, dt * 0.8f);
        }

        /// The hint opens when the parent's light settles on a new hidden surprise (start, leaving an anchorage, or a
        /// change of destination). While open, it shows only if the boat is sailing and its bow is far off the light.
        /// It closes for good once the child has turned onto the light (held for a second), or after 15 s of sailing,
        /// and only opens again for the next new destination. Mid-voyage drifting never brings it back.
        void UpdateLightHint(Voyage v, float dt)
        {
            float now = Time.time;
            if (v.aimed != lastAimed) { lastAimed = v.aimed; aimedSince = now; }
            if (v.aimed >= 0 && v.aimed != hintFor && now - aimedSince > HintSettle)
            {
                hintFor = v.aimed;
                hintOpen = true;
                hintSailTime = 0f;
                onCourseTime = 0f;
            }

            bool sailing = v.phase == Phase.Sailing && !v.paused && !v.anchored;
            float err = 0f;
            if (hintOpen && sailing)
            {
                Vector3 to = v.BeamAimPoint() - v.boatPos;
                err = Mathf.DeltaAngle(v.heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                hintSailTime += dt;
                onCourseTime = Mathf.Abs(err) < OnCourse ? onCourseTime + dt : 0f;
                if (onCourseTime > 1f || hintSailTime > HintMaxSail || v.aimed < 0) hintOpen = false;
                if (Mathf.Abs(err) > OffCourse) hintSide = err > 0 ? 1f : -1f;   // positive = turn right (clockwise)
            }
            HintVisible = hintOpen && sailing && Mathf.Abs(err) > OffCourse && hintSailTime > HintDelay;

            lightGroup.alpha = Mathf.MoveTowards(lightGroup.alpha, HintVisible ? 0.6f + 0.4f * Mathf.Sin(now * 3f) : 0f, dt * 2.5f);
            float rootW = root.rect.width;
            lightHint.anchoredPosition = new Vector2(hintSide > 0 ? rootW - 70f : 70f, 30f);
            lightArrow.localRotation = Quaternion.Euler(0, 0, hintSide > 0 ? -90 : 90);
            lightArrow.anchoredPosition = new Vector2(hintSide * (8f + 6f * Mathf.Sin(now * 3f)), 0);
        }

        void UpdateBreathCue(Voyage v, float dt)
        {
            var g = BreathGuide.I;
            bool show = g != null && g.on && v.phase != Phase.Ended && !v.paused;
            cueGroup.alpha = Mathf.MoveTowards(cueGroup.alpha, show ? 1f : 0f, dt * 1.5f);
            cueRoot.anchoredPosition = new Vector2(Mathf.Lerp(cueRoot.anchoredPosition.x, v.steer * 10f, dt * 6f), 0);
            if (g == null || cueGroup.alpha <= 0.001f)
            {
                knobImg.color = ArrArt.Gold;
                stickKnob.localScale = Vector3.one;
                return;
            }

            Color cool = new Color(0.70f, 0.88f, 1f), warm = ArrArt.Gold;
            float p = g.stageProgress, e = Mathf.SmoothStep(0f, 1f, p), time = Time.time;
            // fade the motes in and out at the stage edges so the loop never pops
            float edge = Mathf.Clamp01(p * 5f) * Mathf.Clamp01((1f - p) * 5f);
            float ring, glowA, coreA;
            Color col;
            switch (g.stage)
            {
                case BreathGuide.Stage.Inhale:
                    // breathe in: the ring opens and cool light is drawn in from all around
                    ring = Mathf.Lerp(0.62f, 1.25f, e);
                    col = Color.Lerp(warm, cool, Mathf.Clamp01(p * 4f));
                    glowA = Mathf.Lerp(0.25f, 0.65f, e);
                    coreA = Mathf.Lerp(0.55f, 1f, e);
                    for (int i = 0; i < motes.Length; i++)
                    {
                        float q = Mathf.Repeat(p * 1.4f + i * 0.383f, 1f);   // each mote travels inward, staggered
                        float a = i * 2.39996f + 0.4f * time;                 // golden-angle spread round the ring
                        float r = Mathf.Lerp(84f, 24f, q);
                        motes[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        motes[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(16f, 24f, q);
                        motes[i].color = new Color(cool.r, cool.g, cool.b, Mathf.Sin(q * Mathf.PI) * edge);
                    }
                    break;
                case BreathGuide.Stage.Exhale:
                    // breathe out: the ring settles and warm light streams up and forward, like the breath pushing the mist ahead
                    ring = Mathf.Lerp(1.25f, 0.62f, e);
                    col = Color.Lerp(cool, warm, Mathf.Clamp01(p * 4f));
                    glowA = Mathf.Lerp(0.65f, 0.25f, e);
                    coreA = Mathf.Lerp(1f, 0.55f, e);
                    for (int i = 0; i < motes.Length; i++)
                    {
                        float q = Mathf.Repeat(p * 1.3f + i * 0.383f, 1f);   // staggered stream out of the ring
                        float k = Mathf.Sin(i * 12.9898f) * 0.5f;             // fixed lane per mote, -0.5..0.5
                        float a = Mathf.PI * 0.5f + k * 1.3f + 0.12f * Mathf.Sin(time * 3f + i);
                        float r = Mathf.Lerp(26f, 110f, q);
                        motes[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        motes[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(24f, 12f, q);
                        motes[i].color = new Color(warm.r, warm.g, warm.b, (1f - q) * edge);
                    }
                    break;
                default:
                    ring = 0.62f + 0.02f * Mathf.Sin(time * 4f);
                    col = warm;
                    glowA = 0.25f;
                    coreA = 0.55f;
                    foreach (var m in motes) m.color = Color.clear;
                    break;
            }
            cueRing.rectTransform.localScale = Vector3.one * ring;
            cueRing.color = new Color(col.r, col.g, col.b, 0.85f);
            cueGlow.rectTransform.localScale = Vector3.one * (0.7f + 0.45f * ring);
            cueGlow.color = new Color(col.r, col.g, col.b, glowA * 0.6f);
            // the knob is the cue's core: it swells and turns cool on the in-breath, warm and smaller on the out-breath
            float kb = cueGroup.alpha;
            knobImg.color = Color.Lerp(ArrArt.Gold, Color.Lerp(col, Color.white, 0.15f * coreA), kb);
            // the dot breathes together with the ring (0.8x–1.45x; the ring goes 0.62x–1.25x)
            stickKnob.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f + 0.65f * (ring - 0.62f) / 0.63f, kb);
        }

        void BuildEnd(Voyage v)
        {
            for (int i = friendsRow.childCount - 1; i >= 0; i--) Destroy(friendsRow.GetChild(i).gameObject);
            // only the friends they met, by name, as a memory: no row of stars or icons (that would read as a
            // rating), nothing missed is shown, nothing is counted
            var met = new List<Surprise>();
            foreach (var s in v.surprises) if (s.found) met.Add(s);
            string names = met.Count == 0 ? "" : "\n\nToday you met\n" + string.Join(", ", met.ConvertAll(s => s.name));
            endText.text = "Today's voyage is over\nThank you for exploring together" + names;
            endText.rectTransform.offsetMin = new Vector2(40, 0);
            endText.rectTransform.offsetMax = new Vector2(-40, 0);
        }
    }
}

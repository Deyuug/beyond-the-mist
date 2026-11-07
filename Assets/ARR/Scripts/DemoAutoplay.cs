using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ARR
{
    /// Scripted run of the full loop (robot parent aims the light, robot child follows it), used to verify
    /// builds and capture stills.  Command line:  -arrCapture <outputDir>
    public class DemoAutoplay : MonoBehaviour
    {
        string dir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Check()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-arrCapture")
                {
                    var go = new GameObject("ARR_Autoplay");
                    DontDestroyOnLoad(go);
                    go.AddComponent<DemoAutoplay>().dir = args[i + 1];
                }
        }

        // ---------------------------------------------------------------- robot players (shared with the promo)
        /// Parent: turn the light toward a surprise at a human-ish speed.
        public static void AimParent(Voyage v, int target)
        {
            if (target < 0) { v.SetBeamInput(0); return; }
            float want = v.BearingFromLighthouse(v.surprises[target].pos);
            float diff = Mathf.DeltaAngle(v.beamAngle, want);
            v.SetBeamInput(Mathf.Abs(diff) < 1.5f ? 0 : Mathf.Clamp(diff / 12f, -1f, 1f));
        }

        /// Child: steer toward the golden light path.
        public static void SteerChild(Voyage v)
        {
            Vector3 to = v.BeamAimPoint() - v.boatPos;
            float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float diff = Mathf.DeltaAngle(v.heading, want);
            v.SetSteer(Mathf.Clamp(diff / 25f, -1f, 1f));
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return new WaitForSeconds(3f);
            var v = Voyage.I;
            v.scripted = true;
            Shot("01_ready");
            yield return null;

            v.plannedDuration = 300f;
            BreathSignal.I.SetSource(BreathSource.Udp);   // quiet until the scripted breath below
            v.StartVoyage();
            yield return new WaitForSeconds(2.5f);
            Shot("01b_start_light_in_view");
            yield return null;
            BreathSignal.I.SetSource(BreathSource.Udp);   // quiet: only the scripted breaths below
            Layout.I.Set(LayoutMode.ChildOnly);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-arrTreasureTest") >= 0)
            {
                // the boat 60 m short of the Little Turtle, facing it, with the light on it: thick mist, then calm
                var t0s = v.surprises[0];
                float bearing = v.BearingFromLighthouse(t0s.pos);
                Vector3 to = Voyage.Dir(bearing);
                v.boatPos = t0s.pos - to * 60f;
                v.heading = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                v.SetBeamAngle(bearing);
                OceanView.I.SnapCamera();
                BreathGuide.I.SetCalm(0f, 0f);
                yield return new WaitForSeconds(3f);
                Debug.Log("[ARR] treasure glow " + Shader.GetGlobalVector("_ArrTreasure") + " aimed=" + v.aimed
                          + " dist=" + Vector3.Distance(v.boatPos, t0s.pos).ToString("F1")
                          + " screen=" + OceanView.I.cam.WorldToScreenPoint(t0s.pos));
                Shot("t_thick");
                BreathGuide.I.SetCalm(1f, 1f);
                yield return new WaitForSeconds(4f);
                Debug.Log("[ARR] calm: dist=" + Vector3.Distance(v.boatPos, t0s.pos).ToString("F1")
                          + " screen=" + OceanView.I.cam.WorldToScreenPoint(t0s.pos) + " found=" + t0s.found);
                Shot("t_calm");
                yield return null;
                Application.Quit();
                yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-arrBreathTest") >= 0)
            {
                yield return BreathTest();
                Application.Quit();
                yield break;
            }
            yield return new WaitForSeconds(6f);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-arrClip") >= 0)
            {
                // record 30 s of the child's view at a locked 20 fps: steady breathing with the ring slowly clears the mist
                string clip = Path.Combine(dir, "clip");
                Directory.CreateDirectory(clip);
                Time.captureFramerate = 20;
                BreathSignal.I.SetSource(BreathSource.Simulated);   // a simulated child breathing with the guide
                for (int f = 0; f < 600; f++)
                {
                    yield return new WaitForEndOfFrame();
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(clip, $"f{f:00000}.jpg"), tex.EncodeToJPG(90));
                    Destroy(tex);
                }
                Time.captureFramerate = 0;
                Application.Quit();
                yield break;
            }
            Shot("02a_before_breath");
            yield return null;
            BreathSignal.I.TriggerPulse();
            float tb = Time.time;
            foreach (var (t, name) in new[] { (0.8f, "02b_breath_0.8s"), (1.8f, "02c_breath_1.8s"), (3.2f, "02d_breath_3.2s"), (7f, "02e_breath_7s") })
            {
                while (Time.time - tb < t) yield return null;
                Shot(name);
                yield return null;
            }
            // breathing cue: one frame mid in-breath, one mid out-breath
            var g = BreathGuide.I;
            while (!(g.stage == BreathGuide.Stage.Inhale && g.stageProgress > 0.65f)) yield return null;
            Shot("02f_cue_inhale");
            while (!(g.stage == BreathGuide.Stage.Exhale && g.stageProgress > 0.35f)) yield return null;
            Shot("02g_cue_exhale");
            yield return null;
            Layout.I.Set(LayoutMode.Split);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-arrQuick") >= 0)
            {
                Debug.Log("[ARR] " + AmbientAudio.I.MusicStatus);
                yield return new WaitForSeconds(1.5f);
                Shot("02h_split");
                yield return null;
                Application.Quit();
                yield break;
            }
            BreathSignal.I.SetSource(BreathSource.Simulated);
            v.demoSpeed = 3f;

            for (int target = 0; target < v.surprises.Count; target++)
            {
                float t0 = Time.time;
                bool midShot = false;
                var ui = FindAnyObjectByType<ChildUI>();
                float hintSeconds = 0f, driftUntil = -1f;
                bool hintShot = false, drifted = false, hintDuringDrift = false;
                while (!v.surprises[target].found && Time.time - t0 < 70f)
                {
                    AimParent(v, target);
                    // mid-voyage, drift off the light on purpose for 3 s: the arrow must NOT come back
                    if (!drifted && v.surprises[target].progress > 0.3f) { drifted = true; driftUntil = Time.time + 3f; }
                    if (Time.time < driftUntil) { v.SetSteer(1f); if (ui.HintVisible) hintDuringDrift = true; }
                    else SteerChild(v);
                    if (ui.HintVisible)
                    {
                        hintSeconds += Time.deltaTime;
                        if (!hintShot) { hintShot = true; Shot($"{3 + target * 2:00}a_hint_{target}"); }
                    }
                    if (!midShot && v.surprises[target].progress > 0.45f)
                    {
                        midShot = true;
                        Shot($"{3 + target * 2:00}_revealing_{target}");
                    }
                    yield return null;
                }
                Debug.Log($"[ARR] leg {target}: arrow shown {hintSeconds:0.0}s while setting off, shown during mid-voyage drift = {hintDuringDrift}");
                v.SetSteer(0);
                v.SetBeamInput(0);
                yield return new WaitForSeconds(2.5f);
                Shot($"{4 + target * 2:00}_found_{target}");
                yield return null;
                Debug.Log($"[ARR] anchored={v.anchored} speed={v.currentSpeed:0.00}");
                yield return null;
                Debug.Log($"[ARR] surprise {target} found={v.surprises[target].found} after {Time.time - t0:0.0}s");
                if (target == 1)
                {
                    v.TogglePause();
                    yield return new WaitForSeconds(1.5f);
                    Shot("07b_paused");
                    yield return null;
                    v.TogglePause();
                    Layout.I.Set(LayoutMode.ChildOnly);
                    yield return new WaitForSeconds(1.5f);
                    Shot("07c_child_fullscreen");
                    yield return null;
                    Layout.I.Set(LayoutMode.ParentOnly);
                    yield return new WaitForSeconds(1f);
                    Shot("07d_parent_fullscreen");
                    yield return null;
                    Layout.I.Set(LayoutMode.Split);
                }
            }

            v.demoSpeed = 1f;
            yield return new WaitForSeconds(4f);
            Shot("12_all_found");
            yield return null;
            v.EndTreatment();
            yield return new WaitForSeconds(3f);
            Shot("13_ended");
            yield return new WaitForSeconds(1f);
            Debug.Log($"[ARR] autoplay done. found={v.FoundCount} phase={v.phase} elapsed={v.elapsed:0}");
            Application.Quit();
        }

        /// -arrBreathTest: four phases, logging the calm level to breath_log.csv and taking stills:
        ///   steady  60 s  a simulated child breathing with the guide
        ///   silent  30 s  no breath detected at all (stopped, or too quiet for the microphone)
        ///   random  30 s  breaths at random moments, ignoring the ring
        ///   rushed  30 s  a breath every 1.6 s
        ///   natural 50 s  a young child's natural pace, steady (a breath every 2.55–2.85 s)
        ///   natural_off 30 s  the same with the guide off, from thick mist
        /// With -arrBreathNatural: only the two natural phases, from a fresh start.
        IEnumerator BreathTest()
        {
            var g = BreathGuide.I;
            var log = new System.Text.StringBuilder("t,phase,calm,match,period,clarity,cycle\n");
            float T0 = Time.time, nextLog = 0f;
            var rng = new System.Random(7);
            void Row(string phase) => log.Append($"{Time.time - T0:F1},{phase},{g.Calm:F3},{g.Match:F3},{g.period:F2},{(OceanView.I != null ? OceanView.I.clarity : 0f):F3},{g.LastCycle}\n");
            IEnumerator Phase(string name, float seconds, Func<float> nextGap)
            {
                float start = Time.time, nextPulse = nextGap != null ? nextGap() : float.MaxValue, nextShot = 0f;
                while (Time.time - start < seconds)
                {
                    float t = Time.time - start;
                    if (Time.time - T0 >= nextLog) { nextLog += 0.5f; Row(name); }
                    if (t >= nextShot) { Shot($"b_{name}_{(int)t:00}s"); nextShot += 15f; }
                    if (t >= nextPulse) { BreathSignal.I.TriggerPulse(); nextPulse += nextGap(); }
                    yield return null;
                }
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-arrBreathNatural") >= 0)
            {
                // a young child breathing at their own natural pace from the very start, guide on, then off
                BreathSignal.I.SetSource(BreathSource.Udp);
                yield return Phase("natural", 60f, () => 2.55f + (float)rng.NextDouble() * 0.3f);
                g.SetCalm(0f, 0.2f);
                g.on = false;
                yield return Phase("natural_off", 30f, () => 2.55f + (float)rng.NextDouble() * 0.3f);
                g.on = true;
                File.WriteAllText(Path.Combine(dir, "breath_log.csv"), log.ToString());
                yield break;
            }
            BreathSignal.I.SetSource(BreathSource.Simulated);
            yield return Phase("steady", 60f, null);
            BreathSignal.I.SetSource(BreathSource.Udp);   // quiet: only the scripted breaths below
            yield return Phase("silent", 30f, null);
            yield return Phase("random", 30f, () => 1.8f + (float)rng.NextDouble() * 5f);
            yield return Phase("rushed", 30f, () => 1.6f);
            // a young child's natural resting pace (~22 a minute), steady: must clear the mist, guide on and off
            yield return Phase("natural", 50f, () => 2.55f + (float)rng.NextDouble() * 0.3f);
            g.SetCalm(0f, 0.2f);
            g.on = false;
            yield return Phase("natural_off", 30f, () => 2.55f + (float)rng.NextDouble() * 0.3f);
            g.on = true;
            Shot("b_end");
            yield return null;
            File.WriteAllText(Path.Combine(dir, "breath_log.csv"), log.ToString());
        }

        void Shot(string name)
        {
            string p = Path.Combine(dir, name + ".png");
            ScreenCapture.CaptureScreenshot(p);
            Debug.Log("[ARR] shot " + p);
        }
    }
}

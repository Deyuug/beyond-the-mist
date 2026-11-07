using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ARR
{
    /// Stills for the slide deck, staged like the promo:  -arrDeckShots <outputDir>
    ///   deck_start.png       child view before the start: the boat facing the lighthouse in thick mist
    ///   deck_jelly.png       child view: the starlight jellyfish has just appeared (with its line)
    ///   deck_calm.png        child view: a clear, calm sea with the coral island ahead on the right
    ///   deck_breath_in.png   child view: thick mist while the ring grows (breathe in)
    ///   deck_breath_out.png  child view: the mist cleared by steady breathing, while the ring shrinks (breathe out)
    public class DeckShots : MonoBehaviour
    {
        string dir;
        int aimTarget = -1;
        bool drive;
        float holdCalm = -1f;   // keeps the mist at a staged level (no breath signal is running)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Check()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-arrDeckShots")
                {
                    var go = new GameObject("ARR_DeckShots");
                    DontDestroyOnLoad(go);
                    go.AddComponent<DeckShots>().dir = args[i + 1];
                }
        }

        void Update()
        {
            var v = Voyage.I;
            if (v == null) return;
            DemoAutoplay.AimParent(v, aimTarget);
            if (drive) DemoAutoplay.SteerChild(v); else v.SetSteer(0);
            if (holdCalm >= 0f && BreathGuide.I != null) BreathGuide.I.SetCalm(holdCalm, Mathf.Max(holdCalm, 0.2f));
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            ChildUI.SuppressReadyHint = true;
            var v = Voyage.I;
            v.scripted = true;
            BreathSignal.I.SetSource(BreathSource.Udp);   // quiet: no breaths unless staged
            Layout.I.Set(LayoutMode.ChildOnly);
            // before the start: the boat facing the lighthouse in thick mist, its light already in view
            holdCalm = 0f;
            yield return new WaitForSeconds(3f);
            yield return Shot("deck_start");
            v.StartVoyage();
            yield return new WaitForSeconds(1f);

            // the jellyfish appears: light on it, the child following, the mist already thinning
            holdCalm = 0.55f;
            PromoRecorder.Place(1, 32f, 0f, true);
            v.surprises[1].progress = 0.9f;
            aimTarget = 1; drive = true;
            float t0 = Time.time;
            while (!v.surprises[1].found && Time.time - t0 < 30f) yield return null;
            yield return new WaitForSeconds(1.6f);
            yield return Shot("deck_jelly");

            // a clear, calm sea: the coral island ahead on the right, the lighthouse far behind it
            drive = false; aimTarget = -1;
            AmbientAudio.I.chimesEnabled = false;
            v.Find(v.surprises[3]);
            AmbientAudio.I.chimesEnabled = true;
            holdCalm = 1f;
            yield return new WaitForSeconds(14f);   // let the queued lines ("say hello") fade first
            PromoRecorder.Place(3, 75f, -22f, false);
            yield return new WaitForSeconds(3f);
            yield return Shot("deck_calm");

            // breathing: thick mist while the ring grows, then cleared by steady breathing while it shrinks
            PromoRecorder.Place(2, 140f, -30f, false);
            holdCalm = 0f;
            yield return new WaitForSeconds(2.5f);
            yield return Stage(BreathGuide.Stage.Inhale, 0.65f);
            yield return Shot("deck_breath_in");
            holdCalm = 0.85f;
            yield return new WaitForSeconds(2.5f);
            yield return Stage(BreathGuide.Stage.Exhale, 0.55f);
            yield return Shot("deck_breath_out");

            Application.Quit();
        }

        static IEnumerator Stage(BreathGuide.Stage stage, float progress)
        {
            var g = BreathGuide.I;
            while (g.stage == stage) yield return null;   // wait for the next one from its start
            while (g.stage != stage || g.stageProgress < progress) yield return null;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string p = Path.Combine(dir, name + ".png");
            ScreenCapture.CaptureScreenshot(p);
            Debug.Log("[ARR] deck shot " + p);
            yield return null;
            yield return null;
        }
    }
}

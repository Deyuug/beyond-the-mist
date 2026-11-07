using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ARR
{
    /// Records a scripted 75 s demo as a JPG frame sequence at a locked 30 fps, plus the game's own soundscape
    /// (audio.wav), rendered a fixed number of samples per frame so sound and picture stay in sync.
    /// Launch the build with:  -arrPromo <outputDir>   (add -arrAudioOnly to re-render only audio.wav, on the
    /// identical timeline, without capturing frames: for swapping the soundtrack of an existing video)
    /// Captions and title cards are composed afterwards (Tools/make_promo.py); keep the timings in sync.
    public class PromoRecorder : MonoBehaviour
    {
        public const int Fps = 30;
        public const float Length = 75f;

        string dir;
        Image fade;
        float fadeTarget = 1f, fadeAlpha = 1f;
        float t0;
        int aimTarget = -1;
        bool drive, breathe;
        float pulseAt = -1f;
        readonly System.Collections.Generic.List<float> breathTimes = new System.Collections.Generic.List<float>();
        readonly System.Collections.Generic.List<float> foundTimes = new System.Collections.Generic.List<float>();
        BreathGuide.Stage lastStage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Check()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-arrPromo")
                {
                    var go = new GameObject("ARR_Promo");
                    DontDestroyOnLoad(go);
                    go.AddComponent<PromoRecorder>().dir = args[i + 1];
                }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            ChildUI.SuppressReadyHint = true;
            BuildFade();
            Voyage.I.scripted = true;
            BreathSignal.I.SetSource(BreathSource.Udp);   // quiet: the script breathes steadily with the guide
            Layout.I.Set(LayoutMode.ChildOnly);
            yield return new WaitForSecondsRealtime(2.5f);

            var audio = AmbientAudio.I;
            audio.muted = false;
            audio.BeginOffline();
            int rate = audio.SampleRate;
            int perFrame = Mathf.RoundToInt(rate / (float)Fps);
            var buf = new float[perFrame * 2];
            var pcm = new MemoryStream();
            var pcmWriter = new BinaryWriter(pcm);

            Voyage.I.SurpriseFound += s => { if (AmbientAudio.I.chimesEnabled) foundTimes.Add(Time.time - t0); };
            Time.captureFramerate = Fps;
            breathe = true;
            BreathGuide.I.SetCalm(0.1f, 0.2f);
            StartCoroutine(Script());
            bool audioOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "-arrAudioOnly") >= 0;
            int frames = Mathf.RoundToInt(Length * Fps);
            for (int f = 0; f < frames; f++)
            {
                yield return new WaitForEndOfFrame();
                if (!audioOnly)
                {
                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(dir, $"f{f:00000}.jpg"), tex.EncodeToJPG(92));
                    Destroy(tex);
                }
                audio.RenderOffline(buf);
                foreach (float s in buf) pcmWriter.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
            WriteWav(Path.Combine(dir, "audio.wav"), pcm.ToArray(), rate);
            File.WriteAllText(Path.Combine(dir, "found.txt"), string.Join("\n", foundTimes.ConvertAll(x => x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))));
            File.WriteAllText(Path.Combine(dir, "breaths.txt"), string.Join("\n", breathTimes.ConvertAll(x => x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))));
            Debug.Log($"[ARR] promo recorded {frames} frames + audio ({rate} Hz) -> {dir}");
            Application.Quit();
        }

        static void WriteWav(string path, byte[] data, int rate)
        {
            using var w = new BinaryWriter(File.Create(path));
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data.Length);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)2);
            w.Write(rate); w.Write(rate * 4); w.Write((short)4); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(data.Length);
            w.Write(data);
        }

        void BuildFade()
        {
            var go = new GameObject("PromoFade", typeof(RectTransform));
            DontDestroyOnLoad(go);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 500;
            fade = UIKit.Image(go.transform, "Black", Color.black);
            UIKit.Stretch(fade.rectTransform);
        }

        void Update()
        {
            if (fade != null)
            {
                fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeTarget, Time.deltaTime / 0.4f);
                fade.color = new Color(0, 0, 0, fadeAlpha);
            }
            var v = Voyage.I;
            if (v == null) return;
            DemoAutoplay.AimParent(v, aimTarget);
            if (drive) DemoAutoplay.SteerChild(v); else v.SetSteer(0);

            // a calm child breathing with the ring: one breath, detected just after each shrink begins
            var g = BreathGuide.I;
            if (breathe && g != null)
            {
                if (g.stage == BreathGuide.Stage.Exhale && lastStage != BreathGuide.Stage.Exhale) pulseAt = Time.time + BreathGuide.DetectLag;
                lastStage = g.stage;
                if (pulseAt > 0f && Time.time >= pulseAt) { pulseAt = -1f; BreathSignal.I.TriggerPulse(); breathTimes.Add(Time.time - t0); }
            }
        }

        IEnumerator At(float t)
        {
            while (Time.time - t0 < t) yield return null;
        }

        IEnumerator Script()
        {
            t0 = Time.time;
            var v = Voyage.I;

            // S1 0-6  title over the night sea
            fadeTarget = 0;
            yield return At(5.6f); fadeTarget = 1;

            // S2 6-14  three roles; the clinician starts
            yield return At(6f); Layout.I.Set(LayoutMode.Split); fadeTarget = 0;
            yield return At(8f); v.StartVoyage();
            yield return At(13.6f); fadeTarget = 1;

            // S3 14-32  child view: breathing with the ring, steady breathing slowly clears the mist
            yield return At(14f); Layout.I.Set(LayoutMode.ChildOnly); Place(0, 120f, 0f, false); BreathGuide.I.SetCalm(0.05f, 0.5f); fadeTarget = 0;
            yield return At(31.6f); fadeTarget = 1;

            // S4 32-44  the parent turns the light, the child steers into it
            yield return At(32f); Layout.I.Set(LayoutMode.Split); Place(0, 150f, -35f, false, 60f); fadeTarget = 0;
            yield return At(33f); aimTarget = 0;
            yield return At(35.5f); drive = true;
            yield return At(43.6f); fadeTarget = 1;

            // S5 44-54  the mist parts, the turtle appears, the boat stops to say hello (child view)
            yield return At(44f); Layout.I.Set(LayoutMode.ChildOnly); Place(0, 45f, 0f, true); v.surprises[0].progress = 0.5f; fadeTarget = 0;
            yield return At(53.6f); fadeTarget = 1;

            // S6 54-60  the clinician pauses at any time
            yield return At(54f); Layout.I.Set(LayoutMode.Split); drive = false; aimTarget = 1; fadeTarget = 0;
            yield return At(55.5f); v.TogglePause();
            yield return At(58.5f); v.TogglePause();
            yield return At(59.6f); fadeTarget = 1;

            // S7 60-68  one light, one boat, one shared discovery (child view)
            yield return At(60f);
            Layout.I.Set(LayoutMode.ChildOnly);
            ForceFound(1); ForceFound(2);
            Place(3, 42f, 0f, true); v.surprises[3].progress = 0.55f; aimTarget = 3; drive = true;
            fadeTarget = 0;
            yield return At(67.6f); fadeTarget = 1;

            // S8 68-75  the clinician ends; a summary without any score
            yield return At(68f); Layout.I.Set(LayoutMode.Split); drive = false; fadeTarget = 0;
            yield return At(68.3f); v.EndTreatment();
            yield return At(74.4f); fadeTarget = 1;
        }

        /// Off-screen discoveries for the cut: no chime for them (only on-screen finds are heard).
        static void ForceFound(int i)
        {
            AmbientAudio.I.chimesEnabled = false;
            Voyage.I.Find(Voyage.I.surprises[i]);
            AmbientAudio.I.chimesEnabled = true;
        }

        /// Scripted cut: put the boat `dist` metres short of a surprise on the lighthouse line,
        /// optionally with the light already on it, heading `yawOffset` degrees off the line.
        internal static void Place(int target, float dist, float yawOffset, bool aimed, float lateral = 0f)
        {
            var v = Voyage.I;
            var s = v.surprises[target];
            float bearing = v.BearingFromLighthouse(s.pos);
            Vector3 dir = Voyage.Dir(bearing);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            v.boatPos = s.pos - dir * dist + side * lateral;
            Vector3 to = s.pos - v.boatPos;
            v.heading = Mathf.Repeat(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg + yawOffset, 360f);
            if (aimed) v.SetBeamAngle(bearing);
            v.Unanchor();
            v.currentSpeed = v.cruiseSpeed;
            OceanView.I.SnapCamera();
            FindAnyObjectByType<MistField>()?.Scatter();
        }
    }
}

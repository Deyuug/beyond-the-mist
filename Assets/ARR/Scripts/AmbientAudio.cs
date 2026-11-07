using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// A calm, reassuring soundscape. No melody, no chimes, no reward sounds, nothing that sounds like wind: just
    ///   - a soft, warm hush (pink noise low-passed twice at ~450 Hz, so it never hisses)
    ///   - slow ocean waves: a wash that swells and recedes every 6-10 s, each from a slightly different direction,
    ///     through a non-resonant filter that opens only to ~600 Hz (a resonant sweep is what makes noise sound
    ///     like wind), with a little soft foam as it recedes
    ///   - a deep, slow swell underneath
    ///   - on each breath, a very soft low swell (~300 Hz) that rises over ~0.6 s and fades over ~1.4 s
    ///   - when the child and parent find a friend together: a gentle music-box arpeggio (C major pentatonic,
    ///     5 notes 0.18 s apart, each ringing out over ~2.8 s). It celebrates the shared discovery only;
    ///     breathing never makes a sound like this.
    /// Everything above is synthesised; left and right use independent noise so the sea feels wide.
    /// On top, quietly: soothing background music (Resources/ARR/Music), looped and faded in at the start, about
    /// 9 dB above the sea. Music: "Silver Blue Light" by Kevin MacLeod (incompetech.com), CC BY 4.0.
    /// M mutes everything.
    public class AmbientAudio : MonoBehaviour
    {
        public static AmbientAudio I;
        public bool muted;
        public float volume = 0.55f;

        int sampleRate = 44100;
        volatile float breezeTarget;
        float breeze;

        // per-channel generators
        class Chan
        {
            public uint rng;
            public float p0, p1, p2, pinkLp, pinkLp2, brown, rumbleLp, rumbleBp, breezeLow, breezeLow2;
        }
        readonly Chan[] ch = { new Chan { rng = 0x9E3779B9 }, new Chan { rng = 0x85EBCA6B } };

        class Wave
        {
            public float t, dur, gain, pan;
            public uint rng;
            public float low, band, foamHi, foamLo;
        }
        readonly List<Wave> waves = new List<Wave>();

        // discovery chime voices (shared between the main thread and the audio thread)
        struct Voice { public float freq, amp, age, decay; }
        readonly List<Voice> voices = new List<Voice>();
        readonly object voiceLock = new object();
        static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.5f };
        /// scripted recordings switch this off around off-screen discoveries
        public bool chimesEnabled = true;
        float nextWave = 0.5f, clock;
        uint schedRng = 0xC2B2AE35;
        float swellPhase;

        AudioSource src, music;
        public float musicVolume = 0.3f;

        public int SampleRate => sampleRate;
        public string MusicStatus => music == null ? "music missing"
            : $"music '{music.clip.name}' {music.clip.length:F0}s loadType={music.clip.loadType} playing={music.isPlaying} t={music.time:F1}s volume={music.volume:F2}";

        /// Recording: stop the live output and render the soundscape on demand from the main thread,
        /// a fixed number of samples per captured frame, so sound and picture stay in sync.
        public void BeginOffline() { if (src != null) src.Stop(); if (music != null) music.Stop(); }
        public void RenderOffline(float[] stereo) => OnRead(stereo);

        public void Init()
        {
            I = this;
            sampleRate = AudioSettings.outputSampleRate;
            src = gameObject.AddComponent<AudioSource>();
            src.clip = AudioClip.Create("arr_sea", sampleRate * 2, 2, sampleRate, true, OnRead);
            src.loop = true;
            src.spatialBlend = 0;
            src.volume = 1f;
            src.Play();
            var clip = Resources.Load<AudioClip>("ARR/Music/SilverBlueLight");
            if (clip != null)
            {
                music = gameObject.AddComponent<AudioSource>();
                music.clip = clip;
                music.loop = true;
                music.spatialBlend = 0f;
                music.volume = 0f;   // fades in
                music.Play();
            }
            if (Voyage.I != null) Voyage.I.SurpriseFound += OnFound;
        }

        void OnFound(Surprise s) { if (chimesEnabled) Arpeggio(5, 0.14f); }

        /// The "found a friend" sound: an ascending music-box arpeggio on the C major pentatonic.
        public void Arpeggio(int count, float amp)
        {
            lock (voiceLock)
                for (int i = 0; i < count; i++)
                    voices.Add(new Voice { freq = Pentatonic[(i * 2) % Pentatonic.Length], amp = amp, decay = 2.8f, age = -i * 0.18f });
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            if (Voyage.I != null) Voyage.I.SurpriseFound -= OnFound;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) muted = !muted;
            if (music != null)
                music.volume = Mathf.MoveTowards(music.volume, muted ? 0f : musicVolume, Time.unscaledDeltaTime * musicVolume / 3f);
            breezeTarget = BreathFlow.Exhaling ? 1f : 0f;
        }

        static float White(ref uint s)
        {
            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
            return (s / (float)uint.MaxValue) * 2f - 1f;
        }

        static float Rand01(ref uint s) => (White(ref s) + 1f) * 0.5f;

        /// Chamberlin state-variable filter step; returns low-pass, writes band-pass.
        static float Svf(float input, float f, float q, ref float low, ref float band)
        {
            low += f * band;
            float high = input - low - q * band;
            band += f * high;
            return low;
        }

        float Coef(float hz) => 2f * Mathf.Sin(Mathf.PI * Mathf.Min(hz, sampleRate / 6.5f) / sampleRate);

        float OnePole(float hz) => 1f - Mathf.Exp(-2f * Mathf.PI * hz / sampleRate);

        void OnRead(float[] data)
        {
            float dt = 1f / sampleRate;
            float vol = muted ? 0f : volume;
            float fRumble = Coef(85f);
            float aBed = OnePole(450f), aBreeze = OnePole(300f), aFoamHi = OnePole(2200f), aFoamLo = OnePole(800f);
            float breezeUp = 1f - Mathf.Exp(-1f / (0.6f * sampleRate)), breezeDown = 1f - Mathf.Exp(-1f / (1.4f * sampleRate));

            for (int i = 0; i + 1 < data.Length; i += 2)
            {
                clock += dt;
                swellPhase += dt * 0.045f;
                if (swellPhase > 1f) swellPhase -= 1f;
                breeze += (breezeTarget - breeze) * (breezeTarget > breeze ? breezeUp : breezeDown);

                // schedule the next wave
                if (clock >= nextWave)
                {
                    waves.Add(new Wave
                    {
                        dur = 6f + Rand01(ref schedRng) * 3.5f,
                        gain = 0.7f + Rand01(ref schedRng) * 0.3f,
                        pan = (Rand01(ref schedRng) * 2f - 1f) * 0.45f,
                        rng = schedRng ^ 0x27D4EB2F,
                    });
                    nextWave = clock + 5.5f + Rand01(ref schedRng) * 4.5f;
                }

                float outL = 0f, outR = 0f;
                for (int c = 0; c < 2; c++)
                {
                    var C = ch[c];
                    float w = White(ref C.rng);
                    // pink noise (Paul Kellet, economy), low-passed twice: a warm hush, no hiss
                    C.p0 = 0.99765f * C.p0 + w * 0.0990460f;
                    C.p1 = 0.96300f * C.p1 + w * 0.2965164f;
                    C.p2 = 0.57000f * C.p2 + w * 1.0526913f;
                    float pink = (C.p0 + C.p1 + C.p2 + w * 0.1848f) * 0.11f;
                    C.pinkLp += (pink - C.pinkLp) * aBed;
                    C.pinkLp2 += (C.pinkLp - C.pinkLp2) * aBed;
                    // deep swell
                    C.brown = (C.brown + 0.02f * w) / 1.02f;
                    float rumble = Svf(C.brown * 3.5f, fRumble, 1.2f, ref C.rumbleLp, ref C.rumbleBp);
                    float swell = 0.6f + 0.4f * Mathf.Sin(swellPhase * Mathf.PI * 2f + c * 0.7f);
                    // the breath: a soft, low swell of the sea (no band-passed whoosh)
                    C.breezeLow += (w - C.breezeLow) * aBreeze;
                    C.breezeLow2 += (C.breezeLow - C.breezeLow2) * aBreeze;

                    float s = C.pinkLp2 * 0.10f + rumble * 0.035f * swell + C.breezeLow2 * 0.16f * breeze;
                    if (c == 0) outL += s; else outR += s;
                }

                // waves: a filtered wash whose brightness follows the swell, then a soft fizz as it recedes
                for (int k = waves.Count - 1; k >= 0; k--)
                {
                    var wv = waves[k];
                    wv.t += dt;
                    float x = wv.t / wv.dur;
                    if (x >= 1f) { waves.RemoveAt(k); continue; }
                    float env = x < 0.38f ? Mathf.Pow(Mathf.Sin(x / 0.38f * Mathf.PI * 0.5f), 2f) : Mathf.Pow(1f - (x - 0.38f) / 0.62f, 1.7f);
                    float n = White(ref wv.rng);
                    float wash = Svf(n, Coef(120f + 480f * env), 1.9f, ref wv.low, ref wv.band);
                    wv.foamHi += (n - wv.foamHi) * aFoamHi;
                    wv.foamLo += (n - wv.foamLo) * aFoamLo;
                    float foam = (wv.foamHi - wv.foamLo) * (x > 0.38f ? env * env : 0f);   // ~0.8–2.2 kHz, only as it recedes
                    float v = (wash * 0.45f + foam * 0.03f) * env * wv.gain;
                    float a = (wv.pan + 1f) * Mathf.PI * 0.25f;
                    outL += v * Mathf.Cos(a);
                    outR += v * Mathf.Sin(a);
                }

                // discovery chime: sine + a slightly detuned octave, soft 5 ms attack, long ring
                lock (voiceLock)
                {
                    for (int k = voices.Count - 1; k >= 0; k--)
                    {
                        var vc = voices[k];
                        vc.age += dt;
                        if (vc.age > 0f)
                        {
                            float env = Mathf.Exp(-vc.age * 3f / vc.decay) * Mathf.Clamp01(vc.age * 200f);
                            float ph = vc.age * vc.freq * Mathf.PI * 2f;
                            float tone = (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2.01f)) * vc.amp * env;
                            outL += tone;
                            outR += tone;
                        }
                        if (vc.age > vc.decay * 2f) voices.RemoveAt(k);
                        else voices[k] = vc;
                    }
                }

                // gentle soft clip, then master volume
                data[i] = vol * (float)System.Math.Tanh(outL * 1.2f) / 1.2f;
                data[i + 1] = vol * (float)System.Math.Tanh(outR * 1.2f) / 1.2f;
            }
        }
    }
}

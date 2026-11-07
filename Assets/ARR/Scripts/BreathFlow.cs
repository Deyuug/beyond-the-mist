using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// The air moved by one exhale: not a single burst, but a short, sustained forward stream.
    ///
    /// While an exhale lasts (ExhaleSeconds), the bow keeps releasing small puffs of air every PuffInterval,
    /// with an ease-in / sustain / ease-out envelope. The overlapping puffs form a continuous jet that reaches
    /// forward, then decelerates and spreads after the breath ends.
    ///
    /// Each puff is an incompressible axisymmetric blob (s along the axis, rho from the axis):
    ///   axial   u_s   = U f(s) g(rho),              f = exp(-(s - s_c)^2 / L^2),  g = exp(-rho^2 / w^2)
    ///   radial  u_rho = -U f'(s) w^2 / (2 rho) (1 - g)
    /// which satisfies div u = 0: inside the stream the air moves forward, at the stream's head mist is pushed
    /// ahead and aside, and along its sides air is drawn back in, so the mist closes softly afterwards.
    /// Duration and strength are the same for every exhale: breathing longer or harder is never rewarded.
    /// Each breath is only a small, identical ripple; the mist as a whole follows BreathGuide.Calm (a slow measure
    /// of how steadily the child breathes with the guide), so breathing more often does not clear it faster.
    public static class BreathFlow
    {
        public const int MaxPuffs = 20;

        struct Puff { public Vector3 origin, axis; public float born, amp, carry, clear; }
        static readonly List<Puff> puffs = new List<Puff>();
        static readonly Vector4[] origins = new Vector4[MaxPuffs];
        static readonly Vector4[] axes = new Vector4[MaxPuffs];
        static readonly Vector4[] lengths = new Vector4[MaxPuffs];

        // ---- exhale
        public const float ExhaleSeconds = 1.2f;      // how long the bow releases air (a short, natural out-breath)
        public const float PuffInterval = 0.22f;
        static float exhaleStart = -100f, prevExhaleStart = -100f, nextPuff;
        static float exhaleClear = 1f;   // how much this exhale's air may thin the mist at the bow (0..1)
        /// the local channel at the bow only thins the mist (never a fully clear hole)
        const float TubeClear = 0.5f;
        static int lastTickFrame = -1;

        // ---- one puff (fixed for every breath)
        public const float U0 = 3.0f;        // peak air speed of one puff, m/s (a gentle ripple, not a gust)
        public const float Decay = 2.4f;     // strength e-folding time, s
        public const float Travel = 11f;     // exhale speed relative to the boat, m/s
        public const float Brake = 3.0f;     // deceleration time, s
        public const float Width0 = 1.5f, WidthGrow = 0.9f;
        public const float Length0 = 2.4f, LengthGrow = 1.1f;
        public const float Life = 10f;

        public static bool Exhaling => Time.time - exhaleStart < ExhaleSeconds;

        /// Boat-centred breathing of the air, in m/s: positive = flowing outward (for ~2 s after an accepted
        /// out-breath), negative = drawn gently inward (while the guide ring opens for the in-breath).
        /// The same for every breath: never stronger for a harder or longer one.
        public static float RadialSpeed => 6.0f * OutBreath01 - 3.5f * InBreath01;
        public static float OutBreath01
        {
            get { float t = Time.time - exhaleStart; return t >= 0f && t < 2f ? Mathf.Sin(Mathf.PI * t / 2f) : 0f; }
        }
        public static float InBreath01
        {
            get
            {
                var g = BreathGuide.I;
                return g != null && g.on && g.stage == BreathGuide.Stage.Inhale ? Mathf.Sin(Mathf.PI * g.stageProgress) : 0f;
            }
        }

        /// 0..1 strength of the exhale right now (ease in, sustain, ease out); 0 when not exhaling.
        public static float ExhaleEnvelope => Exhaling ? Envelope(Time.time - exhaleStart) : 0f;

        /// How much a puff has cleared the layered mist: opens over half a second, closes slowly (no abrupt return).
        public static float Clear(float age) =>
            Mathf.SmoothStep(0f, 1f, age / 0.5f) * Mathf.Exp(-Mathf.Max(0f, age - 0.8f) / 2.2f) * Mathf.Clamp01((Life - age) / 2.5f);

        static float Envelope(float t)
        {
            float x = Mathf.Clamp01(t / ExhaleSeconds);
            // ease in, sustain, ease out; clamp first: sin(pi) is a tiny negative float and pow() of it is NaN
            return Mathf.Pow(Mathf.Max(0f, Mathf.Sin(x * Mathf.PI)), 0.6f);
        }

        public static void Reset() { puffs.Clear(); exhaleStart = prevExhaleStart = -100f; }

        /// A breath event starts one exhale. clear (0..1) scales how much its air thins the mist at the bow for a
        /// moment: a breath in rhythm 1, one off the beat less. The starlight response is the same for every breath.
        public static void BeginExhale(float clear = 1f)
        {
            prevExhaleStart = exhaleStart;
            exhaleStart = Time.time;
            exhaleClear = Mathf.Clamp01(clear);
            nextPuff = Time.time;
        }

        /// Called every frame with the bow position, heading and boat speed: keeps the stream flowing while exhaling.
        /// The air leaves the boat with the boat's own speed plus the exhale, then slows to the still air,
        /// so the stream reaches out ahead of the bow instead of being overtaken by the boat.
        public static void Tick(Vector3 bow, Vector3 forward, float boatSpeed = 0f)
        {
            if (lastTickFrame == Time.frameCount) return;
            lastTickFrame = Time.frameCount;
            float now = Time.time;
            puffs.RemoveAll(p => now - p.born > Life);
            while (Exhaling && now >= nextPuff)
            {
                float amp = Envelope(nextPuff - exhaleStart + PuffInterval * 0.5f);
                if (puffs.Count >= MaxPuffs) puffs.RemoveAt(0);
                forward.y = 0;
                puffs.Add(new Puff { origin = bow, axis = forward.normalized, born = nextPuff, amp = amp, carry = boatSpeed, clear = exhaleClear });
                nextPuff += PuffInterval;
            }
        }

        /// Current state of a puff: centre distance along the axis, strength, width, length.
        static void State(Puff p, float age, out float sc, out float u, out float w, out float l)
        {
            sc = 1.0f + (Travel + p.carry) * Brake * (1f - Mathf.Exp(-age / Brake));
            u = U0 * p.amp * Mathf.Exp(-age / Decay) * Mathf.Clamp01(age / 0.2f);
            w = Width0 + WidthGrow * age;
            l = Length0 + LengthGrow * age;
        }

        public static Vector3 Velocity(Vector3 pos)
        {
            Vector3 v = Vector3.zero;
            float now = Time.time;
            foreach (var pf in puffs)
            {
                float age = now - pf.born;
                if (age < 0 || age > Life) continue;
                State(pf, age, out float sc, out float u, out float w, out float l);
                Vector3 rel = pos - pf.origin;
                float s = Vector3.Dot(rel, pf.axis);
                Vector3 radial = rel - pf.axis * s;
                float rho = radial.magnitude;
                float x = (s - sc) / l;
                float f = Mathf.Exp(-x * x);
                float df = -2f * x / l * f;
                float g = Mathf.Exp(-rho * rho / (w * w));
                v += pf.axis * (u * f * g);
                if (rho > 1e-3f) v += radial / rho * (-u * df * w * w / (2f * rho) * (1f - g));
            }
            return v;
        }

        /// _PuffOrigin = (origin.xyz, centre distance), _PuffAxis = (axis.x, axis.z, strength, width),
        /// _PuffLength = (length, clear amount)
        public static void Push(ComputeShader cs)
        {
            Fill();
            cs.SetVectorArray("_PuffOrigin", origins);
            cs.SetVectorArray("_PuffAxis", axes);
            cs.SetVectorArray("_PuffLength", lengths);
        }

        /// Same data for materials (the layered mist shader).
        public static void PushGlobal()
        {
            Fill();
            Shader.SetGlobalVectorArray("_PuffOrigin", origins);
            Shader.SetGlobalVectorArray("_PuffAxis", axes);
            Shader.SetGlobalVectorArray("_PuffLength", lengths);
        }

        static void Fill()
        {
            float now = Time.time;
            for (int i = 0; i < MaxPuffs; i++)
            {
                if (i < puffs.Count && now - puffs[i].born >= 0 && now - puffs[i].born < Life)
                {
                    var p = puffs[i];
                    float age = now - p.born;
                    State(p, age, out float sc, out float u, out float w, out float l);
                    origins[i] = new Vector4(p.origin.x, p.origin.y, p.origin.z, sc);
                    axes[i] = new Vector4(p.axis.x, p.axis.z, u, w);
                    lengths[i] = new Vector4(l, Clear(age) * p.amp * TubeClear * p.clear, 0, 0);
                }
                else
                {
                    origins[i] = Vector4.zero;
                    axes[i] = new Vector4(0, 1, 0, 1);
                    lengths[i] = new Vector4(1, 0, 0, 0);
                }
            }
        }

        /// Overall "how much air is moving" 0..1, for gentle secondary effects.
        public static float Activity
        {
            get
            {
                float a = 0, now = Time.time;
                foreach (var p in puffs) a = Mathf.Max(a, p.amp * Mathf.Exp(-(now - p.born) / Decay));
                return a;
            }
        }
    }
}

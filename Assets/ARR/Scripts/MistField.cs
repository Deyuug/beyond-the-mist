using UnityEngine;

namespace ARR
{
    /// Low, soft banks of violet sea mist drifting around the boat.
    /// A breath's puff of air (BreathFlow) carries nearby banks forward and aside; they have inertia,
    /// so they glide away, swirl a little and drift back into place over several seconds.
    public class MistField : MonoBehaviour
    {
        const int Count = 70;

        struct Bank { public Vector3 pos, offset, offVel; public float size, alpha, rot, spin, fade; }

        Bank[] banks;
        ParticleSystem fogPS;
        ParticleSystem.Particle[] parts;

        public void Init(Transform root)
        {
            banks = new Bank[Count];
            parts = new ParticleSystem.Particle[Count];
            var mat = ArrArt.Mat("SoftAlpha", new Color(0.50f, 0.46f, 0.74f, 1f), ArrArt.Cloud);
            fogPS = MakeSystem(root, "MistBanks", mat, Count);
            var r = fogPS.GetComponent<ParticleSystemRenderer>();
            r.sortMode = ParticleSystemSortMode.Distance;
            r.maxParticleSize = 4f;
            Scatter();
        }

        /// Re-scatter all mist around the boat (also used after a scripted cut in the promo).
        public void Scatter()
        {
            var v = Voyage.I;
            for (int i = 0; i < Count; i++) Spawn(ref banks[i], v.boatPos, v.Forward, true);
        }

        public static ParticleSystem MakeSystem(Transform parent, string name, Material mat, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = 100000f;
            var em = ps.emission;
            em.enabled = false;
            var sh = ps.shape;
            sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        void Spawn(ref Bank b, Vector3 boat, Vector3 fwd, bool initial)
        {
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            if (initial)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float d = Mathf.Lerp(80f, 210f, Mathf.Sqrt(Random.value));
                b.pos = boat + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * d;
            }
            else b.pos = boat + fwd * Random.Range(140f, 210f) + side * Random.Range(-150f, 150f);
            b.pos.y = Random.Range(0.5f, 4.5f);
            b.offset = b.offVel = Vector3.zero;
            b.size = Random.Range(18f, 42f);
            b.alpha = Random.Range(0.08f, 0.16f);
            b.rot = Random.Range(0f, 360f);
            b.spin = Random.Range(-2f, 2f);
            b.fade = initial ? 1f : 0f;
        }

        void Update()
        {
            var v = Voyage.I;
            if (v == null) return;
            float dt = Time.deltaTime;
            Vector3 boat = v.boatPos, fwd = v.Forward;
            int active = Mathf.RoundToInt(Count * Mathf.Lerp(0.35f, 1f, v.MistLevel));
            Vector3 drift = new Vector3(0.4f, 0, 0.2f);
            for (int i = 0; i < Count; i++)
            {
                ref Bank b = ref banks[i];
                b.pos += drift * dt;
                // carried by the breath's air, then eased back (spring + damping, no snapping)
                Vector3 air = BreathFlow.Velocity(b.pos + b.offset);
                b.offVel += (air - b.offVel) * (1f - Mathf.Exp(-dt * 1.6f));
                b.offVel += -b.offset * 0.3f * dt;
                b.offset += b.offVel * dt;
                b.offset.y = Mathf.Max(b.offset.y, -b.pos.y + 0.3f);
                b.rot += (b.spin + b.offVel.magnitude * 4f) * dt;
                Vector3 rel = b.pos - boat; rel.y = 0;
                if (rel.magnitude > 220f || Vector3.Dot(rel, fwd) < -80f) Spawn(ref b, boat, fwd, false);
                // keep the space right around the boat clear so the heading is always readable
                float targetFade = i < active && rel.magnitude > 60f ? 1f : 0f;
                b.fade = Mathf.MoveTowards(b.fade, targetFade, dt / 3f);
                parts[i].position = b.pos + b.offset;
                parts[i].startSize = b.size;
                parts[i].rotation = b.rot;
                parts[i].startColor = new Color(1, 1, 1, b.alpha * b.fade);
                parts[i].remainingLifetime = 100000f;
                parts[i].startLifetime = 100000f;
                parts[i].velocity = Vector3.zero;
            }
            fogPS.SetParticles(parts, Count);
        }
    }
}

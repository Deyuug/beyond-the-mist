using UnityEngine;

namespace ARR
{
    /// The one lighthouse. Its beam is the parent's voice in the child's world: the parent turns it on the tablet,
    /// and the same golden beam (plus a golden path on the water) moves in the child's view.
    /// The lantern, halo and beam ignore most of the fog, so the light can be found from anywhere on the sea.
    public class LighthouseView : MonoBehaviour
    {
        public static LighthouseView I;
        public float beamIntensity = 1f;

        Transform beamRoot;
        Transform[] beams;
        Material[] beamMats;
        Transform halo, haloFar;
        static readonly float[] BaseAlpha = { 0.22f, 0.30f, 0.55f };

        // golden birds that fly out along the beam (they swing with it, so they also point the way)
        class Bird { public Transform t, wl, wr; public float along, lateral, height, speed, phase; }
        readonly System.Collections.Generic.List<Bird> birds = new System.Collections.Generic.List<Bird>();
        float shownAngle;

        public void Init(Transform root)
        {
            I = this;
            var v = Voyage.I;
            var rock = ArrArt.Lit(new Color(0.24f, 0.22f, 0.38f), new Color(0.02f, 0.02f, 0.05f));
            var rock2 = ArrArt.Lit(new Color(0.32f, 0.27f, 0.46f), new Color(0.03f, 0.02f, 0.06f));
            var grass = ArrArt.Lit(new Color(0.20f, 0.36f, 0.42f), new Color(0.02f, 0.05f, 0.06f));
            var grass2 = ArrArt.Lit(new Color(0.28f, 0.48f, 0.48f), new Color(0.03f, 0.06f, 0.07f));
            var white = ArrArt.Lit(new Color(0.95f, 0.92f, 0.96f), new Color(0.22f, 0.21f, 0.26f));
            var stripe = ArrArt.Lit(new Color(0.86f, 0.42f, 0.52f), new Color(0.16f, 0.06f, 0.09f));
            var dark = ArrArt.Lit(new Color(0.16f, 0.16f, 0.30f), new Color(0.03f, 0.03f, 0.06f));
            var glass = ArrArt.Lit(new Color(1f, 0.92f, 0.7f), new Color(1.3f, 1.0f, 0.55f));
            var warm = ArrArt.Lit(new Color(1f, 0.85f, 0.55f), new Color(1.0f, 0.7f, 0.35f));
            var cream = ArrArt.Lit(new Color(0.94f, 0.88f, 0.80f), new Color(0.18f, 0.16f, 0.14f));
            var roofRed = ArrArt.Lit(new Color(0.62f, 0.30f, 0.40f), new Color(0.10f, 0.04f, 0.06f));

            var r = new GameObject("Lighthouse").transform;
            r.SetParent(root, false);
            r.position = v.lighthouse;

            // ---- island: layered rocks, a grassy cap, boulders at the waterline, grass tufts
            OceanView.Blob(r, new Vector3(0, -1.6f, 0), new Vector3(30f, 7f, 26f), rock, 11, 5, 1.3f);
            OceanView.Blob(r, new Vector3(-1.5f, 0.6f, 1f), new Vector3(20f, 4f, 17f), grass, 10, 4, 4.1f);
            OceanView.Blob(r, new Vector3(-3f, 1.5f, 2.5f), new Vector3(11f, 3f, 9f), grass2, 9, 4, 6.4f);
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2 + 0.3f;
                float d = 12.5f + (i % 3) * 1.6f;
                OceanView.Blob(r, new Vector3(Mathf.Cos(a) * d, -0.4f + (i % 2) * 0.4f, Mathf.Sin(a) * d * 0.85f),
                    new Vector3(3.2f + i % 3, 2.2f + (i % 2), 2.8f + (i % 4) * 0.5f), i % 2 == 0 ? rock : rock2, 6, 4, i * 1.7f);
            }
            for (int i = 0; i < 26; i++)
            {
                float a = i * 2.39996f, d = 3.5f + (i * 37 % 7);
                var t = Models.Part(r, "Tuft", Models.Cone(4), i % 2 == 0 ? grass : grass2,
                    new Vector3(Mathf.Cos(a) * d, 1.9f + Mathf.Sin(i) * 0.3f, Mathf.Sin(a) * d * 0.8f), Quaternion.Euler(Mathf.Sin(i * 3f) * 12f, i * 40, 0),
                    new Vector3(0.35f, 0.8f + (i % 3) * 0.25f, 0.35f));
            }

            // ---- keeper's cottage with a warm window
            var cottage = Models.Node(r, "Cottage", new Vector3(-6.5f, 2.2f, 3.2f), Quaternion.Euler(0, 25, 0));
            var walls = new MeshBuilder();
            walls.Box(new Vector3(0, 1.0f, 0), new Vector3(3.2f, 2.0f, 2.6f));
            Models.Part(cottage, "Walls", walls, cream);
            Models.Part(cottage, "Roof", Models.Roof(3.8f, 1.5f, 3.2f), roofRed, new Vector3(0, 2.0f, 0), Quaternion.Euler(0, 90, 0), Vector3.one);
            var win = new MeshBuilder();
            win.Box(new Vector3(0.7f, 1.05f, 1.31f), new Vector3(0.6f, 0.6f, 0.05f));
            win.Box(new Vector3(-1.61f, 1.05f, 0.2f), new Vector3(0.05f, 0.6f, 0.6f));
            Models.Part(cottage, "Windows", win, warm);
            var door = new MeshBuilder();
            door.Box(new Vector3(-0.6f, 0.6f, 1.31f), new Vector3(0.6f, 1.2f, 0.05f));
            Models.Part(cottage, "Door", door, dark);
            Models.Tube(cottage, new Vector3(1.0f, 2.0f, -0.6f), new Vector3(1.0f, 3.3f, -0.6f), 0.22f, 0.2f, rock2, 5);
            OceanView.MakeGlow(cottage, new Vector3(0.7f, 1.05f, 1.6f), 2.2f, new Color(1f, 0.75f, 0.4f, 0.5f), out _, false, 0.4f);

            // ---- tower: tapered, with stripes, a door and little windows
            const float baseY = 1.9f, topY = 14.6f;
            var tower = Models.Node(r, "Tower", Vector3.zero);
            Models.Part(tower, "Plinth", Models.Lathe("lh_plinth", new[] { 0f, 2.6f, 2.6f, 2.2f, 0f }, new[] { 0f, 0f, 0.5f, 0.7f, 0.7f }, 10),
                rock2, new Vector3(0, baseY - 0.5f, 0), Quaternion.identity, Vector3.one);
            Models.Part(tower, "Body", Models.Lathe("lh_body", new[] { 2.05f, 1.95f, 1.62f, 1.42f }, new[] { 0f, 1.5f, 9.5f, topY - baseY }, 10),
                white, new Vector3(0, baseY, 0), Quaternion.identity, Vector3.one);
            float R(float y) => Mathf.Lerp(2.0f, 1.42f, Mathf.InverseLerp(baseY, topY, y)) + 0.04f;
            foreach (float y in new[] { 4.2f, 8.4f, 12.2f })
                Models.Part(tower, "Stripe", Models.Lathe($"lh_stripe_{y}", new[] { R(y), R(y + 1.3f) }, new[] { 0f, 1.3f }, 10, 360f, true),
                    stripe, new Vector3(0, y, 0), Quaternion.identity, Vector3.one);
            var deco = new MeshBuilder();
            deco.Box(new Vector3(0, baseY + 0.75f, -1.98f), new Vector3(0.9f, 1.5f, 0.12f));
            Models.Part(tower, "Door", deco, dark);
            var windows = new MeshBuilder();
            foreach (float y in new[] { 6.3f, 10.4f })
                foreach (float a in new[] { 40f, 220f })
                {
                    float rr = R(y) - 0.02f;
                    Vector3 p = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * rr, y, Mathf.Cos(a * Mathf.Deg2Rad) * rr);
                    windows.Box(p, new Vector3(0.38f, 0.6f, 0.38f), Quaternion.Euler(0, a, 0));
                }
            Models.Part(tower, "Windows", windows, warm);

            // ---- gallery deck with railing
            Models.Part(tower, "Gallery", Models.Lathe("lh_gallery", new[] { 0f, 2.25f, 2.25f, 0f }, new[] { 0f, 0f, 0.28f, 0.28f }, 14),
                dark, new Vector3(0, topY, 0), Quaternion.identity, Vector3.one);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2;
                Vector3 p = new Vector3(Mathf.Cos(a) * 2.1f, topY + 0.28f, Mathf.Sin(a) * 2.1f);
                Models.Tube(tower, p, p + Vector3.up * 0.75f, 0.035f, 0.035f, dark, 4);
            }
            Models.Part(tower, "Rail", Models.Ring(2.14f, 0.08f, 0.07f, 16), dark, new Vector3(0, topY + 1.0f, 0), Quaternion.identity, Vector3.one);

            // ---- lantern room: glowing glass, frame, roof, finial
            float lampY = Voyage.LighthouseHeight;
            Models.Part(tower, "Glass", Models.Lathe("lh_glass", new[] { 0f, 1.12f, 1.12f, 0f }, new[] { 0f, 0f, 1.7f, 1.7f }, 8),
                glass, new Vector3(0, lampY - 0.85f, 0), Quaternion.identity, Vector3.one);
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) / 8f * Mathf.PI * 2;
                Vector3 p = new Vector3(Mathf.Cos(a) * 1.15f, lampY - 0.9f, Mathf.Sin(a) * 1.15f);
                Models.Tube(tower, p, p + Vector3.up * 1.8f, 0.045f, 0.045f, dark, 4);
            }
            Models.Part(tower, "Roof", Models.Lathe("lh_roof", new[] { 0f, 1.55f, 1.4f, 0.75f, 0.25f, 0f }, new[] { 0f, 0f, 0.2f, 0.8f, 1.3f, 1.45f }, 10),
                roofRed, new Vector3(0, lampY + 0.85f, 0), Quaternion.identity, Vector3.one);
            Models.Part(tower, "Finial", Models.Gem(), dark, new Vector3(0, lampY + 2.25f, 0), Quaternion.identity, Vector3.one * 0.5f);
            Models.Tube(tower, new Vector3(0, lampY + 2.6f, 0), new Vector3(0, lampY + 3.4f, 0), 0.03f, 0.02f, dark, 4);

            // ---- the light itself: a bright core, a near halo and a far halo that shines through the mist
            halo = OceanView.MakeGlow(r, new Vector3(0, lampY, 0), 14f, new Color(1f, 0.85f, 0.55f, 0.95f), out _, false, 0.05f);
            haloFar = OceanView.MakeGlow(r, new Vector3(0, lampY, 0), 46f, new Color(1f, 0.78f, 0.45f, 0.35f), out _, false, 0.1f);

            // ---- beam: a wide soft shaft, a mid shaft and a bright core, each facing the camera around its own axis
            beamRoot = new GameObject("Beam").transform;
            beamRoot.SetParent(r, false);
            beamRoot.localPosition = new Vector3(0, lampY, 0);
            float[] near = { 3.4f, 2.0f, 0.8f };
            float[] far = { 44f, 24f, 9f };
            float[] alpha = BaseAlpha;
            beams = new Transform[3];
            beamMats = new Material[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Shaft" + i);
                go.transform.SetParent(beamRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = ArrArt.BeamQuad(Voyage.BeamLength, near[i], far[i]);
                var mr = go.AddComponent<MeshRenderer>();
                beamMats[i] = ArrArt.Mat("Additive", new Color(1f, 0.82f, 0.49f, alpha[i]), ArrArt.Beam);
                beamMats[i].SetFloat("_FogFactor", 0.18f);
                mr.sharedMaterial = beamMats[i];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                beams[i] = go.transform;
            }
            shownAngle = v.beamAngle;
            BuildBirds(r);
        }

        void BuildBirds(Transform parent)
        {
            var mat = ArrArt.Lit(ArrArt.Gold, new Color(1.0f, 0.72f, 0.30f));
            var wing = new MeshBuilder();
            // one swept-back wing, built along +X from the shoulder
            wing.Tri(Vector3.zero, new Vector3(0.55f, 0.02f, -0.12f), new Vector3(0.05f, 0, -0.38f));
            wing.Tri(Vector3.zero, new Vector3(0.05f, 0, -0.38f), new Vector3(0.55f, 0.02f, -0.12f));
            wing.Tri(new Vector3(0.55f, 0.02f, -0.12f), new Vector3(1.1f, 0.06f, -0.42f), new Vector3(0.45f, 0, -0.3f));
            wing.Tri(new Vector3(0.55f, 0.02f, -0.12f), new Vector3(0.45f, 0, -0.3f), new Vector3(1.1f, 0.06f, -0.42f));
            var wingMesh = wing.Build("bird_wing");
            var body = new MeshBuilder();
            body.Box(new Vector3(0, 0, -0.1f), new Vector3(0.14f, 0.1f, 0.6f));
            var bodyMesh = body.Build("bird_body");
            var rnd = new System.Random(5);
            for (int i = 0; i < 34; i++)
            {
                var b = new Bird
                {
                    t = Models.Node(parent, "Bird", Vector3.zero),
                    along = (float)rnd.NextDouble(),
                    lateral = (float)rnd.NextDouble() * 2f - 1f,
                    height = 1f + (float)rnd.NextDouble() * 5f,
                    speed = 5f + (float)rnd.NextDouble() * 3f,
                    phase = (float)rnd.NextDouble() * 6.3f,
                };
                float sc = 0.9f + (float)rnd.NextDouble() * 0.8f;
                b.t.localScale = Vector3.one * sc;
                Models.Part(b.t, "Body", bodyMesh, mat, Vector3.zero, Quaternion.identity, Vector3.one);
                b.wr = Models.Node(b.t, "WingR", new Vector3(0.06f, 0, 0));
                Models.Part(b.wr, "W", wingMesh, mat, Vector3.zero, Quaternion.identity, Vector3.one);
                b.wl = Models.Node(b.t, "WingL", new Vector3(-0.06f, 0, 0));
                Models.Part(b.wl, "W", wingMesh, mat, Vector3.zero, Quaternion.identity, new Vector3(-1, 1, 1));
                birds.Add(b);
            }
        }

        void UpdateBirds(Vector3 lamp, Vector3 d, float dt)
        {
            Vector3 flat = new Vector3(d.x, 0, d.z).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, flat);
            float len = Voyage.BeamLength * 0.85f;
            float time = Time.time;
            foreach (var b in birds)
            {
                b.along += b.speed * dt / len;
                if (b.along > 1f) b.along -= 1f;
                float dist = 8f + b.along * len;
                float halfWidth = 1.2f + dist * 0.07f;
                Vector3 onBeam = lamp + d * dist;
                Vector3 p = onBeam + side * (b.lateral * halfWidth) + Vector3.up * (b.height * Mathf.Lerp(0.4f, 1f, b.along))
                            + Vector3.up * Mathf.Sin(time * 1.3f + b.phase) * 0.4f;
                p.y = Mathf.Max(p.y, 1.5f);
                b.t.position = p;
                b.t.rotation = Quaternion.LookRotation(flat + side * Mathf.Sin(time * 0.7f + b.phase) * 0.15f, Vector3.up);
                float fade = Mathf.Clamp01(b.along / 0.06f) * Mathf.Clamp01((1f - b.along) / 0.1f);
                var cam = OceanView.I != null ? OceanView.I.cam : null;
                if (cam != null) fade *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 30f, Vector3.Distance(cam.transform.position, p)));
                b.t.localScale = Vector3.one * Mathf.Max(0.001f, fade) * (0.9f + 0.4f * Mathf.Repeat(b.phase, 1f));
                float flap = Mathf.Sin(time * 7.5f + b.phase * 3f) * 32f;
                b.wr.localRotation = Quaternion.Euler(0, 0, flap);
                b.wl.localRotation = Quaternion.Euler(0, 0, -flap);
            }
        }

        void OnDestroy() { if (I == this) I = null; }

        void LateUpdate()
        {
            var v = Voyage.I;
            var cam = OceanView.I != null ? OceanView.I.cam : null;
            if (v == null || cam == null) return;
            float dt = Time.deltaTime;
            // soft, never flickering: the visible beam eases toward the parent's angle
            shownAngle = Mathf.LerpAngle(shownAngle, v.beamAngle, 1f - Mathf.Exp(-dt * 6f));
            float pulse = 0.94f + 0.06f * Mathf.Sin(Time.time * 1.3f);
            beamIntensity = pulse;
            halo.localScale = Vector3.one * 14f * pulse;
            haloFar.localScale = Vector3.one * 46f * pulse;

            float tilt = Mathf.Atan2(Voyage.LighthouseHeight - 0.5f, Voyage.BeamLength) * Mathf.Rad2Deg;
            Vector3 d = Quaternion.Euler(tilt, shownAngle, 0) * Vector3.forward;
            // the mist slices scatter this light (see Mist.shader)
            Vector3 lamp = beamRoot.position;
            Shader.SetGlobalVector("_ArrLamp", new Vector4(lamp.x, lamp.y, lamp.z, pulse));
            Shader.SetGlobalVector("_ArrBeam3D", new Vector4(d.x, d.y, d.z, Voyage.BeamLength));
            UpdateBirds(lamp, d, dt);
            float align = Mathf.Abs(Vector3.Dot((cam.transform.position - lamp).normalized, d));
            float[] lo = { 0.965f, 0.975f, 0.985f }, hi = { 0.992f, 0.996f, 0.999f };
            for (int i = 0; i < beams.Length; i++)
            {
                float a = BaseAlpha[i] * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lo[i], hi[i], align)));
                beamMats[i].SetColor("_Color", new Color(1f, 0.82f, 0.49f, a));
                Vector3 mid = beamRoot.position + d * 60f;
                Vector3 toCam = (cam.transform.position - mid).normalized;
                Vector3 side = Vector3.Cross(d, toCam);
                if (side.sqrMagnitude < 1e-4f) continue;
                side.Normalize();
                beams[i].rotation = Quaternion.LookRotation(d, Vector3.Cross(d, side));
            }
        }

        /// The angle actually drawn (eased), used by the water path shader and the chart so all stay in sync.
        public float ShownAngle => shownAngle;
    }
}

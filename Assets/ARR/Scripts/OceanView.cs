using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// The child's large-screen view: a deep, dreamy night sea and the little white sailboat with its yellow sail,
    /// seen from just behind so the heading and the lighthouse light always stay in the centre of the view.
    public class OceanView : MonoBehaviour
    {
        public static OceanView I;

        public Camera cam;
        public Transform boat;
        Transform ocean, sky, rig, pennant, boatLamp;

        float smoothPitch, smoothRoll, smoothHeave, camYaw;
        bool camPlaced;
        float anchorView;
        float mist = 1f;
        public float clear01;          // 0 misty night .. 1 the whole sea explored
        public float clarity;          // 0 thick mist .. 1 clear: follows the breathing guide's calm level (in steps)
        public Vector4 glowPos;
        public Color glowColor = ArrArt.Gold;

        public void Init(Transform root)
        {
            I = this;
            BuildCamera(root);
            BuildSky(root);
            BuildOcean(root);
            BuildBoat(root);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            camYaw = Voyage.I.heading;
            UpdateGlobals();
        }

        void OnDestroy() { if (I == this) I = null; }

        // ------------------------------------------------------------------ build
        void BuildCamera(Transform root)
        {
            var go = new GameObject("ChildCamera");
            go.transform.SetParent(root, false);
            cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ArrArt.Horizon;
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1400f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            go.AddComponent<AudioListener>();
            go.AddComponent<StylizeFX>();
        }

        void BuildSky(Transform root)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SkyDome";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.localScale = Vector3.one * 1800f;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(ArrArt.Shader("Sky"));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sky = go.transform;
        }

        void BuildOcean(Transform root)
        {
            const int n = 180;
            const float extent = 340f;
            var verts = new Vector3[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)(n - 1) * 2 - 1, v = j / (float)(n - 1) * 2 - 1;
                    float x = Mathf.Sign(u) * Mathf.Pow(Mathf.Abs(u), 1.7f) * extent;
                    float z = Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1.7f) * extent;
                    verts[j * n + i] = new Vector3(x, 0, z);
                }
            var tris = new int[(n - 1) * (n - 1) * 6];
            int t = 0;
            for (int j = 0; j < n - 1; j++)
                for (int i = 0; i < n - 1; i++)
                {
                    int a = j * n + i;
                    tris[t++] = a; tris[t++] = a + n; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = a + n; tris[t++] = a + n + 1;
                }
            var mesh = new Mesh { name = "ocean" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(extent * 2, 20, extent * 2));

            var go = new GameObject("Ocean");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var m = new Material(ArrArt.Shader("Ocean"));
            m.SetColor("_DeepColor", new Color(0.012f, 0.040f, 0.15f));
            m.SetColor("_ShallowColor", new Color(0.045f, 0.12f, 0.34f));
            m.SetColor("_FoamColor", new Color(0.40f, 0.48f, 0.82f));
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ocean = go.transform;
        }

        // ------------------------------------------------------------------ the sailboat
        void BuildBoat(Transform root)
        {
            boat = new GameObject("Sailboat").transform;
            boat.SetParent(root, false);

            var white = ArrArt.Lit(new Color(0.97f, 0.96f, 0.98f), new Color(0.30f, 0.30f, 0.38f));
            var stripe = ArrArt.Lit(new Color(0.30f, 0.55f, 0.95f), new Color(0.06f, 0.12f, 0.26f));
            var bottom = ArrArt.Lit(new Color(0.16f, 0.22f, 0.50f), new Color(0.03f, 0.05f, 0.12f));
            var wood = ArrArt.Lit(new Color(0.78f, 0.58f, 0.40f), new Color(0.16f, 0.10f, 0.05f));
            var woodDark = ArrArt.Lit(new Color(0.48f, 0.33f, 0.22f), new Color(0.08f, 0.05f, 0.03f));
            var cream = ArrArt.Lit(new Color(0.96f, 0.92f, 0.84f), new Color(0.26f, 0.24f, 0.22f));
            var metal = ArrArt.Lit(new Color(0.80f, 0.82f, 0.88f), new Color(0.18f, 0.18f, 0.22f));
            var warm = ArrArt.Lit(new Color(1f, 0.85f, 0.55f), new Color(1.1f, 0.78f, 0.38f));

            // hull: lofted through stations from stern to bow (z), with a sheer line rising to the bow
            float[] zs = { -1.9f, -1.25f, -0.45f, 0.4f, 1.2f, 1.9f, 2.45f, 2.85f };
            float[] ws = { 0.80f, 1.00f, 1.12f, 1.12f, 0.98f, 0.72f, 0.34f, 0.03f };
            float[] hs = { 0.60f, 0.57f, 0.56f, 0.58f, 0.63f, 0.71f, 0.81f, 0.92f };
            float[] ks = { -0.12f, -0.40f, -0.50f, -0.50f, -0.42f, -0.24f, 0.06f, 0.48f };
            Vector3 P(int s, int k, float side)
            {
                float w = ws[s], h = hs[s], kz = ks[s], z = zs[s];
                switch (k)
                {
                    case 0: return new Vector3(side * w, h, z);
                    case 1: return new Vector3(side * w * 0.995f, h - 0.15f, z);
                    case 2: return new Vector3(side * w * 0.96f, Mathf.Lerp(h, kz, 0.45f), z);
                    case 3: return new Vector3(side * w * 0.66f, Mathf.Lerp(h, kz, 0.86f), z);
                    default: return new Vector3(0, kz, z);
                }
            }
            var bStripe = new MeshBuilder();
            var bWhite = new MeshBuilder();
            var bBottom = new MeshBuilder();
            for (int s = 0; s < zs.Length - 1; s++)
                foreach (float side in new[] { -1f, 1f })
                    for (int k = 0; k < 4; k++)
                    {
                        var mb = k == 0 ? bStripe : k < 3 ? bWhite : bBottom;
                        mb.Quad(P(s, k, side), P(s + 1, k, side), P(s + 1, k + 1, side), P(s, k + 1, side), true);
                    }
            // transom (flat stern)
            Vector3 tc = new Vector3(0, (hs[0] + ks[0]) * 0.5f, zs[0]);
            foreach (float side in new[] { -1f, 1f })
                for (int k = 0; k < 4; k++)
                {
                    var mb = k == 0 ? bStripe : k < 3 ? bWhite : bBottom;
                    Vector3 p0 = P(0, k, side), p1 = P(0, k + 1, side);
                    mb.Tri(tc, p0, p1); mb.Tri(tc, p1, p0);
                }
            Models.Part(boat, "HullStripe", bStripe, stripe);
            Models.Part(boat, "Hull", bWhite, white);
            Models.Part(boat, "HullBottom", bBottom, bottom);

            // teak deck with plank seams, and a toe rail along the sheer
            var deck = new MeshBuilder();
            for (int s = 0; s < zs.Length - 1; s++)
            {
                Vector3 a = P(s, 0, -1) - Vector3.up * 0.03f, b = P(s, 0, 1) - Vector3.up * 0.03f;
                Vector3 c = P(s + 1, 0, 1) - Vector3.up * 0.03f, d = P(s + 1, 0, -1) - Vector3.up * 0.03f;
                deck.Quad(a, d, c, b, true);
            }
            Models.Part(boat, "Deck", deck, wood);
            var seams = new MeshBuilder();
            for (int k = -2; k <= 2; k++)
                seams.Box(new Vector3(k * 0.36f, 0.56f, 0.3f), new Vector3(0.025f, 0.015f, 4.2f - Mathf.Abs(k) * 0.9f));
            Models.Part(boat, "Seams", seams, woodDark);
            for (int s = 0; s < zs.Length - 1; s++)
                foreach (float side in new[] { -1f, 1f })
                    Models.Tube(boat, P(s, 0, side) + Vector3.up * 0.03f, P(s + 1, 0, side) + Vector3.up * 0.03f, 0.05f, 0.05f, woodDark, 5);

            // little cabin with round portholes
            var cabin = Models.Node(boat, "Cabin", new Vector3(0, 0.56f, -0.95f));
            var cabinMesh = new MeshBuilder();
            cabinMesh.Box(new Vector3(0, 0.24f, 0), new Vector3(1.12f, 0.48f, 1.15f));
            Models.Part(cabin, "Walls", cabinMesh, cream);
            var roofMesh = new MeshBuilder();
            roofMesh.Box(new Vector3(0, 0.5f, 0), new Vector3(1.24f, 0.08f, 1.28f));
            roofMesh.Box(new Vector3(0, 0.56f, 0), new Vector3(1.0f, 0.06f, 1.0f));
            Models.Part(cabin, "Roof", roofMesh, stripe);
            foreach (float side in new[] { -1f, 1f })
                foreach (float z in new[] { -0.28f, 0.22f })
                    Models.Part(cabin, "Porthole", Models.TubeMesh(10, 1f), warm, new Vector3(side * 0.56f, 0.26f, z),
                        Quaternion.Euler(0, 0, 90), new Vector3(0.11f, 0.03f, 0.11f));
            Models.Part(cabin, "FrontWindow", Models.TubeMesh(10, 1f), warm, new Vector3(0, 0.26f, 0.58f), Quaternion.Euler(90, 0, 0), new Vector3(0.12f, 0.03f, 0.12f));
            // life ring on the cabin's side
            Models.Part(cabin, "LifeRing", Models.Torus(0.16f, 0.05f, 12, 6), ArrArt.Lit(new Color(1f, 0.45f, 0.38f), new Color(0.3f, 0.1f, 0.08f)),
                new Vector3(-0.6f, 0.28f, -0.05f), Quaternion.Euler(0, 0, 90), Vector3.one);

            // stanchions and a thin lifeline
            for (int s = 1; s <= 5; s++)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 basePt = P(s, 0, side) * 0.96f + new Vector3(0, P(s, 0, side).y * 0.04f, 0);
                    Models.Tube(boat, basePt, basePt + Vector3.up * 0.42f, 0.022f, 0.02f, metal, 4);
                    if (s < 5)
                    {
                        Vector3 next = P(s + 1, 0, side) * 0.96f + new Vector3(0, P(s + 1, 0, side).y * 0.04f, 0);
                        Models.Tube(boat, basePt + Vector3.up * 0.42f, next + Vector3.up * 0.42f, 0.012f, 0.012f, metal, 4);
                    }
                }

            // mast with a masthead light and a waving pennant
            Models.Tube(boat, new Vector3(0, 0.55f, 0.35f), new Vector3(0, 5.15f, 0.35f), 0.075f, 0.045f, cream, 7);
            Models.Part(boat, "MastLight", Models.Gem(), warm, new Vector3(0, 5.15f, 0.35f), Quaternion.identity, Vector3.one * 0.18f);
            pennant = Models.Node(boat, "Pennant", new Vector3(0, 5.0f, 0.35f));
            Models.Part(pennant, "Flag", Models.Cloth(Vector3.zero, new Vector3(0, -0.32f, 0), new Vector3(0, -0.16f, -0.8f), Vector3.right, 0.05f, 3),
                ArrArt.Lit(ArrArt.PinkGlow, ArrArt.PinkGlow * 0.3f), Vector3.zero, Quaternion.identity, Vector3.one);

            // main sail and boom on a rig that swings out so the triangle reads from behind
            rig = Models.Node(boat, "Rig", new Vector3(0, 0, 0.35f));
            Models.Tube(rig, new Vector3(0, 1.35f, 0), new Vector3(0, 1.35f, -2.15f), 0.05f, 0.04f, cream, 6);
            var sailMat = ArrArt.Lit(new Color(1f, 0.82f, 0.24f), new Color(0.42f, 0.30f, 0.06f));
            Models.Part(rig, "Sail", Models.Cloth(new Vector3(0, 1.42f, -0.02f), new Vector3(0, 5.0f, 0.0f), new Vector3(0, 1.42f, -2.1f), Vector3.right, 0.38f, 7),
                sailMat, Vector3.zero, Quaternion.identity, Vector3.one);
            // a lighter band near the foot, like a panel seam
            Models.Part(rig, "SailBand", Models.Cloth(new Vector3(0.012f, 1.42f, -0.02f), new Vector3(0.012f, 1.95f, -0.02f), new Vector3(0.012f, 1.42f, -2.1f), Vector3.right, 0.06f, 3),
                ArrArt.Lit(new Color(1f, 0.95f, 0.75f), new Color(0.35f, 0.32f, 0.2f)), Vector3.zero, Quaternion.identity, Vector3.one);
            Models.Part(boat, "Jib", Models.Cloth(new Vector3(0, 1.0f, 2.55f), new Vector3(0, 4.3f, 0.42f), new Vector3(0.42f, 1.05f, 0.95f), Vector3.right, 0.22f, 6),
                ArrArt.Lit(new Color(1f, 0.93f, 0.66f), new Color(0.35f, 0.3f, 0.16f)), Vector3.zero, Quaternion.identity, Vector3.one);
            Models.Tube(boat, new Vector3(0, 0.95f, 2.6f), new Vector3(0, 4.35f, 0.4f), 0.012f, 0.012f, metal, 4);   // forestay

            // tiller at the stern
            Models.Tube(boat, new Vector3(0, 0.62f, -1.85f), new Vector3(0, 0.75f, -1.1f), 0.035f, 0.03f, woodDark, 5);

            // bow lantern
            Models.Tube(boat, new Vector3(0, 0.88f, 2.5f), new Vector3(0, 1.22f, 2.55f), 0.025f, 0.02f, woodDark, 4);
            Models.Part(boat, "Lantern", Models.Lathe("lantern", new[] { 0f, 0.09f, 0.11f, 0.09f, 0.04f, 0f }, new[] { 0f, 0f, 0.1f, 0.2f, 0.26f, 0.3f }, 6),
                warm, new Vector3(0, 1.1f, 2.6f), Quaternion.identity, Vector3.one);
            boatLamp = MakeGlow(boat, new Vector3(0, 1.25f, 2.6f), 1.1f, new Color(1f, 0.8f, 0.5f, 0.75f), out _, false, 0.4f);
        }

        // ------------------------------------------------------------------ shared helpers
        public static Transform Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// Low-poly ellipsoid (faceted by the flat-shaded SoftLit shader).
        public static Transform Blob(Transform parent, Vector3 localPos, Vector3 size, Material m, int lon = 9, int lat = 6, float seed = 0)
        {
            var b = new MeshBuilder();
            var rings = new List<Vector3[]>();
            for (int j = 0; j <= lat; j++)
            {
                float v = j / (float)lat * Mathf.PI;
                var ring = new Vector3[lon];
                for (int i = 0; i < lon; i++)
                {
                    float u = i / (float)lon * Mathf.PI * 2;
                    float jitter = 1f + (j > 0 && j < lat ? (Mathf.PerlinNoise(i * 0.9f + seed, j * 0.9f + seed) - 0.5f) * 0.25f : 0);
                    ring[i] = new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u)) * 0.5f * jitter;
                }
                rings.Add(ring);
            }
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    int k = (i + 1) % lon;
                    Vector3 a = rings[j][i], bb = rings[j][k], c = rings[j + 1][k], d = rings[j + 1][i];
                    b.Tri(a, bb, c);
                    b.Tri(a, c, d);
                }
            return Models.Part(parent, "Blob", b.Build("blob"), m, localPos, Quaternion.identity, size);
        }

        /// Camera-facing glow sprite. fogFactor < 1 lets beacons shine through the mist.
        public static Transform MakeGlow(Transform parent, Vector3 localPos, float size, Color color, out Material mat, bool soft = false, float fogFactor = 1f)
        {
            var go = new GameObject("Glow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = ArrArt.Quad;
            var r = go.AddComponent<MeshRenderer>();
            mat = ArrArt.Mat(soft ? "SoftAlpha" : "Additive", color, ArrArt.Glow);
            if (mat.HasProperty("_FogFactor")) mat.SetFloat("_FogFactor", fogFactor);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<FaceCamera>();
            return go.transform;
        }

        // ------------------------------------------------------------------ runtime
        void Update()
        {
            var v = Voyage.I;
            if (v == null) return;
            float dt = Time.deltaTime;

            Waves.Time = Time.time;
            bool calm = v.paused || v.phase != Phase.Sailing || v.anchored;
            Waves.Amp = Mathf.MoveTowards(Waves.Amp, calm ? 0.35f : 0.6f, dt * 0.1f);
            Waves.PushToShaders();

            clear01 = Mathf.MoveTowards(clear01, v.FoundCount / (float)Mathf.Max(1, v.surprises.Count), dt / 6f);
            // each breath in rhythm clears the mist one step further (see BreathGuide); just smoothed here
            float calmLevel = BreathGuide.I != null ? BreathGuide.I.Calm : 0f;
            clarity = Mathf.Lerp(clarity, calmLevel, 1f - Mathf.Exp(-dt * 3f));
            mist = Mathf.MoveTowards(mist, v.MistLevel, dt / 6f);

            // the boat rides the waves and leans gently into turns
            Vector3 p = v.boatPos;
            Vector3 fwd = v.Forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            float hC = Waves.Height(p.x, p.z);
            float hBow = Waves.Height(p.x + fwd.x * 2f, p.z + fwd.z * 2f);
            float hStern = Waves.Height(p.x - fwd.x * 2f, p.z - fwd.z * 2f);
            float hR = Waves.Height(p.x + right.x * 1.2f, p.z + right.z * 1.2f);
            float hL = Waves.Height(p.x - right.x * 1.2f, p.z - right.z * 1.2f);
            float k = 1f - Mathf.Exp(-dt * 4f);
            smoothHeave = Mathf.Lerp(smoothHeave, hC * 0.8f, k);
            smoothPitch = Mathf.Lerp(smoothPitch, Mathf.Atan2(hStern - hBow, 4f) * Mathf.Rad2Deg * 0.7f, k);
            smoothRoll = Mathf.Lerp(smoothRoll, Mathf.Atan2(hL - hR, 2.4f) * Mathf.Rad2Deg * 0.6f - v.steer * 6f, k);
            boat.position = new Vector3(p.x, smoothHeave, p.z);
            boat.rotation = Quaternion.Euler(smoothPitch, v.heading, smoothRoll);
            float sailOut = v.anchored ? 12f : 35f;   // sail eased in while resting at anchor
            rig.localRotation = Quaternion.Slerp(rig.localRotation, Quaternion.Euler(0, sailOut - v.steer * 8f + Mathf.Sin(Time.time * 0.7f) * 2f, 0), dt * 2f);
            pennant.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 3.1f) * 18f + Mathf.Sin(Time.time * 7.3f) * 6f, 0);
            boatLamp.localScale = Vector3.one * (1.0f + 0.1f * Mathf.Sin(Time.time * 2.3f));
            UpdateGlobals();
        }

        void LateUpdate()
        {
            var v = Voyage.I;
            if (v == null || cam == null) return;
            float dt = Time.deltaTime;
            // chase camera behind and above, lagging softly so turns stay gentle (low motion-sickness risk)
            camYaw = Mathf.LerpAngle(camYaw, v.heading, 1f - Mathf.Exp(-dt * 1.8f));
            Vector3 back = Voyage.Dir(camYaw);
            Vector3 b = boat.position;
            anchorView = Mathf.MoveTowards(anchorView, v.anchored ? 1f : 0f, dt / 2.5f);
            float av = Mathf.SmoothStep(0f, 1f, anchorView);
            // high enough that the sail does not hide the sea ahead (where the breath and the light path are)
            Vector3 target = new Vector3(b.x, 0, b.z) - back * Mathf.Lerp(13.5f, 16f, av) + Vector3.up * Mathf.Lerp(9.0f, 10.2f, av);
            cam.transform.position = camPlaced ? Vector3.Lerp(cam.transform.position, target, 1f - Mathf.Exp(-dt * 3f)) : target;
            camPlaced = true;
            Vector3 look = new Vector3(b.x, 0.6f, b.z) + back * 18f;
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);

            if (sky != null) sky.position = cam.transform.position;
            if (ocean != null)
            {
                Vector3 c = cam.transform.position;
                ocean.position = new Vector3(Mathf.Round(c.x), 0, Mathf.Round(c.z));
            }
        }

        /// Snap the camera after a scripted cut (promo).
        public void SnapCamera() { camPlaced = false; if (Voyage.I != null) camYaw = Voyage.I.heading; }

        void UpdateGlobals()
        {
            var v = Voyage.I;
            Color horizon = Color.Lerp(ArrArt.Horizon, ArrArt.DawnHorizon, clear01 * 0.8f);
            Color zenith = Color.Lerp(ArrArt.Zenith, ArrArt.DawnZenith, clear01 * 0.8f);
            Vector3 moonDir = new Vector3(-0.45f, 0.22f, 1f).normalized;
            Shader.SetGlobalVector("_ArrSunDir", moonDir);
            Shader.SetGlobalColor("_ArrSunColor", ArrArt.Sun * Mathf.Lerp(0.9f, 1.15f, clear01));
            Shader.SetGlobalColor("_ArrHorizon", horizon);
            Shader.SetGlobalColor("_ArrZenith", zenith);
            Shader.SetGlobalVector("_ArrRipple", new Vector4(0, -100, 0, 99));
            Shader.SetGlobalFloat("_ArrRippleStrength", 0f);
            Shader.SetGlobalVector("_ArrGlowPos", glowPos);
            Shader.SetGlobalColor("_ArrGlowColor", glowColor);
            if (v != null)
            {
                Vector3 d = Voyage.Dir(LighthouseView.I != null ? LighthouseView.I.ShownAngle : v.beamAngle);
                Shader.SetGlobalVector("_ArrBeamOrigin", new Vector4(v.lighthouse.x, 0, v.lighthouse.z, LighthouseView.I != null ? LighthouseView.I.beamIntensity : 1f));
                Shader.SetGlobalVector("_ArrBeamDir", new Vector4(d.x, 0, d.z, Voyage.BeamLength));
            }
            RenderSettings.fogColor = horizon;
            // the sea is thick with mist by default (it really limits how far you can see);
            // every exhale clears the air for a few seconds: the view opens, then the mist slowly returns
            float baseFog = Mathf.Lerp(0.012f, 0.026f, mist);
            RenderSettings.fogDensity = baseFog * Mathf.Lerp(1.35f, 0.4f, clarity);
            RenderSettings.ambientLight = horizon;
            if (cam != null) cam.backgroundColor = horizon;
        }
    }
}

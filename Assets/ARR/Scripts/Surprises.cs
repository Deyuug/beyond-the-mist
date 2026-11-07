using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// The hidden surprises. Each sits inside its own patch of mist that the child cannot see through.
    /// When the parent's light points at it and the child sails along that light, the patch thins and the
    /// surprise rises out of the sea. Breathing has no influence here at all.
    public class Surprises : MonoBehaviour
    {
        /// A joint that sways around an axis (flippers, tentacles, leaves, seaweed).
        struct Sway { public Transform t; public Quaternion rest; public Vector3 axis; public float amp, speed, phase; }

        class View
        {
            public Surprise s;
            public Transform root, model, body, lid;
            public readonly List<Sway> sways = new List<Sway>();
            public readonly List<Transform> bobbers = new List<Transform>();
            public readonly List<Transform> bells = new List<Transform>();
            public Material glowMat;
            public readonly List<Material> mistMats = new List<Material>();
            public readonly List<Transform> mist = new List<Transform>();
            public float reveal;
        }

        class Flare { public Transform t; public Material m; public Vector3 start, vel; public float age, life, size; public Color c; }

        readonly List<View> views = new List<View>();
        readonly List<Flare> flares = new List<Flare>();
        Transform root;

        public void Init(Transform r)
        {
            root = r;
            foreach (var s in Voyage.I.surprises) views.Add(Build(s));
            Voyage.I.SurpriseFound += OnFound;
        }

        void OnDestroy() { if (Voyage.I != null) Voyage.I.SurpriseFound -= OnFound; }

        static void AddSway(View v, Transform t, Vector3 axis, float amp, float speed, float phase)
        {
            v.sways.Add(new Sway { t = t, rest = t.localRotation, axis = axis, amp = amp, speed = speed, phase = phase });
        }

        View Build(Surprise s)
        {
            var v = new View { s = s };
            v.root = new GameObject("Surprise_" + s.name).transform;
            v.root.SetParent(root, false);
            v.root.position = s.pos;
            v.model = Models.Node(v.root, "Model", Vector3.zero);
            switch (s.id)
            {
                case 0: BuildTurtle(v, s.color); break;
                case 1: BuildJellies(v, s.color); break;
                case 2: BuildChest(v); break;
                default: BuildCoral(v, s.color); break;
            }
            OceanView.MakeGlow(v.root, new Vector3(0, 2.5f, 0), 14f, new Color(s.color.r, s.color.g, s.color.b, 0f), out v.glowMat);

            // the patch of mist hiding it
            var cloudMat = ArrArt.Mat("SoftAlpha", new Color(0.5f, 0.46f, 0.74f, 0.5f), ArrArt.Cloud);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2 + Random.Range(-0.2f, 0.2f);
                float rad = Random.Range(2f, 20f);
                var m = new Material(cloudMat);
                var t = OceanView.MakeGlow(v.root, new Vector3(Mathf.Cos(a) * rad, Random.Range(2f, 7f), Mathf.Sin(a) * rad), Random.Range(16f, 26f), Color.white, out _, true);
                t.GetComponent<MeshRenderer>().sharedMaterial = m;
                v.mist.Add(t);
                v.mistMats.Add(m);
            }
            v.model.localScale = Vector3.one * 0.001f;
            return v;
        }

        // ------------------------------------------------------------------ sea turtle
        void BuildTurtle(View v, Color c)
        {
            var shell = Models.Soft(new Color(0.22f, 0.52f, 0.50f), c * 0.32f, 0.85f);
            var scute = Models.Soft(new Color(0.36f, 0.72f, 0.60f), c * 0.22f, 0.85f);
            var rim = Models.Soft(new Color(0.62f, 0.86f, 0.70f), c * 0.2f, 0.6f);
            var skin = Models.Soft(new Color(0.55f, 0.84f, 0.74f), c * 0.12f, 0.5f);
            var belly = Models.Soft(new Color(0.85f, 0.88f, 0.70f), c * 0.1f, 0.5f);
            var eye = ArrArt.Lit(new Color(0.05f, 0.05f, 0.1f));
            var glint = ArrArt.Lit(Color.white, Color.white * 0.8f);

            var body = Models.Node(v.model, "Turtle", Vector3.zero);
            body.localScale = Vector3.one * 1.35f;
            v.body = body;
            Models.Part(body, "Belly", Models.Dome(10), belly, new Vector3(0, 0.2f, 0), Quaternion.Euler(180, 0, 0), new Vector3(1.5f, 0.28f, 1.8f));
            Models.Part(body, "Shell", Models.Dome(10), shell, new Vector3(0, 0.18f, 0), Quaternion.identity, new Vector3(1.65f, 1.0f, 2.0f));
            Models.Part(body, "Rim", Models.Torus(1.62f, 0.13f, 14, 5), rim, new Vector3(0, 0.2f, 0), Quaternion.identity, new Vector3(1f, 1f, 1.22f));
            Models.Part(body, "Scute", Models.Dome(6), scute, new Vector3(0, 1.08f, 0), Quaternion.identity, new Vector3(0.6f, 0.14f, 0.7f));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2 + 0.52f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.82f, 0.86f, Mathf.Sin(a) * 1.0f);
                Models.Part(body, "Scute", Models.Dome(6), scute, p, Quaternion.FromToRotation(Vector3.up, new Vector3(p.x * 0.6f, 1f, p.z * 0.5f)), new Vector3(0.48f, 0.12f, 0.52f));
            }
            Models.Tube(body, new Vector3(0, 0.32f, 1.6f), new Vector3(0, 0.42f, 2.05f), 0.36f, 0.32f, skin, 7);
            var head = Models.Node(body, "Head", new Vector3(0, 0.45f, 2.3f));
            OceanView.Blob(head, Vector3.zero, new Vector3(0.8f, 0.66f, 1.0f), skin, 8, 5, 3);
            foreach (float sx in new[] { -1f, 1f })
            {
                OceanView.Prim(PrimitiveType.Sphere, head, new Vector3(sx * 0.3f, 0.12f, 0.24f), Vector3.one * 0.15f, eye);
                OceanView.Prim(PrimitiveType.Sphere, head, new Vector3(sx * 0.33f, 0.17f, 0.3f), Vector3.one * 0.05f, glint);
            }
            AddSway(v, head, Vector3.up, 8f, 0.7f, 0f);
            foreach (float sx in new[] { -1f, 1f })
            {
                var front = Models.Node(body, "FrontFlipper", new Vector3(sx * 1.3f, 0.28f, 0.95f));
                OceanView.Blob(front, new Vector3(sx * 0.95f, 0, 0.1f), new Vector3(2.0f, 0.16f, 0.75f), skin, 8, 3, sx);
                front.localRotation = Quaternion.Euler(0, sx * -28f, 0);
                AddSway(v, front, Vector3.forward, 24f, 2.0f, sx > 0 ? 0 : Mathf.PI);
                var rear = Models.Node(body, "RearFlipper", new Vector3(sx * 0.95f, 0.22f, -1.35f));
                OceanView.Blob(rear, new Vector3(sx * 0.42f, 0, -0.2f), new Vector3(0.95f, 0.14f, 0.55f), skin, 7, 3, sx + 2);
                rear.localRotation = Quaternion.Euler(0, sx * 30f, 0);
                AddSway(v, rear, Vector3.forward, 14f, 2.0f, sx > 0 ? Mathf.PI : 0);
            }
            Models.Part(body, "Tail", Models.Cone(5), skin, new Vector3(0, 0.25f, -1.9f), Quaternion.Euler(-90, 0, 0), new Vector3(0.18f, 0.5f, 0.18f));
            OceanView.MakeGlow(body, new Vector3(0, 0.0f, 0), 7f, new Color(c.r, c.g, c.b, 0.4f), out _);
        }

        // ------------------------------------------------------------------ starlight jellyfish
        void BuildJellies(View v, Color c)
        {
            var bellMesh = Models.Lathe("jelly_bell", new[] { 0f, 0.9f, 1.05f, 1.0f, 0.82f, 0.5f, 0.15f, 0f },
                new[] { 0.12f, 0.05f, 0.18f, 0.48f, 0.78f, 0.98f, 1.06f, 1.07f }, 12);
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2 + 0.4f;
                Color col = i % 2 == 0 ? c : ArrArt.PinkGlow;
                float scale = i == 0 ? 1.35f : 0.9f + (i % 3) * 0.15f;
                var j = Models.Node(v.model, "Jelly" + i, i == 0 ? new Vector3(0, 2.6f, 0) : new Vector3(Mathf.Cos(a) * 4.5f, 1.8f + (i % 3) * 1.1f, Mathf.Sin(a) * 4.5f));
                j.localScale = Vector3.one * scale;
                v.bobbers.Add(j);
                var bellMat = Models.Soft(col * 0.5f, col * 0.75f, 0.3f);
                var bell = Models.Part(j, "Bell", bellMesh, bellMat, Vector3.zero, Quaternion.identity, Vector3.one);
                v.bells.Add(bell);
                // frilled rim
                for (int k = 0; k < 12; k++)
                {
                    float b = k / 12f * Mathf.PI * 2;
                    Models.Part(j, "Frill", Models.Dome(5), Models.Soft(col * 0.7f, col * 0.6f, 0.4f),
                        new Vector3(Mathf.Cos(b) * 0.98f, 0.04f, Mathf.Sin(b) * 0.98f), Quaternion.Euler(180, 0, 0), new Vector3(0.2f, 0.16f, 0.2f));
                }
                OceanView.MakeGlow(j, new Vector3(0, 0.45f, 0), 3.2f, new Color(col.r, col.g, col.b, 0.6f), out _);
                OceanView.MakeGlow(j, new Vector3(0, 0.5f, 0), 1.1f, new Color(1, 1, 1, 0.5f), out _);
                // wavy tentacles: chains of short segments
                var tent = Models.Soft(col * 0.45f, col * 0.55f, 0.3f);
                for (int k = 0; k < 8; k++)
                {
                    float b = k / 8f * Mathf.PI * 2 + 0.2f;
                    Transform joint = Models.Node(j, "Tentacle", new Vector3(Mathf.Cos(b) * 0.7f, 0.05f, Mathf.Sin(b) * 0.7f));
                    int segs = 4 + k % 3;
                    for (int n = 0; n < segs; n++)
                    {
                        Models.Tube(joint, Vector3.zero, new Vector3(0, -0.42f, 0), 0.04f - n * 0.005f, 0.035f - n * 0.005f, tent, 4);
                        AddSway(v, joint, new Vector3(Mathf.Sin(b), 0, -Mathf.Cos(b)), 12f + n * 3f, 1.6f, k * 0.7f + n * 0.55f);
                        joint = Models.Node(joint, "Joint", new Vector3(0, -0.42f, 0));
                    }
                }
                // two frilly oral arms
                for (int k = 0; k < 2; k++)
                {
                    Transform joint = Models.Node(j, "Arm", new Vector3(k == 0 ? 0.15f : -0.15f, 0.08f, 0));
                    for (int n = 0; n < 5; n++)
                    {
                        OceanView.Blob(joint, new Vector3(0, -0.25f, 0), new Vector3(0.28f, 0.5f, 0.08f), Models.Soft(ArrArt.PinkGlow * 0.7f, ArrArt.PinkGlow * 0.6f, 0.4f), 5, 3, n);
                        AddSway(v, joint, Vector3.right, 10f, 1.1f, k * 1.3f + n * 0.7f);
                        joint = Models.Node(joint, "Joint", new Vector3(0, -0.45f, 0));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ treasure on a sandbar
        void BuildChest(View v)
        {
            var sand = Models.Soft(new Color(0.60f, 0.54f, 0.68f), new Color(0.06f, 0.05f, 0.08f), 0.8f);
            var sand2 = Models.Soft(new Color(0.72f, 0.66f, 0.76f), new Color(0.08f, 0.07f, 0.09f), 0.8f);
            var rock = ArrArt.Lit(new Color(0.30f, 0.27f, 0.44f));
            var wood = ArrArt.Lit(new Color(0.60f, 0.38f, 0.25f), new Color(0.10f, 0.05f, 0.03f));
            var woodDark = ArrArt.Lit(new Color(0.36f, 0.22f, 0.15f));
            var gold = ArrArt.Lit(ArrArt.Gold, ArrArt.Gold * 0.4f);
            var trunk = Models.Soft(new Color(0.55f, 0.42f, 0.34f), new Color(0.06f, 0.04f, 0.03f), 0.9f);
            var leaf = Models.Soft(new Color(0.25f, 0.56f, 0.52f), new Color(0.04f, 0.10f, 0.10f), 0.6f);

            OceanView.Blob(v.model, new Vector3(0, -0.7f, 0), new Vector3(12f, 2.6f, 9f), sand, 10, 4, 8);
            OceanView.Blob(v.model, new Vector3(1.2f, -0.1f, -0.8f), new Vector3(7f, 1.6f, 5.5f), sand2, 9, 4, 3);
            OceanView.Blob(v.model, new Vector3(-4.6f, -0.2f, -2.2f), new Vector3(2.2f, 1.6f, 1.8f), rock, 6, 4, 1);
            OceanView.Blob(v.model, new Vector3(4.8f, -0.3f, 2.4f), new Vector3(1.6f, 1.2f, 1.4f), rock, 6, 4, 5);

            // palm tree: a curved trunk and drooping leaves that sway
            Vector3 prev = new Vector3(3.2f, 0.4f, -1.6f);
            for (int i = 1; i <= 6; i++)
            {
                float t = i / 6f;
                Vector3 p = new Vector3(3.2f + t * t * 1.6f, 0.4f + t * 5.2f, -1.6f - t * 0.6f);
                Models.Tube(v.model, prev, p, 0.24f - t * 0.08f, 0.22f - t * 0.08f, trunk, 6);
                prev = p;
            }
            var crown = Models.Node(v.model, "Crown", prev);
            for (int i = 0; i < 7; i++)
            {
                Transform joint = Models.Node(crown, "Leaf", Vector3.zero, Quaternion.Euler(0, i * 51.4f, 0));
                for (int n = 0; n < 3; n++)
                {
                    var seg = Models.Node(joint, "Seg", Vector3.zero, Quaternion.Euler(20f + n * 18f, 0, 0));
                    OceanView.Blob(seg, new Vector3(0, 0, 0.55f), new Vector3(0.55f - n * 0.1f, 0.06f, 1.2f), leaf, 6, 3, i + n);
                    if (n == 0) AddSway(v, joint, Vector3.right, 4f, 0.9f, i);
                    joint = Models.Node(seg, "Next", new Vector3(0, 0, 1.1f));
                }
            }
            for (int i = 0; i < 3; i++)
                OceanView.Prim(PrimitiveType.Sphere, crown, new Vector3(Mathf.Cos(i * 2.1f) * 0.25f, -0.25f, Mathf.Sin(i * 2.1f) * 0.25f), Vector3.one * 0.3f, woodDark);

            // starfish and shells on the sand
            Models.Part(v.model, "Starfish", Models.Star(0.6f, 0.24f, 0.14f), ArrArt.Lit(new Color(1f, 0.55f, 0.48f), new Color(0.35f, 0.14f, 0.1f)),
                new Vector3(-2.4f, 0.7f, 1.6f), Quaternion.Euler(0, 20, 0), Vector3.one);
            for (int i = 0; i < 4; i++)
                Models.Part(v.model, "Shell", Models.Cone(5), sand2, new Vector3(-1.5f + i * 1.3f, 0.66f, 2.4f - (i % 2) * 0.6f),
                    Quaternion.Euler(80, i * 70, 0), new Vector3(0.18f, 0.32f, 0.12f));

            // the chest: planked wood, gold bands, a rounded lid on a hinge
            var chest = Models.Node(v.model, "Chest", new Vector3(-0.4f, 0.62f, 0.2f), Quaternion.Euler(0, 25, 0));
            var box = new MeshBuilder();
            box.Box(new Vector3(0, 0.48f, 0), new Vector3(1.9f, 0.95f, 1.26f));
            Models.Part(chest, "Box", box, wood);
            var seams = new MeshBuilder();
            foreach (float y in new[] { 0.32f, 0.64f })
            {
                seams.Box(new Vector3(0, y, 0.635f), new Vector3(1.88f, 0.03f, 0.02f));
                seams.Box(new Vector3(0, y, -0.635f), new Vector3(1.88f, 0.03f, 0.02f));
            }
            Models.Part(chest, "Seams", seams, woodDark);
            var bands = new MeshBuilder();
            foreach (float x in new[] { -0.62f, 0.62f }) bands.Box(new Vector3(x, 0.48f, 0), new Vector3(0.14f, 0.98f, 1.3f));
            bands.Box(new Vector3(0, 0.03f, 0), new Vector3(1.96f, 0.08f, 1.32f));
            bands.Box(new Vector3(0, 0.62f, 0.66f), new Vector3(0.28f, 0.34f, 0.05f));
            Models.Part(chest, "Bands", bands, gold);
            var keyhole = new MeshBuilder();
            keyhole.Box(new Vector3(0, 0.6f, 0.69f), new Vector3(0.06f, 0.12f, 0.02f));
            Models.Part(chest, "Keyhole", keyhole, woodDark);

            Quaternion lidRot = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(-90, 0, 0);
            var hinge = Models.Node(chest, "Hinge", new Vector3(0, 0.95f, -0.63f));
            Models.Part(hinge, "Lid", Models.Lathe("chest_lid", new[] { 0f, 0.63f, 0.63f, 0f }, new[] { -0.95f, -0.95f, 0.95f, 0.95f }, 8, 180f, true),
                wood, new Vector3(0, 0, 0.63f), lidRot, Vector3.one);
            foreach (float x in new[] { -0.62f, 0.62f })
                Models.Part(hinge, "LidBand", Models.Lathe("chest_band", new[] { 0f, 0.66f, 0.66f, 0f }, new[] { -0.07f, -0.07f, 0.07f, 0.07f }, 8, 180f, true),
                    gold, new Vector3(x, 0, 0.63f), lidRot, Vector3.one);
            v.lid = hinge;

            // treasure: coins and gems, some spilled on the sand
            for (int i = 0; i < 16; i++)
            {
                bool spilled = i >= 10;
                Vector3 p = spilled
                    ? new Vector3(Random.Range(-0.6f, 1.8f), 0.05f, Random.Range(0.8f, 1.8f))
                    : new Vector3(Random.Range(-0.7f, 0.7f), 0.93f + Random.Range(0f, 0.12f), Random.Range(-0.4f, 0.4f));
                Models.Part(chest, "Coin", Models.Coin(), gold, p, Quaternion.Euler(Random.Range(-25, 25), Random.Range(0, 360), Random.Range(-25, 25)), new Vector3(0.16f, 0.16f, 0.16f));
            }
            Color[] gems = { new Color(1f, 0.5f, 0.8f), new Color(0.5f, 0.95f, 1f), new Color(0.75f, 0.6f, 1f) };
            for (int i = 0; i < 5; i++)
            {
                Color g = gems[i % 3];
                Models.Part(chest, "Gem", Models.Gem(), ArrArt.Lit(g * 0.6f, g * 0.8f), new Vector3(-0.5f + i * 0.25f, 1.0f, Random.Range(-0.3f, 0.3f)),
                    Quaternion.Euler(Random.Range(-30, 30), i * 40, Random.Range(-30, 30)), Vector3.one * 0.32f);
            }
            OceanView.MakeGlow(chest, new Vector3(0, 1.1f, 0), 3.5f, new Color(1f, 0.82f, 0.45f, 0.8f), out _);
        }

        // ------------------------------------------------------------------ glowing coral island
        void BuildCoral(View v, Color c)
        {
            var rock = Models.Soft(new Color(0.28f, 0.26f, 0.44f), new Color(0.03f, 0.03f, 0.06f), 0.9f);
            var rock2 = Models.Soft(new Color(0.36f, 0.30f, 0.50f), new Color(0.04f, 0.03f, 0.07f), 0.9f);
            OceanView.Blob(v.model, new Vector3(0, -1.0f, 0), new Vector3(16f, 3.2f, 13f), rock, 11, 4, 11);
            OceanView.Blob(v.model, new Vector3(-3f, 0f, 1.5f), new Vector3(7f, 2.2f, 6f), rock2, 9, 4, 2);
            OceanView.Blob(v.model, new Vector3(3.5f, -0.2f, -2f), new Vector3(6f, 1.8f, 5f), rock2, 8, 4, 7);

            Color[] cols = { c, new Color(0.78f, 0.6f, 1f), new Color(0.45f, 0.92f, 0.85f), new Color(1f, 0.62f, 0.45f) };
            var rnd = new System.Random(7);
            float R01() => (float)rnd.NextDouble();
            Vector3[] roots = { new Vector3(-2.5f, 0.9f, 1.0f), new Vector3(0.5f, 0.7f, -0.5f), new Vector3(2.8f, 0.5f, 1.4f), new Vector3(-0.8f, 0.6f, 3.0f), new Vector3(1.8f, 0.6f, -2.6f) };
            for (int i = 0; i < roots.Length; i++)
            {
                Color col = cols[i % cols.Length];
                var mat = Models.Soft(col * 0.6f, col * 0.5f, 0.5f);
                var tip = ArrArt.Lit(col, col * 0.9f);
                Branch(v.model, roots[i], Vector3.up, 1.6f + R01() * 0.6f, 0.24f, 3, mat, tip, R01);
            }
            // sea fans
            for (int i = 0; i < 3; i++)
            {
                Color col = i == 1 ? ArrArt.PinkGlow : c;
                var fan = Models.Node(v.model, "Fan", new Vector3(-4.2f + i * 3.4f, 0.7f, -1.2f + (i % 2) * 2.4f), Quaternion.Euler(0, i * 50f, 0));
                OceanView.Blob(fan, new Vector3(0, 1.1f, 0), new Vector3(2.2f, 2.0f, 0.12f), Models.Soft(col * 0.55f, col * 0.45f, 0.7f), 9, 5, i);
                AddSway(v, fan, Vector3.right, 6f, 0.8f, i);
            }
            // anemones with swaying tentacles
            for (int i = 0; i < 4; i++)
            {
                Color col = cols[(i + 1) % cols.Length];
                var an = Models.Node(v.model, "Anemone", new Vector3(-1.5f + i * 1.4f, 0.8f, 2.2f - (i % 2) * 4.5f));
                Models.Part(an, "Cup", Models.Lathe("anemone_cup", new[] { 0f, 0.28f, 0.34f, 0.45f, 0f }, new[] { 0f, 0f, 0.3f, 0.42f, 0.42f }, 8),
                    Models.Soft(col * 0.5f, col * 0.3f, 0.6f), Vector3.zero, Quaternion.identity, Vector3.one);
                for (int k = 0; k < 9; k++)
                {
                    float b = k / 9f * Mathf.PI * 2;
                    var j = Models.Node(an, "Tentacle", new Vector3(Mathf.Cos(b) * 0.36f, 0.42f, Mathf.Sin(b) * 0.36f),
                        Quaternion.FromToRotation(Vector3.up, new Vector3(Mathf.Cos(b) * 0.6f, 1f, Mathf.Sin(b) * 0.6f)));
                    Models.Tube(j, Vector3.zero, Vector3.up * 0.5f, 0.05f, 0.025f, ArrArt.Lit(col * 0.7f, col * 0.7f), 4);
                    AddSway(v, j, new Vector3(-Mathf.Sin(b), 0, Mathf.Cos(b)), 15f, 1.4f, k * 0.6f + i);
                }
            }
            // seaweed at the waterline
            var weed = Models.Soft(new Color(0.22f, 0.50f, 0.46f), new Color(0.05f, 0.12f, 0.12f), 0.5f);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2;
                Transform joint = Models.Node(v.model, "Weed", new Vector3(Mathf.Cos(a) * 6.2f, 0.1f, Mathf.Sin(a) * 5.0f));
                for (int n = 0; n < 5; n++)
                {
                    Models.Tube(joint, Vector3.zero, Vector3.up * 0.55f, 0.09f - n * 0.012f, 0.08f - n * 0.012f, weed, 4);
                    AddSway(v, joint, Vector3.right, 9f + n * 3f, 0.9f, i + n * 0.5f);
                    joint = Models.Node(joint, "Joint", Vector3.up * 0.55f);
                }
            }
            OceanView.MakeGlow(v.model, new Vector3(0, 2.2f, 0), 9f, new Color(c.r, c.g, c.b, 0.35f), out _);
        }

        static void Branch(Transform parent, Vector3 start, Vector3 dir, float len, float r, int depth, Material mat, Material tip, System.Func<float> rnd)
        {
            Vector3 end = start + dir * len;
            Models.Tube(parent, start, end, r, r * 0.75f, mat, 6);
            if (depth == 0)
            {
                Models.Part(parent, "Tip", Models.Gem(), tip, end, Quaternion.identity, Vector3.one * (r * 2.4f));
                return;
            }
            int kids = rnd() < 0.5f ? 2 : 3;
            for (int k = 0; k < kids; k++)
            {
                Quaternion q = Quaternion.AngleAxis(25f + rnd() * 20f, Quaternion.AngleAxis(k * 360f / kids + rnd() * 40f, Vector3.up) * Vector3.right);
                Branch(parent, end, (q * dir).normalized, len * 0.72f, r * 0.72f, depth - 1, mat, tip, rnd);
            }
        }

        // ------------------------------------------------------------------ discovery
        void OnFound(Surprise s)
        {
            Color c = s.color;
            for (int i = 0; i < 26; i++)
            {
                Vector3 start = s.pos + new Vector3(Random.Range(-6f, 6f), Random.Range(0.5f, 3f), Random.Range(-6f, 6f));
                SpawnFlare(start, Vector3.up * Random.Range(1f, 2.6f) + Random.insideUnitSphere * 0.6f, i % 3 == 0 ? ArrArt.Gold : c, Random.Range(4f, 8f), Random.Range(0.6f, 1.4f));
            }
        }

        void SpawnFlare(Vector3 start, Vector3 vel, Color c, float life, float size)
        {
            var go = new GameObject("Sparkle");
            go.transform.SetParent(root, false);
            var f = new Flare { t = go.transform, start = start, vel = vel, c = c, life = life, size = size };
            OceanView.MakeGlow(f.t, Vector3.zero, size, c, out f.m);
            go.transform.position = start;
            flares.Add(f);
        }

        void Update()
        {
            var vy = Voyage.I;
            var ov = OceanView.I;
            if (vy == null || ov == null) return;
            float dt = Time.deltaTime;
            float time = Time.time;
            Vector4 glow = Vector4.zero;
            Color glowCol = ArrArt.Gold;
            float bestDist = float.MaxValue;

            foreach (var v in views)
            {
                var s = v.s;
                float target = s.found ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, s.progress)) * 0.6f;
                v.reveal = Mathf.MoveTowards(v.reveal, target, dt / (s.found ? 2.5f : 4f));
                float r = v.reveal;
                // rises out of the sea as it is revealed
                v.model.localScale = Vector3.one * Mathf.Max(0.001f, Mathf.Sqrt(r));
                v.model.localPosition = new Vector3(0, -3f * (1 - r) + Waves.Height(s.pos.x, s.pos.z) * 0.6f, 0);
                v.glowMat.SetColor("_Color", new Color(s.color.r, s.color.g, s.color.b, r * (s.found ? 0.24f + 0.06f * Mathf.Sin(time * 1.5f) : 0.12f)));

                // the mist patch thins with shared progress
                float mistA = (1f - s.progress) * 0.6f + 0.02f;
                for (int i = 0; i < v.mistMats.Count; i++)
                {
                    v.mistMats[i].SetColor("_Color", new Color(0.5f, 0.46f, 0.74f, mistA * (0.75f + 0.25f * Mathf.Sin(time * 0.4f + i))));
                    v.mist[i].localPosition += new Vector3(Mathf.Sin(time * 0.2f + i), 0, Mathf.Cos(time * 0.17f + i)) * dt * 0.3f;
                }

                Animate(v, time, dt);

                if (s.found)
                {
                    float d = Vector3.Distance(vy.boatPos, s.pos);
                    if (d < bestDist) { bestDist = d; glow = new Vector4(s.pos.x, 1.5f, s.pos.z, 0.5f); glowCol = s.color; }
                }
            }
            ov.glowPos = glow;
            ov.glowColor = glowCol;
            UpdateTreasureGlow(vy, dt, time);

            for (int i = flares.Count - 1; i >= 0; i--)
            {
                var f = flares[i];
                f.age += dt;
                if (f.age > f.life) { Destroy(f.t.gameObject); flares.RemoveAt(i); continue; }
                float t = f.age / f.life;
                f.t.position = f.start + f.vel * f.age;
                f.m.SetColor("_Color", new Color(f.c.r, f.c.g, f.c.b, Mathf.Clamp01(f.age / 0.3f) * (1f - t)));
                f.t.localScale = Vector3.one * (1f + t * 0.5f);
            }
        }

        // ---- a small patch of glowing sea where the parent's light points, so the child can find the surprise
        int glowShown = -1;
        float glowLevel;
        Vector3 glowAt;

        void UpdateTreasureGlow(Voyage vy, float dt, float time)
        {
            int want = vy.aimed >= 0 && !vy.surprises[vy.aimed].found ? vy.aimed : -1;
            float target = 0f;
            if (want >= 0 && want == glowShown)
            {
                // fainter far away, brightest within ~40 m, breathing gently
                float d = Vector3.Distance(vy.boatPos, vy.surprises[want].pos);
                float near = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(40f, 140f, d));
                target = (0.35f + 0.65f * near) * (0.85f + 0.15f * Mathf.Sin(time * 2f));
            }
            // a new target: fade the old patch out first, then move and fade in; found: fade out over ~3 s
            glowLevel = Mathf.MoveTowards(glowLevel, target, dt / (glowShown >= 0 && vy.surprises[glowShown].found ? 3f : 1.2f));
            if (want != glowShown && glowLevel < 0.02f) { glowShown = want; if (want >= 0) glowAt = vy.surprises[want].pos; }
            Color c = glowShown >= 0 ? Color.Lerp(ArrArt.Gold, vy.surprises[glowShown].color, 0.4f) : ArrArt.Gold;
            Shader.SetGlobalVector("_ArrTreasure", new Vector4(glowAt.x, 0f, glowAt.z, glowLevel));
            Shader.SetGlobalColor("_ArrTreasureColor", c);
        }

        void Animate(View v, float time, float dt)
        {
            if (v.reveal < 0.01f) return;
            foreach (var sw in v.sways)
                sw.t.localRotation = sw.rest * Quaternion.AngleAxis(Mathf.Sin(time * sw.speed + sw.phase) * sw.amp, sw.axis);

            if (v.body != null)
            {
                // the turtle paddles a slow, wide circle
                float a = time * 0.16f;
                v.body.localPosition = new Vector3(Mathf.Cos(a) * 5f, Mathf.Sin(time * 0.8f) * 0.08f, Mathf.Sin(a) * 5f);
                v.body.localRotation = Quaternion.Euler(Mathf.Sin(time * 1.0f) * 2f, -a * Mathf.Rad2Deg, Mathf.Sin(time * 2.0f) * 3f);
            }
            for (int i = 0; i < v.bobbers.Count; i++)
            {
                var j = v.bobbers[i];
                var p = j.localPosition;
                p.y = (i == 0 ? 2.6f : 1.8f + (i % 3) * 1.1f) + Mathf.Sin(time * 0.9f + i) * 0.5f;
                j.localPosition = p;
                float sq = 1f + 0.09f * Mathf.Sin(time * 2.2f + i * 1.3f);
                v.bells[i].localScale = new Vector3(sq, 2f - sq, sq);
            }
            if (v.lid != null)
                v.lid.localRotation = Quaternion.Slerp(v.lid.localRotation, Quaternion.Euler(v.s.found ? -105f : 0f, 0, 0), dt * 1.5f);
        }
    }
}

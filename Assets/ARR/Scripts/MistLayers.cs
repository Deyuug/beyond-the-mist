using UnityEngine;

namespace ARR
{
    /// Layered sea mist (Unity counterpart of "Exponential Height Fog + Local Fog Volumes + animated noise fog
    /// material + Sphere Mask"): a stack of translucent slices over the water, densest near the sea.
    /// Drives the shader globals: noise texture, wind, breath puffs, local clear pockets and dense patches.
    public class MistLayers : MonoBehaviour
    {
        public static MistLayers I;
        const int Layers = 8;
        const float BaseHeight = 0.9f, Step = 0.55f;
        readonly Transform[] slices = new Transform[Layers];
        float density = 1f;
        readonly Vector4[] clearSpots = new Vector4[5];
        readonly Vector4[] denseSpots = new Vector4[4];

        public void Init(Transform root)
        {
            I = this;
            Shader.SetGlobalTexture("_MistNoise", MakeNoise(256));
            var mesh = new Mesh { name = "mist_slice" };
            const float h = 250f;
            mesh.vertices = new[] { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(h, 0, h), new Vector3(-h, 0, h) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(h * 2, 1, h * 2));
            var shader = ArrArt.Shader("Mist");
            for (int i = 0; i < Layers; i++)
            {
                float y = BaseHeight + i * Step;
                var m = new Material(shader);
                m.SetFloat("_Layer", i * 1.618f);
                // exponential height profile: most of the mist hugs the water
                m.SetFloat("_LayerWeight", 0.70f * Mathf.Exp(-(y - BaseHeight) / 2.4f));
                m.renderQueue = 2980 + i;   // the camera is always above the slices: draw bottom to top
                slices[i] = Models.Part(root, "MistSlice" + i, mesh, m, new Vector3(0, y, 0), Quaternion.identity, Vector3.one);
            }
        }

        void OnDestroy() { if (I == this) I = null; }

        /// Tileable value-noise fBm (so the slices can scroll forever without seams).
        static Texture2D MakeNoise(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var px = new Color32[size * size];
            int[] periods = { 6, 12, 24, 48 };
            float[] weights = { 0.45f, 0.28f, 0.17f, 0.10f };
            var rnd = new System.Random(11);
            var lattices = new float[periods.Length][];
            for (int o = 0; o < periods.Length; o++)
            {
                lattices[o] = new float[periods[o] * periods[o]];
                for (int k = 0; k < lattices[o].Length; k++) lattices[o][k] = (float)rnd.NextDouble();
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float v = 0;
                    for (int o = 0; o < periods.Length; o++)
                    {
                        int P = periods[o];
                        float fx = x / (float)size * P, fy = y / (float)size * P;
                        int x0 = (int)fx, y0 = (int)fy;
                        float tx = fx - x0, ty = fy - y0;
                        tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                        float L(int a, int b) => lattices[o][((b % P + P) % P) * P + ((a % P + P) % P)];
                        float top = Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), tx);
                        float bot = Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), tx);
                        v += Mathf.Lerp(top, bot, ty) * weights[o];
                    }
                    byte b8 = (byte)(Mathf.Clamp01(v) * 255);
                    px[y * size + x] = new Color32(b8, b8, b8, 255);
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        void LateUpdate()
        {
            var v = Voyage.I;
            var ov = OceanView.I;
            if (v == null || ov == null || ov.cam == null) return;
            float dt = Time.deltaTime;

            // slices follow the camera (snapped, so the world-space noise never swims)
            Vector3 c = ov.cam.transform.position;
            for (int i = 0; i < Layers; i++)
                slices[i].position = new Vector3(Mathf.Round(c.x / 2f) * 2f, BaseHeight + i * Step, Mathf.Round(c.z / 2f) * 2f);

            // the sea becomes a little clearer as it is explored together (narrative only)
            density = Mathf.MoveTowards(density, Mathf.Lerp(0.8f, 1f, v.MistLevel), dt / 6f);
            float clarity = OceanView.I != null ? OceanView.I.clarity : 0f;
            // thick, cloud-sea mist that thins step by step with breaths in rhythm (1.15x thick .. 0.3x clear)
            Shader.SetGlobalFloat("_MistDensity", density * Mathf.Lerp(1.15f, 0.3f, clarity));
            Shader.SetGlobalColor("_MistColorLow", new Color(0.26f, 0.36f, 0.72f));
            Shader.SetGlobalColor("_MistColorHigh", new Color(0.60f, 0.70f, 0.97f));
            Shader.SetGlobalVector("_MistWind", new Vector4(0.6f, 0.25f, -0.35f, 0.5f));

            Vector3 bow = v.boatPos + v.Forward * 2.4f + Vector3.up * 1.2f;
            Shader.SetGlobalVector("_ArrBow", new Vector4(bow.x, bow.y, bow.z, 0.45f));   // mist ~45% thinner right by the boat, never cleared
            Shader.SetGlobalVector("_ArrBowDir", new Vector4(v.Forward.x, 0, v.Forward.z, 0));
            BreathFlow.Tick(Stardust.Bow(v), v.Forward, v.currentSpeed * v.demoSpeed);
            BreathFlow.PushGlobal();

            clearSpots[0] = new Vector4(v.lighthouse.x, 0, v.lighthouse.z, 13f);
            for (int i = 0; i < 4; i++)
            {
                if (i < v.surprises.Count)
                {
                    var s = v.surprises[i];
                    float r = s.found ? 26f : 26f * Mathf.SmoothStep(0, 1, s.progress);
                    clearSpots[i + 1] = new Vector4(s.pos.x, 0, s.pos.z, r);
                    denseSpots[i] = new Vector4(s.pos.x, 0, s.pos.z, s.found ? 0 : 1.4f * (1f - s.progress));
                }
                else { clearSpots[i + 1] = Vector4.zero; denseSpots[i] = Vector4.zero; }
            }
            Shader.SetGlobalVectorArray("_ArrClearSpots", clearSpots);
            Shader.SetGlobalVectorArray("_ArrDenseSpots", denseSpots);
        }
    }
}

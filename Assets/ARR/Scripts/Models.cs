using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// Procedural low-poly modelling helpers: surfaces of revolution, tapered tubes, parts.
    /// Everything is flat-shaded by SoftLit, which gives the "dreamy low-poly" look of the art direction.
    public static class Models
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        /// Surface of revolution around +Y. Profile points go bottom -> top along the outer surface;
        /// a radius of 0 at either end closes the shape. arc < 360 makes a partial shell (e.g. a chest lid).
        public static Mesh Lathe(string key, float[] r, float[] y, int seg, float arc = 360f, bool doubleSided = false)
        {
            if (key != null && cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var b = new MeshBuilder();
            bool full = arc >= 359.9f;
            int cols = full ? seg : seg + 1;
            var pts = new Vector3[r.Length, cols];
            for (int j = 0; j < r.Length; j++)
                for (int i = 0; i < cols; i++)
                {
                    float t = arc * i / seg * Mathf.Deg2Rad;
                    pts[j, i] = new Vector3(r[j] * Mathf.Cos(t), y[j], r[j] * Mathf.Sin(t));
                }
            for (int j = 0; j < r.Length - 1; j++)
                for (int i = 0; i < seg; i++)
                {
                    int i1 = full ? (i + 1) % seg : i + 1;
                    Vector3 a = pts[j, i], bb = pts[j, i1], c = pts[j + 1, i1], d = pts[j + 1, i];
                    b.Tri(a, d, c);
                    b.Tri(a, c, bb);
                    if (doubleSided) { b.Tri(a, c, d); b.Tri(a, bb, c); }
                }
            var m = b.Build(key ?? "lathe");
            if (key != null) cache[key] = m;
            return m;
        }

        /// Unit tube along +Y (radius 1 at the bottom, `taper` at the top), capped.
        public static Mesh TubeMesh(int seg, float taper)
        {
            taper = Mathf.Round(taper * 20f) / 20f;
            return Lathe($"tube_{seg}_{taper}", new[] { 0f, 1f, taper, 0f }, new[] { 0f, 0f, 1f, 1f }, seg);
        }

        /// A tapered tube from a to b (local space of parent).
        public static Transform Tube(Transform parent, Vector3 a, Vector3 b, float r0, float r1, Material m, int seg = 6)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            var t = Part(parent, "Tube", TubeMesh(seg, r1 / Mathf.Max(r0, 1e-4f)), m, a,
                len > 1e-5f ? Quaternion.FromToRotation(Vector3.up, d / len) : Quaternion.identity,
                new Vector3(r0, Mathf.Max(len, 1e-4f), r0));
            return t;
        }

        public static Transform Part(Transform parent, string name, Mesh mesh, Material m, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        public static Transform Part(Transform parent, string name, MeshBuilder b, Material m)
        {
            return Part(parent, name, b.Build(name), m, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public static Transform Node(Transform parent, string name, Vector3 pos, Quaternion? rot = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = rot ?? Quaternion.identity;
            return t;
        }

        /// Flat-shaded material with softer facets (for organic shapes).
        public static Material Soft(Color c, Color emission = default, float flat = 0.45f)
        {
            var m = ArrArt.Lit(c, emission);
            m.SetFloat("_Flat", flat);
            return m;
        }

        // ---------------------------------------------------------------- common shapes
        public static Mesh Dome(int seg = 10) =>
            Lathe($"dome_{seg}", new[] { 1f, 0.97f, 0.86f, 0.66f, 0.38f, 0f }, new[] { 0f, 0.22f, 0.5f, 0.76f, 0.94f, 1f }, seg);

        public static Mesh Cone(int seg = 8) => Lathe($"cone_{seg}", new[] { 0f, 1f, 0f }, new[] { 0f, 0f, 1f }, seg);

        public static Mesh Gem() => Lathe("gem", new[] { 0f, 0.55f, 0.42f, 0f }, new[] { 0f, 0.45f, 0.75f, 0.9f }, 6);

        public static Mesh Coin() => Lathe("coin", new[] { 0f, 1f, 1f, 0f }, new[] { 0f, 0f, 0.14f, 0.14f }, 10);

        public static Mesh Ring(float r, float thickness, float height, int seg = 16) =>
            Lathe($"ring_{r}_{thickness}_{height}_{seg}", new[] { r - thickness, r, r, r - thickness, r - thickness },
                new[] { 0f, 0f, height, height, 0f }, seg, 360f, true);

        public static Mesh Torus(float R, float rr, int seg = 14, int around = 6)
        {
            var r = new float[around + 1];
            var y = new float[around + 1];
            for (int i = 0; i <= around; i++)
            {
                float t = i / (float)around * Mathf.PI * 2f;
                r[i] = R + rr * Mathf.Cos(t);
                y[i] = rr * Mathf.Sin(t);
            }
            return Lathe($"torus_{R}_{rr}_{seg}_{around}", r, y, seg, 360f, true);
        }

        /// Five-pointed star slab (starfish).
        public static Mesh Star(float outer, float inner, float h)
        {
            string key = $"star_{outer}_{inner}_{h}";
            if (cache.TryGetValue(key, out var c) && c != null) return c;
            var b = new MeshBuilder();
            var pts = new Vector3[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float rad = i % 2 == 0 ? outer : inner;
                pts[i] = new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad);
            }
            Vector3 top = new Vector3(0, h, 0), bottom = Vector3.zero;
            for (int i = 0; i < 10; i++)
            {
                Vector3 p0 = pts[i], p1 = pts[(i + 1) % 10];
                b.Tri(top, p1, p0); b.Tri(top, p0, p1);
            }
            var m = b.Build(key);
            cache[key] = m;
            return m;
        }

        /// Triangular prism (a little roof): ridge along Z.
        public static Mesh Roof(float w, float h, float l)
        {
            string key = $"roof_{w}_{h}_{l}";
            if (cache.TryGetValue(key, out var c) && c != null) return c;
            var b = new MeshBuilder();
            Vector3 a0 = new Vector3(-w / 2, 0, -l / 2), b0 = new Vector3(w / 2, 0, -l / 2), t0 = new Vector3(0, h, -l / 2);
            Vector3 a1 = new Vector3(-w / 2, 0, l / 2), b1 = new Vector3(w / 2, 0, l / 2), t1 = new Vector3(0, h, l / 2);
            b.Quad(a0, t0, t1, a1, true);
            b.Quad(b0, b1, t1, t0, true);
            b.Tri(a0, b0, t0); b.Tri(a0, t0, b0);
            b.Tri(a1, t1, b1); b.Tri(a1, b1, t1);
            var m = b.Build(key);
            cache[key] = m;
            return m;
        }

        /// A subdivided, bellied triangle (sails, pennants, frills). Double-sided.
        public static Mesh Cloth(Vector3 a, Vector3 b, Vector3 c, Vector3 bellyDir, float belly, int n = 6)
        {
            var mb = new MeshBuilder();
            Vector3 P(int i, int j)
            {
                float l1 = i / (float)n, l2 = j / (float)n, l0 = 1f - l1 - l2;
                return a * l0 + b * l1 + c * l2 + bellyDir * (belly * 27f * l0 * l1 * l2);
            }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n - i; j++)
                {
                    Vector3 p0 = P(i, j), p1 = P(i + 1, j), p2 = P(i, j + 1);
                    mb.Tri(p0, p1, p2); mb.Tri(p0, p2, p1);
                    if (j < n - i - 1)
                    {
                        Vector3 p3 = P(i + 1, j + 1);
                        mb.Tri(p1, p3, p2); mb.Tri(p1, p2, p3);
                    }
                }
            return mb.Build("cloth");
        }
    }

    /// Minimal flat-shaded mesh builder.
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<int> t = new List<int>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool doubleSided = false)
        {
            Tri(a, b, c); Tri(a, c, d);
            if (doubleSided) { Tri(a, c, b); Tri(a, d, c); }
        }

        public void Box(Vector3 center, Vector3 size, Quaternion? rot = null)
        {
            Quaternion r = rot ?? Quaternion.identity;
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z) => center + r * new Vector3(x * h.x, y * h.y, z * h.z);
            Vector3 a = P(-1, -1, -1), b = P(1, -1, -1), c = P(1, 1, -1), d = P(-1, 1, -1);
            Vector3 e = P(-1, -1, 1), f = P(1, -1, 1), g = P(1, 1, 1), hh = P(-1, 1, 1);
            Quad(a, d, c, b); Quad(f, g, hh, e); Quad(e, hh, d, a);
            Quad(b, c, g, f); Quad(d, hh, g, c); Quad(a, b, f, e);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}

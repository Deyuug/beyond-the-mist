using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// Every texture, sprite, material and the font are generated at runtime. No imported assets.
    public static class ArrArt
    {
        // palette
        // "Dreamy Night Sailing": Midnight Indigo #06123E, Deep Ocean #123B73, Violet Mist #6B4C91,
        // Pink Glow #D28AB8, Lighthouse Gold #FFD07C
        public static readonly Color MidnightIndigo = new Color(0.024f, 0.071f, 0.243f);
        public static readonly Color DeepOcean = new Color(0.071f, 0.231f, 0.451f);
        public static readonly Color VioletMist = new Color(0.42f, 0.30f, 0.57f);
        public static readonly Color PinkGlow = new Color(0.82f, 0.54f, 0.72f);
        // kept deep and dark on purpose: the warm lighthouse beam must be the brightest thing on screen
        public static readonly Color Horizon = new Color(0.17f, 0.21f, 0.45f);      // misty horizon = fog colour (pale misty blue)
        public static readonly Color Zenith = new Color(0.010f, 0.030f, 0.12f);
        public static readonly Color DawnHorizon = new Color(0.19f, 0.21f, 0.50f);  // when the sea has been explored
        public static readonly Color DawnZenith = new Color(0.02f, 0.05f, 0.18f);
        public static readonly Color Sun = new Color(0.62f, 0.58f, 0.74f);          // the moon, soft and dim
        public static readonly Color Gold = new Color(1.0f, 0.816f, 0.486f);
        public static readonly Color Cyan = new Color(0.55f, 0.95f, 0.92f);
        public static readonly Color Ink = new Color(0.06f, 0.13f, 0.17f);
        public static readonly Color Panel = new Color(0.07f, 0.10f, 0.25f, 0.94f);
        public static readonly Color PanelLight = new Color(0.12f, 0.16f, 0.34f, 0.96f);
        public static readonly Color TextMain = new Color(0.93f, 0.94f, 1.0f);
        public static readonly Color TextDim = new Color(0.66f, 0.68f, 0.86f);


        static readonly Dictionary<string, UnityEngine.Object> cache = new Dictionary<string, UnityEngine.Object>();

        public static void ClearCache() { cache.Clear(); font = null; }

        static T Cached<T>(string key, Func<T> make) where T : UnityEngine.Object
        {
            if (cache.TryGetValue(key, out var o) && o != null) return (T)o;
            var v = make();
            cache[key] = v;
            return v;
        }

        // ---------------- font ----------------
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null)
                    font = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Avenir Next", "Avenir", "Segoe UI", "Helvetica Neue", "Helvetica",
                        "PingFang SC", "Microsoft YaHei", "Arial Unicode MS", "Arial"
                    }, 32);
                return font;
            }
        }

        // ---------------- shaders / materials ----------------
        public static Shader Shader(string name)
        {
            var s = Resources.Load<Shader>("ARR/" + name);
            if (s == null) s = UnityEngine.Shader.Find("ARR/" + name);
            if (s == null) Debug.LogError("[ARR] missing shader " + name);
            return s;
        }

        public static Material Mat(string shader, Color color, Texture tex = null)
        {
            var m = new Material(Shader(shader));
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (tex != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            return m;
        }

        public static Material Lit(Color color, Color emission = default)
        {
            var m = Mat("SoftLit", color);
            m.SetColor("_Emission", emission);
            return m;
        }

        // ---------------- textures ----------------
        static Texture2D NewTex(int w, int h, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear };
            return t;
        }

        static Texture2D Radial(string key, int size, Func<float, float> alphaOfR)
        {
            return Cached(key, () =>
            {
                var t = NewTex(size, size);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (x + 0.5f) / size * 2 - 1, v = (y + 0.5f) / size * 2 - 1;
                        float a = Mathf.Clamp01(alphaOfR(Mathf.Sqrt(u * u + v * v)));
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                t.SetPixels32(px); t.Apply();
                return t;
            });
        }

        public static Texture2D SoftDot => Radial("softdot", 64, r => Mathf.Pow(Mathf.Clamp01(1 - r), 2.2f));
        public static Texture2D Glow => Radial("glow", 128, r => Mathf.Exp(-r * r * 9f) * 0.9f + Mathf.Pow(Mathf.Clamp01(1 - r), 3) * 0.25f);

        public static Texture2D Cloud => Cached("cloud", () =>
        {
            const int s = 128;
            var t = NewTex(s, s);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s * 2 - 1, v = (y + 0.5f) / s * 2 - 1;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float n = Mathf.PerlinNoise(x * 0.045f + 3.1f, y * 0.045f + 7.7f) * 0.65f + Mathf.PerlinNoise(x * 0.11f, y * 0.11f) * 0.35f;
                    float a = Mathf.Clamp01(1 - r) * Mathf.Clamp01(1 - r * 0.6f);
                    a = Mathf.Clamp01(a * (0.55f + n * 0.9f));
                    a = a * a * (3 - 2 * a);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        });

        /// u across the beam (soft edges), v along the beam (fades out with distance).
        public static Texture2D Beam => Cached("beam", () =>
        {
            const int s = 64;
            var t = NewTex(s, s);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    // a flat, graphic wedge of light with only slightly softened edges
                    float across = Mathf.SmoothStep(0f, 1f, u / 0.14f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.14f);
                    float a = across * Mathf.Pow(1 - v, 1.1f) * 0.95f;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        });

        // ---------------- UI sprites (supersampled shape tests) ----------------
        static Sprite ShapeSprite(string key, int size, Func<float, float, bool> inside, float border = 0)
        {
            return Cached(key, () =>
            {
                var t = NewTex(size, size);
                var px = new Color32[size * size];
                const int ss = 4;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int hit = 0;
                        for (int sy = 0; sy < ss; sy++)
                            for (int sx = 0; sx < ss; sx++)
                            {
                                float u = (x + (sx + 0.5f) / ss) / size * 2 - 1;
                                float v = (y + (sy + 0.5f) / ss) / size * 2 - 1;
                                if (inside(u, v)) hit++;
                            }
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / (ss * ss)));
                    }
                t.SetPixels32(px); t.Apply();
                return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                    SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            });
        }

        public static Sprite Circle => ShapeSprite("circle", 128, (u, v) => u * u + v * v < 0.94f);
        public static Sprite Ring => ShapeSprite("ring", 128, (u, v) => { float r = Mathf.Sqrt(u * u + v * v); return r < 0.96f && r > 0.78f; });

        public static Sprite RoundRect => ShapeSprite("roundrect", 64, (u, v) =>
        {
            const float r = 0.6f; // radius in uv units (~19px)
            float ax = Mathf.Max(Mathf.Abs(u) - (1 - r), 0), ay = Mathf.Max(Mathf.Abs(v) - (1 - r), 0);
            return ax * ax + ay * ay < r * r;
        }, 24);

        public static Sprite Boat => ShapeSprite("boat", 64, (u, v) =>
        {
            // arrow pointing up
            if (v > 0.92f || v < -0.8f) return false;
            float half = (0.92f - v) * 0.36f;
            if (Mathf.Abs(u) > half) return false;
            return !(v < -0.45f && Mathf.Abs(u) < (-0.45f - v) * 0.9f);
        });

        public static Sprite Heart => ShapeSprite("heart", 128, (u, v) =>
        {
            float x = u * 1.25f, y = v * 1.25f + 0.15f;
            float a = x * x + y * y - 1;
            return a * a * a - x * x * y * y * y < 0;
        });

        public static Sprite Moon => ShapeSprite("moon", 128, (u, v) =>
            u * u + v * v < 0.72f && (u - 0.38f) * (u - 0.38f) + (v - 0.28f) * (v - 0.28f) > 0.5f);

        public static Sprite CloudIcon => ShapeSprite("cloudicon", 128, (u, v) =>
        {
            bool c(float cx, float cy, float r) => (u - cx) * (u - cx) + (v - cy) * (v - cy) < r * r;
            return c(-0.42f, -0.12f, 0.36f) || c(0.05f, 0.12f, 0.5f) || c(0.48f, -0.12f, 0.34f) ||
                   (v > -0.48f && v < -0.12f && Mathf.Abs(u) < 0.5f);
        });

        public static Sprite Star => ShapeSprite("star", 128, (u, v) =>
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = i % 2 == 0 ? 0.95f : 0.42f;
                pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.06f);
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((pts[i].y > v) != (pts[j].y > v) && u < (pts[j].x - pts[i].x) * (v - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            return inside;
        });

        public static Sprite GlowSprite => Cached("glowsprite", () =>
        {
            var t = Glow;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
        });

        // ---------------- meshes ----------------
        public static Mesh Quad => Cached("quad", () =>
        {
            var m = new Mesh { name = "arr_quad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        });

        /// Flat trapezoid along +Z in the XZ plane (narrow at the origin). Oriented every frame to face the camera
        /// around its own axis, so the soft u-edges of the Beam texture read as a volumetric shaft of light.
        public static Mesh BeamQuad(float length, float nearWidth, float farWidth)
        {
            var m = new Mesh { name = "arr_beam" };
            m.vertices = new[]
            {
                new Vector3(-nearWidth / 2, 0, 0), new Vector3(nearWidth / 2, 0, 0),
                new Vector3(farWidth / 2, 0, length), new Vector3(-farWidth / 2, 0, length)
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }
    }
}

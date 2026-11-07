using UnityEngine;

namespace ARR
{
    /// CPU mirror of the Gerstner waves in Ocean.shader, so the boat and floating lights ride the same sea.
    public static class Waves
    {
        // xy = direction, z = steepness, w = wavelength (metres)
        public static readonly Vector4[] Params =
        {
            new Vector4(1.0f, 0.35f, 0.16f, 36f),
            new Vector4(0.35f, 1.0f, 0.13f, 22f),
            new Vector4(-0.6f, 0.8f, 0.10f, 13f),
            new Vector4(0.9f, -0.45f, 0.07f, 7.5f),
        };

        public static float Amp = 0.7f;
        public static float Time;

        public static void PushToShaders()
        {
            Shader.SetGlobalVectorArray("_ArrWaves", Params);
            Shader.SetGlobalFloat("_ArrWaveAmp", Amp);
            Shader.SetGlobalFloat("_ArrTime", Time);
        }

        public static float Height(float x, float z)
        {
            float h = 0f;
            for (int i = 0; i < Params.Length; i++)
            {
                Vector4 w = Params[i];
                float k = 2f * Mathf.PI / w.w;
                float c = Mathf.Sqrt(9.8f / k);
                Vector2 d = new Vector2(w.x, w.y).normalized;
                float s = w.z * Amp;
                float f = k * (d.x * x + d.y * z - c * Time);
                h += s / k * Mathf.Sin(f);
            }
            return h;
        }
    }
}

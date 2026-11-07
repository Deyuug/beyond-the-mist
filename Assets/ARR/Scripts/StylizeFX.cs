using UnityEngine;

namespace ARR
{
    /// Picture-book finish on the child camera (Stylize.shader):
    ///   - mist haze: a soft blur + faint veil over the whole view while the mist is thick; it clears step by
    ///     step with breaths in rhythm, together with the mist
    ///   - navy/gold split tone, printed grain, vignette
    /// UI (screen-space overlay) is drawn afterwards and stays crisp.
    [RequireComponent(typeof(Camera))]
    public class StylizeFX : MonoBehaviour
    {
        public float grain = 0.075f;
        public float vignette = 0.38f;
        public float hazeStrength = 1.0f;
        Material mat;

        void Awake()
        {
            var sh = ArrArt.Shader("Stylize");
            if (sh != null && sh.isSupported) mat = new Material(sh);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null) { Graphics.Blit(src, dst); return; }

            // cheap wide blur: successive bilinear down-samples, one up-sample
            var half = RenderTexture.GetTemporary(Mathf.Max(1, src.width / 2), Mathf.Max(1, src.height / 2), 0, src.format);
            var quarter = RenderTexture.GetTemporary(Mathf.Max(1, src.width / 4), Mathf.Max(1, src.height / 4), 0, src.format);
            var eighth = RenderTexture.GetTemporary(Mathf.Max(1, src.width / 8), Mathf.Max(1, src.height / 8), 0, src.format);
            half.filterMode = quarter.filterMode = eighth.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, half);
            Graphics.Blit(half, quarter);
            Graphics.Blit(quarter, eighth);
            Graphics.Blit(eighth, quarter);
            mat.SetTexture("_BlurTex", quarter);

            var v = Voyage.I;
            var ov = OceanView.I;
            float mist = v != null ? Mathf.Lerp(0.6f, 1f, v.MistLevel) : 1f;
            float clarity = ov != null ? ov.clarity : 0f;
            mat.SetFloat("_Haze", hazeStrength * mist * (1f - 0.8f * clarity));
            mat.SetColor("_VeilColor", RenderSettings.fogColor * 1.2f);
            mat.SetFloat("_Grain", grain);
            mat.SetFloat("_Vignette", vignette);
            mat.SetFloat("_GrainTime", Mathf.Floor(Time.time * 12f));
            mat.SetColor("_ShadowTint", new Color(0.02f, 0.05f, 0.18f, 0.55f));
            mat.SetColor("_HighTint", new Color(0.10f, 0.06f, -0.04f, 0.35f));
            Graphics.Blit(src, dst, mat);

            RenderTexture.ReleaseTemporary(half);
            RenderTexture.ReleaseTemporary(quarter);
            RenderTexture.ReleaseTemporary(eighth);
        }
    }
}

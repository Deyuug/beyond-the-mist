using UnityEngine;

namespace ARR
{
    /// Drives the GPU starlight mist (StardustSim.compute + Stardust.shader).
    /// Equivalent of the brief's VFX Graph setup, done with a compute shader so it runs in the built-in pipeline:
    /// curl-noise turbulence + ribbon attraction + a breath air-flow field (BreathFlow) the dust is dragged by.
    public class Stardust : MonoBehaviour
    {
        public static Stardust I;
        public int count = 98304;
        public float brightness = 0.8f;

        ComputeShader cs;
        ComputeBuffer buffer;
        Material mat;
        int kInit, kStep;
        bool supported;
        const int Stride = 16 * 4;

        public void Init()
        {
            I = this;
            BreathFlow.Reset();
            supported = SystemInfo.supportsComputeShaders;
            cs = Resources.Load<ComputeShader>("ARR/StardustSim");
            if (!supported || cs == null) { Debug.LogWarning("[ARR] compute shaders unavailable: starlight mist disabled"); supported = false; return; }
            buffer = new ComputeBuffer(count, Stride);
            kInit = cs.FindKernel("Init");
            kStep = cs.FindKernel("Step");
            mat = new Material(ArrArt.Shader("Stardust"));
            mat.SetTexture("_MainTex", ArrArt.SoftDot);
            mat.SetBuffer("_Particles", buffer);
            SetCommon(0f);
            cs.SetBuffer(kInit, "_Particles", buffer);
            cs.Dispatch(kInit, Mathf.CeilToInt(count / 256f), 1, 1);
        }

        void OnDestroy()
        {
            buffer?.Release();
            buffer = null;
            if (I == this) I = null;
        }


        public static Vector3 Bow(Voyage v) => v.boatPos + v.Forward * 2.6f + Vector3.up * 1.3f;

        void SetCommon(float dt)
        {
            var v = Voyage.I;
            Vector3 fwd = v.Forward, right = Vector3.Cross(Vector3.up, fwd);
            cs.SetInt("_Count", count);
            cs.SetFloat("_Time", Time.time);
            cs.SetFloat("_Dt", Mathf.Min(dt, 0.05f));
            cs.SetVector("_Boat", v.boatPos);
            cs.SetVector("_Fwd", fwd);
            cs.SetVector("_Right", right);
            cs.SetFloat("_Calm", v.paused || v.phase == Phase.Ended ? 1f : 0f);
            cs.SetFloat("_RibbonScale", 0.011f);
            cs.SetFloat("_FlowSpeed", 0.9f);
            cs.SetFloat("_Turbulence", 0.5f);
            BreathFlow.Push(cs);
            // the air around the boat breathes: out on the out-breath, gently in on the in-breath
            cs.SetVector("_Radial", new Vector4(v.boatPos.x, 1.2f, v.boatPos.z, BreathFlow.RadialSpeed));
            cs.SetFloat("_RadialRadius", 30f);
        }

        void Update()
        {
            if (Voyage.I == null) return;
            BreathFlow.Tick(Bow(Voyage.I), Voyage.I.Forward, Voyage.I.currentSpeed * Voyage.I.demoSpeed);
            if (!supported) return;
            SetCommon(Time.deltaTime);
            cs.SetBuffer(kStep, "_Particles", buffer);
            cs.Dispatch(kStep, Mathf.CeilToInt(count / 256f), 1, 1);

            var cam = OceanView.I != null ? OceanView.I.cam : null;
            if (cam == null || !cam.isActiveAndEnabled) return;
            Vector3 vp = cam.WorldToViewportPoint(Voyage.I.boatPos + Vector3.up * 2.5f);
            mat.SetVector("_BoatScreen", new Vector4(vp.x * 2 - 1, vp.y * 2 - 1, cam.aspect, 0));
            mat.SetFloat("_Brightness", brightness * Mathf.Lerp(0.85f, 1f, Voyage.I.MistLevel));
            mat.SetFloat("_MinPixels", 1.6f);
            var rp = new RenderParams(mat)
            {
                camera = cam,
                worldBounds = new Bounds(Voyage.I.boatPos, Vector3.one * 600f),
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = false,
            };
            Graphics.RenderPrimitives(rp, MeshTopology.Triangles, count * 6);
        }
    }
}

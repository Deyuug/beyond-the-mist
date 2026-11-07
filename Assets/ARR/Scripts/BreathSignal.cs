using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ARR
{
    public enum BreathSource { Simulated, Microphone, Udp }

    /// Turns a breath-like sound into gentle, fixed-size "breath pulses" for the starlight particles.
    ///
    /// Microphone pipeline: RMS -> noise-floor calibration -> 0..1 level -> hysteresis + cooldown -> pulse.
    /// Every pulse has the same amplitude however loud the sound was, so nothing rewards breathing harder.
    /// The pulse never affects boat speed or discovery. A microphone cannot measure real airflow: this is a
    /// non-medical demo signal. Space (or the parent's button) always triggers a pulse as a manual fallback.
    public class BreathSignal : MonoBehaviour
    {
        public static BreathSignal I;
        public const int UdpPort = 9000;

        public BreathSource source = BreathSource.Simulated;

        [Header("Output")]
        public float level;        // smoothed 0..1 input level (for display only)
        public float pulse;        // 0..1, set to 1 on each breath event, then fades
        public event Action Pulsed;

        [Header("Microphone calibration")]
        public float noiseFloor = 0.006f;
        public float activeLevel = 0.04f;
        public float triggerThreshold = 0.4f;
        public float releaseThreshold = 0.18f;
        public float cooldownSeconds = 1.5f;
        public float pulseDecayPerSecond = 0.45f;

        public readonly float[] history = new float[120];   // pulse history ~16 s at 7.5 Hz (the parent's waveform)
        public int historyHead;
        float histTimer;

        bool armed = true;
        float lastTrigger = -100f;
        float nextSimulated = 2f;

        // microphone
        AudioClip micClip;
        string micDevice;
        float[] micBuf;
        const int Window = 1024;
        float calibrateUntil;
        float calibMax;

        // udp
        UdpClient udp;
        Thread udpThread;
        volatile float udpValue;
        long udpTicks;

        public string SourceLabel
        {
            get
            {
                switch (source)
                {
                    case BreathSource.Microphone:
                        if (micDevice == null) return "Microphone (no device found)";
                        return Time.time < calibrateUntil ? "Microphone · calibrating room noise…" : "Microphone";
                    case BreathSource.Udp:
                        bool fresh = DateTime.UtcNow.Ticks - Interlocked.Read(ref udpTicks) < TimeSpan.TicksPerSecond * 2;
                        return fresh ? $"UDP:{UdpPort} connected" : $"UDP:{UdpPort} waiting for data";
                    default: return "Simulated natural breathing";
                }
            }
        }

        void Awake() { I = this; }

        /// The simulated child: their own resting pace with natural variation, gently drawn toward the guide's
        /// out-breath when it is on (children tend to fall in with a calm visual rhythm), plus the occasional
        /// quick extra breath or longer pause, so the demo shows the mist settling as the rhythm steadies.
        float simPeriod;
        void ScheduleSimulated(float now)
        {
            if (simPeriod <= 0f) simPeriod = UnityEngine.Random.Range(4.4f, 5.6f);
            simPeriod = Mathf.Clamp(simPeriod + UnityEngine.Random.Range(-0.1f, 0.1f), 3.5f, 6.5f);
            float next = now + simPeriod * UnityEngine.Random.Range(0.92f, 1.08f);
            var g = BreathGuide.I;
            if (g != null && g.on)
            {
                float guideNext = now + g.TimeToNextExhale + BreathGuide.DetectLag;
                if (Mathf.Abs(guideNext - next) < simPeriod * 0.5f) next = Mathf.Lerp(next, guideNext, 0.5f);
            }
            float r = UnityEngine.Random.value;
            if (r < 0.04f) next = now + simPeriod * 0.45f;        // a quick extra breath
            else if (r < 0.08f) next = now + simPeriod * 1.7f;    // a longer pause
            nextSimulated = next;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            StopMic();
            StopUdp();
        }

        public void CycleSource() => SetSource((BreathSource)(((int)source + 1) % 3));

        public void SetSource(BreathSource s)
        {
            if (source == BreathSource.Microphone) StopMic();
            if (source == BreathSource.Udp) StopUdp();
            source = s;
            level = 0;
            armed = true;
            if (s == BreathSource.Microphone) StartMic();
            if (s == BreathSource.Udp) StartUdp();
        }

        /// One detected breath (also the Space-key fallback). Only its timing is used downstream.
        public void TriggerPulse()
        {
            pulse = 1f;
            lastTrigger = Time.time;
            Pulsed?.Invoke();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.B)) CycleSource();
            if (Input.GetKeyDown(KeyCode.Space)) TriggerPulse();

            float target = 0f;
            switch (source)
            {
                case BreathSource.Simulated:
                    if (Time.time >= nextSimulated)
                    {
                        TriggerPulse();
                        ScheduleSimulated(Time.time);
                    }
                    target = pulse;
                    break;
                case BreathSource.Microphone:
                    target = MicLevel();
                    break;
                case BreathSource.Udp:
                    target = Mathf.Clamp01(udpValue);
                    break;
            }
            level = Mathf.Lerp(level, target, 1f - Mathf.Exp(-6f * dt));

            if (source != BreathSource.Simulated)
            {
                // hysteresis + cooldown: one sound event = one pulse, a continuous noise does not re-trigger
                if (level < releaseThreshold) armed = true;
                if (armed && level >= triggerThreshold && Time.time - lastTrigger >= cooldownSeconds)
                {
                    armed = false;
                    TriggerPulse();
                }
            }

            pulse = Mathf.MoveTowards(pulse, 0f, dt * pulseDecayPerSecond);

            histTimer += dt;
            while (histTimer > 1f / 7.5f)
            {
                histTimer -= 1f / 7.5f;
                history[historyHead] = Mathf.Max(pulse, source == BreathSource.Simulated ? 0 : level * 0.6f);
                historyHead = (historyHead + 1) % history.Length;
            }
        }

        // ---------------- microphone ----------------
        void StartMic()
        {
            if (Microphone.devices.Length == 0) { micDevice = null; return; }
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 2, 44100);
            micBuf = micClip != null ? new float[Window * micClip.channels] : null;
            // measure the room (and a running nebuliser) for two seconds before reacting
            calibrateUntil = Time.time + 2f;
            calibMax = 0f;
        }

        void StopMic()
        {
            if (!string.IsNullOrEmpty(micDevice) && Microphone.IsRecording(micDevice)) Microphone.End(micDevice);
            micDevice = null;
            micClip = null;
        }

        float MicLevel()
        {
            if (micClip == null || micBuf == null) return 0f;
            int pos = Microphone.GetPosition(micDevice);
            if (pos <= 0) return 0f;
            int start = (pos - Window + micClip.samples) % micClip.samples;
            if (!micClip.GetData(micBuf, start)) return 0f;
            double sum = 0;
            for (int i = 0; i < micBuf.Length; i++) sum += micBuf[i] * micBuf[i];
            float rms = Mathf.Sqrt((float)(sum / micBuf.Length));

            if (Time.time < calibrateUntil)
            {
                calibMax = Mathf.Max(calibMax, rms);
                noiseFloor = calibMax * 1.3f + 0.002f;
                activeLevel = Mathf.Max(noiseFloor * 4f, noiseFloor + 0.02f);
                return 0f;
            }
            return Mathf.Clamp01((rms - noiseFloor) / Mathf.Max(0.001f, activeLevel - noiseFloor));
        }

        // ---------------- UDP: plain-text 0..1 levels, e.g. "0.42" or "/breath 0.42" ----------------
        void StartUdp()
        {
            try
            {
                udp = new UdpClient(UdpPort);
                udpThread = new Thread(UdpLoop) { IsBackground = true };
                udpThread.Start();
            }
            catch (Exception e) { Debug.LogWarning("[ARR] UDP breath input unavailable: " + e.Message); udp = null; }
        }

        void UdpLoop()
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            while (udp != null)
            {
                try
                {
                    string s = Encoding.ASCII.GetString(udp.Receive(ref ep)).Trim();
                    int sp = s.LastIndexOf(' ');
                    if (sp >= 0) s = s.Substring(sp + 1);
                    if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                    {
                        udpValue = v;
                        Interlocked.Exchange(ref udpTicks, DateTime.UtcNow.Ticks);
                    }
                }
                catch { return; }
            }
        }

        void StopUdp()
        {
            var u = udp;
            udp = null;
            try { u?.Close(); } catch { }
            udpThread = null;
        }
    }
}

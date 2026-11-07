using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    public enum Phase { Ready, Sailing, Ended }

    public class Surprise
    {
        public int id;
        public string name;          // shown to the parent
        public string childLine;     // shown to the child when it appears
        public Vector3 pos;
        public Color color;
        public float progress;       // 0..1 shared discovery progress (mist lifts with it)
        public bool found;
        public float foundAt;
    }

    /// Shared state of "Beyond the Mist".
    ///   Child     steers the boat (one-handed joystick, left/right only; the boat moves forward by itself)
    ///   Parent    sees the hidden surprises and turns the lighthouse beam toward them
    ///   Clinician starts, pauses and ends; never scored, never extended by the game
    /// Breathing is deliberately NOT part of this state: it only drives the starlight particles.
    public class Voyage : MonoBehaviour
    {
        public static Voyage I;

        // ---------- world ----------
        public Vector3 lighthouse = Vector3.zero;
        public const float LighthouseHeight = 16f;
        public const float BeamLength = 260f;
        public readonly List<Surprise> surprises = new List<Surprise>();

        // ---------- treatment / clinician ----------
        public Phase phase = Phase.Ready;
        public bool paused;
        public float plannedDuration = 300f;
        public float elapsed;
        public float demoSpeed = 1f;
        public bool scripted;                 // autoplay / promo drives the inputs instead of the devices

        // ---------- child: boat ----------
        // starts south of the lighthouse, facing it, so the light is in view from the first second
        public Vector3 boatPos = new Vector3(-30, 0, -55);
        public float heading = 12f;           // degrees clockwise from +Z
        public float steer;                    // -1..1 from the joystick
        public float cruiseSpeed = 4.2f;
        public float currentSpeed;
        public const float TurnRate = 28f;
        public Vector3 Forward => Dir(heading);

        // ---------- parent: lighthouse beam ----------
        public float beamAngle = 0f;           // degrees clockwise from +Z; starts straight ahead of the boat, at nothing
        public float beamInput;                // -1..1 from keys / buttons
        public Vector3 BeamDir => Dir(beamAngle);

        // ---------- cooperation, recomputed every frame ----------
        public int aimed = -1;                 // surprise the beam is pointing at, or -1
        public bool Guided => aimed >= 0;
        public bool following;                 // the boat is sailing along / toward the light
        public const float AimTolerance = 7f;

        // ---------- after a discovery the boat drops anchor to enjoy it ----------
        public bool anchored;
        public int anchoredAt = -1;           // surprise the boat is resting beside
        float anchorSince;
        public const float AnchorMinSeconds = 6f;
        public bool CanSetSail => anchored && elapsed - anchorSince > AnchorMinSeconds;

        public int FoundCount { get { int c = 0; foreach (var s in surprises) if (s.found) c++; return c; } }
        public bool AllFound => FoundCount == surprises.Count;
        public float MistLevel => 1f - 0.65f * FoundCount / Mathf.Max(1, surprises.Count);

        public event Action<Surprise> SurpriseFound;
        public event Action<string> ChildMessage;
        public event Action<Phase> PhaseChanged;

        public static Vector3 Dir(float deg) => new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad), 0, Mathf.Cos(deg * Mathf.Deg2Rad));

        void Awake()
        {
            I = this;
            AddSurprise("Little Turtle", "A glowing little turtle swims out!", 28f, 165f, new Color(0.45f, 0.95f, 0.8f));
            AddSurprise("Starlight Jellyfish", "Starlight jellyfish are dancing in the mist!", -68f, 185f, new Color(0.85f, 0.6f, 1f));
            AddSurprise("Treasure Chest", "You found a little treasure chest!", 118f, 170f, new Color(1f, 0.82f, 0.45f));
            AddSurprise("Coral Island", "A glowing coral island!", -150f, 200f, new Color(1f, 0.55f, 0.75f));
        }

        void OnDestroy() { if (I == this) I = null; }

        void AddSurprise(string name, string line, float bearing, float dist, Color c)
        {
            surprises.Add(new Surprise { id = surprises.Count, name = name, childLine = line, pos = lighthouse + Dir(bearing) * dist, color = c });
        }

        public float BearingFromLighthouse(Vector3 p)
        {
            Vector3 d = p - lighthouse;
            return Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
        }

        // ================= clinician =================
        public void StartVoyage()
        {
            if (phase != Phase.Ready) return;
            phase = Phase.Sailing;
            PhaseChanged?.Invoke(phase);
            ChildMessage?.Invoke("Off we go! Follow the golden light");
        }

        public void TogglePause()
        {
            if (phase != Phase.Sailing) return;
            paused = !paused;
            ChildMessage?.Invoke(paused ? "The boat is taking a little rest" : "Sailing on");
        }

        public void EndTreatment()
        {
            if (phase == Phase.Ended) return;
            phase = Phase.Ended;
            paused = false;
            PhaseChanged?.Invoke(phase);
        }

        // ================= inputs =================
        public void SetSteer(float v) => steer = Mathf.Clamp(v, -1f, 1f);
        public void SetBeamInput(float v) => beamInput = Mathf.Clamp(v, -1f, 1f);
        public void SetBeamAngle(float deg) => beamAngle = Mathf.Repeat(deg, 360f);

        // ================= simulation =================
        void Update()
        {
            float realDt = Time.deltaTime;
            float dt = realDt * demoSpeed;

            // the parent can move the light at any time, even before the voyage starts
            if (Mathf.Abs(beamInput) > 0.01f) beamAngle = Mathf.Repeat(beamAngle + beamInput * 40f * realDt, 360f);
            UpdateAim();

            bool sailing = phase == Phase.Sailing && !paused;
            if (sailing) elapsed += dt;

            // anchored beside a discovery: sail on once the parent's light points at a new hidden place
            if (sailing && anchored && CanSetSail && Guided)
            {
                anchored = false;
                anchoredAt = -1;
                ChildMessage?.Invoke("The light points somewhere new. Let's go!");
            }

            bool moving = sailing && !anchored;
            float targetSpeed = moving ? cruiseSpeed : 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (moving ? 1.2f : 1.6f) * dt);
            if (sailing)
            {
                float turn = anchored ? 0.5f : 1f;   // at anchor the child can still turn slowly to look around
                heading = Mathf.Repeat(heading + steer * TurnRate * turn * realDt, 360f);
                if (anchored && Mathf.Abs(steer) < 0.1f && anchoredAt >= 0 && elapsed - anchorSince < 4f)
                {
                    // gently swing the bow toward the new friend
                    Vector3 to = surprises[anchoredAt].pos - boatPos;
                    // a little to its left, so the friend appears beside the sail rather than behind it
                    heading = Mathf.MoveTowardsAngle(heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - 16f, 14f * dt);
                }
                KeepInsideSea(dt);
            }
            boatPos += Forward * currentSpeed * dt;
            AvoidIsland();

            following = moving && IsFollowingLight();
            if (moving) UpdateDiscovery(dt);
        }

        void UpdateAim()
        {
            aimed = -1;
            float best = AimTolerance;
            foreach (var s in surprises)
            {
                if (s.found) continue;
                float diff = Mathf.Abs(Mathf.DeltaAngle(beamAngle, BearingFromLighthouse(s.pos)));
                if (diff < best) { best = diff; aimed = s.id; }
            }
        }

        /// The point on the beam the boat should be heading for: a little ahead of the boat's projection onto it.
        public Vector3 BeamAimPoint()
        {
            Vector3 d = BeamDir;
            float t = Vector3.Dot(boatPos - lighthouse, d);
            // the further the boat is from the lighthouse, the further along the light it should aim:
            // leaving one surprise for the next, it cuts across to meet the new light instead of turning back home
            Vector3 off = boatPos - lighthouse; off.y = 0;
            float minAlong = Mathf.Clamp(off.magnitude * 0.6f, 30f, BeamLength);
            t = Mathf.Clamp(t + 45f, minAlong, BeamLength);
            return lighthouse + d * t;
        }

        bool IsFollowingLight()
        {
            Vector3 to = BeamAimPoint() - boatPos; to.y = 0;
            if (to.sqrMagnitude < 1f) return true;
            float diff = Mathf.Abs(Mathf.DeltaAngle(heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg));
            return diff < 28f;
        }

        void UpdateDiscovery(float dt)
        {
            if (aimed < 0) return;
            var s = surprises[aimed];
            float dist = Vector3.Distance(Flat(boatPos), Flat(s.pos));
            // mist around the target thins as the boat comes closer, but only while the two of them cooperate
            float closeness = Mathf.Clamp01(1f - (dist - 18f) / 120f);
            if (following && closeness > s.progress) s.progress = Mathf.MoveTowards(s.progress, closeness, dt * 0.22f);
            if (s.progress >= 0.92f || dist < 16f) Find(s);
        }

        public void Find(Surprise s)
        {
            if (s.found) return;
            s.found = true;
            s.progress = 1f;
            s.foundAt = Time.time;
            anchored = true;
            anchoredAt = s.id;
            anchorSince = elapsed;
            SurpriseFound?.Invoke(s);
            ChildMessage?.Invoke(s.childLine);
            ChildMessage?.Invoke(AllFound ? "The whole starlight sea is glowing" : "Let's stop and say hello");
        }

        /// Used by scripted cuts (promo) to release the anchor.
        public void Unanchor() { anchored = false; anchoredAt = -1; }

        void AvoidIsland()
        {
            // the lighthouse island is solid: the boat slides around it instead of passing through
            Vector3 d = boatPos - lighthouse; d.y = 0;
            const float r = 24f;
            if (d.magnitude < r && d.sqrMagnitude > 0.01f) boatPos = lighthouse + d.normalized * r;
        }

        void KeepInsideSea(float dt)
        {
            // a soft current turns the boat back toward the lighthouse if it drifts too far: no way to get lost
            Vector3 d = boatPos - lighthouse; d.y = 0;
            if (d.magnitude > 300f)
            {
                float home = Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;
                heading = Mathf.MoveTowardsAngle(heading, home, 15f * dt);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}

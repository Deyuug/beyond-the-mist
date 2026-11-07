using System.Collections.Generic;
using UnityEngine;

namespace ARR
{
    /// The breathing guide (the ring on the joystick knob) and the slow "calm level" that clears the mist.
    ///
    /// The mist responds to how steadily the child breathes WITH the guide over several breaths; never to a
    /// single exhale, its strength, or how many there are. The aim is a comfortable, stable rhythm.
    ///  - The guide follows the child first: its pace adapts to the child's own measured pace (anywhere from a
    ///    young child's natural resting pace to a slow one; the values below are demo defaults the clinical team
    ///    should set), and its phase gently locks onto the child's out-breaths, so the child never has to chase
    ///    the ring and never has to slow down: steady breathing at their own pace clears the mist.
    ///  - Match, per guide cycle (timing only: loudness, strength and length are ignored):
    ///      one good breath: as the shrink begins (from just before it to about half way) and
    ///      about one guide period after the last breath                                   → 1
    ///      one on the beat but the interval uneven (the ring follows the child, so a lucky
    ///      hit is easy; it is the steady interval that makes a rhythm)                    → 0.5
    ///      one on the beat plus another calm breath (breathing a bit faster)  → 0.4
    ///      a breath only off the beat (the ring then moves toward it)         → 0.2
    ///      no breath detected this cycle                                      → 0.2
    ///      an extra breath that comes too soon (rushing)                      → 0.1
    ///    The guide only adapts its pace to breaths in the comfortable range, never to rushed breathing.
    ///  - Each cycle's score first goes through the median of the last 3 cycles, so a single odd breath (a quick
    ///    extra one, a longer pause) barely matters, while two or more cycles in a row off the ring do.
    ///  - The calm level (how clear the mist is) moves in steps: each good breath that follows another good one
    ///    (a rhythm, not a lucky hit; the very first breath, or the first after a long pause, counts on its own) Each such breath clears the mist one
    ///    Step further (about five clear it completely) and it stays clear while the rhythm goes on; it is never
    ///    blown fully open by a single breath. A step can only go a little beyond what the match so far supports,
    ///    so a lucky breath now and then while breathing unevenly clears little.
    ///  - Once the rhythm is lost (no good breath for ~2.4 guide periods, or no breath at all for ~8 s) the mist
    ///    slowly comes back, and the
    ///    match decides how far: after a long steady stretch one odd cycle barely shows, a few cycles off the ring
    ///    bring part of it back, uneven breathing brings it all back. Nothing is shown as a score or a failure.
    ///  - Guide off: a breath counts as "in rhythm" when the breathing is regular (the match follows regularity).
    ///    No reliable breath signal: the mist settles back to a fairly thick default, the game stays playable.
    ///  - Every breath (not one that comes too soon) also gets the same starlight response and a little air at
    ///    the bow (BreathFlow); off the beat that air thins the mist only half as much, and only for a moment.
    /// Ring grows = breathe in; ring shrinks = breathe out. The clinician can switch the guide off (button or G).
    public class BreathGuide : MonoBehaviour
    {
        public static BreathGuide I;

        public enum Stage { Inhale, Exhale, Rest }

        public bool on = true;

        // the range the guide follows the child within, in seconds per breath: about 9–30 breaths a minute, from a
        // young child's natural resting pace to a slow, calm one. The ring follows the child's own pace inside it
        // (it never asks them to slow down); it starts at DefaultPeriod and adapts within a few breaths.
        // Demo defaults only: the clinical team should set these for each child.
        public const float MinPeriod = 2.0f, MaxPeriod = 6.5f;
        public const float DefaultPeriod = 5.0f;
        // a child's own natural resting range, used when the guide is off (steadiness only)
        const float NaturalMin = MinPeriod, NaturalMax = MaxPeriod;
        const float InhaleFrac = 1.3f / 3.9f, ExhaleFrac = 2.2f / 3.9f;
        /// a breath is usually detected a moment after the out-breath begins
        public const float DetectLag = 0.35f;
        /// with no breath detected for a while, the mist settles back to a fairly thick default
        public const float NoSignalCalm = 0.15f;
        /// one breath in rhythm clears the mist this much further (about five clear it completely)
        public const float Step = 0.2f;
        const float StepRate = 0.4f;          // a step is applied over about half a second, never a pop
        const float ReturnRate = 1f / 10f;    // how fast the mist comes back once the rhythm is lost, per second

        public float period { get; private set; } = DefaultPeriod;
        public float InhaleSeconds => period * InhaleFrac;
        public float ExhaleSeconds => period * ExhaleFrac;
        public float RestSeconds => period - InhaleSeconds - ExhaleSeconds;

        public Stage stage { get; private set; }
        public float stageProgress { get; private set; }   // 0..1 within the current stage

        /// 0..1, slow: how calm and steady the breathing has been. Drives the mist; never displayed.
        public float Calm { get; private set; }
        /// averaged match with the guide (or regularity when the guide is off), 0..1
        public float Match { get; private set; }
        public bool SignalLost => Time.time - lastBreath > Mathf.Max(1.6f * period, 6f);

        /// for tests: the last cycle's tally "onBeat/offBeat/tooSoon"
        public string LastCycle { get; private set; } = "";
        float t, phaseCorrection;
        float lastBreath = -100f, lastRipple = -100f, firstBreath = -1f;
        float stepGoal, holdUntil = -100f;
        bool prevGood;    // the previous breath was a good one
        float prevGoodGap;
        int offInARow;    // breaths in a row that missed the beat
        bool rhythmHit;   // this cycle's on-beat breath was a good one (about one period after the last)
        int onBeat, offBeat, tooSoon;
        readonly List<float> intervals = new List<float>();
        readonly List<float> recentScores = new List<float>();

        void Awake() { I = this; }
        void Start() { if (BreathSignal.I != null) BreathSignal.I.Pulsed += OnBreath; }

        void OnDestroy()
        {
            if (I == this) I = null;
            if (BreathSignal.I != null) BreathSignal.I.Pulsed -= OnBreath;
        }

        public void Toggle() => on = !on;

        /// For scripted recordings only: start a scene from a given calm level.
        public void SetCalm(float calm, float match) { Calm = stepGoal = calm; Match = match; holdUntil = -100f; }

        /// How clear the match alone keeps the mist once the rhythm is lost: uneven breathing (match ~0.3 or
        /// less) lets it all come back, a long steady stretch (~0.9) keeps it clear.
        static float Settled(float match) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.9f, match));

        bool Calibrating => firstBreath < 0f || Time.time - firstBreath < 30f || intervals.Count < 4;

        /// Seconds until the ring next starts to shrink.
        public float TimeToNextExhale => t < InhaleSeconds ? InhaleSeconds - t : period - t + InhaleSeconds;

        // ---------------------------------------------------------------- breaths
        void OnBreath()
        {
            float now = Time.time;
            float gap = now - lastBreath;
            // rushed = a quick extra breath, measured against the child's own pace (which the period follows)
            bool rushed = lastBreath > 0f && gap < Mathf.Max(1.2f, 0.55f * period);
            if (lastBreath > 0f && gap > 1.0f && gap < MaxPeriod * 2.2f)
            {
                intervals.Add(gap);
                if (intervals.Count > 6) intervals.RemoveAt(0);
            }
            bool first = lastBreath < 0f || gap > 2.2f * period;   // the first breath, or the first after a pause
            if (firstBreath < 0f) firstBreath = now;
            lastBreath = now;

            bool good = false, inRhythm = false;
            if (on)
            {
                if (rushed) tooSoon++;   // no phase change, no credit
                else
                {
                    // where in the guide cycle did it land? (timing only, with generous margins)
                    float exStart = InhaleSeconds;
                    bool onTheBeat = t >= exStart - 0.4f && t <= exStart + 0.55f * ExhaleSeconds;
                    if (onTheBeat) onBeat++; else offBeat++;
                    offInARow = onTheBeat ? 0 : offInARow + 1;
                    // good: on the beat AND about one period after the last breath; in rhythm: a good breath after
                    // another good one (the ring follows the child, so one lucky hit is easy; two in a row is a rhythm)
                    good = onTheBeat && (first || Mathf.Abs(gap - period) < 0.25f * period);
                    // ...and about as long as the last good one: an even rhythm, not two lucky hits
                    inRhythm = good && (first || (prevGood && Mathf.Abs(gap - prevGoodGap) < 0.2f * period));
                    if (good) prevGoodGap = first ? period : gap;
                    if (good) rhythmHit = true;
                    // follow the child: nudge the ring's phase toward this out-breath (applied smoothly in Update)
                    float err = Mathf.Repeat(t - exStart - DetectLag + period * 0.5f, period) - period * 0.5f;
                    // (a breath half a period away is normally left alone, but after two misses in a row from a child
                    // breathing evenly the ring re-aligns with them even then, so it never stays out of step)
                    if (Mathf.Abs(err) < 0.45f * period) phaseCorrection -= err * (Calibrating ? 0.6f : 0.4f);
                    else if (offInARow >= 2 && Spread() < 0.15f) phaseCorrection -= err * 0.5f;
                }
            }
            else if (intervals.Count >= 3)
            {
                // guide off: steadiness alone
                Match = Mathf.Lerp(Match, Regularity(), 0.25f);
                good = inRhythm = !rushed && Regularity() >= 0.6f;
            }
            prevGood = good;

            // the same starlight and bow air for every breath, and nothing for a breath that comes too soon
            if (!rushed && now - lastRipple >= 1.5f)
            {
                lastRipple = now;
                BreathFlow.BeginExhale(inRhythm ? 1f : 0.5f);
            }

            // a breath in rhythm clears the mist one step further; any good breath keeps it clear for a while
            // (so one missed or odd breath barely shows)
            if (inRhythm)
            {
                float ceiling = Mathf.Min(1f, Settled(Match) + 2f * Step);
                stepGoal = Mathf.Max(stepGoal, Mathf.Min(Calm + Step, ceiling));
            }
            if (good) holdUntil = now + 2.4f * period;
        }

        /// 1 = even intervals at a comfortable pace; lower for uneven or rushed breathing.
        /// How uneven the recent intervals are: ~0.03 for steady breathing, ~0.3 for random. Median-based, so one
        /// longer pause or quick breath barely changes it.
        float Spread()
        {
            if (intervals.Count < 3) return 1f;
            float med = Median(intervals);
            return Median(intervals.ConvertAll(x => Mathf.Abs(x - med))) / med;
        }

        float Regularity()
        {
            float med = Median(intervals);
            float steady = Mathf.Clamp01(1f - Spread() / 0.2f);
            bool comfortable = med >= NaturalMin * 0.9f && med <= NaturalMax * 1.2f;
            return comfortable ? steady : steady * 0.4f;
        }

        static float Median(List<float> xs)
        {
            var s = new List<float>(xs);
            s.Sort();
            return s.Count % 2 == 1 ? s[s.Count / 2] : 0.5f * (s[s.Count / 2 - 1] + s[s.Count / 2]);
        }

        // ---------------------------------------------------------------- cycle
        void EndCycle()
        {
            if (on && lastBreath > 0f)
            {
                float score = tooSoon > 0 || onBeat > 1 ? 0.1f
                            : onBeat == 1 ? (offBeat > 0 ? 0.4f : rhythmHit ? 1f : 0.5f)
                            : 0.2f;   // only off the beat, or no breath detected this cycle
                recentScores.Add(score);
                if (recentScores.Count > 3) recentScores.RemoveAt(0);
                Match = Mathf.Lerp(Match, Median(recentScores), 0.35f);
            }
            // follow the child's own pace (never rushed breaths), between cycles so the ring never jumps: quickly
            // at first, then gently, but faster again when the child's pace has clearly changed. Also while the
            // guide is off, so "too soon" is always judged against the child's own pace.
            var comfortable = intervals.FindAll(x => x >= MinPeriod && x <= MaxPeriod);
            if (comfortable.Count >= 3)
            {
                float target = Median(comfortable);
                // faster only when the child's own pace is even and has clearly changed (never chasing uneven breaths)
                float rate = Calibrating ? 0.6f : Mathf.Abs(target - period) > 0.1f * period && Spread() < 0.12f ? 0.35f : 0.12f;
                period = Mathf.Lerp(period, target, rate);
            }
            LastCycle = $"{onBeat}/{offBeat}/{tooSoon}";
            onBeat = offBeat = tooSoon = 0;
            rhythmHit = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G)) Toggle();
            float dt = Time.deltaTime;

            // apply the phase nudge as a brief, gentle change of speed (0.4x–1.8x): the ring never jumps or reverses
            float step = Mathf.Clamp(phaseCorrection, -0.6f * dt, 0.8f * dt);
            phaseCorrection -= step;
            t += dt + step;
            if (t >= period) { t -= period; EndCycle(); }

            if (t < InhaleSeconds) { stage = Stage.Inhale; stageProgress = t / InhaleSeconds; }
            else if (t < InhaleSeconds + ExhaleSeconds) { stage = Stage.Exhale; stageProgress = (t - InhaleSeconds) / ExhaleSeconds; }
            else { stage = Stage.Rest; stageProgress = Mathf.Clamp01((t - InhaleSeconds - ExhaleSeconds) / RestSeconds); }

            // the calm level: up only in steps (breaths in rhythm); once the rhythm is lost it slowly settles back
            // to what the match supports (or a thick default with no breath signal), never a sudden change
            if (Calm < stepGoal) Calm = Mathf.MoveTowards(Calm, stepGoal, dt * StepRate);
            else if (Time.time > holdUntil || SignalLost)
            {
                float floor = Settled(Match);
                if (SignalLost) floor = Mathf.Min(floor, NoSignalCalm);
                if (Calm > floor) Calm = Mathf.MoveTowards(Calm, floor, dt * ReturnRate);
                stepGoal = Calm;
            }
        }
    }
}

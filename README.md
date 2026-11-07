# Beyond the Mist — ARR Hackathon 2026 Unity prototype

**English** | [中文](README.zh-CN.md)

**One light, one boat, one shared discovery.** During nebuliser treatment, a child faces a big screen and uses a one-hand joystick to sail a small boat through a misty night sea. On a tablet, the parent sees hidden surprises the child cannot see, and turns a lighthouse beam to light the way. When the child breathes steadily with the breath reticle in the middle of the screen, the sea mist slowly clears; breathing more often or harder never clears it faster. The interface is in English.

> The parent can show the way, but only the child can steer the ship.

![Sailing: the child's big screen on the left, the parent's treasure chart on the right](docs/screenshots/03_revealing_0.png)

## Background and team

Built in 48 hours at the [ARR Hackathon](https://www.scivr.co.uk/arr-hackathon) (Asymmetric Reframed Reality, run by Goldsmiths SEE VR Lab with King's College London), 4–6 November 2026.

- **The child** captains a small sailing boat through a misty night sea on a big screen, steering with a one-hand joystick.
- **The parent** sees a different view, a treasure chart on a tablet, and turns a lighthouse beam to show the way. They can show the way, but only the child can steer the ship.
- **Breathing** calmly, in rhythm with a soft ring in the middle of the screen, clears the mist step by step. Breathing harder or longer never helps. There are no scores.
- **Clinicians** start, pause and end the session. The game never extends the treatment.

Downloads for Windows and macOS are on the [Releases](../../releases) page. Editor version Unity 6000.3.28f1; everything except the background music is generated from code.

**Team:** [Chengyan Tong](https://github.com/RowanXB) (team lead, Unity build), Ruohao Sun, [Yutong Deng](https://github.com/Deyuug) and Junyi Wang. The idea and direction came from the whole team; the Unity prototype was built by Chengyan Tong, almost entirely with Claude Code as pair programmer, iterating on teammates' playtest feedback. The breathing in the demo is simulated: the prototype "hears" breathing through a microphone (sound, not airflow), so it is a non-medical demonstration only.



### Team Contributions

## Yutong Deng — Concept & Experience Design

- Contributed to the overall concept development and experience direction.
- Explored asymmetric interactions between the child, parent, and clinician.
- Developed user experience flows and interaction concepts for the immersive environment.
- Contributed visual research, design references, and project proposal materials.
- Participated in prototype evaluation and iterative design feedback.



## Demo video

[`docs/BeyondTheMist_demo.mp4`](docs/BeyondTheMist_demo.mp4): 75 seconds, 1080p, English captions. The sound is gentle background music over the game's soft sea sound, with a soft music-box chime when the Little Turtle and the Coral Island are found.

> Background music: "Silver Blue Light" by Kevin MacLeod (incompetech.com), Licensed under Creative Commons: By Attribution 4.0 License (http://creativecommons.org/licenses/by/4.0/). Include this attribution in the description whenever you publish the video (see `Tools/music/CREDITS.txt`). In order, the video shows: the three roles; breathing steadily with the breath reticle (starlight drawn in and pushed out with each breath); the thick mist clearing step by step; the parent turning the light while the child steers; finding the Little Turtle and stopping the boat; a clinician pause; more discoveries; the clinician ending the session.

To re-record (picture + game sound) and then mix in the background music:

```bash
"Builds/BeyondTheMist.app/Contents/MacOS/Beyond the Mist" -arrPromo /tmp/arr_promo
python3 Tools/make_promo.py /tmp/arr_promo /tmp/demo_nomusic.mp4
python3 Tools/swap_audio.py /tmp/demo_nomusic.mp4 /tmp/arr_promo/audio.wav "Tools/music/Silver Blue Light.mp3" docs/BeyondTheMist_demo.mp4
```

To change only the sound and keep the picture, use `-arrAudioOnly` to re-render just the game sound on the same timeline (a few tens of seconds), then replace the audio track with `swap_audio.py` (the picture is copied, not re-encoded).

## Running

- **macOS:** open `Builds/BeyondTheMist.app` (universal: Intel and Apple silicon).
- **Windows:** run `Builds/Windows/BeyondTheMist.exe` (64-bit).
- **Unity:** add this folder in Unity Hub (editor 6000.3.28f1), open `Assets/Scenes/BeyondTheMist.unity` and press Play. Everything except the background music is generated from code.
- **Rebuild:** menu `ARR → Build macOS App` or `ARR → Build Windows App`.

The default is a split screen: the child's big screen on the left, the parent's tablet on the right (with a separate clinician console at the bottom). Press `F1` to switch to a single full-screen view; projecting the child's view full screen onto a big display is recommended. With two displays connected, press `F2` or launch with `-dual`.

## A full session

1. **Ready:** the boat waits south of the lighthouse, bow pointing at it, so the golden light is on screen from the first second.
2. **Clinician starts:** the clinician presses Start and the boat begins to drift slowly forward on its own.
3. **Parent turns the light:** the parent drags on the chart (or uses `Q`/`E`) to aim the light at a hidden surprise (Little Turtle, Starlight Jellyfish, Treasure Chest, Coral Island).
4. **Child steers into the light:** the child uses the joystick to turn the boat towards the golden light path. The beam, its glow and the golden birds all shine through the mist, and the light path on the water glows in the mist too. Direction cues come mainly from the parent's light. Only when heading for a new destination with the bow more than 35° off the light path does a short arrow pointing to the light path appear on the child's screen; it fades about 1 second after the boat is back on course, and never shows during normal sailing.
5. **Progress together:** the mist around a surprise clears slowly only when the light is on it and the boat is following the light; then the surprise rises out of the sea. The small patch of sea where the lit surprise sits (about 20 m across) gives off a soft rippling glow, with a warm golden halo in the mist above it. The halo is computed along the line of sight, so even in thick mist it is not hidden by nearby mist layers, and following the light brings the destination into view. It grows brighter as the boat gets closer (brightest within 40 m) and fades about 3 seconds after the meeting; when the parent changes target, it fades at the old spot before lighting up at the new one.
6. **Stop to look:** the boat slows to a stop and turns its bow alongside the surprise so the child can take a good look. Once the parent turns the light towards the next surprise, the boat sets off again.
7. **Pause or end at any time:** the clinician can pause or end whenever needed. The end screen only says "Today you met…": **no counts, no scores**.

## Parent tablet

- **Treasure chart (left):** the lighthouse, the boat and its wake, the golden beam, and four hidden friends (marked "met" once found). Press and drag on the chart to point the light where your finger is; below it are the "Beam left / right" buttons and the beam angle.
- **Teamwork:** describes in words how the two of you are working together: whether the light is on a surprise, whether the child is following the light, how far the mist has cleared, and a hint about what to do next. No scores, progress bars or percentages.
- **Breath · clearing the mist:** a breath waveform showing every breath detected in roughly the last 16 seconds (live volume when using a microphone or sensor), with the signal source and a "Simulate breath" button underneath.
- **Hidden surprises:** the state of each of the four surprises (Hidden in mist / In the light / Mist clearing / Met).
- **Your child is steering:** the child's current heading and turn.
- **Clinician console (bottom):** planned length 3/5/8 minutes, timer, breath guide switch (Breath guide), Start/Pause, End, Reset.
- The top bar also has two demo buttons, Demo speed and Switch view.

## Breathing: helping the child find a comfortable, steady rhythm

The aim is not "breathe out more" but calm, steady breathing. So **the mist clears in steps, with the rhythm**: each breath out in rhythm clears one step (about 1/5); as long as the rhythm holds, the mist gets thinner each time until it is fully clear. Only when the rhythm is lost does the mist slowly return, and how much returns depends on the match. A single breath never clears the mist completely, and breathing more often, harder or longer never clears it faster.

**Breath reticle (centre of the screen):** no text, semi-transparent, slightly above centre (over the sea ahead of the boat, clear of the mast top), so the child's gaze rests naturally on the middle of the screen.
- A thin ring grows and shrinks with the breath and stays in one place, giving a steady point to look at.
- The small dot in the middle shifts gently left or right with the joystick, and the small arrow on the side being turned towards lights up to show the turn.
- About 60% opaque while sailing; about 25% when the boat stops to look at a friend, so the friend is not covered; hidden on the pause and end screens.
- **Ring grows = breathe in:** the ring opens and turns cool blue, and the specks of light around it are drawn into the ring.
- **Ring shrinks = breathe out:** the ring turns gold and contracts, and the specks of light flow upward out of the ring.
- **Pause:** a small gold ring rests for a moment, then the next breath in begins.

**The guide follows the child first** (`BreathGuide.cs`):
- The default pace is a slow 5 seconds per breath (about 12 breaths a minute; in : out : pause ≈ 1.7 : 2.8 : 0.5). In the first 30 seconds or so, the guide moves towards the child's own measured pace (the median of the last few breath intervals), but only within the range below.
- When the child breathes out, the ring's phase shifts a little towards the child's timing (by briefly speeding up or slowing down, never jumping or going backwards), so the child never has to chase the ring. If the child breathes evenly but misses the beat twice in a row (for example exactly half a beat off), the ring also realigns to the child.
- **The ring follows the child's own pace and never asks the child to slow down:** the follow range is 2–6.5 seconds per breath (about 9–30 breaths a minute), covering everything from a young child's everyday natural breathing to slower calm breathing (demo defaults; in real use clinicians should set these for the child's age and condition). In the first 30 seconds or so it catches up with the child quickly; after that, if the child's pace changes steadily, the ring follows within a few cycles; when breathing is uneven, the ring does not chase it. Extra breaths much faster than the child's own pace (shorter than about 55%) count as rushed, are not counted, and do not make the ring speed up. Breathing steadily at their own natural pace is all the child needs to clear the mist.

**Match** (timing only, never loudness, force or breath length): scored once at the end of each guide cycle.
- A breath just as the ring starts to shrink (from slightly before the shrink to halfway through it), about one cycle after the previous breath (±25%) → 1
- On the beat, but the interval is wrong (the ring follows the child, so hitting the beat by chance is easy; only a steady interval counts as rhythm) → 0.5
- One on the beat plus one calm extra breath (breathing a little fast) → 0.4
- Only off-beat breaths (the ring then moves towards the child) → 0.2
- No breath detected this cycle → 0.2
- An extra breath too close to the previous one (rushed) → 0.1

Each cycle's score is first taken as the median of the last 3 cycles: an occasional extra breath or pause has almost no effect, and the match only drops after two or more cycles off the beat.

**How the mist clears and returns** ("calm", 0 thick mist … 1 fully clear):
- **Clears step by step:** a "good breath" is one on the beat and about one cycle after the previous one. After two good breaths in a row with similar intervals (within 20% of a cycle), each good breath from the second onward clears one step (0.2, smoothly over about 0.5 seconds); the first breath, or the first after a long pause, counts on its own. Following the rhythm steadily clears the mist completely in about 4–5 breaths; when the child breathes at their own faster natural pace, the ring takes a dozen seconds or so to catch up, then the mist clears step by step in the same way.
- **Guarding against lucky hits:** each step can go at most 0.4 above what the current match supports, so a single lucky hit clears very little.
- **The mist stays away while the rhythm holds:** each good breath holds the mist for about 2.4 cycles; an occasional extra breath or pause in between barely moves it.
- **The mist only returns when the rhythm is lost, and how much depends on the match:** after about 2.4 cycles with no good breath, or about 8 seconds with no breath detected at all, the mist slowly returns at 0.1 per second (about 8–10 seconds from fully clear to thick mist, never sudden), back to the level the match supports. One off cycle after a long steady stretch brings back almost no mist; several cycles off the beat bring back some; uneven breathing brings it all back.

Calm drives the mist layers over the sea, the distant fog and a soft blur over the whole screen: mist layer density ranges from 1.15× (thick) to 0.3× (clear), and distant fog from 1.35× to 0.4×. In thick mist the whole picture is noticeably softened (blur plus a light mist tint; the UI is unaffected), with the blur strength matching the 5 November 18:07 build; once the mist clears the picture is mostly sharp. The mist within about 3–9 m of the boat is a little thinner (up to 45% thinner) but never empty. Calm is **never shown** anywhere in the interface: no score, no "wrong" message; the child only sees the sea becoming quieter and clearer.
- **With the guide switched off:** evenly spaced breaths (within 2–6.5 seconds per breath) count as good breaths, and the match measures how even the breathing is instead. Evenness uses the median, so an occasional pause or extra breath has almost no effect.
- **When no breath is detected** (for about 8 seconds or more, for example when the child stops or breathes too quietly for the microphone): the mist slowly returns to a thicker default, and the game remains playable.

**Feedback for every breath** (`BreathFlow.cs` + `StardustSim.compute`): every breath gets the same response, so the child feels their breathing is seen.
- **Starlight drawn in and pushed out with each breath:** within about 30 m of the boat, floating starlight particles are pushed outward with trails as the child breathes out (about 2 seconds), and drawn back towards the boat as the ring breathes in. This only affects the starlight particles, not the mist.
- **A small airflow from the bow:** the bow sends out a very light forward airflow (an incompressible axisymmetric jet: axial velocity `u_s = U·f(s)·g(ρ)`, radial velocity `u_ρ = −U·f′(s)·w²/(2ρ)·(1−g)`, satisfying div u = 0), which thins a channel through the mist ahead of the boat (by at most half, never punching a hole; off-beat breaths have half the strength), and then slowly closes again.
- A breath too close to the previous one gets no extra feedback.

**Simulated breathing** (default source): simulates a child with their own pace, with natural variation, gradually falling in with the guide, occasionally taking an extra breath or pausing a little longer, to demonstrate the mist slowly clearing as the breathing settles.

**Test** (`-arrCapture <dir> -arrBreathTest`): in order, 60 seconds of steady breathing with the guide, 30 seconds of no breathing, 30 seconds of random breathing, 30 seconds of rushed breathing every 1.6 seconds, 50 seconds of steady breathing at a young child's natural pace (every 2.55–2.85 seconds, about 22 breaths a minute), and 30 seconds at the same pace with the guide off; calm is written to `breath_log.csv`. Add `-arrBreathNatural` to test only the last two stages, breathing at the natural pace from the start. Latest results: steady breathing rises one step per breath in rhythm (0.2 → 0.4 → 0.6 → 0.8 → 1.0), fully clear in about 20 seconds, and an occasional long pause by the simulated child brings back only about 0.2 of mist; with no breathing the mist starts returning after about 8 seconds and is back to thick in about 10 seconds; random and rushed breathing stay at thick mist (0) throughout; at a young child's natural pace the ring catches up in about 16 seconds from the start and the mist is fully clear in about 35 seconds; with the guide off it is fully clear in about 15 seconds.

**Microphone processing** (`BreathSignal.cs`, `B` switches the source):
- **Pipeline:** RMS → calibrate ambient noise for the first 2 seconds (including the running nebuliser) → normalise → hysteresis threshold plus a 1.5-second cooldown → record the time of one breath. From then on only that time is used, never the loudness.
- **Sources:** simulated natural breathing (default), microphone, or UDP port 9000 (receives values from 0 to 1).
- **Manual fallback:** the space bar records one breath, as a backup for live demos.

> **Prototype limits:** a microphone only measures how loud the sound is, not the real airflow, and with a mask on it cannot judge breath timing accurately either. Real use needs a chest belt or an airflow/pressure sensor in the nebuliser tubing (which can connect over UDP). This is only a demonstration signal for non-medical use.

## Sea mist (following Unreal Engine advice, implemented in Unity)

| Suggestion | Implementation in this project |
|---|---|
| Exponential Height Fog + Volumetric Fog | 8 semi-transparent mist slices (`MistLayers` + `Mist.shader`), denser closer to the sea (exponential height falloff). The lighthouse beam turns the mist gold as it passes through, equivalent to volumetric light scattering. The global fog stays light so local effects read clearly |
| Local Fog Volumes | The mist around hidden surprises is thicker and slowly clears as the two work together; the lighthouse island and surprises already met sit in clear space |
| Animated noise fog material | Two scales of tileable noise drift slowly, giving a continuous, soft sea mist rather than "smoke" made of individual particles |
| Sphere Mask / breath pushes the fog | Replaced by a small forward jet channel plus advection for each breath, acting only ahead of the bow; the overall mist is driven by calm |
| Gradual recovery | The mist only returns when the rhythm is lost, by an amount set by the match, taking about 8–10 seconds from fully clear to thick, with no sudden jumps |
| Real-time performance and future XR | The mist slices are a fixed number of planes, so the cost is predictable; every effect lives in the same scene, with no extra scenes |

[`docs/breath_airflow.mp4`](docs/breath_airflow.mp4) is a 30-second screen recording: the simulated child breathes steadily with the breath reticle in the middle of the screen, and the mist clears one step with every breath in rhythm.

## Sound

The game has gentle background music and quiet, soft ambient sound, with no wind-like hiss:
- Background music: "Silver Blue Light" (Kevin MacLeod, CC BY 4.0, attribution below), looping, fading in over the first 3 seconds, about 9 dB above the waves, streamed.
- A warm bed: pink noise through two low-pass filters at about 450 Hz, so it never hisses.
- Waves: one every 6–10 seconds, rising and falling softly; the filter has no resonance (a resonant sweep is exactly what makes "wind"), opens only to about 600 Hz, and leaves a soft wash of surf as each wave retreats.
- A low swell.
- Each breath: a very light low surge at about 300 Hz, rising over about 0.6 seconds and falling over 1.4 seconds.
- **When a friend is found:** a soft music-box arpeggio (C major pentatonic, 5 notes 0.18 seconds apart, about 2.8 seconds of ring-out). It only rewards discoveries the parent and child make together; breathing never triggers a reward sound. The background music is also in C major, so the two never clash.

Everything except the music is synthesised in real time; `M` mutes it all. The music attribution is in the in-game shortcut help (`H`), and the Windows package includes `CREDITS.txt`. Measured spectrum: about 2% of the energy is above 1 kHz and about 0.3% above 2 kHz.

## Art style (following the "sailing in the mist" reference image)

| Feature of the reference image | Implementation |
|---|---|
| Deep navy base, warm gold as the only accent colour | Sky, sea and mist are all pulled into deep blue; the lighthouse light, sail, lantern and the surprises' halos use gold and warm colours |
| Print grain / stippled texture | Post effect on the child's view, `StylizeFX` + `Stylize.shader`: split toning with navy shadows and warm-gold highlights, fine grain changing about 12 times a second, and a soft vignette (UI text is unaffected) |
| Stippled moon and starry sky | A large stippled moon, with a faint purple-pink gradient along the horizon (palette inspired by Monument Valley) |
| Flat yellow beam with birds inside | The lighthouse light is a flat, crisp-edged wedge; a flock of golden birds flies outward along the beam, turning with it, which also helps the child find the light's direction |
| Layered, fluffy sea of clouds | Crisper mist outlines, a bright rim on the side facing the moon, a golden rim where the light hits, and slight posterised colour banding |
| Simple lines on the sea | A few thin, pale streaks drift slowly with the waves |

## No scoring

- **Child:** no buttons, no counters, no star-collection bar. The end screen only says "Today you met: the Little Turtle, the Starlight Jellyfish…", as a memory.
- **Parent:** no progress bars or percentages, only words describing the state: "Hidden in the mist", "The mist is thinning", "Almost there", "Met". The breath waveform only shows detected breaths and gives no score.
- **Clinician:** the timer is only for the clinician; reaching the planned length only shows a reminder and never ends the session automatically.

## Controls

| Role | Controls |
|---|---|
| Child | One-hand joystick (gamepad left stick) / `←` `→` / `A` `D`: steer left and right. When the boat is stopped, the child can still slowly turn to look around |
| Parent | Drag on the chart, hold "Turn light left / right", or press `Q` / `E`: turn the lighthouse beam |
| Clinician | Control bar: length (switch between 3/5/8 minutes before starting), breath guide switch, Start/Pause, End; keyboard `Enter` start · `P` pause/resume · `X` end · `R` reset |
| Breathing (demo) | `Space` records one breath; `B` switches the signal source; `G` toggles the breath guide |
| Demo | `F1` switch view · `F2` dual display · `]` 4× speed · `M` mute · `H` shortcut help |

## Sharing with friends

Packaged apps are on the GitHub Releases page:
- **Windows:** unzip and run `BeyondTheMist.exe`. If SmartScreen appears, click "More info → Run anyway".
- **macOS:** the app is not signed by Apple, so it is blocked the first time you open it. Double-click it once, then go to "System Settings → Privacy & Security" and click "Open Anyway". For the latest version, rebuild with the menu `ARR → Build macOS App`.

## Code structure

`Assets/ARR/Scripts/`

| File | Purpose |
|---|---|
| `Voyage.cs` | Shared state: boat, light, hidden surprises, teamwork check, stopping, lighthouse island collision, clinician controls |
| `BreathSignal.cs` | Breath events (simulated / microphone / UDP) |
| `BreathGuide.cs` | Breath guide (follows the child's pace first, then gently aligns the phase), match, and calm changing step by step with the rhythm (drives the mist) |
| `AmbientAudio.cs` | Synthesised ambient noise and waves |
| `BreathFlow.cs` | A gentle jet for each breath (CPU and GPU share the same flow-field formula) |
| `MistLayers.cs` + `Resources/ARR/Mist.shader` | Layered sea mist: height fog, local fog volumes, airflow advection, light scattering |
| `Stardust.cs` + `StardustSim.compute` + `Stardust.shader` | GPU starlight particles (ribbon streamlines, fine dust in the air, star points), drawn in and pushed out around the boat with each breath |
| `Models.cs` | Procedural modelling tools (lathes, tubes, cloth triangles and more) |
| `OceanView.cs` · `Waves.cs` | Night sea, sailing boat, follow camera |
| `LighthouseView.cs` | Lighthouse model and the parent-controlled beam (three layers, shines through the mist) |
| `Surprises.cs` | Models, reveals and animation for the four surprises |
| `MistField.cs` | Distant banks of mist (adds depth) |
| `StylizeFX.cs` + `Resources/ARR/Stylize.shader` | Post effect on the child's view: blur and a light mist tint in thick mist (clearing step by step with the mist), split toning, print grain, vignette |
| `ChildUI.cs` / `ParentUI.cs` | Child interface (no buttons; the semi-transparent breath reticle in the middle of the screen; a direction arrow shown only when turning towards a new destination) / parent tablet: treasure chart, Teamwork, breath waveform, hidden surprises list, child's steering, and the clinician console at the bottom |
| `DemoAutoplay.cs` | `-arrCapture <dir>`: runs the whole flow automatically and takes screenshots; `-arrQuick` runs only the breathing part; `-arrClip` records a 30-second clip following the breath guide; `-arrTreasureTest` places the boat 60 m in front of the Little Turtle with the light on it and captures thick-mist and calm frames to check the treasure glow |
| `PromoRecorder.cs` | `-arrPromo <dir>`: records the 75-second demo video frame by frame from a storyboard, rendering the game's own sound in sync as `audio.wav`; `Tools/make_promo.py` then adds English captions and composes it |
| `DeckShots.cs` | `-arrDeckShots <dir>`: takes a few screenshots of the child's view for slides (the lighthouse at the start, finding the jellyfish, a calm sea, thick mist on the in-breath, mist clearing on the out-breath) |

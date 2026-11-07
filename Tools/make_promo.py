#!/usr/bin/env python3
"""Compose the 75 s demo video (with the game's own sound) from what the app recorded.

1. Record frames + sound:  "Builds/BeyondTheMist.app/Contents/MacOS/Beyond the Mist" -arrPromo <frames_dir>
   (writes f00000.jpg ... and audio.wav: the in-game soundscape, rendered in sync with the frames)
2. Compose:                python3 Tools/make_promo.py <frames_dir> docs/BeyondTheMist_demo.mp4

The storyboard timings below must match PromoRecorder.cs. This writes the game's own soft sea sound only
(no reward chimes, as in the game); add the background music afterwards with Tools/swap_audio.py.
"""
import os
import subprocess
import sys
import tempfile

FPS = 30
LENGTH = 75.0
SANS = "/System/Library/Fonts/Avenir Next.ttc"      # index 2 = Demi Bold, 7 = Regular
SERIF = "/System/Library/Fonts/Supplemental/Georgia.ttf"
SERIF_IT = "/System/Library/Fonts/Supplemental/Georgia Italic.ttf"

LOGO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "docs", "logo_cream.png")
BG = "0x0d1330"
CREAM = (243, 235, 215, 255)
GOLD = (240, 196, 110, 255)

# (start, end, main line, sub line)
CAPTIONS = [
    (0.4, 5.6, "Nebuliser time, turned into a shared voyage", "A child steers, a parent lights the way"),
    (6.0, 13.6, "The child steers. The parent lights the way. The clinician runs the treatment.", "Three roles, one voyage"),
    (14.0, 22.6, "Breathe with the ring in the middle of the sea", "Breathe in and the starlight drifts closer; breathe out and it drifts away"),
    (22.6, 31.6, "Steady breathing clears the mist", "Each breath in rhythm clears it a little more; more or harder adds nothing"),
    (32.0, 37.6, "Only the parent can see the hidden friends", "They drag the lighthouse beam on the treasure chart"),
    (37.6, 43.6, "The child steers into the golden light", "The parent can show the way, but can’t steer"),
    (44.0, 53.6, "Together, the mist parts and a friend appears", "The boat stops so they can say hello"),
    (54.0, 59.6, "The clinician can pause or end at any time", "The game never extends the treatment"),
    (60.0, 67.6, "One light, one boat, one shared discovery", "Starlight jellyfish, a treasure chest, a coral island…"),
    (68.0, 71.8, "No scores, just a shared memory", "The voyage ends with who you met today"),
    (71.8, 74.8, "The parent can show the way, but only the child can steer the ship.", "Beyond the Mist · ARR Hackathon · Unity prototype"),
]
TITLE = ("Beyond the Mist", "a breath-guided voyage for children in nebuliser treatment")


def font(path, size, index=0):
    from PIL import ImageFont
    return ImageFont.truetype(path, size, index=index)


def render_caption(path, main, sub, title=None, title_sub=None):
    """Transparent 1920x1080 PNG with the caption band text (and an optional centred title card)."""
    from PIL import Image, ImageDraw
    img = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if title:
        d.rectangle([160, 18, 1760, 918], fill=(8, 12, 36, 110))
        if os.path.exists(LOGO):
            logo = Image.open(LOGO).convert("RGBA")
            logo.thumbnail((190, 170))
            img.alpha_composite(logo, (960 - logo.width // 2, 300 - logo.height // 2))
        d.text((960, 470), title, font=font(SERIF, 128), fill=CREAM, anchor="mm")
        d.text((960, 580), title_sub, font=font(SERIF_IT, 44), fill=GOLD, anchor="mm")
    size = 42 if len(main) < 60 else 36
    d.text((960, 964), main, font=font(SANS, size, 2), fill=CREAM, anchor="mm")
    d.text((960, 1026), sub, font=font(SANS, 28, 7), fill=GOLD, anchor="mm")
    img.save(path)


def build_inputs_and_filter(tmp):
    inputs, parts = [], [
        "[0:v]scale=1600:900:flags=lanczos[v]",
        f"color=c={BG}:s=1920x1080:r={FPS}:d={LENGTH}[bg]",
        "[bg][v]overlay=160:18:shortest=1[base]",
    ]
    last = "base"
    for i, (a, b, main, sub) in enumerate(CAPTIONS):
        png = os.path.join(tmp, f"cap{i}.png")
        if i == 0:
            render_caption(png, main, sub, *TITLE)
        else:
            render_caption(png, main, sub)
        inputs += ["-loop", "1", "-framerate", str(FPS), "-t", str(LENGTH), "-i", png]
        idx = i + 2  # 0 = frames, 1 = sound
        f = 0.35
        parts.append(f"[{idx}:v]format=rgba,fade=t=in:st={a}:d={f}:alpha=1,fade=t=out:st={b - f}:d={f}:alpha=1[c{i}]")
        parts.append(f"[{last}][c{i}]overlay=0:0:enable='between(t,{a},{b})'[o{i}]")
        last = f"o{i}"
    # the game's soundscape, gently faded in and out and brought to a comfortable, even loudness
    parts.append(f"[1:a]afade=t=in:st=0:d=1.5,afade=t=out:st={LENGTH - 2.5}:d=2.5,loudnorm=I=-20:TP=-2:LRA=11[a]")
    return inputs, ";".join(parts), last


def main():
    frames, out = sys.argv[1], sys.argv[2]
    wav = os.path.join(frames, "audio.wav")
    if not os.path.exists(wav):
        sys.exit(f"missing {wav}: record with -arrPromo first")
    with tempfile.TemporaryDirectory() as tmp:
        inputs, graph, last = build_inputs_and_filter(tmp)
        cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
               "-framerate", str(FPS), "-i", os.path.join(frames, "f%05d.jpg"),
               "-i", wav, *inputs,
               "-filter_complex", graph, "-map", f"[{last}]", "-map", "[a]",
               "-c:v", "libx264", "-preset", "slow", "-crf", "26", "-pix_fmt", "yuv420p",
               "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-movflags", "+faststart", "-t", str(LENGTH), out]
        subprocess.run(cmd, check=True)
    print("wrote", out)


if __name__ == "__main__":
    main()

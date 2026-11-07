#!/usr/bin/env python3
"""Replace the soundtrack of the demo video without touching the picture.

  1. Re-render the game's sound on the identical timeline (no frames captured):
       "Builds/BeyondTheMist.app/Contents/MacOS/Beyond the Mist" -arrPromo <dir> -arrAudioOnly
  2. Mix it under the background music and swap it in (the video stream is copied, not re-encoded):
       python3 Tools/swap_audio.py docs/BeyondTheMist_demo.mp4 <dir>/audio.wav "Tools/music/Silver Blue Light.mp3" out.mp4

Music: "Silver Blue Light" by Kevin MacLeod (incompetech.com), CC BY 4.0 (see Tools/music/CREDITS.txt).
"""
import subprocess
import sys

LENGTH = 75.0
CREDIT = 'Music: "Silver Blue Light" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0'


def lufs(path, pre=""):
    """Integrated loudness of the first LENGTH seconds (after an optional filter chain)."""
    af = (pre + "," if pre else "") + "ebur128=framelog=quiet"
    r = subprocess.run(["ffmpeg", "-hide_banner", "-t", str(LENGTH), "-i", path, "-af", af, "-f", "null", "-"],
                       capture_output=True, text=True)
    lines = [l for l in r.stderr.splitlines() if l.strip().startswith("I:")]
    return float(lines[-1].split()[1])


def main():
    video, sea, music, out = sys.argv[1:5]
    # fixed (static) gains, so the discovery chime keeps its natural attack and ring
    music_pre = f"atrim=0:{LENGTH},afade=t=in:st=0:d=2.5,afade=t=out:st={LENGTH - 3.5}:d=3.5"
    sea_pre = f"highpass=f=40,lowpass=f=6000,afade=t=in:st=0:d=1.5,afade=t=out:st={LENGTH - 2.5}:d=2.5"
    g_music = -23.0 - lufs(music, music_pre)
    g_sea = -31.0 - lufs(sea, sea_pre)
    graph = ";".join([
        f"[2:a]{music_pre},asetpts=PTS-STARTPTS,volume={g_music:.2f}dB,aresample=48000[m]",
        f"[1:a]{sea_pre},volume={g_sea:.2f}dB,aresample=48000[s]",
        "[m][s]amix=inputs=2:normalize=0,alimiter=limit=0.9:attack=5:release=80,aresample=48000[a]",
    ])
    cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
           "-i", video, "-i", sea, "-i", music,
           "-filter_complex", graph, "-map", "0:v", "-map", "[a]",
           "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-ar", "48000",
           "-metadata", f"comment={CREDIT}", "-movflags", "+faststart", "-t", str(LENGTH), out]
    subprocess.run(cmd, check=True)
    print(f"wrote {out} (music {g_music:+.1f} dB, game sound {g_sea:+.1f} dB)")


if __name__ == "__main__":
    main()

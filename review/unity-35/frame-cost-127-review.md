# Frame-cost study 127: race stutter on Bayview (29 September)

Richard reported minor but definite stutters while racing around Bayview. This study found where the time goes and fixed the frame pacing. The racing AI is unchanged: the drivers still step at a fixed 120 Hz with the same logic, so this isn't an AI keep/reject study.

## Method

- **Setup:** rendered, windowed 1920×1080 Bayview races (`run-frame-cost.ps1 -Study 127 -Rendered`), 24 cars, seed 1709, on the first monitor (144 Hz).
- **New `--frame-probe` columns:**
  - per-frame script time for every car's fixed step, and for the AI `Plan()` alone (`FrameCost`);
  - the number of physics steps in the frame;
  - CPU main-thread and GPU time from FrameTimingManager (`PlayerSettings.enableFrameTimingStats` on).
- **`--time-physics`:** steps physics from a timed script (probe runs only).

## Findings

| Run | What it showed |
|---|---|
| bayview-probe-2 | Uncapped: mean 6.1 ms, p95 17 ms, 433 frames over 16.7 ms. The one 237 ms frame is the trial writing its report at the finish; it isn't gameplay |
| bayview-probe-3 | GPU is only about 2.5 ms. Spike frames carry 2–5 physics steps where normal frames carry about 0.6 |
| bayview-probe-4 | AI `Plan()` averages about 2.1 ms per frame; the worst frames reach 10–21 ms when the pack bunches. The average spike frame has no more planning than a normal one |
| bayview-probe-5 | Physics itself is cheap: 0.26 ms per step. Car scripts are 3.4 ms per step, mostly AI |
| bayview-paced-6 | With VSync on (144 Hz): 82% of frames within 2 ms of the median, 17.6% missing the refresh |
| bayview-racepaced-7 | **VSync at every second refresh (72 fps on 144 Hz): 91% within 2 ms, 1.0% missing the refresh, stdev 2.3 ms** |

**Cause:**
- **Pacing:** races forced vSync off with an uncapped 120 fps target, which presented unevenly on high-refresh displays.
- **Step cost:** with vSync at 144 Hz, any frame that carries a 120 Hz AI and physics step costs about 8–9 ms. That's over the 6.9 ms budget, so about one frame in six missed the refresh and the race juddered.

## Changes (kept)

- **Pacing:** a player's race now follows the Frame rate setting. With VSync (the default) above 100 Hz, it syncs to every second refresh (`GraphicsOptions.ApplyRacePacing`), which is 72 fps on a 144 Hz monitor. The 60 fps, 120 fps and unlimited choices are unchanged, and automated trials keep their old timing.
- **Hidden HUD:** the fallback text HUD in career races no longer rebuilds every frame while the race HUD hides it.

Smoke20 passes.

## Left open

- **AI planning spikes:** a few frames still spike to 10–21 ms of AI planning when the pack bunches. Making planning cheaper or spreading it out changes AI behaviour, so it needs its own flag and keep/reject study.
- **Free roam:** no race AI there, so it's cheaper per step. It already follows the Frame rate setting and wasn't changed.

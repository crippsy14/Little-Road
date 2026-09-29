# What makes a game look good: audit and plan

Written 28 September 2026 at Richard's request: research what makes a game look good regardless of style, check each point against Little Road, and plan whatever is missing.

The sources agree on a small set of fundamentals. Style (toy, painterly, realistic) changes how each is expressed, not whether it matters.

## The fundamentals

### 1. Value hierarchy
Light and dark are read before colour. A scene should work in greyscale: the focal point has the strongest contrast and the background the least. Value is "what the player sees first, second and third", and it is validated with greyscale tests.

### 2. A controlled palette
A limited family of hues, controlled saturation, one or two accents, and colour used with meaning (danger, reward, path).

### 3. Shape language and silhouettes
Landmarks and interactive things read by outline alone, from far away. Controlled asymmetry and "hero" shapes break up repetition.

### 4. Composition
- Focal points and leading lines (roads, rivers and fences pointing at landmarks).
- Framing (trees, arches and cliffs at the frame edges).
- Foreground, midground and background layers.
- Areas of rest against areas of detail.

### 5. Detail density and hierarchy
Put most of the refinement near focal points and keep quiet zones between them. Use big, medium and small forms.

### 6. Lighting as design
- Key, fill and rim: the sun as key, a coloured ambient fill ("dark navy ambient in shadows"), and a rim light that separates silhouettes.
- Motivated local lights.
- Contrast managed so important things get the light.
- Bounce and global illumination for soft believability (Tiny Glade's stylised look rests on real-time GI).
- Keep albedo out of the extremes so bounced light still reads.

### 7. Grounding and contact
Objects must sit in the world:
- contact shadows and ambient occlusion where they meet the ground;
- a soft transition where buildings meet terrain;
- no floating or sunken props.

### 8. Atmosphere and depth
- Aerial perspective (haze, fog).
- Height fog in valleys.
- A sky that matches the light.

### 9. Surface response
- Roughness variation.
- Edge highlights (bevels catch light).
- Grime and wear where it would gather.
- Specular reflections of the real environment.

### 10. Image quality and stability
- Anti-aliasing with no shimmer.
- Filtered patterns.
- No LOD or culling pop (cross-fades).
- A steady frame rate with no hitches.

### 11. Motion and life
- Wind in foliage and water.
- Secondary motion: cars that roll, pitch and settle, flags and bunting that move.
- Ambient creatures and traffic.
- Clouds.

### 12. Feedback ("juice")
- Particles.
- Camera response to speed and impact.
- Readable UI that stays still.

## Little Road against the fundamentals

| # | Fundamental | Status | Notes |
|---|---|---|---|
| 1 | Value hierarchy | Partial | Towns and the castle read well. Wide valley views are mid-value everywhere: mid-green hills against mid-grey mountains. Needs a greyscale review of the key views. |
| 2 | Palette | Done | `WorldLook` limits, the grade, per-town palettes, the Pit Stop brand colours. |
| 3 | Silhouettes | Mostly done | The castle is now a hero landmark. The distant mountains are a soft grey mass; their ridgelines could be crisper and darker near the snow line. |
| 4 | Composition | Partial | Roads lead to the town and the castle. There's no deliberate framing at arrivals: the tunnel exit and the causeway end are good spots for a framed view. |
| 5 | Detail hierarchy | Mostly done | Towns are dense and the valley has patchwork and woods. The hill slopes inside the town spiral are empty lawn. |
| 6 | Lighting | Partial | There's a sun key, an SH fill and night lights. Missing: **rim light** (silhouettes melt into the backgrounds) and **bounce**, meaning warm ground light on walls' lower halves and cool sky light on up-facing surfaces. |
| 7 | Grounding | Partial | SSAO is weak (radius 0.8). Kit props and buildings meet terrain in a hard line. Floating and sunk props have been found by eye several times (station car, tunnel lamp), so this needs an automatic placement audit. |
| 8 | Atmosphere | Done | Procedural sky, fog, valley mist, time of day. |
| 9 | Surfaces | Mostly done | Kit patterns, bevels, base weathering, probes. Cars have clearcoat paint. |
| 10 | Image stability | Partial | TAA now really runs (the UI camera stack had been switching it off). Hitches are fixed (grass building, probes, first-sight warm-up). Distance-culled props **pop** at 280 and 700 m, with no cross-fade. |
| 11 | Motion and life | Partial | Wind, water, clouds, birds, trains and traffic. Missing: **car body roll, pitch and suspension** (cars move like bricks), and flags and bunting are static. |
| 12 | Feedback | Mostly done | Dust, skid marks, speed FOV, the zipline ride. There's no camera response to impacts. |

## Plan, by visual impact per effort

1. **Car body motion.** Body roll in corners, nose dive under braking, squat under power, and a settle on landing. Visual only (the collider and physics are untouched), so every car on screen feels weighty and alive. This is the single most "toy with real physics" cue.
2. **Rim and bounce light in the kit shader.**
   - A soft sky-coloured rim on silhouettes.
   - A warm ground bounce on downward and lower surfaces.
   - A cool sky fill on upward faces.
   Buildings, props and landmarks then separate from their backgrounds.
3. **Grounding.**
   - Stronger, tighter SSAO for contact.
   - A soft dark footing under kit pieces that sit on terrain.
   - A placement audit (review mode) that reports every kit piece floating or sunk by more than 0.3 m.
4. **Fade instead of pop.** Dithered cross-fade (`LOD_FADE_CROSSFADE`) for LOD switches and for the distance-culled props.
5. **Value pass.** Greyscale captures of the key views. Darken and cool the mountains' lower slopes and forests, and keep the brightest values for landmarks, roads and water.
6. **Motion in the town.** Flags and bunting flutter; the kit shader's wind already sways leaves.
7. **Framed arrivals.** Frame the tunnel exit and the causeway end with trees and landforms, and dress the town spiral's inner slopes with terraced gardens and paths.

Each step is checked with the drive benchmark (`tools/drive-benchmark.ps1`) and valley captures before and after.

## Sources

- [Lighting, The Level Design Book](https://book.leveldesignbook.com/process/lighting)
- [Radiator Blog: Lighting theory for 3D games, part 4](https://www.blog.radiator.debacle.us/2015/07/lighting-theory-for-3d-games-part-4.html)
- [Color Theory for Game Art: The Production Application Guide](https://nastyrodent.com/color-theory-for-game-art/)
- [Environment Artist Playbook: From Blockout to Final Pass (RMCAD)](https://www.rmcad.edu/blog/environment-artist-playbook-from-blockout-to-final-pass/)
- [Creating Game Environments: Layout, Composition, Colour and Layers](https://islandclass.org/2026/09/26/game-environment-art-layout-colour-layers/)
- [Game Lighting Design: Complete Visual Guide 2025](https://generalistprogrammer.com/tutorials/game-lighting-design-complete-visual-guide-2025)
- [Lighting design fundamentals: how and where to use colored light (Game Developer)](https://www.gamedeveloper.com/design/lighting-design-fundamentals-how-and-where-to-use-colored-light)
- [Tom Stochastic on Tiny Glade's rendering (real-time GI in a stylised game)](https://x.com/h3r2tic/status/1813197502411092186)
- [How to Make Your Game Feel Good: Game Feel and Juice](https://egmatic.com/blog/how-to-make-your-game-feel-good)

## Progress (28 September 2026, evening)

| Step | Status | Evidence |
|---|---|---|
| 1. Car body motion | Done (`BodyMotion`, `--no-body-motion` to compare). Roll, dive, squat and bob on springs; wheels stay planted; physics untouched. | `review/map-sweep/db6/drive-*.png` |
| 2. Rim and bounce | Done (`Kit.shader`, `WorldLook.KitRim/KitBounce`, `--no-rim-bounce`). Bounce was reduced to 0.14 after the first capture. | `rim-off-2` against `rim-on-2` |
| 3. Grounding | SSAO is stronger and tighter (1.5 intensity, 0.6 m radius). `PlacementAudit` (`--placement-audit`) found 16,464 pieces checked. The real hits are a Bayview bush (−93, −1) and surf rocks below the headland cliffs. The rest are vines on slopes and houses on plinths, which is expected. | `placement-2` |
| 4. Fade instead of pop | Done. Kit props, tree stand-ins and car LODs cross-fade. | Kit shader `LOD_FADE_CROSSFADE` |
| 5. Value pass | Done for the mountains: forest climbs to a ragged treeline at about 480 m, giving every range a dark base under rock and snow. | `look-3/v09`, `v13` |
| 6. Town motion | Bunting sways and flutters (`LittleRoad/Bunting`). | |
| 7. Town hill | The hill inside the Via Crest spiral is terraced: vines and lavender along the contours with dry-stone walls, plus olive groves and cypress meadow (1,286 pieces). Before this it was bare lawn: its planting pass checked a rule that reserves the whole hill. | `look-4/v09-castle.png` |

Also fixed: a podium car sometimes rendered blank on the result screen. The preview stage now shows the full-detail meshes directly instead of relying on the LOD group (`smoke10`).

### Continued

- **Framed arrival.** Stone pines and cypresses on the verges past the Headland Tunnel frame the first view of Hillcrest. The left-hand cypresses stand wide of the castle sightline. See `look-6/v04`.
- **Camera response.** `CameraShake`: trauma from knocks against cars, walls and props, shaken by trauma² with smooth noise. It ignores kerbs and ground, and `--no-camera-shake` turns it off.
- **Flags and banners.** They fly and hang in the wind. `build_bayview_kit.py` `flag_freedom()` bakes a per-vertex "freedom" into KitHeight.y, and `Kit.shader` `KIT_FLAG` waves it in every pass, so shadows move too. Ten kit pieces were re-exported.
- **Value check.** In greyscale the castle is the lightest mass in its views, and forested mountain bases now separate the ranges from the hills. The weak spot was pale grey rock on moderate slopes. The mainland rock is now warm, darker limestone with stronger strata, and grass holds to steeper ground (`_MeadowBias` .17). See `look-9` against `look-7`.

# Little Road art direction: toy shapes, real light

Little Road is a toy-car game, so the shapes stay chunky and readable: rounded kit buildings, simple
trees, and cars that look like toys. The light and colour should feel like a real late afternoon:
natural greens, warm sun, cool shadows, drifting clouds and soft haze. Toy colours on toy shapes
under flat light are hard on the eyes, and that is what we moved away from on 2026-09-26.

Everything below is enforced in code. `unity/Assets/Scripts/WorldLook.cs` holds the numbers, and
`unity/Assets/Editor/WorldLookChecks.cs` fails the build if they drift.

## Rules

1. **Hues belong to the builders, loudness belongs to WorldLook.** Pick whatever hue a thing
   needs, but create materials with `TrialWorld.Material(...)`. It passes every colour through
   `WorldLook.Surface`, which scales saturation by 0.9 (cap 0.72) and rolls brightness off above
   0.55 (×0.6, cap 0.82, floor 0.05). Real paint, plaster and stone sit well below paper white;
   the roll-off is what stopped light surfaces reading as washed-out pastel (2026-09-28). Only car
   body paint is exempt (`graded:false`), so the paint shop shows the colour the player chose.
   Town palettes are earth pigments, not ice-cream pastels: ochre, saffron, terracotta, Pompeian
   red, rose, sienna and sage walls with bottle-green, slate-blue and oxblood shutters (Hillcrest);
   coral, mustard, sea-green, dusky rose and cornflower by the sea (Bayview).
2. **The ground stays part photographic.** Terrain materials call `WorldLook.Terrain(material)`.
   `_Stylise` (0 = the credited grass and rock maps, 1 = flat toy colour) is 0.5 for meadows,
   0.55 for runoff and 0.3 for masonry. The check allows 0.7 at most. Don't set `_Stylise` or the
   `_ToyGrass*` colours by hand; change `WorldLook`.
3. **Light has shape.** One sun (2.4, shadow strength 0.92; nearly white at a high afternoon
   sun, warm only when it is low), a dim cool fill (0.26), and a
   trilight ambient that is darker at the ground than the sky. Shadows reach 240 m in free roam
   and 140 m in races. SSAO is on in `PC_Renderer` (intensity 1.0, radius 0.8: props and cars sit on the ground). Temporal AA is the default (`WorldLook.Camera`).
4. **The grade is gentle.** Neutral tonemapping, saturation −4, contrast +24, cool shadows and
   warm highlights (shadows/midtones/highlights), a +3 white balance, exposure +0.18, soft bloom
   (0.22) and a vignette (0.2). This is set up by `WorldLook.Grade`; `TimeOfDay` adds exposure at
   night.
4b. **Day and night.** The town world's sky is procedural (`LittleRoad/DaySky`, `DaySky.hlsl`),
   so the sun can be anywhere. `TimeOfDay` runs a real sun path (latitude 44°, late summer: sunrise
   06:00, sunset 20:00) and drives the sun and moon, sky gradient and glow, clouds, light, ambient,
   fog, valley mist and exposure from keys by sun elevation (the 55° key is the afternoon above).
   Clouds share the sun's cloud-shadow noise. The sea reflects the same sky function. At night
   `NightLights` lights windows (lit-glass kit parts, plus about half of all ordinary windows via
   the Kit shader's `_KitNight`), puts a pool of warm point lights on the street lamps nearest the
   camera and runs headlamps (emissive lenses on every car, spot beams on the nearest eight).
   Options: Graphics > Time of day = Cycle (about 20 min a day, nights 3× faster) or a fixed time;
   reviews use a fixed afternoon unless `--time morning|afternoon|golden|dusk|night|<hour>`.
5. **The sky moves.** A tileable cloud cookie on the sun (30% shade, 900 m tiles) drifts at
   3.2 m/s. Custom shaders must declare `#pragma multi_compile_fragment _ _LIGHT_COOKIES` and
   either use URP's lighting functions with a world position (`GetMainLight(shadowCoord,
   positionWS, shadowMask)`, `UniversalFragmentPBR`) or sample `SampleMainLightCookie`. The
   terrain, foliage, wheat and distant shaders already do.
6. **Water is water-coloured.** Deep teal-green with high smoothness; the sky reflection
   supplies the blue. The sea uses `LittleRoad/CoastalWater`.
7. **New shaders** take vertex or material colours as sRGB (`SRGBToLinear`), use the main light
   with shadows and cookies, apply fog with `MixFog`, and carry a DepthOnly pass.

## When adding content

- A new building or prop kit: author in `source-art/build_bayview_kit.py` using palette names.
  Palette colours may be bright; they are graded at runtime, and the check confirms every palette
  entry grades into the limits.
- **Every prop sits on the land at the land's angle** (Richard, 18 Oct). Kit pieces are fitted as they spawn
  (`GroundFit`, behind `--prop-forge` for now). Give a new kit id its kind in `GroundFit.kinds`:
  - `tilt`: loose things settle on the slope, up to a cap;
  - `rake`: runs follow the slope along their length;
  - `plumb`: trees and posts stay upright, sunk until no edge of the foot hangs;
  - `leave`: buildings, which stand on plinths and pads.
  A small id that isn't listed is plumb. Runs (walls, fences) use `Runs`, and object families use `Objects.Place`.
  Both follow the ground themselves.
- A new area: call `TrialWorld.Create`, which sets up light, grade and clouds, and build terrain
  with `TerrainMaterial` / `WorldLook.Terrain`.
- To review a change, capture before and after with `tools/capture-map.ps1` and compare the
  frames.

## Town services standard: the Pit Stop

Every town has a Pit Stop, identical everywhere so players recognise it on sight (like a
Pokémon Centre). It is built from `pit_stop_2` / `pit_stop_3` in `source-art/build_bayview_kit.py`
and placed with `PitStop.Build` (`unity/Assets/Scripts/PitStop.cs`). `PitStopChecks` fails the
build if a kit loses a bay's host marker, sign or the pylon tyre.

- **Look:** an orange wing roof with white racing stripes and a white soffit, a chequered band,
  colour-coded bays with round icon badges, and a roadside pylon topped by a giant spinning
  tyre. The bay colours are garage teal, parts yellow and body-shop pink.
- **Bays:**
  - Garage: the town's local race, fitting owned parts, and swapping owned bodies.
  - Parts: the town's performance parts.
  - Body shop: new models (looks only) and paint.
- **Size:** Bayview is the tutorial town, so it has only the garage and parts bays
  (`pit_stop_2`). Every other town gets all three (`pit_stop_3`).
- **Placement:** on the main road at the town's arrival point, never deep inside a landmark.
  - The frontage faces the road, with a forecourt of about 9 m under the wing.
  - The pylon stands at the kerb on the side traffic arrives from.
  - Keep the plot clear of trees and row houses (HillcrestTown `reserved`).
- **Bodies:** sold at a body shop from `CarBodies.ForSale`. Every body there must use the
  standard racing envelope, so bodies never change pace. Owned bodies swap live in any garage
  bay (`RoadCar.ReplaceBody`).

## Hero landmarks

Every town has one landmark that reads from anywhere in its area and rewards a close look. Hillcrest Castle is the model (`castle_donjon`, `castle_palace`, and the `castle_*` towers, walls and gatehouse in `build_bayview_kit.py`; laid out in `HillcrestTown.Castle`):
- **The silhouette comes first.** It needs a stepped skyline with one tallest element (the belfry at about 58 m on a 52 m hill) and a crowd of needle spires and cones. Gold finials catch the sun.
- **Colour inside the grey.** Red banners, blue-slate roofs, a warm palazzo with terracotta, and white string courses.
- **Something to do.** The Zip Tower ride (`ZipLine.cs`) turns the landmark into a shortcut and a view. Site any cable or long ride with `ZipLine.Survey` (the `--zip-survey` world review): at least 8 m clear under the car, and a clear station footprint.
- **Lit at night.** Use `GlassLit` windows (lancets on the donjon, dormers, and the loggia lamps).

## Interaction prompts (who you can talk to)

Names no longer float above cars as text. The design follows common guidance for in-world
prompts: one consistent place on screen for the action, a small in-world marker only for "who",
text large enough to read from a sofa, and glyphs that match the device in hand (Xbox
Accessibility Guideline 101 asks for at least 26 px text at 1080p on console and 18 px on PC,
with button glyphs at least that size).

- **In the world: a talk badge** (`TalkBadge`). A round speech bubble in the character's colour
  with three dots ("!" for the current objective). It holds a steady ~58 px on screen, appears
  within 38 m (fading from 30 m), dims to 35% when hidden behind scenery, and pulses larger and
  turns sun-yellow for the one person you'll talk to. Roaming locals and parked cars only get a
  badge while they are that person.
- **On screen: the interaction card** (`AdventureUI.Interaction`). Bottom centre, always the same
  place: the device's button glyph (keycap E, PlayStation cross, Xbox A; `InputGlyphs` tracks the
  last device used), "Talk to Mara" at 38 px and the role ("Pit Stop Garage") at 24 px in the
  character's colour. It pops in when someone is in range. When you're still moving it reads
  "Slow down to talk to ..." and dims the glyph.
- Control hints use the same glyph set and are at least 24 px.

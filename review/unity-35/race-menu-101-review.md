# 101: 3D showroom and workshop inspection

23 September2026. Implements the user's request to treat the car display as a showroom rather than a painted background, and continues the interactive paddock visual pass. This remains part of the full campaign goal.

## Presentation

The actual entered car rotates in a real3D dark studio: physical floor/back wall and ribs, standing illuminated panels, floor guides and a marked luminous turntable. The studio set is combined by material to avoid separate renderer submissions for each trim segment. Preview-layer lighting is isolated during synchronous renders and restored immediately. The showroom is opt-in for the race preview; ordinary garage stages retain their backdrop. Car renders increase to900x500 for the larger inspector. All generated set meshes/materials and render textures are owned and released with the preview stage.

The coastal illustration remains behind the wider menu. The inspector replaces scenery directly behind the car with the showroom, adds a workshop mat under actual fitted3D parts, numbered sections, an admission sticker, and separate driver/part notes. Car/equipment/driver data and all selection/return/pause controls remain authoritative and read-only. This is a stylised studio, not a claim of photorealistic materials or ray-traced reflections.

## Verification

- Nondevelopment build:498 editor checks,164 frozen source/script inputs.
-212 runtime preview checks pass, including all24 loadouts, rotation with paused race time, synthetic controller entry/return, seven fitted-part selections, wrap and grid tour.
- Actual1080p player preview and rival inspector screenshots inspected: real room/platform behind the car, separated inspection areas, all seven selectable parts and actual driver graph visible. No coastal PNG behind the car.
- Controlled race parity and connected workflow results recorded below once complete.

100 compiled/passed498 editor checks but failed at runtime because its new Unlit shader was stripped. The scoped diagnostic hook exited normally with failure code2; retain that failed candidate.101 uses the already retained Lit/emission material variant.100 is not a successful runtime build.

## Limits and next priority

Physical DualSense/mouse acceptance, small-window readability and motion comfort remain open. The previous connected recovery limitation remains tracked separately. This hidden functional run is not performance acceptance. The user has requested a measured choppiness/performance pass after this UI work; racing frame budgets remain unmet. No desktop shortcut promotion. Full campaign, UI, endurance, progression, teammate and content scope remains required.

Original coastal artwork is retained from97: unity/Assets/Resources/UI/coastal-race-postcard-v1.png; built-in image-generation tool provenance and exact prompt at references/ui/coastal-race-postcard-v1.md. No new raster image generation was needed for this real3D showroom.

### Controlled race result

101 preview run exited0 without runtime errors. Recorded motion, ordered gates, impacts, recoveries, overlap peaks, track/context samples and car race results exactly match99. This verifies the sampled controlled Bayview fixture, not every scenario. Hidden profiling numbers are deliberately not treated as rendered-performance evidence.

### User visual direction

After viewing the actual101 showroom/inspector screenshot, the user answered: "Yes—keep this direction." Retain the dark3D showroom and workshop direction. This accepts the visual direction, not untested physical-controller, small-window or performance criteria. Move next to measured choppiness/performance after the final connected check.

### Final connected verification

Connected session and fresh-process reload both exited0.17/18 integration checks pass; the existing race2NoTeleportRecovery check remains false. Both24-car, three-lap fields finish through ordered physical gates. Player outcomes22nd/96.656807s and16th/92.807747s earn100+130 credits; wallet230 and fitted upgrade/quest state survive reload. Retry cancel/commit checks pass. The first result capture was visually inspected:22/24,1:36.657,+100 credits, wallet100, readable return/retry controls. Connected outcomes are not asserted deterministic between builds; the separate controlled preview race supplies the exact recorded parity evidence.

101's user-approved showroom direction is retained as an intermediate milestone. All processes finished; no shortcut or production promotion. Next: measured rendering/AI frame stalls using normal-speed matched evidence, preserving recorded racing and the accepted UI.

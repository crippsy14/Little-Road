# Visual parts browser: reference and specification

19 September 2026; implementation update 21 September. User requests the original's list-based selection with each part represented visually in 3D. The engine category now passes runtime review in [checkpoint 18](../review/iteration-30-engine-browser.md). The other six categories still use the text/button prototype; the full browser requirement is not yet satisfied.

## What is established and what remains to verify

[RPGamer's PS2 guide](https://gamefaqs.gamespot.com/ps2/915200-road-trip/faqs/21740), Shops and Getting Started, distinguishes purchasing in the Parts Shop from fitting owned equipment at Q's Factory. Its Change Parts description covers functional equipment, appearance and bodies, including teammate equipment. The [HG2 parts catalogue](https://w.atwiki.jp/choroq_game/pages/152.html) remains the category/part reference already reviewed in document 10. These written sources establish the selection structure, not the exact menu geometry or rendering technique.

The user's firsthand recollection is a list with a 3D representation of each part. Preserve that as an explicit design requirement. [RacingGameArchive's PS2 playthrough](https://www.youtube.com/watch?v=sp7ax3xeYbQ) was opened and sampled around the initial factory/driving/race sequence; that sampling did not reach the detailed parts-selection screen. The emulator inspection below supplies the direct visual evidence. Do not claim that every original row was animated or that its part images were live meshes rather than prerendered images.

The user then opened their original game in PCSX2. Direct visual inspection of [the captured engine menu](../references/original/2026-09-19-parts-engine.png) confirms a category list at upper left, with Tires, Engine, Chassis, Transmission, Steering and Brake visible and a down-scroll indicator. A blue highlight with gold border marks Engine. The car remains visible on a workshop platform to the right. Individual engine models appear in overlapping green cards with orange/gold nameplates: a larger selected Panther card sits in front, with smaller Normal cards behind/alongside. A separate lower-right panel reads Power 180 and Energy Use 4. The item selection is therefore a layered carousel of part cards alongside a category list, not simply a table of text rows.

Only the displayed state was inspected; no controller input was sent. The available automation has no gamepad-input API. Animation, rotation controls, exact navigation mapping, equipped indicators, purchase confirmations and the original rendering technique remain unverified. The screenshot is research reference only, not a game asset to redistribute in our build. Search results for Choro Q 2 on PS1, HG3/Works or Road Trip: The Arcade Edition are not evidence for this PS2 game's menu.

## Our required experience

Use the confirmed category-list plus visual-carousel arrangement: category navigation on the left, a whole-car workshop preview, and individual part cards with the selected part larger and brought forward. Every entry has a recognisable 3D representation of the actual part, name, price or owned state, and fitted indicator. The selected entry gets a larger rotatable 3D view. Preserve the tactile catalogue character while reducing overlap where needed for readability. Engine internals can be viewed as standalone components without pretending they are externally visible on the car.

Keep buying, owning and fitting distinct. Show a focused comparison against the currently fitted part: price, fit eligibility, benefits, drawbacks and effects on the complete setup. Selecting a row previews it; it must not silently buy or fit it. Buying does not force installation. Fitting owned parts is free, and cancelling restores the real fitted setup.

For fuel equipment, show empty tank mass, capacity, chosen starting load, loaded vehicle mass and estimated range/stint length. Engine entries show power/torque and consumption trade-offs. Tyres show grip/surface suitability and endurance life. Do not reduce these decisions to one opaque overall score, or display untested lap-time predictions as facts.

Support saved named setups, body/paint/decals, filters for owned/affordable/eligible parts and consistent navigation as the catalogue grows. Never make the player scroll through dozens of irrelevant categories for a simple tyre change. Hide unimplemented purchasable parts; future catalogue plans belong in development documents.

## Presentation and controls

- Original Little Road colours, typography, models and layout details, taking inspiration from the reference's tactile catalogue rather than copying its assets.
- Stable item scale, lighting and background; recognisable silhouettes for engines, tanks, tyres and gearboxes. Individual variants must be visibly distinguishable where advertised.
- D-pad/stick navigation, shoulder-button category changes, explicit confirm/back, and a clearly indicated model-rotation control. Final button mapping must respect the existing DualSense specification and be tested on real hardware.
- Mouse/keyboard parity, readable focus, preserved selection on return, scroll into view, clear unavailable states, scalable text and no meaning conveyed by colour alone.
- Performance-conscious previews: use original 3D assets; cached renders from those assets can represent inactive rows, with a live rotatable selected model. Do not render dozens of full scenes every frame. This is our proposed implementation, not a claim about the PS2 internals.

## Build sequence and acceptance

The original screen's key arrangement is now confirmed. Next make a reviewable layout, then implement one complete category with three real part models, comparisons, purchase/fitting and controller flow. Expand to all current seven slots before adding fuel/tank/tyre-life presentation. Share data with the driving model and save catalogue so preview statistics cannot diverge from equipped behaviour.

Acceptance: no missing model or incorrect variant; accurate owned/fitted/price/eligibility state; no purchase from merely highlighting; cancelled previews leave the save unchanged; insufficient credits cannot buy; repeated confirmation cannot charge twice; large catalogues retain stable focus; previews reflect fuel load and parts correctly; complete purchase and fitting loop works with keyboard and physical DualSense; save/reload preserves the result. Review at normal 1080p play distance and profile the garage on the same PC as the race.

## First engine implementation — 21 September

`PartsBrowser` uses the existing `EquipmentCatalog` and transactional `AdventureStore`; no parallel balance table or save schema is introduced. Three original engine models are authored by `source-art/build_engine_previews.py`, with editable Blender masters and stable-ID FBX resources. The stock air cleaner, workshop intake/filter and club trumpets distinguish the variants beyond paint alone.

`PartPreviewStage` uses one isolated camera. It caches the inactive cards and the owned car, and renders the selected model on selection or rotation. Idle frames do not recapture the models. The first category proves the flow; generalising the presentation mapping and proving a second category remain next steps before claiming a reusable complete catalogue.

Comparisons swap the highlighted engine into a copy of the actual fitted loadout and call the same tuning calculation as driving. Power is a relative index, explicitly stock setup = 100, not invented horsepower. The 0–30 mph estimate integrates the real force model on level dry tarmac; top speed includes both available power and the fitted gearbox ceiling. These are setup estimates, not promised race results. Fuel figures remain absent until the fuel simulation exists.

Buy and Fit are separate controls. Highlighting, rotating, cancelling and changing categories never buy or fit. The right stick is reserved for inspection; navigation uses the left stick/D-pad or keyboard. Physical DualSense operation and visible-window performance are separate acceptance gates from the synthetic input review.

Final engine review passes 24 runtime checks and the connected delivery/upgrade/24-car race/payout/reload smoke. The latter awards and reloads 100 credits with the Workshop engine still fitted. Six actual Unity captures support the visual review; this does not close physical hardware or whole-game visual acceptance.

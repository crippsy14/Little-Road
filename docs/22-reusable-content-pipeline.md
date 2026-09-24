# Reusable content without losing polish

21 September 2026. Research and project-specific recommendation, not a claim that these systems are already implemented. Keep Unity 6000.6.1f1 for this milestone; no new engine, asset-store framework or package migration is required to fix the current race.

## Recommendation

Build a small set of reusable components, content definitions and validation tools around the existing game, introducing each when the next real feature needs it. In web terms: reusable view components, typed content records, domain services and migrations. Art still needs authored silhouettes, composition and review; reuse should save setup work and propagate fixes rather than make every place or car identical.

The current source already separates part IDs/tuning in `EquipmentCatalog` from saved ownership in `AdventureStore`, and body art from performance. Car assembly, parts screens and track/event setup remain more procedural and tightly coupled (`TrialWorld`, `AdventureGame`, `RaceTrial`, `Circuit`). Extend these seams in small verified steps.

## Cars and world art — priority 3

Use a shared vehicle root containing driving, input/AI, lights and equipment integration. A body definition supplies its visual prefab, wheel pivots, lamp bindings, LODs, camera framing and an explicitly reviewed collision envelope. Prefer composition of body, wheels and trim over a deep hierarchy of inherited variants. Unity prefab variants inherit a base prefab with local overrides, which is useful for a few curated derivatives of an approved master. [Unity prefab variants](https://docs.unity3d.com/6000.0/Documentation/Manual/PrefabVariants.html)

Keep original Blender masters and bounded recipes. Standardize metres, orientation, named wheel pivots, material roles, UV/decal zones and export paths. Validate these at import. Import presets can apply common asset settings, reducing accidental differences between exports. [Unity presets](https://docs.unity3d.com/6000.0/Documentation/Manual/Presets.html)

Prototype a replacement body through the current assembler first; only extract a reusable prefab workflow once the second master confirms which parts are actually shared. Do not tie driving performance to a visual silhouette or infer a production collider from every distant LOD. Author and validate the envelope against wheel steering/spin and body geometry.

For buildings and scenery, use a limited modular kit: wall bays, roof sections, shop fronts, trim, planting and shared materials. Preserve bespoke landmarks and composed town layouts. Generate/export static kit pieces ahead of play where practical. Review draw calls, material counts, LOD changes and moving-camera appearance in a full field before multiplying content. LOD Groups support multiple model detail levels, including externally authored meshes. [Unity LOD configuration](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/lod/lod-group/lod-group-configure)

**Proof of reuse:** add the next genuinely different car body without editing the driving controller, lights logic or race spawner; it must pass the same import, collision and visual checks.

## Parts browser — priority 5

Retain stable part IDs and one authoritative tuning catalogue. Add a separate presentation mapping from part ID to preview prefab, display metadata and fitting category. Avoid maintaining competing tuning values in JSON and Unity assets. Small ScriptableObject definitions are suitable for editor-authored visual references; they are shared authoring data, not the player's save file. [Unity data/logic separation](https://unity.com/how-to/separate-game-data-logic-scriptable-objects)

Split the screen into reusable category tabs, part rows, comparison panel, ownership/action controls and one preview stage. A dedicated camera renders the selected part/car to a RenderTexture. Reuse the preview stage and a small pool of visible rows; do not instantiate a lit scene and camera for every catalogue item. Use the same equipment calculation for the preview and the actual car. Selecting an item previews it; purchasing and fitting remain explicit transactions.

The current UI uses uGUI and the Input System. Keep that integration for the first polished browser, using reusable prefabs and shared styling rather than rebuilding controls for each part. UI Toolkit is a credible alternative with a web-like document/style workflow, but switching frameworks alone will not improve the art or controller behaviour. Unity documents both systems and their different strengths. Reassess only if a bounded prototype proves an advantage for this particular screen. [Unity UI comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html)

**Proof of reuse:** adding a tyre or engine requires a catalogue record and preview mapping, with no new menu logic. Verify keyboard/controller navigation, long names, unavailable/owned/fitted states, wallet changes, save/reload and unknown retired IDs.

## Tracks, events and campaign — priority 7

Introduce stable-ID definitions for tracks, race events and quest objectives as the second track is authored. Track data owns route samples, ordered gates, safe grid slots, recovery positions and pit anchors. Event data refers to a track and defines class eligibility, laps/duration, entrants, equipment, payouts and unlock rules. A tougher class can then reuse a track without copying race code or silently scaling rivals to the player's upgrades.

Separate persistent campaign state from temporary event state. Saves retain content IDs and schema versions; migrate deliberately when IDs or rules change. Validate missing references, duplicate IDs, impossible unlock dependencies, unsupported equipment and malformed grid/gate data before building. Quest definitions can reuse delivery, visit, collection and race-result objectives, while authored dialogue, routes and rewards create variety.

Start with a shared bootstrap and separate region/event scenes. Additive scene loading can load a scene while retaining the current ones; use it when multiple regions or persistent services justify the complexity. Do not introduce streaming solely for the present small island. [Unity additive scenes](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Additive.html)

Keep present local asset loading until profiling or catalogue size makes on-demand loading useful. Addressables requires explicit lifetime management: loaded references need matching releases. Adopt it behind a small content-loading interface when needed, with loading-failure and repeated scene-change tests. It is not a prerequisite for offline expansion. [Addressables memory management](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/MemoryManagement.html)

**Proof of reuse:** a second event on Bayview changes only its event definition; a second track supplies scenery/route data and passes the existing driving/race/save checks. Measure campaign duration from distinct authored activities, not duplicated races or inflated prices.

## Order of adoption

1. Checkpoint 16's launch fix and rear-grid race are delivered with automated evidence; retain the new vertical-motion checks as part of route validation and close outstanding manual/controller acceptance.
2. Next fidelity pass: improve compact glass/roof/highlights and demonstrate a second master using the shared assembly contract. Record moving-game and performance comparisons.
3. Build one polished parts category end to end, then feed other categories through the same browser and preview stage.
4. Extract track/event definitions while adding the next event and track. Preserve existing saves and known-good Bayview behaviour throughout.
5. Expand quest/region content and introduce asynchronous loading only when measured needs warrant it.

Each step should deliver visible game progress and a demonstrated reduction in repeated work. A broad framework rewrite, mass procedural body generation, DOTS conversion and online content delivery are outside this recommendation.

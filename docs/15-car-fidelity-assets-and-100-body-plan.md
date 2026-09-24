# Car fidelity, asset options and a scalable body catalogue

17 September 2026. Review requested before further development and the proposed Unity migration. Recommendation: build a small, high-quality modular vehicle foundation, accepting third-party assets only where their style and editable structure help. Do not mass-produce the current prototype geometry.

## Review of the actual models

Evidence inspected: `source-art/build_car.py`, the [four-body game-engine gallery](../review/captures/evening-07-final/09-body-gallery.png), and the [vehicle/equipment concept](../references/concepts/vehicle-equipment.png). The concept is an illustrated target, not a specification that can be converted automatically into production geometry.

| Area | Current evidence | Required improvement |
| --- | --- | --- |
| Body surfaces | Compact uses six coarse cross-sections with bevels; cabin is a separate simple shell | Deliberately shaped bonnet, shoulders, rear quarters, roof and wheel arches, with continuous highlight flow |
| Family variety | Coupe lowers the compact's upper vertices; utility stretches the same cabin and adds a load | Distinct wheelbase/proportions, roof/window outlines, front and rear volumes, and recognisable identities |
| Windows/interior | Dark surface panels; little visible interior information | Recessed glazing and seals, pillars, dashboard/seats, sensible reflections and controlled transparency |
| Panel detail | Separate handles/lights, mostly uninterrupted body paint | Door/bonnet/hatch seams, fuel flap, proper lamp lenses/recesses, grille depth, wheel-arch trim and underbody |
| Wheels | Shared cylinders and spokes | Better tyre profile, sidewall/tread treatment, rim variety, hubs and believable fit inside the arches |
| Materials | Mostly flat material colours | Controlled paint clearcoat/roughness, rubber, glass, lamps and trim; restrained surface detail appropriate to the reference |
| Personalisation | Paint colour and a fixed coupe stripe | Explicit paint masks and stable body UVs; independently stored decals/livery layout |
| Production scalability | Four script-selected variants; collision hulls baked per model | Versioned body definitions, sockets, UV conventions, LODs, provenance, automatic validation and repeatable export |

Adding polygons or subdivision alone will not fix these issues. The current models remain useful for dimensions, animation pivots and gameplay tests; the compact, utility and coupe should be rebuilt from a better surface model rather than preserved at any cost.

## Asset review

These are a shortlist, not purchase recommendations or validated imports. Product claims and marketing previews are distinguished from inspection of source meshes. No pack was purchased or imported. Listed compatibility does not establish compatibility with our exact Unity version.

| Candidate | Verified listing facts | Assessment for Little Road |
| --- | --- | --- |
| [Kenney Toy Car Kit](https://kenney.nl/assets/toy-car-kit) | Free, CC0; page lists 100 files including track and other pieces | Preview inspected. Useful toy proportions and prototyping content, but simpler than our target. **100 files does not mean 100 unique car bodies.** |
| [Kenney Car Kit](https://kenney.nl/assets/car-kit) | CC0; page lists 45 files | Useful free comparison/kit source. No claim of higher fidelity or exact body count without unpacking and inspecting it. |
| [Quaternius Cars Pack](https://quaternius.com/packs/cars.html) | Eight car models; FBX, OBJ and Blend; CC0 | A free source candidate for adaptation. Source topology, UVs, wheel hierarchy and target-style fit remain untested. |
| [TSP172's Cartoon Car Pack](https://assetstore.unity.com/packages/3d/vehicles/land/tsp172-s-cartoon-car-pack-392864) | Listing: five vehicles including a cruiser/police variant; approximately 16k–28k triangles; door/wheel rigs; URP listed for Unity 6000.0.25f1. Web listing showed US$12 before tax | Preview inspected. More detailed conventional car proportions and interiors; would need substantial proportion/art changes for our rounded miniature style. Four base identities plus a police variation must not be miscounted as five unique silhouettes. A candidate for a controlled comparison, not a verified drop-in solution. |
| [Synty POLYGON Street Racer](https://syntystore.com/products/polygon-street-racer) | Modular vehicles, opening panels, liveries and many attachments. Listed 15 vehicles include non-car items; 33 presets are also listed | Modular exploded-car preview inspected. Strong ownership/customisation structure and richer detail, but intentionally angular and less miniature than our concept. The live UK page showed about £183; prices/region/tax can change. Our rounded art direction would still need adaptation. |
| [Synty POLYGON Pro Racer](https://syntystore.com/products/polygon-pro-racer) | Listing includes open-wheel, prototype, touring, rally and other racing assets | Potential companion for the performance-class breadth; not source-inspected or purchase-approved. No conclusion on exact Unity compatibility yet. |
| [OlyPoly Customizable Car Pack](https://assetstore.unity.com/packages/3d/vehicles/land/customizable-car-pack-341910) | US$19.99 web listing before tax; updated January 2026; URP listed for Unity 2022.3.20f1 | Further modular candidate. Geometry, real body count and style fit remain unverified; do not infer them from the product title. |

Free sources establish that useful starter content exists. The candidates reviewed do not establish a free, complete, high-fidelity replacement for our exact art direction. Paid modular packs can save work, but buying variety does not automatically supply a coherent 100-body miniature-car catalogue. Licence/provenance and permitted source-delivery format must be recorded before any asset is integrated into the distributable project.

## Recommended production approach

First bring three very different master bodies to the visual target: a compact road car, a low sports coupe and an open-wheel racer. Review front, side, rear, three-quarter and normal chase-camera views under the same lighting. Compare any acquired candidate in that same scene. This establishes whether original work, adapted assets or a mixture produces the best result for this game.

Preserve the rounded miniature proportions in the reference. Build shared high-quality modules for wheels, tyres, lamps, mirrors, glass, interior pieces and declared equipment mounts. Keep proportions constrained so attachments still meet the body, wheels sit inside their arches and steering/suspension travel stays clear. The first quality gate is attractive, coherent cars at gameplay distance and garage close-up distance.

Then grow toward roughly **100 curated body designs**, with decals counted separately. A possible production structure is 20 authored master designs with up to five reviewed body variants each. This is a capacity plan, not evidence that 100 distinct cars exist. A variant counts as a body only if its geometry and silhouette are meaningfully different; paint, race numbers, a roof load or a spoiler alone do not count. Some masters will need more authored work instead of five acceptable derivatives.

Suggested 20 master identities across ten families:

| Family | Two initial master directions |
| --- | --- |
| Small hatch | Rounded city runabout; upright retro hatch |
| Saloon | Compact three-box; long classic saloon |
| Estate / utility | Short estate; compact panel van |
| Pickup / off-road | Small pickup; short off-roader |
| Classic coupe | Rounded fastback; squared long-bonnet coupe |
| Road sports | Small roadster; mid-engine coupe |
| Rally | Short rally hatch; notchback rally car |
| GT / touring | Wide touring coupe; endurance GT |
| Sports prototype | Closed-cockpit endurance; open-cockpit sports racer |
| Open-wheel | Vintage narrow racer; modern wide-track formula-inspired car |

The new approximate 100-body ambition supersedes the old 12-body planning minimum. Six bodies remain an intermediate milestone, not the final cap. Classes influence field appearance, while performance remains governed by fitted parts. Body choice must preserve the user's investment in their car.

## Generator and decal system design

The generator should assemble and export authored geometry. It is not a random mesh factory. A versioned recipe selects a master, constrained proportions, compatible front/rear/roof modules, sockets, materials, collision specification and LOD rules. Save stable IDs and recipe versions; a software update must not silently change an owned car's shape.

Separate appearance from equipment: `body_id`, `body_recipe_version`, `paint`, `wheel_style` and `livery_id` are distinct from engine/tyres/transmission/turbo/performance configuration. A livery stores decal asset IDs, body surface/UV region, position, scale, rotation, colour, opacity and layer order. Text/number mirroring must remain readable. UV seams, glass masks, wheel arches and attachment boundaries need explicit exclusion/placement rules.

Begin with original stripes, two-tone areas, number panels, geometric graphics and fictional local-team/sponsor marks. Deliver an initial 20–30 reusable graphic motifs and several layout templates; combine them without counting the resulting colour combinations as new body models. Procedural vector-style graphics can be rasterised into atlases. Do not use real sponsor logos as default content.

For a racing build, composite or cache livery textures when appearance changes, rather than creating dozens of separate projected decal draws on every car each frame. The garage can offer richer editing and previews. Load only the bodies/materials needed for the current field, and share common wheels and material resources where appropriate. The exact Unity shader/import implementation depends on the migration trial.

Initial *profiling targets*, not accepted performance: approximately 12k–30k triangles for a close-view body including normal visible detail, substantially reduced LODs for the field, and a small shared material set. A maximum of 24 cars at 30k triangles is about 720k vehicle triangles before scenery, shadows and additional passes; this arithmetic is not a 60-fps guarantee. Measure CPU, GPU, draw calls, skinning, collision and texture memory in an actual 24-car race.

## Acceptance before scaling

1. Three master bodies visually meet the concept rubric in a common review scene.
2. Body UVs, paint masks and at least three decal layouts survive normal and extreme valid shape settings without obvious stretch, seams or unreadable text.
3. All modules have correct scale/pivots, wheel centres and socket orientation; materials and textures survive export/import.
4. Steering, suspension travel, terrain contact and car-to-car impacts do not produce visible clipping; per-body collision envelopes remain fair and disclosed.
5. LOD transitions and cached liveries work in a profiled 24-car race; the generator produces repeatable artifacts from versioned recipes.
6. Ten bodies pass a silhouette/contact-sheet review before expanding to 25, 50 and approximately 100. Reject weak variants rather than padding the count.

**Status, compact study:** the original four prototypes are preserved. A new Bayview Compact study now has editable Blender source, a versioned recipe, formed body surfaces, actual wheel openings, glazing/interior detail, separate axle pivots and three Unity detail levels. Its 61,680-triangle close mesh exceeds the initial profiling target; corrected LOD1/2 contain 30,124/14,354 triangles. These are geometry counts, not measured frame-rate acceptance. The recipe reproduces this authored study; it is not yet a constrained modular body generator.

Direct Blender and Unity review images are saved under `review/unity-03`. Runtime review caught wheel mesh children being animated as pivots; explicit pivot selection now has imported-geometry spin/steering checks. The compact still needs softer roof/frame transitions, better integrated lamps, tyre surface detail, finished UVs and decal masks. New coupe and open-wheel masters, the decal editor and the ~100-body catalogue remain unimplemented. None of the three-master visual acceptance gate is yet claimed complete. See [iteration 10](../review/iteration-10-compact-master.md).

## Future Le Mans and modern F1 roster — 21 September request

The user requests fast race-car bodies spanning essentially each distinct car design participating in the 24 Hours of Le Mans, plus a modern F1-shaped car that recognisably has the correct form. This is deferred content work; continue the current parts browser first. The current formula prototype is a placeholder for this purpose, not acceptance of the requested modern F1 body.

Before production, research a dated Le Mans entry list and make a coverage inventory of distinct competing designs grouped by their actual classes. Include the variation between individual prototype and GT designs; a single generic body per broad class is insufficient. Exact season, class names and vehicle list must be verified when that inventory is made, not inferred from old regulations. Track every planned body and its reference sheet, implementation and review status.

Gather front, side, rear and three-quarter reference images for each intended design and a contemporary F1 car, favouring official organiser/manufacturer/team sources. Record the source and season. Use the sheets to check silhouette, wheelbase/track proportions, cockpit, nose, wings, wheel enclosure/exposure and other defining shapes before adding detail. Preserve the original rounded-miniature direction while making the design's race-car form convincing. These are research references; deliver original game assets within the existing art/licensing policy.

Give each race body multiple racing liveries with readable numbers and the existing fictional-team/decal vocabulary. A field may contain the same body repeatedly with different liveries, numbers and driver identities. Body variants and livery variants remain separately counted; liveries do not inflate the approximately 100-body target. Use these fast-looking bodies more often in higher-spec events, while equipment and class setup continue to determine performance rather than body appearance alone.

Current compact baseline has subsequently reached [checkpoint 17](../review/iteration-29-glass-and-roof.md), including a hollow cabin. The user accepts its general look and asks to move on from minor car polishing. The historical study counts/status above are not the current export counts.

# Car bodies, race identity and forgiving collisions

**Updated 29 September:**
- **Bodies:** tied to chassis platforms, one per class, with kit versions of earlier favourites ([doc 45](45-roadmap-and-feature-spec.md) F7).
- **Collision boxes:** now per body, fitted to each model's length and its shoulder-and-tyre width without mirrors (`CarBodies.ShoulderWidth` / `Length`; `--contact-review`). The single shared envelope described below is retired.


Added 16 September 2026 from the user's request for varied bodies from ordinary road cars to F1-style cars, class-appropriate race fields, visible collision integrity and meaningful but recoverable crashes.

## Body roster

17 September update: the user requested a fidelity review, assessment of existing assets, and a route toward roughly 100 body shapes with decals. The fidelity plan that followed (doc 15, retired 4 October; see git history) superseded the 12-body planning target below. Body variants must be visually distinct; recolours and decal layouts remain separate. The six-body milestone is retained as an intermediate delivery step.

The completed game should have a broad catalogue of original, recognisable silhouettes. A working production target is at least 12 distinct bodies across compact hatchbacks, small saloons, vans/utility cars, classic coupes, modern sports cars, rally cars, GT/endurance cars, sports prototypes and open-wheel/F1-style racers. These are original designs; exact licensed real-world replicas are not required.

The first multi-body milestone is six substantially different silhouettes: compact hatch, small utility vehicle, road coupe, rally hatch, sports prototype and open-wheel racer. Recolours do not count as different models. The existing one-body prototype is superseded as the intended presentation; it remains an implementation milestone only.

Body selection expresses identity. Engines, tyres, gearing and other fitted equipment determine performance. A favourite compact can become a serious race car; selecting an F1-style shell alone does not secretly add power or downforce. Functional aero is a separate declared part. Body changes retain owned equipment, colours where compatible, saved progression and garage history.

Use consistent toy-like scale and a defined vehicle clearance envelope. Preserve each silhouette while avoiding a narrow model gaining an undisclosed collision/traffic advantage. Declared physical differences, if later introduced, must be explained and balanced; they must not creep in accidentally through mismatched meshes and colliders.

## Race-field appearance

- Early local fields: mostly modest hatchbacks, saloons, vans and older coupes, with occasional visibly upgraded cars.
- Club/rally fields: more sporting bodies, rally equipment and distinctive liveries, chosen to suit the track.
- Higher circuit classes: increasing proportions of GT, prototype and open-wheel bodies with appropriate fitted performance parts.
- Specialist events: body mixes fit the terrain and discipline. Open-wheel racers should not dominate a rough expedition event merely because it is a higher class.

Higher-spec races should look faster as well as drive faster. Mix bodies, colours and equipment within each field; avoid 23 clones or making a silhouette a guaranteed finishing position. Use fixed class/track field definitions with natural driver variation. Do not rescale opponents' equipment in response to the player's purchases.

## Collision integrity

Visible car bodies and wheels must not pass through other cars, walls, buildings, barriers or the ground during ordinary driving and reviewed impacts. Verify mesh bounds, wheel steering/spin, body lean, terrain pitch and every fitted visible part against the actual collision setup. Check overtaking side clearance, bumper contact, side swipes and multi-car compression at race speed.

Use appropriate collision hulls and swept-motion checks. Do not solve clipping by letting cars become ghosts, disabling race collisions or hiding contact with the camera. A finished car may leave active traffic in a clearly presented finish/parking state; that is distinct from ignoring contact during a race.

Acceptance uses both measured penetration/contact evidence and frame-by-frame rendered review. No visible sustained interpenetration is acceptable. Tiny physics contact tolerances must be bounded and documented; a generous invisible collision box that makes the model feel wider than it looks is also a defect.

## Crash response

Crashes should cost time without routinely deciding the whole race. Target soft glancing contact as a brief disturbance, a moderate impact as roughly a 1–3 second loss and a heavy frontal impact as a larger but recoverable setback, initially about 3–6 seconds. These are initial playtest bands, not implemented guarantees.

Response depends on relative closing speed and impact angle. A gentle side brush must not apply the same speed loss as a head-on collision. Avoid applying a severe penalty repeatedly while two cars remain touching. Preserve steering/braking control, offer clear impact sound/visual feedback, and keep recovery behind the next checkpoint. Do not add punitive repair bills, destroyed upgrades, instant retirement or permanent performance damage in the introductory build.

## Required paired tests

1. Fresh-car clean solo runs establish time, lap splits and contact-free reference behaviour.
2. Repeat the same seed and equipment with a controlled rear impact at the start; compare lap/race time and the victim's outcome.
3. Repeat with a low-speed brush, oblique side contact, moderate rear impact and severe wall impact; verify increasing severity produces sensible time loss.
4. Test attacker/victim symmetry and repeatedly touching cars; look for clipping, stacking penalties, launching, spin loops and unfair loss of control.
5. Check two-, three- and 24-car races, different body families, steering extremes, hills, bridge barriers and crowded starts.
6. Capture impact frames and post-impact recovery, and measure maximum overlap, time to regain control, time loss, checkpoint integrity and any resets.
7. Have the user sense-check the response after automated results are plausible. The race must remain recoverable and enjoyable.

The original prototype uses one compact body and a simple fixed speed reduction on lateral contact. That is not the final roster or crash system. Current limitations and test failures must remain visible in the review.
# Evening prototype update — 17 September 2026

Four original Blender bodies now exist: compact, upright utility with roof load, low coupe with spoiler, and an open-wheel study. The Class C field uses compact/utility/coupe bodies. The open-wheel asset is a model study for later-class content, not an unlocked class or player body selector. Six initial body families and the broader roster remain target scope. Each body has its own small conservative convex collision hull, including front-wheel steering extremes. Contact penetration, impact timing and recovery acceptance remain unverified; do not read the existence of those hulls as a no-clipping certification.

**Update (28 September 2026).** The coupe, utility and formula prototype bodies above have been removed (models, sources and checks). Older saves that owned one move to the Puddle starter. The racing fields now use the 24 generated bodies from `source-art/build_family_cars.py`, all inside the standard envelope (`CarBodies.Envelope`). New bodies should come from that generator.

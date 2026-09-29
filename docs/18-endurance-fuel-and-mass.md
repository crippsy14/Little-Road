# Endurance, fuel, tyres and vehicle mass

**Updated 29 September:** tyre wear, fuel and pit stops now **arrive with class A Touring**, as "the professional league", and class E Endurance extends them to long stints and day-to-night races.
- **Fuel:** a GT-style fuel mix (1–6), changeable during the race, sets burn = throttle × delivered power × mix; engines get an efficiency figure.
- **Order:** the systems and the strategy AI are prototyped on one pit-lane test track during class B.

See [doc 45](45-roadmap-and-feature-spec.md) F8. The mechanics below still apply.


19 September 2026. User-requested expansion; specification only. The current prototype does not implement these endurance systems. This promotes energy/endurance from a later possibility to a required part of the 12-hour game.

## Reference and design intent

The original PS2 game has fuel consumption, reduced mobility when empty, automatic refuelling on entering buildings, and an Endurance Run event, according to [RPGamer's guide, Screen Display and Races](https://gamefaqs.gamespot.com/ps2/915200-road-trip/faqs/21740). The user remembers fuel mainly limiting late-game jet use and considers it underused. That is useful firsthand design context, not a verified statement of the original developers' intent.

Our extension makes engine demand, boost, carried fuel, tyre life and pit time interact throughout endurance racing. Strategic tyre changes and predictive AI pit planning are our requirements; the reference research has not established those systems in the original.

## Fuel, capacity and weight

- Fuel quantity is a changing race/exploration state. Tank capacity is a fitted equipment property. A larger tank does not force the driver to fill it.
- Add standard, lightweight short-stint and larger endurance tanks, with declared capacity and empty tank mass. Show dry vehicle mass, current fuel mass, total mass and estimated range. These are functional parts with genuine trade-offs, not cosmetic menu entries.
- Total simulated mass = base running gear + fitted equipment including empty tank + carried fuel. Avoid double-counting existing chassis mass. As fuel burns, mass decreases continuously; refuelling raises it. Update the physical body and driving calculations consistently.
- Body shells remain cosmetic under the established ownership rule. Do not introduce hidden mass/performance penalties merely because a player likes a van or a compact. Functional chassis/equipment and fuel determine racing mass.
- Engines have power/torque and efficiency/consumption curves. Using more delivered power generally consumes more fuel; boosted/high-output operation is expensive. A powerful engine at part throttle need not always consume more than a weak engine working flat out. Offer efficient endurance engines as meaningful sidegrades.
- Model idle and load-dependent burn plus boost cost; no consumption or wear while paused, browsing the garage or loading. Use one model for player and opponents. If events use accelerated fuel/wear rates, declare those multipliers in event rules and apply them equally.
- Boost draws from an explicitly declared resource. If it uses the main tank, include its cost in range estimates and AI planning; do not charge twice through both engine output and an undocumented boost drain.
- Out of fuel: stop normal propulsion and boost; provide a slow limp/recovery option with a time cost, clearly marked as assisted. This cannot refill instantly, advance checkpoints or become a faster racing strategy. Free-roam recovery remains forgiving and cannot create a money softlock.

## How mass should feel

Extra mass primarily slows acceleration and hill climbing, increases energy demand and changes braking/turning transients. With limited straights, a heavy car can struggle to reach its potential maximum before the next bend. Flat-road terminal speed remains determined chiefly by available power, drag, rolling resistance and gearing; do not impose an arbitrary top-speed penalty solely for tank capacity.

The existing `DriveModel` already includes mass in acceleration/braking calculations, but mass is static and drag is simplified. Extend that foundation, separating rolling resistance from aerodynamic drag and propagating current mass through all related values. Do not merely update `Rigidbody.mass` while leaving cached tuning unchanged. A larger engine supplies potential power, tyres determine how much can be used, and cornering/braking share the traction budget.

Initial implementation can use a centred fuel mass contribution and an arcade grip model. Detailed moving fuel, tank slosh, individual tyre heat distribution and mass-dependent centre-of-gravity shifts are outside the first endurance milestone. Do not claim real-world accuracy from a simplified model or make braking distance scale directly with mass regardless of tyre/brake limits.

## Tyres and stops

Tyre compounds offer peak grip versus usable life, with surface suitability retained. Wear depends on distance, load and driving stress; heavy loads and sliding can increase wear. Grip degrades progressively with readable warnings. Worn tyres do not permanently delete the player's purchased part. Opening sprint progression retains its no-repair-bill policy.

The player selects starting fuel, compound and a provisional strategy before the race. During the race, show fuel remaining, estimated laps/range, tyre condition, pit entry and the next planned service. Estimates are labelled estimates and adapt to recent consumption. Provide a simple pit-request interface usable on DualSense, with a pause-assisted strategy screen in offline play.

Pit flow: request/choose services → approach marked entry → obey a declared speed limit → follow a physical pit lane → stop in assigned box → receive fuel and/or tyres → safe release → merge back. Entry, waiting, service and exit all cost real race time. No teleport through traffic, lap shortcut or free momentum reset. Design the checkpoint branch so pitting counts a lap exactly once when appropriate.

Allow fuel-only, tyre-only and combined stops, including a partial top-up. Initial service model: fixed stop overhead plus fuel amount / fill rate plus tyre-change duration when selected; serial service is the initial transparent rule. Later parallel service would require explicit retuning. Tyres reset wear only when that service completes. Fuelling can stop at the requested amount; never exceed capacity or grant negative service time.

Fuel and tyre service are initially included in event participation so they do not undermine the grinding economy or make a valid low finish lose money. Strategy costs race time. Tough opposition comes from capable driving, suitable parts and smart strategy, without hidden fuel/grip privileges or rubber-banding.

## AI strategy and traffic

Every rival maintains fuel usage per lap, uncertainty/reserve, tyre degradation, recent pace, distance to the next service opportunity and estimated pit loss. Re-evaluate at sensible intervals and after unusual consumption, damage-free contact delays or weather changes when weather exists.

Choose between continuing, fuelling, changing tyres and combining services by comparing projected finish time and safe range. Pit before fuel cannot cover the next service opportunity plus reserve. Use hysteresis/commitment so a car does not repeatedly change its mind across the entry. Select tyre service when expected remaining grip loss outweighs pit cost; avoid an unnecessary final-lap stop if safe to finish.

Driver/team personalities can vary reserves and strategic risk within safe bounds. Variation must not be simulated by random pit visits or artificially assigned positions. Predictable wear/fuel outcomes should remain explainable. Handle pit queues, occupied boxes, blocked entry and release traffic, including two- and three-car arrivals followed by the full 24-car field.

Use actual race distance and elapsed time for classification, including cars in the pit lane. After the time limit, finish when the leader next crosses the line, then classify others by laps/distance and finish order under declared event rules. Lap-count endurance events may use the existing finish model. Keep lapped traffic readable and prevent aggressive blocking during blue-flag yielding.

## Progression and staged implementation

1. Prove dynamic mass and consumption in matched solo runs; add real tank parts and UI comparisons.
2. Add tyre degradation, service accounting and safe persistence; prove fuel-only/tyre-only/combined stops.
3. Build a short 8–12 minute development event with meaningful one-stop versus two-stop possibilities. Test two/three cars in the lane before 24. This test duration does not replace the final events.
4. Integrate AI predictive strategy, lapped traffic, pit warnings, result detail and pause/suspend/resume.
5. Author the proposed 20/30/40-minute endurance ladder with tough class-appropriate rivals and distinctive tracks. Require coherent equipment preparation and skill, not one compulsory expensive setup.

Free driving uses accessible garages/service locations and low-pressure refuelling. Introductory races can begin with enough fuel to finish comfortably. Explain fuel strategy before requiring it to progress. Campaign/event eligibility should not reveal unavailable tracks as playable.

## Acceptance tests

| Test | Required observation |
| --- | --- |
| Same car, different starting fuel | Loaded mass changes by exactly the declared fuel contribution; heavier load affects acceleration/climb without a fake gear-ceiling change |
| Same fuel quantity, different tanks | Only empty tank mass differs initially; extra capacity alone does not add a full tank's fuel weight |
| Full-throttle/part-throttle/boost runs | Consumption responds to actual demand and declared efficiency; reserve/range estimates converge; no hidden AI advantage |
| Grip-limited and power-limited sections | Engine gains remain limited by tyres; dynamic mass does not double-apply acceleration/braking penalties |
| Long stint | Fuel and wear remain bounded; worn tyres lose grip progressively; pit decisions change as conditions change |
| Pit accounting | Correct services, quantity, elapsed time and checkpoints; fuel-only does not repair tyres; partial fuel does not fill the tank |
| Two/three/24 pit arrivals | No permanent queues, overlapping service cars, unsafe releases or lost lap progress |
| Competing strategies | Across matched seeds, efficient/long-stint and powerful/short-stint setups each have suitable events; no strategy dominates every track |
| Emergency and last lap | AI responds safely to fuel shortage and avoids pointless stops; limp/recovery cannot beat normal service |
| Save/suspend/reload | Player and rival fuel, wear, strategy, race time and pit state survive; no free refill, rewound competitors or duplicated payout |
| Full event | Correct result, classification and exactly-once saved earnings after a complete player-controlled endurance race |

Record lap times, fuel used, fuel carried, tyre condition, pit losses and result distributions alongside the existing natural-variance/equipment tests. Better equipment should improve suitable-event potential; a heavy full tank or unsuitable compound can legitimately lose a short event. Never require every expensive part to dominate every circumstance.

# Project brief

**Updated 29 September:** the campaign is **at least 12 hours and open-ended**. 12 hours is a floor, not a cap, and the game grows as content is added. The plan of record for classes, racers, teams and world growth is [doc 45](45-roadmap-and-feature-spec.md), with the parts ladder in [doc 44](44-parts-tiers-and-progression.md).


Working title: Little Road. Version: 0.4, 19 September 2026.

Current scope and work order: [12-hour campaign and priorities](17-scope-and-priorities.md). New required systems: [endurance, fuel, tyre service and changing vehicle mass](18-endurance-fuel-and-mass.md), and [visual parts selection](19-parts-browser-reference-and-spec.md). These expand the finished-game scope; the introductory slice below remains an intermediate milestone.

Updated user requirements: [24-car racing and progression](09-racing-and-parts-progression.md), [expanded ownership and parts](10-expanded-ownership-and-parts.md), and [DualSense on Windows](11-dualsense-controller-spec.md). These supersede the previous three-rival/two-upgrade prototype limits. Full ownership now includes performance, specialist traversal, utility, customisation and garage progression; the first implementation milestone does not cap that ambition.

## Proposed vision

A cheerful driving adventure in a world of car characters. Explore winding roads, get to know small communities, enter local races and gradually turn an ordinary little car into a capable travelling companion.

Our first audience is ourselves: the prototype should recapture the pleasure of going somewhere interesting in a car we enjoy controlling. The finished game now targets at least 12 hours of substantial first-playthrough content, measured through playtesting. A commercial release and final title remain undecided.

## Working assumptions

- Windows PC first, single player, offline.
- Keyboard and PS5 DualSense controls on Windows, including USB/Bluetooth and complete menu navigation; see docs/11-dualsense-controller-spec.md.
- Detailed, stylised 3D with rounded miniature-car proportions, inviting scenery and clear modern menus. Pursue the agreed high-fidelity visual target; low polygon counts are not an artistic ceiling.
- Unity with C# and Blender is the approved direction from 17 September. A separate migration trial uses the installed Unity 6000.6.1f1 and URP; the Godot/GDScript playable checkpoint is preserved until Unity reaches feature parity. See document 16 for the migration gates and current evidence. Verified development hardware: i7-14700KF, RTX 4070 Ti SUPER, approximately 32 GB RAM.
- A modest initial region loaded as one scene; add region loading only when scale requires it.
- Original world, art and writing.
- Initial performance goal: 1080p at 60 fps on the verified development PC; this is a target to test, not a measured result or a minimum customer specification.

## The first complete slice

Build a 10–15 minute introductory route through the essential experience. Stock-to-winning race progression extends beyond that introduction, targeting 30–60 minutes for initial balancing:

1. Start beside a small garage and learn acceleration, steering, braking, reverse and reset.
2. Explore a compact town and speak to a resident.
3. Accept a delivery that introduces the coastal road and a second location.
4. Find a collectible on a short optional detour.
5. Return for a reward and purchase an upgrade with a noticeable effect.
6. Enter a 24-car race, earn money even from a low finish, and begin repeated parts development toward midfield, podiums and wins.
7. Save, quit and return with currency, upgrades and completion state intact.

The town, route and race can share scenery. A race can reuse part of the free-driving route with event boundaries and checkpoint logic.

## Content budget for the slice

| Content | Initial limit |
| --- | --- |
| Region | One town, one road loop, one optional detour |
| Vehicle bodies | Six distinct silhouettes in the first multi-body milestone, from modest road cars to prototype/open-wheel racers; broader catalogue and class-specific field mixes in document 13 |
| Residents | Three characters with brief dialogue |
| Quests | One delivery with a clear start, objective and reward |
| Racing | At least 24 cars including the player; one authored opening track, with explicit track/class expansion |
| Upgrades | Six functional slots and at least 12 purchasable parts initially; expanded racing, traversal, utility and cosmetic families in the ownership spec |
| Collectibles | Five placements using one reusable system |
| Buildings | Garage, shop and a few reusable exterior pieces |
| Audio | Basic engine, tyres, impacts, UI feedback and one ambience loop |
| Persistence | One local save slot with a versioned data format |

## Build order and decision gates

| Stage | Deliverable | Evidence needed before expanding |
| --- | --- | --- |
| 1. Driving study | Primitive car, test course, camera, reset | Comfortable town-speed turning; predictable braking, slopes and recovery; keyboard and controller trials |
| 2. Small journey | Greybox town and scenic loop | A new player can follow the route and notice the optional detour without constant instructions |
| 3. Adventure loop | Dialogue, delivery, reward, garage and upgrade | One complete progression loop works; reward cannot be collected repeatedly by mistake |
| 4. Race | 24 cars, countdown, laps, full placing, payouts and retry | Fixed rival setups, no recurring opening-bend jams, ordered gates, fair rewards and measured stock-to-upgraded improvement |
| 5. Persistence and art | Save/load, original assets, sound and readable UI | Full loop survives quitting; imported assets have correct scale, pivots, materials and collision |
| 6. Playable build | Windows export and playtest notes | The exported build runs outside the editor and meets the agreed performance target |

These are milestones, not calendar estimates. Estimate later stages after stage 1 demonstrates the actual handling and iteration workload. A substantial multi-region adventure will require sustained content creation and playtesting beyond this slice.

## Technical direction

17 September migration update: the following Godot controller discussion describes the preserved prototype. The Unity trial uses a Rigidbody with force-based forward/lateral movement and explicit grip-limited yaw, retaining the existing arcade power relationships. Physical collision, slope/suspension behaviour and player feel still require Unity trials. The goal remains the complete connected adventure, equipment and race slice; the migration test circuit does not replace that scope.

Start with a custom arcade controller using CharacterBody3D, with explicit acceleration, braking, lateral grip, gravity and ground alignment. This is a prototype choice to favour predictable handling. If ramps, suspension and physical collisions require more convincing responses, compare a RigidBody3D controller before building the world around it.

Keep the visual car separate from the collision body so we can add body lean and wheel animation without destabilising movement. Make the follow camera adjustable and test collision avoidance around buildings.

Godot's documentation flags limitations in VehicleBody3D. Do not assume that adding that node provides finished handling. [Official VehicleBody3D documentation](https://docs.godotengine.org/en/stable/classes/class_vehiclebody3d.html).

Store vehicle tuning, parts, dialogue, quests and race definitions as data separate from their presentation. Use stable IDs for quests and collectibles so saves survive scene changes. The initial AI can follow authored paths with speed targets at corners; test recovery before adding traffic or elaborate overtaking.

## Main risks and responses

| Risk | Response |
| --- | --- |
| Driving feels dull or frustrating | Resolve it on a test course before investing in polished scenery |
| World becomes large but empty | Start with a short route offering frequent meaningful discoveries |
| Art consumes the project | Reuse a small modular building and prop set; prove one vehicle import first |
| AI cars repeatedly get stuck | Add recovery and repeated race tests before extending tracks |
| Progress is lost or duplicated | Make rewards idempotent; version save data and test interrupted progress |
| Too many systems arrive at once | Finish the first slice before adding fuel, town growth or minigames |

## Later possibilities

Before expanding the map, build a **depth prototype**: road-versus-dirt tyre trade-offs, one equipment-enabled alternate route, and one recruited resident who opens a service. This checks whether exploration, customisation and community progress reinforce one another. See the mechanics document for the proposed chain and test criteria.

Additional regions, recruitable residents, an expanding home town, teammate racing, day/night ambience, photo challenges, radio programmes and traversal upgrades can all serve the broader vision. Multiplayer, console releases and a seamless continent remain outside the initial scope. The user-requested varied car catalogue is now explicitly included in the staged body-roster plan in document 13.

## Collaboration

The user supplies creative direction and playtest reactions, and can model assets in Blender if desired. Development work covers scripts, scenes, import conventions, game systems and builds. Handling quality must be judged by playing: successful code checks cannot establish whether driving feels good.

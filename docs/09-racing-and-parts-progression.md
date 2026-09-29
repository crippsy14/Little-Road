# Racing, parts and earning progression

**Updated 29 September:** superseded in part.
- **Class ladder, part steps and reward multipliers:** now in [doc 44](44-parts-tiers-and-progression.md): C Street, B Tuner, A Touring, S Racing, E Endurance, with 3 steps per slot plus sidegrades, and multipliers B ×2.8, A ×6, S ×11, E ×19.
- **Unlocks:** class B opens globally when class C is complete.
- **Tracks:** Quarry Link and Ridge Run are now class C events in region 3 of [doc 45](45-roadmap-and-feature-spec.md).
- **Grid:** 11 m rows with the pole car on the line (study 128).

The earning principles below (every finish pays, stock struggles, no rubber-banding) still hold.


16 September 2026. User-directed scope change, researched before implementation. This supersedes the earlier three-opponent/two-upgrade scope. The existing 0.1 executable is an engineering prototype and does not yet satisfy this revision.

## Binding direction

- Initial complete build: at least 24 simultaneous entrants, including the player (23 AI opponents).
- A stock car should struggle to reach 12th. Skill must still matter; never force a finishing position.
- Every valid finish earns money. Repeating races funds improvements from backmarker to midfield, front runner and winner.
- Expand functional parts beyond an engine switch and one tyre choice. Ownership and fitted equipment are separate; keep and refit old parts free.
- Each added track must have its own unlock, surface/geometry demands, rival setups and class progression. Later classes should demand stronger and better-matched builds.
- Preserve exploration, resident quests and the visual target alongside racing.

## What the reference actually documents

These are community reports, translated where necessary, rather than claims from hands-on testing or access to source code. Regional differences remain possible.

The HG2 progression guide describes C → B → A → Super A licensing, with top-six results across a class's events allowing advancement. It lists six C events and nine each for B and A, followed by a championship and a final rival. Teammate results can also count. [HG2 progression chart](https://w.atwiki.jp/choroq_game/pages/61.html).

The course catalogue separates track and class, reusing some venues across classes, and lists increasing prizes by class and placing. Its table pays the top six. Exact payouts are not portable evidence: this Japanese reference lists 800 for a C win, while the English walkthrough describes two opening wins yielding 600 total. The discrepancy is unresolved; our economy below is original. [HG2 courses and prizes](https://w.atwiki.jp/choroq_game/pages/118.html), [English walkthrough](https://gamefaqs.gamespot.com/ps2/915200-road-trip/faqs/21740).

The Peach circuit guide reports 24-car races, traffic bunching, and a stock setup capable of winning the opening C event. Its B-class advice instead recommends a coordinated tyre, engine, chassis, transmission, steering and brake setup. Our hard stock-car opening is therefore a deliberate departure. [Peach circuit guide](https://w.atwiki.jp/choroq_game/pages/169.html).

The user recalls opponents stopping at the opening S-bend, making stock wins much easier. The guide corroborates opening-S traffic bunching and passing opportunities, but the exact stopping defect has not been independently reproduced. Treat this as a concrete failure case to avoid: do not reproduce a field-wide bottleneck and mistake it for balanced starter difficulty. Test tight alternating bends with the full grid, multiple starting orders, a slow/stopped obstacle and safe overtaking. No routine AI recovery should be necessary at an ordinary opening bend; investigate repeated stops over two seconds and record all recoveries.

The parts catalogue distinguishes surface-specific tyres, engine power and consumption, chassis mass, transmission gearing, steering response and brake response. Taller gearing can underperform with insufficient engine power; shorter gearing can suit a tighter course. Big tyres also affect obstacle traversal. These interactions support matching parts to routes rather than buying a single universal speed number. Internal values listed by the wiki are not adopted as our physics formulas. [HG2 parts catalogue](https://w.atwiki.jp/choroq_game/pages/152.html).

## Our race structure

Use 24 real entrants with individual progression, laps, ordered gates and finish times. Give each a stable identity, colour, equipment loadout and driving profile. A dense pack must brake, leave room, pass and recover; duplicating the current three moving models 23 times is insufficient. Countdown, position/24, lap display, results, payout breakdown, retry and return-to-world are required.

No automatic scaling to the player's purchases. An event's rival loadouts are fixed by track and class; modest seeded driver variation gives variety. Upgrading should make a previously hard race visibly easier. No hidden speed boosts or forced placement to meet the target finish band. Difficulty should come from equipment, lines, braking, traffic and the track.

Grid starts at 24th in the opening progression study, with a safe two-wide grid. Test randomised starting slots later, separately from equipment balance. Use a physically driven controller for rivals with authored road guidance, corner-speed planning, traffic avoidance, collision response and recovery behind the last valid gate. Do not award gate progress on recovery.

## Parts catalogue to build

Values are initial design targets, not measured handling or copied original stats. Stock parts remain owned. Each slot permits one equipped part.

| Slot | Opening choices | Difference the driver should feel | Later expansion |
| --- | --- | --- | --- |
| Engine | Stock; workshop tune; club motor | Acceleration and hill pulling; grip can limit usable power | Sport, race, endurance variants |
| Tyres | Stock; touring; road sport; trail | Cornering and traction by road surface; trail loses some tarmac precision | Wet, slick, rally, snow |
| Transmission | Stock; close ratio; long ratio | Strong corner exits versus higher speed ceiling; weak engines cannot exploit long gearing | Adjustable final drive within bounded ranges |
| Brakes | Stock; sport pads | Shorter braking zones with sufficient tyre grip | Race pads, endurance consistency |
| Steering | Stock; quick rack | Quicker response and tighter low-speed turns, without creating unlimited tyre grip | Adjustable sensitivity and advanced rack |
| Chassis | Stock; light chassis | Better power-to-weight, braking and response | Stronger lightweight chassis with rough-road trade-offs |
| Suspension | Stock; road kit; trail kit | Body control versus clearance/rough-ground stability | Race, rally and specialist travel kits |
| Aero / special | Deferred from opening purchases | Downforce trades speed for fast-corner grip; traversal changes routes | Wings, limited boost and exploration tools |

Opening implementation minimum: six functional slots (engine, tyres, transmission, brakes, steering, chassis), at least 12 purchasable parts plus stock equipment, with two useful setups. This is a milestone, not the full ownership specification: [the expanded parts document](10-expanded-ownership-and-parts.md) covers all original categories and our additions. Suspension is a subsequent extension once terrain and suspension behaviour support it; do not sell inactive stats. Cosmetic bodies and paint do not erase investment in functional parts.

The garage should show price, owned/fitted state, before/after stats, trade-offs, class availability and a suggested use. The driver chooses whether to fit a purchase. Switching owned setups costs nothing. No wear, repair bill or entry fee in the opening economy.

## Opening economy: initial tuning proposal

Use credits. Start with 0 racing credits on a fresh progression test; the car is complete and driveable. Existing saves must migrate without losing already purchased equipment or money. Adventure rewards remain an alternative source, but their combined opening value must not instantly buy a winning car.

Target 2–3 minute races, three laps where the current route supports that duration. A completed valid C race pays:

| Finish | Credits |
| --- | ---: |
| 24–19 | 100 |
| 18–13 | 130 |
| 12–9 | 170 |
| 8–5 | 220 |
| 4 | 280 |
| 3 | 330 |
| 2 | 400 |
| 1 | 500 |

These are total payouts, not bonuses to stack. All finishers are paid; quitting or skipping gates pays nothing. Award once per completed event. Early DNF has no monetary penalty. A retry creates a fresh event identity. Rewards and records must be saved together so reopening a results screen cannot pay twice.

Starting part prices: quick rack 250, sport pads 300, touring tyres 350, workshop engine 450, trail tyres 450, close-ratio gearbox 500, light chassis 650, road-sport tyres 750, long-ratio gearbox 800, club engine 1,000, race pads 900, precision rack 850. Later catalogue prices follow measured gains, not just ordinal tiers.

First useful purchase: about three backfield finishes (250 / 100 rounded up). A 450-credit engine: five such finishes. A mixed early kit of rack + touring tyres + engine costs 1,050, or eleven minimum-payout finishes before accounting for improving results and adventure earnings. This is arithmetic, not proof of enjoyable pacing. Target first win after roughly 30–60 minutes of learning and several purchases; revise from actual trials. The former 10–15 minute target now describes the introductory delivery/race session, not reaching a fully upgraded winner.

## Measurable progression targets

| Build on opening C track | Target with competent, comparable driving |
| --- | --- |
| Stock | Usually 18th–24th; top 12 requires exceptional driving or unusual race circumstances |
| One sensible upgrade | Tangible sector gain; typically several places or a smaller gap, not an automatic podium |
| Balanced early kit | Midfield becomes achievable, roughly 10th–16th |
| Developed C build matched to track | Top six and podiums plausible; wins require a clean drive |
| Strong C build in B | Falls back into a competitive midfield/backfield until further development |

These bands are acceptance targets, never finish-order code. Measure acceleration, braking and lap times independently to establish that a part actually causes the gain. Use identical driver logic and race seeds for automated comparisons, then human trials to judge difficulty and enjoyment. Report distributions and anomalous traffic incidents; do not tune one scripted run to one exact result.

## Tracks and classes

Separate a track definition (geometry, surface sectors, gates, width, hazards and corner speeds) from an event definition (track, class, lap count, field loadouts, rewards and prerequisites).

| Content step | Track character | Unlock target | What it teaches |
| --- | --- | --- | --- |
| Initial track: Bayview Coast | Rolling tarmac, long climb, sweepers and village bends | Available immediately, Class C | Engine/gear interaction; grip and braking in traffic |
| Next authored track: Quarry Link | Rough sections, short straights and hairpins | Top 12 at Bayview C | Trail tyres, torque and close gearing |
| Next authored track: Ridge Run | Longer straights, elevation and harder braking zones | Top 8 at Quarry C | Power, gearing, chassis and brakes together |
| Class B return events | Stronger cars on familiar and new tracks | Top six on every released required C event | Car development and more consistent driving |
| Class A and championship | More demanding configurations and specialist events | Class B record requirements | Full setup choices and series consistency |

Only authored, validated tracks may unlock. A preview card must say a planned track is unavailable rather than sending the player to missing content. Preserve earned licences when new tracks are added; version championship requirements so an update never silently revokes progress. Earlier classes stay replayable for income, with fixed opposition. Class reward multipliers start at C 1×, B 1.8×, A 3×, subject to credits-per-minute testing; higher-class part prices rise accordingly.

The initial complete build must demonstrate 24-car racing and the stock-to-upgraded earning loop on the first track. More tracks expand that structure as they are authored. A second class on the same layout must be named as a class event, not presented as a new track.

## Implementation order and acceptance

1. Finish this research/design record and update the brief, backlog and goal gates.
2. Add a data-driven parts catalogue, owned/equipped inventory, modifiers and save migration. Validate purchase affordability, unique ownership, free refitting and actual controller effects.
3. Replace the race with a 24-car event system: countdown, multi-lap progress, physical opponents, full placing, results and payout for every finish.
4. Run repeated rendered/headless races; correct pile-ups, stalled rivals, checkpoint exploits and finish-order errors. Measure 1080p frame times while the pack is visible.
5. Balance stock, individual upgrades and combined builds against the same field. Record finish distributions and time-to-purchase. Human playtests must establish that struggling feels motivating rather than arbitrary.
6. Author the next track and its class-specific event data, verify unlocks and persistent records, then repeat balancing and visual review.

Research gate is complete at the design level. Exact original-game physics, region-dependent payout tables and firsthand feel remain unverified. Our prices, unlock conditions, field loadouts and performance targets above are original proposals and must be tuned from the implemented game.
# Live driving feedback: track width, acceleration and recovery

19 September expansion: [document 18](18-endurance-fuel-and-mass.md) adds required endurance events, load-dependent fuel usage, tank capacity/empty-mass upgrades, changing fuel weight, tyre life, pit services and strategic AI. The opening sprint economy's no-wear/repair-bill promise does not exclude endurance tyre degradation; initial event services are included, with race time as their cost. [Document 17](17-scope-and-priorities.md) sets the 12-hour content target and current priority order.

16 September 2026, after the user watched the race test. These are priority requirements for the next driving iteration:

- Cars that crash or leave the road must rejoin reliably and behind their next required checkpoint. An opponent staying off-course is a defect even if other cars finish.
- The race course must be substantially wider than the current village road, with a racing surface, runoff, varied corner radii and clear track edges. Preserve the village/adventure character outside events while making the authored racing route feel like a circuit.
- Drivers must use the available width and choose lines around traffic. Avoid two fixed queues that make the race feel like vehicles on rails.
- Acceleration and the gradual approach to available top speed must matter. Initial stock tuning must not deliver near-instant maximum speed. Engine force should taper with speed and interact with gearing, mass, slopes and braking zones.
- Different equipment and drivers should produce meaningful speed differences throughout a lap, with corner exits, straights and braking creating passing opportunities. Avoid long stretches where the entire pack simply sits at a similar speed cap.
- Fine numerical tuning can follow, but these behaviours are prerequisites for accepting the balance report. Earlier narrow-course results remain diagnostic evidence, not final intended race design.

The subsequent power-and-grip decision and implementation boundary are recorded in [the evening handling handoff](14-power-grip-and-evening-handoff.md). Engine power, turbo and gearing must work with tyre grip, rather than act as independent speed-cap bonuses.

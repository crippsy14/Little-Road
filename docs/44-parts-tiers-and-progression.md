# Parts tiers, names, looks and the progression economy

Written 29 September 2026 for Richard's request to plan how the parts split between tiers, name them, plan their look, and balance progression early around tracks, races and grinding. This is a plan for review, not yet implemented. It builds on [09](09-racing-and-parts-progression.md) (economy and classes), [10](10-expanded-ownership-and-parts.md) (the >60 variant target), [13](13-car-roster-and-collisions.md) (bodies per class), [17](17-scope-and-priorities.md) (12-hour campaign) and [18](18-endurance-fuel-and-mass.md) (endurance).

## Five classes

This keeps Richard's shape and the existing C → B → A → S ladder, and adds E on top.

| Class | Name | Fantasy | Bodies on the grid | Where |
|---|---|---|---|---|
| C | **Street** | Your first car and bolt-ons from the local garage | Compacts, vans, small saloons | Bayview, Hillcrest (today) |
| B | **Tuner** | Aftermarket catalogue parts and weekend track days | Hot hatches, tuner coupés, rally-bred saloons | Hillcrest's valley shop, then a new hill/quarry region |
| A | **Touring** | Works touring cars: door-to-door on real circuits | Touring saloons and GT-lite coupés | A coastal-city / street-circuit region |
| S | **Racing** | Purpose-built race machinery | GT racers, sports racers, open-wheel | A grand-prix park |
| E | **Endurance** | Prototype hybrids, day into night | Le Mans-inspired prototypes and GTs (doc 15) | An endurance ring with pit lane (doc 18) |

## How many parts per slot and class

**Recommendation: three steps per slot per class (Good, Better, Best), plus a specialist sidegrade only where there's a real trade-off.**

Richard suggested four per slot per tier. I'd go with three:
- **Four steps make each purchase too small to feel.** They split a class's gain into about 5% steps, and at that size players can't feel the difference on the road.
- **Four steps means a lot of modelling.** It would be 140 parts, and many would be near-duplicates.

With three steps, each purchase is an obvious jump (about +10% on its main stat). The step into the next class then feels like a new era.

Sidegrades (tyres for gravel or wet, gearboxes short or long, an efficient engine for endurance) add choice without adding rungs to the ladder.

That gives 7 stock parts + 105 ladder parts + about 12 sidegrades, roughly **125 parts** across the campaign. This fits doc 10's ">60 meaningful variants" and doc 17's scope.

**Rules for the ladder:**
- **Within a slot:** Better is about 10% up on Good, and Best about 10% up on Better, on the slot's main stat. The next class's Good is about 5% over this class's Best.
- **A fully built class-C car** still sits in the backfield or midfield of a class-B race (doc 09's target). Upgrading carries you forward, but you can't skip a class.
- **Stock parts are always owned.** Old parts stay owned and fitting is free (as now).

## Names

These are working names; brands and flavour text come later.

| Slot | C Street | B Tuner | A Touring | S Racing | E Endurance |
|---|---|---|---|---|---|
| **Engine** | Workshop tune · Club motor · Sport 1.6 | Twin-cam 16V · High-comp twin-cam · Valley V6 | Touring 2.0 · Touring 2.0 Evo · Works 2.0 (throttle bodies) | Race V6 · Race V8 · Screamer V10 | Hybrid V6 · Hybrid V8 · Twelve-hour hybrid V8; sidegrade: **Economy hybrid** (range) |
| **Turbo / induction** | Street turbo · Club turbo · Sport turbo | Big-single · Twin-scroll · Twin turbo | Touring twin-scroll · Works turbo · Anti-lag system | Race turbo + intercooler · Twin race turbos · Qualifying boost | E-turbo · ERS pack · Twin e-turbo |
| **Tyres** | Touring · Road sport · Club sport; sidegrade: **Trail** | Sport · Hill grip · Tuner semi-slick; sidegrade: **Rally** | Semi-slick · Touring race · Works race; sidegrades: **Wet**, **Gravel** | Hard slick · Medium slick · Soft slick; sidegrade: **Race wet** | Endurance hard · Endurance medium · Qualifier; sidegrade: **Night/wet** |
| **Transmission** | Close-ratio · Five-speed sport · Short-shift; sidegrade: **Long-ratio** | Six-speed · Short-shift six · LSD six-speed | Touring close six · Touring sequential · Works sequential | Sequential dog box · Paddle shift · Seamless shift | Endurance paddle · Hybrid seamless · Twelve-hour box; sidegrade: **Low-drag long box** |
| **Brakes** | Sport pads · Race pads · Big-bore front | Drilled two-pot · Braided big brake · Ceramic | Slotted four-pot · Touring big brake · Cooled works brake | Six-pot monobloc · Carbon race · Brake-by-wire | Carbon-ceramic · Regen hybrid · Twelve-hour package |
| **Steering** | Quick rack · Precision rack · Sport rack & wheel | Quick-ratio · Rally rack · Tuner wheel & rack | Touring rack · Quick-release wheel · Works rack | Race rack · Paddle wheel · Assisted race rack | Endurance display wheel · Hybrid assist rack · Night-stint wheel |
| **Chassis** | Light chassis · Seam-welded · Stripped shell | Strut-braced · Half cage · Carbon panels | Touring cage · Lightweight touring · Works shell | Race tub · Carbon race tub · Ground-effect floor | Endurance monocoque · Hybrid carbon tub · Low-drag tub |

**Moving today's parts:**
- **Bayview parts:** these become class C, mostly as they stand.
- **Hillcrest's valley parts:** the Valley V6, twin turbo, hill grip, rally tyres, ceramic brakes, rally rack, carbon chassis and six-speed become class B, sold at Rosa's once B opens.
- **Holes to fill:** where a C "Best" doesn't exist yet (Sport 1.6, Sport turbo, Club sport tyres, Short-shift, Big-bore front, Sport rack & wheel, Stripped shell), it's new.

## How each class looks

These are the rules for `source-art/build_part_previews.py`. Each class has its own family look, and the three steps within it add detail and the step colour. The shape changes between classes, not just the colour.

| Class | Materials and colour | Shapes that say "this class" |
|---|---|---|
| C Street | Cast iron, zinc, black rubber, grey-teal to teal to red paint (today's scheme) | Inline-4 with a round air cleaner, steel wheels with vent holes, single-pot sliding callipers, plain ladder frames, thin two-spoke wheels |
| B Tuner | Anodised purple and blue, polished alloy, braided steel hoses, bright filter socks, blue silicone | Twin-cam heads with cam-cover logos, cone filters, 5-spoke and split-spoke alloys, drilled discs and two-pots, strut braces, a bucket-seat hint, three-spoke suede wheels |
| A Touring | Works livery in white and red, lightweight grey castings, gold anodising | Individual throttle bodies, dry-sump tank, multi-spoke forged wheels, slotted discs with four-pots, roll cage, quick-release hubs |
| S Racing | Carbon weave, gold heat-shield foil, heat-tinted titanium (blue and purple) | V8/V10 with trumpets, sequential box with paddles, centre-lock wheels and slicks, six-pot monoblocs, carbon tub, flat-bottom wheel |
| E Endurance | Carbon plus endurance white and amber, orange high-voltage cabling, LED details | Hybrid battery pack and e-motor housing, e-turbo, carbon-ceramic discs, quick-change wheel nuts, fuel cell with quick-fill (doc 18), display steering wheel |

The step colour inside a class stays grey-teal (Good), teal (Better) and red (Best). Each class then adds its own material family on top. Hillcrest's amber becomes the Tuner purple when those parts move to B.

## Economy and campaign length

**Assumptions (arithmetic only, to be checked in play):**
- **Payouts:** C races pay 100–500 by place (doc 09). A player progressing through a class averages about 250 credits per race.
- **Class multipliers:** other classes scale that by a reward multiplier, set slightly above the price multiplier, so later classes don't grind longer.
- **Side income:** exploration, deliveries, postcards and trials cover about a quarter of each class.
- **Endurance:** E events are about three times as long and pay three times as much.

**Build definitions:**
- **Competitive build:** Better in 4 key slots and Good in the other 3.
- **Podium build:** Best in 4 slots and Better in 3.
- **Everything:** every part in the class. This is optional collecting, the grind for completionists.

| Class | Step prices (Good / Better / Best) | Competitive build | Podium build | All parts | Average pay per race | Races to competitive | Races to podium build | Races for everything | Racing time to podium build |
|---|---|---|---|---|---|---|---|---|---|
| C Street | 300 / 650 / 1,100 | 3,500 | 6,350 | 14,350 | 249 | 10 | 22 | 54 | 1.5 h |
| B Tuner | 780 / 1,690 / 2,860 | 9,100 | 16,510 | 37,310 | 696 | 9 | 20 | 50 | 1.3 h |
| A Touring | 1,620 / 3,510 / 5,940 | 18,900 | 34,290 | 77,490 | 1,491 | 9 | 19 | 48 | 1.5 h |
| S Racing | 3,000 / 6,500 / 11,000 | 35,000 | 63,500 | 143,500 | 2,734 | 9 | 20 | 49 | 1.6 h |
| E Endurance | 5,100 / 11,050 / 18,700 | 59,500 | 107,950 | 243,950 | 14,169 | 3 | 6 | 16 | 1.9 h |

**Campaign totals:**
- **Racing time:** about **3.7 h** to reach a competitive build in every class and **7.8 h** to reach podium builds.
- **Everything else:** at the 12-hour minimum, that leaves about 4 h for exploration, residents and deliveries. The campaign is open-ended (Richard, 29 Sep): 12 hours is a floor, not a cap, and more regions, events and activities simply add to it.
- **Completionist grind:** buying every part is roughly 50 races per class.

**Rewards and prices to change:**
- **Class multipliers:** doc 09's rewards and part prices stay, apart from the class multipliers. These become B ×2.8, A ×6, S ×11 and E ×19 per sprint-equivalent. Today they're B 1.8 and A 3.
- **C prices:** these settle to the table. Today's prices run from 250 to 1,400, and most move less than 30%.

## Tracks and events this implies

To reach a podium build takes about 20 races per class. Four or five events per class means each is raced three to five times, which is enough to learn it without feeling like a grind.

The unlock rule stays: a podium at every event of a class opens the next class (RaceCalendar today).

| Class | Events | Venues |
|---|---|---|
| C | 3 | Bayview Coast; Hillcrest Raceway; Castle Hill sprint (the climb as a race) |
| B | 4 | Valley back roads; Quarry Link (gravel, from doc 09); Ridge Run (doc 09); Hillcrest Raceway reversed |
| A | 5 | A new coastal city: harbour street circuit, touring circuit (full and club layouts), night street race, Bayview Coast extended |
| S | 5 | The grand-prix park: GP layout, national layout, a wet race, sprint and feature races |
| E | 3 | The endurance ring: a 45-minute stint, a 90-minute race, and a finale that runs day into night (pit stops, fuel and tyres from doc 18) |

That's **20 events** on about **12 layouts** in **6 regions**. Each region has a parts shop that stocks its class and every class below it, and the body shop follows the same pattern.

## Decisions for Richard

1. Class letters and names: **C Street, B Tuner, A Touring, S Racing, E Endurance**. OK?
2. **Three steps per slot per class plus sidegrades (about 125 parts)**, instead of four (140+ near-duplicates)?
3. Move Hillcrest's valley parts to **class B**?
4. Endurance as the top class, with doc 18's fuel and pit systems arriving with it?

Once these are agreed, the next steps are:
- change `parts.json` to the new ladder (C complete, B from the valley parts);
- re-price class C and set the new reward multipliers;
- extend the Blender part script with the class looks;
- run the balance checks on the existing proving-ground and race harnesses.

## Richard's decisions (29 September) and what they change

1. **Classes confirmed:** C Street, B Tuner, A Touring, S Racing, E Endurance.
2. **Three steps (Good, Better, Best) per slot per class, plus sidegrades, and a "fourth" item: a chassis platform that gates each class.** Specific bodies fit specific platforms. This makes body buying part of progression and gives a visual step between classes.
   - **The chassis slot splits in two:**
     - **Platform:** bought once per class. It sets the class, the bodies that fit, and which class's parts can be fitted.
     - **Frame:** the in-class weight ladder (C: Light, Seam-welded, Stripped shell), as before.
   - **Bodies per platform:** a few new bodies plus **kit versions of earlier favourites**. For example, the Puddle, then Puddle Tuner (flares, wing), then Puddle Touring (cage, splitter). A favourite car stays viable (doc 10), and kits are cheaper to model than new shells.
   - **Size bands:** bodies on one platform share a size band, which suits the per-body collision boxes (29 Sep) and keeps each class's field coherent.
   - **Pricing:** the platform is the class's milestone purchase, about the cost of that class's competitive build. Its parts can't be fitted until it's owned.
3. **Hillcrest's valley parts stay class C.**
   - **Class C** is the whole first set of regions. A global **class B** opens when all of class C is complete, and when the map and number of races make sense for it.
   - **Early test cars:** B, A, S and E cars can be built early to test their racing AI, without being in the campaign yet.
   - **New parts by location:** each new location's shop brings 2–3 local specialities. They're revealed when the location is discovered (fog of war to garage), and some are locked behind a results tracker. For example, a better engine in the fourth location needs 3 podiums.
   - **Gating rule:** trackers count results that can be earned more than one way (podiums at any C event, not one specific race), so one hard track can't block progress. The locked part's card shows the tracker (1/3).
   - **Economy:** class C becomes the longest class (roughly 3–4 h rather than 1.5 h). Rebalance the table once the number of C locations and events is fixed.
4. **Tyre wear, fuel and pit stops arrive in class A:** "you've made the professional league".
   - **Fuel:** a GT-style fuel mix (about 1–6), changeable during the race. Burn scales with throttle × delivered power × mix; a higher mix gives more power for more fuel. Engines get an efficiency figure beside power.
   - **Tyres:** compounds with their own wear rates, worn faster by load and sliding (doc 18).
   - **Pit lanes:** class A venues are designed with pit lanes from the start.
   - **AI:** strategy for pits and fuel mix is AI work under the keep/reject protocol, so it starts before class A content.
   - **Class B** stays pure driving and upgrades. **Class E** uses the same systems at long-stint, day-to-night scale.

**Next:**
- class C in `parts.json` (3 steps per slot, frame rename, local specialities, trackers);
- the platform and body-kit model;
- re-price C and rebalance per region;
- extend the part script with the class C look.

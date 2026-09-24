# Meaningful car ownership: expanded specification

16 September 2026. Replaces every earlier reference to a two-upgrade limit. The six-slot implementation milestone is a foundation, not the complete equipment ambition.

19 September expansion: fuel tanks are required functional equipment, with independent capacity, empty mass and starting fuel load. Powerful engines consume fuel according to demand and efficiency; fuel mass affects the shared driving model as it burns. Endurance tyre life and pit strategy are required campaign systems, specified in [document 18](18-endurance-fuel-and-mass.md). Ownership presentation must use the researched category list and 3D part carousel in [document 19](19-parts-browser-reference-and-spec.md). The current implementation has seven slots including turbo; earlier six-slot language below describes an intermediate minimum.

## Reference coverage

Reviewed every category in the [HG2 parts catalogue](https://w.atwiki.jp/choroq_game/pages/152.html), including stock, improved and specialist entries. Its category map is:

| Category | Documented range |
| --- | --- |
| Tyres | Road, wet, off-road, snow, oversized, exceptional |
| Engines | Power tiers, efficiency differences, exceptional |
| Chassis | Weight tiers |
| Transmission | Gearing tiers |
| Steering | Response tiers |
| Brakes | Different response profiles |
| Wheels | Decorative designs |
| Lights | Standard, fog, spot |
| Wing set | Air control/downforce |
| Special propulsion | Propeller, jet turbine |
| Options | Water skis, flight wings, patrol lamp, advertising signs |
| Stickers | Decorative livery |
| Horns | Sound choices |
| Meters | Instrument designs |

This is complete category coverage of that catalogue, not a claim to have tested every item or reconciled every regional name. Original numerical balancing and exact item names remain in the linked reference; our designs below use original names and values.

Body choice also spans many shapes, including unusual vehicles. [HG2 body catalogue](https://choroq.fandom.com/wiki/Body_List_-_Choro-Q_HG_2). Paint and functional equipment should therefore support attachment to a favourite vehicle rather than requiring a new identity at every performance tier.

The adjacent item catalogue also includes garage interiors, display rewards and quest objects; these are ownership/world progression, not engine modifications. [HG2 items](https://w.atwiki.jp/choroq_game/pages/172.html). Coin Radar is a separate navigation upgrade revealing collectibles on the map. [Coin Radar](https://choroq.fandom.com/wiki/Coin_Radar).

## Revised ownership requirement

The player develops a persistent, recognisable car with a collection of interchangeable equipment, saved setups and visible history. Meaningful ownership includes performance, route access, utility, visual identity and investment in a home garage. A high purchase price alone is not meaningful depth.

Owned equipment is retained when removed. Refitting is free. Stock equipment remains available. Parts can have upgrades within a role and sidegrades between roles. Display real trade-offs and compatibility before purchase, and show their effect through driving, model changes, sound or world interactions. Cosmetic choices must not secretly determine race performance.

## Our expanded equipment families

The following is an original design specification. Variant names/counts are development targets, not original-game facts or claims that these systems already exist.

| Family | Planned functional choices | Decisions and connections |
| --- | --- | --- |
| Engines | Entry, workshop, club, sport, race, torque, endurance | Acceleration, usable torque, speed potential and energy consumption; keep a modest efficient option useful |
| Tyres | Touring, road sport, semi-slick, slick, wet, all-terrain, rally, mud, winter, studded, oversized | Surface-specific grip, stopping, drag and obstacle clearance; expensive slicks should remain bad on mud |
| Transmission | Stock, close, balanced sport, long, rally low-range, race adjustable | Match engine and track; long gearing needs power, close gearing helps repeated acceleration |
| Chassis | Standard, light, reinforced, race light | Mass, response and stability; reinforced builds suit rough routes and carrying equipment |
| Brakes | Progressive, sport, race, endurance | Stopping response, modulation and sustained use; tyre grip remains a limit |
| Steering | Standard, quick, precision, stability-oriented | Input response and controllable turning; physical tyre grip cannot be bypassed by a rack |
| Suspension, new family | Road, sport, track, rally, expedition | Ride height, damping and rough-ground control; introduce after a working suspension/ground-contact model |
| Differential, new family | Open, road limited-slip, rally limited-slip | Traction versus turning behaviour; only sell once driven-wheel traction is actually modelled |
| Aero | Small road spoiler, low-drag race kit, high-downforce kit, adjustable air-control kit | High-speed grip versus drag; distinguish racing aero from flight hardware |
| Energy, new family | Standard, endurance reserve, lightweight race supply, boost reserve | Range, weight and boost use; later endurance events provide a reason to choose |
| Propulsion | Short-burst booster, efficient touring booster, aquatic propulsor | Overtaking or traversal, with resource costs and class restrictions |
| Traversal | Float/skimming kit, underwater kit, glider kit, oversized wheel/clearance package | Discover routes and optional challenges through equipment combinations |
| Lighting | Broad fog beams, distance lamps, trail lamps, cosmetic lamp styles | Read different environments at night; basic safe visibility remains available to everyone |
| Navigation and tools | Map, landmark journal, proximity finder, rally trip meter, route recorder | Finding, revisiting and learning routes; discovery tools hint rather than reveal everything immediately |
| Sponsor equipment | Local delivery sign, touring sponsor, regional race sponsor | Alternative earning goals for exploration and racing; rewards have clear eligibility and cannot be farmed by idling |

This targets over 60 functional variants across the eventual game, developed alongside suitable tracks and regions. It is not a promise to implement dozens of inactive menu entries in the first build. Every family needs a behaviour test and a place where its choice matters.

## Personalisation and garage life

The user-requested road-car-to-F1-style body catalogue, class-specific field mixes and collision requirements are specified in [Car roster and collisions](13-car-roster-and-collisions.md). Recolours do not count as different bodies; body identity remains separate from fitted performance.

Support original interchangeable bodies; primary/secondary paint and finishes; wheel styles/colours; number plates and race numbers; decals and earned liveries; horn sounds; visible fitted equipment; instrument themes; garage interiors, trophies and photographs. Keep performance parts compatible across ordinary bodies so a favourite compact remains viable late in the campaign. Specialist bodies may eventually have declared mount differences, but no hidden performance advantages.

Save named setups such as Coastal Club, Quarry Rally and Long Tour. Preview the entire setup's stats, cost of missing parts, class eligibility, surface strengths and fitted appearance before confirming. Let the player compare against a personal best rather than an opaque overall rating alone.

Part discovery should come from several sources: shops, race milestones, resident services, exploration rewards and late-game specialist workshops. Money buys development; licences and discovery broaden access. Preserve more than one useful purchase order. An early body/paint purchase must never make the player unable to earn money.

## Interactions we must prove

1. Engine plus suitable gearing beats either mismatch; a long gearbox can be a poor early purchase and the shop explains why.
2. Strong engine plus weak tyres produces traction/handling limits. Better brakes cannot exceed available tyre grip.
3. Road sport and trail setups trade places in performance on tarmac and rough ground.
4. Lightweight builds accelerate well; an expedition setup earns its weight through access, stability or range.
5. A new route yields a resident or part that changes future choices, connecting exploration with racing.
6. Lighting and navigation equipment improve a real night/rally activity; cosmetic lamps and instrument themes stay clearly cosmetic.
7. Advanced traversal combines compatible hardware and resources; race rules explicitly allow or prohibit it so flight cannot bypass circuit gates.
8. Old parts retain uses in alternate setups, earlier classes or future teammate cars.

## Staging, with no silent scope reduction

**First complete playable build:** six functional slots; stock plus at least 12 purchasable parts; useful road/trail choices; ownership separate from fitting; garage comparisons; save migration; 24-car racing; payouts for every valid finish; tested improvement through the field. This is now materially beyond two upgrades.

**Next content milestone:** a second authored track, class-specific competition, saved loadouts, upgraded garage presentation, suspension and meaningful terrain interaction, one equipment-enabled detour and one resident service. Equipment availability follows released content.

**Broader game:** extend the remaining families with their supporting systems: wet/snow surfaces, night driving, energy/endurance, aero, boost, water/air traversal, sponsorship and garage rewards. Class and event rules keep specialist equipment balanced. These remain in the specification; their implementation status must be explicit.

No part is marked implemented merely because a data entry exists. Acceptance requires its advertised physical or utility effect, correct buying/fitting/persistence, suitable interface and a tested use case. New parts must not make AI rubber-band to negate the purchase.

## Evidence limits

Research covers the original's full category range and related ownership systems. It is not a verified reconstruction of every regional item price, hidden formula or handling nuance. Suspension, differentials, expanded energy hardware and the variant targets above are our additions. The original's special end-game equipment is inspiration for optional late progression, not an excuse to make ordinary race balance irrelevant.

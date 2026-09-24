# Mechanics and system connections

16 September 2026. The user explicitly highlighted the reference game's mechanical depth. This document separates researched behaviour from proposed systems for our game.

## Additional documented behaviour

The community walkthrough describes engines with different power/energy use, tyre specialisation, chassis weight, gearing, steering and brakes. Fuel depletion reduces speed; entering buildings refills it. Up to two teammates can race with the player, earn extra winnings and improve with equipped parts over several races. Some events depend on time of day, and the endurance event includes pit stops. These are guide reports awaiting hands-on verification, not reverse-engineered formulas. [RPGamer's guide: parts, teammates, stamps and HUD sections](https://gamefaqs.gamespot.com/ps2/915200-road-trip/faqs/21740).

The community wiki distinguishes the ordinary Wing Set's air control from Flight Wing equipment. It describes the latter working with a Jet Turbine to enable flight at sufficient speed. The Jet Turbine also supplies powerful acceleration with heavy fuel consumption. These demonstrate interaction between equipment, traversal and resource use; exact thresholds need not be copied into our game. [Wing Set](https://choroq.fandom.com/wiki/Wing_Set), [Flight Wing](https://choroq.fandom.com/wiki/Flight_Wing), [Jet Turbine](https://choroq.fandom.com/wiki/Jet_Turbine_%28Choro-Q_HG_2%29).

Treat community statements about hidden behaviour and efficiency as hypotheses to verify. In particular, do not encode their numerical values as authoritative physics parameters.

## Why those interactions matter

Our analysis: the depth comes from systems changing the value of other systems. A part can affect the route you take, the route can reveal a character, and the character can create a new reason to revisit a town. A larger list of isolated races would not reproduce that relationship.

The first driving slice is a production milestone. It is not the full creative ambition. Before adding many locations, we should prove one satisfying chain of discovery, equipment choice and persistent world change.

## Proposed design model for our game

| System | A meaningful player decision | Connection to another system | Earliest useful test |
| --- | --- | --- | --- |
| Handling and surfaces | Take the smooth long road or rough shortcut? | Tyres and route geometry | First slice: feel different grip on dirt |
| Equipment | Prefer acceleration, grip or a specialist ability? | Race tactics and exploration | Multiple equipment slots with measurable effects and track-specific trade-offs |
| Economy | Buy a general improvement or save for an exploration tool? | Discovery rewards and shops | Quest/race payouts support more than one sensible purchase order |
| Quest state | Help a local immediately or investigate another lead? | Residents, landmarks and rewards | One delivery with persistent state |
| Collections | Follow a visible trail or search beyond the obvious route? | Landmark literacy and equipment | A collectible suggests an alternate path |
| Town development | Which new resident or service matters to the player? | Recruitment changes available activities | One recruit opens one useful service |
| Team progression | Invest in the player car or a teammate? | Parts economy and race results | Later experiment with one teammate before managing two |
| Resource use | Spend boost now or preserve range? | Equipment choice and journey length | A short controlled boost/fuel test after basic handling |
| World time | Travel now or return for a local activity? | NPC availability and ambience | One time-sensitive optional event, with clear feedback |
| Travel journal | Pursue a known goal or an unexplained hint? | Quest clarity and discovery | Track completed tasks and interesting places without revealing every secret |

Everything in this table is a proposal for our own game. It does not assert that the original uses these exact rules or interfaces.

## A compact depth prototype

Example scenario using provisional content:

1. A mechanic asks for a delivery to an isolated roadside stop.
2. The main road is always passable. A muddy shortcut is visible from it, making the geography memorable.
3. Completing the delivery earns enough for a choice: stronger acceleration or tyres suited to the rough route.
4. The tyres make the shortcut easier, and exploring it leads to a stranded travelling shopkeeper.
5. Helping that character brings a new service to town.
6. The service offers an item useful for a new optional challenge on a route the player already knows.

This uses a small amount of scenery more than once, but changes the player's purpose and options on each visit. Avoid making the wrong early purchase a dead end: the main road and repeatable earnings remain available.

Acceptance checks:

- A player can explain why they chose an upgrade.
- The change is perceptible in play, without relying only on a stat label.
- Finding the resident changes something visible and useful in town.
- Revisiting the route reveals a new possibility.
- Players who prioritise racing or exploring can both make progress.
- Saves preserve the service unlock, equipment and completed rewards.

## Architecture to support depth without building everything immediately

Define a small equipment data model with categories, prices, modifiers and optional capability tags. A capability such as `water_travel` can later be consulted by traversal and quest systems without scattering item-name checks throughout the code. Capability tags are our proposed architecture; they are not claims about the original engine.

Record progression as stable facts: quest completed, resident recruited, service unlocked, collectible found and event result. Dialogue, town scenes and shops can read those facts. Store facts in one versioned save model so a scene transition does not silently reset progress.

Keep cosmetics separate from functional equipment. A player should be able to develop a favourite car identity while changing its abilities. Prototype this distinction before creating a large body catalogue.

Avoid implementing a general-purpose framework before we have working examples. Build the delivery and one service unlock first, then extract reusable structures from their actual needs.

## Research still needed before detailed balancing

Observe equipment changes under comparable conditions, including fuel, surfaces, air control and race outcomes. Check teammate improvement over repeated trials rather than inferring it from one result. Document event prerequisites and the effect of recruiting specific residents. Record reproducible observations with video timestamps where available.

This research pass establishes a credible system map; it does not yet constitute an exhaustive quest catalogue, item database or verified reconstruction of the original game's balancing.

# Reference game research

Research date: 16 September 2026.

## Identity and scope

The confirmed reference is the PlayStation 2 game **Road Trip Adventure**, also released as **Road Trip / Everywhere Road Trip** and **Choro Q HG 2**. GameFAQs records E-Game as developer, the North American release in 2002 and regional title aliases. The user confirmed this is the intended game. [Release record](https://gamefaqs.gamespot.com/ps2/915200-road-trip/data), [regional naming overview](https://en.wikipedia.org/wiki/Road_Trip_Adventure).

The research below is a desk study, not a hands-on replay. Publisher claims and player-guide observations are identified separately. Proposed design choices are our analysis, not statements about the original game's implementation.

## Documented features

| Feature | Evidence from the reference | Source |
| --- | --- | --- |
| A society of cars | Car characters inhabit the world; winning its major championship offers a route to becoming president. | Publisher |
| Exploration and favours | Travel and helping other cars support the player's racing career. | Publisher |
| Parts and teammates | Equipped parts affect team performance; advertising and selling parts provide income. | Publisher |
| Customisation and extras | The publisher advertises more than 250 customisable parts, photography and a two-player head-to-head mode. | Publisher |
| An expanding home town | The walkthrough describes recruiting residents who establish buildings in My City. | Player guide |
| Multiple collection goals | Stamps, hidden coins and photo locations provide additional objectives. | Player guide |
| Diverse activities | The guide covers races, minigames, shops and teammate recruitment. | Player guide |
| A connected journey | The opening route takes the player from Peach Town to Fuji City and onward to My City. | Player guide |

Sources: [System 3's official game page](https://system3.com/product/road-trip-adventure/); [Steven Wootton / RPGamer's walkthrough, updated 22 March 2015](https://gamefaqs.gamespot.com/ps2/915200-road-trip/faqs/21740). The latter is community documentation, useful for feature detail but not a source for internal engine or physics claims.

The screenshot archive also documents the town view, dialogue, a body shop, a coin, nighttime driving and the progress interface. It is useful visual reference without assuming that screenshots reveal handling quality. [MobyGames screenshot archive](https://www.mobygames.com/game/34657/road-trip/screenshots/).

## Design interpretation

The strongest concept is that **driving is how you inhabit the world**. The car is simultaneously the player's character, means of travel and object of progression. Our game should make all three roles feel connected.

The following are design deductions from the documented features, to be tested in our prototype:

1. **Give driving somewhere a purpose.** A resident's request can introduce a route, a landmark and another character. A journey should advance more than a counter.
2. **Make the road itself enjoyable.** Gentle bends, elevation changes, readable junctions and enticing side paths should reward movement even when no event is running.
3. **Allow changes of pace.** A player can alternate between exploring, talking, collecting and competing. Racing should not consume every minute of the experience.
4. **Give upgrades a noticeable use.** Better tyres might make a rough shortcut easier; better acceleration might change how a race start feels. Avoid a shop full of imperceptible numerical changes.
5. **Make places memorable.** Distinct landmarks and local personalities offer a reason to remember a town beyond its position on a map.
6. **Reflect the player's progress in the world.** A repaired sign, a newly opened service or a returning resident can make an early, affordable version of town growth.

Our proposed repeating loop is: discover a place → meet a resident or find an event → complete a driving challenge → earn a useful reward → improve the car → explore further. This is a design model, not a precise reproduction of every original progression rule.

## What to carry forward, and how to adapt it

| Design goal | Proposed approach for our game | First-slice priority |
| --- | --- | --- |
| Toy-like personality | Original compact car silhouettes, expressive movement and readable colour palettes | High |
| Freedom to wander | One compact, continuous region with a loop and optional side path | High |
| Social adventure | Three distinct residents and short conversations | High |
| Meaningful car ownership | Persistent car identity, interchangeable multi-slot equipment and saved setups; see the expanded ownership specification | High |
| Competition | 24-car racing with repeatable earnings, parts progression and class-specific track demands | High |
| Discovery | Five collectibles, including one off the obvious route | Medium |
| Lasting community progress | One visible change after completing a resident's request | Later |
| A world-spanning adventure | Additional regions with their own identities | After the first slice works |
| Large activity catalogue | Additional minigames, teammate management and photography | Later |

These quantities are proposed scope limits, not counts from the original game.

## Original creative direction

Use new town layouts, characters, dialogue, vehicle designs, music and game branding. The reference supplies design inspiration; our project will develop its own identity. A possible premise is reviving a neglected touring festival by reconnecting small communities and earning invitations to local races. This is a suggestion for discussion, not an approved story.

## Remaining research

- Establish which memories matter most: exploring, racing, customisation, My City or the atmosphere. The user specifically highlighted mechanical depth; see the separate mechanics document.
- Review a representative opening, ordinary drive, race and shop visit in gameplay footage or a hands-on session. Record observations separately from assumptions.
- Measure perceived steering response, speed, camera distance, braking and recovery. The sources reviewed do not establish exact physics values.
- Observe how frequently landmarks, NPC interactions and rewards interrupt a normal journey. Use those observations to tune our route density, not to reproduce a map.
- Determine whether fuel, nighttime travel and unusual traversal equipment are essential to the user's vision before committing to those systems.

## Source quality notes

The publisher page establishes the core advertised feature set. GameFAQs supplies a release catalogue and a detailed community guide. MobyGames supplies visual evidence. Regional naming is cross-checked with a secondary overview. A manual archive was found but its page could not be retrieved, so no manual-specific claims are used. No game binaries or extracted assets were obtained, and no claim is made that the original was played during this research.

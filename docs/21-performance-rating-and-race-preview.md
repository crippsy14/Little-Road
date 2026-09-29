# Car performance rating and pre-race comparison

**Updated 29 September:** the preview adds each racer's **ability radar and driver rating** beside the car rating ([doc 45](45-roadmap-and-feature-spec.md) F2). Car rating and driver rating always stay separate.


**23 September implementation:**87–88 share one immutable entry roster between the full24-car preview and spawning, including actual fitted parts, model/paint, grid position and implemented driver traits. Equipment-only dry-road-v1 gives stock100, street128 and club177; it weights attainable speed, usable pull, dry grip, braking and yaw relative to stock. It is an initial estimate with broader fixture calibration still open. Previewing does not reroll form or consume an entry; confirmation freezes the fitted setup.88 also previews retries and preserves paid results on cancel. Functional connected tests pass through two races and a separate-process reload; a prolonged recovery cluster remains an AI issue. Physical controller and player usability review remain open. [Review](../review/unity-35/race-preview-88-review.md); next visual scope is [document39](39-driving-hud-and-playful-ui.md).

19 September original request. The user referenced Gran Turismo as inspiration for an understandable overall performance-points metric, not a request to reproduce its formula or interface.

## Requested experience

**22 September scheduling clarification:** once baseline racing feels competitive and driver personalities are implemented, make this preview and a broader UI polish pass the next player-facing milestone alongside bringing the improved racing AI into the adventure build. Show **every entrant's actual fitted loadout and driver personality**, including the player and teammates when present, with controller-readable detail rather than ratings alone. Keep equipment potential distinct from driver traits. Do not present fixture-only AI as integrated adventure racing: validate event entry, rear-grid start, finish/results, exactly-once payout, saved progression and DualSense use in the adventure build.

Give each equipped car a simple baseline performance rating that summarizes the contributions of its fitted parts. A crude first version is acceptable if it is consistent and useful. Before starting a race, show the player's rating alongside the opponents and, when teammates exist, their cars too. The player should be able to judge the relative strength of the field and see how upgrades change their prospects.

The preview must use the actual entered cars and equipment. It must not invent rival ratings, secretly upgrade the field after the player buys parts, or imply a predetermined finishing order. Make the preview readable with a controller and show meaningful changes when the player adjusts a setup.

## Design constraints for the later specification

- Start by evaluating a weighted parts-based score, as suggested by the user. Normalize unlike contributions rather than literally adding horsepower, grip and gear ratios. No formula, scale or class threshold is fixed yet.
- Respect the existing equipment interactions: gearing cannot manufacture engine power, tyres constrain usable power, mass affects acceleration and handling, and endurance fuel load can change performance. A simple metric may approximate these effects, but document its limitations and avoid obvious exploits such as unlimited points from unusable top-speed gearing.
- Keep **car potential separate from driver ability**. The car rating should summarize its equipment; the future driver/teammate/track-familiarity systems affect how well that potential is used. A stronger rating does not guarantee victory.
- Decide the reference conditions for the baseline (fuel load, tyre condition and surface) before balancing scores. A track-specific estimate or endurance suitability indicator can be considered later; neither is required by this initial note.
- Cosmetic body shapes and decals should not grant performance points merely for looking faster. Any real chassis/body performance difference must come from a documented functional attribute.
- Version the calculation and validate saved equipment against the current catalogue. Consider class restrictions only in a dedicated balancing decision; this request does not itself establish new performance caps or matchmaking rules.

## Validation and scheduling

Use the existing matched solo, passing and 24-car tests to compare scores with measured car potential, across multiple tracks and equipment combinations. Check weak/strong setups, upgrades with tradeoffs and visually different cars on equivalent equipment. Do not distort driving physics to force every rating to predict every result perfectly.

Continue the race-entry screen with [driver and teammate progression](20-driver-personality-and-teammate-progression.md), the [equipment design](10-expanded-ownership-and-parts.md) and the campaign/event system. The implemented first slice above does not complete teammates, class balancing or the final UI design.

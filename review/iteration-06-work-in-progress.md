# Driver variation and balance work in progress

Historical iteration. The evening source checkpoint changes handling and collision envelopes, and supersedes the implementation status below. See [iteration 07](iteration-07-evening.md). Preserve these earlier results as diagnostics; do not treat them as validation of the new physics.

16 September 2026. Goal active. User requested a current race screenshot during this work; [the fresh capture](captures/race-progress-06/05-racing.png) is actual rendered gameplay at 1920×1080, from current source. [The concept](../references/concepts/coastal-world.png) remains the visual target. The current source has not yet replaced the packaged Windows download from iteration 05.

## Implemented and checked

- Opponents keep racing behind the player-results screen. A scrollable full standings view displays all 24 drivers. Leaving results restores player collision; payout remains single-use.
- 38 functional checks pass, including the new result lifecycle checks.
- Recorded seeds vary pace, braking margin, small line offsets, consistency and following distance through actual control inputs. Normal races use their persistent race serial as the seed. Equipment and finishing times are not randomised.
- Driver following selects the closest relevant car. Passing checks lane clearance and keeps a physical following gap.
- Fixed false collision speed penalties caused by upward-facing road-joint contacts being classified as wall flags.
- Fresh physical cars isolate solo repetitions. Reusing a car was shown to contaminate repeated controls; pre-fix evidence is retained.

## Evidence collected

[Solo matrix](balance-solo.json): 25 complete runs, five equipment profiles, duplicate seed-zero controls plus seeds 11/22/33. Duplicate controls repeat exactly. All final solo runs have zero recoveries and zero genuine lateral-contact frames. Median varied-driver three-lap times are approximately stock 98.02 s, engine 94.95 s, tyres 97.67 s, balanced 92.98 s and developed 69.92 s. The start position is matched to the last slot in a 24-car race; the first lap includes that approach. Variation comes from driving inputs, not artificial timing jitter.

[Overtaking tests](overtaking-trials.json): six passing cases, two and three cars across three seeds. The stronger car starts behind, physically overtakes every opponent and wins; all entrants finish without recovery. These ran before the subsequent passing-clearance adjustment and need rerunning against that adjustment.

[First complete field matrix](balance-field-conservative-passing.json): nine 24-car races, all cars finished without recovery. Stock placements 22/17/22, balanced 19/19/18, developed 16/16/20. This is a failed balance result: the developed car's solo advantage is being lost in traffic. It is not ready for a tuning sense-check.

The same stock-player field showed natural rival position changes: rival 16 finished 13th/11th/18th; rival 18 finished 16th/15th/20th; rival 19 finished 21st/21st/14th. These demonstrate variable outcomes with fixed equipment, not a final approved variance distribution.

Alternating-bend fixtures with shuffled grids [11](race-trials/s-bend-seed11.json), [22](race-trials/s-bend-seed22.json) and [33](race-trials/s-bend-seed33.json) finished all 24 cars without recovery. Maximum one-metre-progress stall intervals were about 1.87, 1.50 and 2.92 seconds respectively. These also precede the latest passing-gap adjustment.

## Current unresolved change

Changed overly conservative lane-gap rejection to check current and one-second-projected clearance. One [current-code trial](race-trials/passing-gap-check.json) finished all 24, with the developed player 14th in 104.62 s and no recovery. It still fails the intended progression to a winning setup. The change needs broader verification or replacement. Existing solo results remain relevant because there were no other cars; previous field and overtaking reports must not be treated as validation of the revised traffic logic.

Next work: diagnose why the much faster car remains trapped in the full field; improve passing without inducing collisions; rerun matched field and small-field matrices; test a stopped-car obstruction; render and inspect the results interface; produce a concise balance report; export/package only after verification. Continue the unchanged visual, controller, audio, equipment, additional-track and performance goals.

# Driver personality, teammate growth and track familiarity

**Updated 29 September:** this is now specified in [doc 45](45-roadmap-and-feature-spec.md).
- **F1:** one persistent Racer record for rivals, recruits and teammates.
- **F2:** six abilities shown as a radar chart (Braking, Consistency, Line, Control, Racecraft, Strategy), with Aggression and Patience as personality tags; "risk" becomes Aggression.
- **F3:** teams from class A, you plus up to 2, one team per class, with Call to re-add any racer you've found, keeping their experience.
- **F4:** XP toward a per-ability potential cap.
- **F5:** racers found around the world, some with "prove yourself" requirements.

The principles below (only show real abilities, car and driver kept separate, no scripted results) still hold.


**23 September implemented first slice:**86–88 add stable identities, causal preferred passing clearance from risk, braking judgement and small immutable race-form variation; the preview shows only these active traits. Equipment capability remains independent.40 physical form runs across20 paired seeds show a tiny lap-time spread for the variable setting, not established finishing-rank variety. Broader abilities, teammate learning and track familiarity remain unimplemented. See [document38](archive/racing-ai-v1/38-driver-personalities-and-adventure-milestone.md) for current evidence and limitations. The user's next focus is [visual UI work](39-driving-hud-and-playful-ui.md).

**Latest moving review,23September:** the user likes the progress but classifies prototype43's current style as **risk0/10**, not the desired average racer. Aim for an average around5: recognise smaller usable gaps across the continuous road width, carry an equipment advantage into active pass attempts, and commit rather than matching slower traffic indefinitely. Risk10 should overcommit readily and may crash; low-risk characters can preserve the current cautious style. Risk is separate from car performance and driver skill, so a fast car can still have a cautious driver. Occasional contact is a permitted racing consequence, not a random crash quota or permission for clipping. Priority on resume is an assertive risk5 comparison and evidence of released pace/passing opportunities, ahead of further early-braking personality tuning. Preserve0 as the recorded comparison. Player review is positive about progress, but rejects current caution as average racecraft acceptance.

**22 September prototype preparation:** [document34](archive/racing-ai-v1/34-driver-identity-prototype.md) defines the first bounded behavioural slice, braking judgement plus consistency, with explicit neutral parity, seeded variation and forecast ownership. It is a proposal, not implemented skills or a replacement for the later full teammate/familiarity progression specification. Baseline moving review and outstanding engineering gates remain visible.

**Implementation sequencing, 21 September:** [document 32](archive/racing-ai-v1/32-racing-ai-ground-truth-and-implementation.md) now governs baseline control and racecraft. Its F1-inspired tactical choice supersedes the older immediate planner recommendation; the abilities, personality, form, familiarity and teammate requirements in this brief remain in scope. Implement them after competent baseline racing passes its measured and moving-review gates.

19 September 2026 — requested future design direction, not an implementation-ready specification. The user explicitly asked to record this while continuing current work, then give it a full design pass later.

**21 September implementation reference:** the user identified [Little Road: racing AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>) as the intended existing AI plan. It has been read in full and is the engineering starting point for the next AI milestone. This document supplies the original design intent; it is not a replacement implementation plan. Finish the current hill/combined equipment tests first, then follow that plan's phased reference-driver, physical-recovery, racecraft and ability work on the requested varied technical test circuit. Teammate levelling, familiarity persistence and endurance integration remain later phases. Recheck the external plan's source observations against current code: controlled reference braking has changed, and grade-aware body alignment is being implemented for elevated routes.

## Intended experience

Racers should be engaging individuals with recognizable strengths and weaknesses. Their underlying abilities should influence the decisions and execution visible in a race. The user's examples—racing-line ability, turning, braking and overtaking—illustrate the direction; they are **not a committed list of attributes**. Research and prototype the most useful small set before fixing the schema or formulas.

Retain the existing natural variation: the same driver can perform better or worse on different days, react to traffic, and finish in a different position. Stable identity and learned ability should coexist with variable execution. Stronger equipment must still matter, and driver ability cannot bypass vehicle grip, power, braking, fuel or collision limits. Avoid assigning a desired finish order or quietly strengthening opponents when the player upgrades.

## Requested future progression

- **Teammate development:** when racing teammates are introduced, participation should improve their racing ability over time. Include a visible levelling/progression system so the player understands that improvement and can recognize the teammate's development.
- **Track familiarity:** maintain a separate notion of experience, ability or comfort for each specific track. Familiarity should improve how a driver handles that course without being identical to their overall level.
- **Interacting influences:** distinguish car/equipment performance, persistent driver traits, learned general ability, track familiarity and temporary race form. Work out their relationship before adding overlapping bonuses.

This is a requested later racing-system design area. It does not authorize immediate implementation or commit to a full team-management simulation.

## Questions for the full specification

Define which abilities change line choice, braking judgment, corner execution, passing, defending and recovery; how those choices remain legible and fair; and how driver style differs from competence. Consider whether all opponents use the same capability model and how rival identities persist across events.

For teammate growth, specify earning rules, levels or continuous skills, learning rates and limits, feedback, catch-up for new teammates and safeguards against rewarding idling or repetitive trivial races. Decide what a level communicates without hiding the actual driver strengths. These are design questions, not approved numerical rules.

For familiarity, define track identity (including reverse layouts and variants), what is learned, how quickly, whether knowledge transfers or decays, and what an unfamiliar driver does safely. Decide whether surface, weather, vehicle class and endurance/pit strategy change familiarity or are separate skills. Avoid making newcomers unusable.

Cover recruitment/selection only to the extent needed for racing teammates; explicitly define any broader management scope. Include save persistence and migration, controller-readable progression UI, economy/reward interactions and explainable post-race feedback.

## Evidence needed before acceptance

Extend [the existing driver and balance protocol](archive/racing-ai-v1/12-driver-skill-and-balance-testing.md), rather than replacing it with scripted results. Compare matched equipment and seeds while changing one driver influence at a time. Use solo controls, two-/three-car overtakes and full 24-car fields across multiple tracks and classes. Examine distributions of lap times, finishes, braking errors, clean passes, contacts and recovery—not just averages or a single winning run.

Demonstrate that teammate improvement and track learning produce understandable benefits without eliminating race-to-race variation, erasing equipment advantages or enabling impossible driving. Verify saved progression and exploit resistance. Human playtests must establish that distinct behaviours make racing more engaging; metric separation alone cannot prove that.

## Scheduling

### Proving-ground integration — 21 September

Use the [equipment performance courses](24-equipment-performance-test-courses.md) as reusable fixtures when this skills design receives its full specification and prototype pass. Preserve the deterministic equipment baseline, then hold the car fixed and vary one approved driver influence at a time, using paired seeds and repeated runs. Keep temporary form, persistent ability and track familiarity separately recorded. Follow solo measurements with two-/three-car passing and 24-car field tests; a solo course cannot establish overtaking skill or engaging race behaviour.

The checkpoint 19 reference driver is a measurement policy, not an implementation of this future skill system. Its conservative U-turn braking currently underuses stronger brakes. Correct that baseline limitation before interpreting skill comparisons. The fixtures introduce no new attribute schema, teammate XP rules or familiarity formula; this document remains the design starting point requested by the user.

Follow the latest order in [scope and priorities](17-scope-and-priorities.md): the user now requests AI driving work after the current hill/combined tests, using the exact implementation plan linked above. Preserve reliability and visual acceptance as ongoing requirements. Give teammate growth and familiarity their dedicated integration/specification pass before adding them. No attribute schema, XP curve, level cap, familiarity formula or production estimate is fixed by this original intent note.
## Contact and racing risk — user clarification, 22 September

Occasional racing contact is acceptable; the goal is competitive, believable racing rather than universal collision avoidance. Personality should vary willingness to commit to a marginal opening, persistence, patience and margin choice within physically achievable manoeuvres. Aggression is separate from skill: an aggressive skilled driver can complete a cleaner pass than an aggressive novice. Do not add random crashes or grant grip/power/ramming advantages to manufacture drama.

Evaluate each personality over repeated comparable races using pace, completed passes, pair incident frequency, severity, time lost, recovery and ability to continue. A little rubbing can be an acceptable cost of a worthwhile move; contact count alone is neither success nor failure. Keep the neutral baseline competent and reasonably restrained, without making it refuse ordinary overtakes. Controlled clean-opportunity tests retain no-contact requirements. Set personality-specific evaluation limits before their test batch; do not excuse systematic path or braking defects as personality. See document32 for the full-field acceptance clarification.

# 73 · How we work (13 October 2026)

Richard, 13 October: "instead of me just saying 'let's do x and y town', it would be great if we can have that planning moved
to yourself with your own standards being applied ... so you can plan, execute, and judge quality yourself with agents doing
specific things."

This doc sets out who does what, the cycle every piece of work goes through, the bars it has to clear, and the rules that keep
the project lean. It replaces ad-hoc task lists. `docs/next-steps.md` remains the running status log.

## 1 · Roles

**Claude: planner, builder and first judge.**
- Chooses what comes next, from the plan of record:
  - the roadmap (doc 45);
  - the world blueprint and its build order (doc 58);
  - this doc's standards.
- Writes each piece's brief, then researches, builds, measures and fixes it.
- Judges quality against the bars in section 3 before Richard sees anything.
- Uses agents for narrow jobs (section 5) and keeps the whole picture in the main session.

**Richard: director and playtester.**
- Approves, edits or vetoes each brief before work starts. This is the only planning step that waits on him.
- Plays each finished piece. Reports go in through the snap tool (section 4), or in a sentence in chat.
- Makes the calls that are his:
  - creative direction, story and tone;
  - anything spent or published;
  - commits and play-copy releases (standing rule: commit only when asked).

## 2 · The cycle

Every district, feature or system goes round the same loop:

1. **Brief (one page, in `docs/`).**
   - What it is, and why now: where it sits in doc 58 and what it unlocks.
   - Real-world references.
   - The player's experience in three sentences.
   - Scope in and out; risks; how it will be judged (section 3).
   - Richard says yes, edits or vetoes.
2. **Research.** Real references (places, roads, buildings, sounds), and how comparable games do it. Done by agents, with the sources kept in the brief.
3. **Build behind a flag.**
   - New behaviour goes behind an off-by-default `--flag`, with an editor check (the CLAUDE.md rules).
   - New districts are designed for the district system from the start:
     - deferred building (`Districts`);
     - their own settle pass;
     - a town layer set up through `WhenBuilt`.
4. **Self-judge.** Every bar in section 3, measured, with evidence kept: captures, audit text, timing lines. Failures are fixed or written down with a reason.
5. **Playtest.** Richard plays it (a play-copy build). Snap reports land in the inbox.
6. **Fix round.** Each report is reproduced at its exact spot, fixed, and checked with a before/after capture.
7. **Landmark.** Full regression, then:
   - the flag becomes the default (Richard's call);
   - commit when asked;
   - the status line in `next-steps.md`.

A rolling plan of the next three pieces stays at the top of `next-steps.md`, so Richard can see what's coming without having to set it.

## 3 · The bars

A piece is done when it clears these, measured rather than eyeballed:

- **Area standards (doc 57).**
  - `--area-audit` PASS for the new area: floating pieces, loose bunting, lips, steep unguarded banks, road seams, cars off the ground.
  - The genre checklist: things to do, landmarks, wayfinding.
- **Street review.** Photos of every street, reviewed by the street-reviewer agent, with every confirmed defect fixed.
- **Art direction** (`docs/art-direction.md`).
  - Toy shapes and real light.
  - Colours and materials through `WorldLook` and `TrialWorld.Material`; `WorldLookChecks` stays green.
- **Performance budgets.** Measured with `tools/drive-benchmark.ps1` and the load timing:

  | Budget | Limit |
  |---|---|
  | Ready to drive (a build's second launch) | 3.5 s or less |
  | Race entry | 1.0 s or less |
  | Drive benchmark, frames over 33 ms | 2 or fewer |
  | Drive benchmark, p99 frame | 20 ms or less |
  | A background district's busiest frame | 50 ms or less |

  A new district must not move ready-to-drive by more than 0.1 s: it builds in the background.
- **Same world.** Any change that isn't meant to change the world keeps the world fingerprint (`--world-fingerprint --no-static-batching`) and the town print (`--town-print`). A change that is meant to records its new baseline in the brief.
- **Driving.**
  - Roads are drivable or guarded: the edge audit (`--edge-audit`).
  - Races finish with no new assisted resets (the keep/reject rules in CLAUDE.md).
- **Releasable.** Nothing that blocks a possible release:
  - no editor-only dependencies at runtime;
  - no licence-unknown assets;
  - the Unity plan's terms respected.

## 4 · Bug reports: the snap tool

- **Taking a snap:** in-game, F9 on the keyboard, or Select + R3 on a pad (the "view" button plus right-stick click). It saves:
  - a screenshot;
  - where the car is (position, heading, the area's name);
  - the time of day, the build and the save;
  - an optional one-line note.

  Snaps go into `snaps/<date>/` in the game's data folder (`%USERPROFILE%\AppData\LocalLow\Little Road Project\Little Road Unity Trial`). The play copy and the dev build share it, and rebuilds never touch it.
- **Triage:**
  - Claude reads the folder and logs each snap in `docs/bug-inbox.md`: what's wrong, its cause, and open / fixed / won't-fix.
  - Each one is reproduced at its exact pose (`--spot-views`), fixed, and shown fixed with a before/after capture.
- **Notes:** anything Richard just mentions in chat goes into the inbox too, so nothing lives only in conversation.

## 5 · Agents

- **Who does what:**
  - **Reviewers** read evidence and report; they never edit: street review, visual defects, docs.
  - **Researchers** gather references and sources.
  - **Explorers** map code before a change: dependencies, readers.
  - Fixes are made in the main session, which holds the whole picture.
- **Cost (4 October):** reviewers and reporters run on the cheaper models (sonnet or haiku; `street-reviewer-high` for street review). Opus only when a judgement call needs it.
- **Briefs:** every agent's brief states the question, the evidence and the output wanted, so its answer can be checked.

## 6 · Keeping it lean

- **Checks earn their place.** Every editor check guards something a player sees, a standard in section 3, or a contract between systems that would otherwise break silently.
  - A check that pins exact tuning numbers is loosened to a range, or turned into a measured test.
  - A check that only protects code nothing uses is retired with that code.
- **Flags have a lifetime.** A study or A/B flag is removed, with its dead branch, once the study is written up. Escape hatches for defaults (`--no-…`) stay while they're useful for comparisons.
- **Generated output is pruned** by `tools/cleanup.ps1` after every build:
  - per-tick telemetry only on request;
  - caches only for the two most recently used builds.
- **Docs:** one doc per piece of work, and `next-steps.md` as the log. Superseded docs say so at the top rather than being rewritten.
- **The audit (13 October onwards):** every check file, flag and tool is classified as keep, merge, loosen or retire, and Richard sees the list before anything is removed.
  - First suspects: about 3,400 lines and 34 check files from the first racing AI's studies (forecasts, drafting, dynamic rejoin). The first AI itself is kept as the benchmark (`--first-ai`).

## 7 · Order of work from 13 October

1. Commit the 12 October work (after full regression sr-speed-1).
2. The snap tool and the bug inbox.
3. The bloat audit: the list for Richard, then one tested removal batch.
4. The first self-planned district: a brief from doc 58's build order, designed for background building.

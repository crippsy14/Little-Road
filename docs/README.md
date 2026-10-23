# Little Road docs

Five living records hold every game decision, direction and design rule still in force. Read them in this order:

| Doc | What it holds |
|---|---|
| [design.md](design.md) | The game: vision and tone, classes and career, races and pay, cars, parts, economy, driving and tuning, racing AI, sound, interface, look, characters and activities, the plan of record |
| [world.md](world.md) | Glasswater: the continent, the campaign and story, the build order, the standards every place is built to, and each built place |
| [next-steps.md](next-steps.md) | Where the game stands: what's in it, what's next, what waits on Richard, known issues |
| [how-we-work.md](how-we-work.md) | Roles, the cycle, the bars, keep/reject, regression, agents, tools, git |
| [bug-inbox.md](bug-inbox.md) | Every snap and chat report, with its status |

**Data** that code and tools read: `data/race-stars.json` (planned stars; `FieldRecipeChecks`, `tools/field-table.py`,
`tools/world-plan.py`), `data/field-recipes.json` (written by the build), `data/world-plan.json` (the world blueprint's
places and tracks). Skills: `.claude/skills/track-design`, `.claude/skills/world-plan`.

**Keeping it small:** when a decision changes, edit the record where it lives. Don't add a doc that supersedes another.
A brief for a piece of work can be its own file while the work is under way; when it lands, fold its decisions into the
records and delete the brief.

## Retired docs (23 Oct 2026)

Richard, 23 Oct: the docs were "bloating the information space", and he was having to repeat ideas. The 65 docs below
(about 1.1 MB) were folded into the records above (about 100 KB) and deleted, and next-steps.md was rewritten. Their round-by-round logs, study numbers and superseded plans stay in
git: the last commit that has them all is **35a4d35** (`git show 35a4d35:docs/<file>`). Code comments still cite these
numbers (for example `docs/47`); this table says where each one's live content went.

| Doc | Title | Live content now in |
|---|---|---|
| 09 | Racing and parts progression | design §3, §5 |
| 10 | Expanded ownership and parts | design §5 |
| 13 | Car roster and collisions | design §3, §4 |
| 18 | Endurance, fuel and mass | design §7 |
| 44 | Parts tiers and progression | design §2, §5, §6 |
| 45 | Roadmap and feature spec | design §12 (racers and teams), §14; world §1 |
| 46 | Racing AI v2 design | design §8 |
| 47 | Parts, drivetrain and sound | design §5, §7, §9 |
| 48 | Tuning shop | design §7 |
| 49 | Music | design §9 |
| 50 | Title, saves and class gating | design §2, §10 |
| 51 | Claude's additions | design §12 |
| 52 | Road network | world §2 (build rules: junctions) |
| 53 | The Regale | design §12 |
| 54 | The Duke's Challenge | design §7, §12 |
| 55 | Engine scorecard | design §9; how-we-work §7 |
| 56 | Wider game round | design §5, §10, §12 |
| 57 | New-area standards | world §2; how-we-work §3 |
| 58 | World and campaign design | world §1 |
| 59 | Quarry Hills | world §3 |
| 60 | Outsider review | design §10, §12 |
| 61 | World tiles | design §13 |
| 62 | Street review, quarry works, licence | world §2, §3; how-we-work §3 |
| 63 | Blast Run and Tip Leap | design §12; world §3 |
| 64 | Farm challenges and activity HUD | design §10, §12 |
| 65 | Class B Tuner cars | design §2, §4 |
| 66 | Car builder v2 | superseded by the Car Forge (design §4) |
| 67 | Class B races and unlocks | design §2, §5 |
| 68 | Vinelands (first plan) | world §3 (Vinelands) |
| 69 | My City | world §4 |
| 70 | Race fields across the map | design §8; data/race-stars.json |
| 71 | Racing lines and fast cars | design §8 |
| 72 | World streaming | design §13; how-we-work §3 |
| 73 | How we work | how-we-work |
| 74 | Bloat audit | how-we-work §6 |
| 75 | Car Forge spec | design §4 (the full spec is in crippsy14/car-forge) |
| 76 | Prop Forge design | design §11 |
| 77 | Prop Forge brief | design §11 |
| 78 | Prop Forge stage two | design §11 |
| 79 | Bayview rebuilt | world §3 (Bayview) |
| 80 | Three towns remodelled | world §2, §3; how-we-work §3 |
| 81 | Whole map order | world §1 (build order) |
| 82 | Bramblewick | world §2 (tracks), §3 |
| 83 | Parts system | design §5 |
| 84 | Hybrid prototype | design §7 |
| 85 | Vinelands brief | world §2, §3 |
| 86–93 | Vinelands zones 1–8 | world §3 (Vinelands) |
| 94 | Racing AI pace review | design §8 |
| 95 | Car Forge models everywhere | design §4, §10 |
| art-direction | Art direction | design §11 |
| graphics-scorecard | Graphics scorecard | design §11 |
| rendering-and-streaming | Rendering and streaming | design §13 |
| ui-design | UI design | design §10 |
| visual-quality-audit | Visual quality audit | design §11 |
| research/75a, 75b, 76a, 76b | Car and toy research | design §4 (adopted conclusions) |

Older retirements: the first racing AI and its notes (commit 88131e7 and before), and 17 docs removed on 30 Sep.

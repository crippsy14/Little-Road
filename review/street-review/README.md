# Street review: where to continue (paused 4 Oct; photo review completed 5 Oct — see full-review-summary.md)

Richard paused the seven street-review agents to save usage. Each wrote its findings so far, plus a "Continue from here" section, to a file in this folder:

| File | Streets |
|---|---|
| bayview.md | Bayview road, Hillcrest mainline (the Bayview stretch) |
| hillcrest-via-crest.md | Via Crest |
| hillcrest-lower.md | Quay Road, Falls Road, Station Road |
| quarry-high-street-terrace.md | High Street, Terrace Row |
| quarry-quay-lane.md | Quay Lane, Kettle Falls Road |
| quarry-chapel-haul.md | Chapel Street, Haul Road |
| quarry-road.md | Quarry Road |

**The photos.** The source photos are in `review/quarry-hills/streets-1/`, laid out as `streets/<town>/<street>/NNN-{along,left,right}.png`. That's 408 shots, one set every 40 m. `streets.json` gives each shot's camera and look-at position.
- They were taken with `.\tools\quarry-run.ps1 -Name <new> -Flags @('--street-review') -TimeoutSeconds 2400`. The `--street-review` mode is in VillageReview.cs.
- Close-ups use `.\tools\capture-quarry.ps1 -Name <new> -Shots "name:x,y,z>lx,ly,lz;..."`, where `y` can be `g+N` for N metres above the ground.

**To resume.**
- Use cheaper agents (model sonnet or haiku) on the unreviewed photos listed in each file's "Continue from here" section.
- They report only; the main session triages and fixes. They must not rebuild the dev build.
- Never reuse a run name.

## Effort comparison (set up 5 Oct, still to run)
- Reviewer agent types, Sonnet with reports only:
  - `street-reviewer` (low effort);
  - `street-reviewer-medium`;
  - `street-reviewer-high`.
  
  They are defined in `.claude/agents/`. That folder was created mid-session, so they only load in a new session.
- Already done: one Sonnet run at the default effort on Quarry Road 005–012 (`trial-quarry-road-sonnet.md`). About two-thirds of its 15 findings were real; see the agent-usage-cost memory.
- **Run 5 Oct.** Results are in `effort-comparison.md`. Use **`street-reviewer-high`** to resume the review.
  - On the same 24 photos it cost about 97k tokens, against 82k for low and 83k for medium.
  - It found the most real defects (5 of the 7 Opus defects, plus two new ones). Its doubtful items were rated low/low.
  - Medium added nothing over low.
  - An Opus 5.5 run with the same instructions (103k tokens, 272 s) also found 12 of the 14 checklist defects, the same as Sonnet high, but at Opus prices. It caught the 010-right topiary but missed the floating car, so it's not worth it as the default.
  - Every Sonnet level missed the small topiary-in-car at 010-right, and no run caught the awning valances. Check suspected small clashes with close-ups.
  - Agents may not write their report file (the medium one refused), so save the reply yourself if the file is missing.

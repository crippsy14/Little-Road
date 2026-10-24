---
name: car-brief-check
description: How to judge a car model against its car brief (from the car-brief skill), requirement by requirement, and decide whether the car counts as created. It covers the evidence pack (tools/brief_evidence.py in Car Forge), the measured bands, a blind read by a cheap agent who never sees the brief, the per-requirement verdicts with evidence, how each FAIL is classed (MODEL, ENGINE GAP or BRIEF CONFLICT), the independent second check before a "created" verdict, and the round report. Use it after building or changing any car that has a brief, and to audit existing cars against new briefs.
---

# Checking a car against its brief

A car is **created** only when **every** requirement in its brief passes. That is Richard's bar (24 Oct). The check is
how the bar is held honestly: evidence for every verdict, a reader who hasn't seen the brief, and a second check before
anything is called done. Write briefs with the **car-brief** skill.

## 1. The rules of judging

1. **All or nothing.** One FAIL means not created. A "nearly", a "partly" or an "in spirit" is a FAIL.
2. **Unseen is absent.** If no picture in the evidence pack shows an element, it fails. Add a close-up to see it;
   never assume it.
3. **Built, not painted.** Where the brief describes a form (a lamp's reflector, a grille's bars, a shut line, a bulge),
   a flat texture or decal fails. Printed graphics pass only where the brief asks for print (tampo liveries).
4. **Judge against the brief, not against the Forge.** "Good for what the engine can do" is not a verdict. If the
   Forge can't make it, the verdict is FAIL with the cause ENGINE GAP.
5. **Never move the goalposts in a check round.** Don't edit the brief during or after a check to make the model pass.
   A requirement can change only in a separate step, with the reason written in the brief's change log: it was
   impossible to check, it contradicted another requirement, or Richard changed his mind. Never "the model can't do
   it".
6. **Evidence for every verdict.** Each one names the picture (file and view) it was judged from. A measured
   requirement quotes the number.
7. **Look before you write.** Open every picture in the pack before the first verdict. Judge from what is in the
   pictures, not from what the design file says should be there.

## 2. The round, step by step

Each check is a numbered **round**. Rounds are never overwritten (`docs/how-we-work.md`).

### Step 1: the evidence pack (one call, about 15 s)

From `car-forge` (Blender's Python, `PYTHONPATH=vendor;.`):

```
python tools/brief_evidence.py designs/roster/<id>.yaml --out out/briefs/<id>/check-<N> --brief briefs/<class>/<id>.md
```

It writes:
- `hero.png`, `turntable.png` (clay, paint, and close-ups of the face, tail and wheel);
- `ortho.png`, `silhouette.png`, `clay.png`, `zebra.png`, `base.png`, `chase.png`;
- `measures.txt`: every band in the brief's measures block, PASS or FAIL.

Each "Seen in" name in the brief maps onto a picture:

| Seen in | Picture |
|---|---|
| side, front, rear, top | `ortho.png` |
| 34front, 34rear | `hero.png` |
| face, tail, wheel | `turntable.png` (its close-ups) |
| clay | `clay.png` |
| zebra | `zebra.png` |
| base | `base.png` |
| chase | `chase.png` |
| M | `measures.txt` |
| roster | `python tools/game_sheet.py designs/roster/<id>.yaml <neighbours…> --out out/briefs/<id>/check-<N>/roster.png`, and `.\forge.ps1 roster` for look-alike scores |
| blind | the blind read (step 2) |

`base.png` is drawn in curvature colours: in studio light the underside is black, and flat-lit relief (an embossed
name) vanishes. If a requirement needs a closer look than the pack gives, take one with `.\forge.ps1 shot <design> --at x,y,z --r 0.15
--view <v> --out out/briefs/<id>/check-<N>/extra-<name>.png`, and cite it.

A car that isn't from the Forge (a legacy model, a hand-made mesh) gets the same set of views by whatever renderer
made it. The verdicts follow the same rules.

### Step 2: the blind read (a cheap agent, before you read your own verdicts)

Spawn one agent with `model: "sonnet"` for a hero car (a starter, a flagship, a car the player owns), else `"haiku"`;
see the agent-usage-cost memory. (The Puddle, 24 Oct: haiku read the hero sheet's small faces as "dopey" and "dull",
and sonnet read the same car as "cheerful" and "smiling". Keep one reader model across a car's rounds once it is
chosen.) Give it only
`hero.png`, `silhouette.png` and `chase.png`: no brief, no name, no concept line. Ask it to answer in a few words each:

1. What kind of vehicle is this, and what era does it feel like?
2. What is its personality, in one word? What expression does its face have, in one word?
3. Name the three things you notice first.
4. What is the one thing about it that no other toy car would have?
5. From the silhouette alone, what would you guess it is?
6. Does it look like a toy or a scale model? Why?

Save its answers word for word in the round's report. The brief's `blind` requirements (usually in CH and SP) pass only
when the reader's own words match the brief's type, expression and signature. A synonym counts; a guess you have to
argue for does not. A blind reader who names a signature the brief didn't intend is useful too: note it.

### Step 3: every requirement, one by one

Go through the brief in order and give every ID one row:

```
| ID | Verdict | Evidence | What is seen | Cause | Fix |
|---|---|---|---|---|---|
| FA3 | FAIL | turntable.png face | grille is a flat printed grid, no depth or bars | ENGINE GAP | grille infill 'bars' with depth: forge/components/grille |
| SP2 | PASS | measures.txt | H_over_L 0.52 in [0.48, 0.54] | | |
```

For every FAIL, give the cause:
- **MODEL:** the design file can fix it. Name the field (`regions.body.side_bulge`, `components.lamps.front.shape`).
- **ENGINE GAP:** Car Forge can't build it yet. Name the missing capability and where in the engine it would go. Each
  ENGINE GAP becomes a Forge work item (step 6). The brief stays as written.
- **BRIEF CONFLICT:** two requirements contradict each other, or one can't be checked. Fix the brief in a separate,
  logged step, never in this round. A conflict about the car's design intent (its signature, its face) goes to
  Richard. Don't settle it by loosening a band yourself. An internal contradiction, such as a part one requirement asks
  to be plated and another's list forbids, may be fixed with the reason logged.

### Step 4: the second check (only when every row passes)

Before any **created** verdict, spawn one more agent (`model: "sonnet"`). Give it the brief and the whole evidence
pack, but not your verdicts. It writes its own verdict for every requirement. Then compare:
- Where it says FAIL and you said PASS, look again at its evidence. If it's right, the verdict becomes FAIL.
- Where you disagree after looking again, the requirement fails until a new render settles it.
- Where it says PASS and you said FAIL, keep your FAIL unless its evidence shows you missed something.
This guards against grading your own work.

### Step 5: the report

Append the round to `car-forge/briefs/<class>/<id>.checks.md`. Never rewrite an earlier round. Each round records:
- the date, the round number, and the design file with the car-forge commit (or "uncommitted");
- the counts: `PASS n / FAIL m / of total`, with the FAILs split by cause;
- **the verdict:** `CREATED` or `NOT CREATED`;
- the measures table and the blind read, word for word;
- the requirement table (step 3);
- **the fix list:** MODEL fixes in the order they'll be made, then ENGINE GAPs grouped by engine area;
- for a CREATED round, the second check's disagreements and how each was settled.

When a round's verdict is CREATED, set the brief's status line to `created (check N, <date>)`.

### Step 6: act on it

- **MODEL fails:** fix them in the design file, then run the next round. Fix the biggest level first (silhouette, then
  volumes, then parts), as the brief was written.
- **ENGINE GAPs:** add them to the Forge's work log (`car-forge/docs/log.md`, a "brief gaps" entry naming the brief and
  IDs). Then build the capability in the engine, with its gate and tests as usual. A gap shared by several briefs is
  the most valuable work in the Forge. Ask Richard only when a gap is a large engine job (days, not hours).
- **Show Richard each round's hero picture and verdict** with SendUserFile (the model-iteration-loop memory: render
  photos each round, no game publish). Ask for his view of the car at milestones: the first round, and when it's
  CREATED.
- **Clean up:** delete the round's pictures once the report is written. Keep only the pictures the report or a doc
  links to (`docs/how-we-work.md`: photos are removed once checked).

## 3. Auditing an existing car

To find out how far an existing car is from where it should be, write its brief first with the car-brief skill. Write
it for the car it *should* be, not a description of the model as it is. Then run round 1 against the current model.
The FAIL list, split by cause, is the honest gap: how much is design-file work, and how much is engine work.

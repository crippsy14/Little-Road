# Study 128: new baselines after the grid and collision-box change (29 September)

Richard asked for fresh baselines after the starting-grid change. They also include the per-body collision boxes (same build).

Earlier baselines (study 127 and before, `review/class-sort/base-*`) are **superseded**: they're kept for the record, but new studies compare against these.

**What changed in the build:**
- **Grid:** the pole car's nose is on the start line. Rows of three are 11 m apart with a 1.8 m stagger; before, the front row was 8 m back and rows were 5.5 m apart.
- **Collision boxes:** each body's box now fits its own length and its shoulder-and-tyre width, mirrors excluded. Before, one 3.19 × 2.32 m box served every car.

**Pace baseline** (`run-pace-study.ps1 -Study 128 -Label base -SkipBaseline`, 24 cars, Cedar and Bayview, seeds 1709–1711):

| Run | Finished | Collective (s) | Contacts (pairs) | Assisted | Physical recoveries (s) | Study 125 collective |
|---|---|---|---|---|---|---|
| bayview s1709 | 24/24 | 919.0 | 12 (6) | 0 | 7 (88.1) | 803.4 |
| bayview s1710 | 24/24 | 793.8 | 12 (6) | 0 | 2 (14.1) | 796.7 |
| bayview s1711 | 24/24 | 797.4 | 2 (1) | 0 | 2 (16.6) | 794.1 |
| cedar s1709 | 24/24 | 2115.9 | 16 (8) | 0 | 0 | 2116.6 |
| cedar s1710 | 24/24 | 2110.4 | 12 (6) | 0 | 0 | 2124.4 |
| cedar s1711 | 24/24 | 2118.7 | 12 (6) | 0 | 0 | 2114.0 |

**What the numbers show:**
- **Overall:** everything finishes, with no assisted resets. Collective times match study 125 within ±15 s, except Bayview 1709.
- **Contacts are up** (study 125: 0–6), mostly low-speed nudges as the pack forms.
- **Bayview start hits:** there are two hard rear-ends just after the start, 12.2 m/s at 2.7 s on seed 1709 and 12.6 m/s at 4.5 s on seed 1710. With 7 m clear between rows, a club-kit car behind a stock car reaches speed before closing on it, and the AI's launch doesn't yet brake for that.
- **Bayview 1709's extra time** comes from 5 cars needing physical recovery (7–29 s each) after early contacts.
- **Candidate follow-up** (an AI study with a flag): make the launch traffic-aware, or trial a shorter row pitch (8 m) against this baseline.

**Class-sort baseline** (`run-class-sort.ps1 -Name grid29`, 15 cars, 2 laps). Before (`base-review.md`): Cedar passed; Hillcrest failed on every seed with 6 final inversions.

| Track | Verdict | Final inversions | Contacts | Recoveries | Assisted |
|---|---|---|---|---|---|
| Cedar | PASS (3/3) | 0 | low | 0 | 0 |
| Hillcrest | PASS on 1709 and 1710; FAIL on 1711 | 0 / 0 / 1 | 3 / 9 / 2 | 2 / 1 / 3 | 0 |

Hillcrest, which used to leave two street cars stuck behind stock cars all race, now sorts on 2 of 3 seeds. The right-sized cars leave room to pass.

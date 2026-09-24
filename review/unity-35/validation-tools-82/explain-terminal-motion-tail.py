"""Supplement, never replace, a failed raw parity check for post-finish rows."""
import json
import sys
from pathlib import Path

base, candidate = map(Path, sys.argv[1:3])
read = lambda p: json.loads(p.read_text(encoding="utf-8-sig"))
a, b = read(base / "race.json"), read(candidate / "race.json")
raw = read(candidate / "motion-parity.json")
finished = all(r["finished"] == r["carCount"] and all(c["time"] > 0 for c in r["cars"]) for r in [a, b])
end_a, end_b = (max(c["time"] for c in r["cars"]) for r in [a, b])
shared = min(len(a["motion"]), len(b["motion"]))
tails = [a["motion"][shared:], b["motion"][shared:]]
checks = dict(completeFields=finished, sameFinalFinish=end_a == end_b,
              identicalSharedMotion=a["motion"][:shared] == b["motion"][:shared],
              excessRowsOnlyAfterFinalFinish=all(x["time"] > end_a for tail in tails for x in tail),
              identicalActiveMotion=[x for x in a["motion"] if x["time"] <= end_a] == [x for x in b["motion"] if x["time"] <= end_b],
              allOtherRawChecksMatch=all(v for k, v in raw["checks"].items() if k != "motion"))
out = dict(scope="Raw parity result retained. This supplement accepts only identical common motion, identical active-race motion/events/results and an excess tail strictly after every entrant finishes.",
           rawPassed=raw["passed"], finalFinish=end_a, motionRows=[len(a["motion"]), len(b["motion"])],
           excessRows=[len(t) for t in tails], checks=checks, explainedTerminalTail=all(checks.values()))
(candidate / "terminal-motion-tail.json").write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
print(json.dumps(out, indent=2))
raise SystemExit(0 if out["explainedTerminalTail"] else 1)

"""Audit the cheap curve ranking against fixed-curve planar executions."""
import collections
import json
import statistics
import sys
from pathlib import Path

source = Path(sys.argv[1])
report = json.loads(source.read_text(encoding="utf-8-sig"))
groups = collections.defaultdict(list)
for row in report["rows"]:
    groups[f"{row['loadout']}-{row['entry']}"].append(row)
cases = {}
for name, rows in groups.items():
    selected = next(r for r in rows if r["rank"] == 0)
    eligible = [r for r in rows if r["finished"] and r["contained"]]
    best = min(eligible, key=lambda r: r["executedSeconds"], default=None)
    regret = selected["executedSeconds"] - best["executedSeconds"] if best and selected["finished"] else None
    comparable = [r for r in rows if r["estimatedSeconds"] >= 0 and r["finished"]]
    cases[name] = dict(
        horizon=selected["gate"] - selected["start"],
        selectedBlend=selected["blend"],
        selectedContained=selected["contained"],
        fastestContainedRank=best["rank"] if best else None,
        selectedRegretSeconds=regret,
        medianEstimateErrorSeconds=statistics.median(r["estimatedSeconds"] - r["executedSeconds"] for r in comparable) if comparable else None,
        geometricRejects=sum(r["estimatedSeconds"] < 0 for r in rows),
        unsafeModelExecutions=sum(not r["contained"] for r in rows),
        passed=selected["contained"] and regret is not None and regret <= .25,
    )
out = dict(scope=report["scope"], cases=cases, passed=len(cases) == 8 and all(c["passed"] for c in cases.values()))
source.with_name(source.stem + "-analysis.json").write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
print(json.dumps(out, indent=2))
raise SystemExit(0 if out["passed"] else 1)

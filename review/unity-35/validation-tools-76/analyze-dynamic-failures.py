"""Summarize captured rejection evidence without treating it as executed motion."""
import collections
import json
import sys
from pathlib import Path

root = Path(sys.argv[1])
track = json.loads((root / "race.track.json").read_text(encoding="utf-8-sig"))
audits = track["failedPlanAudits"]
entries = [entry for audit in audits for entry in audit["entries"]]
candidates = [candidate for entry in entries for candidate in entry["candidates"]]
opening = [entry for audit in track["openingPlanAudits"] for entry in audit["entries"] if entry["captureReason"] == "opening-unresolved"]
first = entries[0] if entries else None
out = dict(
    scope="Captured failed selections and nominal forecast rejections. Timestamp includes countdown; these are not actual road departures. Unchecked lower-ranked alternatives are not proven infeasible.",
    failedSelections=len(entries), dropped=sum(a["dropped"] for a in audits),
    checkedCandidateReasons=dict(collections.Counter(c["reason"] for c in candidates)),
    firstFailure=None if first is None else dict(timestamp=first["timestamp"], self=first["self"], steering=first["steering"], candidates=first["candidates"]),
    openingReplayStates=[dict(timestamp=e["timestamp"], previousPath=e["previousPath"], self=e["self"], steering=e["steering"]) for e in opening],
)
(root / "failure-analysis.json").write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: out[k] for k in ["failedSelections", "dropped", "checkedCandidateReasons"]}, indent=2))

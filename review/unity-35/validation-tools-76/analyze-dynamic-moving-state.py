"""Distinguish a poor shortlist from an exhausted curve family in captured states."""
import json
import sys
from pathlib import Path

source = Path(sys.argv[1])
report = json.loads(source.read_text(encoding="utf-8-sig"))
cases = []
for row in report["rows"]:
    viable = [c for c in row["candidates"] if c["geometryValid"] and c["shortForecastContained"]]
    gate_viable = [c for c in viable if c["gateContained"]]
    fallback = row.get("fallbackRank", -1)
    chosen = next((c for c in viable if c["rank"] in [0, fallback]), None) if fallback >= 0 else None
    cases.append(dict(timestamp=row["timestamp"], route=row["route"], speed=row["speed"],
                      physicallyViableRanks=[c["rank"] for c in viable],
                      wholeGateViableRanks=[c["rank"] for c in gate_viable],
                      originalShortlistHasViable=any(c["rank"] < 2 for c in viable),
                      fallbackRank=fallback, fallbackHasViable=chosen is not None,
                      fallbackGateContained=bool(chosen and chosen["gateContained"])))
exact = report["recordedCandidates"] > 0 and report["recordedCandidates"] == report["exactMatches"]
out = dict(scope=report["scope"], originalForecastsMatch=exact, cases=cases,
           recoverableObservations=sum(bool(c["physicallyViableRanks"]) for c in cases),
           noViableCurveObservations=sum(not c["physicallyViableRanks"] for c in cases))
if all("fallbackRank" in row for row in report["rows"]):
    out["fallbackRecoversEveryAvailableCase"] = exact and all(c["fallbackHasViable"] and c["fallbackGateContained"] for c in cases if c["physicallyViableRanks"])
source.with_name(source.stem + "-analysis.json").write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
print(json.dumps(out, indent=2))

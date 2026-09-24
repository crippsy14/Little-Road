"""Describe observed braking; this cannot prove that a brake request was unnecessary."""
import argparse
import collections
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('folder', type=Path)
args = parser.parse_args()
data = json.loads((args.folder / 'race.track.json').read_text(encoding='utf-8-sig'))
contexts = {(s['id'], s['time']): s for s in data['contextSamples']}
counts = collections.Counter()
causes = collections.Counter()
regions = collections.Counter()
for s in data['samples']:
    if s['inputOwner'] != 'AI' or s['speed'] < 2:
        continue
    counts['movingSamples'] += 1
    pedal = s['pedal']
    counts['braking' if pedal < -.01 else 'coasting' if pedal <= .01 else 'powered'] += 1
    if pedal >= -.01:
        continue
    counts['lightBrake' if pedal >= -.15 else 'mediumBrake' if pedal >= -.5 else 'heavyBrake'] += 1
    c = contexts.get((s['id'], s['time']), {})
    cause = 'safety active' if c.get('safetyActive') else c.get('reason', 'missing context')
    causes[cause] += 1
    regions[str(s['region'])] += 1
    if pedal >= -.15 and s['speed'] - s['target'] < 1 and not c.get('safetyActive'):
        counts['lightBrakeWithin1mpsOfTargetWithoutSafety'] += 1

out = dict(
    scope='10 Hz moving AI observations. Reason labels are diagnostics, not a causal attribution of the final pedal. No counterfactual coast trajectory is measured here.',
    counts=dict(counts),
    movingDutyPercent={k: 100 * counts[k] / max(1, counts['movingSamples']) for k in ('braking', 'coasting', 'powered', 'lightBrake', 'mediumBrake', 'heavyBrake')},
    brakingContextReasons=dict(causes),
    brakingRegions=dict(regions),
    nextTest='Evaluate zero-pedal rollout against the same upcoming speed/road/traffic constraints before replacing mild braking. Preserve emergency and required corner braking. Compare time, exits, contacts, road bounds and brake/coast duty.'
)
(args.folder / 'braking-analysis.json').write_text(json.dumps(out, indent=2) + '\n', encoding='utf-8')
print(json.dumps(out, indent=2))

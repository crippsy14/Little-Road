"""Compare the frozen prototype40 field pair, retaining failed experiments."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'review/unity-35'
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
results = {}
for variant in ('control', 'timed'):
    folder = root / ('field-40-' + variant)
    race = read(folder / 'race.json')
    track = read(folder / 'race.track.json')
    finished = {c['id']: c['time'] for c in race['cars'] if c['finished']}
    impacts = race['impacts']
    # Physics exports each participant's record. Preserve raw counts and also
    # identify pair events; do not mislabel two sides of one contact as two crashes.
    pairs = {(i['time'], min(i['id'], i['other']), max(i['id'], i['other'])) for i in impacts}
    results[variant] = dict(
        finished=len(finished), expected=race['carCount'],
        totalCarSeconds=sum(finished.values()) if len(finished) == race['carCount'] else None,
        lastCarSeconds=max(finished.values()) if finished else None,
        contactRecords=len(impacts), pairContactEvents=len(pairs),
        maximumContactRelativeSpeed=max((i['relativeSpeed'] for i in impacts), default=0),
        unresolvedActiveSamples=sum(s['roadUnresolved'] for s in track['contextSamples']
                                    if s['id'] in finished and 0 <= s['time'] <= finished[s['id']]),
        outsideBodySamples=sum(c['outsideBodySamples'] for c in track['results']),
        physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
        assistedRecoveries=sum(c['recoveries'] for c in race['cars']),
        missedGates=sum(c['missedGates'] for c in race['cars']))
a, b = results['control'], results['timed']
out = dict(scope='Frozen one-lap mixed 24-car Cedar field; not rendering or general AI acceptance.',
           decision='Reject timed rear-corridor prototype40: slower collective pace, more and harder contacts, more unresolved paths. Keep the flag disabled.',
           results=results,
           totalCarSecondsDelta=b['totalCarSeconds']-a['totalCarSeconds'],
           collectiveSlowerPercent=100*(b['totalCarSeconds']/a['totalCarSeconds']-1),
           lastCarSecondsDelta=b['lastCarSeconds']-a['lastCarSeconds'])
(root/'timed-corridor-40-comparison.json').write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps(out, indent=2))

"""Summarise the retained-trajectory experiment without hiding failed gates."""
import json
import sys
from collections import Counter
from pathlib import Path

root = Path(sys.argv[1])
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
arms = {}
for name in ('field-control', 'field-retained'):
    folder = root / name
    race = read(folder / 'race.json')
    track = read(folder / 'race.track.json')
    car = race['cars'][0]
    audits = track['continuationPlanAudits']
    entries = [e for bucket in audits for e in bucket['entries']]
    retained = [e for e in entries if e['captureReason'] == 'retained-trajectory']
    samples = {(s['id'], s['time']): s for s in track['samples']}
    braking = Counter()
    eligible = 0
    for p in track['pedalSamples']:
        s = samples[(p['id'], p['time'])]
        if p['id'] != 0 or s['time'] >= car['time'] or s['speed'] < 2:
            continue
        eligible += 1
        if p['applied'] >= -.01:
            continue
        source = 'follower'
        for before, after, stage in (('follower', 'leader', 'moving leader'),
                                     ('leader', 'scenery', 'scenery'),
                                     ('scenery', 'applied', 'immediate safety')):
            if p[after] < p[before] - 1e-6:
                source = stage
        braking[source] += 1
    frames = read(folder / 'race.driver-frames.json')['frames'][1:]
    outside = [s for s in track['samples'] if s['maxBodyOffset'] > race['trackWidth'] / 2]
    arms[name] = dict(
        finished=race['finished'], totalCarSeconds=sum(c['time'] for c in race['cars']),
        previewTime=car['time'], previewContactRecords=car['contacts'],
        impactRecords=len(race['impacts']), peakImpactSpeed=max(e['relativeSpeed'] for e in race['impacts']),
        physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
        outsideSamples=len(outside), maximumBodyOffset=max(s['maxBodyOffset'] for s in track['samples']),
        outsideEvents=[dict(id=s['id'], time=s['time'], bodyOffset=s['maxBodyOffset']) for s in outside],
        trajectory={key: sum(c[key] for c in track['roadCosts']) for key in
                    ('trajectoryAttempts', 'trajectoryAccepted', 'trajectoryRejected')},
        auditCaptured=len(entries), auditDropped=sum(b['dropped'] for b in audits),
        retainedCaptured=len(retained), retainedWithPositiveProgress=sum(e['selectedPath']['consumed'] > 0 for e in retained),
        previewEligiblePedalSamples=eligible, previewBrakingByFinalStage=dict(braking),
        meanAllAiMsPerSnapshot=sum(f['decisionMs'] + f['snapshotMs'] for f in frames) / sum(f['snapshotCalls'] for f in frames))
out = dict(scope='Single deterministic Cedar field. Audit-enabled candidate timing is not a matched performance benchmark. Contact records may include both participants. Capped audit entries are not a complete population.',
           arms=arms, collectiveSecondsSaved=arms['field-control']['totalCarSeconds']-arms['field-retained']['totalCarSeconds'],
           previewSecondsLost=arms['field-retained']['previewTime']-arms['field-control']['previewTime'],
           regressions={k: read(root / 'regressions/analysis.json')[k] for k in ('passed', 'total')},
           disabledParity=read(root / 'field-control/motion-parity.json')['passed'],
           soloParity=read(root / 'standing-solo/motion-parity.json')['passed'],
           decision='Do not promote: preview pace regresses, body containment fails and impact severity increases despite controlled checks passing.')
(root / 'comparison.json').write_text(json.dumps(out, indent=2) + '\n', encoding='utf-8')
print(json.dumps(out, indent=2))

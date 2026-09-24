"""Preserve compact race outcomes and inventory intentionally local review evidence.

Run from any directory. No simulation is started and no original evidence is changed.
Requires local raw evidence to regenerate the full historical index.
"""
import csv
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / 'review'


def write_csv(path, columns, rows):
    with path.open('w', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        writer.writerows(rows)


def main():
    rows = []
    errors = []
    for path in sorted(REVIEW.rglob('race.json')):
        try:
            data = json.loads(path.read_text(encoding='utf-8-sig'))
        except (ValueError, OSError) as error:
            errors.append({'file': path.relative_to(ROOT).as_posix(), 'error': str(error)})
            continue
        cars = data.get('cars', [])
        if not cars:
            continue
        expected = data.get('carCount', len(cars))
        complete = len(cars) == expected and all(c.get('finished') and c.get('time', 0) > 0 for c in cars)
        player = next((c for c in cars if c.get('id') == 0), {})
        rows.append(dict(
            file=path.relative_to(ROOT).as_posix(), track=data.get('trackId', ''),
            trackVersion=data.get('trackVersion', ''), policy=data.get('driverPolicy', ''),
            seed=data.get('seed', ''), equipment=data.get('equipment', ''),
            grid=data.get('gridMode', ''), autonomous=data.get('autonomous', ''),
            laps=data.get('requiredLaps', ''), cars=expected,
            finished=sum(bool(c.get('finished')) for c in cars),
            collectiveSeconds=sum(c['time'] for c in cars) if complete else '',
            car0Seconds=player.get('time', '') if player.get('finished') else '',
            car0Place=player.get('place', '') if player.get('finished') else '',
            contactParticipantRecords=sum(c.get('contacts', 0) for c in cars),
            assistedResets=sum(c.get('recoveries', 0) for c in cars),
            physicalRecoveries=sum(c.get('physicalRecoveries', 0) for c in cars),
            physicalRecoverySeconds=sum(c.get('physicalRecoverySeconds', 0) for c in cars),
            acceptance='Inventory only; consult matching review and launch configuration'))
    if rows:
        write_csv(REVIEW / 'test-results-index.csv', list(rows[0]), rows)
    (REVIEW / 'test-results-index-notes.json').write_text(json.dumps(dict(
        scope='All readable local review/**/race.json with car outcomes. Mixed versions, fixtures, grids and manual/automatic control; not a comparable leaderboard. Blank collective time means incomplete field. Raw timing does not imply acceptance.',
        runs=len(rows), unreadable=errors), indent=2) + '\n', encoding='utf-8')
    ignored = subprocess.check_output(['git', 'ls-files', '--others', '--ignored', '--exclude-standard', '-z', '--', 'review'], cwd=ROOT).decode('utf-8').split('\0')
    inventory = [dict(file=p, bytes=(ROOT / p).stat().st_size, storage='Local only; excluded generated evidence, not deleted') for p in ignored if p and (ROOT / p).is_file()]
    write_csv(REVIEW / 'local-evidence-inventory.csv', ['file', 'bytes', 'storage'], inventory)
    print(json.dumps(dict(indexedRuns=len(rows), unreadable=len(errors), localFiles=len(inventory), localBytes=sum(r['bytes'] for r in inventory))))


if __name__ == '__main__':
    main()

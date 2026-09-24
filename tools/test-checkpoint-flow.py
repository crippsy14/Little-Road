import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec=importlib.util.spec_from_file_location('checkpoint_flow',Path(__file__).with_name('analyze-checkpoint-flow.py'))
flow=importlib.util.module_from_spec(spec);spec.loader.exec_module(flow)

class CheckpointFlowTests(unittest.TestCase):
    def test_early_completion_is_met_but_missing_passages_remain_pending(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            base=dict(trackId='cedar',trackVersion='v1',trackLength=1200,carCount=1,
                      checkpointPassages=[dict(id=0,time=2+k*8,requiredProgress=k*100) for k in range(13)])
            for setup in ['street','club']:
                folder=root/f'solo-{setup}';folder.mkdir()
                (folder/'race.json').write_text(json.dumps(base));(folder/'race.track.json').write_text('{}')
            folder=root/'field';folder.mkdir()
            race=dict(base,carCount=24,simSeconds=100,impacts=[],checkpointPassages=[dict(id=i,time=2+k*8,requiredProgress=k*100) for i in range(24) for k in range(13)])
            (folder/'race.json').write_text(json.dumps(race));(folder/'race.track.json').write_text('{"samples":[]}')
            self.assertTrue(flow.analyze(folder)['allCarDeadlinesMet'])
            race['checkpointPassages']=[e for e in race['checkpointPassages'] if not(e['id']==23 and e['requiredProgress']==1200)]
            (folder/'race.json').write_text(json.dumps(race))
            self.assertEqual(flow.analyze(folder)['checkpoints'][-1]['status'],'pending')

    def test_exact_events_ignore_repeated_crossing_and_keep_finish_separate(self):
        race=dict(carCount=1,trackLength=1200,checkpointPassages=[
            dict(id=0,time=2,requiredProgress=0),dict(id=0,time=7,requiredProgress=0),
            dict(id=0,time=80,requiredProgress=1200),dict(id=0,time=160,requiredProgress=2400)])
        passages,_=flow.passages(race,{})
        self.assertEqual(passages,{0:{0:2,12:80}})

    def test_geometric_progress_alone_is_not_a_valid_checkpoint(self):
        race=dict(carCount=1,trackLength=1200,requiredLaps=1,cars=[dict(id=0,finished=False)])
        track=dict(samples=[dict(id=0,time=1,gate=0,progress=50),dict(id=0,time=2,gate=1,progress=51),
                            dict(id=0,time=3,gate=1,progress=400)])
        passages,_=flow.passages(race,track)
        self.assertEqual(passages,{0:{0:2}})

    def test_isolated_delay_and_pack_delay_are_distinct(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            def save(folder,race,track):
                folder.mkdir();(folder/'race.json').write_text(json.dumps(race));(folder/'race.track.json').write_text(json.dumps(track))
            base=dict(trackId='cedar',trackVersion='v1',trackLength=1200,carCount=1,checkpointPassages=[dict(id=0,time=2+k*8,requiredProgress=k*100) for k in range(13)])
            for setup in ['street','club']:save(root/f'solo-{setup}',base,{})
            race=dict(base,carCount=24,simSeconds=140,impacts=[dict(id=23,time=5,relativeSpeed=4)],checkpointPassages=[])
            for i in range(24):
                for k in range(13):race['checkpointPassages'].append(dict(id=i,time=2+k*8+(20 if i==23 else 0),requiredProgress=k*100))
            save(root/'field',race,dict(samples=[]))
            result=flow.analyze(root/'field');start=result['checkpoints'][0]
            self.assertEqual(start['passedByDeadline'],23);self.assertEqual(start['passedTotal'],24)
            self.assertEqual(start['status'],'isolated-delay-review');self.assertFalse(result['allCarDeadlinesMet'])
            self.assertEqual(start['delayedCarEvidence'][0]['contactEventsInSector'],1)
            race['checkpointPassages']=[e for e in race['checkpointPassages'] if e['id']<21]
            (root/'field/race.json').write_text(json.dumps(race))
            self.assertEqual(flow.analyze(root/'field')['checkpoints'][0]['status'],'multiple-car-delay')

if __name__=='__main__':unittest.main()

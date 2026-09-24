"""Arithmetic witnesses for the balance metrics, including clipped exposure and missing data."""
import importlib.util,unittest
from pathlib import Path
def module(name,file):
    s=importlib.util.spec_from_file_location(name,Path(__file__).with_name(file));m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
t=module('throttle','analyze-straight-throttle.py');o=t.opening
class Metrics(unittest.TestCase):
    def test_clipped_pedal_exposure(self):
        rows=[dict(time=x,pedal=p) for x,p in [(0,0),(1,1),(3,.5),(5,-1),(6,0)]]
        a=t.percentages(t.exposure(rows,.5,5.5))
        self.assertEqual(a['seconds'],5);self.assertEqual(a['averageThrottlePercent'],60)
        self.assertEqual(a['fullThrottlePercent'],40);self.assertEqual(a['brakePercent'],10)
        self.assertEqual(a['partialThrottlePercent'],40);self.assertEqual(a['coastPercent'],10)
    def test_missing_tail_rejected(self):
        with self.assertRaises(AssertionError):t.exposure([dict(time=0,pedal=1),dict(time=1,pedal=1)],0,2)
    def test_crossing_uses_unwrapped_progress(self):
        self.assertEqual(o.crossing([dict(time=2,progress=-10),dict(time=4,progress=10)],0),3)
    def test_spread_and_tied_ranks(self):
        rows={i:[dict(time=0,progress=p,speed=10,roadOffset=i),dict(time=2,progress=p+20,speed=10,roadOffset=i)] for i,p in enumerate([-10,10,10])}
        a=o.frame_at(rows,1);self.assertEqual(a['spreadMetres'],20)
        self.assertEqual([x['rankRange'] for x in a['cars']],[[1,2],[1,2],[3,3]])
if __name__=='__main__':unittest.main()

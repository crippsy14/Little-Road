from pathlib import Path
r=Path(__file__).resolve().parents[1]
def edit(name,old,new):
 p=r/name;s=p.read_text(encoding='utf-8');assert old in s,(name,old);p.write_bytes(s.replace(old,new).encode('utf-8'))
s='unity/Assets/Scripts/'
edit(s+'RaceTrial.cs','new[]{"solo","standing-solo","pace-solo","rear-control"','new[]{"draft-straight","draft-pullout","solo","standing-solo","pace-solo","rear-control"')
edit(s+'TrafficFixture.cs','new[]{"solo","standing-solo","pace-solo","follow"','new[]{"draft-straight","draft-pullout","solo","standing-solo","pace-solo","follow"')
edit(s+'TrafficFixture.cs','if(scenario=="no-room") {','if(scenario.StartsWith("draft-")) {\n                foreach(var car in race.cars)car.PlaceTrafficFixture(start+car.driverId*gap,0);\n                evidence.scope="Declared two-car straight acceleration experiment. Identical full-throttle commands for three seconds; pullout applies a steering input from 1 to 2 seconds. All speed/position changes use ordinary Rigidbody physics. Then ordinary AI resumes. Compare the initial three seconds, not whole-lap time.";\n            }\n            if(scenario=="no-room") {')
edit(s+'TrafficFixture.cs','if(witness&&!witnessReleased) {','if(evidence.scenario.StartsWith("draft-"))foreach(var car in race.cars)car.commandSource=race.elapsed<3?DraftCommand:null;\n            if(witness&&!witnessReleased) {')
edit(s+'TrafficFixture.cs','static Vector2 BrakeLeader','Vector2 DraftCommand(RoadCar car)=>new Vector2(1,evidence.scenario=="draft-pullout"&&car.driverId==0&&race.elapsed>=1&&race.elapsed<2?.35f:0);\n        static Vector2 BrakeLeader')
# Capture the tow every physics tick only in its dedicated bounded experiment.
edit(s+'RaceTrial.cs','if(elapsed>=nextSample) {nextSample+=1;','if(elapsed>=nextSample) {nextSample+=Arg("--traffic-case","").StartsWith("draft-")?1f/120:1;')
# Cache fixture telemetry cadence: do not allocate command-line argument arrays each frame.
edit(s+'RaceTrial.cs','float nextSample;','float nextSample,sampleInterval=1;')
edit(s+'RaceTrial.cs','automation=Has("--trial-auto")','sampleInterval=Arg("--traffic-case","").StartsWith("draft-")?1f/120:1;\n            automation=Has("--trial-auto")')
edit(s+'RaceTrial.cs','nextSample+=Arg("--traffic-case","").StartsWith("draft-")?1f/120:1;','nextSample+=sampleInterval;')
edit('tools/run-coasting-regressions.ps1','[switch]$RearPressureFilter,','[switch]$RearPressureFilter,[switch]$LightDrafting,')
edit('tools/run-coasting-regressions.ps1','rearPressureFilter=[bool]$RearPressureFilter;','rearPressureFilter=[bool]$RearPressureFilter;lightDrafting=[bool]$LightDrafting;')
edit('tools/run-coasting-regressions.ps1','$options.RearPressureFilter=[bool]$RearPressureFilter','$options.RearPressureFilter=[bool]$RearPressureFilter\n $options.LightDrafting=[bool]$LightDrafting')
doc=r/'docs/34-driver-identity-prototype.md'
with doc.open('a',encoding='utf-8') as f:f.write('\n\nThe user has now reviewed69 positively: substantially better race behaviour and no obvious visible logic flaws. Treat69 with rear-pressure filtering as the latest **race-feel reference**, while preserving its measured timing, boundary and performance limitations above. Desktop55 is unchanged. Next keep pace work bounded, add light symmetric drafting, then move into driver personalities. Do not recover faster old times by making leaders yield. Drafting design and acceptance: `docs/35-light-drafting.md`.\n')
doc=r/'review/unity-35/rear-pressure-69-review.md'
with doc.open('a',encoding='utf-8') as f:f.write('\n\n## Completed viewing and new priority\n\nThe user reviewed the replay as substantially better racing, with no obvious visible logic flaws.69 becomes the new race-feel reference. The replay reproduces enabled69 raw race arrays exactly. Its resolution changes from1920x1080 to3840x2160; frame p9527.4515ms/p9953.0819ms are not a fixed1080 performance comparison. Original budgets remain failed, with zero warm driver/worker allocation and no dropped frame records. Desktop55 is unchanged.\n\nA subsequent exact-parity decision audit (`decision-audit`) records131 car0 opening decisions without drops; the broader failed-candidate capture drops36 entries and captures nominal forecasts only. It suggests spare-space margin exhaustion and unresolved-plan straight braking around the hairpin, but does not establish a single causal fix. Retain these leads without delaying the user-requested bounded drafting/pace work and move toward personalities.\n')

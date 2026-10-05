using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LittleRoad
{
    // Captures the actual adventure scene. Review poses are explicit; no scenery
    // is substituted and no generated illustration is used as game evidence.
    public sealed class VillageReview:MonoBehaviour
    {
        [Serializable] public sealed class Evidence {public string scope="Static car-sized clearance and road/shoulder support samples, not continuous collision proof";public int roadSamples,garageSamples,forecourtSamples,supportSamples;public List<string> obstructions=new();public bool passed;}
        [Serializable] public sealed class DrivingEvidence {public string scope="Physical garage entry and reverse exit using synthetic trigger input; not a manual route playtest";public bool entered,exited,passed;public int contacts,recoveries;public float entryZ,exitZ,maxLateralError;public Vector3 exitPosition,exitVelocity;public string[] exitContacts;}
        AdventureUI ui;Camera cameraView;RoadCar player;string folder;
        // --street-review (Richard, 4 Oct: review every street in every town for misplaced assets and room to improve):
        // every road through Bayview, Hillcrest and Quarry Hills, every 40 m: a driver's-eye view along it and each
        // frontage from across the road, into captures/<town>/<street>/, with streets.json listing every photo's camera
        // and look-at point (so a close-up can repeat the view with capture-quarry.ps1 -Shots).
        IEnumerator StreetReview(RaceTrial world)
        {
            string Town(Vector3 p){var a=AreaBanner.Area(p);return a=="BAYVIEW"||a=="BAYVIEW HALT"?"bayview":a=="HILLCREST"||a=="HILLCREST CASTLE"?"hillcrest":a=="QUARRY HILLS"||a=="SLATE QUAY"?"quarry-hills":null;}
            string Slug(string s)=>new string(s.ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)?c:'-').ToArray()).Trim('-');
            var streets=new List<(string name,Vector3[] line,float half,bool closed)>{("Bayview road",world.track.points,Circuit.Width*.5f,true)};
            foreach(var road in HillcrestRoute.All())streets.Add((road.name,road.line,road.width*.5f,false));
            player.body.isKinematic=true;player.body.position+=Vector3.down*30;
            var manifest=new List<string>();int shots=0;
            foreach(var (name,line,half,closed) in streets) {
                int n=line.Length;var run=new List<float>{0};for(int i=1;i<n;i++)run.Add(run[^1]+Vector3.Distance(line[i],line[i-1]));float length=run[^1];
                Vector3 At(float s){s=Mathf.Clamp(s,0,length);int i=1;while(i<n-1&&run[i]<s)i++;return Vector3.Lerp(line[i-1],line[i],Mathf.InverseLerp(run[i-1],run[i],s));}
                int index=0;
                for(float s=10;s<length-10;s+=40) {
                    var p=At(s);string town=Town(p);if(town==null)continue;
                    var ahead=At(s+3)-At(s-3);ahead.y=0;if(ahead.sqrMagnitude<1e-4f)continue;ahead.Normalize();var right=Vector3.Cross(Vector3.up,ahead);
                    var views=new List<(string kind,Vector3 cam,Vector3 look)>{("along",p+Vector3.up*1.7f-right*1.2f,At(s+28)+Vector3.up*1.2f)};
                    foreach(int side in new[]{-1,1})views.Add((side<0?"left":"right",p-right*side*(half+2)+Vector3.up*6.5f-ahead*4,p+right*side*(half+9)+Vector3.up*3+ahead*4));
                    string dir=Path.Combine(folder,"streets",town,Slug(name));Directory.CreateDirectory(dir);
                    foreach(var (kind,cam,look) in views) {
                        cameraView.transform.position=cam;cameraView.transform.LookAt(look);cameraView.fieldOfView=55;WorldLook.Cut(cameraView);
                        yield return new WaitForSeconds(.3f);string file=$"{index:000}-{kind}.png";ui.Capture(Path.Combine(dir,file));shots++;
                        manifest.Add($"{{\"town\":\"{town}\",\"street\":\"{name}\",\"file\":\"streets/{town}/{Slug(name)}/{file}\",\"along_m\":{s:0},\"kind\":\"{kind}\",\"camera\":\"{cam.x:0.0},{cam.y:0.0},{cam.z:0.0}\",\"look\":\"{look.x:0.0},{look.y:0.0},{look.z:0.0}\"}}");
                    }
                    index++;
                }
            }
            File.WriteAllText(Path.Combine(folder,"streets.json"),"[\n"+string.Join(",\n",manifest)+"\n]\n");
            Debug.Log($"LITTLE_ROAD_STREET_REVIEW shots={shots} streets={manifest.Select(m=>m.Split(',')[1]).Distinct().Count()}");
        }
        IEnumerator Start()
        {
            folder=RaceTrial.Arg("--trial-captures");if(string.IsNullOrEmpty(folder))throw new Exception("World review requires --trial-captures");Directory.CreateDirectory(folder);
            var world=GetComponent<RaceTrial>();player=world.cars[0];ui=GetComponent<AdventureUI>();cameraView=Camera.main;
            Place(player.body.position,player.body.rotation.eulerAngles.y);
            yield return new WaitForSeconds(.3f);ui.Capture(Path.Combine(folder,"01-default-chase.png"));
            ui.SetVisible(false);
            if(RaceTrial.Has("--world-map")){yield return WorldMap.Capture(cameraView,folder,GetComponent<AdventureGame>().ResidentDirectory(),world.track);Application.Quit(0);yield break;}
            if(RaceTrial.Has("--edge-audit")){File.WriteAllText(Path.Combine(folder,"edge-audit.txt"),PlacementAudit.Edges(world.track));Application.Quit(0);yield break;}
            if(RaceTrial.Has("--seam-audit")){File.WriteAllText(Path.Combine(folder,"seam-audit.txt"),PlacementAudit.Seams(world.track));Application.Quit(0);yield break;}
            // --rejoin-test (Richard, 30 Sep: at Bayview Halt, once on the station grass you can't get back onto the
            // road): the car starts on the grass either side of the mainline, pointing at it at several angles, and
            // drives at it; each run reports whether it got onto the tarmac, and where and how it stopped if not.
            if(RaceTrial.Has("--rejoin-test")) {
                var sb=new System.Text.StringBuilder();int passed=0,runs=0;player.controlsBlocked=false;
                foreach(float side in new[]{1f,-1f})foreach(float angle in new[]{90f,60f,35f,20f})foreach(float throttle in new[]{.35f,1f})foreach(float z in new[]{-117f,-134f,-150f}) {
                    // The road runs roughly north-south at x 68-76 here; start 5 m out on the grass, clear of the palms,
                    // benches, lamps, map board and parked cars.
                    float x=side>0?81.5f:63.5f;float yaw=(side>0?270:90)+(side>0?-1:1)*(90-angle);
                    player.body.isKinematic=true;player.body.position=new Vector3(x,.3f,z);player.body.rotation=Quaternion.Euler(0,yaw,0);player.body.isKinematic=false;
                    player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;player.speed=0;
                    float t=0;player.commandSource=_=>new Vector2(throttle,0);bool onRoad=false;float worstStall=0;Vector3 stallAt=default;string touching="";
                    while(t<12){yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;
                        var p=player.body.position;if(p.x>68.3f&&p.x<75.8f&&player.OnRoadSurface()){onRoad=true;break;}   // (the road dips to -0.14 m at the south end)
                        if(t>1.5f&&Mathf.Abs(player.speed)<.4f){worstStall+=Time.fixedDeltaTime;stallAt=p;if(!string.IsNullOrEmpty(player.observedContactSurface))touching=$"{player.observedContactSurface} ({player.observedContactDeltaV:0.00} m/s)";}player.observedContactSurface=null;player.observedContactDeltaV=0;}
                    player.commandSource=null;runs++;if(onRoad)passed++;
                    if(!onRoad){var c=player.body.position;var f=player.transform.forward;cameraView.transform.position=c-f*5+Vector3.up*2.2f+player.transform.right*2.5f;cameraView.transform.LookAt(c+f*1.5f);yield return null;ui.Capture(Path.Combine(folder,$"rejoin-fail-{runs:00}.png"));}
                    var end=player.body.position;
                    sb.AppendLine($"{(onRoad?"ok  ":"FAIL")} from {(side>0?"station side":"sea side")} at {angle,2:0}° throttle {throttle:0.00} z {z}: {(onRoad?$"on the road after {t:0.0} s":$"stuck {(Mathf.Abs(end.x-72)<6.5f?"AT THE ROAD EDGE":"on scenery")} at ({end.x:0.00},{end.y:0.000},{end.z:0.00}) stalled {worstStall:0.0} s, speed {player.speed:0.0}, touching {touching}, grip {player.Grip:0.0}")}");
                }
                sb.Insert(0,$"rejoin runs={runs} ok={passed}\n");File.WriteAllText(Path.Combine(folder,"rejoin-test.txt"),sb.ToString());Debug.Log("LITTLE_ROAD_REJOIN "+passed+"/"+runs);
                Application.Quit(0);yield break;
            }
            // --trap-test: the car drives through every speed trap (docs/51) from 150 m back along the road at full throttle,
            // steering along it; each pass must be seen, logged and paid.
            if(RaceTrial.Has("--trap-test")) {
                var sb=new System.Text.StringBuilder();player.controlsBlocked=false;int seen=0;
                var log=new List<string>();Application.LogCallback hook=(m,s,t)=>{if(m.StartsWith("LITTLE_ROAD_SPEED_TRAP "))log.Add(m);};Application.logMessageReceived+=hook;
                foreach(var trap in SpeedTraps.All) {
                    var path=trap.approach;int from=Mathf.Max(0,path.Count-60);var start=path[from];var dirIn=(path[Mathf.Min(path.Count-1,from+3)]-start);dirIn.y=0;float gy=start.y;foreach(var h in Physics.RaycastAll(start+Vector3.up*30,Vector3.down,60))if(h.collider.GetComponentInParent<RoadSurface>()!=null)gy=Mathf.Max(gy,h.point.y);
                    player.body.isKinematic=true;player.body.position=new Vector3(start.x,gy+.3f,start.z);player.body.rotation=Quaternion.LookRotation(dirIn.normalized);player.body.isKinematic=false;
                    player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;player.speed=0;int before=log.Count;float t=0;
                    int aim=from;
                    player.commandSource=c=>{while(aim<path.Count-1&&Vector3.Distance(path[aim],c.body.position)<9)aim++;var to=(aim>=path.Count-1?path[^1]+(path[^1]-path[^2]).normalized*25:path[aim])-c.body.position;to.y=0;float angle=Vector3.SignedAngle(c.transform.forward,to,Vector3.up);return new Vector2(1,Mathf.Clamp(angle/25,-1,1));};
                    while(t<25&&log.Count==before){yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;}
                    // (And straight on: 0.6 s later the car must still be going, not stopped by the camera's confetti.)
                    float at=player.speed;for(float w=0;w<.6f;w+=Time.fixedDeltaTime)yield return new WaitForFixedUpdate();float after=player.speed;
                    player.commandSource=null;bool ok=log.Count>before&&after>at*.8f;if(ok)seen++;
                    sb.AppendLine($"{(ok?"ok  ":"FAIL")} {trap.id,-9} {(log.Count>before?log[^1]:"no pass logged")} after {t:0.0} s, {at*2.237f:0} mph then {after*2.237f:0} mph 0.6 s on (marks {trap.marks[0]}/{trap.marks[1]}/{trap.marks[2]})");
                }
                Application.logMessageReceived-=hook;
                sb.Insert(0,$"traps={SpeedTraps.All.Count} seen={seen}\n");File.WriteAllText(Path.Combine(folder,"trap-test.txt"),sb.ToString());Application.Quit(0);yield break;
            }
            // --train-race-test [bayview]: Beat the Train with an autopilot on the road route at 95% of the speed profile the
            // timetable assumes (docs/51); logs who won and by how much.
            if(RaceTrial.Has("--train-race-test")) {
                bool toHillcrest=RaceTrial.Arg("--train-race-test")!="bayview";var race=GetComponent<TrainRace>();var route=TrainRace.Route(toHillcrest);
                var sb=new System.Text.StringBuilder();player.controlsBlocked=false;
                var start=route[1];var into=route[5]-route[1];into.y=0;   // (on the road: the drop-off point faces the map board)
                player.body.isKinematic=true;player.body.position=start+Vector3.up*.4f;player.body.rotation=Quaternion.LookRotation(into.normalized);player.body.isKinematic=false;player.body.linearVelocity=Vector3.zero;
                var speeds=SpeedProfile.Speeds(route,player.tuning);int aim=1;
                var log=new List<string>();Application.LogCallback hook=(m,s,t)=>{if(m.StartsWith("LITTLE_ROAD_TRAIN_RACE"))log.Add(m);};Application.logMessageReceived+=hook;
                if(ValleyTrain.Instance!=null)ValleyTrain.Instance.Hold(toHillcrest?ValleyTrain.BayviewStop:ValleyTrain.HillcrestStop,6);
                race.Begin(toHillcrest);
                // Floored through the countdown: the car must stay put but rev (docs/51, Richard 1 Oct).
                Vector3 held=player.body.position;float maxRpm=0,maxMove=0,moveAt=0,startT=Time.time;bool movedEarly=false,moveHold=false;
                player.commandSource=c=>{
                    if(race.Holding){if(c.gears!=null)maxRpm=Mathf.Max(maxRpm,c.gears.Rpm);var moved=c.body.position-held;moved.y=0;if(moved.magnitude>maxMove){maxMove=moved.magnitude;moveAt=Time.time-startT;moveHold=c.holdStill;}if(moved.magnitude>.3f)movedEarly=true;return new Vector2(1,0);}
                    while(aim<route.Count-1&&Vector3.Distance(route[aim],c.body.position)<Mathf.Clamp(c.speed*.6f,6,14))aim++;
                    var to=route[aim]-c.body.position;to.y=0;float angle=Vector3.SignedAngle(c.transform.forward,to,Vector3.up);
                    float want=speeds[Mathf.Min(route.Count-1,aim+2)]*.95f;return new Vector2(DriveModel.PedalForSpeed(c.tuning,c.speed,want),Mathf.Clamp(angle/22,-1,1));};
                yield return new WaitForSeconds(1.5f);{var g=race.Goal;var back=(player.body.position-g);back.y=0;back=back.normalized;var keep=cameraView.transform.position;var keepR=cameraView.transform.rotation;cameraView.transform.position=g+back*55+Vector3.up*14;cameraView.transform.LookAt(g+Vector3.up*6);yield return null;ui.Capture(Path.Combine(folder,"train-race-beacon.png"));cameraView.transform.SetPositionAndRotation(keep,keepR);}
                float t=0,nextLog=0;while(race.Active&&t<600){yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;if(t>=nextLog){nextLog+=10;sb.AppendLine($"  t {t:0} elapsed {race.Elapsed:0.0} at ({player.body.position.x:0},{player.body.position.z:0}) speed {player.speed:0.0} throttle {player.throttle:0.00} aim {aim} blocked {player.controlsBlocked} kinematic {player.body.isKinematic}");}}
                player.commandSource=null;Application.logMessageReceived-=hook;
                sb.AppendLine($"rating {CarPerformanceRating.Calculate(player.tuning)}  stock ideal {TrainRace.StockTime(toHillcrest):0.0} s");
                sb.AppendLine($"countdown: {(movedEarly?"FAIL moved":"ok held still")} (most {maxMove:0.00} m at {moveAt:0.00} s, held {moveHold}), revved to {maxRpm:0} rpm");foreach(var l in log)sb.AppendLine(l);
                sb.AppendLine($"ended at ({player.body.position.x:0},{player.body.position.z:0}) after {t:0.0} s");
                File.WriteAllText(Path.Combine(folder,"train-race-test.txt"),sb.ToString());Application.Quit(0);yield break;
            }
            // --fireworks-review (with --time night --fireworks-now): the harbour show from the Bayview promenade (docs/51).
            if(RaceTrial.Has("--fireworks-review")) {
                cameraView.transform.position=new Vector3(-40,7,-118);cameraView.transform.LookAt(new Vector3(-30,62,-205));cameraView.fieldOfView=60;
                yield return new WaitForSeconds(4);
                for(int i=0;i<8;i++){ui.Capture(Path.Combine(folder,$"fireworks-{i}.png"));yield return new WaitForSeconds(1.5f);}
                Application.Quit(0);yield break;
            }
            // --horn-test: a honk in Hillcrest's market square (docs/51); folk answer in bubbles.
            if(RaceTrial.Has("--horn-test")) {
                var sq=HillcrestRoute.Square;var at=sq+new Vector3(-6,0,-4);float gy=ValleyField.Height(at.x,at.z);foreach(var h in Physics.RaycastAll(new Vector3(at.x,gy+20,at.z),Vector3.down,40))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null)gy=Mathf.Max(gy,h.point.y);
                player.body.isKinematic=true;player.body.position=new Vector3(at.x,gy+.4f,at.z);player.body.rotation=Quaternion.Euler(0,30,0);player.body.isKinematic=false;player.body.linearVelocity=Vector3.zero;
                yield return new WaitForSeconds(2);cameraView.transform.position=player.transform.position+new Vector3(-8,9,-12);cameraView.transform.LookAt(player.transform.position);
                var game=GetComponent<AdventureGame>();game.TestHonk();yield return new WaitForSeconds(.6f);
                ui.Capture(Path.Combine(folder,"horn-square.png"));Debug.Log("LITTLE_ROAD_HORN_TEST done");Application.Quit(0);yield break;
            }
            // --photo-requests-test: the camera at a natural vantage for each fixed sight of Pip's (docs/51); each must count.
            if(RaceTrial.Has("--photo-requests-test")) {
                var sb=new System.Text.StringBuilder();int ok=0,total=0;
                var lighthouse=PhotoRequests.All[0].where().Value;
                var shots=new List<(string id,Vector3 from,Vector3 look)>{
                    ("photo_lighthouse",lighthouse+new Vector3(28,-5,20),lighthouse),
                    ("photo_halt",new Vector3(66,3,-128),new Vector3(93,4,-142)),
                    ("photo_castle",HillcrestRoute.Vista+Vector3.up*2,HillcrestRoute.CastleCourt+Vector3.up*10),
                    ("photo_falls",HillcrestRoute.Lookout+Vector3.up*2,HillcrestRoute.FallsHead+Vector3.up*1.5f)};
                var anyBalloon=FindAnyObjectByType<BalloonDrift>();if(anyBalloon!=null){var b=anyBalloon.transform.position+Vector3.up*8;shots.Add(("photo_balloons",b+new Vector3(-70,-30,-70),b));}
                foreach(var (id,from,look) in shots) {
                    cameraView.transform.position=from;cameraView.transform.LookAt(look);cameraView.fieldOfView=50;yield return null;
                    var seen=PhotoRequests.Seen(cameraView);bool hit=seen.Exists(r=>r.id==id);total++;if(hit)ok++;
                    sb.AppendLine($"{(hit?"ok  ":"FAIL")} {id,-17} from ({from.x:0},{from.y:0},{from.z:0}) {Vector3.Distance(from,look):0} m · sees {string.Join(", ",seen.ConvertAll(r=>r.id))}");
                    ui.Capture(Path.Combine(folder,id+".png"));
                }
                sb.Insert(0,$"photo requests ok={ok}/{total}\n");File.WriteAllText(Path.Combine(folder,"photo-requests.txt"),sb.ToString());Application.Quit(0);yield break;
            }
            // --probe-line "x0,z0,x1,z1": every solid surface under a line, every 0.25 m (which collider, how high).
            if(RaceTrial.Has("--probe-line")) {
                var sb=new System.Text.StringBuilder();Physics.SyncTransforms();
                foreach(var probe in RaceTrial.Arg("--probe-line").Split(';')){
                var v=probe.Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                Vector3 a=new(v[0],0,v[1]),b=new(v[2],0,v[3]);float length=Vector3.Distance(a,b);sb.AppendLine("line "+probe);
                for(float s=0;s<=length;s+=.25f) {
                    var p=Vector3.Lerp(a,b,s/length);var hits=Physics.RaycastAll(p+Vector3.up*60,Vector3.down,120).Where(h=>!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null).OrderByDescending(h=>h.point.y).ToArray();
                    sb.AppendLine($"{s,6:0.00} ({p.x:0.00},{p.z:0.00}) "+string.Join(" | ",hits.Take(3).Select(h=>$"{h.point.y:0.000} {h.collider.name}{(h.collider.GetComponentInParent<RoadSurface>()!=null?" [road]":"")} n.y={h.normal.y:0.00}")));
                }}
                File.WriteAllText(Path.Combine(folder,"probe-line.txt"),sb.ToString());Application.Quit(0);yield break;
            }
            // --renderers-at "x,z;x,z": every renderer whose bounds stand over each point (name, material, bounds' height span),
            // for finding what draws an unexplained patch in a photo.
            if(RaceTrial.Has("--renderers-at")) {
                var sb=new System.Text.StringBuilder();
                foreach(var spot in RaceTrial.Arg("--renderers-at").Split(';')) {
                    var v=spot.Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();sb.AppendLine("at "+spot);
                    foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
                        var b=r.bounds;if(v[0]<b.min.x||v[0]>b.max.x||v[1]<b.min.z||v[1]>b.max.z||b.size.x>3000)continue;
                        string path=r.name;for(var t=r.transform.parent;t!=null&&path.Length<120;t=t.parent)path=t.name+"/"+path;
                        sb.AppendLine($"  {path} [{(r.sharedMaterial!=null?r.sharedMaterial.name+" / "+r.sharedMaterial.shader.name:"none")}] y {b.min.y:0.0}..{b.max.y:0.0} size {b.size.x:0}x{b.size.z:0}");
                    }
                }
                // --renderers-high "x,z,r": small renderers (under 3 m) centred within r of the point and over 6 m above the ground,
                // counted by name and material (what stands on the rooftops).
                if(RaceTrial.Has("--renderers-high")) {
                    var v=RaceTrial.Arg("--renderers-high").Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();var counts=new Dictionary<string,int>();
                    foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
                        var b=r.bounds;if(b.size.magnitude>3||new Vector2(b.center.x-v[0],b.center.z-v[1]).magnitude>v[2]||b.center.y<ValleyField.Height(b.center.x,b.center.z)+6)continue;
                        string key=r.name+" ("+(r.transform.parent!=null?r.transform.parent.name:"-")+") ["+(r.sharedMaterial!=null?r.sharedMaterial.name:"none")+"]";counts[key]=counts.TryGetValue(key,out int n)?n+1:1;
                    }
                    sb.AppendLine("high near "+RaceTrial.Arg("--renderers-high"));foreach(var kv in counts.OrderByDescending(k=>k.Value))sb.AppendLine($"  {kv.Value,5} {kv.Key}");
                }
                File.WriteAllText(Path.Combine(folder,"renderers-at.txt"),sb.ToString());Application.Quit(0);yield break;
            }
            // --spot-views "x,z,yaw;x,z,yaw;...": the car parked at each spot, seen from 14 m up and 22 m back, and
            // from straight above (road edges, banks and lips, in context).
            if(RaceTrial.Has("--spot-views")) {
                int n=0;
                foreach(var spot in RaceTrial.Arg("--spot-views").Split(';')) {
                    var v=spot.Split(',');float x=float.Parse(v[0],System.Globalization.CultureInfo.InvariantCulture),z=float.Parse(v[1],System.Globalization.CultureInfo.InvariantCulture),yaw=v.Length>2?float.Parse(v[2],System.Globalization.CultureInfo.InvariantCulture):0;
                    float y=float.MinValue;foreach(var h in Physics.RaycastAll(new Vector3(x,200,z),Vector3.down,400))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null)y=Mathf.Max(y,h.point.y);if(y==float.MinValue)y=0;
                    float up=v.Length>3?float.Parse(v[3],System.Globalization.CultureInfo.InvariantCulture):45;
                    var at=new Vector3(x,y+.1f,z);var back=Quaternion.Euler(0,yaw,0)*Vector3.back;n++;
                    yield return Frame($"spot-{n:00}-side",at,yaw,at+back*up*.5f+Vector3.up*up*.32f,at);
                    yield return Frame($"spot-{n:00}-top",at,yaw,at+Vector3.up*up+back*.01f,at);
                }
                Application.Quit(0);yield break;
            }
            // --duke-review: Duke Rolo's Regale (docs/53) at the castle, from four sides, the player's car out of shot.
            if(RaceTrial.Has("--duke-review")) {
                var duke=GameObject.Find("Resident castle");
                if(duke==null){File.WriteAllText(Path.Combine(folder,"duke-review.txt"),"no Duke");Application.Quit(2);yield break;}
                var c=duke.transform.position;var f=duke.transform.forward;var r=duke.transform.right;
                player.body.isKinematic=true;player.body.position=c+Vector3.down*30;
                foreach(var (name,from,look) in new[]{("duke-front",c+f*6.5f+r*3.8f+Vector3.up*1.9f,c+Vector3.up*.55f),("duke-rear",c-f*6.8f-r*3.6f+Vector3.up*2.1f,c+Vector3.up*.55f),
                                                      ("duke-side",c+r*7.8f+Vector3.up*1.1f,c+Vector3.up*.55f),("duke-courtyard",c+f*9+Vector3.up*13,c)}) {
                    cameraView.transform.position=from;cameraView.transform.LookAt(look);cameraView.fieldOfView=45;WorldLook.Cut(cameraView);
                    yield return new WaitForSeconds(.5f);ui.Capture(Path.Combine(folder,name+".png"));
                }
                Application.Quit(0);yield break;
            }
            // --view-shots "name:x,y,z>lx,ly,lz;...": a still from any camera position looking at any point (the
            // player's car parked out of shot). Used to check placements, e.g. the castle gatehouse on Via Crest.
            if(RaceTrial.Has("--view-shots")) {
                // A height written "g+2" is 2 m above the ground there (driver's-eye views without looking heights up).
                Vector3 V(string text){var s=text.Split(',');float F(string x)=>float.Parse(x,System.Globalization.CultureInfo.InvariantCulture);float x0=F(s[0]),z0=F(s[2]);
                    float y=s[1].StartsWith("g")?ValleyField.Height(x0,z0)+(s[1].Length>1?F(s[1].Substring(1)):0):F(s[1]); /* ("g" alone: on the ground) */return new Vector3(x0,y,z0);}
                player.body.isKinematic=true;player.body.position+=Vector3.down*30;
                foreach(var shot in RaceTrial.Arg("--view-shots").Split(';')) {
                    var name=shot.Split(':');var ends=name[1].Split('>');
                    cameraView.transform.position=V(ends[0]);cameraView.transform.LookAt(V(ends[1]));cameraView.fieldOfView=50;WorldLook.Cut(cameraView);
                    Time.timeScale=Mathf.Max(Time.timeScale,1);yield return new WaitForSecondsRealtime(.5f);ui.Capture(Path.Combine(folder,name[0]+".png"));   // (real time: a paused game hung here)
                    // Frame cost from this view (vsync off while measuring): mean and worst of 60 frames.
                    int vsync=QualitySettings.vSyncCount,target=Application.targetFrameRate;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    yield return null;yield return null;float sum=0,worst=0;
                    for(int i=0;i<60;i++){yield return null;float ms=Time.unscaledDeltaTime*1000;sum+=ms;worst=Mathf.Max(worst,ms);}
                    QualitySettings.vSyncCount=vsync;Application.targetFrameRate=target;
                    Debug.Log($"LITTLE_ROAD_VIEW_FRAME view={name[0]} mean_ms={sum/60:0.0} worst_ms={worst:0.0}");
                }
                Application.Quit(0);yield break;
            }
            if(RaceTrial.Has("--street-review")){yield return StreetReview(world);Application.Quit(0);yield break;}
            if(RaceTrial.Has("--car-audit")){yield return new WaitForSeconds(1);File.WriteAllText(Path.Combine(folder,"car-audit.txt"),StationarySpot.Audit());Application.Quit(0);yield break;}
            if(RaceTrial.Has("--area-audit")){yield return new WaitForSeconds(1);var text=AreaAudit.Run(GetComponent<AdventureGame>().ResidentDirectory(),world.track,out string json);
                File.WriteAllText(Path.Combine(folder,"area-audit.txt"),text);File.WriteAllText(Path.Combine(folder,"area-audit.json"),json);Application.Quit(0);yield break;}
            if(RaceTrial.Has("--placement-audit")){File.WriteAllText(Path.Combine(folder,"placement-audit.txt"),PlacementAudit.Run());Application.Quit(0);yield break;}
            if(RaceTrial.Has("--zip-survey")){File.WriteAllText(Path.Combine(folder,"zip-survey.txt"),ZipLine.Survey());Application.Quit(0);yield break;}
            if(RaceTrial.Has("--valley-views"))yield return ValleyViews(world);else {
            yield return Frame("02-village-approach",new Vector3(-39,.10f,-91),270,new Vector3(-28,4,-88),new Vector3(-66,2.2f,-104));
            yield return Frame("03-garage-forecourt",new Vector3(-66,.10f,-99),180,new Vector3(-69,3.4f,-88),new Vector3(-65,2.6f,-108));
            yield return Frame("04-open-workshop",new Vector3(-65,.10f,-108),180,new Vector3(-66.5f,2.9f,-100),new Vector3(-65,1.9f,-112));
            yield return Frame("05-village-wide",new Vector3(-65,.10f,-98),180,new Vector3(-14,15,-70),new Vector3(-42,2,-110));
            yield return Frame("06-coastal-road",world.track.At(570)+Vector3.up*.1f,0,new Vector3(-103,6,-44),new Vector3(-117,2,15));
            float bridge=world.track.Nearest(new Vector3(-117,0,9),out _);
            var forward=world.track.Forward(bridge);var right=world.track.Right(bridge);var centre=world.track.At(bridge);
            yield return Frame("07-bridge-approach",world.track.At(bridge-23),Quaternion.LookRotation(world.track.Forward(bridge-23)).eulerAngles.y,world.track.At(bridge-34)+Vector3.up*4,centre+Vector3.up*1.2f);
            yield return Frame("08-bridge-profile",centre,Quaternion.LookRotation(forward).eulerAngles.y,centre+right*43+forward*15+Vector3.up*5,centre+Vector3.down*4);
            var water=FindFirstObjectByType<CoastalWater>();
            if(water==null||!water.Surface.shader.isSupported)throw new Exception("Coastal water missing or unsupported");
            water.Surface.SetFloat("_ReviewTime",0);
            yield return Frame("09-water-time-0",centre,Quaternion.LookRotation(forward).eulerAngles.y,centre+right*43+forward*15+Vector3.down*3,centre+Vector3.down*10);
            water.Surface.SetFloat("_ReviewTime",1.6f);
            yield return Frame("10-water-time-1-6",centre,Quaternion.LookRotation(forward).eulerAngles.y,centre+right*43+forward*15+Vector3.down*3,centre+Vector3.down*10);
            var coast=world.track.At(554);var coastForward=world.track.Forward(554);
            water.Surface.SetFloat("_ReviewTime",0);
            yield return Frame("11-distant-water-time-0",coast,Quaternion.LookRotation(coastForward).eulerAngles.y,coast-coastForward*10+Vector3.up*4,coast+coastForward*10+Vector3.up);
            water.Surface.SetFloat("_ReviewTime",1.6f);
            yield return Frame("12-distant-water-time-1-6",coast,Quaternion.LookRotation(coastForward).eulerAngles.y,coast-coastForward*10+Vector3.up*4,coast+coastForward*10+Vector3.up);
            water.Surface.SetFloat("_ReviewTime",-1);
            if(RaceTrial.Has("--map-views"))yield return MapViews(world);
            }
            var evidence=new Evidence();Physics.SyncTransforms();
            var lane=GameObject.Find("Lighthouse lane");
            if(lane==null)evidence.obstructions.Add("Missing lighthouse access lane");
            else foreach(Vector3 vertex in lane.GetComponent<MeshFilter>().sharedMesh.vertices) {
                Vector3 p=lane.transform.TransformPoint(vertex);float d=world.track.Nearest(p,out _);
                if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(world.track.At(d).x,world.track.At(d).z))<11.8f)
                    evidence.obstructions.Add("Lighthouse lane geometry intrudes inside outer road shoulder");
            }
            for(float d=0;d<world.track.length;d+=4) {
                evidence.roadSamples++;var hits=Physics.OverlapBox(world.track.At(d)+Vector3.up, new Vector3(7.8f,.8f,1.5f),Quaternion.LookRotation(world.track.Forward(d)));
                foreach(var hit in hits)if(hit.CompareTag("Respawn"))evidence.obstructions.Add("Road "+d.ToString("0.0")+": "+hit.name);
            }
            float bay=GarageBayX;
            for(float z=-99;z>=-109;z-=1) {
                evidence.garageSamples++;var hits=Physics.OverlapBox(new Vector3(bay,.9f,z),new Vector3(1.25f,.75f,1.8f),Quaternion.identity);
                foreach(var hit in hits)if(hit.CompareTag("Respawn"))evidence.obstructions.Add("Garage "+z+": "+hit.name);
            }
            for(float x=-72;x<= -20;x+=2) {
                evidence.forecourtSamples++;var hits=Physics.OverlapBox(new Vector3(x,.9f,-95),new Vector3(1.35f,.75f,2),Quaternion.Euler(0,90,0));
                foreach(var hit in hits)if(hit.CompareTag("Respawn"))evidence.obstructions.Add("Forecourt "+x+": "+hit.name);
            }
            // Reject a visible road/shoulder over an unsupported void, and
            // terrain projecting through the road. Exclude the review car.
            for(float d=0;d<world.track.length;d+=2)foreach(float offset in new[]{-11f,-7f,0,7f,11f}) {
                evidence.supportSamples++;Vector3 point=world.track.At(d)+world.track.Right(d)*offset;
                var hits=Physics.RaycastAll(point+Vector3.up*2,Vector3.down,4).Where(h=>h.collider.GetComponentInParent<RoadCar>()==null).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0||Mathf.Abs(hits[0].point.y)>.08f)evidence.obstructions.Add("Road support "+d+" offset "+offset+": "+(hits.Length==0?"missing surface":hits[0].collider.name+" height "+hits[0].point.y));
            }
            evidence.obstructions=evidence.obstructions.Distinct().ToList();evidence.passed=evidence.obstructions.Count==0;
            File.WriteAllText(Path.Combine(folder,"clearance.json"),JsonUtility.ToJson(evidence,true));
            yield return DriveGarage();
            Debug.Log("LITTLE_ROAD_WORLD_REVIEW_COMPLETE clearance="+evidence.passed+" folder="+folder);Application.Quit(evidence.passed&&drivePassed?0:2);
        }
        // Fixed before/after views for the Bayview map sweep: overhead, eight chase-cam
        // views around the lap (as the player sees it), and landmark/wide shots.
        IEnumerator MapViews(RaceTrial world)
        {
            var track=world.track;float fov=cameraView.fieldOfView;
            cameraView.fieldOfView=40;yield return Frame("m01-overhead",track.At(0),0,new Vector3(0,420,-160),new Vector3(0,0,-5));cameraView.fieldOfView=fov;
            for(int i=0;i<8;i++) {
                float d=track.length*(i+.5f)/8;var f=track.Forward(d);var at=track.At(d);
                yield return Frame("m"+(i+2).ToString("00")+"-chase-"+Mathf.RoundToInt(d),at,Quaternion.LookRotation(f).eulerAngles.y,at-f*6.5f+Vector3.up*3.4f,at+Vector3.up*.9f+f*1.7f);
            }
            var post=track.At(track.length*.46f)-track.Right(track.length*.46f)*16;
            yield return Frame("m10-lighthouse",post+(post-track.At(track.length*.46f)).normalized*-4,0,post+new Vector3(-26,9,-22),post+new Vector3(4,6,6));
            yield return Frame("m11-village-from-sea",new Vector3(-40,0,-95),90,new Vector3(-30,14,-160),new Vector3(-35,3,-100));
            yield return Frame("m13-harbour",new Vector3(-40,0,-95),90,new Vector3(-12,6,-172),new Vector3(-34,-6,-138));
            yield return Frame("m14-promenade",new Vector3(-40,0,-95),90,new Vector3(-8,2.2f,-129),new Vector3(-45,-1,-137));
            yield return Frame("m15-windmill",track.At(track.length*.6f),0,new Vector3(-24,48,-40),new Vector3(-24,28,12));
            yield return Frame("m16-farm",track.At(track.length*.6f),0,new Vector3(46,34,-38),new Vector3(38,8,0));
            yield return Frame("m17-campsite",track.At(track.length*.6f),0,new Vector3(-40,34,-46),new Vector3(-58,12,-20));
            yield return Frame("m18-green",new Vector3(-40,0,-95),90,new Vector3(-20,9,-136),new Vector3(-35,1,-121));
            yield return Frame("m19-car-park",new Vector3(-40,0,-95),90,new Vector3(4,5,-93),new Vector3(20,0,-103));
            // Interaction prompt: stopped in front of Mara at the Pit Stop garage bay.
            if(PitStop.Hosts.TryGetValue("bayview/garage",out var mara)){var car=mara.at+new Vector3(0,0,6.2f);
                ui.SetVisible(true);yield return Frame("i00-prompt-mara",car,180,car+new Vector3(1.5f,3.4f,6.5f),car+Vector3.up*.9f+new Vector3(0,0,-1.7f));ui.SetVisible(false);}
            yield return Frame("h1-causeway-start",new Vector3(27,0,-101.5f),100,new Vector3(14,7,-92),new Vector3(60,0,-125));
            yield return Frame("h2-causeway-side",new Vector3(27,0,-101.5f),100,new Vector3(150,6,-250),new Vector3(80,-4,-240));
            yield return Frame("h3-mainland",new Vector3(27,0,-101.5f),100,new Vector3(100,70,-330),new Vector3(92,20,-560));
            yield return Frame("h4-hill-road",new Vector3(27,0,-101.5f),100,new Vector3(150,40,-470),new Vector3(100,15,-520));
            yield return Frame("m12-inland-hills",track.At(track.length*.6f),0,new Vector3(-10,42,-60),new Vector3(-20,6,30));
        }
        // Hillcrest Valley review frames (--valley-views): the crossing, the reveal, the town,
        // the landmarks and an overhead map. The car is parked by the square throughout.
        IEnumerator ValleyViews(RaceTrial world)
        {
            var sq=HillcrestRoute.Square;var park=sq+new Vector3(-6,0,-4);
            bool costOnly=RaceTrial.Has("--valley-cost-only");
            IEnumerator V(string name,Vector3 cameraPosition,Vector3 target,float fov=55,bool keepPlayer=false)
            {
                if(costOnly)yield break;
                cameraView.fieldOfView=fov;if(!keepPlayer)PlaceAt(park,60);
                cameraView.transform.position=cameraPosition;cameraView.transform.LookAt(target);WorldLook.Cut(cameraView);yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.6f);
                ui.Capture(Path.Combine(folder,name+".png"));Debug.Log("VALLEY_FRAME "+name);
            }
            var main=HillcrestRoute.Mainline;float north=HillcrestRoute.PortalNorthRun,south=HillcrestRoute.PortalSouthRun;
            Vector3 On(float d,float up=0,float side=0)=>main.At(d)+Vector3.up*up+main.Right(d)*side;
            yield return V("v01-causeway-approach",On(north-150,4.5f,-1),On(north,6));
            yield return V("v02-cliff-from-sea",new Vector3(20,6,-360),new Vector3(100,30,-470));
            yield return V("v03-tunnel",On(north+40,2.4f),On(south,1.5f));
            yield return V("v04-the-reveal",On(south+6,3.2f),On(south+6,-6)+main.Forward(south+6)*200,62);
            yield return V("v05-vista",HillcrestRoute.Vista+new Vector3(-4,3,4),new Vector3(200,30,-1100));
            yield return V("v06-valley-aerial",new Vector3(60,300,-420),new Vector3(160,0,-1100),60);
            yield return V("v07-town-from-bridge",On(main.Length-50,2.5f),new Vector3(200,40,-1100));
            yield return V("v08-square",sq+new Vector3(-34,12,-8),sq+new Vector3(10,2,0));
            yield return V("v09-castle",new Vector3(110,85,-990),new Vector3(200,52,-1110));
            yield return V("v10-falls",HillcrestRoute.Lookout+new Vector3(-2,5,-6),HillcrestRoute.FallsHead+new Vector3(10,-6,20));
            // The drop itself, from the gorge below (the lookout sits above the lip).
            {var river=HillcrestRoute.River.line;int fall=System.Array.FindIndex(river,q=>q.y<.9f);var foot=river[Mathf.Min(river.Length-1,fall+4)];var down=river[Mathf.Min(river.Length-1,fall+14)];
             var back=down-foot;back.y=0;back.Normalize();yield return V("v10b-falls-below",down+back*6+Vector3.up*7,foot+Vector3.up*5);}
            yield return V("v11-raceway",new Vector3(-280,55,-980),new Vector3(-450,10,-1260));
            yield return V("v12-station",new Vector3(420,45,-930),new Vector3(505,9,-1035));
            yield return V("v13-mountains",new Vector3(150,14,-1320),new Vector3(150,260,-3600));
            yield return V("v14-back-to-sea",new Vector3(200,70,-1150),new Vector3(100,20,-450));
            yield return V("v15-overhead",new Vector3(100,2600,-1080),new Vector3(100,0,-1079),45);
            yield return V("v16-bayview-halt",new Vector3(62,10,-98),new Vector3(100,1,-145));
            yield return V("v17-sea-viaduct",new Vector3(60,14,-300),new Vector3(135,10,-380));
            var crest=HillcrestRoute.Crest;yield return V("v18-street-life",crest.At(140)+Vector3.up*2.2f-crest.Forward(140)*6,crest.At(170)+Vector3.up*1.5f);
            yield return V("v19-quay-life",new Vector3(20,3.2f,-905)+Vector3.up*6,new Vector3(160,6,-915));
            yield return V("v20-lake-shore",new Vector3(-330,16,-1180),new Vector3(-400,4,-1260));
            yield return V("v21-poplar-avenue",HillcrestRoute.Mainline.At(HillcrestRoute.Mainline.Length-160)+Vector3.up*2.5f,HillcrestRoute.Mainline.At(HillcrestRoute.Mainline.Length-100)+Vector3.up*2);
            // Interaction prompts: stopped beside Gio (prompt card + targeted badge), then the Pit Stop
            // badges from the road while driving past (the card asks you to slow down).
            if(HillcrestTown.Spots.TryGetValue("motors",out var gio)) {
                var rot=Quaternion.Euler(0,gio.yaw,0);var stand=gio.at+rot*new Vector3(0,0,6.5f);
                PlaceAt(stand,gio.yaw+180);yield return new WaitForSeconds(.8f);
                ui.SetVisible(true);yield return V("i01-prompt-gio",stand+rot*new Vector3(-2.5f,3.2f,7.5f),gio.at+Vector3.up*1.2f,58,true);ui.SetVisible(false);
                var road=gio.at+rot*new Vector3(-14,0,10);PlaceAt(road,gio.yaw+90);yield return new WaitForSeconds(.8f);
                yield return V("i02-pit-badges",gio.at+rot*new Vector3(-22,3.5f,10),gio.at+Vector3.up*2+rot*new Vector3(4,0,0),58,true);
            }
            // Meadow grass up close (GrassField): eye height across open ground by the lake.
            {var g=new Vector3(-310,0,-1160);g.y=ValleyField.Height(g.x,g.z)+1.3f;var look=new Vector3(-340,0,-1190);look.y=ValleyField.Height(look.x,look.z)+.4f;
             PlaceAt(park,60);cameraView.transform.position=g;cameraView.transform.LookAt(look);yield return new WaitForSeconds(1.5f);yield return V("g01-meadow-close",g,look);}
            // Pit Stops (every town): three-quarter arrival view and the bays close up.
            foreach(var pair in PitStop.Sites) {
                var site=pair.Value;float yaw=0;foreach(var h in PitStop.Hosts)if(h.Key.StartsWith(pair.Key+"/")){yaw=h.Value.yaw;break;}
                var rot=Quaternion.Euler(0,yaw,0);
                yield return V("p-"+pair.Key+"-arrival",site+rot*new Vector3(-28,9,19),site+Vector3.up*5+rot*new Vector3(0,0,4));
                yield return V("p-"+pair.Key+"-bays",site+rot*new Vector3(4,2.4f,19),site+Vector3.up*2.6f,62);
            }
            if(HillcrestTown.Chimneys.Count>0){var ch=HillcrestTown.Chimneys[0];yield return V("v22-chimney",ch+new Vector3(10,4,10),ch);}
            // The Bayview end: junction, quay, gateway, bridge, ridge and each tunnel mouth.
            yield return V("v23-junction",On(0,4,-10)-main.Forward(0)*14,On(40,1));
            yield return V("v24-halt-forecourt",new Vector3(80,3.2f,-104),new Vector3(92,1,-150));
            yield return V("v25-halt-from-sea",new Vector3(150,6,-205),new Vector3(92,2,-135));
            yield return V("v26-gateway",On(40,2.2f,-1.5f),On(110,2));
            yield return V("v27-bridge-arches",new Vector3(40,-6,-300),new Vector3(90,4,-330));
            yield return V("v28-north-portal",On(north-45,4,-3),On(north,6));
            yield return V("v29-sea-portal",HillcrestRoute.SeaPortal+new Vector3(-6,6,40),HillcrestRoute.SeaPortal+Vector3.up*3);
            yield return V("v30-ridge-from-sea",new Vector3(-150,8,-330),new Vector3(0,30,-480));
            yield return V("v31-south-portal",On(south+30,4,2),On(south,5));
            yield return V("v32-valley-rail-portal",HillcrestRoute.ValleyPortal+new Vector3(40,8,-6),HillcrestRoute.ValleyPortal+Vector3.up*3);
            // Golden Acre Farm. A crop circle and a drive line are pressed into the wheat first.
            Vector3 Fm(float x,float z,float up=0){var p=HillcrestRoute.Farm(x,z);p.y=ValleyField.Height(p.x,p.z)+up;return p;}
            var golden=WheatField.All.Find(f=>f.name=="Golden Acre wheat");var ring=Fm(HillcrestTown.WheatLocal.x+30,HillcrestTown.WheatLocal.y);
            if(golden!=null) {
                for(float a=0;a<360;a+=3){var dir=new Vector3(Mathf.Sin(a*Mathf.Deg2Rad),0,Mathf.Cos(a*Mathf.Deg2Rad));var tangent=Vector3.Cross(Vector3.up,dir);golden.Press(ring+dir*14,tangent,tangent,1.2f,1.3f);golden.Press(ring+dir*5,tangent,tangent,1.2f,1.3f);}
                Vector3 from=Fm(-46,-8),to=Fm(0,24);for(float t=0;t<=1;t+=.01f)golden.Press(Vector3.Lerp(from,to,t),to-from,to-from,2.3f,1.2f);
            }
            yield return V("v33-farm-aerial",Fm(-170,150,80),Fm(0,0));
            yield return V("v34-farm-yard",Fm(-116,40,5),Fm(-60,78,3));
            yield return V("v35-wheat-car-height",Fm(-52,-14,1.5f),Fm(10,-2,1));
            yield return V("v36-crop-circle",ring+new Vector3(-30,40,-30),ring);
            yield return V("v37-sunflowers",Fm(-70,-60,6),Fm(-98,4));
            yield return V("v38-farm-gate",Fm(-150,48,4),Fm(-100,56,3));
            // Realism sweep: the Bayview gatehouses from the water, the station forecourt, the paddock.
            yield return V("v39-gatehouses-from-sea",new Vector3(118,3,-372),new Vector3(120,20,-452));
            yield return V("v40-gatehouses-close",On(north-60,6,-12),new Vector3(115,24,-452));
            yield return V("v41-station-forecourt",new Vector3(404,14,-1004),new Vector3(436,8,-1036));
            yield return V("v42-paddock",new Vector3(-320,22,-1012),new Vector3(-362,9,-1056));
            // Junctions and the raceway (v2 layout).
            yield return V("j01-mainline-quay",new Vector3(92,22,-872),new Vector3(110,7,-905));
            yield return V("j02-crest",new Vector3(244,20,-948),new Vector3(262,7,-922));
            yield return V("j03-station-road",new Vector3(378,20,-962),new Vector3(395,7,-938));
            yield return V("j04-bayview-junction",new Vector3(56,14,-104),new Vector3(68,0,-80));
            yield return V("j05-give-way",On(main.Length-42,2.2f,1.6f),On(main.Length-2,1.2f));
            yield return V("r01-raceway-overhead",new Vector3(-400,700,-1262),new Vector3(-400,0,-1261),50);
            yield return V("r02-raceway-hairpins",new Vector3(-120,30,-1230),new Vector3(-200,9,-1310));
            yield return V("r03-raceway-t1",new Vector3(-470,6,-1082),new Vector3(-584,9,-1115));
            // Fidelity close-ups: a street facade, the castle walls, causeway masonry, trees.
            yield return V("f01-facades",crest.At(150)-crest.Right(150)*1.5f+Vector3.up*2.2f,crest.At(162)+crest.Right(162)*12+Vector3.up*4);
            yield return V("f02-castle",HillcrestRoute.CastleCourt+new Vector3(-50,8,-45),HillcrestRoute.CastleCourt+Vector3.up*8);
            yield return V("f03-causeway",On(north-120,-1,11),On(north-90,0,3));
            yield return V("f04-square-close",sq+new Vector3(-14,2.5f,-6),sq+new Vector3(14,5,8));
            // Hillcrest Castle and the zipline: far, courtyard, the Zip Tower, the whole span, and a real ride.
            var court=HillcrestRoute.CastleCourt;
            yield return V("c01-castle-lower-town",court+new Vector3(150,-22,230),court+Vector3.up*22,50);
            yield return V("c02-castle-courtyard",court+new Vector3(-4,3.5f,-30),court+new Vector3(0,14,18));
            yield return V("c03-castle-from-valley",court+new Vector3(-260,-30,120),court+Vector3.up*25,40);
            var zip=ZipLine.Active;
            if(zip!=null&&!costOnly) {
                var inward=Quaternion.Euler(0,zip.top.facing+180,0)*Vector3.forward;
                yield return V("z01-zip-tower",zip.top.bay+inward*16+Vector3.up*4,zip.top.bay+Vector3.up*9);
                var midSpan=(zip.top.cable+zip.bottom.cable)*.5f;var across=Vector3.Cross(Vector3.up,(zip.bottom.cable-zip.top.cable).normalized);
                yield return V("z02-zip-span",midSpan+across*150+Vector3.up*25,midSpan,50);
                yield return V("z07-zip-landing",zip.bottom.bay+Quaternion.Euler(0,zip.bottom.facing,0)*new Vector3(22,20,-34),zip.bottom.bay+Vector3.up*3,50);
                yield return V("z03-zip-station",zip.bottom.bay+Quaternion.Euler(0,zip.bottom.facing,0)*new Vector3(10,5,-14),zip.bottom.bay+Vector3.up*6);
                foreach(bool down in new[]{true,false}) {
                    var from=down?zip.top:zip.bottom;PlaceAt(from.bay+Quaternion.Euler(0,from.facing,0)*Vector3.back*5,from.facing);
                    player.body.isKinematic=true;float start=Time.time;var shots=down?new[]{(3.5f,"z04-ride-launch"),(9f,"z05-ride-mid")}:new[]{(12f,"z06-ride-up")};int next=0;
                    StartCoroutine(zip.Ride(player,down));yield return null;
                    while(zip.Busy&&Time.time-start<120) {
                        var fwd=player.transform.forward;fwd.y=0;fwd.Normalize();
                        cameraView.transform.position=player.transform.position-fwd*13+Vector3.up*5.5f+Vector3.Cross(Vector3.up,fwd)*4;cameraView.transform.LookAt(player.transform.position+Vector3.up);
                        if(next<shots.Length&&Time.time-start>=shots[next].Item1){ui.Capture(Path.Combine(folder,shots[next].Item2+".png"));next++;}
                        yield return null;
                    }
                    var to=down?zip.bottom:zip.top;float miss=Vector3.Distance(player.body.position,to.bay);
                    player.body.isKinematic=false;
                    Debug.Log($"ZIP_RIDE down={down} seconds={Time.time-start:0.0} arrivalMiss={miss:0.00}");
                    File.AppendAllText(Path.Combine(folder,"zip-ride.txt"),$"down={down} seconds={Time.time-start:0.0} arrivalMiss={miss:0.00}\n");
                }
            }
            cameraView.fieldOfView=55;
            // Frame cost at driving-height views (mean / 95th percentile over 240 frames each).
            var lines=new List<string>();int cap=Application.targetFrameRate;Application.targetFrameRate=1000;QualitySettings.vSyncCount=0;
            foreach(var (name,at,look) in new[]{("reveal",On(south+6,3.2f),On(south+6,-6)+main.Forward(south+6)*200),("vista",HillcrestRoute.Vista+new Vector3(-4,3,4),new Vector3(200,30,-1100)),
                ("square",sq+new Vector3(-20,3,-6),sq+new Vector3(20,3,6)),("castle",HillcrestRoute.CastleCourt+new Vector3(0,4,-20),HillcrestRoute.CastleCourt+new Vector3(0,4,20)),("quay",new Vector3(60,9,-905),new Vector3(300,9,-925)),("bayview",new Vector3(-30,4,-80),new Vector3(-60,2,-110))}) {
                cameraView.transform.position=at;cameraView.transform.LookAt(look);yield return new WaitForSeconds(.5f);
                var times=new List<float>();for(int i=0;i<240;i++){yield return null;times.Add(Time.unscaledDeltaTime*1000);}
                times.Sort();lines.Add($"{name} mean={times.Average():0.00}ms p95={times[(int)(times.Count*.95f)]:0.00}ms");
            }
            Application.targetFrameRate=cap;File.WriteAllLines(Path.Combine(folder,"valley-frame-cost.txt"),lines);Debug.Log("VALLEY_FRAME_COST "+string.Join(" | ",lines));
        }
        void PlaceAt(Vector3 position,float heading)
        {
            position.y=ValleyField.Height(position.x,position.z)+.02f-player.ContactBottom;
            player.body.position=position;player.body.rotation=Quaternion.Euler(0,heading,0);player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;
            player.transform.SetPositionAndRotation(position,player.body.rotation);Physics.SyncTransforms();
        }
        bool drivePassed;
        // The Pit Stop's garage bay (kit X mirrors on import: Blender -4.5 lands at +4.5).
        static float GarageBayX=>BayviewKit.Has("pit_stop_2")?VillageKit.PitStopSite.x+4.5f:-65;
        void Place(Vector3 position,float heading)
        {
            position.y=.015f-player.ContactBottom;
            player.body.position=position;player.body.rotation=Quaternion.Euler(0,heading,0);player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;
            // Review teleports must synchronise the render transform as well
            // as the physics pose before a camera capture requests culling.
            player.transform.SetPositionAndRotation(position,player.body.rotation);Physics.SyncTransforms();
        }
        IEnumerator Frame(string name,Vector3 carPosition,float heading,Vector3 cameraPosition,Vector3 target)
        {
            Place(carPosition,heading);
            cameraView.transform.position=cameraPosition;cameraView.transform.LookAt(target);yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.5f);
            if(Vector3.Distance(player.body.position,player.transform.position)>.05f)throw new Exception("Review physics/render poses diverged: "+name);
            ui.Capture(Path.Combine(folder,name+".png"));
            Debug.Log("WORLD_FRAME "+name+" body="+player.body.position+" render="+player.transform.position+" visibleRenderers="+player.GetComponentsInChildren<Renderer>().Count(r=>r.isVisible));
        }
        IEnumerator DriveGarage()
        {
            float bay=GarageBayX;var result=new DrivingEvidence();Place(new Vector3(bay,0,-98),180);
            var probe=player.gameObject.AddComponent<GarageContactReview>();
            yield return new WaitForSeconds(.1f);player.controlsBlocked=false;
            int initialContacts=player.contacts,initialRecoveries=player.recoveries;
            // A real controller plugged in would stay Gamepad.current and hide the synthetic pad's input.
            var others=Gamepad.all.Where(g=>g.enabled).ToArray();foreach(var g in others)InputSystem.DisableDevice(g);
            var pad=InputSystem.AddDevice<Gamepad>("GarageClearanceTest");pad.MakeCurrent();
            try {
                float deadline=Time.time+14;
                while(player.body.position.z> -107&&Time.time<deadline) {
                    InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=player.speed<2?.30f:0});
                    result.maxLateralError=Mathf.Max(result.maxLateralError,Mathf.Abs(player.body.position.x-bay));yield return new WaitForFixedUpdate();
                }
                Debug.Log($"GARAGE_DRIVE_STATE z={player.body.position.z:0.00} speed={player.speed:0.00} throttle={player.throttle:0.00} blocked={player.controlsBlocked} running={player.GetComponentInParent<RoadCar>()!=null} pad={Gamepad.current?.name} trigger={Gamepad.current?.rightTrigger.ReadValue():0.00} kinematic={player.body.isKinematic} time={Time.timeScale}");
                result.entryZ=player.body.position.z;result.entered=result.entryZ<= -107;
                InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});
                deadline=Time.time+14;
                while(player.body.position.z< -98&&Time.time<deadline) {
                    InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=player.speed> -2?.30f:0});
                    result.maxLateralError=Mathf.Max(result.maxLateralError,Mathf.Abs(player.body.position.x-bay));yield return new WaitForFixedUpdate();
                }
                result.exitZ=player.body.position.z;result.exited=result.exitZ>= -98;
                result.exitPosition=player.body.position;result.exitVelocity=player.body.linearVelocity;result.exitContacts=probe.contacts.ToArray();
            }finally {InputSystem.RemoveDevice(pad);foreach(var g in others)InputSystem.EnableDevice(g);player.controlsBlocked=true;}
            result.contacts=player.contacts-initialContacts;result.recoveries=player.recoveries-initialRecoveries;
            result.passed=result.entered&&result.exited&&result.contacts==0&&result.recoveries==0&&result.maxLateralError<.3f;
            drivePassed=result.passed;File.WriteAllText(Path.Combine(folder,"garage-driving.json"),JsonUtility.ToJson(result,true));
            Debug.Log("LITTLE_ROAD_GARAGE_DRIVING passed="+result.passed);
        }
    }
    public sealed class GarageContactReview:MonoBehaviour
    {
        public readonly HashSet<string> contacts=new();
        void OnCollisionStay(Collision collision)
        {
            foreach(var point in collision.contacts)if(point.normal.y<.9f)
                contacts.Add(collision.collider.name+" normal="+point.normal.ToString("F3")+" point="+point.point.ToString("F3"));
        }
    }
}

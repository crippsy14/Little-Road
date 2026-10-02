using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    [DefaultExecutionOrder(-100)]
    public sealed class PerformanceTrial:MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public float time,routeDistance,travelled,x,y,z,speed,offset,throttle,steering,targetSpeed,longitudinalAcceleration,lateralVelocity,requestedYaw,actualYaw;
            public float routeHeight,grade,groundClearance,alignmentDegrees,expectedCoastAcceleration;
            public bool grounded=true;
        }
        [Serializable] public sealed class Sector {public string name;public float endRouteDistance,time,elapsed,speed;}
        [Serializable] public sealed class Result
        {
            public string id,course,fixtureVersion=PerformanceCourse.Version,loadout,driver,surface="dry tarmac",body="compact_v1",utc,assemblyVersion;
            public int repeat,seed=1709,steps,contacts,recoveries,missedGates;
            public int airborneSteps,groundQuerySaturations;
            public bool completed,valid,sustainedSpeedReached;
            public string failure;
            public float time,routeLength,travelledDistance,averageSpeed,routeAverageSpeed,peakSpeed,sustainedTopSpeed=-1,
                zeroTo30=-1,zeroTo50=-1,stopDistance=-1,stopTime=-1,brakeEntrySpeed,apexMinSpeed=-1,turnExitSpeed=-1,
                maxAbsOffset,maxLateralVelocity,maxAirClearance,physicsStep,straightLength,turnRadius,roadWidth=Circuit.Width,
                predictedTopSpeed,predictedZeroTo30,simulationScale;
            public float entrySpeed,finalSpeed,startHeight,endHeight,peakHeight,maxAlignmentDegrees;
            public CarTuning tuning;
            public FittedPart[] fitted;
            public Sector[] sectors;
            public Sample[] samples;
        }
        [Serializable] sealed class Batch
        {
            public string fixtureVersion=PerformanceCourse.Version,unity,graphics,utc;
            public string scope="Actual RoadCar/PhysX, dry fixtures including authored grade/coasting cases when selected; no wet surface, AI variance or visible performance acceptance.";
            public bool complete;public Result[] runs;
        }
        sealed class Case {public string course,loadout;public int repeat;}
        [Serializable] sealed class ProfilePoint {public float distance,height,grade,turnCurvature;public Vector3 position;}
        [Serializable] sealed class Profile {public string course,version;public float start,finish;public ProfilePoint[] samples;}
        readonly List<Case> cases=new();readonly List<Result> results=new();readonly List<Sample> samples=new();readonly List<Sector> sectors=new();
        readonly HashSet<Material> materials=new();readonly List<Mesh> meshes=new();
        RaceTrial race;PerformanceCourse course;RoadCar car;GameObject scenery;Camera cameraView;AdventureUI ui;
        Result result;int caseIndex=-1,nextGate,steps;float elapsed,warmup,sampleAt,travelled,previousSpeed,previousRoute,sectorStart,peak,minApex,maxOffset,maxLateral,maxClearance;
        Vector3 previousPosition;bool running,settling,manual,capturePending;string output,captureName;float nextCaseAt;double distanceSum;
        static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;

        void Start()
        {
            Application.logMessageReceived+=OnLog;
            try {
                output=RaceTrial.Arg("--performance-output");if(string.IsNullOrEmpty(output))throw new ArgumentException("Performance trials require an explicit --performance-output folder.");
                output=Path.GetFullPath(output);Directory.CreateDirectory(output);
                if(File.Exists(Path.Combine(output,"batch.json")))throw new IOException("Use a new output folder; previous trial evidence will not be overwritten.");
                manual=RaceTrial.Has("--performance-manual");int repeats=int.Parse(RaceTrial.Arg("--performance-repeats","3"),Invariant);
                if(repeats<1||repeats>100)throw new ArgumentException("Repeat count must be 1..100.");
                foreach(string fixture in RaceTrial.Arg("--performance-courses","straight,brake,uturn").Split(','))
                    foreach(string setup in RaceTrial.Arg("--performance-loadouts","stock,engine_workshop,transmission_long,tyres_sport,brakes_sport,brakes_sport+tyres_sport,club").Split(','))
                        for(int repeat=0;repeat<repeats;repeat++){_ = new PerformanceCourse(fixture);_ = PerformanceCourse.Loadout(setup);cases.Add(new Case{course=fixture,loadout=setup,repeat=repeat});}
                if(manual&&cases.Count!=1)throw new ArgumentException("Manual mode needs one course, loadout and repeat.");
                Time.timeScale=manual?1:Mathf.Clamp(float.Parse(RaceTrial.Arg("--performance-timescale","1"),Invariant),.25f,12);
                Time.fixedDeltaTime=1f/120;Time.maximumDeltaTime=.25f;Application.targetFrameRate=120;QualitySettings.vSyncCount=0;
                race=GetComponent<RaceTrial>();race.freeRoam=true;race.elapsed=1;
                ui=gameObject.AddComponent<AdventureUI>();cameraView=new GameObject("Proving ground chase").AddComponent<Camera>();cameraView.tag="MainCamera";
                cameraView.fieldOfView=55;cameraView.nearClipPlane=.1f;cameraView.farClipPlane=2500;
                cameraView.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                var sun=new GameObject("Proving ground sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.shadows=LightShadows.Soft;
                RenderSettings.ambientLight=new Color(.55f,.64f,.72f);cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.50f,.72f,.83f);
                BeginNext();
            }catch(Exception e){Debug.LogException(e);}
        }
        void OnLog(string text,string stack,LogType type){if(type==LogType.Exception){Debug.LogError("PERFORMANCE_TRIAL_FAILED "+text);Application.Quit(2);}}
        void BeginNext()
        {
            ClearCase();caseIndex++;
            if(caseIndex>=cases.Count){SaveBatch(true);Debug.Log("LITTLE_ROAD_PERFORMANCE_BATCH_COMPLETE runs="+results.Count+" valid="+results.Count(r=>r.valid));Application.Quit(results.All(r=>r.valid)?0:3);return;}
            var item=cases[caseIndex];course=new PerformanceCourse(item.course);race.track=course.track;
            BuildScenery();SaveProfile();var go=TrialWorld.Car("compact_v1",new Color(.08f,.56f,.49f));go.transform.position=course.track.At(course.start)+Vector3.up*.15f;go.transform.rotation=Quaternion.LookRotation(course.track.Forward(course.start),course.track.Normal(course.start));
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)materials.Add(material);
            car=go.AddComponent<RoadCar>();var fit=PerformanceCourse.Loadout(item.loadout);car.Setup(race,0,EquipmentCatalog.Stats(fit),false,1709,item.loadout);
            Physics.SyncTransforms();
            if(car.ground!=null) {
                car.ground.Sample();if(!car.ground.hasSupport)throw new InvalidOperationException("No physical support at graded start.");
                car.body.position+=car.ground.normal*(.012f-car.ground.clearance);
            } else {var bounds=car.GetComponent<BoxCollider>().bounds;car.body.position+=Vector3.up*(.012f-bounds.min.y);}
            Physics.SyncTransforms();
            if(!manual)car.commandSource=Commands;car.controlsBlocked=true;race.cars.Add(car);race.running=true;
            result=new Result {id=$"{caseIndex+1:000}-{item.course}-{item.loadout.Replace('+','_')}-r{item.repeat+1}",course=item.course,loadout=item.loadout,repeat=item.repeat+1,
                fixtureVersion=course.GeometryVersion,driver=manual?"human":course.HasTurn?PerformanceCourse.ReferencePolicy:course.IsCoast?"matched coast v1":"scripted maneuver v1",utc=DateTime.UtcNow.ToString("O"),
                assemblyVersion=typeof(RoadCar).Assembly.ManifestModule.ModuleVersionId.ToString(),physicsStep=Time.fixedDeltaTime,simulationScale=Time.timeScale,
                tuning=car.tuning,fitted=fit.Select(p=>new FittedPart{slot=p.Key,id=p.Value}).ToArray(),straightLength=course.straight,turnRadius=course.radius,
                predictedTopSpeed=car.tuning.AttainableSpeed,predictedZeroTo30=PartsBrowser.Sprint(car.tuning),routeLength=course.finish-course.start,
                startHeight=course.track.At(course.start).y,entrySpeed=course.IsCoast?12:course.id=="brake"?20:0};
            samples.Clear();sectors.Clear();nextGate=steps=0;elapsed=warmup=sampleAt=travelled=sectorStart=peak=maxOffset=maxLateral=maxClearance=0;minApex=float.MaxValue;
            distanceSum=0;running=false;settling=true;Follow(true);RefreshHUD("Settling at the start");
        }
        Vector2 Commands(RoadCar driver)
        {
            if(!running)return Vector2.zero;
            // Small physical contact yaw would otherwise accumulate into metres
            // of drift over 900 m. Use the same centring policy for every setup;
            // it requests steering rather than correcting pose or velocity.
            if(course.id=="brake")return new Vector2(driver.speed>.12f?-1:0,course.CentreSteering(driver));
            if(course.IsCoast)return new Vector2(0,course.CentreSteering(driver));
            if(course.id=="straight"||course.id=="hill")return new Vector2(1,course.CentreSteering(driver));
            return course.ReferenceCommands(driver);
        }
        void FixedUpdate()
        {
            if(car==null)return;
            if(car.ground!=null)car.ground.Sample();
            if(settling) {
                warmup+=Time.fixedDeltaTime;if(warmup<.5f)return;
                settling=false;running=true;car.controlsBlocked=false;
                if(course.id=="brake"){car.body.linearVelocity=car.transform.forward*20;result.brakeEntrySpeed=20;}
                if(course.IsCoast)car.body.linearVelocity=car.DriveForward*result.entrySpeed;
                previousPosition=car.body.position;previousSpeed=Vector3.Dot(car.body.linearVelocity,car.DriveForward);previousRoute=course.start;peak=previousSpeed;
                Record(0,course.start,previousSpeed,0);Debug.Log("PERFORMANCE_RUN_START "+result.id);
                return;
            }
            if(!running)return;
            float dt=Time.fixedDeltaTime;steps++;elapsed=steps*dt;
            float speed=Vector3.Dot(car.body.linearVelocity,car.DriveForward),route=course.track.Nearest(car.body.position,out float offset);
            distanceSum+=Vector3.Distance(car.body.position,previousPosition);travelled=(float)distanceSum;peak=Mathf.Max(peak,speed);maxOffset=Mathf.Max(maxOffset,Mathf.Abs(offset));
            maxLateral=Mathf.Max(maxLateral,Mathf.Abs(Vector3.Dot(car.body.linearVelocity,car.transform.right)));
            float clearance=car.ground==null?car.GetComponent<BoxCollider>().bounds.min.y:car.ground.hasSupport?car.ground.clearance:1;
            maxClearance=Mathf.Max(maxClearance,clearance);
            result.peakHeight=Mathf.Max(result.peakHeight,course.track.At(route).y);
            if(car.ground!=null){if(!car.ground.grounded)result.airborneSteps++;result.maxAlignmentDegrees=Mathf.Max(result.maxAlignmentDegrees,car.ground.alignmentDegrees);}
            if(course.id!="brake") {
                if(result.zeroTo30<0&&speed>=30/2.237f)result.zeroTo30=CrossTime(previousSpeed,speed,30/2.237f,elapsed,dt);
                if(result.zeroTo50<0&&speed>=50/2.237f)result.zeroTo50=CrossTime(previousSpeed,speed,50/2.237f,elapsed,dt);
                while(nextGate<course.gates.Length&&route>=course.gates[nextGate]&&previousRoute<course.gates[nextGate]) {
                    float crossing=CrossTime(previousRoute,route,course.gates[nextGate],elapsed,dt);
                    sectors.Add(new Sector{name=course.sectors[nextGate],endRouteDistance=course.gates[nextGate]-course.start,time=crossing-sectorStart,elapsed=crossing,speed=speed});
                    sectorStart=crossing;nextGate++;
                }
            }
            if(course.HasTurn&&route>=course.turnStart&&route<=course.turnEnd)minApex=Mathf.Min(minApex,speed);
            if(course.HasTurn&&previousRoute<course.turnEnd&&route>=course.turnEnd)result.turnExitSpeed=speed;
            if(RaceTrial.Has("--performance-captures")&&result.repeat==1&&course.HasTurn&&previousRoute<(course.turnStart+course.turnEnd)*.5f&&route>=(course.turnStart+course.turnEnd)*.5f){capturePending=true;captureName=result.id+"-apex";}
            if(RaceTrial.Has("--performance-captures")&&result.repeat==1&&course.track.hasElevation&&course.track.At(previousRoute).z<380&&course.track.At(route).z>=380){capturePending=true;captureName=result.id+"-crest";}
            if(elapsed>=sampleAt){Record(offset,route,speed,(speed-previousSpeed)/dt);sampleAt+=.1f;}
            previousPosition=car.body.position;previousSpeed=speed;previousRoute=route;
            if(Mathf.Abs(offset)>Circuit.Width*.5f){Finish(false,"Left the road: centre outside measured width");return;}
            if(car.recoveries>0){Finish(false,"Recovery invalidated measured run");return;}
            if(course.id=="brake"&&speed<=.15f){result.stopDistance=travelled;result.stopTime=elapsed;Finish(true,null);return;}
            if(course.id!="brake"&&nextGate==course.gates.Length){Finish(true,null);return;}
            if(elapsed>(manual?600:90))Finish(false,"Timed out before completion");
        }
        static float CrossTime(float before,float after,float target,float time,float dt)=>time-dt+dt*Mathf.Clamp01((target-before)/Mathf.Max(.000001f,after-before));
        void Record(float offset,float route,float speed,float acceleration)
        {
            Vector3 p=car.body.position;samples.Add(new Sample{time=elapsed,routeDistance=route-course.start,travelled=travelled,x=p.x,y=p.y,z=p.z,speed=speed,offset=offset,
                throttle=car.throttle,steering=car.steering,targetSpeed=car.targetSpeed,longitudinalAcceleration=acceleration,
                lateralVelocity=Vector3.Dot(car.body.linearVelocity,car.transform.right),requestedYaw=car.requestedYaw,actualYaw=car.body.angularVelocity.y});
            var sample=samples[^1];sample.routeHeight=course.track.At(route).y;sample.grade=course.track.Forward(route).y;
            if(car.ground!=null){sample.grounded=car.ground.grounded;sample.groundClearance=car.ground.hasSupport?car.ground.clearance:1;sample.alignmentDegrees=car.ground.alignmentDegrees;}
            sample.expectedCoastAcceleration=-DriveModel.Resistance(car.tuning,speed)+Vector3.Dot(Physics.gravity,car.DriveForward);
        }
        void Finish(bool complete,string failure)
        {
            running=false;car.controlsBlocked=true;result.completed=complete;result.valid=complete&&car.contacts==0&&car.recoveries==0;result.failure=failure??(car.contacts>0?"Contact during controlled run":null);
            result.time=elapsed;result.steps=steps;result.travelledDistance=travelled;result.averageSpeed=travelled/elapsed;
            result.routeAverageSpeed=course.id=="brake"?-1:(previousRoute-course.start)/elapsed;
            result.peakSpeed=peak;result.maxAbsOffset=maxOffset;result.maxLateralVelocity=maxLateral;result.maxAirClearance=maxClearance;
            result.contacts=car.contacts;result.recoveries=car.recoveries;result.missedGates=course.id=="brake"?0:course.gates.Length-nextGate;
            result.apexMinSpeed=minApex==float.MaxValue?-1:minApex;result.sectors=sectors.ToArray();result.samples=samples.ToArray();
            result.finalSpeed=previousSpeed;result.endHeight=course.track.At(previousRoute).y;result.groundQuerySaturations=car.ground==null?0:car.ground.querySaturations;
            var window=samples.Where(s=>s.time>=elapsed-3).ToArray();
            if(course.id=="straight"&&window.Length>=28&&window.Max(s=>s.speed)-window.Min(s=>s.speed)<.12f&&Mathf.Abs(window[^1].speed-window[0].speed)/(window[^1].time-window[0].time)<.03f) {
                result.sustainedSpeedReached=true;result.sustainedTopSpeed=window.Average(s=>s.speed);
            }
            File.WriteAllText(Path.Combine(output,result.id+".json"),JsonUtility.ToJson(result,true));WriteCSV(result);results.Add(result);SaveBatch(caseIndex==cases.Count-1);
            Debug.Log($"PERFORMANCE_RUN_RESULT {result.id} valid={result.valid} seconds={elapsed:0.000} peak={peak:0.000} offset={maxOffset:0.000}");
            RefreshHUD(result.valid?"Run complete":"INVALID: "+result.failure);
            if(RaceTrial.Has("--performance-captures")&&result.repeat==1){capturePending=true;captureName=result.id+"-finish";}
            nextCaseAt=Time.unscaledTime+.3f;
        }
        void Update()
        {
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){if(running)Finish(false,"Aborted by user");Application.Quit();return;}
            if(car!=null&&(running||settling))RefreshHUD(settling?"Preparing":"Measuring");
            if(car!=null&&!running&&!settling&&!manual&&Time.unscaledTime>=nextCaseAt)BeginNext();
        }
        void LateUpdate(){if(car==null)return;Follow(false);if(capturePending){capturePending=false;ui.Capture(Path.Combine(output,captureName+".png"));}}
        void Follow(bool snap)
        {
            Vector3 desired=car.transform.position-car.transform.forward*7+Vector3.up*4;
            cameraView.transform.position=snap?desired:Vector3.Lerp(cameraView.transform.position,desired,1-Mathf.Exp(-Time.deltaTime*9));
            cameraView.transform.LookAt(car.transform.position+car.transform.forward*2+Vector3.up*.7f);
        }
        void RefreshHUD(string status)
        {
            ui.HUD($"PROVING GROUNDS / {course.id.ToUpperInvariant()}  ·  {result.loadout}  ·  repeat {result.repeat}  ·  {status}\n"+
                $"{elapsed:0.00} s   /   {Mathf.Abs(Vector3.Dot(car.body.linearVelocity,car.transform.forward))*2.237f:0.0} mph   /   {travelled:0.0} m   /   {caseIndex+1} of {cases.Count}",
                manual?"Drive: WASD / arrows or controller     Esc: end test":"Controlled equipment test · actual vehicle physics · dry tarmac · Esc: abort");
        }
        Material Material(string name,Color color,float smooth=.25f){var material=TrialWorld.Material(name,color,0,smooth);materials.Add(material);return material;}
        void Ribbon(string name,float center,float width,float height,Material material)
        {
            var mesh=course.track.Ribbon(center,width,height);meshes.Add(mesh);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(scenery.transform);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        void Box(string name,Vector3 p,Vector3 size,Material material,bool solid=false,Quaternion? rotation=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(scenery.transform);go.transform.position=p;go.transform.localScale=size;
            if(rotation.HasValue)go.transform.rotation=rotation.Value;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());}
        }
        void BuildScenery()
        {
            scenery=new GameObject("Measured "+course.id+" course");
            var grass=Material("Proving ground turf",new Color(.32f,.42f,.28f));var asphalt=Material("Dry proving asphalt",new Color(.20f,.23f,.25f));
            var white=Material("Boundary paint",new Color(.91f,.91f,.83f));var amber=Material("Timing stripe",new Color(.94f,.61f,.18f));
            Box("Continuous flat ground",new Vector3(course.radius,course.track.hasElevation?-1.5f:-.5f,course.straight*.5f),new Vector3(2*course.radius+260,1,course.straight+2*course.radius+260),grass,true);
            if(course.track.hasElevation) {
                var support=course.track.Ribbon(0,28,0);meshes.Add(support);
                var go=new GameObject("Surveyed physical grade",typeof(MeshCollider),typeof(RoadSupport));go.transform.SetParent(scenery.transform);go.GetComponent<MeshCollider>().sharedMesh=support;
                Ribbon("Graded shoulders",0,28,.001f,grass);
            }
            Ribbon("Asphalt",0,Circuit.Width,.022f,asphalt);Ribbon("Left boundary",-7.7f,.15f,.025f,white);Ribbon("Right boundary",7.7f,.15f,.025f,white);
            foreach(float gate in course.gates.Concat(new[]{course.start})) {
                var position=course.track.At(gate);var rotation=Quaternion.LookRotation(course.track.Forward(gate));
                Box("Measured timing stripe",position+Vector3.up*.028f,new Vector3(16,.006f,.20f),amber,false,rotation);
                foreach(int side in new[]{-1,1})Box("Timing marker",position+course.track.Right(gate)*side*9+Vector3.up*.5f,new Vector3(.25f,1,.25f),amber);
            }
            if(course.HasTurn)for(float d=course.turnStart;d<=course.turnEnd;d+=4)
                Box("Turn kerb",course.track.At(d)+course.track.Right(d)*7.95f+Vector3.up*.025f,new Vector3(.65f,.012f,2),white,false,Quaternion.LookRotation(course.track.Forward(d)));
        }
        void SaveProfile()
        {
            string path=Path.Combine(output,course.id+"-profile.json");if(File.Exists(path))return;
            var survey=new List<ProfilePoint>();for(float d=course.start;d<=course.finish;d+=5)survey.Add(new ProfilePoint{distance=d-course.start,position=course.track.At(d),height=course.track.At(d).y,grade=course.track.Forward(d).y,turnCurvature=course.track.Curvature(d)});
            File.WriteAllText(path,JsonUtility.ToJson(new Profile{course=course.id,version=course.GeometryVersion,start=course.start,finish=course.finish,samples=survey.ToArray()},true));
        }
        void WriteCSV(Result r)
        {
            using var writer=new StreamWriter(Path.Combine(output,r.id+".csv"));writer.WriteLine("time_s,route_m,travelled_m,speed_mps,offset_m,throttle,steering,accel_mps2,lateral_mps,target_mps,x,y,z,route_height_m,grade_sine,ground_clearance_m,alignment_deg,grounded,expected_coast_accel_mps2");
            foreach(var s in r.samples)writer.WriteLine(string.Join(",",new[]{s.time,s.routeDistance,s.travelled,s.speed,s.offset,s.throttle,s.steering,s.longitudinalAcceleration,s.lateralVelocity,s.targetSpeed,s.x,s.y,s.z,s.routeHeight,s.grade,s.groundClearance,s.alignmentDegrees,s.grounded?1:0,s.expectedCoastAcceleration}.Select(v=>v.ToString("R",Invariant))));
        }
        void SaveBatch(bool complete)=>File.WriteAllText(Path.Combine(output,"batch.json"),JsonUtility.ToJson(new Batch{complete=complete,unity=Application.unityVersion,graphics=SystemInfo.graphicsDeviceName,utc=DateTime.UtcNow.ToString("O"),runs=results.ToArray()},true));
        void ClearCase()
        {
            running=settling=false;race.running=false;race.cars.Clear();
            if(car!=null){var physicsMaterial=car.GetComponent<Collider>().sharedMaterial;Destroy(car.gameObject);if(physicsMaterial!=null)Destroy(physicsMaterial);car=null;}
            if(scenery!=null)Destroy(scenery);foreach(var material in materials)if(material!=null)Destroy(material);materials.Clear();foreach(var mesh in meshes)Destroy(mesh);meshes.Clear();
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;Time.timeScale=1;}
    }
}

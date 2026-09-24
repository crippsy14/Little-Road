using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LittleRoad;
using UnityEngine;

// Diagnostic full-gate executions of FIXED candidate curves. This uses the
// shared flat-road prediction model, not Unity contacts or runtime replanning.
public static class DynamicRejoinWitness
{
    public static void Export(){Run();}
    [Serializable] public sealed class Row
    {
        public string loadout,entry;public int rank;
        public float start,gate,blend,estimatedSeconds,executedSeconds,exitSpeed,maximumBodyOffset;
        public bool finished,contained;
    }
    [Serializable] public sealed class Report
    {
        public string scope="Fixed-curve planar model executions, 120Hz; no traffic, scenery, Rigidbody contacts or replanning. Runtime paired trials remain independently required.";
        public List<Row> rows=new List<Row>();
    }
    public static List<string> Run()
    {
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var report=new Report();const float dt=1f/120;
        foreach(string loadout in new[]{"street","club"})foreach(string entry in new[]{"left","middle","right","deep"}) {
            var tuning=loadout=="street"?CarTuning.Street:CarTuning.Club;
            var profile=new RacingSpeedProfile(line,tuning);var planner=new DynamicRejoinPlanner(profile);
            float start=course.regionStarts[1]+(entry=="deep"?15:-35);
            float side=entry=="left"?-4:entry=="middle"?0:4;
            var forward=road.Forward(start);var position=road.At(start)+road.Right(start)*side;
            var self=new ObservedCar(0,position,forward*20,forward,road.Right(start),start,side,20,1.2f,2.3f);
            var initial=new DriverObservation(0,dt,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false);
            ContextPathState previous=default;planner.Prepare(in initial,in previous,false);
            if(start+planner.Horizon<course.regionStarts[3]+25)throw new Exception("Dynamic planning gate ends before the fixture corner exit");
            bool late=false;for(int rank=0;rank<planner.Count;rank++)late|=start+planner.Path(rank).blend>=course.regionStarts[3];
            if(!late)throw new Exception("No candidate can rejoin after the corner apex/exit");
            for(int rank=0;rank<planner.Count;rank++) {
                var path=planner.Path(rank);var row=new Row{loadout=loadout,entry=entry,rank=rank,start=start,gate=start+planner.Horizon,blend=path.blend,
                    estimatedSeconds=float.IsFinite(planner.EstimatedTime(rank))?planner.EstimatedTime(rank):-1};
                var model=new PlanarDrivePrediction(position,forward*20,forward,0,0);var follower=new ContextPathFollower(profile,false);
                float travelled=0,last=start;
                for(int step=0;step<3600;step++) {
                    float route=road.Nearest(model.position,out float offset);
                    float gain=Mathf.Repeat(route-last+road.Length*.5f,road.Length)-road.Length*.5f;
                    travelled+=gain;path.consumed+=Mathf.Max(0,gain);last=route;
                    float speed=Vector3.Dot(model.velocity,model.forward);
                    row.maximumBodyOffset=Mathf.Max(row.maximumBodyOffset,model.MaxBodyOffset(road,Vector3.zero,new Vector3(1.2f,.5f,2.3f)));
                    if(travelled>=planner.Horizon){row.finished=true;row.executedSeconds=step*dt;row.exitSpeed=speed;break;}
                    var live=new ObservedCar(0,model.position,model.velocity,model.forward,Vector3.Cross(Vector3.up,model.forward),route,offset,speed,1.2f,2.3f,model.yawRate);
                    var o=new DriverObservation(step*dt,dt,live,Array.Empty<ObservedCar>(),road,tuning,1,0,false,model.steering);
                    var command=follower.Follow(in o,in path);model.Step(tuning,command.pedal,command.steering,dt);
                }
                row.contained=row.finished&&row.maximumBodyOffset<=Circuit.Width*.5f-.1f;report.rows.Add(row);
            }
        }
        string root=Directory.GetParent(Application.dataPath).Parent.FullName;
        string output=RaceTrial.Arg("--dynamic-witness-output");
        bool immutable=output!=null;
        if(output==null) {
            string export=RaceTrial.Arg("--review-export")??"";
            var version=Regex.Match(export,@"little-road-context-(\d+)-review");
            immutable=version.Success;
            output=version.Success?Path.Combine(root,$"review/unity-35/dynamic-rejoin-{version.Groups[1].Value}-witness.json")
                :Path.Combine(root,"review/unity-01/dynamic-rejoin-witness.json");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        // Versioned evidence is append-only. Ordinary unversioned editor checks
        // may replace only the explicitly current unity-01 report.
        using(var stream=new FileStream(output,immutable?FileMode.CreateNew:FileMode.Create,FileAccess.Write))
        using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(JsonUtility.ToJson(report,true));
        return new List<string>{"Corner-aware ranking gates extend beyond the measured exit for both loadouts and all four entries",
            "Every initial corner candidate family contains a rejoin after the apex/exit; fixed-curve model witness results exported separately"};
    }
}

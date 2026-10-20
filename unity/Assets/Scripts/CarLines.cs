using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // A racing line for each car (docs/71 phase 2; Richard, 8 Oct: "a racing line is generated for each car based off
    // of their performance as the race loads"); on by default. Cars are grouped by performance (grip, brakes, top
    // speed, power, aero; rounded so near-identical cars share a line). Each group's line starts from the track's
    // shared line and moves ~8 m control points sideways, within +-5.4 m, keeping every move that cuts that car's own
    // lap time (a point-mass lap: corner speed from grip and aero, braking and power through the friction circle),
    // with a small smoothness term so the line stays drivable. Lines are cached on disk by track, group and version.
    public static class CarLines
    {
        // The default since 8 Oct (study 223: -33 s over six races, Bayview -1.1%, no resets); --no-car-lines for the shared line.
        public static bool On=>!RaceTrial.Has("--no-car-lines");
        public static float BoundFor(Circuit road)=>road.width>=Circuit.Width?5.4f:road.width*.5f-1.8f;   // (5.4 m on a standard 16 m circuit)
       public const string Version="car-line-v1";
        const float Spacing=8,Smooth=.001f,Use=.9f;
        static readonly Dictionary<string,RacingLine> lines=new();
        static float Round(float v,float step)=>Mathf.Round(v/step)*step;
        // Coarse enough that a mixed field makes a handful of groups: a line hardly changes for a car a little grippier or
        // a little stronger.
        public static string Group(CarTuning t)=>string.Format(CultureInfo.InvariantCulture,"g{0:0}-b{1:0}-v{2:0}-p{3:0}-a{4:0.0}",
            Round(t.roadGrip,1),Round(t.braking,3),Round(t.AttainableSpeed,4),Round(DriveModel.EngineForce(t,25),1),Round(t.downforce,.1f));
        static string Key(Circuit road,RacingLine shared,CarTuning t)=>shared.data.id+"|"+RacingLine.BayviewGeometryHash(road).Substring(0,12)+"|"+Group(t)+"|"+Version;
        static string FileOf(string key)=>Path.Combine(Application.persistentDataPath,"racing-lines",Sanitise(key)+".json");

        // Before the cars spawn: every group the field needs, from disk where cached, the rest baked in parallel (a few
        // hundred ms to ~1.5 s each, one core each), then cached.
        public static void Prepare(Circuit road,RacingLine shared,IEnumerable<CarTuning> field)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();var todo=new Dictionary<string,CarTuning>();int groups=0,fromDisk=0;
            foreach(var t in field){string key=Key(road,shared,t);if(lines.ContainsKey(key)||todo.ContainsKey(key))continue;groups++;
                string file=FileOf(key);
                try{if(File.Exists(file)){lines[key]=new RacingLine(road,JsonUtility.FromJson<RacingLine.Data>(File.ReadAllText(file)));fromDisk++;continue;}}catch(Exception e){Debug.LogWarning("Car line cache ignored: "+e.Message);}
                todo[key]=t;}
            var keys=new List<string>(todo.Keys);var baked=new RacingLine.Data[keys.Count];var laps=new (float before,float after)[keys.Count];
            // (Static settings that read the command line are initialised here, on the main thread, before the workers.)
            RacerDriverPolicy.CornerSpeed(1,1,.01f);foreach(var t in todo.Values)DriveModel.Aero(t);
            System.Threading.Tasks.Parallel.For(0,keys.Count,i=>{var t=todo[keys[i]];baked[i]=Bake(road,shared,t,shared.data.id+"-"+Group(t),out float b,out float a);laps[i]=(b,a);});
            for(int i=0;i<keys.Count;i++) {
                // A line that fails its own checks never stops a race: that group drives the shared line.
                try{lines[keys[i]]=new RacingLine(road,baked[i]);}
                catch(Exception e){Debug.LogWarning($"LITTLE_ROAD_CAR_LINE rejected group={Group(todo[keys[i]])}: {e.Message}; shared line used");lines[keys[i]]=shared;continue;}
                try{var file=FileOf(keys[i]);Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,JsonUtility.ToJson(baked[i]));}catch(Exception e){Debug.LogWarning("Car line not cached: "+e.Message);}
                Debug.Log($"LITTLE_ROAD_CAR_LINE baked group={Group(todo[keys[i]])} lap {laps[i].before:0.00}s -> {laps[i].after:0.00}s");
            }
            Debug.Log($"LITTLE_ROAD_CAR_LINES groups={groups} cached={fromDisk} baked={keys.Count} ms={watch.ElapsedMilliseconds}");
        }

        // The car's line on this road, from memory, then disk, then baked.
        public static RacingLine For(Circuit road,RacingLine shared,CarTuning tuning)
        {
            string key=Key(road,shared,tuning);
            if(lines.TryGetValue(key,out var line))return line;
            string file=FileOf(key);
            try{if(File.Exists(file)){line=new RacingLine(road,JsonUtility.FromJson<RacingLine.Data>(File.ReadAllText(file)));lines[key]=line;Debug.Log($"LITTLE_ROAD_CAR_LINE cached group={Group(tuning)}");return line;}}
            catch(Exception e){Debug.LogWarning("Car line cache ignored: "+e.Message);}
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var data=Bake(road,shared,tuning,shared.data.id+"-"+Group(tuning),out float before,out float after);
            try{line=new RacingLine(road,data);}catch(Exception e){Debug.LogWarning($"LITTLE_ROAD_CAR_LINE rejected group={Group(tuning)}: {e.Message}; shared line used");lines[key]=shared;return shared;}
            lines[key]=line;
            Debug.Log($"LITTLE_ROAD_CAR_LINE baked group={Group(tuning)} lap {before:0.00}s -> {after:0.00}s ms={watch.ElapsedMilliseconds}");
            try{Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,JsonUtility.ToJson(data));}catch(Exception e){Debug.LogWarning("Car line not cached: "+e.Message);}
            return line;
        }
        static string Sanitise(string key){var chars=key.ToCharArray();for(int i=0;i<chars.Length;i++)if(!char.IsLetterOrDigit(chars[i])&&chars[i]!='-'&&chars[i]!='.')chars[i]='_';return new string(chars);}

        public static RacingLine.Data Bake(Circuit road,RacingLine shared,CarTuning tuning,string id,out float before,out float after)
        {float Bound=BoundFor(road);
            float length=road.length;int n=Mathf.CeilToInt(length/Spacing);
            var centre=new Vector2[n];var normal=new Vector2[n];var offset=new float[n];
            for(int i=0;i<n;i++){var p=road.At(i*length/n);centre[i]=new Vector2(p.x,p.z);}
            for(int i=0;i<n;i++){var f=(centre[(i+1)%n]-centre[(i+n-1)%n]).normalized;normal[i]=new Vector2(f.y,-f.x);}
            for(int i=0;i<n;i++)offset[i]=Mathf.Clamp(shared.Offset(i*length/n),-Bound,Bound);
            var lap=new Lap(tuning,n);
            float J(float[] o)=>lap.Time(centre,normal,o)+Smooth*Bending(centre,normal,o);
            before=lap.Time(centre,normal,offset);float best=J(offset);
            foreach(float step in new[]{1f,.5f,.25f,.1f})
                for(int pass=0;pass<8;pass++) {
                    bool improved=false;
                    for(int i=0;i<n;i++)foreach(float sign in new[]{1f,-1f}) {
                        float old=offset[i],trial=Mathf.Clamp(old+sign*step,-Bound,Bound);if(trial==old)continue;
                        offset[i]=trial;float j=J(offset);
                        if(j<best-1e-5f){best=j;improved=true;break;}
                        offset[i]=old;
                    }
                    if(!improved)break;
                }
            after=lap.Time(centre,normal,offset);
            var path=new Vector2[n];for(int i=0;i<n;i++)path[i]=centre[i]+normal[i]*offset[i];
            return RacingLine.Sample(road,path,id,shared.data.trackId,"per-car minimum-time line ("+Group(tuning)+"), not validated ideal",
                $"{Version}: coordinate descent on {n} controls at ~{Spacing}m, +-{Bound}m, smoothness {Smooth}, corner use {Use}; periodic cubic B-spline");
        }
        static float Bending(Vector2[] c,Vector2[] nrm,float[] o)
        {
            int n=c.Length;float sum=0;
            for(int i=0;i<n;i++){int a=(i+n-1)%n,b=(i+1)%n;var d=(c[a]+nrm[a]*o[a])-2*(c[i]+nrm[i]*o[i])+(c[b]+nrm[b]*o[b]);sum+=d.sqrMagnitude;}
            return sum;
        }

        // A point-mass lap of a closed polyline: corner speed from grip (with aero) at each control, then backward braking
        // and forward drive passes over two laps through the friction circle. The same physics as the AI's speed plan.
        sealed class Lap
        {
            readonly CarTuning t;readonly float[] k,seg,v,limit;readonly float budget,aero,top;
            public Lap(CarTuning tuning,int n){t=tuning;k=new float[n];seg=new float[n];v=new float[n];limit=new float[n];budget=tuning.roadGrip*1.65f;aero=DriveModel.Aero(tuning);top=tuning.AttainableSpeed;}
            float Grip(float speed)=>budget*(1+aero*speed*speed);
            public float Time(Vector2[] c,Vector2[] nrm,float[] o)
            {
                int n=c.Length;
                for(int i=0;i<n;i++) {
                    int a=(i+n-1)%n,b=(i+1)%n;Vector2 pa=c[a]+nrm[a]*o[a],p=c[i]+nrm[i]*o[i],pb=c[b]+nrm[b]*o[b];
                    var u=p-pa;var w=pb-p;float cross=u.x*w.y-u.y*w.x;k[i]=2*Mathf.Abs(cross)/Mathf.Max(.001f,u.magnitude*w.magnitude*(pa-pb).magnitude);seg[i]=w.magnitude;
                    limit[i]=Mathf.Min(top,RacerDriverPolicy.CornerSpeed(budget,Use,k[i],aero));v[i]=limit[i];
                }
                for(int pass=0;pass<2;pass++) {
                    for(int j=2*n-1;j>=0;j--){int i=j%n,b=(i+1)%n;float lat=v[b]*v[b]*k[i],g=Grip(v[b]),trac=Mathf.Sqrt(Mathf.Max(0,g*g-lat*lat));
                        float dec=Mathf.Min(t.braking,trac)+DriveModel.Resistance(t,v[b]);v[i]=Mathf.Min(v[i],Mathf.Sqrt(v[b]*v[b]+2*dec*seg[i]));}
                    for(int j=0;j<2*n;j++){int i=j%n,b=(i+1)%n;float lat=v[i]*v[i]*k[i],g=Grip(v[i]),trac=Mathf.Sqrt(Mathf.Max(0,g*g-lat*lat));
                        float acc=(v[i]>=t.gearCeiling?0:Mathf.Min(DriveModel.EngineForce(t,v[i]),trac))-DriveModel.Resistance(t,v[i]);
                        v[b]=Mathf.Min(v[b],Mathf.Sqrt(Mathf.Max(.01f,v[i]*v[i]+2*acc*seg[i])));}
                }
                float time=0;for(int i=0;i<n;i++)time+=2*seg[i]/Mathf.Max(.1f,v[i]+v[(i+1)%n]);
                return time;
            }
        }
    }
}

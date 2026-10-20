using System;
using System.Security.Cryptography;
using UnityEngine;

namespace LittleRoad
{
    public sealed class RacingLine
    {
        [Serializable] public sealed class Data {public int schema;public string id,trackId,trackVersion,trackHash,status,method;public float roadLength;public float[] offsets;public Vector3[] positions;}
        public readonly Data data;
        public readonly Vector3[] points;
        public readonly float[] segmentLength,curvature;
        public readonly float roadLength,roadStep,pathLength;
        public int Count=>points.Length;
        // The widest a line may sit from the road centre: the AI's usable edge (RacerDriverPolicy.Edge, docs/71).
        // (5.6 m on a standard 16 m circuit, as it always was; on a narrower one, 1.6 m inside its edge: a car's half-width and 0.7 m.)
        public static float MaxOffsetFor(Circuit road)=>road.width>=Circuit.Width?5.6f:road.width*.5f-1.6f;
        public readonly float MaxOffset,halfRoad;
        public RacingLine(Circuit road,string id,string trackId,string trackVersion):this(road,Load(road,id,trackId,trackVersion)){}
        static Data Load(Circuit road,string id,string trackId,string trackVersion)
        {
            var asset=Resources.Load<TextAsset>("RacingLines/"+id);if(asset==null)throw new ArgumentException("Missing racing line: "+id);
            var data=JsonUtility.FromJson<Data>(asset.text);
            var geometry=Resources.Load<TextAsset>("Tracks/cedar-technical");
            using var sha=SHA256.Create();string hash=trackId=="bayview"?BayviewGeometryHash(road):BitConverter.ToString(sha.ComputeHash(geometry.bytes)).Replace("-","");
            if((data.schema!=1&&data.schema!=2)||data.trackId!=trackId||data.trackVersion!=trackVersion||data.trackHash!=hash||Mathf.Abs(data.roadLength-road.length)>.05f||data.offsets==null||data.offsets.Length<10)
                throw new ArgumentException("Stale or incompatible racing line: "+id);
            if(data.schema==2&&(data.positions==null||data.positions.Length!=data.offsets.Length))throw new ArgumentException("Missing Cartesian racing-line samples: "+id);
            return data;
        }
        public RacingLine(Circuit road,Data baked)
        {
            data=baked;MaxOffset=MaxOffsetFor(road);halfRoad=road.Half;
            roadLength=road.length;roadStep=road.length/data.offsets.Length;points=new Vector3[data.offsets.Length];segmentLength=new float[Count];curvature=new float[Count];
            for(int i=0;i<Count;i++) {
                if(float.IsNaN(data.offsets[i])||float.IsInfinity(data.offsets[i])||Mathf.Abs(data.offsets[i])>MaxOffset+.01f)throw new ArgumentException("Racing-line offset exceeds initial body/tracking corridor.");
                points[i]=data.schema==2?data.positions[i]:road.At(i*roadStep)+road.Right(i*roadStep)*data.offsets[i];
                if(!float.IsFinite(points[i].x)||!float.IsFinite(points[i].y)||!float.IsFinite(points[i].z))throw new ArgumentException("Non-finite racing-line point.");
                if(data.schema==2){float mapped=road.Nearest(points[i],out float side);float hintError=Mathf.Abs(Mathf.DeltaAngle(mapped/road.length*360,i*roadStep/road.length*360))*road.length/360;
                    if(Mathf.Abs(side)>MaxOffset+.01f||hintError>4||Mathf.Abs(points[i].y-road.At(mapped).y)>.01f)throw new ArgumentException("Cartesian racing-line sample left its local road corridor.");}
            }
            float length=0;
            for(int i=0;i<Count;i++) {
                var before=points[i]-points[(i+Count-1)%Count];var after=points[(i+1)%Count]-points[i];
                segmentLength[i]=after.magnitude;length+=segmentLength[i];
                curvature[i]=2*Vector3.Cross(before,after).y/Mathf.Max(.001f,before.magnitude*after.magnitude*(before+after).magnitude);
                if(segmentLength[i]<.1f||Vector3.Dot(after,road.Forward(i*roadStep))<=0)throw new ArgumentException("Reversed or degenerate racing-line segment.");
            }
            pathLength=length;
        }
        // The bake in tools/bake-racing-line-candidates.py (--smooth-only), run on the live road:
        // bounded bending-energy offsets at ~8 m controls, then a periodic C2 cubic B-spline sampled
        // every ~2 m. Heights follow the road. Used for circuits built at runtime (Hillcrest).
        public static Data BakeSmooth(Circuit road,string id,string trackId,float bound=4)
        {
            float length=road.length;int count=Mathf.CeilToInt(length/8);var centre=new Vector2[count];var normal=new Vector2[count];var offset=new float[count];
            for(int i=0;i<count;i++){var p=road.At(i*length/count);centre[i]=new Vector2(p.x,p.z);}
            for(int i=0;i<count;i++){var f=(centre[(i+1)%count]-centre[(i+count-1)%count]).normalized;normal[i]=new Vector2(f.y,-f.x);}
            var path=new Vector2[count];var second=new Vector2[count];
            // Cooked once (WorldCache, docs/72): the 24000 iterations took ~0.3 s for the Hillcrest Raceway on every start-up
            // and race entry. Keyed by the road's control points and the bound, so a changed road bakes afresh.
            ulong key=14695981039346656037;void Mix(float f){key=(key^(uint)System.BitConverter.SingleToInt32Bits(f))*1099511628211;}
            foreach(var c in centre){Mix(c.x);Mix(c.y);}Mix(bound);string cache="smooth-line-"+key.ToString("x16");
            if(!WorldCache.TryRead(cache,offset)) {
                for(int iteration=0;iteration<24000;iteration++) {
                    for(int i=0;i<count;i++)path[i]=centre[i]+normal[i]*offset[i];
                    for(int i=0;i<count;i++)second[i]=path[(i+count-1)%count]-2*path[i]+path[(i+1)%count];
                    for(int i=0;i<count;i++){var fourth=second[(i+count-1)%count]-2*second[i]+second[(i+1)%count];offset[i]=Mathf.Clamp(offset[i]-.025f*2*Vector2.Dot(normal[i],fourth),-bound,bound);}
                }
                WorldCache.Write(cache,offset);
            }
            for(int i=0;i<count;i++)path[i]=centre[i]+normal[i]*offset[i];
            return Sample(road,path,id,trackId,"runtime geometric seed, not validated ideal line","bounded bending-energy controls at ~8m;24000 iterations;"+bound+"m offset bound; periodic cubic B-spline");
        }
        // A closed path of control points (x, z) on the road, as a periodic cubic B-spline sampled every ~2 m, with heights
        // settled on the road: the bake's last step, shared by the smooth bake and the per-car lines (CarLines).
        public static Data Sample(Circuit road,Vector2[] path,string id,string trackId,string status,string method)
        {
            float length=road.length;int count=path.Length;
            int samples=Mathf.CeilToInt(length/2);var data=new Data{schema=2,id=id,trackId=trackId,trackVersion=trackId+"-runtime",status=status,
                method=method,roadLength=length,offsets=new float[samples],positions=new Vector3[samples]};
            for(int k=0;k<samples;k++) {
                float s=k*length/samples,coordinate=s/length*count;int i=Mathf.FloorToInt(coordinate);float t=coordinate-i;
                Vector2 P(int j)=>path[((j%count)+count)%count];
                var q=((1-t)*(1-t)*(1-t)*P(i-1)+(3*t*t*t-6*t*t+4)*P(i)+(-3*t*t*t+3*t*t+3*t+1)*P(i+1)+t*t*t*P(i+2))/6;
                // Settle the height on the road (Nearest is 3-D, as in the corridor check above).
                var at=new Vector3(q.x,road.At(s).y,q.y);for(int pass=0;pass<4;pass++)at.y=road.At(road.Nearest(at,out _)).y;
                float side0=Vector3.Dot(new Vector3(q.x-road.At(s).x,0,q.y-road.At(s).z),road.Right(s));
                // Kept inside the corridor (a per-car line can cut a tight bend past it): a point that sits too far out, or
                // whose nearest road point is somewhere else, is put back at its clamped offset across the road here.
                float mapped=road.Nearest(at,out float side);float along=Mathf.Abs(Mathf.Repeat(mapped-s+length*.5f,length)-length*.5f);
                if(Mathf.Abs(side)>MaxOffsetFor(road)-.05f||along>3) {
                    side0=Mathf.Clamp(side0,-(MaxOffsetFor(road)-.05f),MaxOffsetFor(road)-.05f);at=road.At(s)+road.Right(s)*side0;
                    for(int pass=0;pass<4;pass++)at.y=road.At(road.Nearest(at,out _)).y;
                }
                data.offsets[k]=Mathf.Clamp(side0,-MaxOffsetFor(road),MaxOffsetFor(road));data.positions[k]=at;
            }
            return data;
        }
        // An authored line's lateral offsets carried onto a circuit with the same layout (Cedar ->
        // Cedar Hills): positions are rebuilt from the new road, so heights follow its elevation.
        public static Data Transfer(Circuit road,string id,string trackId)
        {
            var asset=Resources.Load<TextAsset>("RacingLines/"+id);if(asset==null)throw new ArgumentException("Missing racing line: "+id);
            var source=JsonUtility.FromJson<Data>(asset.text);
            return new Data{schema=1,id=id+"-on-"+trackId,trackId=trackId,trackVersion=trackId+"-runtime",status="offsets transferred from "+id,method=source.method,roadLength=road.length,offsets=source.offsets,positions=null};
        }
        public static string BayviewGeometryHash(Circuit road)
        {
            using var bytes=new System.IO.MemoryStream();using var writer=new System.IO.BinaryWriter(bytes);
            writer.Write(road.points.Length);foreach(var p in road.points){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}writer.Flush();
            using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-","");
        }
        public int Index(float roadDistance,out float fraction)
        {float coordinate=Mathf.Repeat(roadDistance,roadLength)/roadStep;int i=Mathf.Min(Count-1,Mathf.FloorToInt(coordinate));fraction=coordinate-i;return i;}
        public Vector3 At(float roadDistance){int i=Index(roadDistance,out float f);return Vector3.Lerp(points[i],points[(i+1)%Count],f);}
        public float Offset(float roadDistance){int i=Index(roadDistance,out float f);return Mathf.Lerp(data.offsets[i],data.offsets[(i+1)%Count],f);}
        public float Project(Vector3 position,float roadHint,out float error)
        {
            int centre=Index(roadHint,out _);float best=float.MaxValue,result=roadHint;error=0;
            // Local progress hint prevents a jump to an adjacent track branch.
            for(int j=-16;j<=16;j++) {
                int i=(centre+j+Count)%Count;var a=points[i];var edge=points[(i+1)%Count]-a;
                float f=Mathf.Clamp01(Vector3.Dot(position-a,edge)/edge.sqrMagnitude);var point=a+edge*f;float sq=(position-point).sqrMagnitude;
                if(sq<best){best=sq;result=(i+f)*roadStep;error=Vector3.Dot(position-point,Vector3.Cross(Vector3.up,edge.normalized));}
            }
            return Mathf.Repeat(result,roadLength);
        }
    }
    public sealed class RacingSpeedProfile
    {
        public readonly RacingLine line;
        public readonly float[] speed,acceleration;
        public readonly float predictedLapTime;
        public readonly int iterations;
        public readonly DriverPlanningSettings planning;
        public RacingSpeedProfile(RacingLine line,CarTuning tuning,DriverPlanningSettings planning=default,bool drafting=false)
        {
            this.planning=planning;
            this.line=line;int n=line.Count;speed=new float[n];acceleration=new float[n];
            float budget=tuning.roadGrip*1.65f;
            // Grade-aware: per-point grade (gravity along the line) and vertical curvature (load on
            // crests/dips). Both are zero on a level line, so the profile is then unchanged.
            var grade=new float[n];var vertical=new float[n];bool graded=false;
            if(GradeModel.Enabled&&GradeModel.Physics) {
                for(int i=0;i<n;i++){int next=(i+1)%n;var d=line.points[next]-line.points[i];float run=new Vector2(d.x,d.z).magnitude;grade[i]=run>.01f?d.y/run:0;}
                for(int i=0;i<n;i++){int prev=(i+n-1)%n;float ds=Mathf.Max(.1f,(line.segmentLength[prev]+line.segmentLength[i])*.5f);vertical[i]=(grade[i]-grade[prev])/ds;if(grade[i]!=0)graded=true;}
            }
            float Grip(int i,float v)=>graded?budget*GradeModel.Load(v,vertical[i]):budget;
            float Gravity(int i)=>graded?GradeModel.Gravity*grade[i]/Mathf.Sqrt(1+grade[i]*grade[i]):0;
            for(int i=0;i<n;i++) {
                speed[i]=Mathf.Min(tuning.AttainableSpeed,Mathf.Sqrt(budget*PaceMargins.Corner/Mathf.Max(.00001f,Mathf.Abs(line.curvature[i]))));
                if(graded)for(int pass=0;pass<3;pass++)speed[i]=Mathf.Min(tuning.AttainableSpeed,Mathf.Sqrt(Grip(i,speed[i])*PaceMargins.Corner/Mathf.Max(.00001f,Mathf.Abs(line.curvature[i]))));
            }
            int iteration=0;
            for(;iteration<64;iteration++) {
                float change=0;
                for(int i=n-1;i>=0;i--) {
                    int next=(i+1)%n;float lateral=speed[next]*speed[next]*Mathf.Max(Mathf.Abs(line.curvature[i]),Mathf.Abs(line.curvature[next]));
                    float g=Grip(i,speed[next]);float traction=Mathf.Sqrt(Mathf.Max(0,g*g-lateral*lateral));
                    float braking=Mathf.Min(tuning.braking,traction)*PaceMargins.ProfileBrake*planning.ReferenceBrakeScale+DraftingModel.Resistance(speed[next],tuning.mass,drafting?DraftingModel.MaximumReduction:0,tuning.DragFactor);
                    if(graded)braking=Mathf.Max(.5f,braking+Gravity(i));
                    float value=Mathf.Min(speed[i],Mathf.Sqrt(speed[next]*speed[next]+2*braking*line.segmentLength[i]));change=Mathf.Max(change,speed[i]-value);speed[i]=value;
                }
                for(int i=0;i<n;i++) {
                    int next=(i+1)%n;float lateral=speed[i]*speed[i]*Mathf.Abs(line.curvature[i]);
                    float g=Grip(i,speed[i]);float traction=Mathf.Sqrt(Mathf.Max(0,g*g-lateral*lateral));
                    float force=speed[i]>=tuning.gearCeiling?0:Mathf.Min(DriveModel.EngineForce(tuning,speed[i]),traction);
                    float net=force-DriveModel.Resistance(tuning,speed[i])-Gravity(i);
                    float value=Mathf.Min(speed[next],Mathf.Sqrt(Mathf.Max(.01f,speed[i]*speed[i]+2*net*line.segmentLength[i])));change=Mathf.Max(change,speed[next]-value);speed[next]=value;
                }
                if(change<.0001f)break;
            }
            iterations=iteration+1;float time=0;
            for(int i=0;i<n;i++){int next=(i+1)%n;acceleration[i]=(speed[next]*speed[next]-speed[i]*speed[i])/(2*line.segmentLength[i]);time+=2*line.segmentLength[i]/Mathf.Max(.1f,speed[i]+speed[next]);}
            predictedLapTime=time;
        }
        public float Speed(float distance){int i=line.Index(distance,out float f);return Mathf.Lerp(speed[i],speed[(i+1)%line.Count],f);}
        public float Acceleration(float distance){int i=line.Index(distance,out float f);return Mathf.Lerp(acceleration[i],acceleration[(i+1)%line.Count],f);}
    }
}

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
        public RacingLine(Circuit road,string id,string trackId,string trackVersion)
        {
            var asset=Resources.Load<TextAsset>("RacingLines/"+id);if(asset==null)throw new ArgumentException("Missing racing line: "+id);
            data=JsonUtility.FromJson<Data>(asset.text);
            var geometry=Resources.Load<TextAsset>("Tracks/cedar-technical");
            using var sha=SHA256.Create();string hash=trackId=="bayview"?BayviewGeometryHash(road):BitConverter.ToString(sha.ComputeHash(geometry.bytes)).Replace("-","");
            if((data.schema!=1&&data.schema!=2)||data.trackId!=trackId||data.trackVersion!=trackVersion||data.trackHash!=hash||Mathf.Abs(data.roadLength-road.length)>.05f||data.offsets==null||data.offsets.Length<10)
                throw new ArgumentException("Stale or incompatible racing line: "+id);
            if(data.schema==2&&(data.positions==null||data.positions.Length!=data.offsets.Length))throw new ArgumentException("Missing Cartesian racing-line samples: "+id);
            roadLength=road.length;roadStep=road.length/data.offsets.Length;points=new Vector3[data.offsets.Length];segmentLength=new float[Count];curvature=new float[Count];
            for(int i=0;i<Count;i++) {
                if(float.IsNaN(data.offsets[i])||float.IsInfinity(data.offsets[i])||Mathf.Abs(data.offsets[i])>5.01f)throw new ArgumentException("Racing-line offset exceeds initial body/tracking corridor.");
                points[i]=data.schema==2?data.positions[i]:road.At(i*roadStep)+road.Right(i*roadStep)*data.offsets[i];
                if(!float.IsFinite(points[i].x)||!float.IsFinite(points[i].y)||!float.IsFinite(points[i].z))throw new ArgumentException("Non-finite racing-line point.");
                if(data.schema==2){float mapped=road.Nearest(points[i],out float side);float hintError=Mathf.Abs(Mathf.DeltaAngle(mapped/road.length*360,i*roadStep/road.length*360))*road.length/360;
                    if(Mathf.Abs(side)>5.01f||hintError>4||Mathf.Abs(points[i].y-road.At(mapped).y)>.01f)throw new ArgumentException("Cartesian racing-line sample left its local road corridor.");}
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
            for(int i=0;i<n;i++)speed[i]=Mathf.Min(tuning.AttainableSpeed,Mathf.Sqrt(budget*PaceMargins.Corner/Mathf.Max(.00001f,Mathf.Abs(line.curvature[i]))));
            int iteration=0;
            for(;iteration<64;iteration++) {
                float change=0;
                for(int i=n-1;i>=0;i--) {
                    int next=(i+1)%n;float lateral=speed[next]*speed[next]*Mathf.Max(Mathf.Abs(line.curvature[i]),Mathf.Abs(line.curvature[next]));
                    float traction=Mathf.Sqrt(Mathf.Max(0,budget*budget-lateral*lateral));
                    float braking=Mathf.Min(tuning.braking,traction)*PaceMargins.ProfileBrake*planning.ReferenceBrakeScale+DraftingModel.Resistance(speed[next],tuning.mass,drafting?DraftingModel.MaximumReduction:0);
                    float value=Mathf.Min(speed[i],Mathf.Sqrt(speed[next]*speed[next]+2*braking*line.segmentLength[i]));change=Mathf.Max(change,speed[i]-value);speed[i]=value;
                }
                for(int i=0;i<n;i++) {
                    int next=(i+1)%n;float lateral=speed[i]*speed[i]*Mathf.Abs(line.curvature[i]);
                    float traction=Mathf.Sqrt(Mathf.Max(0,budget*budget-lateral*lateral));
                    float force=speed[i]>=tuning.gearCeiling?0:Mathf.Min(DriveModel.EngineForce(tuning,speed[i]),traction);
                    float net=force-DriveModel.Resistance(speed[i],tuning.mass);
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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Exact straights and sampled circular turns. A closed return route keeps
    // production projection/pursuit working; each measured run has explicit
    // start/finish gates and never depends on traversing the unused return.
    public sealed class PerformanceCourse
    {
        public const string Version="dry-proving-grounds-v1";
        public const string ReferencePolicy="fixed reference policy v2";
        public readonly string id;
        public readonly Circuit track;
        public readonly float straight,radius,start=20,finish,turnStart,turnEnd;
        public bool HasTurn=>id=="uturn"||id=="combined";
        public bool IsCoast=>id.StartsWith("coast-");
        public string GeometryVersion=>track.hasElevation?"graded-proving-grounds-v1":Version;
        public readonly float[] gates;
        public readonly string[] sectors;
        public PerformanceCourse(string name)
        {
            bool graded=name=="hill"||name=="combined"||name=="coast-flat"||name=="coast-up"||name=="coast-down";
            if(name!="straight"&&name!="uturn"&&name!="brake"&&!graded)throw new ArgumentException("Unknown proving course: "+name);
            id=name;straight=name=="uturn"?220:1200;radius=HasTurn?22:45;
            var points=new List<Vector3>();int linear=Mathf.CeilToInt(straight/2),arc=Mathf.CeilToInt(Mathf.PI*radius/1.5f);
            for(int i=0;i<linear;i++){float z=straight*i/linear;points.Add(new Vector3(0,graded?HillHeight(z):0,z));}
            for(int i=0;i<arc;i++){float a=Mathf.PI-Mathf.PI*i/arc;points.Add(new Vector3(radius+radius*Mathf.Cos(a),0,straight+radius*Mathf.Sin(a)));}
            for(int i=0;i<linear;i++)points.Add(new Vector3(2*radius,0,straight*(1-i/(float)linear)));
            for(int i=0;i<arc;i++){float a=-Mathf.PI*i/arc;points.Add(new Vector3(radius+radius*Mathf.Cos(a),0,radius*Mathf.Sin(a)));}
            track=new Circuit(points.ToArray());turnStart=track.distance[linear];turnEnd=track.distance[linear+arc];
            finish=HasTurn?turnEnd+140:920;
            gates=HasTurn?new[]{turnStart-60,turnStart,(turnStart+turnEnd)*.5f,turnEnd,finish}:new[]{220f,520f,finish};
            sectors=HasTurn?new[]{"approach","braking approach","first half turn","second half turn","exit straight"}:new[]{"first 200m","next 300m","final 400m"};
            if(graded) {
                float AtZ(float z)=>track.distance[Mathf.RoundToInt(z*linear/straight)];
                if(IsCoast) {
                    start=AtZ(name=="coast-up"?220:name=="coast-down"?600:20);finish=start+20;
                    gates=new[]{finish};sectors=new[]{"matched coasting segment"};
                } else if(name=="hill") {
                    finish=AtZ(920);gates=new[]{AtZ(120),AtZ(400),AtZ(500),AtZ(780),finish};
                    sectors=new[]{"flat approach","climb","summit plateau","descent","flat exit"};
                } else {
                    gates=new[]{AtZ(120),AtZ(400),AtZ(500),AtZ(780),turnStart-60,turnStart,(turnStart+turnEnd)*.5f,turnEnd,finish};
                    sectors=new[]{"flat approach","climb","summit plateau","descent","fast approach","braking approach","first half turn","second half turn","exit straight"};
                }
            }
        }
        // 16% sustained grade with 60 m cosine transitions: 35.2 m summit,
        // 100 m plateau and a matching descent. No abrupt ramp/crest seams.
        public static float HillHeight(float z)=>.16f*(GradeIntegral(z-120)-GradeIntegral(z-500));
        static float GradeIntegral(float distance)
        {
            if(distance<=0)return 0;
            if(distance<60)return .5f*(distance-60/Mathf.PI*Mathf.Sin(Mathf.PI*distance/60));
            if(distance<=220)return distance-30;
            if(distance<280){float t=distance-220;return 190+.5f*(t+60/Mathf.PI*Mathf.Sin(Mathf.PI*t/60));}
            return 220;
        }
        public static Dictionary<string,string> Loadout(string specification)
        {
            if(specification=="stock"||specification=="street"||specification=="club")return EquipmentCatalog.TestLoadout(specification);
            var fit=EquipmentCatalog.StockLoadout();var slots=new HashSet<string>();
            foreach(string id in specification.Split('+')){var part=EquipmentCatalog.Get(id);if(!slots.Add(part.slot))throw new ArgumentException("Duplicate slot in loadout: "+specification);fit[part.slot]=id;}
            return fit;
        }
        public Vector2 ReferenceCommands(RoadCar car)
        {
            float speed=Mathf.Max(0,car.speed),position=car.pathDistance;
            float look=Mathf.Clamp(5+speed*.38f,7,18);
            Vector3 delta=track.At(position+look)-car.body.position;delta.y=0;
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(car.transform.forward,Vector3.up),delta,Vector3.up)*Mathf.Deg2Rad;
            float curvature=2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude);
            float steer=Mathf.Clamp(curvature*Mathf.Max(speed,3)*1.06f/car.tuning.yaw,-1,1);
            float budget=car.tuning.roadGrip*1.65f,target=BrakingTarget(car.tuning,position,speed);
            target=Mathf.Min(target,Mathf.Sqrt(budget*.70f/Mathf.Max(.001f,Mathf.Abs(curvature))));
            if(Mathf.Abs(car.offset)>5.5f)target=Mathf.Min(target,9);
            car.targetSpeed=target;
            // Continuous pedal regulation avoids alternating full braking and
            // positive throttle around the race driver's overspeed deadband.
            // This is a controlled measurement driver; race form stays separate.
            float demand=(target-speed)*10+DriveModel.Resistance(speed,car.tuning.mass);
            float pedal=demand>=0?demand/Mathf.Max(.01f,DriveModel.EngineForce(car.tuning,speed)):demand/car.tuning.braking;
            return new Vector2(Mathf.Clamp(pedal,-1,1),steer);
        }
        public float BrakingTarget(CarTuning tuning,float position,float speed)
        {
            // Work backwards from upcoming corner limits through each braking
            // segment. Query the production force model so straights can use
            // fitted brakes while a bend reserves traction for cornering.
            // A fixed fraction of tyre grip everywhere hid brake upgrades.
            float budget=tuning.roadGrip*1.65f,limit=tuning.AttainableSpeed;
            float reaction=4+Mathf.Max(0,speed)*.25f;
            for(float ahead=90;ahead>=0;ahead-=2) {
                float curve=track.Curvature(position+reaction+ahead);
                float corner=Mathf.Sqrt(budget*.64f/Mathf.Max(.001f,curve));
                limit=Mathf.Min(limit,corner);
                DriveModel.Forces(tuning,limit,-1,limit*curve,tuning.roadGrip,0,out float deceleration,out _);
                if(ahead>0)limit=Mathf.Min(tuning.AttainableSpeed,Mathf.Sqrt(limit*limit+4*Mathf.Max(0,-deceleration)*.85f));
            }
            return limit;
        }
        public float CentreSteering(RoadCar car)
        {
            Vector3 delta=track.At(car.pathDistance+12)-car.body.position;delta.y=0;
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(car.transform.forward,Vector3.up),delta,Vector3.up)*Mathf.Deg2Rad;
            return Mathf.Clamp(2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude)*Mathf.Max(car.speed,3)/car.tuning.yaw,-.15f,.15f);
        }
    }
}

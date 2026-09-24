using System;
using UnityEngine;

namespace LittleRoad
{
    public readonly struct RecoveryOption
    {
        public readonly bool clear,exitClear,movingTraffic;
        public readonly float speed,steering;
        public readonly Vector3 end,forward;
        public RecoveryOption(bool clear,bool exitClear,float speed,float steering,Vector3 end,Vector3 forward,bool movingTraffic=false)
        {this.clear=clear;this.exitClear=exitClear;this.speed=speed;this.steering=steering;this.end=end;this.forward=forward;this.movingTraffic=movingTraffic;}
    }

    // A recovery decision supplies ordinary inputs. It cannot relocate a car,
    // alter a gate, or write velocity/forces. Geometry queries live in the sensor.
    public sealed class PhysicalRecovery
    {
        public string State {get;private set;}="Racing";
        public bool Active=>State!="Racing";
        public bool AssistedNeeded {get;private set;}
        public string AssistedReason {get;private set;}
        public int completed;
        public float activeSeconds,waitSeconds,reverseSeconds;
        float stalled,episode,unproductive,bestDistance=float.MaxValue,selectedSpeed,selectedSteering,turnCommitment;
        double nextPlan,directionHoldUntil;
        bool planned,returningToGate,waitingForMovingTraffic;

        public void Observe(in DriverObservation o,float progress,float gate,bool frontBlocked,bool waitingForTraffic=false)
        {
            if(o.finished)return;
            float heading=Vector3.Angle(o.self.forward,o.route.Forward(o.self.route));
            stalled=Mathf.Abs(o.self.speed)<.4f?stalled+o.stepSeconds:0;
            // A queued car can legitimately wait while the rows ahead launch.
            // Still retain a bounded deadlock timeout and all geometric triggers.
            if(!Active&&(Mathf.Abs(o.self.offset)>7.2f||heading>65||progress>gate+.5f||frontBlocked||stalled>(waitingForTraffic?15:2.5f))) {
                State="Stabilise";episode=unproductive=turnCommitment=0;bestDistance=float.MaxValue;planned=false;waitingForMovingTraffic=false;AssistedReason=null;
            }
            if(!Active)return;
            if(progress>gate+.5f)returningToGate=true;
            if(progress<gate-6)returningToGate=false;
            episode+=o.stepSeconds;activeSeconds+=o.stepSeconds;
            bool clearGate=!returningToGate&&progress<=gate+.5f;
            if(clearGate&&!frontBlocked&&Mathf.Abs(o.self.offset)<5.5f&&heading<22&&o.self.speed>1.5f&&State!="Stabilise") {
                State="Racing";completed++;stalled=0;planned=false;AssistedNeeded=false;AssistedReason=null;return;
            }
            // Real moving traffic can justify a long wait. Static geometry or
            // a stationary deadlock must not suppress the timeout forever.
            if(State=="Wait for clearance")waitSeconds+=o.stepSeconds;
            if(State=="Wait for clearance"&&waitingForMovingTraffic)unproductive=0;else unproductive+=o.stepSeconds;
            if(State=="Reverse / turn")reverseSeconds+=o.stepSeconds;
            AssistedNeeded=episode>60&&unproductive>25;
            AssistedReason=AssistedNeeded?(State=="Wait for clearance"?"Persistent geometry or stationary traffic":"Recovery made no useful progress"):null;
        }
        public Vector3 Goal(in DriverObservation o,float progress,float gate)
        {
            // Return behind an unearned gate before approaching it forwards.
            float goal=returningToGate?gate-8:progress+12;
            return o.route.At(goal);
        }
        public bool NeedsPlan(double time)=>Active&&(!planned||time>=nextPlan);
        public void Plan(in DriverObservation o,float progress,float gate,ReadOnlySpan<RecoveryOption> options)
        {
            nextPlan=o.timestamp+.1;planned=true;
            if(o.self.velocity.magnitude>5.5f){State="Stabilise";selectedSpeed=0;selectedSteering=0;return;}
            Vector3 goal=Goal(in o,progress,gate),forward=o.route.Forward(o.self.route);
            float currentHeading=Vector3.Angle(o.self.forward,forward);
            if(currentHeading<65)turnCommitment=0;
            float distance=Vector3.Distance(o.self.position,goal);
            if(distance<bestDistance-.4f){bestDistance=distance;unproductive=0;}
            float best=float.MaxValue;bool found=false;RecoveryOption choice=default;
            foreach(var option in options) {
                if(!option.clear)continue;
                if(o.timestamp<directionHoldUntil&&selectedSpeed*option.speed<0)continue;
                if(currentHeading>110&&Mathf.Abs(option.steering)<.4f)continue;
                // Commit to a rotation direction, not a steering-wheel sign:
                // changing gear during a three-point turn reverses steering.
                if(turnCommitment!=0&&option.steering*option.speed*turnCommitment<0)continue;
                float heading=Vector3.Angle(option.forward,forward)*Mathf.Deg2Rad;
                float routeHint=o.self.route+Vector3.Dot(option.end-o.self.position,forward);
                float lateral=Mathf.Abs(Vector3.Dot(option.end-o.route.At(routeHint),o.route.Right(routeHint)));
                float score=Vector3.Distance(option.end,goal)+heading*(currentHeading>110?6:1.5f)+lateral*.8f+(option.speed<0?1:0)+(option.exitClear?0:10);
                // Small commitment penalty limits forward/reverse chatter.
                if(selectedSpeed*option.speed<0)score+=.6f;
                if(score<best){best=score;choice=option;found=true;}
            }
            waitingForMovingTraffic=false;
            if(!found){foreach(var option in options)waitingForMovingTraffic|=option.movingTraffic;State="Wait for clearance";selectedSpeed=0;selectedSteering=0;return;}
            if(selectedSpeed*choice.speed<=0)directionHoldUntil=o.timestamp+1.2;
            selectedSpeed=choice.speed;selectedSteering=choice.steering;
            if(currentHeading>110&&turnCommitment==0)turnCommitment=Mathf.Sign(selectedSteering*selectedSpeed);
            State=selectedSpeed<0?"Reverse / turn":"Rejoin";
        }
        public DriveCommand Command(in DriverObservation o)
        {
            float speed=o.self.speed,pedal;
            if(selectedSpeed==0)pedal=speed>.12f?-1:speed<-.12f?.35f:0;
            else if(selectedSpeed<0)pedal=speed>selectedSpeed+.08f?-1:0;
            else pedal=speed<selectedSpeed?Mathf.Clamp01((selectedSpeed-speed)*.35f):speed>selectedSpeed+.3f?-1:0;
            return new DriveCommand(pedal,selectedSteering,selectedSpeed,o.self.offset,0);
        }
        public void AssistedReset(){State="Racing";AssistedNeeded=false;AssistedReason=null;episode=stalled=unproductive=0;planned=returningToGate=waitingForMovingTraffic=false;directionHoldUntil=0;selectedSpeed=selectedSteering=turnCommitment=0;}
    }

    // Read-only bounded swept-space queries. Current traffic is taken from the
    // immutable snapshot; its predicted movement is checked along each manoeuvre.
    public sealed class RecoverySensor
    {
        readonly Rigidbody own;
        readonly BoxCollider queryShape;
        readonly Vector3 centre,half;
        readonly Collider[] overlaps=new Collider[32];
        readonly RaycastHit[] hits=new RaycastHit[32];
        readonly RecoveryOption[] options=new RecoveryOption[22];
        double nextDiagnostic;
        string staticBlocker;
        public RecoverySensor(Rigidbody body,BoxCollider shape)
        {
            own=body;centre=shape.center;half=shape.size*.5f;
            // Keep the query shape registered, but outside the world and separate
            // from the car's compound body. It must not alter mass or inertia.
            var query=new GameObject("Recovery clearance query");query.transform.position=new Vector3(0,-10000,0);
            queryShape=query.AddComponent<BoxCollider>();queryShape.isTrigger=true;queryShape.size=2*new Vector3(half.x+.12f,Mathf.Max(.1f,half.y-.12f),half.z+.12f);
        }
        public void Dispose(){if(queryShape!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(queryShape.gameObject);else UnityEngine.Object.DestroyImmediate(queryShape.gameObject);}}
        bool Relevant(Collider collider)=>collider!=null&&collider.attachedRigidbody!=own&&collider.GetComponent<RoadCar>()==null&&collider.CompareTag("Respawn");
        public bool StaticSweepClear(Vector3 from,Vector3 to,Vector3 forward)=>StaticClear(from,to,Quaternion.LookRotation(forward));
        bool Supported(Vector3 position,Vector3 forward)
        {
            var right=Vector3.Cross(Vector3.up,forward);
            var bottom=position+right*centre.x+forward*centre.z+Vector3.up*(centre.y-half.y);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2) {
                var point=bottom+right*(x*half.x)+forward*(z*half.z);
                int count=Physics.RaycastNonAlloc(point+Vector3.up*.5f,Vector3.down,hits,.9f,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)return false;bool found=false;
                for(int i=0;i<count;i++)if(hits[i].normal.y>.9f&&hits[i].collider.GetComponent<RoadSupport>()!=null)found=true;
                if(!found)return false;
            }
            return true;
        }
        public bool StaticSweepClear(Vector3 from,Vector3 to,Vector3 fromForward,Vector3 toForward)
        {
            float turn=Vector3.Angle(fromForward,toForward)*Mathf.Deg2Rad;
            var middle=Vector3.Slerp(fromForward,toForward,.5f).normalized;
            // Bound corner travel while orientation changes across this sweep.
            return StaticClear(from,to,Quaternion.LookRotation(middle),(half.x+half.z)*turn*.5f);
        }
        bool BlocksMotion(Collider obstacle,Vector3 from,Vector3 to,Quaternion rotation)
        {
            // A stop can leave the safety envelope (not the actual body) touching
            // an obstacle. Permit only motion out of that existing overlap;
            // otherwise an inflated query forbids even reversing away from it.
            bool initialOverlap=Physics.ComputePenetration(queryShape,from,rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out var escape,out float before);
            if(!initialOverlap)return true;
            if(Vector3.Dot(to-from,escape)<=.00001f)return true;
            bool overlapsEnd=Physics.ComputePenetration(queryShape,to,rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float after);
            return overlapsEnd&&after>=before-.00001f;
        }
        bool StaticClear(Vector3 from,Vector3 to,Quaternion rotation,float angularPadding=0)
        {
            staticBlocker=null;
            Vector3 extent=half+new Vector3(.12f,-.12f,.12f);extent.y=Mathf.Max(.1f,extent.y);
            extent.x+=angularPadding;extent.z+=angularPadding;
            Vector3 origin=from+rotation*centre+Vector3.up*.12f;Vector3 delta=to-from;
            int count=Physics.OverlapBoxNonAlloc(origin,extent,overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length){staticBlocker="overlap query saturated";return false;}
            for(int i=0;i<count;i++)if(Relevant(overlaps[i])&&BlocksMotion(overlaps[i],origin,origin+delta,rotation)){staticBlocker=overlaps[i].name;return false;}
            if(delta.sqrMagnitude<.0001f)return true;
            count=Physics.BoxCastNonAlloc(origin,extent,delta.normalized,hits,rotation,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length){staticBlocker="sweep query saturated";return false;}
            for(int i=0;i<count;i++)if(Relevant(hits[i].collider)&&BlocksMotion(hits[i].collider,origin,origin+delta,rotation)){staticBlocker=hits[i].collider.name;return false;}
            return true;
        }
        bool TrafficClear(in DriverObservation o,Vector3 position,Vector3 forward,float seconds)=>TrafficClear(in o,position,forward,seconds,out _);
        bool TrafficClear(in DriverObservation o,Vector3 position,Vector3 forward,float seconds,out bool moving)
        {
            moving=false;
            var right=Vector3.Cross(Vector3.up,forward);
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                Vector3 delta=other.position+other.velocity*seconds-position;
                // Conservative oriented rectangle projections, including the
                // other car's heading and a 0.65 m uncertainty/room margin.
                float along=half.z+2.4f*Mathf.Abs(Vector3.Dot(other.forward,forward))+1.4f*Mathf.Abs(Vector3.Dot(other.right,forward))+.65f;
                float across=half.x+2.4f*Mathf.Abs(Vector3.Dot(other.forward,right))+1.4f*Mathf.Abs(Vector3.Dot(other.right,right))+.65f;
                if(Mathf.Abs(Vector3.Dot(delta,forward))<along&&Mathf.Abs(Vector3.Dot(delta,right))<across){moving=other.velocity.sqrMagnitude>.25f*.25f;return false;}
            }
            return true;
        }
        public bool DestinationClear(in DriverObservation o,Vector3 point,Vector3 forward)
        {
            if(!StaticClear(point,point,Quaternion.LookRotation(forward)))return false;
            for(float seconds=0;seconds<=1;seconds+=.25f)if(!TrafficClear(in o,point,forward,seconds))return false;
            return true;
        }
        public bool FrontBlocked(in DriverObservation o,bool recovering,RacingLine line)
        {
            float distance=Mathf.Max(recovering?12:2,2+Mathf.Max(0,o.self.speed)*Mathf.Max(0,o.self.speed)/Mathf.Max(2,o.tuning.braking));
            var end=o.self.position+o.self.forward*distance;
            if(!StaticClear(o.self.position,end,Quaternion.LookRotation(o.self.forward)))return true;
            for(float t=0;t<=1;t+=.2f)if(!TrafficClear(in o,Vector3.Lerp(o.self.position,end,t),o.self.forward,t*.5f))return true;
            if(line!=null) {
                // Anticipate the path the normal driver will actually take,
                // including while racing. Checking only current heading can
                // miss an obstacle beside a bending line until braking is late.
                float projected=line.Project(o.self.position,o.self.route,out _);
                var start=line.At(projected);var displacement=o.self.position-start;var previous=o.self.position;
                for(float metres=2;metres<=Mathf.Max(24,distance);metres+=2) {
                    var point=line.At(projected+metres)+displacement*Mathf.Clamp01(1-metres/12);
                    var forward=(point-previous).normalized;
                    if(!StaticClear(previous,point,Quaternion.LookRotation(forward)))return true;
                    if(!TrafficClear(in o,point,forward,metres/Mathf.Max(4,o.self.speed)))return true;
                    previous=point;
                }
            }
            return false;
        }
        public ReadOnlySpan<RecoveryOption> Sample(in DriverObservation o,float initialSteering)
        {
            int index=0,corridorRejected=0,staticRejected=0,trafficRejected=0,accepted=0;string blocker=null;
            var roadRight=o.route.Right(o.self.route);
            float initialFootprint=half.x*Mathf.Abs(Vector3.Dot(o.self.right,roadRight))+half.z*Mathf.Abs(Vector3.Dot(o.self.forward,roadRight));
            float corridorLimit=Mathf.Max(11.5f,Mathf.Abs(o.self.offset)+initialFootprint+.1f);
            for(int direction=-1;direction<=1;direction+=2)for(int turn=-5;turn<=5;turn++) {
                float target=direction<0?-3:4,steering=turn*.2f,speed=o.self.speed,applied=initialSteering;
                var position=o.self.position;var forward=o.self.forward;bool clear=true,movingTraffic=false;
                for(int step=1;step<=12;step++) {
                    const float dt=.2f;speed=Mathf.MoveTowards(speed,target,o.tuning.acceleration*dt);
                    applied=Mathf.MoveTowards(applied,steering,o.tuning.response*dt);
                    float yaw=applied*o.tuning.yaw*Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sign(speed);
                    var middle=Quaternion.AngleAxis(yaw*dt*.5f*Mathf.Rad2Deg,Vector3.up)*forward;
                    var end=position+middle*speed*dt;
                    forward=Quaternion.AngleAxis(yaw*dt*Mathf.Rad2Deg,Vector3.up)*forward;
                    var rotation=Quaternion.LookRotation(middle);
                    float routeHint=o.self.route+Vector3.Dot(end-o.self.position,o.route.Forward(o.self.route));
                    float side=Vector3.Dot(end-o.route.At(routeHint),o.route.Right(routeHint));
                    float footprint=half.x*Mathf.Abs(Vector3.Dot(Vector3.Cross(Vector3.up,forward),o.route.Right(routeHint)))+half.z*Mathf.Abs(Vector3.Dot(forward,o.route.Right(routeHint)));
                    // Recovery may use the shoulder but must not drive farther
                    // out than its starting excursion or the supported runoff.
                    if(Mathf.Abs(side)+footprint>corridorLimit){corridorRejected++;clear=false;break;}
                    if(!StaticClear(position,end,rotation)){staticRejected++;blocker=staticBlocker;clear=false;break;}
                    if(!TrafficClear(in o,end,forward,step*dt,out movingTraffic)){trafficRejected++;clear=false;break;}
                    position=end;
                }
                float endRoute=o.self.route+Vector3.Dot(position-o.self.position,o.route.Forward(o.self.route));
                var exitDirection=o.route.Forward(endRoute);
                bool exitClear=clear&&StaticClear(position,position+exitDirection*12,Quaternion.LookRotation(exitDirection));
                options[index++]=new RecoveryOption(clear,exitClear,target,steering,position,forward,movingTraffic);
                if(clear)accepted++;
            }
            if(RaceTrial.Has("--recovery-case")&&o.timestamp>=nextDiagnostic){nextDiagnostic=o.timestamp+2;Debug.Log($"RECOVERY_SENSOR id={o.self.id} t={o.timestamp:0.00} clear={accepted} corridor={corridorRejected} static={staticRejected} traffic={trafficRejected} blocker={blocker} offset={o.self.offset:0.00} half={half}");}
            return options;
        }
    }
}

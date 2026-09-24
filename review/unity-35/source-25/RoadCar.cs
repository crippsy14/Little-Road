using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    public sealed class RoadCar : MonoBehaviour
    {
        public Rigidbody body;
        public CarTuning tuning;
        public RaceTrial race;
        public int driverId;
        public bool autonomous, finished,controlsBlocked;
        public float speed, pathDistance, offset, progress, finishTime, targetSpeed;
        public float throttle, steering, pace=1, preferredOffset;
        public float predictedOffset,requestedYaw,lateralSpeed,chosenLane;
        public float SteeringState=>steerState;
        // Observation only: strongest solver impulse since the telemetry sample,
        // including road support contacts excluded from race incident scoring.
        public float observedContactDeltaV;
        public Vector3 observedContactImpulse;
        public string observedContactSurface;
        public int nextGate, laps, contacts, recoveries, missedGates;
        public float nextGateProgress, maxImpact, maxPenetration, bestLap=float.MaxValue;
        public string equipment;
        // Optional input policy for proving-ground maneuvers. It supplies only
        // pedal/steering requests; all forces, grip and contacts stay below.
        [System.NonSerialized] public System.Func<RoadCar,Vector2> commandSource;
        public RoadGrounding ground;
        public Vector3 DriveForward=>ground==null?transform.forward:ground.forward;
        float steerState, previousPath, stuckTime,lapStart,lastSafeDistance;
        double nextAssistedAttempt;
        bool wasCheckpointMissed;
        public bool CheckpointMissed=>!race.freeRoam&&!finished&&progress>nextGateProgress+.5f;
        public float RankedProgress=>Mathf.Min(progress,nextGateProgress);
        public string CheckpointMessage=>CheckpointMissed?"CHECKPOINT MISSED — R / D-pad Down: return before it":"";
        IDriverPolicy driverPolicy;
        public PhysicalRecovery physicalRecovery {get;private set;}
        public TrafficDriverPolicy trafficPolicy {get;private set;}
        public ContextDriverPolicy contextPolicy {get;private set;}
        public Vector3 CollisionHalf {get;private set;}
        RecoverySensor recoverySensor;
        public RacingSpeedProfile referenceProfile {get;private set;}
        readonly ObservedCar[] legacyObserved=new ObservedCar[24];
        WheelVisuals wheels;
        float wheelSpin;

        public void Setup(RaceTrial owner,int id,CarTuning stats,bool ai,int seed,string label)
        {
            race=owner;driverId=id;tuning=stats;autonomous=ai;equipment=label;
            var random=new System.Random(seed);preferredOffset=(float)(random.NextDouble()*3-1.5);
            pace=.94f+(float)random.NextDouble()*.06f;
            driverPolicy=new LegacyDriverPolicy(random);
            if(owner.ReferenceLine!=null){referenceProfile=new RacingSpeedProfile(owner.ReferenceLine,tuning);driverPolicy=new ReferenceDriverPolicy(referenceProfile);}
            body=gameObject.AddComponent<Rigidbody>();body.mass=850*tuning.mass;
            body.constraints=race.track.hasElevation?RigidbodyConstraints.None:RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            // Include angular motion in contact prediction. Keep a diagnostic
            // sweep baseline for matched regressions; neither mode is perfect.
            body.collisionDetectionMode=RaceTrial.Has("--sweep-contacts")?CollisionDetectionMode.ContinuousDynamic:CollisionDetectionMode.ContinuousSpeculative;
            body.linearDamping=0;body.angularDamping=1.5f;body.maxAngularVelocity=2.3f;
            body.solverIterations=12;body.solverVelocityIterations=8;
            body.centerOfMass=new Vector3(0,.45f,0);
            Bounds envelope=new Bounds();bool firstVertex=true;
            foreach(var renderer in GetComponentsInChildren<Renderer>()) {
                Bounds local=renderer.localBounds;
                for(int corner=0;corner<8;corner++) {
                    Vector3 p=local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    p=transform.InverseTransformPoint(renderer.transform.TransformPoint(p));
                    if(firstVertex){envelope=new Bounds(p,Vector3.zero);firstVertex=false;}else envelope.Encapsulate(p);
                }
            }
            envelope.Expand(.015f);Vector3 size=envelope.size;size.x=Mathf.Max(size.x,2.32f);
            var collider=gameObject.AddComponent<BoxCollider>();collider.center=envelope.center;collider.size=size;
            CollisionHalf=size*.5f;
            if(RaceTrial.Has("--physical-recovery")||owner.DriverPolicyId=="traffic-v1"||owner.DriverPolicyId=="context-v1")recoverySensor=new RecoverySensor(body,collider);
            if(RaceTrial.Has("--physical-recovery"))physicalRecovery=new PhysicalRecovery();
            if(owner.DriverPolicyId=="traffic-v1"){trafficPolicy=new TrafficDriverPolicy(referenceProfile,recoverySensor);driverPolicy=trafficPolicy;}
            if(owner.DriverPolicyId=="context-v1"){contextPolicy=new ContextDriverPolicy(referenceProfile,recoverySensor,collider.center,collider.size*.5f);driverPolicy=contextPolicy;}
            // Tyre/rolling resistance is already calculated by DriveModel.
            // Sliding a box with the default contact friction adds an unintended
            // ground brake, disproportionately affecting stock equipment.
            collider.material=new PhysicsMaterial("Arcade tyre contact") {dynamicFriction=0,staticFriction=0,
                frictionCombine=PhysicsMaterialCombine.Minimum,bounciness=.05f,bounceCombine=PhysicsMaterialCombine.Minimum};
            if(race.track.hasElevation){ground=new RoadGrounding(this);ground.Sample();}
            wheels=new WheelVisuals(gameObject);
            pathDistance=race.track.Nearest(transform.position,out offset);previousPath=pathDistance;
            progress=pathDistance>race.track.length*.8f?pathDistance-race.track.length:pathDistance;
            nextGate=0;nextGateProgress=0;lastSafeDistance=progress;
        }
        void FixedUpdate()
        {
            if(race==null || !race.running) return;
            if(ground!=null)ground.Sample();
            float recoveryDatum=ground==null?0:race.track.At(pathDistance).y;
            if(race.freeRoam&&body.position.y<recoveryDatum-3){TryRecover();return;}
            if(controlsBlocked){throttle=0;steering=0;speed=0;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;return;}
            Vector3 driveForward=DriveForward,driveRight=ground==null?transform.right:ground.right,driveUp=ground==null?Vector3.up:ground.normal;
            speed=Vector3.Dot(body.linearVelocity,driveForward);
            float beforeProgress=progress,beforeOffset=offset;
            pathDistance=race.track.Nearest(body.position,out offset);
            float step=Mathf.Repeat(pathDistance-previousPath+race.track.length*.5f,race.track.length)-race.track.length*.5f;
            if(Mathf.Abs(step)<10)progress+=step;
            previousPath=pathDistance;
            if(commandSource!=null){var command=commandSource(this);throttle=Mathf.Clamp(command.x,-1,1);steering=Mathf.Clamp(command.y,-1,1);}
            else if(autonomous || finished) Plan(); else ReadInput();
            if(race.elapsed<0) {throttle=0;steering=0;body.linearVelocity=Vector3.zero;return;}
            race.MotionAudit?.Observe(this,race.elapsed);
            race.ApproachAudit?.Observe(this,race.elapsed);
            steerState=Mathf.MoveTowards(steerState,steering,tuning.response*Time.fixedDeltaTime);
            float grip=Mathf.Abs(offset)<Circuit.Width*.5f?tuning.roadGrip:tuning.dirtGrip;
            float yawRequest=steerState*tuning.yaw*Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sign(speed==0?1:speed);
            DriveModel.Forces(tuning,speed,throttle,yawRequest,grip,0,out float acceleration,out float yaw);
            // PhysX gravity and ground contact already produce the along-grade
            // component. Passing an explicit slope here would count it twice.
            requestedYaw=yaw;
            if(speed < -7 && acceleration<0)acceleration=0;
            bool supported=ground==null||ground.grounded;
            if(supported)body.AddForce(driveForward*acceleration,ForceMode.Acceleration);
            // Steering changes heading; lateral tyre force moves velocity toward
            // that heading within the available grip, preserving collision impulses.
            lateralSpeed=Vector3.Dot(body.linearVelocity,driveRight);
            if(supported)body.AddForce(-driveRight*Mathf.Clamp(lateralSpeed*8,-grip*1.65f,grip*1.65f),ForceMode.Acceleration);
            // A torque servo lets contact impulses resist steering. Directly
            // imposing a new rotation can push a corner into an adjacent car.
            if(supported)body.AddTorque(driveUp*Mathf.Clamp((yaw-Vector3.Dot(body.angularVelocity,driveUp))*20,-25,25),ForceMode.Acceleration);
            if(ground!=null)ground.Align();
            if(!race.freeRoam) {
                if(Mathf.Abs(step)<10)TrackGates(beforeProgress,beforeOffset);
                if(CheckpointMissed&&!wasCheckpointMissed) {
                    missedGates++;
                    Debug.Log($"CHECKPOINT_MISSED id={driverId} gate={nextGate} required={nextGateProgress:0.00} progress={progress:0.00} offset={offset:0.00} time={race.elapsed:0.00}");
                }
                wasCheckpointMissed=CheckpointMissed;
            }
            if(Mathf.Abs(offset)<6 && speed>3 && progress<=nextGateProgress)lastSafeDistance=progress;
            if(autonomous&&!finished) {
                stuckTime=speed<1.5f?stuckTime+Time.fixedDeltaTime:0;
                if(physicalRecovery!=null){if((physicalRecovery.AssistedNeeded||transform.position.y<recoveryDatum-3)&&Time.fixedTimeAsDouble>=nextAssistedAttempt){nextAssistedAttempt=Time.fixedTimeAsDouble+.5;TryRecover();}}
                else if(!race.freeRoam&&progress>nextGateProgress+10)TryRecover();
                else if(stuckTime>6 || Mathf.Abs(offset)>32 || transform.position.y< -2)TryRecover();
            }
            wheelSpin+=speed/.425f*Mathf.Rad2Deg*Time.fixedDeltaTime;
            wheels.Pose(wheelSpin,steerState*25);
        }
        void OnDestroy(){recoverySensor?.Dispose();}
        void ReadInput()
        {
            var k=Keyboard.current;var p=Gamepad.current;float gas=0,brake=0;steering=0;
            if(k!=null) {gas=k.wKey.isPressed||k.upArrowKey.isPressed?1:0;brake=k.sKey.isPressed||k.downArrowKey.isPressed?1:0;
                steering=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);}
            if(p!=null) {gas=Mathf.Max(gas,p.rightTrigger.ReadValue());brake=Mathf.Max(brake,p.leftTrigger.ReadValue());
                if(Mathf.Abs(p.leftStick.x.ReadValue())>.12f)steering=p.leftStick.x.ReadValue();}
            throttle=gas-brake;
        }
        void Plan()
        {
            var stamp=race.DriverAudit!=null?DriverFrameAudit.Begin():default;
            PlanObserved();
            race.DriverAudit?.Record(stamp,false);
        }
        void PlanObserved()
        {
            ReadOnlySpan<ObservedCar> observed;
            double timestamp;
            ObservedCar self;
            if(race.DriverSnapshot!=null) {
                observed=race.DriverSnapshot.Entries;timestamp=race.DriverSnapshot.Timestamp;self=observed[driverId];
            } else {
                // Compatibility capture preserves the frozen driver's cached
                // route/speed semantics. New policies will use the shared snapshot.
                int count=0;
                foreach(var car in race.cars)legacyObserved[count++]=new ObservedCar(car.driverId,car.body.position,car.body.linearVelocity,
                    car.transform.forward,car.transform.right,car.pathDistance,car.offset,car.speed);
                observed=new ReadOnlySpan<ObservedCar>(legacyObserved,0,count);timestamp=Time.fixedTimeAsDouble;
                self=new ObservedCar(driverId,body.position,body.linearVelocity,transform.forward,transform.right,pathDistance,offset,speed);
            }
            var observation=new DriverObservation(timestamp,Time.fixedDeltaTime,self,observed,race.track,tuning,pace,preferredOffset,finished,steerState);
            DriveCommand command;
            if(physicalRecovery!=null&&race.elapsed>=0&&!finished) {
                string previous=physicalRecovery.State;
                bool blocked=((trafficPolicy==null&&contextPolicy==null)||physicalRecovery.Active)&&recoverySensor.FrontBlocked(in observation,physicalRecovery.Active,referenceProfile?.line);
                physicalRecovery.Observe(in observation,progress,nextGateProgress,blocked,trafficPolicy?.WaitingForTraffic??contextPolicy?.WaitingForTraffic??false);
                if(physicalRecovery.NeedsPlan(timestamp))physicalRecovery.Plan(in observation,progress,nextGateProgress,recoverySensor.Sample(in observation,steerState));
                command=physicalRecovery.Active?physicalRecovery.Command(in observation):driverPolicy.Decide(in observation);
                if(previous!=physicalRecovery.State)Debug.Log($"PHYSICAL_RECOVERY id={driverId} time={race.elapsed:0.000} state={physicalRecovery.State} progress={progress:0.00} gate={nextGateProgress:0.00}");
            }else command=driverPolicy.Decide(in observation);
            throttle=command.pedal;steering=command.steering;targetSpeed=command.targetSpeed;
            predictedOffset=command.predictedOffset;chosenLane=command.chosenLane;
        }
        void TrackGates(float beforeProgress,float beforeOffset)
        {
            if(finished)return;
            if(CheckpointCrossing.Forward(beforeProgress,progress,beforeOffset,offset,nextGateProgress,Circuit.Width*.5f+2)) {
                race.RecordCheckpoint(this,nextGateProgress);
                if(nextGate==0) {
                    if(nextGateProgress>race.track.length*.8f) {laps++;float lap=race.elapsed-lapStart;bestLap=Mathf.Min(bestLap,lap);
                        if(laps>=race.requiredLaps) {finished=true;finishTime=race.elapsed;race.Finish(this);}}
                    lapStart=race.elapsed;
                }
                nextGate=(nextGate+1)%12;
                nextGateProgress+=race.track.length/12;
            }
        }
        public bool TryRecover()
        {
            // Return behind the last clear on-course position. Never advance
            // ordered gates; wait if another car occupies the destination.
            float destination=race.freeRoam?race.track.Nearest(body.position,out _):Mathf.Min(lastSafeDistance-4,nextGateProgress-6);
            Vector3 point=race.track.At(destination)+Vector3.up*.16f;
            int rejected=0;
            if(recoverySensor!=null) {
                // The last safe point can be ahead after reversing. An assisted
                // recovery must lose progress, and must not appear in scenery
                // or in the path of an approaching car.
                if(!race.freeRoam)destination=Mathf.Min(destination,progress-4);
                int count=0;foreach(var other in race.cars)legacyObserved[count++]=new ObservedCar(other.driverId,other.body.position,other.body.linearVelocity,
                    other.transform.forward,other.transform.right,other.pathDistance,other.offset,other.speed);
                var self=new ObservedCar(driverId,body.position,body.linearVelocity,transform.forward,transform.right,pathDistance,offset,speed);
                var observation=new DriverObservation(Time.fixedTimeAsDouble,Time.fixedDeltaTime,self,new ReadOnlySpan<ObservedCar>(legacyObserved,0,count),race.track,tuning,pace,preferredOffset,finished);
                bool clear=false;float first=destination;
                for(int step=0;step<=16;step++) {
                    destination=first-step*4;point=race.track.At(destination)+Vector3.up*.16f;
                    if(recoverySensor.DestinationClear(in observation,point,race.track.Forward(destination))){clear=true;break;}
                    rejected++;
                }
                if(!clear)return false;
            } else foreach(var other in race.cars)if(other!=this && Vector3.Distance(other.body.position,point)<5)return false;
            var before=body.position;float beforeProgress=progress;int beforeGate=nextGate;
            string reason=physicalRecovery?.AssistedReason??(body.position.y<race.track.At(pathDistance).y-3?"Below world":"Requested or legacy recovery");
            body.position=point;body.rotation=Quaternion.LookRotation(race.track.Forward(destination));
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            pathDistance=Mathf.Repeat(destination,race.track.length);previousPath=pathDistance;progress=destination;
            stuckTime=0;wasCheckpointMissed=false;recoveries++;physicalRecovery?.AssistedReset();
            race.RecordRecovery(this,before,point,beforeProgress,beforeGate,reason,rejected);return true;
        }
        // Explicit fixture initialisation only. No runtime recovery uses this.
        public void PlaceTrafficFixture(float route,float? roadSide=null)
        {
            if((!RaceTrial.Has("--traffic-case")&&!RaceTrial.Has("--path-probe"))||race.ReferenceLine==null)throw new InvalidOperationException("Traffic placement requires a declared reference-line fixture.");
            var line=race.ReferenceLine;var forward=(line.At(route+.5f)-line.At(route-.5f)).normalized;
            body.position=line.At(route)+Vector3.up*.15f;body.rotation=Quaternion.LookRotation(forward);body.linearVelocity=body.angularVelocity=Vector3.zero;
            if(roadSide.HasValue){body.position=race.track.At(route)+race.track.Right(route)*roadSide.Value+Vector3.up*.15f;body.rotation=Quaternion.LookRotation(race.track.Forward(route));}
            pathDistance=race.track.Nearest(body.position,out offset);previousPath=pathDistance;progress=pathDistance;
            float gateStep=race.track.length/12;int gate=Mathf.FloorToInt(progress/gateStep)+1;nextGate=gate%12;nextGateProgress=gate*gateStep;
            lastSafeDistance=progress;lapStart=0;laps=0;
        }
        public void RestoreApproachFixture(PathApproachAudit.Capture capture)
        {
            if(!RaceTrial.Has("--path-approach-file")||!RaceTrial.Has("--path-probe"))throw new InvalidOperationException("Approach restoration is fixture-only.");
            // Interpolated bodies may retain the old transform until physics.
            // The first control observation must see the recorded heading too.
            transform.SetPositionAndRotation(capture.position,capture.rotation);
            body.position=capture.position;body.rotation=capture.rotation;body.linearVelocity=capture.velocity;body.angularVelocity=capture.angularVelocity;
            steerState=capture.steeringState;steering=capture.steering;throttle=capture.pedal;
            pathDistance=race.track.Nearest(body.position,out offset);previousPath=pathDistance;progress=pathDistance;speed=capture.speed;
            float gateStep=race.track.length/12;int gate=Mathf.FloorToInt(progress/gateStep)+1;nextGate=gate%12;nextGateProgress=gate*gateStep;
            lastSafeDistance=progress;lapStart=0;laps=0;
        }
        public void PlaceRecoveryFixture(float route,float side,float heading,float velocity)
        {
            if(!RaceTrial.Has("--recovery-case"))throw new InvalidOperationException("Recovery fixture placement requires a declared test case.");
            var rotation=Quaternion.LookRotation(race.track.Forward(route))*Quaternion.Euler(0,heading,0);
            body.position=race.track.At(route)+race.track.Right(route)*side+Vector3.up*.15f;body.rotation=rotation;
            body.linearVelocity=rotation*Vector3.forward*velocity;body.angularVelocity=Vector3.zero;
            pathDistance=Mathf.Repeat(route,race.track.length);previousPath=pathDistance;offset=side;progress=route;lastSafeDistance=Mathf.Min(route,-8);
            laps=nextGate=0;nextGateProgress=0;lapStart=0;
        }
        public void PlaceLaneFixture(float side)
        {
            if(!RaceTrial.Arg("--traffic-case","").StartsWith("lane-"))throw new InvalidOperationException("Lane placement requires a declared fixture.");
            const float route=-8;
            body.position=race.track.At(route)+race.track.Right(route)*side+Vector3.up*.15f;body.rotation=Quaternion.LookRotation(race.track.Forward(route));
            body.linearVelocity=body.angularVelocity=Vector3.zero;
            pathDistance=Mathf.Repeat(route,race.track.length);previousPath=pathDistance;offset=side;progress=lastSafeDistance=route;
            laps=nextGate=0;nextGateProgress=lapStart=0;
        }
        void OnCollisionEnter(Collision c)
        {
            ObserveContact(c);
            if(c.collider.GetComponent<RoadCar>()==null && !c.collider.CompareTag("Respawn"))return;
            contacts++;maxImpact=Mathf.Max(maxImpact,c.relativeVelocity.magnitude);
            float depth=0;for(int i=0;i<c.contactCount;i++)depth=Mathf.Max(depth,-c.GetContact(i).separation);
            maxPenetration=Mathf.Max(maxPenetration,depth);
            var other=c.collider.GetComponent<RoadCar>();
            race.RecordImpact(this,other==null?-1:other.driverId,c.relativeVelocity.magnitude,depth,c.collider.name);
        }
        void OnCollisionStay(Collision c)
        {
            ObserveContact(c);
            if(c.collider.GetComponent<RoadCar>()==null && !c.collider.CompareTag("Respawn"))return;
            // Entry contacts can be predictive and have positive separation.
            // Include sustained contact when measuring worst penetration.
            for(int i=0;i<c.contactCount;i++)maxPenetration=Mathf.Max(maxPenetration,-c.GetContact(i).separation);
        }
        void ObserveContact(Collision c)
        {
            float delta=c.impulse.magnitude/body.mass;
            if(delta<=observedContactDeltaV)return;
            observedContactDeltaV=delta;observedContactImpulse=c.impulse/body.mass;observedContactSurface=c.collider.name;
        }
    }
}

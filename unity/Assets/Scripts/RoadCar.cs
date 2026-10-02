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
        public float DragReduction {get;private set;}
        public int DraftLeader {get;private set;}=-1;
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
        [System.NonSerialized] public bool holdStill;
        public RoadGrounding ground;
        public Vector3 DriveForward=>ground==null?transform.forward:ground.forward;
        float steerState, previousPath, stuckTime,lapStart,lastSafeDistance;
        public float LastLapTime {get;private set;}
        public float CurrentLapTime=>nextGateProgress<=0?0:finished?LastLapTime:Mathf.Max(0,race.elapsed-lapStart);
        double nextAssistedAttempt;
        bool wasCheckpointMissed;
        public bool CheckpointMissed=>!race.freeRoam&&!finished&&progress>nextGateProgress+.5f;
        // --gate-first (off by default): credit a checkpoint crossed this tick before the driver and
        // physical recovery read nextGateProgress. Progress is a projection onto 1.5 m route segments;
        // on the inside of a tight corner it jumps ~offset x segment bend when the nearest segment
        // changes (~0.4 m at Hillcrest T5). A jump that lands >0.5 m past a gate read as a missed gate
        // to recovery for that one tick, which stopped a racing car dead (study 125).
        public static bool GateFirst;
        public float RankedProgress=>Mathf.Min(progress,nextGateProgress);
        public string CheckpointMessage=>CheckpointMissed?"CHECKPOINT MISSED — R / D-pad Down: return before it":"";
        IDriverPolicy driverPolicy;
        public PhysicalRecovery physicalRecovery {get;private set;}
        public TrafficDriverPolicy trafficPolicy {get;private set;}
        public ContextDriverPolicy contextPolicy {get;private set;}
        public RacerDriverPolicy racerPolicy {get;private set;}
        // The live gearbox (docs/47): gear, revs, boost and shifts for the physics and the sound.
        int gearRecoveries;
        public GearState gears {get;private set;}
        // The player drives a manual box when Settings > Gears says so; everything autonomous stays automatic.
        public bool ManualGears=>!autonomous&&DrivingOptions.Manual;
        // Pedal use while racing (both AIs, for comparisons): braking, lifting (partial throttle above
        // walking pace) and stopped (under 2 m/s: the pauses of docs/46).
        public float BrakeSeconds {get;private set;}
        public float LiftSeconds {get;private set;}
        public float StoppedSeconds {get;private set;}
        public Vector3 CollisionHalf {get;private set;}
        public Vector3 CollisionCentre {get;private set;}
        // Lowest point of the car's physical contact shape, in local space.
        public float ContactBottom {get;private set;}
        // Tyre grip used on the last physics step (0 while controls are blocked).
        public float Grip {get;private set;}
        public float UnsupportedSeconds {get;private set;}
        RecoverySensor recoverySensor;
        public RacingSpeedProfile referenceProfile {get;private set;}
        public DriverIdentity Driver {get;private set;}
        readonly ObservedCar[] legacyObserved=new ObservedCar[24];
        WheelVisuals wheels;
        float wheelSpin;

        public void Setup(RaceTrial owner,int id,CarTuning stats,bool ai,int seed,string label,DriverIdentity identity=null)
        {
            Driver=identity;
            race=owner;driverId=id;tuning=stats;autonomous=ai;equipment=label;
            var random=new System.Random(seed);preferredOffset=(float)(random.NextDouble()*3-1.5);
            pace=.94f+(float)random.NextDouble()*.06f;
            driverPolicy=new LegacyDriverPolicy(random);
            if(owner.ReferenceLine!=null){var planning=identity!=null?identity.Planning:owner.DriverPolicyId=="context-v1"?DriverPlanningSettings.Preset(RaceTrial.Arg("--driver-braking","neutral")).WithRisk(int.Parse(RaceTrial.Arg("--driver-risk","0"),System.Globalization.CultureInfo.InvariantCulture)):default;referenceProfile=new RacingSpeedProfile(owner.ReferenceLine,tuning,planning,owner.DraftingEnabled);driverPolicy=new ReferenceDriverPolicy(referenceProfile);}
            body=gameObject.AddComponent<Rigidbody>();body.mass=850*tuning.mass;
            body.constraints=race.track.hasElevation?RigidbodyConstraints.None:RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            // Swept contacts: speculative contacts snag the internal edges of road meshes while a car yaws
            // (one-step "knocks"). Study 215 kept swept contacts for every car: all 24 finished on 12 races,
            // contacts 115 -> 54, total time 9617.0 -> 9616.6 s, physics cost unchanged (study 217).
            // --speculative-contacts restores the old mode for comparison.
            body.collisionDetectionMode=RaceTrial.Has("--speculative-contacts")?CollisionDetectionMode.ContinuousSpeculative:CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping=0;body.angularDamping=1.5f;body.maxAngularVelocity=2.3f;
            body.solverIterations=12;body.solverVelocityIterations=8;
            body.centerOfMass=new Vector3(0,.45f,0);
            var (boxCentre,size)=CollisionBox(gameObject);
            var collider=gameObject.AddComponent<BoxCollider>();collider.center=boxCentre;collider.size=size;
            CollisionHalf=size*.5f;CollisionCentre=boxCentre;
            if(!Application.isBatchMode)gameObject.AddComponent<LightTrails>().Setup(this,boxCentre,CollisionHalf);
            if(RaceTrial.Has("--physical-recovery")||owner.DriverPolicyId=="traffic-v1"||owner.DriverPolicyId=="context-v1"||owner.DriverPolicyId=="racer-v2")recoverySensor=new RecoverySensor(body,collider);
            if(RaceTrial.Has("--physical-recovery"))physicalRecovery=new PhysicalRecovery();
            if(owner.DriverPolicyId=="traffic-v1"){trafficPolicy=new TrafficDriverPolicy(referenceProfile,recoverySensor);driverPolicy=trafficPolicy;}
            if(owner.DriverPolicyId=="context-v1"){contextPolicy=new ContextDriverPolicy(referenceProfile,recoverySensor,collider.center,collider.size*.5f);driverPolicy=contextPolicy;}
            if(owner.DriverPolicyId=="racer-v2"){racerPolicy=new RacerDriverPolicy(owner.ReferenceLine,RacerAbilities.From(identity).WithOverrides(RaceTrial.Arg("--racer-abilities")),id,owner.Roster?.Seed??seed,collider.size*.5f);driverPolicy=racerPolicy;if(physicalRecovery!=null)physicalRecovery.OffsetTrigger=13;}
            // Tyre/rolling resistance is already calculated by DriveModel.
            // Sliding a box with the default contact friction adds an unintended
            // ground brake, disproportionately affecting stock equipment.
            collider.material=new PhysicsMaterial("Arcade tyre contact") {dynamicFriction=0,staticFriction=0,
                frictionCombine=PhysicsMaterialCombine.Minimum,bounciness=.05f,bounceCombine=PhysicsMaterialCombine.Minimum};
            ContactBottom=collider.center.y-size.y*.5f;
            if(race.track.hasElevation){ground=new RoadGrounding(this);ground.Sample();}
            // Free roam on the level Bayview road still crosses real hills: let the car pitch and
            // roll with the ground it sits on (races keep their locked axes). --free-roam-level restores.
            else if(race.freeRoam&&!RaceTrial.Has("--free-roam-level")){body.constraints=RigidbodyConstraints.None;tiltToGround=true;}
            // Visible weight transfer (visual only); it moves the wheel pivots, so before WheelVisuals.
            if(BodyMotion.Enabled&&!Application.isBatchMode)(GetComponent<BodyMotion>()??gameObject.AddComponent<BodyMotion>()).Collect(body);
            wheels=new WheelVisuals(gameObject);
            pathDistance=race.track.Nearest(transform.position,out offset);previousPath=pathDistance;
            progress=pathDistance>race.track.length*.8f?pathDistance-race.track.length:pathDistance;
            nextGate=0;nextGateProgress=0;lastSafeDistance=progress;
            // Sound (docs/47 stage 4): car 0 is the one the camera follows, heard as the player's.
            if(GetComponent<CarAudio>()==null)AudioDirector.Attach(this,id==0);
        }
        // Timed for the frame probe (FrameCost.Cars): every car's driving and AI step.
        // The physical box for a car body, fitted to that body: its model's length and height, and its
        // shoulder-and-tyre width (CarBodies.ShoulderWidth, mirrors excluded). It used to be one shared
        // envelope forced to 2.32 m wide: 27 cm of air each side of the Puddle, while the longer field bodies
        // sank up to 54 cm into each other at contact (contact review, 29 Sep). --collider-width overrides
        // the width for before/after reviews.
        public static (Vector3 centre,Vector3 size) CollisionBox(GameObject car)
        {
            Bounds envelope=new Bounds();bool firstVertex=true;
            foreach(var renderer in car.GetComponentsInChildren<Renderer>()) {
                if(renderer is TrailRenderer)continue;Bounds local=renderer.localBounds;
                for(int corner=0;corner<8;corner++) {
                    Vector3 p=local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    p=car.transform.InverseTransformPoint(renderer.transform.TransformPoint(p));
                    if(firstVertex){envelope=new Bounds(p,Vector3.zero);firstVertex=false;}else envelope.Encapsulate(p);
                }
            }
            envelope.Expand(.015f);Vector3 size=envelope.size;
            string body=car.name.Replace("(Clone)","").Trim();var shoulders=CarBodies.ShoulderWidth(body);var length=CarBodies.Length(body);
            if(shoulders.HasValue){size.x=shoulders.Value;envelope.center=new Vector3(0,envelope.center.y,envelope.center.z);}if(length.HasValue)size.z=length.Value;
            if(float.TryParse(RaceTrial.Arg("--collider-width"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float width))size.x=width;
            return (envelope.center,size);
        }
        // Manual shifts are presses, so read them every frame (FixedUpdate can miss or double a press).
        void Update()
        {
            if(controlsBlocked||gears==null||!gears.Manual)return;
            var k=Keyboard.current;var p=Gamepad.current;
            if(k!=null){if(k.leftShiftKey.wasPressedThisFrame)gears.RequestShift(1);if(k.leftCtrlKey.wasPressedThisFrame)gears.RequestShift(-1);}
            if(p!=null){if(p.rightTrigger.wasPressedThisFrame)gears.RequestShift(1);if(p.leftTrigger.wasPressedThisFrame)gears.RequestShift(-1);}
        }
        void FixedUpdate(){long start=System.Diagnostics.Stopwatch.GetTimestamp();try{Step();}finally{FrameCost.Cars+=System.Diagnostics.Stopwatch.GetTimestamp()-start;}}
        void Step()
        {
            if(race==null || !race.running) return;
            if(ground!=null)ground.Sample();
            float recoveryDatum=ground==null?0:race.track.At(pathDistance).y;
            // Free roam spans the island and the mainland: only the sea counts as falling out of the world.
            if(race.freeRoam&&body.position.y<(FreeRoamRoads.Count>0?CoastalLandscape.SeaLevel+1:recoveryDatum-3)){TryRecover();return;}
            Grip=0;
            // Kinematic while carried (the castle zipline): the carrier poses it; velocity can't be set.
            if(controlsBlocked){throttle=0;steering=0;speed=0;if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}return;}
            Vector3 driveForward=DriveForward,driveRight=ground==null?transform.right:ground.right,driveUp=ground==null?Vector3.up:ground.normal;
            speed=Vector3.Dot(body.linearVelocity,driveForward);
            float beforeProgress=progress,beforeOffset=offset;
            pathDistance=race.track.Nearest(body.position,out offset);
            float step=Mathf.Repeat(pathDistance-previousPath+race.track.length*.5f,race.track.length)-race.track.length*.5f;
            if(Mathf.Abs(step)<10)progress+=step;
            previousPath=pathDistance;
            if(GateFirst&&!race.freeRoam&&race.elapsed>=0&&Mathf.Abs(step)<10)TrackGates(beforeProgress,beforeOffset);
            if(race.DraftingEnabled) {
                var snapshot=race.DriverSnapshot;if(snapshot==null)throw new InvalidOperationException("Drafting requires a shared same-step observation.");
                var self=snapshot.Entries[driverId];
                DragReduction=DraftingModel.Reduction(in self,snapshot.Entries,out int leader,race.track.Length);DraftLeader=leader;
            }
            if(commandSource!=null){var command=commandSource(this);throttle=Mathf.Clamp(command.x,-1,1);steering=Mathf.Clamp(command.y,-1,1);}
            else if(autonomous || finished){long planStart=System.Diagnostics.Stopwatch.GetTimestamp();Plan();long planTicks=System.Diagnostics.Stopwatch.GetTimestamp()-planStart;FrameCost.Plan+=planTicks;if(planTicks>FrameCost.PlanMax)FrameCost.PlanMax=planTicks;} else ReadInput();
            // Held for a free-roam countdown (Beat the Train, the Castle Climb; docs/51): planted, but the engine revs.
            if(holdStill) {
                if(tuning.drivetrain!=null&&(gears==null||gears.train!=tuning.drivetrain))gears=new GearState(tuning.drivetrain);
                if(gears!=null){gears.Held=true;gears.Step(0,throttle,Time.fixedDeltaTime);gears.Held=false;}
                body.linearVelocity=new Vector3(0,Mathf.Min(0,body.linearVelocity.y),0);body.angularVelocity=Vector3.zero;speed=0;return;
            }
            if(race.elapsed<0) {throttle=0;steering=0;body.linearVelocity=Vector3.zero;return;}
            race.MotionAudit?.Observe(this,race.elapsed);
            if(autonomous&&!finished&&race.elapsed>0){float dt=Time.fixedDeltaTime;if(throttle<-.05f)BrakeSeconds+=dt;else if(throttle<.9f&&speed>3)LiftSeconds+=dt;if(speed<2)StoppedSeconds+=dt;}
            race.ApproachAudit?.Observe(this,race.elapsed);
            steerState=Mathf.MoveTowards(steerState,steering,tuning.response*Time.fixedDeltaTime);
            float grip=Mathf.Abs(offset)<Circuit.Width*.5f||(race.freeRoam&&OnRoadSurface())?tuning.roadGrip:tuning.dirtGrip;grip*=DriveModel.AeroGrip(tuning,speed);Grip=grip;
            float yawRequest=steerState*tuning.yaw*Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sign(speed==0?1:speed);
            float engineAccel=-1;
            if(tuning.drivetrain!=null) {
                if(gears==null||gears.train!=tuning.drivetrain)gears=new GearState(tuning.drivetrain);
                gears.Manual=ManualGears;
                // A recovery puts the car back on the road at rest: back into first (a manual box too).
                if(gearRecoveries!=recoveries){gearRecoveries=recoveries;gears.Reset();}
                engineAccel=gears.Step(speed,throttle,Time.fixedDeltaTime);
            } else gears=null;
            DriveModel.Forces(tuning,speed,throttle,yawRequest,grip,0,out float acceleration,out float yaw,DragReduction,engineAccel);
            // PhysX gravity and ground contact already produce the along-grade
            // component. Passing an explicit slope here would count it twice.
            requestedYaw=yaw;
            if(speed < -7 && acceleration<0)acceleration=0;
            bool supported=ground==null||ground.grounded;
            // Diagnostic (elevated circuits): racing time with no drive, brake or cornering force applied.
            if(!supported&&race.elapsed>0&&!finished)UnsupportedSeconds+=Time.fixedDeltaTime;
            if(supported)body.AddForce(driveForward*acceleration,ForceMode.Acceleration);
            // Downforce (S and E platforms; docs/47, 54): a push into the road with the square of the speed, so the crests
            // that threw the maximum build into the air at 60-75 m/s keep it planted. Near the road only (1.5 m).
            if(tuning.downforce>0&&(ground==null||ground.hasSupport&&ground.clearance<1.5f))
                body.AddForce(-(ground!=null&&ground.hasSupport?ground.normal:transform.up)*tuning.downforce*speed*speed,ForceMode.Acceleration);
            // Steering changes heading; lateral tyre force moves velocity toward
            // that heading within the available grip, preserving collision impulses.
            lateralSpeed=Vector3.Dot(body.linearVelocity,driveRight);
            if(supported)body.AddForce(-driveRight*Mathf.Clamp(lateralSpeed*8,-grip*1.65f,grip*1.65f),ForceMode.Acceleration);
            // A torque servo lets contact impulses resist steering. Directly
            // imposing a new rotation can push a corner into an adjacent car.
            if(supported)body.AddTorque(driveUp*Mathf.Clamp((yaw-Vector3.Dot(body.angularVelocity,driveUp))*20,-25,25),ForceMode.Acceleration);
            if(ground!=null)ground.Align();
            if(tiltToGround)TiltToGround();
            if(!race.freeRoam) {
                if(!GateFirst&&Mathf.Abs(step)<10)TrackGates(beforeProgress,beforeOffset);
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
            wheelSpin+=speed/wheels.Radius*Mathf.Rad2Deg*Time.fixedDeltaTime;   // (rolling: the tyre's edge moves with the road)
            wheels.Pose(wheelSpin,steerState*25);
        }
        // Garage body swap (looks only): the new model's visuals replace this car's. The collider,
        // mass and tuning stay, so only standard-envelope bodies (every one a body shop sells) qualify.
        public void ReplaceBody(string asset,Color paint)
        {
            if(!CarBodies.UsesStandardEnvelope(asset)||GetComponent<StandardEnvelope>()==null)throw new InvalidOperationException("Only standard-envelope bodies swap in place: "+asset);
            string current=name;var donor=TrialWorld.Car(asset,paint);
            var old=new System.Collections.Generic.List<Transform>();foreach(Transform child in transform)if(child.name==current||child.name.StartsWith(current+"_lod"))old.Add(child);
            foreach(var child in old){child.SetParent(null,false);child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var moved=new System.Collections.Generic.List<Transform>();foreach(Transform child in donor.transform)moved.Add(child);
            foreach(var child in moved)child.SetParent(transform,false);
            var donorGroup=donor.GetComponent<LODGroup>();var group=GetComponent<LODGroup>();
            if(donorGroup!=null){if(group==null)group=gameObject.AddComponent<LODGroup>();group.SetLODs(donorGroup.GetLODs());group.RecalculateBounds();}
            else if(group!=null)DestroyImmediate(group);
            Destroy(donor);name=asset;
            GetComponent<BodyMotion>()?.Collect(body);
            wheels=new WheelVisuals(gameObject);GetComponent<VehicleLights>()?.Recollect();
        }
        bool tiltToGround;readonly RaycastHit[] tiltHits=new RaycastHit[8];
        // Averages the surface normal under the four corners and servos the car's up vector to
        // it (upright when airborne), damping tilt so it follows slopes without rolling over.
        void TiltToGround()
        {
            Vector3 normals=Vector3.zero;int found=0;
            for(int axle=-1;axle<=1;axle+=2)for(int side=-1;side<=1;side+=2) {
                Vector3 origin=body.position+transform.forward*(axle*1.1f)+transform.right*(side*.8f)+transform.up*1.2f;
                int count=Physics.RaycastNonAlloc(origin,Vector3.down,tiltHits,3.5f,~0,QueryTriggerInteraction.Ignore);
                float nearest=float.MaxValue;Vector3 normal=Vector3.zero;
                for(int i=0;i<count;i++){var h=tiltHits[i];if(h.collider.transform.IsChildOf(transform)||h.collider.GetComponentInParent<RoadCar>()!=null||h.normal.y<.55f)continue;if(h.distance<nearest){nearest=h.distance;normal=h.normal;}}
                if(nearest<float.MaxValue){normals+=normal;found++;}
            }
            // Follow slopes up to 30 degrees; beyond that (cliff faces, outcrops) stay near upright.
            Vector3 target=Vector3.RotateTowards(Vector3.up,found>=2?normals.normalized:Vector3.up,30*Mathf.Deg2Rad,0);
            Vector3 tiltVelocity=Vector3.ProjectOnPlane(body.angularVelocity,transform.up);
            float righting=transform.up.y<.77f?160:70; // past ~40 degrees, push back hard
            body.AddTorque(Vector3.ClampMagnitude(Vector3.Cross(transform.up,target)*righting-tiltVelocity*12,righting<100?45:90),ForceMode.Acceleration);
        }
        void OnDestroy(){recoverySensor?.Dispose();}
        void ReadInput()
        {
            var k=Keyboard.current;var p=Gamepad.current;float gas=0,brake=0;steering=0;
            if(k!=null) {gas=k.wKey.isPressed||k.upArrowKey.isPressed?1:0;brake=k.sKey.isPressed||k.downArrowKey.isPressed?1:0;
                steering=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);}
            // Manual gears on a pad: the triggers shift (Update), so the right stick is throttle (up) and brake (down).
            bool manualPad=gears!=null&&gears.Manual;
            if(p!=null&&manualPad) {float stick=p.rightStick.y.ReadValue();if(stick>.08f)gas=Mathf.Max(gas,stick);else if(stick< -.08f)brake=Mathf.Max(brake,-stick);
                if(Mathf.Abs(p.leftStick.x.ReadValue())>.12f)steering=p.leftStick.x.ReadValue();}
            else if(p!=null) {gas=Mathf.Max(gas,p.rightTrigger.ReadValue());brake=Mathf.Max(brake,p.leftTrigger.ReadValue());
                if(Mathf.Abs(p.leftStick.x.ReadValue())>.12f)steering=p.leftStick.x.ReadValue();}
            throttle=gas-brake;
            // Manual box: in reverse the throttle drives backwards and the brake brakes (it never drives);
            // in a forward gear the brake holds the car at a stop instead of reversing.
            if(gears!=null&&gears.Manual) {
                if(gears.Gear<0||gears.TargetGear<0)throttle=brake>.05f?(speed< -.1f?brake:0):-gas;
                else if(throttle<0&&speed<=.1f)throttle=0;
            }
        }
        void Plan()
        {
            var stamp=race.DriverAudit!=null?DriverFrameAudit.Begin():default;
            int plansBefore=contextPolicy?.PlanCount??0;
            PlanObserved();
            race.DriverAudit?.RecordPlans((contextPolicy?.PlanCount??0)-plansBefore);
            race.DriverAudit?.Record(stamp,false);
        }
        public ForecastWork PreparedRoadContinuation=>contextPolicy?.PreparedContinuation;
        public ForecastWork PrepareRoadForecast(RaceWorldSnapshot snapshot)
        {
            if(contextPolicy==null||controlsBlocked||commandSource!=null||(!autonomous&&!finished)||physicalRecovery?.Active==true)return null;
            var observed=snapshot.Entries;
            var observation=new DriverObservation(snapshot.Timestamp,Time.fixedDeltaTime,observed[driverId],observed,race.track,tuning,pace,preferredOffset,finished,steerState,race.DraftingEnabled);
            return contextPolicy.PrepareForecast(in observation);
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
            var observation=new DriverObservation(timestamp,Time.fixedDeltaTime,self,observed,race.track,tuning,pace,preferredOffset,finished,steerState,race.DraftingEnabled);
            if(racerPolicy!=null)racerPolicy.Elapsed=race.elapsed;
            DriveCommand command;
            if(physicalRecovery!=null&&race.elapsed>=0&&!finished) {
                string previous=physicalRecovery.State;
                bool blocked=((trafficPolicy==null&&contextPolicy==null&&racerPolicy==null)||physicalRecovery.Active)&&recoverySensor.FrontBlocked(in observation,physicalRecovery.Active,referenceProfile?.line);
                physicalRecovery.Observe(in observation,progress,nextGateProgress,blocked,trafficPolicy?.WaitingForTraffic??contextPolicy?.WaitingForTraffic??racerPolicy?.WaitingForTraffic??false);
                if(physicalRecovery.NeedsPlan(timestamp)) {
                    physicalRecovery.Plan(in observation,progress,nextGateProgress,recoverySensor.Sample(in observation,steerState));
                }
                command=physicalRecovery.Active?physicalRecovery.Command(in observation):driverPolicy.Decide(in observation);
                if(previous!=physicalRecovery.State)Debug.Log($"PHYSICAL_RECOVERY id={driverId} time={race.elapsed:0.000} state={physicalRecovery.State} progress={progress:0.00} gate={nextGateProgress:0.00}");
            }else command=driverPolicy.Decide(in observation);
            throttle=command.pedal;steering=command.steering;targetSpeed=command.targetSpeed;
            if(SoundDrive.On&&driverId==0)throttle=SoundDrive.Pedal(this,throttle,race.elapsed);
            predictedOffset=command.predictedOffset;chosenLane=command.chosenLane;
        }
        void TrackGates(float beforeProgress,float beforeOffset)
        {
            if(finished)return;
            if(CheckpointCrossing.Forward(beforeProgress,progress,beforeOffset,offset,nextGateProgress,Circuit.Width*.5f+2)) {
                race.RecordCheckpoint(this,nextGateProgress);
                if(nextGate==0) {
                    if(nextGateProgress>race.track.length*.8f) {laps++;float lap=race.elapsed-lapStart;LastLapTime=lap;bestLap=Mathf.Min(bestLap,lap);
                        if(laps>=race.requiredLaps) {finished=true;finishTime=race.elapsed;race.Finish(this);}}
                    lapStart=race.elapsed;
                }
                nextGate=(nextGate+1)%12;
                nextGateProgress+=race.track.length/12;
            }
        }
        readonly RaycastHit[] surfaceHits=new RaycastHit[6];
        // Free roam: tarmac off the Bayview loop (causeway, country roads) is marked with RoadSurface.
        public bool OnRoadSurface()
        {
            int count=Physics.RaycastNonAlloc(body.position+Vector3.up,Vector3.down,surfaceHits,2.6f,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)if(surfaceHits[i].collider.GetComponent<RoadSurface>()!=null)return true;
            return false;
        }
        public bool TryRecover()
        {
            if(race.freeRoam&&FreeRoamRoads.Nearest(body.position,out var roadPoint,out var roadForward)) {
                // Nearest road of any kind (Bayview loop, causeway, mainland), unless a car is parked on it.
                Vector3 target=roadPoint+Vector3.up*.2f;
                foreach(var other in race.cars)if(other!=this&&Vector3.Distance(other.body.position,target)<5)return false;
                body.position=target;body.rotation=Quaternion.LookRotation(new Vector3(roadForward.x,0,roadForward.z));
                body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;stuckTime=0;recoveries++;return true;
            }
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
        // Adventure speech bubbles listen for bumps: (other collider, closing speed).
        public event Action<Collider,float> Bumped;
        void OnCollisionEnter(Collision c)
        {
            ObserveContact(c);Bumped?.Invoke(c.collider,c.relativeVelocity.magnitude);
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

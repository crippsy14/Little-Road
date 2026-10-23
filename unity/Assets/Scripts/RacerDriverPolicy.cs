using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Racing AI v2 (docs/46-racing-ai-v2-design.md), selected with --racer-v2. Four layers:
    //   persona   RacerAbilities scale every number below;
    //   strategy  a small intent machine (Launch, Race, Attack, Defend) with memory and named exits;
    //   tactics   a heat line across the track picks the lateral target (Melder, Game AI Pro ch. 41);
    //   speed     one arbitration per tick: limit line ahead, Gipps following, close guard, with the
    //             binding reason recorded;
    //   control   pure pursuit on the planned offset path plus a kinematic pedal feed-forward.
    // Car physics, checkpoints and PhysicalRecovery are shared with the first AI.
    public sealed class RacerDriverPolicy:IDriverPolicy
    {
        public enum Intent {Launch,Race,Attack,Defend}
        public enum Reason {Limit,Follow,Guard,Launch,Finished}
        const int Bins=57;const float BinLow=-7,BinStep=.25f,Wall=50;
        readonly float HalfRoad;   // (the circuit's half-width: 8 m on a standard circuit, less on Bramblewick's lanes)

        readonly RacerTrack track;
        readonly RacerAbilities ability;
        readonly int id;
        readonly uint seed;
        readonly float halfWidth,halfLength;
        readonly float[] heat=new float[Bins],smooth=new float[Bins];
        readonly float[] allowed=new float[160],raw=new float[220],path=new float[220+2*Span];
        // Lateral bending is measured over +-Span samples (about 8 m): the steering look-ahead rounds off
        // shorter kinks, such as where a planned line meets the road edge.
        const int Span=4;

        // Race clock from RoadCar (negative during the countdown).
        public float Elapsed;
        public Intent State {get;private set;}=Intent.Launch;
        public Reason Binding {get;private set;}=Reason.Launch;
        public bool WaitingForTraffic {get;private set;}
        // Telemetry for race.json (docs/46: attacks, passes, aborts, hesitation and its reason).
        public int Attacks,Passes,Aborts,DefendMoves,Reversals;
        // Why attacks end without a pass: target lost or out of reach, timeout, side closed, no overlap by the braking point.
        public int AbortLost,AbortTimeout,AbortSideClosed,AbortNoOverlap;
        public float TrafficSeconds,TrafficBrakeSeconds,GuardSeconds;

        int tick,target=-1,planned;
        float delta,startColumn,launchDelay,travelled,pedalOut;
        float intentSince,cooldownUntil,defendUntil,followingSince=-1,sideClosedSince=-1,side;
        float lastChange,lastChangeDirection;
        bool started;
        float desired; // speed the limit line allows here, from the last tick
        int allowedCount; // samples in allowed[] from the last tick
        // The speed we could carry at a point `ahead` metres up the road (our last limit profile), so a car
        // is judged slow only against what we could do at the same spot.
        float DesiredAt(float ahead){int k=Mathf.RoundToInt(ahead/track.Step);return k>=0&&k<=allowedCount?allowed[k]:desired;}
        // Observed acceleration of every car (smoothed), so a follower can anticipate a leader pulling away.
        readonly float[] seenSpeed=new float[64],seenAccel=new float[64];

        static readonly float? Hold=float.TryParse(RaceTrial.Arg("--racer-hold"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float hold)?hold:null;
        // Tuning sweeps (--racer-corner-scale, --racer-brake-scale): multiply every driver's grip and brake use.
        static float Scale(string key){return float.TryParse(RaceTrial.Arg(key),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float v)?v:1;}
        // Aero cars (S and E, docs/54): at 60-75 m/s a car a little over its plan into a turning braking zone steers
        // the tyres' whole budget away from the brakes (the yaw takes up to 95% of grip) and the overspeed snowballs
        // into the run-off. Their plan keeps a reserve when braking and turning together: the lateral load counts
        // (1 + reserve) times, so trail braking leaves the brakes some grip. Straight-line braking is unchanged.
        // --aero-reserve <r> (default 0.3; 0 = none) for A/B. Class C-B-A cars have no downforce: untouched.
        static readonly float AeroReserve=float.TryParse(RaceTrial.Arg("--aero-reserve"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float aeroReserve)?aeroReserve:.3f;
        static readonly float AeroCornerUse=Scale("--aero-corner-use");
        // --racer-debug <id>: log that driver's speed decision four times a second.
        static readonly int DebugId=int.TryParse(RaceTrial.Arg("--racer-debug"),out int debugId)?debugId:-1;
        // --traffic-corner-margin (docs/70; on with the field recipe, so by default): with a car close ahead or alongside, corner at no more than 92% of the
        // steady limit (the old roster's best Line, 0.85, used 91.8%). A field of Line ~1.0 drivers cornering at 95%
        // nose to tail on one line crashed at Bayview's first corner and piled up; in clear air Line still counts in full.
        // Grip budget (docs/71): steering and braking share one grip budget (DriveModel.Forces: the yaw takes up to 95%
        // of grip and the brakes get what's left). A lane change on the brakes into a hairpin used the grip the braking
        // plan counted on, so the car arrived 5 m/s fast and understeered off at full lock, with the plan asking for
        // throttle. With it: lane changes are paced to the grip braking and the corner leave, and at full lock above
        // 12 m/s the car lifts (and brakes if over its plan).
        // The default since 8 Oct (Richard: "Lets do both"; study 222 equal pace, 40% fewer contacts); --no-grip-budget
        // races without it.
        static readonly bool GripBudget=!RaceTrial.Has("--no-grip-budget");
        float lastAccel;
        public static float LaneChangeDistance(float shift,float v,float spare)=>Mathf.Sqrt(6*shift*v*v/Mathf.Max(.5f,spare));
        static readonly bool TrafficCornerMargin=RaceTrial.Has("--traffic-corner-margin")||FieldRecipe.On&&!RaceTrial.Has("--no-traffic-corner-margin");
        const float TrafficCornerCap=.92f;
        static readonly float CornerScale=Scale("--racer-corner-scale"),BrakeScale=Scale("--racer-brake-scale"),ReactionScale=Scale("--racer-reaction-scale"),GapScale=Scale("--racer-gap-scale"),LeaderBrakeScale=Scale("--racer-leader-brake-scale");
        // Racecraft v3 (docs/94, 22 Oct; --racecraft-v3, off by default). Study 256: in a 24-car field the finishing order
        // was the grid order (5-11 passes a race from 120-190 attacks; a 10% faster club car from the back passed one car),
        // because the lane target moved 3-6 m nearly every second (380-670 reversals a race), so no move completed, and an
        // attack sat beside its target until the 9 s timeout or the door closed at the apex (70% of aborts). Traffic bound
        // the speed 58-62% of the race at 6-8 m/s below the car's own plan; full-ability drivers were only 2% faster.
        // Three parts, each droppable with --racecraft-v3-without <commit|attack|gap> for diagnosis:
        //   commit  the chosen lane is kept until it is blocked, clearly worse, or a dwell has passed (AcceptLaneChange);
        //   attack  an attack owns the lane beside its target (no heat roll), gives a closing door 1.5 s to reopen, never
        //           aborts for it while overlapping (the target must leave a car's width), and a closed door costs a short
        //           cool-down rather than Patience's;
        //   gap     the target of an attack is followed on a shorter reaction and gap, and not at all once we are beside it
        //           (the close guard still brakes for bodies).
        public static readonly bool RacecraftV3=RaceTrial.Has("--racecraft-v3");
        static bool Dropped(string part){var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="--racecraft-v3-without"&&args[i+1]==part)return true;return false;}
        public static readonly bool V3Commit=RacecraftV3&&!Dropped("commit"),V3Attack=RacecraftV3&&!Dropped("attack"),V3Gap=RacecraftV3&&!Dropped("gap");
        // A lane change is a move of LaneShift or more from the shift held at the last accepted change (so a lane can't be
        // crossed in small steps). It waits LaneDwell seconds and needs LaneGain of heat; LaneBigGain (a slow car's hill in
        // our lane) or a blocked lane moves at once.
        public const float LaneShift=1.5f,LaneDwell=1.2f,LaneGain=1f,LaneBigGain=4f;
        public static bool AcceptLaneChange(float gain,float sinceLastChange,bool blocked)=>blocked||gain>=LaneBigGain||(sinceLastChange>=LaneDwell&&gain>=LaneGain);
        // Telemetry: seconds lost behind traffic (deficit to our own plan over our speed), seconds attacking, lane changes.
        public float TrafficLossSeconds,AttackSeconds;public int LaneChanges;
        float laneAnchor,attackLane;bool attackLaneValid;
        public RacerDriverPolicy(RacingLine line,RacerAbilities abilities,int driverId,int raceSeed,Vector3 colliderHalf)
        {
            track=RacerTrack.For(line);HalfRoad=line.halfRoad;MaxLine=line.MaxOffset;ability=abilities;id=driverId;
            seed=unchecked((uint)raceSeed*2654435761u^(uint)(driverId+1)*40503u);
            halfWidth=colliderHalf.x;halfLength=colliderHalf.z;
            launchDelay=ability.LaunchDelay+.04f*Sample(7,0);
        }

        public DriveCommand Decide(in DriverObservation observation)
        {
            var me=observation.self;var tuning=observation.tuning;var route=observation.route;
            float dt=observation.stepSeconds,v=Mathf.Max(0,me.speed),s=me.route,o=me.offset;
            tick++;
            track.EnsureRoad(route);
            foreach(var other in observation.traffic){int k=other.id&63;float a=(other.speed-seenSpeed[k])/Mathf.Max(dt,.001f);seenAccel[k]=tick==1?0:Mathf.Lerp(seenAccel[k],Mathf.Clamp(a,-30,15),.1f);seenSpeed[k]=other.speed;}
            if(!started){started=true;startColumn=o;delta=o-track.LineOffset(s);}
            if(Elapsed>0)travelled+=v*dt;

            // Strategy and tactics run about ten times a second, staggered across the field.
            if(tick==1||(tick+id*5)%12==0)Plan(in observation,v);

            float edge=Edge;
            float lateralDistance=Mathf.Max(6,Mathf.Abs(PathTarget(s,0,edge)-o)/(ability.LateralRate*Mathf.Clamp(tuning.response/5,.7f,1.25f))*Mathf.Max(v,6));
            if(GripBudget) {
                // A smoothstep move of `shift` metres over L peaks at 6*shift*v^2/L^2 of sideways acceleration: keep it
                // inside what last tick's braking and this point's corner leave of the grip.
                float grip0=tuning.roadGrip*1.65f*track.Load(track.Index(s),v),brakeNeed=Mathf.Max(0,-lastAccel),corner0=v*v*Mathf.Abs(track.LineCurvature(track.Index(s)));
                float spare=Mathf.Max(1.5f,Mathf.Sqrt(Mathf.Max(0,grip0*grip0*.81f-brakeNeed*brakeNeed))-corner0);
                lateralDistance=Mathf.Max(lateralDistance,LaneChangeDistance(Mathf.Abs(PathTarget(s,0,edge)-o),v,spare));
            }

            // Speed: one decision, one reason.
            float spell=Spell();
            float budget=tuning.roadGrip*1.65f;
            float brakeUse=ability.BrakeUse*BrakeScale*spell*(1+ability.BrakeScatter*Sample(3,Mathf.FloorToInt(Mathf.Repeat(s,track.Length)/50)));
            if(State==Intent.Attack&&OverlapInside(in observation))brakeUse+=ability.LateBrake;
            brakeUse=Mathf.Min(brakeUse,.995f);
            float cornerUse=ability.CornerUse*CornerScale*spell*(tuning.downforce>0?AeroCornerUse:1);
            if(TrafficCornerMargin&&InPack(in observation,s,o))cornerUse=Mathf.Min(cornerUse,TrafficCornerCap*CornerScale*(tuning.downforce>0?AeroCornerUse:1));
            bool offRoad=Mathf.Abs(o)>HalfRoad;
            float preview=1+v*.12f;
            float limitSpeed=LimitSpeed(tuning,s,o,v,edge,lateralDistance,budget,cornerUse,brakeUse,offRoad,preview,out float allowedHere);
            float accelLimit=(limitSpeed*limitSpeed-v*v)/(2*preview);
            var reason=Reason.Limit;float accel=accelLimit;desired=allowedHere;

            float brake=Mathf.Min(tuning.braking,budget)*brakeUse;
            float follow=FollowAcceleration(in observation,s,o,v,edge,lateralDistance,brake,out int leader);
            if(follow<accel){accel=follow;reason=Reason.Follow;}
            float guard=GuardAcceleration(in observation,out float nudge);
            if(guard<accel){accel=guard;reason=Reason.Guard;}

            if(observation.finished){accel=Mathf.Min(accel,(18-v)*2);reason=Reason.Finished;}
            if(Elapsed<launchDelay)reason=Reason.Launch;
            Binding=reason;
            if(id==DebugId&&tick%30==0) {
                float k0=track.OffLineCurvature(track.Index(s),s,o,0,8),kp=track.OffLineCurvature(track.Index(s+20),s+20,PathOffset(s,o,20,edge,lateralDistance),0,8);
                Debug.Log($"RACER_DEBUG id={id} t={Elapsed:0.00} s={s:0.0} o={o:0.00} v={v:0.0} limit={limitSpeed:0.0} here={allowedHere:0.0} reason={reason} state={State} delta={delta:0.00} lineHere={track.LineOffset(s):0.00} line20={track.LineOffset(s+20):0.00} kLine={track.LineCurvature(track.Index(s)):0.0000} kRoad={track.RoadCurvature(s):0.0000} k0={k0:0.0000} k20={kp:0.0000} path20={PathOffset(s,o,20,edge,lateralDistance):0.00} lat={lateralDistance:0.0} accel={accel:0.0}");
            }
            if(Elapsed>0&&!observation.finished) {
                if(reason==Reason.Follow){TrafficSeconds+=dt;if(accel<-.5f)TrafficBrakeSeconds+=dt;}
                if(reason==Reason.Guard)GuardSeconds+=dt;
                if(reason==Reason.Follow||reason==Reason.Guard)TrafficLossSeconds+=dt*Mathf.Min(3,Mathf.Max(0,allowedHere-v)/Mathf.Max(v,3));
                if(State==Intent.Attack)AttackSeconds+=dt;
            }
            WaitingForTraffic=(reason==Reason.Follow||reason==Reason.Guard)&&v<3;

            // Control: pure pursuit on the planned offset path.
            float look=Mathf.Clamp(5+v*.4f,6,19);
            float aimOffset=Mathf.Clamp(PathOffset(s,o,look,edge,lateralDistance)+nudge,-edge,edge);
            var aim=route.At(s+look)+route.Right(s+look)*aimOffset;var toAim=aim-me.position;
            // Steer from the direction of travel, not the nose: near the grip limit the car slides a few
            // degrees, and aiming from the nose would under-steer more the harder it corners.
            // (Only while the slide is small: after a knock the nose is what steering turns.)
            var travel=me.velocity;travel.y=0;var nose=me.forward;nose.y=0;
            var heading=v>4&&travel.sqrMagnitude>16&&Vector3.Angle(travel,nose)<12?travel:me.forward;
            float angle=Vector3.SignedAngle(heading,toAim,Vector3.up)*Mathf.Deg2Rad;
            float curve=2*Mathf.Sin(angle)/Mathf.Max(3,toAim.magnitude);
            float steering=Mathf.Clamp(curve*Mathf.Max(v,3)*1.09f/tuning.yaw,-1,1);
            if(Mathf.Abs(angle)>.9f){steering=Mathf.Sign(angle);accel=Mathf.Min(accel,(8-v)*2);}

            float wheel=accel+DriveModel.Resistance(tuning,v);
            float pedal=wheel>=0?wheel/Mathf.Max(.01f,DriveModel.EngineForce(tuning,v)):wheel/tuning.braking;
            // Below the limit line, full power: the braking recursion guarantees we can still slow in time.
            if(reason==Reason.Limit&&(limitSpeed>v+.5f||limitSpeed>=tuning.AttainableSpeed-.15f))pedal=1;
            // Braking: a cautious driver lifts and coasts well before a corner that needs slowing for.
            if(pedal>0&&reason==Reason.Limit&&ability.CoastDistance>1) {
                int kc=Mathf.Min(allowedCount,Mathf.RoundToInt(ability.CoastDistance/track.Step));
                for(int k=0;k<=kc;k++)if(allowed[k]<v-1){pedal=0;break;}
            }
            // Control: a less assured driver feeds the throttle in gently while still turning.
            if(pedal>0&&Mathf.Abs(track.LineCurvature(track.Index(s)))>1/90f)pedal=Mathf.Min(pedal,ability.CornerThrottle);
            // Following: small slowdowns lift and coast (drag alone is ~4 m/s^2 at speed) rather than dab the brakes.
            if(reason==Reason.Follow&&wheel<0&&wheel>-2)pedal=0;
            // Understeer (--grip-budget): at full lock at speed, no throttle; over the plan, brake with what's left.
            if(GripBudget&&v>12&&Mathf.Abs(steering)>.97f)pedal=v>Mathf.Min(limitSpeed,allowedHere)+.5f?Mathf.Min(pedal,-.6f):Mathf.Min(pedal,0);
            pedal=Mathf.Clamp(pedal,-1,1);
            lastAccel=accel;
            // Never select reverse: at a standstill a brake request would drive the car backwards.
            if(v<.5f&&pedal<0||reason==Reason.Launch)pedal=0;
            // Control: how quickly the throttle goes back in. Lifting and braking are immediate.
            pedalOut=pedal>pedalOut&&pedal>0?Mathf.Min(pedal,Mathf.Max(0,pedalOut)+ability.PedalRise*dt):pedal;
            Record(s,o,v,limitSpeed,allowedHere,aimOffset,steering,pedalOut,reason,observation.finished);
            return new DriveCommand(pedalOut,steering,Mathf.Min(limitSpeed,allowedHere),PathOffset(s,o,look,edge,lateralDistance),PathTarget(s,PreviewDistance(v),edge));
        }

        // ---------- Off-road recorder (docs/71) ----------
        // The last 3 s of every car, and the moment it puts its centre off the asphalt: speed against its own plan,
        // where it aimed, its steering and pedal. One RACER_EXCURSION line per departure (re-armed once back on), so a
        // race tells us whether cars leave the road too fast for the corner or at the right speed but steering wide.
        struct Trace{public float t,s,o,v,limit,here,aim,steer,pedal,kLine;public Reason why;}
        readonly Trace[] trace=new Trace[400];int traceAt;bool excursion;
        public int Excursions{get;private set;}
        void Record(float s,float o,float v,float limit,float here,float aim,float steer,float pedal,Reason why,bool finished)
        {
            trace[traceAt++%trace.Length]=new Trace{t=Elapsed,s=s,o=o,v=v,limit=limit,here=here,aim=aim,steer=steer,pedal=pedal,kLine=track.LineCurvature(track.Index(s)),why=why};
            bool off=Mathf.Abs(o)>HalfRoad+.5f;
            if(off&&!excursion&&!finished&&Elapsed>1) {
                Excursions++;var lines=new System.Text.StringBuilder($"RACER_EXCURSION id={id} t={Elapsed:0.00} lap_s={Mathf.Repeat(s,track.Length):0.0} o={o:0.00} v={v:0.0} here={here:0.0} limit={limit:0.0} state={State}");
                for(int back=360;back>=0;back-=12){int k=traceAt-1-back;if(k<0)continue;var x=trace[k%trace.Length];
                    lines.Append($"\n  t={x.t:0.00} s={Mathf.Repeat(x.s,track.Length):0.0} o={x.o:0.00} aim={x.aim:0.00} v={x.v:0.0} plan={x.here:0.0} limit={x.limit:0.0} steer={x.steer:0.00} pedal={x.pedal:0.00} kLine={x.kLine:0.0000} why={x.why}");}
                Debug.Log(lines.ToString());
            }
            excursion=off||excursion&&Mathf.Abs(o)>HalfRoad-.5f;
        }

        // ---------- Speed ----------

        // Backward braking recursion over the stopping window along the planned path, using the planned
        // path's own curvature measured against the road (the outside of a hairpin is tighter than a
        // parallel to the racing line would suggest). Returns the allowed speed at `preview`.
        float LimitSpeed(CarTuning tuning,float s,float o,float v,float edge,float lateralDistance,float budget,float cornerUse,float brakeUse,bool offRoad,float preview,out float here)
        {
            float step=track.Step;
            float window=Mathf.Clamp(v*v/(2*Mathf.Max(3,budget*.5f*brakeUse))+30,30,step*(allowed.Length-2));
            int count=Mathf.CeilToInt(window/step);
            float attainable=tuning.AttainableSpeed;
            float next=attainable;
            // Bending is read from the planned line itself; the short correction from where the car is back
            // onto it is the steering's job, not a corner.
            // Braking skill: a cautious braker wants to be down to a corner's speed `margin` metres before it.
            int margin=Mathf.Min(40,Mathf.RoundToInt(ability.BrakeMargin/step));
            for(int k=-Span;k<=count+margin+Span;k++)path[k+Span]=PathTarget(s,k*step,edge);
            float span=Span*step;
            for(int k=count+margin;k>=0;k--) {
                float x=k*step;int i=track.Index(s+x);
                float kappa=track.OffLineCurvature(i,s+x,PathOffset(s,o,x,edge,lateralDistance),(path[Mathf.Min(k+2*Span,path.Length-1)]-2*path[k+Span]+path[k])/(span*span),span);
                float grip=budget*track.Load(i,k>count?attainable:next),aero=DriveModel.Aero(tuning);
                if(offRoad&&x<12)grip*=tuning.dirtGrip/Mathf.Max(.01f,tuning.roadGrip);
                raw[k]=Mathf.Min(attainable,CornerSpeed(grip,cornerUse,kappa,aero));
                if(k>count)continue;
                float corner=raw[k];for(int j=1;j<=margin;j++)corner=Mathf.Min(corner,raw[k+j]);
                float lateral=next*next*Mathf.Abs(kappa)*(aero>0||GripBudget?1+AeroReserve:1);   // (--grip-budget: every car keeps the trail-braking reserve, docs/71)grip*=1+aero*next*next;
                float traction=Mathf.Sqrt(Mathf.Max(0,grip*grip-lateral*lateral));
                float decel=Mathf.Max(.5f,Mathf.Min(tuning.braking,traction)*brakeUse+DriveModel.Resistance(tuning,next)+track.Gravity(i));
                float value=Mathf.Min(corner,Mathf.Sqrt(next*next+2*decel*step));
                allowed[k]=value;next=value;
            }
            here=allowed[0];allowedCount=count;
            float at=Mathf.Clamp(preview/step,0,count);int a=Mathf.FloorToInt(at);
            return Mathf.Lerp(allowed[a],allowed[Mathf.Min(count,a+1)],at-a);
        }

        // A car within 25 m ahead or 6 m behind, within 4 m across: racing in a pack, not in clear air.
        bool InPack(in DriverObservation observation,float s,float o)
        {
            foreach(var other in observation.traffic) {
                if(other.id==observation.self.id)continue;
                float rel=track.Relative(other.route,s);if(rel>-6&&rel<25&&Mathf.Abs(other.offset-o)<4)return true;
            }
            return false;
        }

        // Gipps safe following speed behind the nearest car in our planned lane: follow close at speed and
        // only brake when the gap really runs out (reaction time from Racecraft, standstill gap from Aggression).
        float FollowAcceleration(in DriverObservation observation,float s,float o,float v,float edge,float lateralDistance,float brake,out int leader)
        {
            leader=-1;float best=float.MaxValue;var me=observation.self;
            // At a standing start the whole grid accelerates together: follow on a shorter headway.
            bool launch=State==Intent.Launch;
            float tau=ability.Reaction*ReactionScale*(launch?.5f:1),b=Mathf.Max(3,brake),bh=b*1.05f*LeaderBrakeScale;
            foreach(var other in observation.traffic) {
                if(other.id==me.id)continue;
                float rel=track.Relative(other.route,s);if(rel<=0||rel>110)continue;
                float gap=rel-halfLength-other.halfLength;
                // Stopped behind a stationary car: judge the lane we are steering into, so we can creep round
                // it instead of waiting for it (and it for us).
                float mine=v<2&&other.speed<1?PathTarget(s,rel,edge):PathOffset(s,o,rel,edge,lateralDistance);
                if(Mathf.Abs(mine-other.offset)>=halfWidth+other.halfWidth+.3f)continue;
                // Racecraft v3 (gap): the car we are attacking is followed on 60% of the reaction time and half the standstill
                // gap, and not at all once we are beside it (its width all but cleared): the close guard brakes for bodies.
                bool attacking=V3Gap&&State==Intent.Attack&&other.id==target;
                if(attacking&&Mathf.Abs(o-other.offset)-halfWidth-other.halfWidth>=0)continue;
                float tauHere=attacking?tau*.75f:tau;
                // Racecraft: read a leader that is pulling away (the launch, a corner exit) instead of
                // assuming it may stop dead this instant.
                float lead=Mathf.Max(0,other.speed+Mathf.Max(0,seenAccel[other.id&63])*tauHere);
                float g=gap-ability.Standstill*GapScale*(launch?.6f:1)*(attacking?.7f:1);
                float term=b*b*tauHere*tauHere+b*(2*g-v*tauHere+lead*lead/bh);
                float safe=term<=0?0:-b*tauHere+Mathf.Sqrt(term);
                float accel=(safe-v)/Mathf.Max(tauHere,.25f);
                if(accel<best){best=accel;leader=other.id;}
            }
            return best;
        }

        // Close range, world space: brake to cancel the closing speed before contact with a body ahead in
        // our lane, and nudge away from a car alongside that is drifting into us.
        float GuardAcceleration(in DriverObservation observation,out float nudge)
        {
            var me=observation.self;nudge=0;float best=float.MaxValue;
            var forward=me.forward;forward.y=0;forward.Normalize();var right=new Vector3(forward.z,0,-forward.x);
            foreach(var other in observation.traffic) {
                if(other.id==me.id)continue;
                var d=other.position-me.position;d.y=0;if(d.sqrMagnitude>16*16)continue;
                float dx=Vector3.Dot(d,forward),dy=Vector3.Dot(d,right);
                var rv=other.velocity-me.velocity;float rx=Vector3.Dot(rv,forward),ry=Vector3.Dot(rv,right);
                float gx=Mathf.Abs(dx)-halfLength-other.halfLength,gy=Mathf.Abs(dy)-halfWidth-other.halfWidth;
                if(dx>0&&gy<.2f&&rx<0) {
                    float closing=-rx;float room=Mathf.Max(.15f,gx-.35f);
                    if(room/closing<.8f){float accel=-closing*closing/(2*room);if(accel<best)best=accel;}
                }
                // Alongside (or about to be) and the side gap closing within half a second: ease away.
                float soon=gy+Mathf.Min(0,Mathf.Sign(dy)*ry)*.5f;
                if(gx<1&&soon<.6f)nudge-=Mathf.Sign(dy)*(.6f-Mathf.Max(-.3f,soon))*1.5f;
            }
            nudge=Mathf.Clamp(nudge,-1.2f,1.2f);
            return best;
        }

        // ---------- Strategy and tactics ----------

        void Plan(in DriverObservation observation,float v)
        {
            var me=observation.self;float s=me.route;float clock=Elapsed;
            float edge=Edge;
            var corner=NextCorner(observation.tuning,s,v);
            UpdateIntent(in observation,v,clock,corner,edge);

            float xp=PreviewDistance(v),sp=s+xp,lineAt=track.LineOffset(sp);
            float lineWeight=State==Intent.Launch?.15f:.6f;
            float apex=ability.ApexError*SmoothSample(5,sp,60);
            float held=Mathf.Clamp(lineAt+delta,-edge,edge);
            for(int i=0;i<Bins;i++) {
                float at=BinLow+i*BinStep;
                float h=lineWeight*Mathf.Abs(at-(lineAt+apex));
                if(Mathf.Abs(at)>edge)h+=100;
                if(State==Intent.Launch)h+=1.6f*Mathf.Clamp01(1-clock/3.5f)*Mathf.Abs(at-startColumn);
                h-=.3f*Mathf.Exp(-(at-held)*(at-held)/(2*.6f*.6f));
                heat[i]=h;
            }
            if(State==Intent.Defend)Hill(DefendOffset(corner,edge),.6f,-2.5f);

            attackLaneValid=false;
            foreach(var other in observation.traffic) {
                if(other.id==me.id)continue;
                float rel=track.Relative(other.route,s);if(rel<-16||rel>75)continue;
                float reach=halfLength+other.halfLength,relSpeed=v-other.speed;
                // Where it will be: its shift from the racing line, carried to our preview point (the line's own
                // sideways movement is already in that; adding raw sideways speed double-counts it).
                float otherAt=Mathf.Clamp(lineAt+other.offset-track.LineOffset(other.route),-HalfRoad,HalfRoad);
                // Side room: the driver's clearance, never under 0.45 m (steering wobble in a corner is ~0.3 m).
                float width=halfWidth+other.halfWidth+Mathf.Max(.45f,ability.Clearance);
                if(Mathf.Abs(rel)<reach+.5f+Mathf.Max(0,-relSpeed)*.3f){Hill(otherAt,width,100);continue;}
                if(rel>0) {
                    float gap=rel-reach,closing=v-other.speed;
                    float time=closing>.1f?gap/closing:float.PositiveInfinity;
                    // Slower than we want to go: its lane costs us, so look for another before we are stuck behind it.
                    float deficit=DesiredAt(rel)-Mathf.Max(0,other.speed);
                    if(time<3.5f||gap<6)Hill(otherAt,width,Mathf.Lerp(1.5f,8,Mathf.Clamp01((3.5f-time)/3))+Mathf.Clamp01((deficit-1.5f)/4)*3);
                    else if(deficit>1.5f&&gap<60)Hill(otherAt,width,Mathf.Clamp01((deficit-1.5f)/4)*Mathf.Clamp01((60-gap)/45)*5);
                    else if(gap<30&&Mathf.Abs(track.LineCurvature(track.Index(sp)))<.004f)Hill(otherAt,.5f,-.4f);
                } else {
                    float gap=-rel-reach,closing=other.speed-v;
                    // A faster car coming past: don't move across it.
                    if(closing>.5f&&gap/closing<.9f)Hill(otherAt,width,30);
                    else if(closing>.5f&&gap/closing<2)Hill(otherAt,width,8);
                }
                if(State==Intent.Attack&&other.id==target) {
                    attackLane=Mathf.Clamp(otherAt+side*(width+.3f),-edge,edge);attackLaneValid=true;
                    Hill(attackLane,.9f,-3.5f);
                    Hill(otherAt-side*width,1,3);
                }
            }
            for(int i=0;i<Bins;i++)smooth[i]=heat[i]>=Wall?heat[i]:(heat[Mathf.Max(0,i-1)]+heat[i]*2+heat[Mathf.Min(Bins-1,i+1)])*.25f;

            // Roll downhill from where we are heading now; a car alongside is a wall we cannot cross.
            int from=Bin(held),lo=from,hi=from;
            // Boxed in (a car's predicted body covers our spot): step out to the nearest free side first.
            // Boxed hold (study 210): only a sidestep (2.5 m); further is a dive through or across that car
            // (Bayview 1710, studies 208-209: 10 m across the road into the bridge corner). With no free spot
            // that close, hold the line; following and braking deal with the car.
            bool boxedHold=false;
            if(smooth[from]>=Wall) {
                int limit=BoxedHold?BoxedStepBins:Bins-1,start=from;
                for(int r=1;r<=limit;r++){if(from-r>=0&&smooth[from-r]<Wall){from-=r;break;}if(from+r<Bins&&smooth[from+r]<Wall){from+=r;break;}}
                boxedHold=BoxedHold&&from==start;
            }
            lo=hi=from;
            if(smooth[from]<Wall){while(lo>0&&smooth[lo-1]<Wall)lo--;while(hi<Bins-1&&smooth[hi+1]<Wall)hi++;}else{lo=0;hi=Bins-1;}
            // Settle into the bottom of our own valley; jump to a different valley only for a clear gain.
            int local=from;
            while(true){int next=local;if(local>lo&&smooth[local-1]<smooth[next])next=local-1;if(local<hi&&smooth[local+1]<smooth[next])next=local+1;if(next==local)break;local=next;}
            int best=local;for(int i=lo;i<=hi;i++)if(smooth[i]<smooth[best])best=i;
            if(best!=local&&smooth[best]>smooth[local]-.8f)best=local;
            if(boxedHold)best=from;
            // Racecraft v3 (attack): an attack owns the lane beside its target, when that lane is in our wall-free range
            // (a third car there, and the heat decides as before).
            int attackBin=Bin(attackLane);
            bool owned=V3Attack&&State==Intent.Attack&&attackLaneValid&&attackBin>=lo&&attackBin<=hi&&smooth[attackBin]<Wall;
            if(owned)best=attackBin;
            // Committed through a corner: once braking for it or turning in, keep the chosen line (a metre of
            // adjustment) unless a car's body is in the way.
            bool cornering=corner.found&&corner.brakeStart<v*.5f||Mathf.Abs(track.LineCurvature(track.Index(s)))>1/60f;
            if(cornering&&smooth[from]<Wall)best=Mathf.Clamp(best,from-4,from+4);
            float chosen=BinLow+best*BinStep;
            // Racecraft v3 (commit): a lane change (LaneShift from the anchor) is kept back unless AcceptLaneChange says so;
            // the lane is then held exactly (not re-rounded to a bin, which would walk it).
            if(V3Commit&&!owned&&Mathf.Abs(chosen-lineAt-laneAnchor)>=LaneShift) {
                int current=Bin(held);
                if(AcceptLaneChange(smooth[current]-smooth[best],clock-lastChange,smooth[current]>=Wall||boxedHold)){laneAnchor=chosen-lineAt;lastChange=clock;}
                else chosen=held;
            }
            float newDelta=chosen-lineAt;
            float change=newDelta-delta;
            if(Mathf.Abs(change)>1.5f) {
                if(Mathf.Sign(change)==-lastChangeDirection&&clock-lastChange<3)Reversals++;
                lastChange=clock;lastChangeDirection=Mathf.Sign(change);laneAnchor=newDelta;LaneChanges++;
            }
            delta=newDelta;planned++;
            // Test fixture (--racer-hold <metres>): drive the whole lap at a fixed shift from the racing line.
            if(Hold.HasValue)delta=Hold.Value;
        }

        void UpdateIntent(in DriverObservation observation,float v,float clock,in Corner corner,float edge)
        {
            var me=observation.self;float s=me.route;
            if(State==Intent.Launch) {
                // Hold the grid column only while the field gets rolling; then faster cars may pick gaps.
                if(clock>3.5f||travelled>70)Enter(Intent.Race,clock);
                return;
            }
            if(observation.finished){if(State!=Intent.Race)Enter(Intent.Race,clock);return;}
            // The car directly ahead in our lane, and anyone closing from behind.
            ObservedCar ahead=default;bool hasAhead=false;float aheadRel=float.MaxValue;
            ObservedCar behind=default;bool hasBehind=false;float behindRel=float.MinValue;
            ObservedCar targetCar=default;bool hasTarget=false;
            foreach(var other in observation.traffic) {
                if(other.id==me.id)continue;
                float rel=track.Relative(other.route,s);
                if(other.id==target){targetCar=other;hasTarget=true;}
                bool lane=Mathf.Abs(other.offset-me.offset)<halfWidth+other.halfWidth+.8f;
                if(rel>0&&rel<aheadRel&&lane){ahead=other;aheadRel=rel;hasAhead=true;}
                if(rel<0&&rel>behindRel&&rel>-22){behind=other;behindRel=rel;hasBehind=true;}
            }
            float reachAhead=hasAhead?halfLength+ahead.halfLength:0;
            bool stuck=hasAhead&&aheadRel-reachAhead<14&&v-ahead.speed<1.5f;
            if(stuck){if(followingSince<0)followingSince=clock;}else followingSince=-1;
            // Racecraft v3 (attack): is the car ahead free, or itself following (a car within 15 m ahead of it, within 3.5 m
            // across)? In a train every car's plan is faster than the pace, so "my plan beats its speed" says nothing about
            // who is quicker; only a free car's speed is its own pace.
            bool targetFree=true;
            if(hasAhead&&V3Attack)foreach(var other in observation.traffic) {
                if(other.id==me.id||other.id==ahead.id)continue;
                float rel=track.Relative(other.route,ahead.route);
                if(rel>0&&rel<15&&Mathf.Abs(other.offset-ahead.offset)<3.5f){targetFree=false;break;}
            }

            switch(State) {
                case Intent.Race:
                    if(hasAhead&&clock>=cooldownUntil&&aheadRel-reachAhead<24) {
                        float closingNow=v-ahead.speed,surplus=.7f*(DesiredAt(aheadRel)-ahead.speed),closingAny=Mathf.Max(closingNow,surplus);
                        bool corners=corner.found&&corner.distance<140;
                        bool straightDraft=!corners&&aheadRel<18;
                        // Racecraft: only go for it with a real speed advantage (equal cars rarely complete a pass,
                        // and each pull-out and abort costs time); patience decides how long a small edge must wait.
                        // Racecraft v3: a car in a train is attacked on the real closing speed. (Counting the plan surplus once
                        // held behind it for Patience's wait was tried on 23 Oct, study 261: attacks 362 -> 410, passes 43 -> 42,
                        // contacts 55 -> 66, total 20 s worse over three Hillcrest races. Everyone in a train has a surplus.)
                        bool held=followingSince>=0&&clock-followingSince>ability.FollowBeforeTrying;
                        float closing=!V3Attack||targetFree?closingAny:closingNow;
                        float edgeNeeded=2.4f-1.2f*ability.aggression;
                        bool want=closing>edgeNeeded||(closing>edgeNeeded*.4f&&held&&(corners||straightDraft));
                        if(want&&PickSide(in observation,ahead,corner,edge,me.offset,v,aheadRel,out float chosenSide)) {
                            target=ahead.id;side=chosenSide;sideClosedSince=-1;Attacks++;Enter(Intent.Attack,clock);return;
                        }
                    }
                    if(hasBehind&&clock>=defendUntil&&corner.found&&corner.brakeStart>v*1.2f&&corner.distance<220) {
                        float gap=-behindRel-halfLength-behind.halfLength,closing=behind.speed-v;
                        bool alongside=gap<.5f;
                        if(!alongside&&gap<10&&closing>.3f&&Sample(11,Mathf.FloorToInt(clock/4))*.5f+.5f<ability.racecraft*(.4f+.6f*ability.aggression)) {
                            DefendMoves++;defendUntil=clock+7;Enter(Intent.Defend,clock);return;
                        }
                    }
                    break;
                case Intent.Attack: {
                    if(!hasTarget){AbortLost++;Abort(clock);return;}
                    float rel=track.Relative(targetCar.route,s),reach=halfLength+targetCar.halfLength;
                    float lateral=Mathf.Abs(me.offset-targetCar.offset)-halfWidth-targetCar.halfWidth;
                    if(rel<-reach*.6f&&lateral>-.2f){Passes++;cooldownUntil=clock+.5f;Enter(Intent.Race,clock);return;}
                    if(rel<-reach*1.5f){Passes++;cooldownUntil=clock+.5f;Enter(Intent.Race,clock);return;}
                    if(rel-reach>35){AbortLost++;Abort(clock);return;}
                    if(clock-intentSince>9){AbortTimeout++;Abort(clock);return;}
                    float room=side>0?edge-(targetCar.offset+targetCar.halfWidth):(targetCar.offset-targetCar.halfWidth)+edge;
                    bool overlap=rel-reach<.5f;
                    // Racecraft v3 (attack): the door closing is the target's move (its line reaching the apex), not a failed
                    // attack: 1.5 s for it to reopen, never while we overlap, and a 1 s cool-down rather than Patience's.
                    if(room<halfWidth*2+.15f&&!(V3Attack&&overlap)){if(sideClosedSince<0)sideClosedSince=clock;if(clock-sideClosedSince>(V3Attack?1.5f:.5f)){AbortSideClosed++;Abort(clock,V3Attack?1f:ability.AttackCooldown);return;}}
                    else sideClosedSince=-1;
                    if(corner.found&&corner.brakeStart<0&&!overlap&&v-targetCar.speed<1){AbortNoOverlap++;Abort(clock);return;}
                    break;
                }
                case Intent.Defend: {
                    bool attackerAlongside=hasBehind&&-behindRel-halfLength-behind.halfLength<.5f;
                    if(attackerAlongside||!hasBehind||(corner.found&&corner.brakeStart<v*.3f)||clock-intentSince>3.5f)Enter(Intent.Race,clock);
                    break;
                }
            }
        }

        void Abort(float clock)=>Abort(clock,ability.AttackCooldown);
        void Abort(float clock,float cooldown){Aborts++;cooldownUntil=clock+cooldown;target=-1;Enter(Intent.Race,clock);}
        void Enter(Intent next,float clock){if(next!=Intent.Attack)target=-1;State=next;intentSince=clock;}

        // Attack the inside of the next corner; on a straight, the side with more room.
        // Pass reach guard (study 209, default; --no-pass-reach-guard): only a side the car can move across to
        // before the braking point. Boxed hold (study 210, default; --no-boxed-hold): see Plan.
        public static readonly bool PassReachGuard=!RaceTrial.Has("--no-pass-reach-guard");
        public static readonly bool BoxedHold=!RaceTrial.Has("--no-boxed-hold");
        public const int BoxedStepBins=10; // 2.5 m
        public const float PassLateralRate=2.5f;
        public static bool CanReachSide(float cross,float brakeStart,float speed)=>cross<=1.5f+PassLateralRate*Mathf.Max(0,brakeStart)/Mathf.Max(speed,1);
        bool PickSide(in DriverObservation observation,in ObservedCar car,in Corner corner,float edge,float myOffset,float speed,float aheadRel,out float chosen)
        {
            float need=halfWidth*2+.15f;
            float right=edge-(car.offset+car.halfWidth),left=(car.offset-car.halfWidth)+edge;
            bool guard=PassReachGuard&&corner.found&&corner.distance<150;
            float laneRight=car.offset+car.halfWidth+halfWidth+.3f,laneLeft=car.offset-car.halfWidth-halfWidth-.3f;
            // A side is open when we can reach it before braking and (racecraft v3) its lane is clear to a car length past
            // the target: an attack into an occupied lane ends beside the target with nowhere to go, a stalemate that slows
            // both and blocks the lane behind (study 257: everyone diving for the inside stopped the pack at Bayview's bridge).
            bool openRight=(!guard||CanReachSide(Mathf.Abs(laneRight-myOffset),corner.brakeStart,speed))&&(!V3Attack||LaneClear(in observation,car,laneRight,aheadRel));
            bool openLeft=(!guard||CanReachSide(Mathf.Abs(laneLeft-myOffset),corner.brakeStart,speed))&&(!V3Attack||LaneClear(in observation,car,laneLeft,aheadRel));
            chosen=corner.found&&corner.distance<150?corner.sign:(right>=left?1:-1);
            float room=chosen>0?right:left;
            if(room>=need&&(chosen>0?openRight:openLeft))return true;
            chosen=-chosen;room=chosen>0?right:left;
            return room>=need+.4f&&!(corner.found&&corner.distance<60)&&(chosen>0?openRight:openLeft);
        }
        // No other car's body in `lane` from our own tail to 12 m past the target. (Study 261, 23 Oct: also requiring the next car
        // in the lane within 45 m to be pulling away from the target, and ending an attack once the car ahead in our own lane ran
        // level with it, turned the 9 s timeouts into early aborts but cost passes, 43 -> 29, and the total gain, −1.0% -> −0.4%:
        // a car that gives up tucks in and coasts, while running alongside kept the pack wider and quicker. Kept as round two.)
        bool LaneClear(in DriverObservation observation,in ObservedCar target,float lane,float aheadRel)
        {
            var me=observation.self;
            foreach(var other in observation.traffic) {
                if(other.id==me.id||other.id==target.id)continue;
                float rel=track.Relative(other.route,me.route);
                if(rel<-halfLength-other.halfLength||rel>aheadRel+12)continue;
                if(Mathf.Abs(other.offset-lane)<halfWidth+other.halfWidth+.3f)return false;
            }
            return true;
        }

        float DefendOffset(in Corner corner,float edge)=>Mathf.Clamp(corner.sign*Mathf.Min(3.2f,edge-1),-edge,edge);

        bool OverlapInside(in DriverObservation observation)
        {
            if(target<0)return false;var me=observation.self;
            foreach(var other in observation.traffic) {
                if(other.id!=target)continue;
                float rel=track.Relative(other.route,me.route);
                return Mathf.Abs(rel)<halfLength+other.halfLength+.5f&&Mathf.Sign(me.offset-other.offset)==side;
            }
            return false;
        }

        struct Corner {public bool found;public float distance,brakeStart,sign,speed;}
        // The next corner that needs braking from the current speed: where it is, which way it turns and
        // how far away its braking starts.
        Corner NextCorner(CarTuning tuning,float s,float v)
        {
            var result=new Corner();float grip=tuning.roadGrip*1.65f;
            float brake=Mathf.Max(3,Mathf.Min(tuning.braking,tuning.roadGrip*1.65f)*ability.BrakeUse);
            float reference=Mathf.Max(v,12);
            for(float x=0;x<260;x+=track.Step*2) {
                int i=track.Index(s+x);float kappa=track.LineCurvature(i);
                float limit=CornerSpeed(grip,ability.CornerUse,kappa,DriveModel.Aero(tuning));
                if(limit<reference*.92f) {
                    result.found=true;result.distance=x;result.sign=Mathf.Sign(kappa);result.speed=limit;
                    result.brakeStart=x-Mathf.Max(0,v*v-limit*limit)/(2*brake);return result;
                }
            }
            return result;
        }

        // Fastest steady speed on curvature `kappa`. The car's yaw is capped at 95% of grip, and its steering
        // servo settles about 0.045 rad/s short of the request (angular damping), which costs 0.045*v of
        // lateral grip: v^2*k = use*(0.95*grip - 0.045*v). With aero grip (grip x (1+aero*v^2), docs/54) the
        // v^2 term on the grip side moves across: v^2*(k - use*0.95*grip*aero) = ... (none at all once it covers k).
        public static float CornerSpeed(float grip,float use,float kappa,float aero=0)
        {
            float k=Mathf.Abs(kappa)-use*.95f*grip*aero;if(k<=.00001f)return 999;
            float b=use*.045f,c=use*.95f*grip;
            return (-b+Mathf.Sqrt(b*b+4*k*c))/(2*k);
        }

        // ---------- Path ----------

        // Planned lines stay well inside the point where grass (and recovery) starts.
        readonly float MaxLine;
        // --narrow-pass: on a circuit narrower than the standard 16 m, traffic and passing may use the road out to its own edge
        // (less the car's half-width and a margin), not just the racing line's corridor. On Bramblewick's 8 m lanes the
        // corridor left 1.5 m beside a car in the middle, under the 2.05 m a pass needs, so 94% of attacks were abandoned
        // ("side closed"). The line itself still keeps its corridor; 16 m circuits are untouched. REJECTED (study 234): passes
        // 28 -> 37 but finishes fell to 20/24 and 18/24, a reset, contacts doubled. Kept off; 9 m lanes won instead.
        public static readonly bool NarrowPass=RaceTrial.Has("--narrow-pass");
        float Edge=>NarrowPass&&MaxLine<5.6f?HalfRoad-halfWidth-.25f:Mathf.Min(HalfRoad-halfWidth-.25f,MaxLine);
        float PreviewDistance(float v)=>Mathf.Clamp(v*.9f,10,35);
        // Target offset from the road centre `x` metres ahead: the chosen shift from the racing line, kept
        // on the usable road.
        float PathTarget(float s,float x,float edge)=>Mathf.Clamp(track.LineOffset(s+x)+delta,-edge,edge);
        // The planned path: a smooth move from the current offset to the target over `distance`.
        float PathOffset(float s,float o,float x,float edge,float distance)
        {
            float t=Mathf.Clamp01(x/distance);t=t*t*(3-2*t);
            return Mathf.Lerp(o,PathTarget(s,x,edge),t);
        }

        void Hill(float centre,float half,float height)
        {
            for(int i=0;i<Bins;i++) {
                float d=Mathf.Abs(BinLow+i*BinStep-centre);
                // Only the body itself is a wall; its soft edge stays crossable.
                if(d<half)heat[i]+=height;else if(d<half+1)heat[i]+=Mathf.Min(height,20)*(1-(d-half));
            }
        }
        static int Bin(float offset)=>Mathf.Clamp(Mathf.RoundToInt((offset-BinLow)/BinStep),0,Bins-1);

        // ---------- Persona noise (seeded per driver and race) ----------

        // Off spells: a square wave with a low duty cycle (Tomlinson and Melder, Game AI Pro ch. 38).
        float Spell()
        {
            float period=24+10*(Sample(1,0)*.5f+.5f),phase=(Sample(2,0)*.5f+.5f)*period;
            float duty=.08f+.1f*(1-ability.consistency);
            return Mathf.Repeat(Elapsed+phase,period)<duty*period?1-ability.SpellDip:1;
        }
        float SmoothSample(int channel,float s,float block)
        {
            float at=Mathf.Repeat(s,track.Length)/block;int k=Mathf.FloorToInt(at);float t=at-k;t=t*t*(3-2*t);
            int blocks=Mathf.Max(1,Mathf.FloorToInt(track.Length/block));
            return Mathf.Lerp(Sample(channel,k%blocks),Sample(channel,(k+1)%blocks),t);
        }
        // Deterministic value in [-1,1] for this driver, race, channel and index.
        float Sample(int channel,int index)
        {
            unchecked {
                uint h=seed^(uint)channel*0x9E3779B9u^(uint)index*0x85EBCA6Bu;
                h^=h>>16;h*=0x7feb352d;h^=h>>15;h*=0x846ca68b;h^=h>>16;
                return (h>>8)*(2f/16777215f)-1f;
            }
        }
    }

    // Per racing line data shared by every v2 driver in a race: line offsets and curvature by road
    // distance, plus grade and vertical load when the grade model is on (Hillcrest).
    public sealed class RacerTrack
    {
        // One per line (per-car lines, docs/71: a race can hold several), rebuilt if the grade model changes.
        static readonly System.Collections.Generic.Dictionary<RacingLine,RacerTrack> cached=new();static bool cachedGrade;
        public readonly float Length,Step;
        readonly RacingLine line;
        readonly float[] offset,curvature,grade,vertical;
        readonly bool graded;
        public static RacerTrack For(RacingLine line)
        {
            bool grade=GradeModel.Enabled&&GradeModel.Physics;
            if(cachedGrade!=grade||cached.Count>32){cached.Clear();cachedGrade=grade;}
            if(!cached.TryGetValue(line,out var t)){t=new RacerTrack(line,grade);cached[line]=t;}
            return t;
        }
        RacerTrack(RacingLine line,bool useGrade)
        {
            this.line=line;Length=line.roadLength;Step=line.roadStep;int n=line.Count;
            offset=(float[])line.data.offsets.Clone();curvature=(float[])line.curvature.Clone();
            grade=new float[n];vertical=new float[n];
            if(useGrade) {
                for(int i=0;i<n;i++){var d=line.points[(i+1)%n]-line.points[i];float run=new Vector2(d.x,d.z).magnitude;grade[i]=run>.01f?d.y/run:0;if(grade[i]!=0)graded=true;}
                for(int i=0;i<n;i++){int prev=(i+n-1)%n;float ds=Mathf.Max(.1f,(line.segmentLength[prev]+line.segmentLength[i])*.5f);vertical[i]=(grade[i]-grade[prev])/ds;}
            }
        }
        public int Index(float s)=>line.Index(s,out _);
        public float LineOffset(float s){int i=line.Index(s,out float f);return Mathf.Lerp(offset[i],offset[(i+1)%offset.Length],f);}
        // Curvature of a path running `d` metres to the right of the racing line (positive = right turn).
        public float LineCurvature(int i)=>curvature[i];
        // Signed road-centre curvature by road distance (positive = right turn), sampled 4 m either side.
        float[] road;
        public void EnsureRoad(IDriverRoute route)
        {
            if(road!=null)return;var values=new float[offset.Length];
            for(int i=0;i<values.Length;i++) {
                float s=i*Step;var a=route.At(s-4);var b=route.At(s);var c=route.At(s+4);
                var before=new Vector3(b.x-a.x,0,b.z-a.z);var after=new Vector3(c.x-b.x,0,c.z-b.z);
                values[i]=2*Vector3.Cross(before,after).y/Mathf.Max(.001f,before.magnitude*after.magnitude*(before+after).magnitude);
            }
            road=values;
        }
        public float RoadCurvature(float s){int i=line.Index(s,out float f);return Mathf.Lerp(road[i],road[(i+1)%road.Length],f);}
        // Curvature of a path `o` metres right of the road centre whose offset bends by `o2` per metre squared:
        // a parallel to the road plus the path's own lateral bending.
        // Curvature of a planned path: the racing line's own (baked) curvature, plus the difference that
        // being off the line makes, measured against the road. Exact on the line; honest far from it.
        public float OffLineCurvature(int i,float s,float o,float o2,float span)
        {
            float line0=LineOffset(s),line2=(LineOffset(s+span)-2*line0+LineOffset(s-span))/(span*span);
            return curvature[i]+PathCurvature(s,o,o2)-PathCurvature(s,line0,line2);
        }
        public float PathCurvature(float s,float o,float o2)
        {
            float k=RoadCurvature(s),scale=Mathf.Max(.3f,1-k*o);
            return k/scale+o2/(scale*scale);
        }
        public float Load(int i,float v)=>graded?GradeModel.Load(v,vertical[i]):1;
        // Along-track gravity that helps braking uphill (positive) and hurts it downhill.
        public float Gravity(int i)=>graded?GradeModel.Gravity*grade[i]/Mathf.Sqrt(1+grade[i]*grade[i]):0;
        // Signed along-track distance from `from` to `to`, wrapped to half a lap.
        public float Relative(float to,float from)=>Mathf.Repeat(to-from+Length*.5f,Length)-Length*.5f;
    }
}

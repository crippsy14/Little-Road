using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// ai-pace-experiments point 2: leader limit judged against the committed path.
public static class PathAwareLeaderChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Path-aware leader: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var follower=new ContextPathFollower(profile,false);
        float d=course.regionStarts[13]+190;var forward=road.Forward(d);
        var self=new ObservedCar(0,road.At(d),forward*18,forward,road.Right(d),d,0,18,1.2f,2.3f);
        float ld=d+25;var lf=road.Forward(ld);
        var lead=new ObservedCar(1,road.At(ld),lf*12,lf,road.Right(ld),ld,0,12,1.2f,2.3f);
        var o=new DriverObservation(1,1f/120,self,new[]{lead},road,CarTuning.Club,1,0,false);
        var requested=new DriveCommand(1,0,33,0,0);
        var legacy=MovingLeaderControl.Apply(in o,requested,out int legacyLeader,out float legacyLimit,out bool legacyLimited);
        ContextPathState none=default;
        var nullPlanned=MovingLeaderControl.Apply(in o,requested,null,in none,out int nullLeader,out float nullLimit,out bool nullLimited);
        Check(legacyLimited&&legacyLeader==1&&nullLimited&&nullLeader==1&&nullLimit==legacyLimit&&nullPlanned.pedal==legacy.pedal,"Without a committed path the original current-offset rule is unchanged");
        var pass=RoadPathFeasibility.FromMotion(in o,4,false);
        Check(Mathf.Abs(follower.PlannedOffset(in pass,0)-self.offset)<.05f&&Mathf.Abs(follower.PlannedOffset(in pass,200)-4)<.05f,"Planned offset starts at the car and reaches the path destination");
        MovingLeaderControl.Apply(in o,requested,follower,in pass,out int passLeader,out _,out bool passLimited);
        Check(!passLimited&&passLeader==-1,"A committed pass that is clear before the catch point is not capped by the car it is passing");
        var hold=RoadPathFeasibility.FromMotion(in o,0,false);
        MovingLeaderControl.Apply(in o,requested,follower,in hold,out int holdLeader,out _,out bool holdLimited);
        Check(holdLimited&&holdLeader==1,"A path that stays behind the slower car keeps the following limit");
        var near=new ObservedCar(1,road.At(d+10),road.Forward(d+10)*10,road.Forward(d+10),road.Right(d+10),d+10,0,10,1.2f,2.3f);
        var close=new DriverObservation(2,1f/120,self,new[]{near},road,CarTuning.Club,1,0,false);
        var closePass=RoadPathFeasibility.FromMotion(in close,4,false);
        MovingLeaderControl.Apply(in close,requested,follower,in closePass,out _,out _,out bool closeLimited);
        Check(closeLimited,"A pass that cannot move clear before contact keeps the following limit");
        var drifting=new ObservedCar(1,road.At(ld),lf*12+road.Right(ld)*3,lf,road.Right(ld),ld,0,12,1.2f,2.3f);
        var drift=new DriverObservation(3,1f/120,self,new[]{drifting},road,CarTuning.Club,1,0,false);
        MovingLeaderControl.Apply(in drift,requested,follower,in pass,out _,out _,out bool driftLimited);
        Check(driftLimited,"A leader drifting into the passing side keeps the following limit");
        var matched=new ObservedCar(1,road.At(ld),lf*18,lf,road.Right(ld),ld,0,18,1.2f,2.3f);
        var level=new DriverObservation(4,1f/120,self,new[]{matched},road,CarTuning.Club,1,0,false);
        var levelLegacy=MovingLeaderControl.Apply(in level,requested,out _,out float levelLimit,out bool levelLegacyLimited);
        var levelPass=MovingLeaderControl.Apply(in level,requested,follower,in pass,out _,out float levelPassLimit,out bool levelPassLimited);
        Check(levelPassLimited==levelLegacyLimited&&levelPassLimit==levelLimit&&levelPass.pedal==levelLegacy.pedal,"No closing speed keeps ordinary following");
        return checks;
    }
}

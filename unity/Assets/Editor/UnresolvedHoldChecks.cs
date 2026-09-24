using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// ai-pace-experiments step 2: --unresolved-hold response to TrafficUnresolved.
public static class UnresolvedHoldChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Unresolved hold: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var follower=new ContextPathFollower(profile,false);
        float d=course.regionStarts[13]+190;var forward=road.Forward(d);var right=road.Right(d);
        DriverObservation At(int frame,Vector3 lateralVelocity)
        {
            var self=new ObservedCar(0,road.At(d)+right*2,forward*8+lateralVelocity,forward,right,d,2,8,1.2f,2.3f);
            return new DriverObservation(frame,1f/120,self,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        }
        var steady=At(1,Vector3.zero);
        var planned=new DriveCommand(.6f,.4f,33,1.5f,2);
        var brake=ContextDriverPolicy.UnresolvedBrakeCommand(in planned);
        Check(brake.pedal==-1&&brake.steering==0&&brake.targetSpeed==0&&brake.predictedOffset==1.5f&&brake.chosenLane==2,"Flag off keeps the full-brake, zero-steering response");
        var hold=ContextDriverPolicy.UnresolvedHoldCommand(in steady,follower,in planned);
        Check(hold.pedal<=0&&hold.targetSpeed<=steady.self.speed,"Holding never adds power or raises the target speed");
        Check(hold.pedal>brake.pedal,"Below the reference speed, holding coasts instead of braking fully");
        Check(hold.predictedOffset==1.5f&&hold.chosenLane==2,"Holding keeps the planned offset diagnostics");
        var opposite=ContextDriverPolicy.UnresolvedHoldCommand(in steady,follower,new DriveCommand(-1,-1,5,1.5f,2));
        Check(opposite.steering==hold.steering,"Holding ignores the unresolved crossing's steering");
        var toRight=ContextDriverPolicy.UnresolvedHoldCommand(At(2,right*1.5f),follower,in planned);
        var toLeft=ContextDriverPolicy.UnresolvedHoldCommand(At(3,-right*1.5f),follower,in planned);
        Check((toRight.steering-hold.steering)*(toLeft.steering-hold.steering)<0,"Lateral drift either way is steered back toward the held offset");
        // --conditional-hold gate: self at offset 2 doing 8 m/s along the road.
        var selfCar=steady.self;
        ObservedCar Other(int id,float ahead,float offset,float speed)=>
            new ObservedCar(id,road.At(d+ahead)+road.Right(d+ahead)*offset,road.Forward(d+ahead)*speed,road.Forward(d+ahead),road.Right(d+ahead),d+ahead,offset,speed,1.2f,2.3f);
        bool Safe(params ObservedCar[] others)
        {
            var all=new ObservedCar[others.Length+1];all[0]=selfCar;Array.Copy(others,0,all,1,others.Length);
            return ContextDriverPolicy.UnresolvedHoldSafe(new DriverObservation(4,1f/120,selfCar,all,road,CarTuning.Club,1,0,false));
        }
        Check(Safe(),"Conditional hold is allowed in clear road");
        Check(!Safe(Other(1,15,2,0)),"Conditional hold brakes for a stopped car 1.3 s ahead in our corridor");
        Check(Safe(Other(1,40,2,0)),"Conditional hold continues when a stopped car is more than 3 s ahead");
        Check(Safe(Other(1,15,2,8)),"Conditional hold continues behind a car at matching speed");
        Check(Safe(Other(1,15,-3,0)),"Conditional hold ignores a stopped car outside our lateral corridor");
        Check(!Safe(Other(1,1,3,8)),"Conditional hold brakes with a car overlapping us lengthwise in our corridor");
        Check(Safe(Other(1,-15,2,20)),"Conditional hold ignores cars behind");
        return checks;
    }
}

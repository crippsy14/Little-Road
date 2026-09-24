using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DriverIdentityChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Driver identity: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();var data=new DriverIdentity.Data{schema=1,drivers=new DriverIdentity.Definition[24]};
        for(int i=0;i<24;i++)data.drivers[i]=DriverIdentity.At(i);
        DriverIdentity.Validate(data);var original=data.drivers[1].id;data.drivers[1].id=data.drivers[0].id;
        bool rejected=false;try{DriverIdentity.Validate(data);}catch(ArgumentException){rejected=true;}
        Require(rejected,"duplicate IDs accepted");data.drivers[1].id=original;
        foreach(float invalid in new[]{-.1f,1.1f,float.NaN}){var invalidDefinition=DriverIdentity.At(1);invalidDefinition.consistency=invalid;rejected=false;try{new DriverIdentity(invalidDefinition,1,true);}catch(ArgumentException){rejected=true;}Require(rejected,"invalid consistency accepted");}
        var mutable=DriverIdentity.At(1);var identity=new DriverIdentity(mutable,1709,true);float before=identity.Planning.BrakeFraction;mutable.braking=.55f;
        Require(identity.Planning.BrakeFraction==before&&DriverIdentity.At(1).braking!=.55f,"mutable definition changed race/catalogue");
        checks.Add("Driver roster validates stable unique identities/ranges and race parameters own their definition values");

        double mean=0,steadyVariance=0,variableVariance=0;
        for(int seed=0;seed<4096;seed++) {
            var a=DriverIdentity.ForRace(1,seed,"steady",true);var b=DriverIdentity.ForRace(1,seed,"variable",true);
            float draw=DriverIdentity.SignedSample(a.Id,seed,"race-form-v1");
            _=DriverIdentity.SignedSample("unrelated",seed,"cosmetics");
            var replay=DriverIdentity.ForRace(1,seed,"steady",true);
            Require(a.FormSample==draw&&a.FormSample==b.FormSample&&a.Planning.Same(in replay.Planning),"form depends on external draws");
            Require(Mathf.Abs(a.Planning.BrakeFraction-.72f)<=.003001f&&Mathf.Abs(b.Planning.BrakeFraction-.72f)<=.024001f,"form exceeds declared symmetric support");
            mean+=draw;steadyVariance+=Math.Pow(a.Planning.BrakeFraction-.72f,2);variableVariance+=Math.Pow(b.Planning.BrakeFraction-.72f,2);
        }
        Require(Math.Abs(mean/4096)<.04&&variableVariance/steadyVariance>63&&variableVariance/steadyVariance<65,"form distribution lost intended mean/spread");
        for(int i=23;i>=0;i--)Require(DriverIdentity.ForRace(i,1709,"mixed",true).FormSample==new DriverIdentity(DriverIdentity.At(i),1709,true).FormSample,"list order affects form");
        checks.Add("4096 seeded form samples reproduce independently of order/cosmetics; symmetric support and8x amplitude are preserved (not a lap-variance claim)");

        var neutral=DriverIdentity.ForRace(0,1709,"neutral",false);var old=new DriverPlanningSettings(.75f,5);
        Require(neutral.Planning.BrakeFraction==old.BrakeFraction&&neutral.Planning.PassingClearance==.35f&&neutral.FormAmplitude==0,"neutral changed baseline parameters");
        Require(DriverIdentity.ForRace(0,1709,"neutral",true).FormAmplitude==0,"upper-limit benchmark clips form");
        var course=new TechnicalCircuit();var road=course.track;var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);var tuning=CarTuning.Club;
        var oldProfile=new RacingSpeedProfile(line,tuning,old);var neutralProfile=new RacingSpeedProfile(line,tuning,neutral.Planning);
        for(int i=0;i<line.Count;i++)Require(oldProfile.speed[i]==neutralProfile.speed[i]&&oldProfile.acceleration[i]==neutralProfile.acceleration[i],"neutral changes physical speed envelope");
        checks.Add("Neutral profile reproduces existing speed arrays and .35m passing spare space exactly; form cap has no hidden clipping");

        float d=course.regionStarts[13]+100;var f=road.Forward(d);var r=road.Right(d);
        var self=new ObservedCar(0,road.At(d),f*20,f,r,d,0,20,1.2f,2.3f);
        var rival=new ObservedCar(1,road.At(d+15),f*10,f,r,d+15,0,10,1.2f,2.3f);
        var traffic=new[]{rival};var o=new DriverObservation(10,1f/120,self,traffic,road,tuning,1,0,false);
        ContextPathState prior=default;
        foreach(int risk in new[]{0,5,10}) {
            var planning=new DriverPlanningSettings(.73f,risk,true);var profile=new RacingSpeedProfile(line,tuning,planning);
            var planner=new DynamicRejoinPlanner(profile,true,true);planner.PrepareTraffic(in o,in prior,false);bool left=false,right=false;
            float expected=2.4f+planning.PassingClearance;
            for(int i=0;i<planner.Count;i++){var p=planner.Path(i);left|=!p.guide&&Mathf.Abs(p.endOffset+expected)<.00001f;right|=!p.guide&&Mathf.Abs(p.endOffset-expected)<.00001f;}
            Require(left&&right,"risk does not change actual passing proposals");
            Require(self.halfWidth==1.2f&&rival.halfWidth==1.2f,"risk changed collision footprint");
        }
        checks.Add("Risk0/5/10 create actual dynamic passing proposals with .8/.35/.1m spare space, leaving body geometry unchanged");
        var path=RoadPathFeasibility.FromMotion(in o,0,false);var half=new Vector3(1.2f,.6f,2.3f);
        var request=new ForecastRequest(in o,in path,Vector3.zero,half,planning:old);
        var changed=new ForecastRequest(in o,in path,Vector3.zero,half,planning:neutral.Planning);
        Require(!request.Matches(in changed),"passing style omitted from forecast identity");
        rejected=false;try{changed.Run(new ContextPathPrediction(oldProfile));}catch(InvalidOperationException){rejected=true;}
        Require(rejected,"forecast accepted a mismatched personality");
        checks.Add("Owned forecast identity rejects different driver-style state even when numerical neutral margins coincide");
        return checks;
    }
}

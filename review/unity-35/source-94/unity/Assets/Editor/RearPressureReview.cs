using System;
using System.Collections.Generic;
using System.IO;
using LittleRoad;
using UnityEngine;

// Constructed observations, not a replay of the user's video or live race.
public static class RearPressureReview
{
    [Serializable] public sealed class Row
    {
        public float gap,side,rearSpeed,clearTarget,target,clearPedal,pedal,clearSteering,steering,followerPedal,afterLeaderPedal;
        public int relevant,rearApproaches,leader;public bool adjacent,roadUnresolved;
    }
    [Serializable] public sealed class Report
    {
        public string scope="Constructed first-decision straight observations with clear road ahead. Same physical road checks and current controller; no scene obstacles or subsequent vehicle integration. Demonstrates possible rear influence, not causality for a specific watched incident or live lap-time cost.";
        public Row[] rows;
    }
    public static void Run()
    {
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Street,default(DriverPlanningSettings).WithRisk(5));
        float d=course.regionStarts[13]+190;var f=road.Forward(d);var right=road.Right(d);
        var half=new Vector3(1.2f,.5f,2.3f);
        var self=new ObservedCar(0,line.At(d),f*18,f,right,d,line.Offset(d),18,half.x,half.z);
        var empty=new DriverObservation(10,1f/120,self,new[]{self},road,CarTuning.Street,1,0,false);
        var clear=new ContextDriverPolicy(profile,null,Vector3.zero,half);var baseline=clear.Decide(in empty);
        var rows=new List<Row>();
        foreach(float gap in new[]{-4f,-5f,-6f,-7f,-8f,-12f,-24f})
        foreach(float side in new[]{0f,-4f,4f})
        foreach(float speed in new[]{12f,18f,26f}) {
            float distance=d+gap,offset=self.offset+side;
            var rear=new ObservedCar(1,road.At(distance)+road.Right(distance)*offset,f*speed,f,road.Right(distance),distance,offset,speed,half.x,half.z);
            var o=new DriverObservation(10,1f/120,self,new[]{self,rear},road,CarTuning.Street,1,0,false);
            var driver=new ContextDriverPolicy(profile,null,Vector3.zero,half);var command=driver.Decide(in o);
            rows.Add(new Row{gap=gap,side=side,rearSpeed=speed,clearTarget=clear.SelectedOffset,target=driver.SelectedOffset,
                clearPedal=baseline.pedal,pedal=command.pedal,clearSteering=baseline.steering,steering=command.steering,
                followerPedal=driver.FollowerPedal,afterLeaderPedal=driver.AfterLeaderPedal,relevant=driver.RelevantCars,
                rearApproaches=driver.RearApproaches,leader=driver.Leader,adjacent=RoadPathFeasibility.LongitudinallyAdjacent(in o,in rear),roadUnresolved=driver.RoadUnresolved});
        }
        string output=RaceTrial.Arg("--rear-pressure-output");if(output==null)throw new ArgumentException("Output required.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(new Report{rows=rows.ToArray()},true));
        Debug.Log("REAR_PRESSURE_REVIEW rows="+rows.Count);
    }
}

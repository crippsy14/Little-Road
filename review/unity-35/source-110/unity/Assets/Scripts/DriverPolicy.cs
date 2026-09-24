using System;
using UnityEngine;

namespace LittleRoad
{
    public interface IDriverRoute
    {
        float Length {get;}
        Vector3 At(float metres);
        Vector3 Forward(float metres);
        Vector3 Right(float metres);
    }
    public readonly struct ObservedCar
    {
        public readonly int id;
        public readonly Vector3 position,velocity,forward,right,colliderCentre;
        public readonly float route,offset,speed,yawRate;
        public readonly float halfWidth,halfLength;
        public ObservedCar(int id,Vector3 position,Vector3 velocity,Vector3 forward,Vector3 right,float route,float offset,float speed,float halfWidth=1.4f,float halfLength=2.4f,float yawRate=0,Vector3 colliderCentre=default)
        {this.id=id;this.position=position;this.velocity=velocity;this.forward=forward;this.right=right;this.route=route;this.offset=offset;this.speed=speed;this.halfWidth=halfWidth;this.halfLength=halfLength;this.yawRate=yawRate;this.colliderCentre=colliderCentre;}
    }
    // A stack-scoped read-only view cannot be retained by a policy for a later
    // tick. The owner fills its pooled snapshot buffer before synchronous reads.
    public readonly ref struct DriverObservation
    {
        public readonly double timestamp;
        public readonly float stepSeconds,pace,preferredOffset,ownSteeringState;
        public readonly bool finished,drafting;
        public readonly float dragReduction;
        public readonly ObservedCar self;
        public readonly ReadOnlySpan<ObservedCar> traffic;
        public readonly IDriverRoute route;
        public readonly CarTuning tuning;
        public DriverObservation(double timestamp,float stepSeconds,ObservedCar self,ReadOnlySpan<ObservedCar> traffic,
            IDriverRoute route,CarTuning tuning,float pace,float preferredOffset,bool finished,float ownSteeringState=0,bool drafting=false,float forecastReduction=-1)
        {this.timestamp=timestamp;this.stepSeconds=stepSeconds;this.self=self;this.traffic=traffic;this.route=route;this.tuning=tuning;this.pace=pace;this.preferredOffset=preferredOffset;this.finished=finished;this.ownSteeringState=ownSteeringState;this.drafting=drafting;
            dragReduction=drafting?(forecastReduction>=0?forecastReduction:DraftingModel.Reduction(in self,traffic,out _,route.Length)):0;}
    }
    public readonly struct DriveCommand
    {
        public readonly float pedal,steering,targetSpeed,predictedOffset,chosenLane;
        public DriveCommand(float pedal,float steering,float targetSpeed,float predictedOffset,float chosenLane)
        {
            this.pedal=SafeInput(pedal);this.steering=SafeInput(steering);
            this.targetSpeed=targetSpeed;this.predictedOffset=predictedOffset;this.chosenLane=chosenLane;
        }
        static float SafeInput(float input)=>float.IsNaN(input)||float.IsInfinity(input)?0:Mathf.Clamp(input,-1,1);
    }
    public interface IDriverPolicy
    {
        DriveCommand Decide(in DriverObservation observation);
    }
}

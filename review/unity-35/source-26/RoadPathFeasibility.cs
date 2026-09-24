using UnityEngine;

namespace LittleRoad
{
    // Opt-in integration experiment. Nominal road feasibility only; moving
    // traffic constraints and emergency braking remain in ContextDriverPolicy.
    public sealed class RoadPathFeasibility
    {
        public const float Allowance=.10f;
        readonly ContextPathPrediction forecast;readonly Vector3 centre,half;readonly RecoverySensor scenery;
        public int Calls {get;private set;}
        public RoadPathFeasibility(RacingSpeedProfile profile,Vector3 centre,Vector3 half,RecoverySensor scenery)
        {forecast=new ContextPathPrediction(profile);this.centre=centre;this.half=half;this.scenery=scenery;}
        public static ContextPathState FromMotion(in DriverObservation o,float destination,bool rejoin)
        {
            float forward=Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route));
            return new ContextPathState{anchor=o.self.route,startOffset=o.self.offset,
                startSlope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(o.self.route))/Mathf.Max(3,forward),-.35f,.35f),
                endOffset=destination,selectedOffset=destination,blend=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60),rejoin=rejoin};
        }
        public static bool LongitudinallyAdjacent(in DriverObservation o,in ObservedCar other)
        {
            var along=o.route.Forward(o.self.route);
            float own=ProjectedRadius(in o.self,along),theirs=ProjectedRadius(in other,along);
            var a=o.self.position+o.self.right*o.self.colliderCentre.x+o.self.forward*o.self.colliderCentre.z;
            var b=other.position+other.right*other.colliderCentre.x+other.forward*other.colliderCentre.z;
            return Mathf.Abs(Vector3.Dot(b-a,along))<own+theirs+.10f;
        }
        static float ProjectedRadius(in ObservedCar car,Vector3 axis)
        {return car.halfLength*Mathf.Abs(Vector3.Dot(car.forward,axis))+car.halfWidth*Mathf.Abs(Vector3.Dot(car.right,axis));}
        public bool Check(in DriverObservation o,in ContextPathState path,float left,float right)
        {
            Calls++;forecast.Predict(in o,path,centre,half,2,false,3,true,Allowance);
            if(!forecast.envelope.Contained)return false;
            // Preserve the currently reachable centre corridor throughout the
            // nominal path, not only at its destination. This is not a forecast
            // of future opponent steering and is not collision certification.
            var road=(Circuit)o.route;
            for(int i=0;i<forecast.Count;i+=6) {
                road.Nearest(forecast.samples[i].state.position,out float offset);
                if(offset<left||offset>right)return false;
                if(i>0&&scenery!=null) {
                    var a=forecast.samples[i-6].state;var b=forecast.samples[i].state;
                    if(!scenery.StaticSweepClear(a.position,b.position,a.forward,b.forward))return false;
                }
            }
            return true;
        }
    }
}

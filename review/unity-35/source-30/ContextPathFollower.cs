using UnityEngine;

namespace LittleRoad
{
    [System.Serializable] public struct ContextPathState
    {
        public float anchor,consumed,startOffset,startSlope,endOffset,blend,selectedOffset;
        public bool rejoin;
    }
    // One follower for actual inputs and prospective physical rollouts. Buffers
    // belong to each instance so a forecast cannot overwrite the live path.
    public sealed class ContextPathFollower
    {
        const int Points=41;const float Step=3;
        readonly RacingSpeedProfile profile;
        readonly Vector3[] points=new Vector3[Points];
        readonly float[] speeds=new float[Points],distances=new float[Points];
        public ContextPathFollower(RacingSpeedProfile profile){this.profile=profile;}
        public static float Smooth(float start,float slope,float end,float distance,float length)
        {
            float t=Mathf.Clamp01(distance/length),a=slope*length,d=end-start;
            return start+a*t+(10*d-6*a)*t*t*t+(-15*d+8*a)*t*t*t*t+(6*d-3*a)*t*t*t*t*t;
        }
        public DriveCommand Follow(in DriverObservation o,in ContextPathState path,ForecastPathCache cache=null)
        {
            float budget=o.tuning.roadGrip*1.65f;
            for(int i=0;i<Points;i++) {
                float along=path.consumed+i*Step,d=path.anchor+along;
                if(cache==null||!cache.TryPoint(along,i*Step,out points[i])) {
                    float side=Smooth(path.startOffset,path.startSlope,path.endOffset,along,path.blend);
                    // When traffic has cleared, progressively reconnect to the guide.
                    if(path.rejoin)side=Mathf.Lerp(side,profile.line.Offset(d),Mathf.SmoothStep(0,1,i*Step/65));
                    points[i]=o.route.At(d)+o.route.Right(d)*side;
                }
                distances[i]=i==0?0:distances[i-1]+Vector3.Distance(points[i],points[i-1]);
                speeds[i]=profile.Speed(d);
            }
            for(int i=1;i<Points-1;i++) {
                var a=points[i]-points[i-1];var b=points[i+1]-points[i];
                float curvature=2*Mathf.Abs(Vector3.Cross(a,b).y)/Mathf.Max(.001f,a.magnitude*b.magnitude*(a+b).magnitude);
                speeds[i]=Mathf.Min(speeds[i],Mathf.Sqrt(budget*.78f/Mathf.Max(.00001f,curvature)));
            }
            for(int i=Points-2;i>=0;i--)speeds[i]=Mathf.Min(speeds[i],Mathf.Sqrt(speeds[i+1]*speeds[i+1]+2*Mathf.Min(o.tuning.braking,budget)*.75f*(distances[i+1]-distances[i])));
            float look=Mathf.Clamp(5+Mathf.Max(0,o.self.speed)*.4f,6,19);int aim=0;
            while(aim<Points-2&&distances[aim+1]<look)aim++;
            var delta=Vector3.Lerp(points[aim],points[aim+1],Mathf.InverseLerp(distances[aim],distances[aim+1],look))-o.self.position;
            // The tyre model permits measurable lateral velocity. Pure pursuit
            // must turn the direction of travel toward the path, rather than
            // assuming that body heading and actual travel are identical.
            var travel=o.self.velocity;travel.y=0;
            var heading=travel.sqrMagnitude>9?travel.normalized:o.self.forward;
            float angle=Vector3.SignedAngle(heading,delta,Vector3.up)*Mathf.Deg2Rad;
            float curve=2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude);
            float steer=Mathf.Clamp(curve*Mathf.Max(3,o.self.speed)*1.09f/o.tuning.yaw,-1,1);
            float target=Mathf.Min(speeds[1],Mathf.Sqrt(budget*.85f/Mathf.Max(.00001f,Mathf.Abs(curve))));
            if(o.finished)target=Mathf.Min(target,18);
            float feed=(speeds[1]*speeds[1]-speeds[0]*speeds[0])/(2*Mathf.Max(.01f,distances[1]));
            float force=feed+(target-o.self.speed)*3+DriveModel.Resistance(o.self.speed,o.tuning.mass);
            float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
            return new DriveCommand(pedal,steer,target,o.self.offset,path.selectedOffset);
        }
    }
}

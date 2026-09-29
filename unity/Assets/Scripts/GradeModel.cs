using UnityEngine;

namespace LittleRoad
{
    // --grade-aware (off by default): context drivers plan for hills. Without it the forecast
    // and the speed profile assume level tarmac, and elevated circuits are refused.
    //   - The path forecast rides at the road's height and feels gravity along the grade.
    //   - The speed profile loses braking downhill, gains it uphill, and scales corner grip by the
    //     load change over crests and dips (1 + v^2 * vertical curvature / g).
    // Set once on the main thread (RaceTrial) before any forecast worker starts; read-only after.
    public static class GradeModel
    {
        public const float Gravity=9.81f;
        public static bool Enabled;
        // Diagnostic arm (--grade-no-physics): accept elevated roads and follow their height, but plan
        // as if level (no gravity along the grade, no crest/dip load). Measures what the physics adds.
        public static bool Physics;
        // Rise per horizontal metre along the road at `meters` (positive uphill in the driving direction).
        public static float Grade(Circuit road,float meters)
        {
            var a=road.At(meters-2);var b=road.At(meters+2);float run=new Vector2(b.x-a.x,b.z-a.z).magnitude;
            return run>.01f?(b.y-a.y)/run:0;
        }
        // Longitudinal gravity deceleration for a car heading `forward` at road distance `meters`.
        public static float Slope(Circuit road,float meters,Vector3 forward)
        {
            if(!Enabled||!Physics||!road.hasElevation)return 0;
            float grade=Grade(road,meters),sign=Vector3.Dot(forward,road.Forward(meters))>=0?1:-1;
            return Gravity*sign*grade/Mathf.Sqrt(1+grade*grade);
        }
        // Normal-load factor on corner grip from vertical curvature (crest < 1 < dip), bounded.
        public static float Load(float speed,float verticalCurvature)=>Mathf.Clamp(1+speed*speed*verticalCurvature/Gravity,.4f,1.3f);
    }
}

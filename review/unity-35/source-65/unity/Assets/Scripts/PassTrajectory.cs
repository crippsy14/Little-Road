using UnityEngine;

namespace LittleRoad
{
    public static class PassTrajectory
    {
        // Produces a candidate only. Current-state road, scenery and traffic
        // checks must accept it before the live controller may keep following it.
        public static bool TryRetain(in DriverObservation o,in ContextPathState previous,float lastRoute,
            float destination,int relevantCars,out ContextPathState candidate)
        {
            candidate=previous;
            if(o.finished||relevantCars==0||previous.guide||previous.rejoin||previous.coast
                ||Mathf.Abs(destination-previous.selectedOffset)>.001f
                ||Mathf.Abs(destination-previous.endOffset)>.001f
                ||Mathf.Abs(destination-o.self.offset)<=.6f)return false;
            candidate.consumed+=Mathf.Max(0,Mathf.Repeat(o.self.route-lastRoute+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f);
            return candidate.consumed<previous.blend;
        }
    }
}

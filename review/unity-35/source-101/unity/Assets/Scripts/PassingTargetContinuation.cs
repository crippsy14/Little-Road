using UnityEngine;

namespace LittleRoad
{
    public static class PassingTargetContinuation
    {
        // Request a comparison, never force the old target or certify a gap.
        // Small returns into a slower car's corridor can abandon a useful pass
        // without triggering the existing whole-body-width switch threshold.
        public static bool ShouldCompare(in DriverObservation o,float proposed,float previous,float desiredSpeed)
        {
            if(Mathf.Abs(proposed-previous)<.01f)return false;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id||desiredSpeed<=other.speed+.8f)continue;
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(gap<=0||gap>100)continue;
                float width=o.self.halfWidth+other.halfWidth+.35f;
                if(Mathf.Abs(proposed-other.offset)<=width&&Mathf.Abs(previous-other.offset)>width)return true;
            }
            return false;
        }
    }
}

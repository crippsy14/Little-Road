using UnityEngine;

namespace LittleRoad
{
    public static class CheckpointCrossing
    {
        // Test the swept route segment, not car heading or a later frame's
        // position. A collision can rotate a car while it crosses legally.
        public static bool Forward(float before,float after,float beforeOffset,float afterOffset,float gate,float halfWidth)
        {
            if(after<=before || before>gate || after<gate)return false;
            float fraction=(gate-before)/(after-before);
            return Mathf.Abs(Mathf.Lerp(beforeOffset,afterOffset,fraction))<=halfWidth;
        }
    }
}

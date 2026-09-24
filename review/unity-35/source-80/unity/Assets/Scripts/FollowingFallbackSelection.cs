using System;

namespace LittleRoad
{
    // Component-test selection only; ordinary enabled use applies to all drivers.
    public enum FollowingFallbackScope { All, Car0, Rivals }
    public static class FollowingFallbackSelection
    {
        public static FollowingFallbackScope Parse(string value)=>value switch {
            "all"=>FollowingFallbackScope.All,"car0"=>FollowingFallbackScope.Car0,"rivals"=>FollowingFallbackScope.Rivals,
            _=>throw new ArgumentException("Unknown following fallback scope: "+value)
        };
        public static bool Applies(FollowingFallbackScope scope,int id)=>id>=0&&(scope==FollowingFallbackScope.All
            ||scope==FollowingFallbackScope.Car0&&id==0||scope==FollowingFallbackScope.Rivals&&id!=0);
    }
}

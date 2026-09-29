using UnityEngine;

namespace LittleRoad
{
    // Camera response to knocks (docs/visual-quality-audit.md, "feedback"): hitting a car, a wall or a prop
    // adds trauma, which decays over about half a second; the camera shakes by trauma² (small knocks
    // barely register, big ones really jolt) with smooth noise, not random jitter. Kerbs, the road and
    // terrain don't count (they aren't tagged obstacles). --no-camera-shake turns it off.
    public static class CameraShake
    {
        public const float MinImpact=3f,FullImpact=12f,Decay=1.8f,MaxAngle=2.2f,MaxOffset=.12f;
        static float trauma,seed;
        public static void Watch(RoadCar car)
        {
            if(RaceTrial.Has("--no-camera-shake")||Application.isBatchMode)return;
            car.Bumped+=(other,speed)=>{
                if(other==null||speed<MinImpact)return;
                bool obstacle=other.CompareTag("Respawn")||other.GetComponentInParent<RoadCar>()!=null;
                if(obstacle)trauma=Mathf.Min(1,trauma+Mathf.InverseLerp(MinImpact,FullImpact,speed)*.8f+.15f);
            };
        }
        // Call after the camera has been posed for the frame.
        public static void Apply(Camera camera)
        {
            if(trauma<=0||camera==null)return;
            trauma=Mathf.Max(0,trauma-Decay*Time.deltaTime);float s=trauma*trauma;seed+=Time.deltaTime*18;
            float N(float o)=>Mathf.PerlinNoise(seed,o)*2-1;
            camera.transform.rotation*=Quaternion.Euler(N(1)*MaxAngle*s,N(2)*MaxAngle*s,N(3)*MaxAngle*s*.6f);
            camera.transform.position+=camera.transform.rotation*new Vector3(N(4),N(5),0)*MaxOffset*s;
        }
        public static void Reset()=>trauma=0;
    }
}

using UnityEngine;

namespace LittleRoad
{
    // Races build only the world around their circuit (Richard, 4 Oct: "only load a certain area around the track for races
    // to make it faster to load in and out"). The Hillcrest and Quarry Hills races used to build the whole world: both
    // towns, the railway, every wood, the farms and the quay. With an area set, the terrain is still built whole (it's
    // the horizon), but towns, woods, farms and the quay outside the area are left out, and a car that strays more than
    // Leash metres from the circuit is put back on it. --race-full-world builds everything, as before (A/B).
    public static class RaceArea
    {
        public static bool Active{get;private set;}
        public static Vector2 Centre{get;private set;}
        public static float Radius{get;private set;}
        public const float Leash=160;   // metres from the circuit's centreline before the car is put back
        // The area: the circuit's bounding circle plus `margin` (what you can see from it that matters).
        public static void Set(Vector3[] line,float margin)
        {
            if(RaceTrial.Has("--race-full-world")||line==null||line.Length==0){Clear();return;}
            Vector2 lo=new(float.MaxValue,float.MaxValue),hi=new(float.MinValue,float.MinValue);
            foreach(var p in line){lo=Vector2.Min(lo,new Vector2(p.x,p.z));hi=Vector2.Max(hi,new Vector2(p.x,p.z));}
            Centre=(lo+hi)*.5f;Radius=(hi-lo).magnitude*.5f+margin;Active=true;
            Debug.Log($"LITTLE_ROAD_RACE_AREA centre={Centre.x:0},{Centre.y:0} radius={Radius:0}");
        }
        public static void Clear(){Active=false;}
        // Whether a piece at (x, z), or something of `size` metres around it, stands in the area (always true when off).
        public static bool Near(float x,float z,float size=0)=>!Active||Vector2.Distance(new Vector2(x,z),Centre)<Radius+size;
        public static bool Near(Vector2 p,float size=0)=>Near(p.x,p.y,size);
        public static bool Near(Vector3 p,float size=0)=>Near(p.x,p.z,size);
    }
}

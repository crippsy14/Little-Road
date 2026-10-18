using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Imported wheel meshes may share the Wheel_ prefix. Animate only the four
    // axle pivots; rotating their children as well sends offset mesh origins
    // around the axle a second time. All LOD pivots receive the same pose.
    public sealed class WheelVisuals
    {
        public readonly Transform[] pivots;
        readonly Quaternion[] rest;
        // --wheel-camber (off by default): a Car Forge body's camber (its file's degrees, top in; class B's drift kit), as
        // a tilt about the car's length before the spin, so a cambered wheel turns true on its own axle.
        public static bool CamberEnabled=>RaceTrial.Has("--wheel-camber");
        readonly float[] camber;
        public static bool IsPivot(string name)=>name=="Wheel_FL"||name=="Wheel_FR"||name=="Wheel_RL"||name=="Wheel_RR";
        // The wheels' rolling radius: the axles' height above the car's base (the models stand on y = 0), so each body
        // turns its wheels at its own rate (the Regale's are 0.345 m, the family cars' 0.39-0.43).
        public readonly float Radius=.425f;
        public WheelVisuals(GameObject car,float? camberDegrees=null)
        {
            var selected=new List<Transform>();
            foreach(var t in car.GetComponentsInChildren<Transform>(true))
                if(IsPivot(t.name))selected.Add(t);
            pivots=selected.ToArray();rest=new Quaternion[pivots.Length];
            for(int i=0;i<pivots.Length;i++)rest[i]=pivots[i].localRotation;
            if(pivots.Length>0){float sum=0;foreach(var p in pivots)sum+=car.transform.InverseTransformPoint(p.position).y;Radius=Mathf.Clamp(sum/pivots.Length,.25f,.6f);}
            // (each side tilts its top towards the car's centre: +z on the right, -z on the left)
            float deg=camberDegrees??(CamberEnabled?CarBodies.Forged(car.name.Replace("(Clone)","").Trim())?.camber??0:0);camber=new float[pivots.Length];
            for(int i=0;i<pivots.Length;i++){float x=car.transform.InverseTransformPoint(pivots[i].position).x;camber[i]=Mathf.Abs(x)<.05f?0:Mathf.Sign(x)*deg;}
            Pose(0,0);
        }
        public void Pose(float spinDegrees,float steerDegrees)
        {
            for(int i=0;i<pivots.Length;i++)
                pivots[i].localRotation=Quaternion.Euler(0,pivots[i].name.StartsWith("Wheel_F")?steerDegrees:0,0)
                    *Quaternion.Euler(0,0,camber[i])*rest[i]*Quaternion.Euler(spinDegrees,0,0);
        }
    }
}

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
        public WheelVisuals(GameObject car)
        {
            var selected=new List<Transform>();
            foreach(var t in car.GetComponentsInChildren<Transform>(true))
                if(t.name=="Wheel_FL"||t.name=="Wheel_FR"||t.name=="Wheel_RL"||t.name=="Wheel_RR")selected.Add(t);
            pivots=selected.ToArray();rest=new Quaternion[pivots.Length];
            for(int i=0;i<pivots.Length;i++)rest[i]=pivots[i].localRotation;
        }
        public void Pose(float spinDegrees,float steerDegrees)
        {
            for(int i=0;i<pivots.Length;i++)
                pivots[i].localRotation=Quaternion.Euler(0,pivots[i].name.StartsWith("Wheel_F")?steerDegrees:0,0)
                    *rest[i]*Quaternion.Euler(spinDegrees,0,0);
        }
    }
}

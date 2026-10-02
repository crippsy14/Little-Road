using System;
using UnityEngine;

namespace LittleRoad
{
    // Gameplay wake model, not CFD. Pure and allocation-free so the live car
    // and owned prospective observations can use the same calculation.
    public static class DraftingModel
    {
        public const float MaximumReduction=.08f;
        static bool Same(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
        static bool Same(Vector3 a,Vector3 b)=>Same(a.x,b.x)&&Same(a.y,b.y)&&Same(a.z,b.z);
        public static bool Same(in ObservedCar a,in ObservedCar b)=>a.id==b.id&&Same(a.position,b.position)&&Same(a.velocity,b.velocity)
            &&Same(a.forward,b.forward)&&Same(a.right,b.right)&&Same(a.colliderCentre,b.colliderCentre)&&Same(a.route,b.route)
            &&Same(a.offset,b.offset)&&Same(a.speed,b.speed)&&Same(a.yawRate,b.yawRate)&&Same(a.halfWidth,b.halfWidth)&&Same(a.halfLength,b.halfLength);
        static float Ramp(float value,float low,float high)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(low,high,value));
        public static float Reduction(in ObservedCar self,ReadOnlySpan<ObservedCar> traffic,out int leader,float routeLength=0,float forecastSeconds=0)
        {
            leader=-1;float best=0;
            if(self.speed<=12)return 0;
            foreach(var observed in traffic) {
                var other=observed;
                if(forecastSeconds>0)other=new ObservedCar(observed.id,observed.position+observed.velocity*forecastSeconds,observed.velocity,observed.forward,observed.right,
                    observed.route+observed.speed*forecastSeconds,observed.offset,observed.speed,observed.halfWidth,observed.halfLength,observed.yawRate,observed.colliderCentre);
                if(other.id==self.id||other.speed<=12)continue;
                var ownCentre=self.position+self.right*self.colliderCentre.x+self.forward*self.colliderCentre.z;
                var otherCentre=other.position+other.right*other.colliderCentre.x+other.forward*other.colliderCentre.z;
                var delta=otherCentre-ownCentre;
                if(routeLength>0) {
                    float along=Mathf.Repeat(other.route-self.route+routeLength*.5f,routeLength)-routeLength*.5f;
                    // Reject a nearby parallel branch or crossing of the circuit.
                    if(along<=0||along>60||Mathf.Abs(along-Vector3.Dot(delta,other.forward))>Mathf.Max(6,along*.25f))continue;
                }
                if(Mathf.Abs(delta.y)>=1.5f)continue;
                float alignment=Vector3.Dot(self.forward,other.forward);
                if(alignment<=.96f)continue;
                // A draft is behind the rear body, not beside/inside it.
                float gap=Vector3.Dot(delta,other.forward)-self.halfLength-other.halfLength;
                if(gap<=0||gap>=35)continue;
                float side=Mathf.Abs(Vector3.Dot(delta,other.right));
                float width=other.halfWidth+self.halfWidth*.5f;
                if(side>=width)continue;
                float strength=Ramp(gap,0,2)*(1-Ramp(gap,5,35))
                    *(1-Ramp(side,0,width))*Ramp(Mathf.Min(self.speed,other.speed),12,26)
                    *Ramp(alignment,.96f,.999f)*(1-Ramp(Mathf.Abs(delta.y),.5f,1.5f));
                float reduction=MaximumReduction*strength;
                // No platoon stacking; stable ties are independent of iteration order.
                if(reduction>best||(reduction>0&&reduction==best&&other.id<leader)){best=reduction;leader=other.id;}
            }
            return best;
        }
        // dragFactor: the car's low-drag bodywork (CarTuning.DragFactor, docs/54); 1 keeps the old arithmetic exactly.
        public static float Resistance(float speed,float mass,float reduction,float dragFactor=1)
        {
            if(reduction<=0)return (.25f+DriveModel.DragK*dragFactor*speed*speed)/mass; // Exact old arithmetic at zero (x1).
            return (.25f+DriveModel.DragK*dragFactor*speed*speed*(1-Mathf.Min(MaximumReduction,reduction)))/mass;
        }
    }
}

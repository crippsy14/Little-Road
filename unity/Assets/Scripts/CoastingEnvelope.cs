using UnityEngine;

namespace LittleRoad
{
    // A short coast is permitted only while the existing braking envelope
    // remains reachable. This never changes the envelope or traffic guards.
    public static class CoastingEnvelope
    {
        public const float Horizon=.25f;
        public static bool Allows(float speed,float mass,float steeringLimit,float[] distances,float[] limits,int count,float maximumDragReduction=0)
        {
            if(count<2||speed<=0||mass<=0||speed>steeringLimit||speed>limits[0])return false;
            const float step=.05f;
            float travelled=0;int segment=0;
            for(int tick=0;tick<5;tick++) {
                // Coasting slows the car. Start-speed distance is an upper
                // bound; use the lowest envelope anywhere across that interval.
                float end=travelled+speed*step;
                if(end>distances[count-1])return false;
                float minimum=At(travelled,distances,limits,count,ref segment);
                while(segment+1<count&&distances[segment+1]<=end)minimum=Mathf.Min(minimum,limits[++segment]);
                minimum=Mathf.Min(minimum,At(end,distances,limits,count,ref segment));
                if(speed>minimum)return false;
                // Drag increases with speed: evaluating it at a lower bound
                // for the next speed gives an upper bound on coast speed.
                float lower=Mathf.Max(0,speed-DraftingModel.Resistance(speed,mass,maximumDragReduction)*step);
                speed=Mathf.Max(0,speed-DraftingModel.Resistance(lower,mass,maximumDragReduction)*step);
                travelled=end;
            }
            return true;
        }
        static float At(float d,float[] distances,float[] limits,int count,ref int segment)
        {
            while(segment+1<count-1&&distances[segment+1]<d)segment++;
            if(segment>=count-1)return limits[count-1];
            return Mathf.Lerp(limits[segment],limits[segment+1],Mathf.InverseLerp(distances[segment],distances[segment+1],d));
        }
    }
}

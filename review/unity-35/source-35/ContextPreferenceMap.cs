using UnityEngine;

namespace LittleRoad
{
    // Reusable preference calculation. No physics queries, forecasts or driver
    // state changes; a separate instance can prepare a prospective first choice.
    public sealed class ContextPreferenceMap
    {
        public const int Slots=25;
        readonly RacingSpeedProfile profile;
        readonly ObservedCar[] neighbours=new ObservedCar[24];
        readonly float[] neighbourGaps=new float[24];
        public readonly float[] value=new float[Slots];
        public readonly bool[] reachable=new bool[Slots];
        public int RelevantCars,RearApproaches;
        public float ReachableLeft,ReachableRight,Edge,Spacing,Guide;
        public ContextPreferenceMap(RacingSpeedProfile profile){this.profile=profile;}
        static float Gap(float a,float b,float length)=>Mathf.Repeat(b-a+length*.5f,length)-length*.5f;
        bool Relevant(in DriverObservation o,in ObservedCar other,float gap,bool rearApproach)
        {
            if(other.id==o.self.id)return false;
            float bodies=o.self.halfLength+other.halfLength;
            if(gap<0)return -gap<bodies+3||rearApproach;
            float closing=Mathf.Max(0,profile.Speed(o.self.route+10)-other.speed);
            return gap<bodies+8+closing*3 && (closing>.8f||gap<bodies+4);
        }
        public float Danger(in DriverObservation o,float offset)
        {
            float danger=0;
            // All map samples share one immutable observation. Reuse its
            // relevance/gap calculation rather than repeating it per slot.
            for(int j=0;j<RelevantCars;j++) {
                var other=neighbours[j];float gap=neighbourGaps[j];
                float width=o.self.halfWidth+other.halfWidth+.6f;
                float overlap=Mathf.Clamp01((width+.7f-Mathf.Abs(offset-other.offset))/.7f);
                danger=Mathf.Max(danger,overlap*(gap<0?1:Mathf.Clamp01(1-gap/100)));
            }
            return danger;
        }
        public void Fill(in DriverObservation o,bool pathActive,float endOffset,bool rearAnticipationEnabled,bool physicalRoad)
        {
            float edge=Circuit.Width*.5f-o.self.halfWidth-.55f;
            float spacing=2*edge/(Slots-1),guide=profile.line.Offset(o.self.route+Mathf.Max(12,o.self.speed));
            RelevantCars=0;RearApproaches=0;ReachableLeft=-edge;ReachableRight=edge;
            foreach(var other in o.traffic) {
                float gap=Gap(o.self.route,other.route,o.route.Length);
                bool rearApproach=rearAnticipationEnabled&&ContextDriverPolicy.RearApproachDuringMerge(in o,in other,gap);
                if(!Relevant(in o,in other,gap,rearApproach))continue;
                neighbours[RelevantCars]=other;neighbourGaps[RelevantCars]=gap;RelevantCars++;
                if(rearApproach)RearApproaches++;
                bool adjacent=!physicalRoad?Mathf.Abs(gap)<o.self.halfLength+other.halfLength+3:RoadPathFeasibility.LongitudinallyAdjacent(in o,in other);
                if(rearApproach||adjacent) {
                    float width=o.self.halfWidth+other.halfWidth+.4f;
                    if(other.offset<o.self.offset)ReachableLeft=Mathf.Max(ReachableLeft,other.offset+width);
                    else ReachableRight=Mathf.Min(ReachableRight,other.offset-width);
                }
            }
            for(int i=0;i<Slots;i++) {
                float offset=-edge+i*spacing;
                reachable[i]=offset>=ReachableLeft&&offset<=ReachableRight;
                float danger=Danger(in o,offset);
                value[i]=-.09f*Mathf.Abs(offset-guide)-.025f*Mathf.Abs(offset-o.self.offset)
                    -(pathActive?.05f*Mathf.Abs(offset-endOffset):0)-4*danger;
            }
            Edge=edge;Spacing=spacing;Guide=guide;
        }
        public int Pick(out float selected)
        {
            int best=-1;float bestValue=float.NegativeInfinity;
            for(int i=0;i<Slots;i++)if(reachable[i]&&value[i]>bestValue){best=i;bestValue=value[i];}
            selected=0;if(best<0)return best;
            selected=-Edge+best*Spacing;
            if(best>0&&best<Slots-1&&reachable[best-1]&&reachable[best+1])selected+=ContextDriverPolicy.PeakOffset(value[best-1],value[best],value[best+1],Spacing);
            return best;
        }
    }
}

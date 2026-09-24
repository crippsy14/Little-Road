using UnityEngine;

namespace LittleRoad
{
    // Reusable preference calculation. No physics queries, forecasts or driver
    // state changes; a separate instance can prepare a prospective first choice.
    public sealed class ContextPreferenceMap
    {
        public const int Slots=25;
        public const int Capacity=Slots+25;
        readonly RacingSpeedProfile profile;
        readonly bool gapCentres,anticipateSpace;
        readonly ObservedCar[] neighbours=new ObservedCar[24];
        readonly float[] neighbourGaps=new float[24];
        readonly float[] passingOffsets=new float[24];
        public readonly float[] value=new float[Capacity];
        public readonly bool[] reachable=new bool[Capacity];
        readonly float[] offsets=new float[Capacity],blockedLeft=new float[24],blockedRight=new float[24];
        public int Count {get;private set;}
        public float Sample(int index)=>offsets[index];
        readonly float[] boundary=new float[24],startsAt=new float[24];
        readonly bool[] fromLeft=new bool[24];
        int constraints;
        public int RelevantCars,RearApproaches;
        public float ReachableLeft,ReachableRight,Edge,Spacing,Guide;
        public ContextPreferenceMap(RacingSpeedProfile profile,bool gapCentres=true,bool anticipateSpace=false)
        {this.profile=profile;this.gapCentres=gapCentres;this.anticipateSpace=anticipateSpace;}
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
                // Risk changes preferred spare space, never physical body size.
                // Preserve the old arithmetic exactly at risk0.
                int risk=profile.planning.Risk;
                float padding=risk<=5?.6f-.09f*risk:.15f-.02f*(risk-5);
                float falloff=risk<=5?.7f-.1f*risk:.2f-.02f*(risk-5);
                float width=o.self.halfWidth+other.halfWidth+padding;
                float overlap=Mathf.Clamp01((width+falloff-Mathf.Abs(offset-passingOffsets[j]))/falloff);
                danger=Mathf.Max(danger,overlap*(gap<0?1:Mathf.Clamp01(1-gap/100)));
            }
            return danger;
        }
        public void Fill(in DriverObservation o,bool pathActive,float endOffset,bool rearAnticipationEnabled,bool physicalRoad,bool timedCorridors=false)
        {
            float edge=Circuit.Width*.5f-o.self.halfWidth-.55f;
            float spacing=2*edge/(Slots-1),guide=profile.line.Offset(o.self.route+Mathf.Max(12,o.self.speed));
            constraints=0;RelevantCars=0;RearApproaches=0;ReachableLeft=-edge;ReachableRight=edge;
            foreach(var other in o.traffic) {
                float gap=Gap(o.self.route,other.route,o.route.Length);
                bool rearApproach=rearAnticipationEnabled&&ContextDriverPolicy.RearApproachDuringMerge(in o,in other,gap);
                if(!Relevant(in o,in other,gap,rearApproach))continue;
                neighbours[RelevantCars]=other;neighbourGaps[RelevantCars]=gap;
                passingOffsets[RelevantCars]=anticipateSpace?PassingOffset(in o,in other,gap,profile.Speed(o.self.route+10)):other.offset;
                RelevantCars++;
                if(rearApproach)RearApproaches++;
                bool adjacent=!physicalRoad?Mathf.Abs(gap)<o.self.halfLength+other.halfLength+3:RoadPathFeasibility.LongitudinallyAdjacent(in o,in other);
                if(rearApproach||adjacent) {
                    float width=o.self.halfWidth+other.halfWidth+.4f;
                    bool left=other.offset<o.self.offset;float limit=other.offset+(left?width:-width);
                    if(left)ReachableLeft=Mathf.Max(ReachableLeft,limit);
                    else ReachableRight=Mathf.Min(ReachableRight,limit);
                    fromLeft[constraints]=left;boundary[constraints]=limit;
                    startsAt[constraints]=timedCorridors&&rearApproach&&!adjacent?RearReservationStart(in o,in other,gap):0;
                    constraints++;
                }
            }
            Count=Slots;
            for(int i=0;i<Slots;i++)offsets[i]=-edge+i*spacing;
            if(gapCentres&&profile.planning.Risk>0&&RelevantCars>0)AddGapCentres(in o,edge);
            for(int i=0;i<Count;i++) {
                float offset=offsets[i];
                reachable[i]=offset>=ReachableLeft&&offset<=ReachableRight;
                float danger=Danger(in o,offset);
                value[i]=-.09f*Mathf.Abs(offset-guide)-.025f*Mathf.Abs(offset-o.self.offset)
                    -(pathActive?.05f*Mathf.Abs(offset-endOffset):0)-4*danger;
            }
            Edge=edge;Spacing=spacing;Guide=guide;
        }
        void AddGapCentres(in DriverObservation o,float edge)
        {
            // Merge observed occupied intervals in fixed scratch storage. Their
            // complement gives actual gaps, including gaps between grid samples.
            // A candidate still needs the ordinary physical forecast and guard.
            int count=0;
            for(int j=0;j<RelevantCars;j++) {
                var other=neighbours[j];
                float width=o.self.halfWidth+other.halfWidth+.4f;
                float left=passingOffsets[j]-width,right=passingOffsets[j]+width;
                int k=count++;
                while(k>0&&blockedLeft[k-1]>left){blockedLeft[k]=blockedLeft[k-1];blockedRight[k]=blockedRight[k-1];k--;}
                blockedLeft[k]=left;blockedRight[k]=right;
            }
            float cursor=Mathf.Max(-edge,ReachableLeft),end=Mathf.Min(edge,ReachableRight);
            for(int j=0;j<count&&cursor<end;j++) {
                float left=Mathf.Min(end,blockedLeft[j]);
                if(left-cursor>.02f)offsets[Count++]=(cursor+left)*.5f;
                cursor=Mathf.Max(cursor,blockedRight[j]);
            }
            if(end-cursor>.02f)offsets[Count++]=(cursor+end)*.5f;
        }
        public static float PassingOffset(in DriverObservation o,in ObservedCar other,float gap,float desiredSpeed)
        {
            // Estimate where an ahead car is moving while we close, rather than
            // chase its current side position. No intent/future input is read.
            // Near-adjacent and rear cars retain their actual occupied space.
            float bodies=o.self.halfLength+other.halfLength;
            if(gap<=bodies+3)return other.offset;
            float closing=Mathf.Max(1,Mathf.Max(o.self.speed,desiredSpeed)-Mathf.Max(0,other.speed));
            float time=Mathf.Clamp((gap-bodies)/closing,0,1.25f);
            float sideways=Vector3.Dot(other.velocity,o.route.Right(other.route));
            float displacement=sideways*.75f*(1-Mathf.Exp(-time/.75f));
            return other.offset+Mathf.Clamp(displacement,-1.5f,1.5f);
        }
        public static float RearReservationStart(in DriverObservation o,in ObservedCar other,float gap)
        {
            // Same observed constant-closing model and 3m longitudinal space
            // as RearApproachDuringMerge. Start one planning interval early.
            // Candidate destinations remain reserved immediately; only the
            // forecast's approach to that destination uses this arrival time.
            var along=o.route.Forward(o.self.route);
            float ownSpeed=Mathf.Max(0,Vector3.Dot(o.self.velocity,along));
            float closing=Vector3.Dot(other.velocity,along)-ownSpeed;
            if(gap>=0||closing<=.8f)return 0;
            float clear=-gap-o.self.halfLength-other.halfLength-3;
            return Mathf.Max(0,clear/closing-.1f);
        }
        public void BoundsAt(float seconds,out float left,out float right)
        {
            left=float.NegativeInfinity;right=float.PositiveInfinity;
            for(int i=0;i<constraints;i++)if(seconds>=startsAt[i]) {
                if(fromLeft[i])left=Mathf.Max(left,boundary[i]);
                else right=Mathf.Min(right,boundary[i]);
            }
        }
        public int Pick(out float selected)
        {
            int best=-1;float bestValue=float.NegativeInfinity;
            for(int i=0;i<Count;i++)if(reachable[i]&&value[i]>bestValue){best=i;bestValue=value[i];}
            selected=0;if(best<0)return best;
            selected=offsets[best];
            if(best>0&&best<Slots-1&&reachable[best-1]&&reachable[best+1])selected+=ContextDriverPolicy.PeakOffset(value[best-1],value[best],value[best+1],Spacing);
            return best;
        }
    }
}

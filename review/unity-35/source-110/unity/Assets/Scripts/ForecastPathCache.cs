using UnityEngine;

namespace LittleRoad
{
    // Experimental approximation inside one forecast only. Never used by the
    // physical car. Rebuilt for every proposed path; no stale traffic/state is
    // shared between planning updates. Outside the table, use exact geometry.
    public sealed class ForecastPathCache
    {
        const float Step=.5f;
        const int Count=401;
        struct Frame {public Vector3 position,right;public float side,guide;}
        readonly Frame[] frames=new Frame[Count];
        readonly bool[] ready=new bool[Count];
        readonly RacingSpeedProfile profile;
        IDriverRoute route;ContextPathState path;
        float origin;bool rejoin;
        public int PreparedSamples {get;private set;}
        public int Hits {get;private set;}
        public int Misses {get;private set;}
        public ForecastPathCache(RacingSpeedProfile profile){this.profile=profile;}
        public void Prepare(IDriverRoute route,in ContextPathState path)
        {
            this.route=route;this.path=path;origin=path.consumed;rejoin=path.rejoin;Hits=Misses=PreparedSamples=0;
            System.Array.Clear(ready,0,ready.Length);
        }
        Frame Sample(int i)
        {
            if(!ready[i]){
                // Identical half-metre sample arithmetic, generated only when
                // consumed by this forecast. No reuse across paths or snapshots.
                float along=origin+i*Step,d=path.anchor+along;
                frames[i]=new Frame{position=route.At(d),right=route.Right(d),
                    side=ContextPathFollower.Lateral(in path,along,path.guide?profile.line.Offset(d):0),
                    guide=rejoin?profile.line.Offset(d):0};
                ready[i]=true;PreparedSamples++;
            }
            return frames[i];
        }
        public bool TryPoint(float along,float ahead,out Vector3 point)
        {
            float coordinate=(along-origin)/Step;int i=Mathf.FloorToInt(coordinate);
            if(i<0||i>=Count-1){Misses++;point=default;return false;}
            Hits++;float f=coordinate-i;var a=Sample(i);var b=Sample(i+1);
            float side=Mathf.Lerp(a.side,b.side,f);
            if(rejoin)side=Mathf.Lerp(side,Mathf.Lerp(a.guide,b.guide,f),Mathf.SmoothStep(0,1,ahead/65));
            point=Vector3.Lerp(a.position,b.position,f)+Vector3.Lerp(a.right,b.right,f)*side;
            return true;
        }
    }
}

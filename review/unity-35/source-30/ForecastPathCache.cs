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
        readonly RacingSpeedProfile profile;
        float origin;bool rejoin;
        public int Hits {get;private set;}
        public int Misses {get;private set;}
        public ForecastPathCache(RacingSpeedProfile profile){this.profile=profile;}
        public void Prepare(IDriverRoute route,in ContextPathState path)
        {
            origin=path.consumed;rejoin=path.rejoin;Hits=Misses=0;
            for(int i=0;i<Count;i++) {
                float along=origin+i*Step,d=path.anchor+along;
                frames[i]=new Frame{position=route.At(d),right=route.Right(d),
                    side=ContextPathFollower.Smooth(path.startOffset,path.startSlope,path.endOffset,along,path.blend),
                    guide=rejoin?profile.line.Offset(d):0};
            }
        }
        public bool TryPoint(float along,float ahead,out Vector3 point)
        {
            float coordinate=(along-origin)/Step;int i=Mathf.FloorToInt(coordinate);
            if(i<0||i>=Count-1){Misses++;point=default;return false;}
            Hits++;float f=coordinate-i;var a=frames[i];var b=frames[i+1];
            float side=Mathf.Lerp(a.side,b.side,f);
            if(rejoin)side=Mathf.Lerp(side,Mathf.Lerp(a.guide,b.guide,f),Mathf.SmoothStep(0,1,ahead/65));
            point=Vector3.Lerp(a.position,b.position,f)+Vector3.Lerp(a.right,b.right,f)*side;
            return true;
        }
    }
}

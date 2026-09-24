using System;

namespace LittleRoad
{
    // Observation only. Fixed-size passage histories never feed the driver.
    public sealed class RaceTiming
    {
        public enum GapKind { Waiting, Seconds, Laps, Player }
        public readonly struct Gap
        {
            public readonly GapKind Kind;public readonly float Value;
            public Gap(GapKind kind,float value=0){Kind=kind;Value=value;}
        }
        sealed class Entry
        {
            public readonly int[] indices;public readonly float[] times;
            public float progress,time,finishTime;public int recovery,slot;public bool observed,finished;
            public Entry(int capacity,int gridSlot){indices=new int[capacity];times=new float[capacity];slot=gridSlot;Clear();}
            public void Clear(){Array.Fill(indices,-1);}
            public void Put(int index,float time){if(index<0)return;int at=index%indices.Length;indices[at]=index;times[at]=time;}
            public bool Get(int index,out float time){int at=index<0?0:index%indices.Length;time=times[at];return index>=0&&indices[at]==index;}
        }
        const float Step=4;
        readonly Entry[] entries;readonly int[] order;readonly float length;
        public int Count=>entries.Length;
        public RaceTiming(float trackLength,int[] slots)
        {
            if(trackLength<=0||slots==null||slots.Length==0)throw new ArgumentException("Timing needs a track and entrants.");
            length=trackLength;entries=new Entry[slots.Length];order=new int[slots.Length];
            int capacity=(int)Math.Ceiling(2*trackLength/Step)+4;
            for(int i=0;i<entries.Length;i++){entries[i]=new Entry(capacity,slots[i]);order[i]=i;}
        }
        public void Observe(int id,float time,float legalProgress,bool finished,float finishTime,int recovery)
        {
            var e=entries[id];if(e.finished)return;
            if(!float.IsFinite(time)||!float.IsFinite(legalProgress))throw new ArgumentException("Invalid timing observation.");
            float dt=time-e.time,delta=legalProgress-e.progress;
            bool discontinuity=e.observed&&(recovery!=e.recovery||delta<-.001f||delta>Math.Max(5,dt*150));
            if(discontinuity)e.Clear();
            if(e.observed&&!discontinuity&&dt>0&&delta>0&&time>=0) {
                int first=Math.Max(0,(int)Math.Floor(e.progress/Step)+1),last=(int)Math.Floor(legalProgress/Step);
                for(int i=first;i<=last;i++)e.Put(i,e.time+(time-e.time)*(i*Step-e.progress)/delta);
            }
            if(!e.observed&&legalProgress>=0&&Math.Abs(legalProgress/Step-Math.Round(legalProgress/Step))<.00001)e.Put((int)Math.Round(legalProgress/Step),time);
            e.progress=legalProgress;e.time=time;e.recovery=recovery;e.observed=true;e.finished=finished;e.finishTime=finishTime;
        }
        bool At(Entry e,float progress,out float time)
        {
            time=0;if(!e.observed||progress<0||progress>e.progress+.001f)return false;
            if(Math.Abs(progress-e.progress)<.001f){time=e.time;return true;}
            int lo=(int)Math.Floor(progress/Step);if(!e.Get(lo,out float a))return false;
            if(Math.Abs(progress-lo*Step)<.001f){time=a;return true;}
            float end=(lo+1)*Step;
            if(!e.Get(lo+1,out float b)){end=e.progress;b=e.time;}
            if(end<=lo*Step||progress>end)return false;
            time=a+(b-a)*(progress-lo*Step)/(end-lo*Step);return true;
        }
        public Gap RelativeTo(int other,int player)
        {
            if(other==player)return new Gap(GapKind.Player);
            var a=entries[other];var p=entries[player];
            if(!a.observed||!p.observed||a.time<0||p.time<0)return new Gap(GapKind.Waiting);
            if(a.finished&&p.finished)return new Gap(GapKind.Seconds,a.finishTime-p.finishTime);
            float distance=a.progress-p.progress;
            int laps=(int)Math.Floor(Math.Abs(distance)/length);
            if(laps>=1)return new Gap(GapKind.Laps,distance>0?-laps:laps);
            var front=distance>=0?a:p;var back=distance>=0?p:a;
            if(!At(front,back.progress,out float passage))return new Gap(GapKind.Waiting);
            return new Gap(GapKind.Seconds,(distance>=0?-1:1)*Math.Max(0,back.time-passage));
        }
        bool Before(int a,int b)
        {
            var x=entries[a];var y=entries[b];
            if(x.finished!=y.finished)return x.finished;
            if(x.finished&&x.finishTime!=y.finishTime)return x.finishTime<y.finishTime;
            if(!x.finished&&x.progress!=y.progress)return x.progress>y.progress;
            return x.slot<y.slot;
        }
        public void Sort(){for(int i=1;i<order.Length;i++){int id=order[i],j=i-1;while(j>=0&&Before(id,order[j])){order[j+1]=order[j];j--;}order[j+1]=id;}}
        public int IdAt(int rankIndex)=>order[rankIndex];
        public int Rank(int id){for(int i=0;i<order.Length;i++)if(order[i]==id)return i+1;throw new ArgumentOutOfRangeException(nameof(id));}
        public int WindowStart(int player,int count=11)=>Math.Clamp(Rank(player)-1-count/2,0,Math.Max(0,Count-count));
    }
}

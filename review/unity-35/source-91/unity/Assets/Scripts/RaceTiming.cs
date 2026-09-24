using System;

namespace LittleRoad
{
    // Observation only. Five minutes of120Hz motion per entrant (~6.6MiB/24
    // cars) bounds memory. Binary search preserves stop arrival/departure times.
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
            const int Capacity=36002;
            public readonly float[] positions=new float[Capacity],times=new float[Capacity];
            public float progress,time,finishTime;public int recovery,slot,count,head;public bool observed,finished;
            public Entry(int gridSlot){slot=gridSlot;}
            public void Clear(){count=0;head=0;}
            public int Index(int chronological)=>(head+chronological)%Capacity;
            public float Position(int chronological)=>positions[Index(chronological)];
            public float Time(int chronological)=>times[Index(chronological)];
            public void Put(float position,float time){int at=Index(count);if(count==Capacity){at=head;head=(head+1)%Capacity;}else count++;positions[at]=position;times[at]=time;}
        }
        readonly Entry[] entries;readonly int[] order;readonly float length;
        public int Count=>entries.Length;
        public RaceTiming(float trackLength,int[] slots)
        {
            if(trackLength<=0||slots==null||slots.Length==0)throw new ArgumentException("Timing needs a track and entrants.");
            length=trackLength;entries=new Entry[slots.Length];order=new int[slots.Length];
            for(int i=0;i<entries.Length;i++){entries[i]=new Entry(slots[i]);order[i]=i;}
        }
        public void Observe(int id,float time,float legalProgress,bool finished,float finishTime,int recovery)
        {
            var e=entries[id];if(e.finished)return;
            if(!float.IsFinite(time)||!float.IsFinite(legalProgress))throw new ArgumentException("Invalid timing observation.");
            float dt=time-e.time,delta=legalProgress-e.progress;
            bool discontinuity=e.observed&&(recovery!=e.recovery||delta<-.001f||delta>Math.Max(5,dt*150));
            if(discontinuity)e.Clear();
            if(!e.observed||discontinuity)e.Put(legalProgress,time);
            else if(dt>0&&delta>0) {
                // Preserve the departure as well as the earlier arrival when
                // this position was held for several observations.
                if(e.count>0&&e.Time(e.count-1)<e.time)e.Put(e.progress,e.time);
                e.Put(legalProgress,time);
            }
            e.progress=legalProgress;e.time=time;e.recovery=recovery;e.observed=true;e.finished=finished;e.finishTime=finishTime;
        }
        bool At(Entry e,float progress,out float time)
        {
            time=0;if(!e.observed||progress<0||progress>e.progress+.001f)return false;
            if(e.count==0||progress<e.Position(0)||progress>e.Position(e.count-1))return false;
            int lo=0,hi=e.count-1;
            while(lo<hi){int mid=(lo+hi)/2;if(e.Position(mid)>=progress)hi=mid;else lo=mid+1;}
            float end=e.Position(lo),b=e.Time(lo);
            if(end==progress){time=b;return true;}
            if(lo==0)return false;
            float start=e.Position(lo-1),a=e.Time(lo-1);
            time=a+(b-a)*(progress-start)/(end-start);return true;
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

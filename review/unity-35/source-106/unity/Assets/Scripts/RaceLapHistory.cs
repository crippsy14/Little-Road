namespace LittleRoad
{
    // Only observe authoritative completed laps; grid launch time is not a lap.
    public sealed class RaceLapHistory
    {
        readonly float[] times=new float[3];readonly int[] laps=new int[3];
        int observed;
        public int Count {get;private set;}
        public bool Observe(int completed,float lastTime)
        {
            if(completed<observed){Count=0;observed=completed;return false;}
            if(completed==observed)return false;
            observed=completed;
            if(completed<=0||lastTime<=0||float.IsNaN(lastTime)||float.IsInfinity(lastTime))return false;
            for(int i=2;i>0;i--){times[i]=times[i-1];laps[i]=laps[i-1];}
            times[0]=lastTime;laps[0]=completed;Count=System.Math.Min(3,Count+1);return true;
        }
        public int Lap(int index)=>laps[index];
        public float Seconds(int index)=>times[index];
    }
}

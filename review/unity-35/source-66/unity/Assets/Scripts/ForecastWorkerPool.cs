using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace LittleRoad
{
    // Own the physical input values, not the stack-scoped observation/traffic
    // view. Route/profile geometry is read-only until the joined batch ends.
    public readonly struct ForecastRequest
    {
        readonly ObservedCar self;
        readonly Circuit road;
        readonly CarTuning tuning;
        readonly ContextPathState path;
        readonly Vector3 centre,half;
        readonly double timestamp;
        readonly float step,steering,seconds,allowance;
        readonly int controlSteps;
        readonly bool prepared;
        readonly DriverPlanningSettings planning;
        public ForecastRequest(in DriverObservation o,in ContextPathState path,Vector3 centre,Vector3 half,
            float seconds=2,int controlSteps=3,float allowance=.1f,bool prepared=true,DriverPlanningSettings planning=default)
        {
            this.planning=planning;
            self=o.self;road=o.route as Circuit;tuning=o.tuning;timestamp=o.timestamp;step=o.stepSeconds;steering=o.ownSteeringState;
            this.path=path;this.centre=centre;this.half=half;this.seconds=seconds;this.controlSteps=controlSteps;this.allowance=allowance;this.prepared=prepared;
            if(road==null)throw new ArgumentException("Owned forecast needs a Circuit.");
        }
        // Exact value comparison, avoiding boxing/epsilon vector equality. A
        // stale or changed request falls back to the existing serial forecast.
        public bool Matches(in ForecastRequest b)
        {
            return planning.Same(in b.planning)&&ReferenceEquals(road,b.road)&&timestamp==b.timestamp&&step==b.step&&steering==b.steering&&seconds==b.seconds&&
                allowance==b.allowance&&controlSteps==b.controlSteps&&prepared==b.prepared&&centre.Equals(b.centre)&&half.Equals(b.half)&&
                self.id==b.self.id&&self.position.Equals(b.self.position)&&self.velocity.Equals(b.self.velocity)&&self.forward.Equals(b.self.forward)&&
                self.right.Equals(b.self.right)&&self.colliderCentre.Equals(b.self.colliderCentre)&&self.route==b.self.route&&self.offset==b.self.offset&&
                self.speed==b.self.speed&&self.yawRate==b.self.yawRate&&self.halfWidth==b.self.halfWidth&&self.halfLength==b.self.halfLength&&
                tuning.power==b.tuning.power&&tuning.acceleration==b.tuning.acceleration&&tuning.mass==b.tuning.mass&&tuning.gearCeiling==b.tuning.gearCeiling&&
                tuning.braking==b.tuning.braking&&tuning.roadGrip==b.tuning.roadGrip&&tuning.dirtGrip==b.tuning.dirtGrip&&tuning.response==b.tuning.response&&tuning.yaw==b.tuning.yaw&&
                path.anchor==b.path.anchor&&path.consumed==b.path.consumed&&path.startOffset==b.path.startOffset&&path.startSlope==b.path.startSlope&&
                path.endOffset==b.path.endOffset&&path.blend==b.path.blend&&path.selectedOffset==b.path.selectedOffset&&path.rejoin==b.path.rejoin&&path.guide==b.path.guide&&path.guideOffset==b.path.guideOffset&&path.guideSlope==b.path.guideSlope&&path.coast==b.path.coast;
        }
        public void Run(ContextPathPrediction prediction)
        {
            if(!planning.Same(in prediction.planning))throw new InvalidOperationException("Forecast request and owned driver's speed profile differ.");
            var o=new DriverObservation(timestamp,step,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false,steering);
            prediction.Predict(in o,path,centre,half,seconds,false,controlSteps,true,allowance,prepared);
        }
    }

    public sealed class ForecastWork
    {
        internal readonly int owner=Thread.CurrentThread.ManagedThreadId;
        internal bool active,ready;
        ForecastRequest request;
        public readonly ContextPathPrediction prediction;
        public Exception Error {get;private set;}
        public bool Completed {get;private set;}
        public double ElapsedMs {get;private set;}
        public long AllocatedBytes {get;private set;}
        public ForecastWork(ContextPathPrediction prediction){this.prediction=prediction??throw new ArgumentNullException(nameof(prediction));}
        public void Set(in ForecastRequest request)
        {
            if(Thread.CurrentThread.ManagedThreadId!=owner||active)throw new InvalidOperationException("Forecast input belongs to the idle owning thread.");
            this.request=request;ready=true;Completed=false;Error=null;
        }
        internal void Begin(){active=true;Completed=false;Error=null;ElapsedMs=0;AllocatedBytes=0;}
        internal void Execute()
        {
            long start=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread();
            try{request.Run(prediction);Completed=true;}
            catch(Exception error){Error=error;}
            finally{ElapsedMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;AllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;}
        }
    }

    // Isolated synchronous batch boundary. Persistent background workers run
    // pure forecasts only; the caller joins before touching results/scene data.
    // Live use is opt-in; scene queries and final decisions remain on the caller.
    public sealed class ForecastWorkerPool:IDisposable
    {
        sealed class Worker
        {
            internal readonly AutoResetEvent wake=new(false);
            internal readonly Thread thread;
            readonly ForecastWorkerPool pool;
            internal Worker(ForecastWorkerPool pool,int index)
            {this.pool=pool;thread=new Thread(Loop){IsBackground=true,Name="Little Road forecast "+index};}
            void Loop()
            {
                for(;;) {
                    wake.WaitOne();if(pool.stopping)return;
                    for(;;) {
                        int index=Interlocked.Increment(ref pool.next);
                        if(index>=pool.count)break;
                        pool.jobs[index].Execute();
                    }
                    if(Interlocked.Decrement(ref pool.remaining)==0)pool.finished.Set();
                }
            }
        }
        readonly int owner=Thread.CurrentThread.ManagedThreadId;
        readonly Worker[] workers;
        readonly ManualResetEvent finished=new(false);
        ForecastWork[] jobs;int count,next,remaining;
        volatile bool stopping;
        bool disposed,running;
        public double ElapsedMs {get;private set;}
        public long MainAllocatedBytes {get;private set;}
        public int WorkerCount=>workers.Length;
        public ForecastWorkerPool(int workerCount)
        {
            if(workerCount<1||workerCount>24)throw new ArgumentOutOfRangeException(nameof(workerCount));
            workers=new Worker[workerCount];
            for(int i=0;i<workers.Length;i++)workers[i]=new Worker(this,i);
            try{foreach(var worker in workers)worker.thread.Start();}
            catch{Dispose();throw;}
        }
        void RequireOwner()
        {if(Thread.CurrentThread.ManagedThreadId!=owner)throw new InvalidOperationException("Forecast batch must be dispatched/joined/disposed by its owning thread.");}
        public void Run(ForecastWork[] work,int length)
        {
            RequireOwner();if(disposed)throw new ObjectDisposedException(nameof(ForecastWorkerPool));
            if(running)throw new InvalidOperationException("Forecast batch is already active.");
            long start=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread();
            if(work==null||length<0||length>work.Length)throw new ArgumentException("Invalid forecast batch.");
            // No mutable prediction/follower/envelope buffers may be shared by
            // two jobs, even if callers pass distinct work wrappers.
            for(int i=0;i<length;i++) {
                if(work[i]==null||!work[i].ready||work[i].active||work[i].owner!=owner)throw new ArgumentException("Forecast job is not owned and ready.");
                for(int j=0;j<i;j++)if(ReferenceEquals(work[i].prediction,work[j].prediction))throw new ArgumentException("Forecast jobs share mutable prediction buffers.");
            }
            if(length>0) {
                running=true;jobs=work;count=length;next=-1;remaining=workers.Length;finished.Reset();
                for(int i=0;i<length;i++)work[i].Begin();
                foreach(var worker in workers)worker.wake.Set();
                finished.WaitOne();
                // All workers have left the input/result buffers. Preserve the
                // exception on its job and permit a subsequent valid batch.
                for(int i=0;i<length;i++)work[i].active=false;
                jobs=null;count=0;running=false;
            }
            ElapsedMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
            MainAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
            for(int i=0;i<length;i++)if(work[i].Error!=null)throw new InvalidOperationException("Forecast job "+i+" failed.",work[i].Error);
        }
        public void Dispose()
        {
            RequireOwner();if(disposed)return;
            if(running)throw new InvalidOperationException("Cannot dispose an active forecast batch.");
            stopping=true;
            foreach(var worker in workers)worker.wake.Set();
            foreach(var worker in workers) {
                if((worker.thread.ThreadState&System.Threading.ThreadState.Unstarted)==0)worker.thread.Join();
                worker.wake.Dispose();
            }
            finished.Dispose();disposed=true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using LittleRoad;
using UnityEngine;

public static class ForecastWorkerChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception("Forecast workers: "+message);}
    static bool ThreadSafeBinding(Type type,string name)
    {
        foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)) {
            if(method.Name!=name&&method.Name!="Internal_"+name)continue;
            foreach(var attribute in method.GetCustomAttributesData())foreach(var argument in attribute.NamedArguments)
                if(argument.MemberName=="IsThreadSafe"&&argument.TypedValue.Value is bool value&&value)return true;
        }
        return false;
    }
    static void Equal(ContextPathPrediction expected,ContextPathPrediction actual)
    {
        Require(expected.Count==actual.Count,"sample count changed");
        for(int i=0;i<expected.Count;i++)Require(expected.samples[i].Equals(actual.samples[i]),"prediction sample changed at "+i);
        Require(expected.envelope.Queries==actual.envelope.Queries&&expected.envelope.Contained==actual.envelope.Contained&&
            expected.envelope.MaximumBound==actual.envelope.MaximumBound&&expected.envelope.FirstUncertainTime==actual.envelope.FirstUncertainTime,"envelope result changed");
        for(int i=0;i<expected.Count-1;i++)Require(expected.envelope.intervalUpperBounds[i]==actual.envelope.intervalUpperBounds[i],"interval bound changed");
        Require(expected.PreparedHits==actual.PreparedHits&&expected.PreparedMisses==actual.PreparedMisses,"cache coverage changed");
    }
    public static List<string> Run()
    {
        var results=new List<string>();
        Require(ThreadSafeBinding(typeof(Quaternion),"AngleAxis")&&ThreadSafeBinding(typeof(Vector3),"Slerp"),"installed native math binding is not marked thread-safe");
        results.Add("Installed AngleAxis/Slerp native bindings declare thread-safe execution");
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var tunings=new[]{CarTuning.Stock,CarTuning.Street,CarTuning.Club};
        var profiles=new RacingSpeedProfile[3];for(int i=0;i<3;i++)profiles[i]=new RacingSpeedProfile(line,tunings[i]);
        var jobs=new ForecastWork[24];var controls=new ContextPathPrediction[24];var requests=new ForecastRequest[24];
        using(var pool=new ForecastWorkerPool(4)) {
            foreach(bool prepared in new[]{false,true}) {
                for(int i=0;i<jobs.Length;i++) {
                    float d=road.length*i/jobs.Length+.137f,side=(i%3-1)*5.3f,speed=12+i;
                    var forward=road.Forward(d);var right=road.Right(d);
                    var self=new ObservedCar(i,road.At(d)+right*side+Vector3.up*.7f,forward*speed+right*.3f,forward,right,d,side,speed,1.2f,2.3f,.04f);
                    var o=new DriverObservation(100,1f/120,self,Array.Empty<ObservedCar>(),road,tunings[i%3],1,0,false,.1f);
                    var path=RoadPathFeasibility.FromMotion(in o,-side,i%2==0);
                    requests[i]=new ForecastRequest(in o,in path,new Vector3(.03f,.5f,.1f),new Vector3(1.2f,.6f,2.3f),prepared:prepared);
                    controls[i]=new ContextPathPrediction(profiles[i%3]);requests[i].Run(controls[i]);
                    jobs[i]=new ForecastWork(new ContextPathPrediction(profiles[i%3]));jobs[i].Set(in requests[i]);
                }
                // Every repetition uses the same owned buffers. Reversing the
                // dispatch order exposes accidental dependence on worker order.
                var original=(ForecastWork[])jobs.Clone();
                for(int repeat=0;repeat<4;repeat++) {
                    pool.Run(jobs,jobs.Length);
                    for(int i=0;i<jobs.Length;i++) {
                        var job=jobs[i];int id=Array.IndexOf(original,job);
                        Require(job.Completed&&job.Error==null,"job did not complete");Equal(controls[id],job.prediction);
                        if(repeat>0)Require(job.AllocatedBytes==0,"recurring worker allocation "+job.AllocatedBytes);
                    }
                    if(repeat>0)Require(pool.MainAllocatedBytes==0,"recurring dispatch/join allocation "+pool.MainAllocatedBytes);
                    Array.Reverse(jobs);
                }
                results.Add("Four concurrent 24-forecast batches exactly match serial samples/envelopes with no warm allocations; prepared="+prepared);
            }
            var duplicate=new[]{jobs[0],new ForecastWork(jobs[0].prediction)};duplicate[1].Set(in requests[0]);
            bool rejected=false;try{pool.Run(duplicate,2);}catch(ArgumentException){rejected=true;}Require(rejected,"shared mutable prediction buffers accepted");
            results.Add("Concurrent batch rejects shared mutable prediction/follower/envelope buffers");
            var good=requests[0];var invalid=default(ForecastRequest);jobs[0].Set(in invalid);
            bool failed=false;try{pool.Run(jobs,jobs.Length);}catch(InvalidOperationException error){failed=error.InnerException is ArgumentException;}
            Require(failed&&!jobs[0].Completed&&jobs[0].Error!=null,"worker exception not propagated or partial result marked complete");
            jobs[0].Set(in good);pool.Run(jobs,jobs.Length);Require(jobs[0].Completed,"valid batch failed after earlier exception");
            pool.Run(jobs,0);
            results.Add("Worker exceptions join cleanly; later valid and empty batches remain usable");
        }
        for(int i=0;i<3;i++) {
            var pool=new ForecastWorkerPool(2);pool.Run(jobs,1);pool.Dispose();pool.Dispose();
            bool rejected=false;try{pool.Run(jobs,1);}catch(ObjectDisposedException){rejected=true;}
            Require(rejected,"disposed pool accepted work");
        }
        results.Add("Repeated worker startup/join/shutdown is clean and disposed pools reject work");
        return results;
    }
}

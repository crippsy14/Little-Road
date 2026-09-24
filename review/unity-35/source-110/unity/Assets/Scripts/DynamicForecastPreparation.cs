using System;
using System.Diagnostics;
using UnityEngine;

namespace LittleRoad
{
    // An owned pure-data stage inside the joined forecast job. Circuit/profile
    // geometry is immutable while workers run; no scene or physics queries.
    public sealed class DynamicForecastPreparation
    {
        public readonly DynamicRejoinPlanner Planner;
        readonly DriverPlanningSettings planning;
        readonly Vector3 centre,half;readonly float allowance;readonly bool preparedPath;
        readonly ObservedCar[] traffic=new ObservedCar[24];int count;
        ObservedCar self;Circuit route;CarTuning tuning;ContextPathState previous;
        double timestamp;float step,pace,preferred,steering,drag;
        bool finished,drafting,hasPrevious,trafficMode;
        public int Calls {get;private set;}
        public int Forecasts {get;private set;}
        public double TotalMs {get;private set;}
        public DynamicForecastPreparation(DynamicRejoinPlanner planner,DriverPlanningSettings planning,Vector3 centre,Vector3 half,float allowance=.1f,bool preparedPath=true)
        {Planner=planner??throw new ArgumentNullException(nameof(planner));this.planning=planning;this.centre=centre;this.half=half;this.allowance=allowance;this.preparedPath=preparedPath;}
        internal void Capture(in DriverObservation o,in ContextPathState prior,bool hasPrior,bool trafficAware)
        {
            if(o.traffic.Length>traffic.Length)throw new ArgumentException("Dynamic forecast traffic capacity exceeded.");
            route=o.route as Circuit??throw new ArgumentException("Dynamic forecast needs immutable Circuit geometry.");
            self=o.self;tuning=o.tuning;timestamp=o.timestamp;step=o.stepSeconds;pace=o.pace;preferred=o.preferredOffset;
            steering=o.ownSteeringState;drag=o.dragReduction;finished=o.finished;drafting=o.drafting;
            previous=prior;hasPrevious=hasPrior;trafficMode=trafficAware;count=o.traffic.Length;o.traffic.CopyTo(traffic);
        }
        internal bool Prepare(out ForecastRequest request)
        {
            var o=new DriverObservation(timestamp,step,self,new ReadOnlySpan<ObservedCar>(traffic,0,count),route,tuning,pace,preferred,finished,steering,drafting,drag);
            long started=Stopwatch.GetTimestamp();
            Planner.PrepareOwned(in o,in previous,hasPrevious,trafficMode);
            TotalMs+=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;Calls++;
            if(Planner.Count==0||!float.IsFinite(Planner.EstimatedTime(0))){request=default;return false;}
            var path=Planner.Path(0);
            request=new ForecastRequest(in o,in path,centre,half,allowance:allowance,prepared:preparedPath,planning:planning);Forecasts++;return true;
        }
    }
}

using System;
using UnityEngine;

namespace LittleRoad
{
    // Opt-in plan evidence. Copies owned observations only on a captured event;
    // allocations make this a diagnostic run, not performance acceptance.
    public sealed class ContextPlanAudit
    {
        [Serializable] public struct Car
        {
            public int id;
            public Vector3 position,velocity,forward,right,colliderCentre;
            public float route,offset,speed,yawRate,halfWidth,halfLength;
            public Car(in ObservedCar s){id=s.id;position=s.position;velocity=s.velocity;forward=s.forward;right=s.right;colliderCentre=s.colliderCentre;
                route=s.route;offset=s.offset;speed=s.speed;yawRate=s.yawRate;halfWidth=s.halfWidth;halfLength=s.halfLength;}
        }
        [Serializable] public struct Candidate
        {
            public ContextPathState path;
            public string reason;
            public float afterSeconds,offset,roadBound;
            public float predictedRouteGain,predictedEndSpeed;
            public Vector3 position,velocity,forward;
        }
        [Serializable] public sealed class Entry
        {
            public double timestamp;
            public float stepSeconds,steering,left,right;
            public bool finished;
            public Car self;
            public Car[] traffic;
            public Candidate[] candidates;
            public CarTuning tuning;
            public bool selectionSwitch,hasPreviousPath;
            public ContextPathState previousPath,selectedPath;
            public float[] preferenceOffsets,preferenceScores;
            public bool[] initiallyReachable;
        }
        [Serializable] public sealed class Evidence
        {
            public string scope="Failed live selections only, exact pre-controller snapshot and candidate rejection state. Observation-only with bounded capture; allocations exclude this run from performance acceptance. Current corridor limits are not future opponent trajectories.";
            public int dropped;
            public Entry[] entries;
        }
        readonly Candidate[] candidates=new Candidate[8];
        readonly float[] offsets=new float[ContextPreferenceMap.Capacity],scores=new float[ContextPreferenceMap.Capacity];
        readonly bool[] reachable=new bool[ContextPreferenceMap.Capacity];
        readonly Entry[] entries=new Entry[64];
        int candidatesUsed,count,dropped,mapCount;
        public void Begin(){candidatesUsed=0;mapCount=0;}
        public void CapturePreferences(ContextPreferenceMap map)
        {
            mapCount=map.Count;
            for(int i=0;i<mapCount;i++){offsets[i]=map.Sample(i);scores[i]=map.value[i];reachable[i]=map.reachable[i];}
        }
        public void Add(in Candidate candidate){if(candidatesUsed<candidates.Length)candidates[candidatesUsed++]=candidate;}
        public void Record(in DriverObservation o,float left,float right,bool selectionSwitch=false,ContextPathState previousPath=default,ContextPathState selectedPath=default)
        {
            if(count==entries.Length){dropped++;return;}
            var traffic=new Car[o.traffic.Length];for(int i=0;i<traffic.Length;i++)traffic[i]=new Car(in o.traffic[i]);
            var attempts=new Candidate[candidatesUsed];Array.Copy(candidates,attempts,candidatesUsed);
            var capturedOffsets=new float[mapCount];var capturedScores=new float[mapCount];var capturedReachable=new bool[mapCount];
            Array.Copy(offsets,capturedOffsets,mapCount);Array.Copy(scores,capturedScores,mapCount);Array.Copy(reachable,capturedReachable,mapCount);
            entries[count++]=new Entry{timestamp=o.timestamp,stepSeconds=o.stepSeconds,steering=o.ownSteeringState,finished=o.finished,
                self=new Car(in o.self),traffic=traffic,tuning=o.tuning,left=left,right=right,candidates=attempts,
                selectionSwitch=selectionSwitch,hasPreviousPath=selectionSwitch,previousPath=previousPath,selectedPath=selectedPath,
                preferenceOffsets=capturedOffsets,preferenceScores=capturedScores,initiallyReachable=capturedReachable};
        }
        public Evidence Export(string scope=null){var copy=new Entry[count];Array.Copy(entries,copy,count);var result=new Evidence{entries=copy,dropped=dropped};if(scope!=null)result.scope=scope;return result;}
    }
}

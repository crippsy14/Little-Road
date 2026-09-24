using System;
using UnityEngine;

namespace LittleRoad
{
    // Opt-in failed-plan evidence. Copies owned observations only on failure;
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
        }
        [Serializable] public sealed class Evidence
        {
            public string scope="Failed live selections only, exact pre-controller snapshot and candidate rejection state. Observation-only with bounded capture; allocations exclude this run from performance acceptance. Current corridor limits are not future opponent trajectories.";
            public int dropped;
            public Entry[] entries;
        }
        readonly Candidate[] candidates=new Candidate[6];
        readonly Entry[] entries=new Entry[64];
        int candidatesUsed,count,dropped;
        public void Begin(){candidatesUsed=0;}
        public void Add(in Candidate candidate){if(candidatesUsed<candidates.Length)candidates[candidatesUsed++]=candidate;}
        public void Record(in DriverObservation o,float left,float right)
        {
            if(count==entries.Length){dropped++;return;}
            var traffic=new Car[o.traffic.Length];for(int i=0;i<traffic.Length;i++)traffic[i]=new Car(in o.traffic[i]);
            var attempts=new Candidate[candidatesUsed];Array.Copy(candidates,attempts,candidatesUsed);
            entries[count++]=new Entry{timestamp=o.timestamp,stepSeconds=o.stepSeconds,steering=o.ownSteeringState,finished=o.finished,
                self=new Car(in o.self),traffic=traffic,tuning=o.tuning,left=left,right=right,candidates=attempts};
        }
        public Evidence Export(){var copy=new Entry[count];Array.Copy(entries,copy,count);return new Evidence{entries=copy,dropped=dropped};}
    }
}

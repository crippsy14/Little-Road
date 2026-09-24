using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    [DefaultExecutionOrder(-100)]
    public sealed class PathPredictionFixture:MonoBehaviour
    {
        [Serializable] public struct Comparison
        {
            public float time,positionError,speedError,headingError,predictedBodyOffset,actualBodyOffset,predictedPedal,actualPedal,predictedSteering,actualSteering;
            public bool flatTarmacDomain;public Vector3 predictedPosition,actualPosition;
        }
        [Serializable] sealed class Evidence
        {
            public string scenario,scope="One frozen continuous path for two seconds. Forecast commands are generated before the car drives it, with an independent instance of the same follower. Actual car uses ordinary Rigidbody forces. No later input history enters the forecast.";
            public float start,startOffset,destination,initialSpeed,horizon=2,physicsStep,forecastMilliseconds;
            public int comparisons,contactsBefore,contactsAfter,forecastControlSteps=1;public bool completed;
            public float predictedDeparture=-1,actualDeparture=-1;
            public Comparison[] samples;
            public ContextPathPrediction.Cost[] forecastCosts;public bool warmForecastParity=true;
            public bool adaptiveEnvelope;public int envelopeQueries,envelopeAuditSamples;
            public float envelopeFirstUncertain=-1,envelopeMaximumBound,envelopeMaximumUnderestimate;
            public PathApproachAudit.Capture sourceApproach;public ContextPathState testedPath;
        }
        RaceTrial race;RoadCar car;BoxCollider shape;Evidence evidence;
        ContextPathState actualPath;ContextPathFollower follower;ContextPathPrediction forecast;ReferenceDriverPolicy reference;
        bool released,started,domain=true;int tick;float lastRoute;
        PathApproachAudit.Capture approach;
        readonly List<Comparison> comparisons=new();
        public void Setup(RaceTrial owner,TechnicalCircuit course)
        {
            race=owner;if(course==null||race.cars.Count!=1)throw new ArgumentException("Path probes require one car on Cedar.");
            string scenario=RaceTrial.Arg("--path-probe");
            if(Array.IndexOf(new[]{"rejoin-left","rejoin-right","switchback-left","switchback-right","overspeed-left","overspeed-right","approach-current","approach-reanchor","approach-guide"},scenario)<0)throw new ArgumentException("Unknown path probe.");
            if(scenario.StartsWith("approach-"))approach=PathApproachAudit.Replay();
            bool rejoin=scenario.StartsWith("rejoin-");bool overspeed=scenario.StartsWith("overspeed-");float side=scenario.EndsWith("left")?-1:1;
            float start=rejoin?course.regionStarts[13]+190:overspeed?course.regionStarts[12]-12:course.regionStarts[11]-12;
            float offset=side*(rejoin?4:5.5f),speed=rejoin?18:overspeed?40:24;
            if(approach!=null){start=approach.route;offset=approach.offset;speed=approach.speed;}
            evidence=new Evidence{scenario=scenario,start=start,startOffset=offset,destination=rejoin?0:offset,initialSpeed=speed,physicsStep=Time.fixedDeltaTime};
            evidence.sourceApproach=approach;
            string cadence=RaceTrial.Arg("--path-control-steps");
            evidence.adaptiveEnvelope=Array.IndexOf(Environment.GetCommandLineArgs(),"--path-adaptive-envelope")>=0;
            if(cadence!=null&&(!int.TryParse(cadence,out evidence.forecastControlSteps)||evidence.forecastControlSteps<1||evidence.forecastControlSteps>6))throw new ArgumentException("Forecast control steps must be 1..6.");
            car=race.cars[0];car.PlaceTrafficFixture(start,offset);shape=car.GetComponent<BoxCollider>();
            if(approach!=null&&(JsonUtility.ToJson(car.tuning)!=JsonUtility.ToJson(approach.tuning)||Vector3.Distance(shape.center,approach.colliderCentre)>.001f||Vector3.Distance(shape.size*.5f,approach.colliderHalf)>.001f))throw new ArgumentException("Replay tuning/body geometry must match captured car.");
            follower=new ContextPathFollower(car.referenceProfile);forecast=new ContextPathPrediction(car.referenceProfile);reference=new ReferenceDriverPolicy(car.referenceProfile);
            car.commandSource=Command;Physics.SyncTransforms();
        }
        void FixedUpdate()
        {
            if(race==null||!race.running)return;
            if(!released&&race.elapsed>=-Time.fixedDeltaTime*1.01f){released=true;
                if(approach!=null){car.RestoreApproachFixture(approach);Physics.SyncTransforms();}
                else car.body.linearVelocity=car.transform.forward*evidence.initialSpeed;}
        }
        Vector2 Command(RoadCar vehicle)
        {
            if(race.elapsed<0)return Vector2.zero;
            var self=new ObservedCar(car.driverId,car.body.position,car.body.linearVelocity,car.transform.forward,car.transform.right,
                car.pathDistance,car.offset,car.speed,car.CollisionHalf.x,car.CollisionHalf.z,car.body.angularVelocity.y);
            var o=new DriverObservation(Time.fixedTimeAsDouble,Time.fixedDeltaTime,self,Array.Empty<ObservedCar>(),race.track,car.tuning,1,0,false,car.SteeringState);
            if(!started) {
                started=true;evidence.contactsBefore=car.contacts;lastRoute=car.pathDistance;
                actualPath=new ContextPathState{anchor=lastRoute,startOffset=car.offset,endOffset=evidence.destination,selectedOffset=evidence.destination,
                    blend=Mathf.Clamp(18+Mathf.Max(0,car.speed)*.9f,22,60)};
                if(approach!=null) {
                    actualPath=approach.path;
                    if(evidence.scenario!="approach-current") {
                        actualPath.anchor=lastRoute;actualPath.consumed=0;actualPath.startOffset=car.offset;
                        actualPath.startSlope=Mathf.Clamp(Vector3.Dot(car.body.linearVelocity,race.track.Right(lastRoute))/Mathf.Max(3,Vector3.Dot(car.body.linearVelocity,race.track.Forward(lastRoute))),-.35f,.35f);
                        actualPath.blend=Mathf.Clamp(18+Mathf.Max(0,car.speed)*.9f,22,60);
                        if(evidence.scenario=="approach-guide") {actualPath.endOffset=car.referenceProfile.line.Offset(lastRoute+actualPath.blend);actualPath.rejoin=true;}
                        actualPath.selectedOffset=actualPath.endOffset;
                    }
                    evidence.destination=actualPath.endOffset;
                }
                evidence.testedPath=actualPath;
                evidence.forecastCosts=new ContextPathPrediction.Cost[4];ContextPathPrediction.Sample[] cold=null;float[] coldBounds=null;
                for(int repeat=0;repeat<4;repeat++) {
                    forecast.Predict(in o,actualPath,shape.center,shape.size*.5f,evidence.horizon,true,evidence.forecastControlSteps,evidence.adaptiveEnvelope);
                    evidence.forecastCosts[repeat]=forecast.LastCost;
                    if(repeat==0){cold=new ContextPathPrediction.Sample[forecast.Count];Array.Copy(forecast.samples,cold,forecast.Count);}
                    else for(int k=0;k<forecast.Count;k++)evidence.warmForecastParity&=cold[k].Equals(forecast.samples[k]);
                    if(evidence.adaptiveEnvelope) {
                        if(repeat==0)coldBounds=(float[])forecast.envelope.intervalUpperBounds.Clone();
                        else for(int k=0;k<forecast.Count-1;k++)evidence.warmForecastParity&=coldBounds[k]==forecast.envelope.intervalUpperBounds[k];
                    }
                }
                evidence.forecastMilliseconds=(float)evidence.forecastCosts[0].totalMs;
                if(evidence.adaptiveEnvelope) {
                    evidence.envelopeQueries=forecast.envelope.Queries;
                    evidence.envelopeFirstUncertain=forecast.envelope.FirstUncertainTime;
                    evidence.envelopeMaximumBound=forecast.envelope.MaximumBound;
                    // Deliberately outside timing: dense original signed-offset
                    // queries and eight substeps independently audit the bounds.
                    for(int k=0;k<forecast.Count;k++) {
                        var item=forecast.samples[k];item.bodyOffset=item.state.MaxBodyOffset(race.track,shape.center,shape.size*.5f);forecast.samples[k]=item;
                        if(k==0)continue;
                        var a=forecast.samples[k-1].state;var b=item.state;
                        for(int j=0;j<=8;j++) {
                            float t=j/8f;var state=a;
                            state.position=Vector3.Lerp(a.position,b.position,t);state.forward=Vector3.Slerp(a.forward,b.forward,t);
                            float offset=state.MaxBodyOffset(race.track,shape.center,shape.size*.5f);
                            evidence.envelopeMaximumUnderestimate=Mathf.Max(evidence.envelopeMaximumUnderestimate,offset-forecast.envelope.intervalUpperBounds[k-1]);
                            evidence.envelopeAuditSamples++;
                        }
                    }
                }
            }
            actualPath.consumed+=Mathf.Max(0,Mathf.Repeat(car.pathDistance-lastRoute+race.track.length*.5f,race.track.length)-race.track.length*.5f);lastRoute=car.pathDistance;
            var command=follower.Follow(in o,in actualPath);
            if(tick<forecast.Count) {
                var predicted=forecast.samples[tick];var delta=predicted.state.position-car.body.position;delta.y=0;
                var velocity=car.body.linearVelocity;velocity.y=0;
                float body=MotionPredictionAudit.ActualBodyOffset(car,shape);
                domain&=(car.ground==null||car.ground.grounded&&car.ground.normal.y>.999f)&&car.transform.up.y>.999f&&Mathf.Abs(car.offset)<Circuit.Width*.5f&&car.contacts==evidence.contactsBefore;
                comparisons.Add(new Comparison{time=predicted.time,positionError=delta.magnitude,speedError=Mathf.Abs(predicted.state.velocity.magnitude-velocity.magnitude),headingError=Vector3.Angle(predicted.state.forward,car.transform.forward),
                    predictedBodyOffset=predicted.bodyOffset,actualBodyOffset=body,predictedPedal=predicted.command.pedal,actualPedal=command.pedal,predictedSteering=predicted.command.steering,actualSteering=command.steering,
                    predictedPosition=predicted.state.position,actualPosition=car.body.position,flatTarmacDomain=domain});
                if(predicted.bodyOffset>Circuit.Width*.5f&&evidence.predictedDeparture<0)evidence.predictedDeparture=predicted.time;
                if(body>Circuit.Width*.5f&&evidence.actualDeparture<0)evidence.actualDeparture=predicted.time;
            }
            if(++tick>=forecast.Count) {
                evidence.completed=true;evidence.contactsAfter=car.contacts;car.commandSource=null;
                command=reference.Decide(in o);
            }
            return new Vector2(command.pedal,command.steering);
        }
        public void Save()
        {
            string report=RaceTrial.Arg("--trial-report");if(report==null)return;
            evidence.comparisons=comparisons.Count;evidence.samples=comparisons.ToArray();
            File.WriteAllText(Path.ChangeExtension(report,"path-prediction.json"),JsonUtility.ToJson(evidence,true));
        }
    }
}

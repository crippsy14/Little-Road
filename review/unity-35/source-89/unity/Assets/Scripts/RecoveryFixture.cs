using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Declared synthetic initial conditions and read-only evidence afterwards.
    // Obstacles in blocked-wait are removed after nine seconds to model a gap
    // becoming available; the separate traffic case uses a moving physical car.
    [DefaultExecutionOrder(100)]
    public sealed class RecoveryFixture:MonoBehaviour
    {
        [Serializable] public sealed class Event {public float time,progress,offset,speed;public int gate,resets;public string state;}
        [Serializable] public sealed class Evidence
        {
            public string fixture,scope="Synthetic recovery start; ordinary physics thereafter. Reset counts remain assisted recovery.";
            public bool complete,returnedBehindMissedGate,unearnedGateAdvance,trafficPassed;
            public float maxStepMetres,maxPhysicalStepMetres,waitingSeconds,reverseSeconds,activeSeconds;
            public int observedResetJumps;
            public int physicalRecoveries,assistedResets,contacts;public Event[] events;
        }
        RaceTrial race;RoadCar car;string fixture,lastState;
        readonly List<GameObject> blocks=new();readonly List<Event> events=new();
        Vector3 previous;bool began,cleared,returned,unearned,passed;float maximumStep,maximumPhysicalStep;int priorResets,resetJumps;
        public void Setup(RaceTrial owner)
        {
            race=owner;car=race.cars[0];fixture=RaceTrial.Arg("--recovery-case");
            if(car.physicalRecovery==null)throw new ArgumentException("Recovery fixtures require --physical-recovery.");
            switch(fixture) {
                case "sideways":car.PlaceRecoveryFixture(-24,0,90,0);break;
                case "wrong-way":car.PlaceRecoveryFixture(-24,0,180,0);break;
                case "off-road":car.PlaceRecoveryFixture(-24,11,0,0);break;
                case "wall":car.PlaceRecoveryFixture(-24,0,0,0);Block(-20.5f,0,5);break;
                case "missed-gate":car.PlaceRecoveryFixture(8,11,0,0);break;
                case "blocked-wait":case "long-blocked-wait":car.PlaceRecoveryFixture(-24,0,0,0);Block(-20.5f,0,26);Block(-27.5f,0,26);break;
                case "permanent-block":
                    car.PlaceRecoveryFixture(-24,10.5f,0,0);
                    Block(-20.5f,10.5f,6);Block(-27.5f,10.5f,6);Block(-24,7.5f,.3f,7);Block(-24,13.5f,.3f,7);
                    // The first nominal fallback location is itself obstructed.
                    // The car must find a clear location farther back.
                    Block(-28,0,4);break;
                case "traffic":
                    if(race.cars.Count!=2)throw new ArgumentException("Traffic recovery needs exactly two cars.");
                    car.PlaceRecoveryFixture(-24,10.5f,-45,0);race.cars[1].PlaceRecoveryFixture(-58,0,0,0);break;
                default:throw new ArgumentException("Unknown recovery fixture: "+fixture);
            }
            Physics.SyncTransforms();previous=car.body.position;
        }
        void Block(float distance,float side,float width,float depth=.3f)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Recovery fixture barrier";go.tag="Respawn";
            go.transform.SetPositionAndRotation(race.track.At(distance)+race.track.Right(distance)*side+Vector3.up,Quaternion.LookRotation(race.track.Forward(distance)));
            go.transform.localScale=new Vector3(width,2,depth);go.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Recovery safety barrier",new Color(.9f,.45f,.12f));blocks.Add(go);
        }
        void FixedUpdate()
        {
            if(race==null||!race.running||race.elapsed<0||car.finished)return;
            if(!began){began=true;previous=car.body.position;
                if(fixture=="traffic")race.cars[1].body.linearVelocity=race.track.Forward(race.cars[1].pathDistance)*22;
            }
            float step=Vector3.Distance(previous,car.body.position);maximumStep=Mathf.Max(maximumStep,step);
            if(car.recoveries==priorResets)maximumPhysicalStep=Mathf.Max(maximumPhysicalStep,step);else resetJumps++;
            previous=car.body.position;priorResets=car.recoveries;
            float release=fixture=="blocked-wait"?9:fixture=="long-blocked-wait"?40:float.PositiveInfinity;
            if(!cleared&&race.elapsed>=release){foreach(var block in blocks){block.GetComponent<Collider>().enabled=false;Destroy(block);}cleared=true;Physics.SyncTransforms();}
            if(car.progress<-.5f)returned=true;
            if(fixture=="missed-gate"&&!returned&&car.nextGate!=0)unearned=true;
            if(fixture=="traffic"&&race.cars[1].progress>car.progress+6)passed=true;
            string state=car.physicalRecovery.State;
            if(state!=lastState){events.Add(new Event{time=race.elapsed,progress=car.progress,offset=car.offset,speed=car.speed,gate=car.nextGate,resets=car.recoveries,state=state});lastState=state;}
        }
        public void Save()
        {
            var path=RaceTrial.Arg("--trial-report");if(path==null)return;
            var data=new Evidence{fixture=fixture,complete=car.finished,returnedBehindMissedGate=returned,unearnedGateAdvance=unearned,trafficPassed=passed,maxStepMetres=maximumStep,maxPhysicalStepMetres=maximumPhysicalStep,observedResetJumps=resetJumps,
                waitingSeconds=car.physicalRecovery.waitSeconds,reverseSeconds=car.physicalRecovery.reverseSeconds,activeSeconds=car.physicalRecovery.activeSeconds,
                physicalRecoveries=car.physicalRecovery.completed,assistedResets=car.recoveries,contacts=car.contacts,events=events.ToArray()};
            File.WriteAllText(Path.ChangeExtension(path,"recovery.json"),JsonUtility.ToJson(data,true));
        }
        void OnGUI()
        {
            if(car==null)return;using var hud=new DisplaySettings.HudScale(true);
            GUI.color=new Color(.07f,.16f,.19f,.92f);GUI.DrawTexture(new Rect(20,235,570,64),Texture2D.whiteTexture);GUI.color=Color.white;
            var style=new GUIStyle(GUI.skin.label){fontSize=18};
            GUI.Label(new Rect(34,239,540,27),$"RECOVERY TEST · {fixture} · {car.physicalRecovery.State}",style);
            GUI.Label(new Rect(34,267,540,25),$"Physical rejoins {car.physicalRecovery.completed} · assisted resets {car.recoveries} · wait {car.physicalRecovery.waitSeconds:0.0}s");
        }
    }
}

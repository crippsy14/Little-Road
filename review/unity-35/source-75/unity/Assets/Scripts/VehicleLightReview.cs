using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LittleRoad
{
    // Exercises real input -> controller -> material updates in the game scene.
    // Synthetic triggers validate integration, not a connected DualSense device.
    public sealed class VehicleLightReview:MonoBehaviour
    {
        [Serializable] public class Sample {public string phase;public float speed,command,expectedCommand;public bool brake,reverse,expectedBrake,expectedReverse,passed;}
        [Serializable] public class Evidence {public string scope="Synthetic trigger and actual AI-controller lamp integration; not physical-controller acceptance";public List<Sample> samples=new();public bool passed;}
        RoadCar car;AdventureUI ui;string folder;readonly Evidence evidence=new();
        IEnumerator Start()
        {
            Application.logMessageReceived+=QuitOnException;
            folder=RaceTrial.Arg("--trial-captures");
            if(string.IsNullOrEmpty(folder)||string.IsNullOrEmpty(RaceTrial.Arg("--adventure-save")))throw new Exception("Vehicle light review requires isolated save and capture paths");
            Directory.CreateDirectory(folder);var world=GetComponent<RaceTrial>();car=world.cars[0];ui=GetComponent<AdventureUI>();ui.SetVisible(false);
            car.controlsBlocked=true;car.body.position=world.track.At(30)+Vector3.up*.1f;car.body.rotation=Quaternion.LookRotation(world.track.Forward(30));
            car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;
            yield return new WaitForSeconds(.2f);car.controlsBlocked=false;
            var pad=InputSystem.AddDevice<Gamepad>("VehicleLightIntegration");
            try {
                InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1});
                float deadline=Time.time+6;while(car.speed<7&&Time.time<deadline)yield return new WaitForFixedUpdate();
                if(car.speed<7)throw new Exception("Forward acceleration did not reach light-test speed");
                yield return CommandApplied(1);Capture("01-forward",false,false,1);
                InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});
                yield return CommandApplied(-1);Capture("02-forward-braking",true,false,-1);
                deadline=Time.time+6;while(car.speed>-.8f&&Time.time<deadline)yield return new WaitForFixedUpdate();
                if(car.speed>-.8f)throw new Exception("Reverse input did not move the car backwards");
                yield return null;Capture("03-reversing",false,true,-1);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return CommandApplied(0);Capture("04-reverse-coast",false,true,0);
                InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1});yield return CommandApplied(1);Capture("05-braking-in-reverse",true,true,1);
                InputSystem.QueueStateEvent(pad,new GamepadState());car.controlsBlocked=true;
                yield return CommandApplied(0);Capture("06-blocked-idle",false,false,0);
                car.controlsBlocked=false;car.autonomous=true;car.body.linearVelocity=car.transform.forward*35;
                yield return CommandApplied(-1);Capture("07-ai-braking",true,false,-1);
                car.controlsBlocked=true;car.autonomous=false;
            } finally {InputSystem.RemoveDevice(pad);}
            evidence.passed=evidence.samples.TrueForAll(s=>s.passed);
            File.WriteAllText(Path.Combine(folder,"lights.json"),JsonUtility.ToJson(evidence,true));
            Debug.Log("LITTLE_ROAD_LIGHT_REVIEW passed="+evidence.passed);Application.Quit(evidence.passed?0:2);
        }
        IEnumerator CommandApplied(float expected)
        {
            // Capturing a PNG can stall one frame. A short timer alone can
            // expire before the next dynamic-input and LateUpdate phases run.
            // Wait for the controller command, then a complete presentation
            // frame; do not wait for the expected lamp result itself.
            float deadline=Time.realtimeSinceStartup+3;
            do {yield return null;} while(Mathf.Abs(car.throttle-expected)>.01f&&Time.realtimeSinceStartup<deadline);
            yield return null;
        }
        void Capture(string phase,bool braking,bool reversing,float expectedCommand)
        {
            var lights=car.GetComponent<VehicleLights>();
            var sample=new Sample{phase=phase,speed=car.speed,command=car.throttle,expectedCommand=expectedCommand,brake=lights.Braking,reverse=lights.Reversing,expectedBrake=braking,expectedReverse=reversing};
            sample.passed=sample.brake==braking&&sample.reverse==reversing&&Mathf.Abs(sample.command-expectedCommand)<.01f;evidence.samples.Add(sample);
            var camera=Camera.main;camera.transform.position=car.transform.position-car.transform.forward*4.6f+car.transform.right*1.7f+Vector3.up*1.8f;
            camera.transform.LookAt(car.transform.position+Vector3.up*.8f);ui.Capture(Path.Combine(folder,phase+".png"));
        }
        void QuitOnException(string message,string stack,LogType type){if(type==LogType.Exception)Application.Quit(2);}
        void OnDestroy(){Application.logMessageReceived-=QuitOnException;}
    }
}

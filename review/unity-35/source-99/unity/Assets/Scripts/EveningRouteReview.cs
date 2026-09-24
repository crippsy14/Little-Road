using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LittleRoad
{
    // Opt-in investigation harness. It drives physical cars with the existing
    // AI; captures are evidence of that route, not a manual input acceptance.
    public sealed class EveningRouteReview:MonoBehaviour
    {
        [Serializable] public class LapEvidence {public string scope;public float distance,seconds,maxOffset;public int contacts,recoveries;public bool completed;}
        [Serializable] public class Result {public string scope;public bool playerFinished,resultMenuOpen;public int laps,nextGate,creditsBefore,creditsAfter,settledSerial,raceSerial;public float progress,nextGateProgress;}
        static bool freeLapDone;static int creditsBefore;
        RaceTrial world;RoadCar car;AdventureUI ui;string folder;
        IEnumerator Start()
        {
            Application.logMessageReceived+=QuitOnException;
            folder=RaceTrial.Arg("--trial-captures");
            if(string.IsNullOrEmpty(folder)||string.IsNullOrEmpty(RaceTrial.Arg("--adventure-save")))throw new Exception("Route review requires isolated save and captures paths");
            Directory.CreateDirectory(folder);world=GetComponent<RaceTrial>();
            while(!world.running||world.cars.Count==0)yield return null;
            car=world.cars[0];ui=GetComponent<AdventureUI>();
            yield return new WaitForSeconds(.5f);
            if(world.freeRoam&&RaceTrial.Has("--review-race-only")){freeLapDone=true;creditsBefore=AdventureSession.Store.Data.credits;AdventureSession.StartRace();yield break;}
            if(world.freeRoam&&!freeLapDone)yield return FreeLap();
            else if(!world.freeRoam)yield return RaceLap();
        }
        void QuitOnException(string message,string stack,LogType type){if(type==LogType.Exception)Application.Quit(2);}
        void OnDestroy(){Application.logMessageReceived-=QuitOnException;}
        IEnumerator FreeLap()
        {
            car.controlsBlocked=true;
            car.body.position=world.track.At(4)+Vector3.up*.15f;car.body.rotation=Quaternion.LookRotation(world.track.Forward(4));
            car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;
            yield return new WaitForFixedUpdate();car.controlsBlocked=false;
            // Finished-car planning gives a gentle 18m/s visual tour and avoids
            // race-only missed-gate recovery while free driving has no gates.
            car.autonomous=true;car.finished=true;
            float start=car.progress,clock=Time.time,next=0;
            var result=new LapEvidence{scope="Continuous physical free-roam AI lap, capped at 18m/s by finished-car planning; one initial on-road placement, no route teleports"};
            while(car.progress-start<world.track.length&&Time.time-clock<100) {
                result.maxOffset=Mathf.Max(result.maxOffset,Mathf.Abs(car.offset));
                if(car.progress-start>=next){ui.Capture(Path.Combine(folder,"free-"+Mathf.RoundToInt(next).ToString("000")+".png"));next+=55;}
                yield return null;
            }
            result.distance=car.progress-start;result.seconds=Time.time-clock;result.completed=result.distance>=world.track.length;result.contacts=car.contacts;result.recoveries=car.recoveries;
            File.WriteAllText(Path.Combine(folder,"free-lap.json"),JsonUtility.ToJson(result,true));
            car.controlsBlocked=true;car.finished=false;freeLapDone=true;creditsBefore=AdventureSession.Store.Data.credits;
            Debug.Log("EVENING_FREE_LAP completed="+result.completed+" distance="+result.distance+" contacts="+result.contacts);
            GetComponent<VerticalMotionAudit>()?.Save();
            AdventureSession.StartRace();
        }
        IEnumerator RaceLap()
        {
            if(RaceTrial.Has("--checkpoint-scenarios"))yield return MissAndRecover();
            car.autonomous=true;float next=0,clock=Time.time;
            while(!car.finished&&Time.time-clock<240) {
                if(car.progress>=next&&next<world.track.length){ui.Capture(Path.Combine(folder,"race-"+Mathf.RoundToInt(next).ToString("000")+".png"));next+=55;}
                yield return null;
            }
            yield return new WaitForSeconds(.5f);ui.Capture(Path.Combine(folder,"race-result.png"));
            var disk=new AdventureStore(AdventureSession.Store.path);
            var result=new Result{scope="Three-lap connected career race driven by existing AI; checks real result menu and saved payout, not manual checkpoint robustness",playerFinished=car.finished,resultMenuOpen=ui.IsOpen,
                laps=car.laps,nextGate=car.nextGate,progress=car.progress,nextGateProgress=car.nextGateProgress,creditsBefore=creditsBefore,creditsAfter=disk.Data.credits,settledSerial=disk.Data.settledSerial,raceSerial=disk.Data.raceSerial};
            File.WriteAllText(Path.Combine(folder,"race-result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("EVENING_RACE_RESULT finished="+result.playerFinished+" menu="+result.resultMenuOpen+" savedCredits="+result.creditsAfter);
            Application.Quit(result.playerFinished&&result.resultMenuOpen&&result.creditsAfter>result.creditsBefore?0:2);
        }
        IEnumerator MissAndRecover()
        {
            // Controlled initial condition, followed by actual Rigidbody
            // movement and a synthetic Triangle press through race input.
            // This is a regression fixture, not a manual playthrough.
            car.controlsBlocked=true;car.autonomous=false;
            while(world.elapsed<5)yield return null;
            var pad=InputSystem.AddDevice<Gamepad>("CheckpointRecoveryTest");
            try {
                float d=-3,side=-11;
                car.body.position=world.track.At(d)+world.track.Right(d)*side+Vector3.up*.2f;
                car.body.rotation=Quaternion.LookRotation(world.track.Forward(d));
                car.body.linearVelocity=world.track.Forward(d)*8;car.body.angularVelocity=Vector3.zero;
                car.pathDistance=Mathf.Repeat(d,world.track.length);car.progress=d;car.offset=side;
                typeof(RoadCar).GetField("previousPath",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(car,car.pathDistance);
                car.controlsBlocked=false;InputSystem.QueueStateEvent(pad,new GamepadState());
                float deadline=Time.time+3;
                while(!car.CheckpointMissed&&Time.time<deadline)yield return null;
                if(!car.CheckpointMissed||car.nextGate!=0||car.laps!=0)throw new Exception("Off-road gate crossing did not stay invalid");
                car.controlsBlocked=true;yield return null;
                ui.Capture(Path.Combine(folder,"checkpoint-missed.png"));
                int recovery=car.recoveries;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.North));yield return new WaitForSeconds(.1f);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                deadline=Time.time+10;
                while(car.recoveries==recovery&&Time.time<deadline)yield return null;
                if(car.recoveries!=recovery+1||car.progress>=car.nextGateProgress||car.nextGate!=0||car.laps!=0)
                    throw new Exception($"Triangle recovery failed: count={car.recoveries} before={recovery} progress={car.progress} gate={car.nextGate} laps={car.laps} queued={world.PlayerRecoveryPending}");
                ui.Capture(Path.Combine(folder,"checkpoint-recovered.png"));
                File.WriteAllText(Path.Combine(folder,"checkpoint-recovery.json"),"{\"scope\":\"Controlled off-road placement; actual physical crossing; synthetic Triangle recovery; not manual play\",\"missedGateRejected\":true,\"recoveryInputPassed\":true,\"unearnedLaps\":0}");
                Debug.Log("CHECKPOINT_RECOVERY_INPUT_PASSED");
            } finally {InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.RemoveDevice(pad);car.controlsBlocked=false;}
        }
    }
}

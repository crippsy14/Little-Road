using System.IO;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Opt-in (--drive-trace <file.csv>) free-roam recorder for manual driving: one row per
    // physics step with inputs, grip, motion, the strongest contact and the render frame time.
    public sealed class DriveTrace:MonoBehaviour
    {
        RoadCar car;StreamWriter writer;float frameTime,worstFrame;int step;
        public void Setup(RoadCar player,string path)
        {
            car=player;Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            writer=new StreamWriter(path,false,new UTF8Encoding(false));
            writer.WriteLine("step,time,speed,velocity,yawRate,throttle,steering,steerState,grip,offset,x,y,z,contactDeltaV,contactSurface,frameMs,worstFrameMs");
        }
        void Update(){frameTime=Time.unscaledDeltaTime*1000;worstFrame=Mathf.Max(worstFrame,frameTime);}
        void FixedUpdate()
        {
            if(writer==null||car==null)return;
            var p=car.body.position;var v=car.body.linearVelocity;
            writer.WriteLine($"{step++},{Time.time:0.000},{car.speed:0.000},{v.magnitude:0.000},{car.body.angularVelocity.y:0.000},{car.throttle:0.00},{car.steering:0.00},{car.SteeringState:0.000},{car.Grip:0.0},{car.offset:0.00},{p.x:0.00},{p.y:0.000},{p.z:0.00},{car.observedContactDeltaV:0.000},{(car.observedContactSurface??"").Replace(',',' ')},{frameTime:0.0},{worstFrame:0.0}");
            car.observedContactDeltaV=0;car.observedContactSurface=null;worstFrame=0;
            if(step%600==0)writer.Flush();
        }
        void OnDestroy(){writer?.Dispose();writer=null;}
    }
}
namespace LittleRoad
{
    // Opt-in (--drive-replay <drive.csv>) feeds a recorded free-roam trace's pedal and steering
    // back into the player car, one row per physics step from the same spawn, then quits.
    public sealed class DriveReplay:MonoBehaviour
    {
        readonly System.Collections.Generic.List<UnityEngine.Vector2> commands=new();int next;
        public void Setup(RoadCar player,string path)
        {
            var lines=System.IO.File.ReadAllLines(path);var head=lines[0].Split(',');int t=System.Array.IndexOf(head,"throttle"),s=System.Array.IndexOf(head,"steering");
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            for(int i=1;i<lines.Length;i++){var c=lines[i].Split(',');commands.Add(new UnityEngine.Vector2(float.Parse(c[t],culture),float.Parse(c[s],culture)));}
            int shift=int.Parse(RaceTrial.Arg("--drive-replay-shift","0"),culture);next=UnityEngine.Mathf.Max(0,shift);
            player.commandSource=_=>next<commands.Count?commands[next++]:UnityEngine.Vector2.zero;
        }
        void Update(){if(next>=commands.Count){UnityEngine.Debug.Log("DRIVE_REPLAY_DONE steps="+commands.Count);UnityEngine.Application.Quit();enabled=false;}}
    }
}

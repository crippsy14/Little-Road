using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Water a car can drive into (Richard's snaps, 24 Oct: "Water is just a floating asset ... it doesn't slow you down or
    // anything. Water needs to behave like water", and at the Bramblewick ford: "water should look to deform, splash, and slow
    // the car down, and affect grip"). The depth of water over a point: the Silver river along its line, the Raceway lake,
    // Glas and the source tarn, and the fords that register themselves. A car in it (RoadCar.Wade) loses grip and is dragged
    // back with its depth and speed, throws spray off its wheels and leaves a wake on the surface; in water deeper than its
    // body it floods, and is put back on the nearest road. In free roam; a race only with --race-water (it changes lap times
    // where a circuit crosses a ford, so it waits for its keep/reject runs).
    public static class Water
    {
        public const float Flooding=1.2f;   // (deeper than this over the car's floor: the car is under water)
        public static bool InRaces=>RaceTrial.Has("--race-water");
        // A ford: a stretch of road under a few centimetres of water (its own spray: FordSplash).
        struct Patch{public Vector3 centre,along;public float halfLength,halfWidth,depth;}
        static readonly List<Patch> patches=new();
        public static void AddFord(Vector3 centre,Vector3 along,float halfLength,float halfWidth,float depth)
        {
            along.y=0;along.Normalize();patches.RemoveAll(p=>(p.centre-centre).sqrMagnitude<1);
            patches.Add(new Patch{centre=centre,along=along,halfLength=halfLength,halfWidth=halfWidth,depth=depth});
        }
        static Bounds riverBox;static Vector3[] riverLine;
        // Depth of water over `p` (0 when dry); `ford`: it's a ford, which sprays for itself.
        public static float Depth(Vector3 p,out bool ford)
        {
            bool isFord=false;float best=0;
            foreach(var f in patches) {
                var d=p-f.centre;float u=Vector3.Dot(d,f.along),w=Vector3.Dot(d,Vector3.Cross(Vector3.up,f.along));
                if(Mathf.Abs(u)<f.halfLength&&Mathf.Abs(w)<f.halfWidth&&Mathf.Abs(d.y)<1.5f&&f.depth>best){best=f.depth;isFord=true;}
            }
            var river=HillcrestRoute.River;
            if(river!=null) {
                if(riverLine!=river.line){riverLine=river.line;riverBox=new Bounds(riverLine[0],Vector3.zero);foreach(var q in riverLine)riverBox.Encapsulate(q);riverBox.Expand(new Vector3(river.width,40,river.width));}
                if(riverBox.Contains(p)&&HillcrestRoute.Distance(riverLine,p.x,p.z,out float y)<river.width*.5f)Deeper(y-.05f-p.y);
            }
            Disc(HillcrestRoute.Lake,HillcrestRoute.LakeRadius,HillcrestRoute.LakeY);
            if(QuarryHills.Enabled) {
                Disc(QuarryHills.Source,QuarryHills.SourceRadius,QuarryHills.SourceY);
                float dx=(p.x-QuarryHills.Pit.x)/QuarryHills.PitRX,dz=(p.z-QuarryHills.Pit.y)/QuarryHills.PitRZ;if(dx*dx+dz*dz<1)Deeper(QuarryHills.LakeY-p.y);
            }
            ford=isFord;return best;
            void Disc(Vector2 c,float r,float y){if((new Vector2(p.x,p.z)-c).sqrMagnitude<r*r)Deeper(y-p.y);}
            void Deeper(float d){if(d>best){best=d;isFord=false;}}
        }
        // How much the water holds a car back, 0-1: any water at all a little (a ford's few centimetres), half a metre fully.
        public static float Wet(float depth)=>depth<=.02f?0:Mathf.Clamp(depth/.5f,.3f,1);

        // ---- spray and wake
        static WaterSpray spray;
        public static void Splash(RoadCar car,float depth,float surface)
        {
            if(spray==null){spray=new GameObject("Water spray").AddComponent<WaterSpray>();}
            spray.Throw(car,depth,surface);
        }
    }
    // Spray off the wheels and a flat wake spreading on the water behind and beside a car in the river or a lake (the ford has its own).
    public sealed class WaterSpray:PuffPool
    {
        readonly Dictionary<RoadCar,float> next=new();
        void Awake(){soft=true;material=SoftMaterial("Water spray",new Color(.86f,.93f,.96f));}
        void Update()=>Step(Time.deltaTime);
        public void Throw(RoadCar car,float depth,float surface)
        {
            float speed=Mathf.Abs(car.speed);if(speed<1.2f)return;
            next.TryGetValue(car,out float at);if(Time.time<at)return;next[car]=Time.time+.05f;
            var t=car.transform;var f=t.forward;var r=t.right;var v=car.body.linearVelocity;float k=Mathf.Min(1,speed/10);
            // (the bow wave and the wheels' spray: up and out, more with speed; deep water throws less up and more aside)
            foreach(int side in new[]{-1,1}) {
                var bow=t.position+f*1.9f+r*side*.7f;bow.y=surface+.05f;
                Emit(bow,r*side*Random.Range(1f,2.2f)*k+Vector3.up*Random.Range(.8f,2f)*k*Mathf.Lerp(1,.6f,Mathf.Clamp01(depth))+v*.25f,Random.Range(.35f,.6f),Random.Range(.5f,.8f),.75f,.3f);
                var rear=t.position-f*1.3f+r*side*.95f;rear.y=surface+.05f;
                Emit(rear,r*side*Random.Range(.4f,1f)*k+Vector3.up*Random.Range(.6f,1.6f)*k-v*.1f,Random.Range(.3f,.5f),Random.Range(.45f,.7f),.65f,.25f);
            }
            // (the wake: flat rings on the surface, left behind and spreading)
            var wake=t.position-f*2.2f;wake.y=surface+.02f;
            Emit(wake,-v*.15f+r*Random.Range(-.6f,.6f),Random.Range(1.4f,2.2f)*Mathf.Lerp(.7f,1.3f,k),Random.Range(1.2f,1.8f),.4f,.06f);
        }
    }
}

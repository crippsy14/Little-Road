using System;
using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;

namespace LittleRoad
{
    // Farmer Giles's challenges at Golden Acre Farm (Richard, 5 Oct: "farming challenges around the city"; doc 64).
    //   The Harvest: Giles bolts a 6 m harvester header to your bumper; flatten as much of the 130 x 86 m wheat as you can
    //     in 100 s. Stars at 30, 50 and 70% harvested.
    //   Apple Shake: 52 apple trees in rows on the hay meadow; knock as many down as you can in 75 s (each one topples and
    //     sheds its apples). Stars at 40, 65 and 90%.
    // Each star pays once (the speed traps' record: best percentage kept). The field regrows and the trees stand back up
    // for every attempt.
    public sealed class FarmChallenges : MonoBehaviour
    {
        public enum Kind{Harvest,Apples}
        public static readonly string[] Ids={"farm_harvest","farm_apples"};
        public static bool Known(string id)=>Array.IndexOf(Ids,id)>=0;
        public static readonly float[] HarvestMarks={.3f,.5f,.7f},AppleMarks={.4f,.65f,.9f};
        public static readonly int[] Prizes={100,150,250};
        public const float HarvestTime=100,AppleTime=75,Countdown=3;
        public static string Name(Kind k)=>k==Kind.Harvest?"The Harvest":"Apple Shake";
        public static float Limit(Kind k)=>k==Kind.Harvest?HarvestTime:AppleTime;
        public static float[] Marks(Kind k)=>k==Kind.Harvest?HarvestMarks:AppleMarks;

        public bool Active{get;private set;}public Kind Current{get;private set;}public float GoAt{get;private set;}
        public float Progress=>Current==Kind.Harvest?(Field!=null?Field.Flattened:0):Orchard.Fraction;
        public float LastResult;public int LastStars;
        RoadCar player;GameObject header;
        public event Action<Kind,float,int> Finished;
        public static WheatField Field=>WheatField.All.Find(f=>f.name=="Golden Acre wheat");

        // Where each one starts (farm-local), facing in.
        public static (Vector3 at,float yaw) Start(Kind k)
        {
            float yaw=HillcrestRoute.FarmYaw;
            Vector3 F(float x,float z){var p=HillcrestRoute.Farm(x,z);p.y=ValleyField.Height(p.x,p.z);return p;}
            return k==Kind.Harvest?(F(-20,41),yaw+180):(F(-42,57.5f),yaw+90);
        }
        public void Begin(Kind k,RoadCar car)
        {
            Cancel();player=car;Current=k;Active=true;GoAt=Time.time+Countdown;LastResult=0;LastStars=0;Orchard.Challenge=k==Kind.Apples;   // (trees stay down during the challenge)
            if(k==Kind.Harvest){Field?.Regrow();FitHeader(true);}else Orchard.ResetAll();
        }
        public void Cancel(){if(!Active)return;Active=false;Orchard.Challenge=false;FitHeader(false);}
        void Update()
        {
            if(!Active)return;
            if(Current==Kind.Apples&&Orchard.Fraction>=1-1e-4f){Finish();return;}
            if(Time.time>=GoAt+Limit(Current))Finish();
        }
        void Finish()
        {
            Active=false;Orchard.Challenge=false;FitHeader(false);LastResult=Progress;int stars=0;var marks=Marks(Current);for(int i=0;i<marks.Length;i++)if(LastResult>=marks[i])stars=i+1;LastStars=stars;
            Debug.Log($"LITTLE_ROAD_FARM_CHALLENGE kind={Current} result={LastResult*100:0.0}% stars={stars}");
            Finished?.Invoke(Current,LastResult,stars);
        }

        // The header: a red cutter bar and a turning yellow reel across the bumper, 6 m wide (visual; the field
        // reads WheatField.Header for its width).
        void FitHeader(bool on)
        {
            WheatField.Header=on?2.4f:0;
            if(header!=null){Destroy(header);header=null;}if(!on||player==null)return;
            header=new GameObject("Harvester header");header.transform.SetParent(player.transform,false);header.transform.localPosition=new Vector3(0,.55f,2.6f);
            var red=TrialWorld.Material("Header red",Toy.Hex(0xd94040),.2f,.4f);var yellow=TrialWorld.Material("Header reel",Toy.Hex(0xffd23f),.1f,.4f);var steel=TrialWorld.Material("Header steel",new Color(.7f,.72f,.75f),.6f,.5f);
            void Part(PrimitiveType t,string name,Vector3 at,Vector3 size,Material m,Vector3 euler=default){var g=GameObject.CreatePrimitive(t);g.name=name;Destroy(g.GetComponent<Collider>());g.transform.SetParent(header.transform,false);g.transform.localPosition=at;g.transform.localScale=size;g.transform.localRotation=Quaternion.Euler(euler);g.GetComponent<Renderer>().sharedMaterial=m;}
            Part(PrimitiveType.Cube,"Cutter bar",new Vector3(0,-.25f,.2f),new Vector3(6.2f,.18f,.9f),red);
            Part(PrimitiveType.Cube,"Header back",new Vector3(0,.15f,-.25f),new Vector3(6.2f,.8f,.12f),red);
            foreach(int s in new[]{-1,1})Part(PrimitiveType.Cube,"Header side",new Vector3(s*3.1f,0,0),new Vector3(.12f,.7f,1),red);
            for(int i=0;i<13;i++)Part(PrimitiveType.Cube,"Tine",new Vector3(-3+i*.5f,-.27f,.75f),new Vector3(.08f,.06f,.35f),steel);
            var reel=new GameObject("Reel");reel.transform.SetParent(header.transform,false);reel.transform.localPosition=new Vector3(0,.45f,.25f);reel.AddComponent<Spin>();
            void ReelPart(Vector3 at,Vector3 size,Vector3 euler){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Reel bat";Destroy(g.GetComponent<Collider>());g.transform.SetParent(reel.transform,false);g.transform.localPosition=at;g.transform.localScale=size;g.transform.localRotation=Quaternion.Euler(euler);g.GetComponent<Renderer>().sharedMaterial=yellow;}
            for(int k=0;k<5;k++){float a=k*72*Mathf.Deg2Rad;ReelPart(new Vector3(0,Mathf.Sin(a)*.38f,Mathf.Cos(a)*.38f),new Vector3(5.9f,.07f,.07f),Vector3.zero);}
            foreach(int s in new[]{-1,1})ReelPart(new Vector3(s*2.9f,0,0),new Vector3(.06f,.8f,.12f),Vector3.zero);
        }
        sealed class Spin:MonoBehaviour{void Update()=>transform.Rotate(Time.deltaTime*240,0,0,Space.Self);}
    }

    // An apple tree that topples when a car hits it (Apple Shake): it falls away from the car over 0.7 s and its apples
    // tumble off; it stands back up for the next attempt (or by itself after 40 s in free roam).
    public sealed class AppleTree : MonoBehaviour
    {
        public bool Down{get;private set;}
        Quaternion upright;Vector3 axis;float fellAt=-1,riseAt=-1;readonly List<GameObject> apples=new();
        public void Setup(List<GameObject> fruit){upright=transform.rotation;apples.AddRange(fruit);Orchard.All.Add(this);}
        void OnDestroy()=>Orchard.All.Remove(this);
        void OnTriggerEnter(Collider other)
        {
            if(Down)return;var car=other.GetComponentInParent<RoadCar>();if(car==null||car!=WheatField.Player||Mathf.Abs(car.speed)<2.5f)return;
            var push=car.body.linearVelocity;push.y=0;if(push.sqrMagnitude<.01f)push=car.transform.forward;
            axis=Vector3.Cross(Vector3.up,push.normalized);Down=true;fellAt=Time.time;riseAt=-1;
            car.body.linearVelocity*=.93f;
            // The apples come off: a little burst of red that drops and rolls.
            foreach(var a in apples)if(a!=null){a.SetActive(false);
                var drop=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(drop.GetComponent<Collider>());drop.transform.position=a.transform.position;drop.transform.localScale=a.transform.lossyScale;
                drop.GetComponent<Renderer>().sharedMaterial=a.GetComponent<Renderer>().sharedMaterial;var f=drop.AddComponent<FlungRock>();f.velocity=push.normalized*Random.Range(1.5f,4)+Random.insideUnitSphere*2+Vector3.up*Random.Range(1,3.5f);f.spin=Random.insideUnitSphere*300;}
            AudioDirector.Beep(220,.05f);
        }
        public void Stand(bool instantly)
        {
            if(!Down)return;Down=false;fellAt=-1;
            if(instantly){transform.rotation=upright;foreach(var a in apples)if(a!=null)a.SetActive(true);}else riseAt=Time.time;
        }
        void Update()
        {
            if(Down) {
                float k=Mathf.Clamp01((Time.time-fellAt)/.7f);k=k*k;transform.rotation=Quaternion.AngleAxis(84*k,axis)*upright;
                if(Time.time-fellAt>40&&!Orchard.Challenge)Stand(false);
            } else if(riseAt>0) {
                float k=Mathf.Clamp01((Time.time-riseAt)/1.4f);transform.rotation=Quaternion.Slerp(transform.rotation,upright,k);
                if(k>=1){riseAt=-1;transform.rotation=upright;foreach(var a in apples)if(a!=null)a.SetActive(true);}
            }
        }
    }
    public static class Orchard
    {
        public static readonly List<AppleTree> All=new();public static bool Challenge;
        public static float Fraction{get{if(All.Count==0)return 0;int n=0;foreach(var t in All)if(t.Down)n++;return n/(float)All.Count;}}
        public static void ResetAll(){foreach(var t in All)t.Stand(true);}
        // Rows of apple trees on the farm's hay meadow (farm-local coordinates through `at`).
        public static int Build(Transform parent,Func<float,float,Vector3> at,float yaw)
        {
            if(!BayviewKit.Has("tree_round"))return 0;var red=TrialWorld.Material("Apple",Toy.Hex(0xd62f2f),0,.55f);int planted=0;
            var holder=new GameObject("Golden Acre orchard").transform;holder.SetParent(parent);
            for(int r=0;r<4;r++)for(int c=0;c<13;c++) {
                float x=-30+c*9+(r%2)*4.5f,z=44+r*9;var p=at(x,z);
                // (Under the animated root: static kit pieces are batched and couldn't topple.)
                var tree=BayviewKit.Spawn("tree_round",p+Vector3.down*.05f,yaw+(c*53+r*31)%360,BayviewKit.AnimatedRoot,null,null,false,.5f+((c+r)%3)*.04f);if(tree==null)continue;tree.transform.SetParent(holder,true);tree.name="Apple tree";
                var b=new Bounds(p,Vector3.zero);foreach(var rend in tree.GetComponentsInChildren<Renderer>())b.Encapsulate(rend.bounds);
                var fruit=new List<GameObject>();
                for(int k=0;k<7;k++){float a=k*2.4f+r+c;var dir=new Vector3(Mathf.Cos(a),.2f+.5f*Mathf.Abs(Mathf.Sin(a*1.7f)),Mathf.Sin(a)).normalized;
                    var apple=GameObject.CreatePrimitive(PrimitiveType.Sphere);apple.name="Apple";UnityEngine.Object.Destroy(apple.GetComponent<Collider>());apple.transform.SetParent(tree.transform,true);
                    apple.transform.position=b.center+Vector3.Scale(dir,b.extents*.92f);apple.transform.localScale=Vector3.one*(.26f/Mathf.Max(.01f,tree.transform.lossyScale.x));apple.GetComponent<Renderer>().sharedMaterial=red;fruit.Add(apple);}
                var trigger=tree.AddComponent<CapsuleCollider>();trigger.isTrigger=true;trigger.radius=1.1f/Mathf.Max(.01f,tree.transform.lossyScale.x);trigger.height=3.2f/Mathf.Max(.01f,tree.transform.lossyScale.x);trigger.center=Vector3.up*trigger.height*.5f;
                tree.AddComponent<AppleTree>().Setup(fruit);planted++;
            }
            return planted;
        }
    }
}

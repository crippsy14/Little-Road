using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The working quarry (Richard, 4 Oct: "moving big vehicles around the quarry which are working, and then watch the race
    // when you're racing"). In the north-east corner of the floor, clear of the rallycross loop and the rally school:
    //   - an excavator digging at a slate spoil heap and slewing round to load;
    //   - two articulated dump trucks on a circuit: loaded under the excavator, out to the tip, reverse up to it and tip;
    //   - a wheel loader shuttling between a stockpile and the slate stacks.
    // In a quarry race (RoadCar.LooseRace) they stop work: the trucks and the loader park side by side behind the start
    // straight facing the track, the excavator turns to watch with its arm raised. Plus, round the floor's edge, standing
    // slate rocks and boulders (kit rock_stand_*), rubble heaps, and a stone circle on the moor.
    public sealed class QuarryWorks:MonoBehaviour
    {
        public static QuarryWorks Instance;public static int Rocks,Heaps;
        // ---- the layout (world x, z)
        static readonly Vector2 HeapA=new(-2158,-2822),HeapB=new(-2245,-2818),HeapC=new(-2236,-2893);
        static readonly Vector2 Digger=new(-2175,-2846),DigAt=new(-2163,-2834),LoadBay=new(-2178,-2855);
        static readonly Vector2 Stacks=new(-2198,-2899);
        // The race-day line-up behind the start straight, facing the track (south).
        static readonly Vector2[] Spectators={new(-2218,-2907),new(-2200,-2907),new(-2181,-2909)};

        // Every point the machines stand on or drive through (QuarryHillsChecks keeps them off the circuit and on the floor).
        public static IEnumerable<Vector2> LayoutPoints()
        {
            foreach(var p in new[]{HeapA,HeapB,HeapC,Digger,DigAt,LoadBay,Stacks,new(-2205,-2866),new(-2200,-2828),new(-2190,-2829),new(-2148,-2842),new(-2156,-2860),new(-2166,-2857),new(-2218,-2888),new(-2214,-2888)})yield return p;
            foreach(var p in Spectators)yield return p;
        }
        static Vector3 G(Vector2 p)=>new(p.x,ValleyField.Height(p.x,p.y),p.y);
        static float Yaw(Vector2 d)=>Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;

        // ---------------------------------------------------------------- building
        public static void Build(Transform root)
        {
            Rocks=Heaps=0;if(!QuarryHills.Enabled)return;
            var slate=CoastalLandscape.SlateMaterial;
            if(slate!=null){Heap(root,HeapA,13,6.5f,slate,11);Heap(root,HeapB,9,4.2f,slate,12);Heap(root,HeapC,7,3.2f,slate,13);}
            if(BayviewKit.Has("slate_stack"))for(int k=0;k<4;k++)BayviewKit.Spawn("slate_stack",G(Stacks+new Vector2(k*3.6f,(k%2)*.6f))+Vector3.down*.03f,8*(k-1.5f),root);
            if(BayviewKit.Has("rock_stand_a"))StandingRocks(root);
            if(!BayviewKit.Has("haul_truck")||!BayviewKit.Has("excavator_base"))return;
            var go=new GameObject("Quarry works");go.transform.SetParent(BayviewKit.AnimatedRoot);Instance=go.AddComponent<QuarryWorks>();Instance.Setup();
        }
        // A spoil heap: a rough cone of slate rubble on the floor, flatter on top, with a collider (you can drive up it).
        static void Heap(Transform root,Vector2 c,float radius,float height,Material material,int seed)
        {
            const int rings=10,around=40;var v=new List<Vector3>();var t=new List<int>();
            for(int k=0;k<=rings;k++)for(int j=0;j<around;j++) {
                float a=j*Mathf.PI*2/around,r=k/(float)rings;float wob=1+(Mathf.PerlinNoise(j*.35f+seed,seed*1.7f)-.5f)*.35f;
                float x=c.x+Mathf.Cos(a)*radius*r*wob,z=c.y+Mathf.Sin(a)*radius*r*wob;float g=ValleyField.Height(x,z);
                float h=height*Mathf.Pow(Mathf.Clamp01(1-r*r),.75f)*(1+(Mathf.PerlinNoise(x*.3f+seed,z*.3f)-.5f)*.25f);
                v.Add(new Vector3(x,g+h-(k==rings?.15f:0),z));
            }
            for(int k=0;k<rings;k++)for(int j=0;j<around;j++){int a=k*around+j,b=k*around+(j+1)%around;t.AddRange(new[]{a,b,a+around,b,b+around,a+around});}
            var mesh=new Mesh{name="Spoil heap"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            // (Facing up: flip if the winding came out downward.)
            if(mesh.normals[around*3].y<0){for(int i=0;i<t.Count;i+=3)(t[i+1],t[i+2])=(t[i+2],t[i+1]);mesh.SetTriangles(t,0);mesh.RecalculateNormals();}
            mesh.RecalculateBounds();
            var go=new GameObject("Spoil heap",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshCollider>().sharedMesh=mesh;Heaps++;
        }
        // Standing rocks and boulders round the floor's edge (not on the circuit, the haul road, the Blondin, the school or
        // the works), and a ring of nine standing stones on open moor north-west of town.
        static void StandingRocks(Transform root)
        {
            var rng=new System.Random(4410);float R()=>(float)rng.NextDouble();string[] ids={"rock_stand_a","rock_stand_b","rock_stand_c","rock_stand_d","rock_stand_d"};
            var c=QuarryHills.FloorCentre;
            for(float a=0;a<360;a+=6.5f) {
                float f=1.02f+R()*.1f,ang=(a+R()*3)*Mathf.Deg2Rad;var p=new Vector2(c.x+Mathf.Cos(ang)*QuarryHills.FloorRX*f,c.y+Mathf.Sin(ang)*QuarryHills.FloorRZ*f);
                string id=ids[rng.Next(ids.Length)];float scale=.8f+R()*.8f,yaw=R()*360;
                if(HillcrestRoute.Distance(QuarryFloor.Loop,p.x,p.y,out _)<QuarryFloor.LoopWidth*.5f+12||ValleyField.RoadDistance(p.x,p.y)<10)continue;
                if(Vector2.Distance(p,QuarryFloor.BlondinFloor)<30||Vector2.Distance(p,new Vector2(QuarryFloor.School.x,QuarryFloor.School.z))<40||InWorks(p))continue;
                var g=G(p);if(ValleyField.Normal(p.x,p.y).y<.75f)continue;
                BayviewKit.Spawn(id,g+Vector3.down*.15f,yaw,root,null,null,true,scale);Rocks++;
                // Rubble at its foot now and then.
                if(R()<.5f){var q=p+new Vector2(R()*8-4,R()*8-4);if(!InWorks(q))BayviewKit.Spawn("rock_stand_d",G(q)+Vector3.down*.1f,R()*360,root,null,null,false,.35f+R()*.3f);}
            }
            // Meini hirion: a stone circle on the moor (open, level ground well away from roads and buildings).
            var kettle=HillcrestRoute.Find(QuarryHills.KettleRoadName);if(kettle==null)return;
            for(float s=kettle.Length*.35f;s<kettle.Length*.9f;s+=25)foreach(int side in new[]{-1,1}) {
                var centre=kettle.At(s)+kettle.Right(s)*side*70;var c2=new Vector2(centre.x,centre.z);bool ok=true;
                for(int k=0;k<12&&ok;k++){float a=k*30*Mathf.Deg2Rad;var q=c2+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*13;
                    ok=ValleyField.RoadDistance(q.x,q.y)>30&&ValleyField.Normal(q.x,q.y).y>.95f&&Mathf.Abs(ValleyField.Height(q.x,q.y)-ValleyField.Height(c2.x,c2.y))<1.6f&&!QuarryHills.Bare(q.x,q.y)
                        &&!BayviewKit.Occupied(new Bounds(new Vector3(q.x,ValleyField.Height(q.x,q.y)+1,q.y),new Vector3(5,4,5)))&&!WorldTiles.Blocked(new Vector3(q.x,0,q.y),3);}
                if(!ok)continue;
                for(int k=0;k<9;k++){float a=(k*40+R()*8)*Mathf.Deg2Rad;var q=c2+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*11;
                    BayviewKit.Spawn(k%3==1?"rock_stand_b":"rock_stand_a",G(q)+Vector3.down*.2f,Yaw(c2-q)+R()*20-10,root,null,null,true,.75f+R()*.35f);Rocks++;}
                StoneCircle=new Vector3(c2.x,ValleyField.Height(c2.x,c2.y),c2.y);return;
            }
        }
        public static Vector3? StoneCircle;
        static bool InWorks(Vector2 p)=>p.x>-2268&&p.x<-2130&&p.y>-2915&&p.y<-2795;

        // ---------------------------------------------------------------- the machines
        // A kit piece's moving part (its "Spin_X" child, pivoted at the hinge), driven to an angle instead of spinning.
        sealed class Hinge
        {
            readonly Transform part,kit;readonly Quaternion rest;
            public Hinge(GameObject go){kit=go.transform;foreach(Transform child in go.GetComponentsInChildren<Transform>())if(child.name.StartsWith("Spin_")){part=child;var spin=child.GetComponent<KitSpin>();if(spin!=null)spin.enabled=false;}
                if(part!=null)rest=Quaternion.Inverse(kit.rotation)*part.rotation;}
            public void Set(float degrees){if(part!=null)part.rotation=Quaternion.AngleAxis(degrees,kit.TransformDirection(Vector3.right))*kit.rotation*rest;}
        }
        sealed class Leg{public Vector2 to;public bool reverse;public int action;} // action: 0 none, 1 load, 2 tip, 3 scoop, 4 dump
        sealed class Machine
        {
            public GameObject go;public Hinge hinge;public List<Leg> legs;public int leg;public float yaw,speed,wait,angle;public Vector2 at;public string kind;public bool loaded;
        }
        readonly List<Machine> trucks=new();Machine loader;GameObject digBase,digTop;Hinge digArm;float slew,arm=-10;bool parked;
        public int TruckTrips{get;private set;}public int Loads{get;private set;}
        public bool RaceDay=>parked;

        void Setup()
        {
            // Excavator: tracks facing the heap, the house on top slewing between the dig and the truck.
            float baseYaw=Yaw(DigAt-Digger);
            digBase=BayviewKit.Spawn("excavator_base",G(Digger)+Vector3.down*.02f,baseYaw,transform,null,null,true,1.1f);
            digTop=BayviewKit.Spawn("excavator_top",G(Digger)+Vector3.down*.02f,baseYaw,transform,null,null,true,1.1f);digArm=new Hinge(digTop);slew=baseYaw;
            // Trucks: the loop (load, out west, forward to the tip's approach, reverse up to the tip and tip, back round east).
            var tipDir=new Vector2(.96f,-.26f);var tip=HeapB+tipDir*11.5f;var tip0=tip+tipDir*14;
            var loop=new List<Leg>{new(){to=LoadBay,action=1},new(){to=new(-2205,-2866)},new(){to=tip0},new(){to=tip,reverse=true,action=2},new(){to=new(-2200,-2828)},
                new(){to=new(-2190,-2829)},new(){to=new(-2148,-2842)},new(){to=new(-2156,-2860)},new(){to=new(-2166,-2857)}};
            for(int i=0;i<2;i++){var m=new Machine{kind="truck",legs=loop,leg=i==0?0:4,speed=6.5f};m.at=i==0?LoadBay+new Vector2(14,-2):new Vector2(-2200,-2828);
                m.yaw=i==0?270:90;m.go=BayviewKit.Spawn("haul_truck",G(m.at),m.yaw,transform,null,null,true,1);m.hinge=new Hinge(m.go);Kinematic(m.go);trucks.Add(m);}
            // Wheel loader: into the stockpile, back out, across to the stacks, tip, back.
            var lw=new List<Leg>{new(){to=HeapC+new Vector2(8,2.5f),action=3},new(){to=new(-2218,-2888),reverse=true},new(){to=Stacks+new Vector2(4,7),action=4},new(){to=new(-2214,-2888),reverse=true}};
            loader=new Machine{kind="loader",legs=lw,leg=0,speed=4.2f,at=new(-2214,-2888),yaw=270};
            loader.go=BayviewKit.Spawn("wheel_loader",G(loader.at),loader.yaw,transform,null,null,true,1);loader.hinge=new Hinge(loader.go);Kinematic(loader.go);
            Kinematic(digTop);
            Debug.Log($"LITTLE_ROAD_QUARRY_WORKS trucks={trucks.Count} loader={(loader.go!=null)} excavator={(digTop!=null)} rocks={Rocks} heaps={Heaps} circle={(StoneCircle.HasValue?StoneCircle.Value.ToString("0"):"none")}");
        }
        static void Kinematic(GameObject go){if(go==null)return;var rb=go.GetComponent<Rigidbody>()??go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;}

        // Review stills: --works-raceday lines them up as on race day; --works-pose freezes each mid-job (the first truck
        // tipping at the tip, the excavator's arm down in the heap, the loader's bucket up at the stacks).
        bool posed;
        void Update()
        {
            if(RoadCar.LooseRace||RaceTrial.Has("--works-raceday")){if(!parked)Park();return;}
            if(RaceTrial.Has("--works-pose")) {
                if(posed)return;posed=true;var tipDir=new Vector2(.96f,-.26f);
                trucks[0].at=HeapB+tipDir*11.5f;trucks[0].yaw=Yaw(tipDir);Pose(trucks[0]);trucks[0].hinge.Set(-48);
                trucks[1].at=LoadBay;trucks[1].yaw=270;Pose(trucks[1]);
                loader.at=Stacks+new Vector2(4,7);loader.yaw=Yaw(-new Vector2(4,7));Pose(loader);loader.hinge.Set(-34);
                slew=Yaw(DigAt-Digger);Slew();digArm.Set(3);return;
            }
            float dt=Mathf.Min(Time.deltaTime,.05f);
            foreach(var m in trucks)Drive(m,dt);
            Drive(loader,dt);
            Excavate(dt);
        }
        // Race day: everyone lines up behind the start straight to watch.
        void Park()
        {
            parked=true;
            for(int i=0;i<trucks.Count;i++){trucks[i].at=Spectators[i];trucks[i].yaw=180;Pose(trucks[i]);trucks[i].hinge.Set(0);}
            if(loader!=null){loader.at=Spectators[2];loader.yaw=180;Pose(loader);loader.hinge.Set(-28);}
            slew=180;Slew();digArm.Set(-32);
        }
        void Drive(Machine m,float dt)
        {
            if(m==null||m.go==null)return;
            if(m.wait>0){m.wait-=dt;Act(m,dt);return;}
            var leg=m.legs[m.leg];
            // Only one truck at the load bay or the tip at a time.
            if(m.kind=="truck"&&(leg.action==1||leg.action==2))foreach(var o in trucks)if(o!=m&&(o.at-leg.to).sqrMagnitude<20*20&&m.legs[o.leg]==leg&&o.wait>0)return;
            var d=leg.to-m.at;float dist=d.magnitude;
            if(dist<.4f){m.at=leg.to;Arrive(m,leg);m.leg=(m.leg+1)%m.legs.Count;Pose(m);return;}
            // Steer: face the way of travel (or away from it, reversing), turning at most 55 degrees a second.
            float want=Yaw(leg.reverse?-d:d);m.yaw=Mathf.MoveTowardsAngle(m.yaw,want,55*dt);
            float off=Mathf.Abs(Mathf.DeltaAngle(m.yaw,want));float v=(leg.reverse?.45f:1)*m.speed*Mathf.Clamp01(dist/6+.25f)*(off>50?.15f:1);
            m.at+=d/dist*Mathf.Min(dist,v*dt);Pose(m);
            if(m.kind=="loader"){m.angle=Mathf.MoveTowards(m.angle,m.loaded?-20:-4,30*dt);m.hinge.Set(m.angle);}
        }
        void Arrive(Machine m,Leg leg)
        {
            switch(leg.action) {
                case 1:m.wait=13;m.angle=0;break;                          // under the excavator: three buckets
                case 2:m.wait=11;m.angle=0;TruckTrips++;break;             // at the tip: up, hold, down
                case 3:m.wait=3;m.loaded=true;break;                       // scoop
                case 4:m.wait=3.5f;break;                                  // dump on the stacks' pile
            }
        }
        // While stopped: the truck's body tips; the loader's bucket lifts and dumps.
        void Act(Machine m,float dt)
        {
            var leg=m.legs[(m.leg+m.legs.Count-1)%m.legs.Count];
            if(m.kind=="truck"&&leg.action==2){float t=11-m.wait;float a=t<4?t/4:t<7?1:Mathf.Max(0,(11-t)/4);m.hinge.Set(-48*Mathf.SmoothStep(0,1,a));}
            if(m.kind=="truck"&&leg.action==1&&m.wait<.1f)Loads++;
            if(m.kind=="loader") {
                if(leg.action==3){m.angle=Mathf.MoveTowards(m.angle,8,20*dt);m.hinge.Set(m.angle);}
                if(leg.action==4){float t=3.5f-m.wait;m.angle=t<1.5f?Mathf.MoveTowards(m.angle,-34,40*dt):Mathf.MoveTowards(m.angle,-10,30*dt);m.hinge.Set(m.angle);if(m.wait<.1f)m.loaded=false;}
            }
        }
        // Ground pose: height from the ground under the machine, pitch from its front and back.
        static void Pose(Machine m)
        {
            var rot=Quaternion.Euler(0,m.yaw,0);var f=rot*Vector3.forward;var p=new Vector3(m.at.x,0,m.at.y);
            float hf=ValleyField.Height(p.x+f.x*3,p.z+f.z*3),hb=ValleyField.Height(p.x-f.x*3,p.z-f.z*3);p.y=(hf+hb)*.5f;
            float pitch=-Mathf.Atan2(hf-hb,6)*Mathf.Rad2Deg;var target=rot*Quaternion.Euler(pitch,0,0);
            var rb=m.go.GetComponent<Rigidbody>();if(rb!=null){rb.MovePosition(p);rb.MoveRotation(target);}else m.go.transform.SetPositionAndRotation(p,target);
        }
        // The excavator: dig (arm down into the heap, up), slew to the truck when one is waiting, dump, slew back.
        float cycle;
        void Excavate(float dt)
        {
            if(digTop==null)return;
            bool truckWaiting=false;foreach(var t in trucks)if(t.wait>0&&t.legs[(t.leg+t.legs.Count-1)%t.legs.Count].action==1)truckWaiting=true;
            float digYaw=Yaw(DigAt-Digger),loadYaw=Yaw(LoadBay+new Vector2(2,0)-Digger);
            cycle+=dt;float c=cycle%8;
            float targetSlew=truckWaiting&&c>3.5f&&c<7.5f?loadYaw:digYaw;slew=Mathf.MoveTowardsAngle(slew,targetSlew,45*dt);Slew();
            // (Positive dips the arm: at 0 the bucket rests on the ground.) In the heap, lift, carry; raised over the truck's body.
            float targetArm=truckWaiting&&c>5&&c<6.5f?-24:c<1.5f?3:c<3?-28:-18;arm=Mathf.MoveTowards(arm,targetArm,35*dt);digArm.Set(arm);
        }
        void Slew()
        {
            var rb=digTop.GetComponent<Rigidbody>();var rot=Quaternion.Euler(0,slew,0);
            if(rb!=null)rb.MoveRotation(rot);else digTop.transform.rotation=rot;
        }
    }
}

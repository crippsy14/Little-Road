using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Quarry Hills' festivals and routines (docs/60, the outsider review: "a town that lives by the clock and throws a party").
    //   Routines (by TimeOfDay.Hour): the Caban opens 7 to 7; the quarry blasts in working hours (8 to 5, Ambience); the
    //   choir gathers at Capel Bethel in the evening (cars parked outside, ready for a chat).
    //   Rally Day (once you've a Rallycross podium, or --festival): flags round the square, a banner over the High Street,
    //   a parade of off-roaders up and down it with pennants on their roofs, and confetti over the square.
    // Built by AdventureGame in free roam (--quarry-hills); placed by search like the notice boards (Wayfinding).
    public sealed class QuarryFestival:MonoBehaviour
    {
        public static bool Forced=>RaceTrial.Has("--festival");
        public static bool On(AdventureProgress p)=>QuarryHills.Enabled&&(Forced||p?.podiums!=null&&p.podiums.Contains("quarry"));
        public static bool CabanOpen=>TimeOfDay.Hour>=7&&TimeOfDay.Hour<19;
        public static bool WorkingHours=>TimeOfDay.Hour>=8&&TimeOfDay.Hour<17;
        public static bool ChoirHours=>TimeOfDay.Hour>=18.5f&&TimeOfDay.Hour<21.75f;
        public static int Flags,Parade,Singers;public static bool Festival;public static Vector3 ParadeAt;
        Transform choir,player;readonly List<Transform> flags=new();Vector3 square;float nextConfetti;

        static readonly string[] ChoirTalk={
            "Choir night! Tenors at the front, and nobody park on the sopranos.",
            "We're doing Calon Lân again. We always do Calon Lân again.",
            "Practice was meant to start at seven. It's Quarry Hills: it starts when the kettle's boiled.",
            "My Gareth sang in this choir for forty years. He'd say the altos are flat. He'd be right.",
            "Listen at the door after: the whole square hums for an hour."};
        static readonly string[] Singers_={"Eirlys","Gareth","Dilys","Bryn","Ceri","Owain","Mair","Iolo"};
        static readonly string[] ParadeTalk={"Rally Day! Wave if you see Gwen!","Beep beep! Mind the pennants!","Up the High Street and back, all afternoon!"};

        public static QuarryFestival Build(Transform parent,AdventureProgress save,RoadCar playerCar)
        {
            var go=new GameObject("Quarry Hills festival and routines");go.transform.SetParent(parent);var f=go.AddComponent<QuarryFestival>();
            f.player=playerCar!=null?playerCar.transform:null;f.square=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);
            Flags=Parade=Singers=0;Festival=On(save);Physics.SyncTransforms();
            f.Choir(go.transform);
            if(Festival){f.FlagsRound(go.transform);f.Banner(go.transform);f.Parade_(go.transform,playerCar);}
            Debug.Log($"LITTLE_ROAD_FESTIVAL festival={Festival} flags={Flags} parade={Parade} choir={Singers} hour={TimeOfDay.Hour:0.0}");
            return f;
        }
        // ---------------------------------------------------------------- choir night
        void Choir(Transform parent)
        {
            if(!QuarryHillsTown.Spots.TryGetValue("chapel",out var chapel))return;
            choir=new GameObject("Choir night at Capel Bethel").transform;choir.SetParent(parent);
            var rng=new System.Random(1904);string[] bodies={"otter","beetle","bubble","pebble","tallboy","kipper","cob","drover","fell"};
            var toChapel=chapel.at-square;toChapel.y=0;float bearing=Mathf.Atan2(toChapel.x,toChapel.z)*Mathf.Rad2Deg,dist=toChapel.magnitude;
            foreach(float r in new[]{dist-11,dist-16})for(int k=-3;k<=3&&Singers<7;k++) {
                var p=square+Quaternion.Euler(0,bearing+k*9,0)*Vector3.forward*r;
                if(!Wayfinding.Ground(p.x,p.z,square.y,3,out var g,out bool road)||road)continue;
                var face=chapel.at-g.point;face.y=0;
                var car=TrialWorld.Car(bodies[rng.Next(bodies.Length)],PaintCatalog.All[rng.Next(PaintCatalog.Standard)].color);car.name="Choir car";car.transform.SetParent(choir);
                // (The square's slate paving is drawn, not solid: on it, stand on its top, not the ground under it.)
                var stand=g.point;if(Mathf.Abs(stand.x-square.x)<23&&Mathf.Abs(stand.z-square.z)<23)stand.y=Mathf.Max(stand.y,QuarryHills.SquareY+.02f);
                car.transform.SetPositionAndRotation(stand+Vector3.up*.03f,Quaternion.LookRotation(face));
                var box=car.AddComponent<BoxCollider>();box.center=new Vector3(0,.9f,0);box.size=new Vector3(1.9f,1.8f,3.9f);car.tag="Respawn";
                if(StationarySpot.Settle(car)<0){Destroy(car);continue;}
                var chat=car.AddComponent<NpcChatter>();chat.speaker=Singers_[Singers%Singers_.Length];chat.title="Capel Bethel choir";chat.colour=Toy.Party[Singers%Toy.Party.Length];
                chat.lines=new[]{"Shh! We're warming up!","Mind the hymn books!","Do-re-mi... beep!"};chat.talk=new[]{ChoirTalk[rng.Next(ChoirTalk.Length)],ChoirTalk[rng.Next(ChoirTalk.Length)]};
                Debug.Log($"LITTLE_ROAD_CHOIR_CAR {car.name} at=({car.transform.position.x:0.0},{car.transform.position.y-square.y:0.00},{car.transform.position.z:0.0}) ground={g.collider.name}");
                Singers++;
            }
            choir.gameObject.SetActive(ChoirHours);
        }
        // ---------------------------------------------------------------- Rally Day
        static Material Mat(string name,int hex)=>TrialWorld.Material(name,Toy.Hex(hex),0,.25f);
        void FlagsRound(Transform parent)
        {
            var pole=TrialWorld.Material("Festival pole",new Color(.93f,.93f,.9f),0,.35f);
            Material[] cloth={Mat("Festival flag red",0xc8102e),Mat("Festival flag green",0x1f8a4c),Mat("Festival flag white",0xf4f1ea),Mat("Festival flag gold",0xf2b81d)};
            for(int k=0;k<36&&Flags<12;k++) {
                float a=k*10+5;var p=square+Quaternion.Euler(0,a,0)*Vector3.forward*(k%2==0?22:25);
                if(!Wayfinding.Ground(p.x,p.z,square.y,3,out var g,out bool road)||road)continue;
                if(!Wayfinding.Clear(g.point+Vector3.up*3.2f,new Vector3(.3f,3,.3f),Quaternion.identity,g.collider))continue;
                bool near=false;foreach(var f in flags)if((f.position-g.point).sqrMagnitude<7*7)near=true;if(near)continue;
                var root=new GameObject("Festival flag").transform;root.SetParent(parent);root.position=g.point;
                Piece(root,"Pole",new Vector3(0,3,0),new Vector3(.1f,6,.1f),pole,true);
                var flag=Piece(root,"Flag",new Vector3(.85f,5.35f,0),new Vector3(1.6f,1f,.04f),cloth[Flags%cloth.Length],false);
                root.rotation=Quaternion.Euler(0,Random.Range(0,360),0);flags.Add(root);Flags++;
            }
        }
        void Banner(Transform parent)
        {
            var road=HillcrestRoute.Find(QuarryHills.HighStreetName);if(road==null)return;
            float s=Mathf.Min(45,road.Length*.3f);var at=road.At(s);var right=road.Right(s);float half=road.width*.5f+1.4f;
            var pole=TrialWorld.Material("Festival pole",new Color(.93f,.93f,.9f),0,.35f);var red=Mat("Festival banner",0xc8102e);
            var root=new GameObject("Rally Day banner").transform;root.SetParent(parent);root.SetPositionAndRotation(at,Quaternion.LookRotation(Vector3.Cross(right,Vector3.up)));
            foreach(int side in new[]{-1,1}) {
                var foot=at+right*side*half;if(Wayfinding.Ground(foot.x,foot.z,at.y,3,out var g,out _))foot=g.point;
                Piece(root,"Banner pole",root.InverseTransformPoint(foot)+new Vector3(0,3.4f,0),new Vector3(.12f,6.8f,.12f),pole,true);
            }
            Piece(root,"Banner",new Vector3(0,5.6f,0),new Vector3(half*2-.6f,1.1f,.05f),red,false);
            foreach(int face in new[]{-1,1})Words(root,"RALLY DAY  ·  DIWRNOD RALI",new Vector3(0,5.6f,face*.04f),Quaternion.Euler(0,face<0?0:180,0),.075f);
        }
        void Parade_(Transform parent,RoadCar playerCar)
        {
            var road=HillcrestRoute.Find(QuarryHills.HighStreetName);if(road==null||road.Length<120)return;
            var holder=new GameObject("Rally Day parade").transform;holder.SetParent(parent);
            Material[] pennant={Mat("Pennant red",0xc8102e),Mat("Pennant green",0x1f8a4c),Mat("Pennant gold",0xf2b81d)};var mast=TrialWorld.Material("Pennant mast",new Color(.2f,.2f,.22f),0,.3f);
            for(int i=0;i<6;i++) {
                var car=TrialWorld.Car(CarBodies.OffRoad[i%CarBodies.OffRoad.Length],PaintCatalog.All[(i*5+3)%PaintCatalog.Standard].color);car.name="Rally Day parade "+i;car.transform.SetParent(holder);
                var cruiser=car.AddComponent<RoadCruiser>();cruiser.Setup(road,20,road.Length-20,24+i*12,1,playerCar);cruiser.Cruise=5.5f;
                Piece(car.transform,"Pennant mast",new Vector3(.5f,2.6f,-.6f),new Vector3(.04f,1.4f,.04f),mast,false);
                Piece(car.transform,"Pennant",new Vector3(.82f,3.05f,-.6f),new Vector3(.6f,.38f,.02f),pennant[i%pennant.Length],false).localRotation=Quaternion.Euler(0,90,0);
                var chat=car.AddComponent<NpcChatter>();chat.speaker="Rally Day";chat.lines=ParadeTalk;chat.talkable=false;
                Parade++;
            }
            ParadeAt=road.At(60);
        }
        static Transform Piece(Transform parent,string name,Vector3 local,Vector3 size,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Destroy(go.GetComponent<Collider>());return go.transform;
        }
        static void Words(Transform parent,string words,Vector3 local,Quaternion rotation,float size)
        {
            var go=new GameObject("Sign / "+words);go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localRotation=rotation;
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=size;text.color=new Color(.98f,.95f,.85f);
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
        }
        void Update()
        {
            if(choir!=null&&choir.gameObject.activeSelf!=ChoirHours)choir.gameObject.SetActive(ChoirHours);
            if(!Festival)return;
            // The flags stir in the wind.
            for(int i=0;i<flags.Count;i++){var flag=flags[i].GetChild(1);flag.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*1.3f+i)*14,0);}
            // Confetti over the square now and then while you're there.
            if(player!=null&&Time.time>nextConfetti&&(player.position-square).sqrMagnitude<150*150){nextConfetti=Time.time+Random.Range(5f,9f);
                Confetti.Burst(square+new Vector3(Random.Range(-14f,14f),Random.Range(5f,8f),Random.Range(-14f,14f)));}
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // What makes Hillcrest Valley feel lived in: the steam train on the viaduct, cars pottering
    // along the town roads, hot-air balloons, birds, and a title card whenever you arrive
    // somewhere new. Free roam only; --no-valley-life leaves the valley still (for comparisons).
    public sealed class ValleyLife:MonoBehaviour
    {
        RoadCar player;
        public void Setup(RoadCar playerCar,bool traffic=true)
        {
            player=playerCar;WheatField.Player=playerCar;ResidentIdle.Player=playerCar!=null?playerCar.transform:null;
            var root=new GameObject("Hillcrest Valley life").transform;
            if(BayviewKit.Has("train_loco"))new GameObject("Hillcrest line train").AddComponent<ValleyTrain>().Setup(root);
            if(traffic){Traffic(root);Parked(root);Townsfolk(root);}
            if(BayviewKit.Has("balloon"))Balloons(root);
            Birds(root);
            if(player!=null){gameObject.AddComponent<AreaBanner>().Setup(player.transform);player.gameObject.AddComponent<DustTrail>().Setup(player);}
            if(HillcrestTown.Chimneys.Count>0)new GameObject("Chimney smoke").AddComponent<ChimneySmoke>().Setup(HillcrestTown.Chimneys,player!=null?player.transform:null);
        }
        void Traffic(Transform root)
        {
            string[] bodies={"minibus","taxi","postvan","beetle","ritz","bubble","camper","pebble","tallboy","marlow"};
            string[] names={"Nell","Hugo","Bea","Otis","Ivy","Rufus","Poppy","Alfie","Mabel","Stan","Juno","Percy"};
            var roads=new List<(HillcrestRoute.Road road,float from,float to)>{
                (HillcrestRoute.Quay,6,HillcrestRoute.Quay.Length-6),(HillcrestRoute.Quay,6,HillcrestRoute.Quay.Length-6),(HillcrestRoute.Quay,6,HillcrestRoute.Quay.Length-6),
                // Via Crest traffic turns round on the approach, 45 m short of the road's end: turning in the
                // castle gateway it met cars coming up nose to nose and jammed the gate.
                (HillcrestRoute.Crest,8,HillcrestRoute.Crest.Length-45),(HillcrestRoute.Crest,8,HillcrestRoute.Crest.Length-45),(HillcrestRoute.Crest,8,HillcrestRoute.Crest.Length-45),
                (HillcrestRoute.Mainline,HillcrestRoute.PortalSouthRun+30,HillcrestRoute.Mainline.Length-8),(HillcrestRoute.Falls,6,HillcrestRoute.Falls.Length-10),
                (HillcrestRoute.StationSpur,6,HillcrestRoute.StationSpur.Length-6),(HillcrestRoute.RacewayAccess,6,HillcrestRoute.RacewayAccess.Length-10)};
            var rng=new System.Random(88);
            for(int i=0;i<roads.Count;i++) {
                var (road,from,to)=roads[i];Color paint=PaintCatalog.All[rng.Next(PaintCatalog.All.Length)].color;
                var go=TrialWorld.Car(bodies[i%bodies.Length],paint);go.name="Valley traffic "+i;go.transform.SetParent(root);
                var cruiser=go.AddComponent<RoadCruiser>();cruiser.Setup(road,from,to,Mathf.Lerp(from,to,(float)rng.NextDouble()),rng.Next(2)*2-1,player);
                var chatter=go.AddComponent<NpcChatter>();chatter.speaker=names[i%names.Length];chatter.colour=paint;chatter.title="Out for a drive";
                chatter.lines=new[]{"Oi! Hillcrest drivers indicate, you know!","Beep beep! Off to market?","Mind my bumper, I'm late for the bakery!","Lovely day for the valley road!","Have you seen the falls yet?","Race you to the castle!","The train's due any minute!"};
            }
            // Quarry Hills' traffic (--quarry-hills): the town's streets, and Dai's dumper grinding up and down the haul road.
            if(QuarryHills.Enabled) {
                string[] welsh={"Bore da! Mind the slate dust!","Iawn? Off to the Caban?","Wrong side of the road, butt!","Chapel's at six. Don't be late!","Seen the Blondin running? Lush.","Rain later. Always is.","Hwyl fawr! See you at the quay!"};
                var quarryRoads=new List<(string road,float from,float back)>{(QuarryHills.QuarryRoadName,30,40),(QuarryHills.QuarryRoadName,30,40),(QuarryHills.HighStreetName,10,10),(QuarryHills.QuayLaneName,10,12),(QuarryHills.ChapelStreetName,8,8),(QuarryHills.TerraceRowName,8,8)};
                for(int i=0;i<quarryRoads.Count;i++) {
                    var road=HillcrestRoute.Find(quarryRoads[i].road);float from=quarryRoads[i].from,to=road.Length-quarryRoads[i].back;Color paint=PaintCatalog.All[rng.Next(PaintCatalog.All.Length)].color;
                    var go=TrialWorld.Car(bodies[(i+3)%bodies.Length],paint);go.name="Quarry traffic "+i;go.transform.SetParent(root);
                    go.AddComponent<RoadCruiser>().Setup(road,from,to,Mathf.Lerp(from,to,(float)rng.NextDouble()),rng.Next(2)*2-1,player);
                    var chatter=go.AddComponent<NpcChatter>();chatter.speaker=new[]{"Delyth","Rhodri","Nerys","Ifan","Carys","Geraint"}[i%6];chatter.colour=paint;chatter.title="Out for a drive";chatter.lines=welsh;
                }
                var haul=HillcrestRoute.Find(QuarryHills.HaulRoadName);var dumper=TrialWorld.Car("tallboy",Toy.Hex(0xf1c40f));dumper.name="Dai's dumper";dumper.transform.SetParent(root);
                dumper.AddComponent<RoadCruiser>().Setup(haul,12,haul.Length-12,haul.Length*.5f,1,player);
                var dai=dumper.AddComponent<NpcChatter>();dai.speaker="Dai the Dumper";dai.colour=Toy.Sun;dai.title="Quarry haulage";
                dai.lines=new[]{"Twelve tonnes of slate on here, butt. Give way!","Haul road's mine till noon!","Brakes? Who needs brakes going UP?","Race you to the floor. Go on. I dare you."};
            }
        }
        // Parked cars where people gather: paddock, square, station, viewpoints.
        void Parked(Transform root)
        {
            var spots=new List<(Vector3 at,float yaw)>();var pad=HillcrestRoute.Paddock;
            for(int i=0;i<8;i++)spots.Add((new Vector3(pad.x-34+i*6.5f,0,pad.z+10),180));
            var sq=HillcrestRoute.Square;var outward=new Vector3(sq.x-HillcrestRoute.HillCentre.x,0,sq.z-HillcrestRoute.HillCentre.y).normalized;
            foreach(float phi in new[]{-44f,44f,-118f,118f}){var d=Quaternion.Euler(0,phi,0)*outward;var p=sq+d*(Mathf.Abs(phi)>100?16.5f:26.5f);spots.Add((p,Quaternion.LookRotation(Vector3.Cross(Vector3.up,d)).eulerAngles.y));}
            var yard=HillcrestRoute.StationYard;for(int i=0;i<3;i++)spots.Add((yard+new Vector3(-7+i*5.5f,0,-12),0));
            var lk=HillcrestRoute.Lookout;spots.Add((lk+new Vector3(-9,0,-6),40));spots.Add((HillcrestRoute.Vista+new Vector3(-8,0,-6),150));
            spots.AddRange(QuarryFloor.Parking); // outside the rally school (--quarry-hills)
            string[] bodies={"marlow","heron","picnic","doodle","otter","bramble","pebble","tallboy","dover","kipper","sprint","roadster"};var rng=new System.Random(41);
            foreach(var (spotAt,yaw) in spots) {
                // Slide along the row (0, +-2.5, +-5, +-7.5 m) to the nearest spot clear of any kit prop.
                var at=spotAt;var along=Quaternion.Euler(0,yaw,0)*Vector3.right;
                foreach(float o in new[]{0f,2.5f,-2.5f,5f,-5f,7.5f,-7.5f}){var c=spotAt+along*o;float gy=ValleyField.Height(c.x,c.z);
                    if(!BayviewKit.Occupied(new Bounds(new Vector3(c.x,gy+.9f,c.z),new Vector3(3.2f,1.4f,3.2f)))){at=c;break;}}
                var p=at;p.y=Mathf.Max(ValleyField.Height(p.x,p.z),at.y>0?at.y:-99)+.02f;
                var go=TrialWorld.Car(bodies[rng.Next(bodies.Length)],PaintCatalog.All[rng.Next(PaintCatalog.All.Length)].color);go.name="Parked car";go.transform.SetParent(root);
                go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
                var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var x in renderers)bounds.Encapsulate(x.bounds);
                var box=go.AddComponent<BoxCollider>();box.center=go.transform.InverseTransformPoint(bounds.center);box.size=new Vector3(1.8f,1.5f,3.8f);go.tag="Respawn";
                StationarySpot.Settle(go);
                var chatter=go.AddComponent<NpcChatter>();chatter.speaker=new[]{"Lou","Kit","Vera","Ned","Olive","Sid","Tess","Arlo"}[rng.Next(8)];chatter.colour=Toy.Sky;
                chatter.title="Parked up";chatter.talk=new[]{Gossip[rng.Next(Gossip.Length)],Gossip[rng.Next(Gossip.Length)]};go.AddComponent<ResidentIdle>();
            }
        }
        // Residents chatting outside their houses: E to talk (AdventureGame), bump for a bubble.
        public sealed class Folk{public Transform car;public string name,title;public string[] lines;}
        public static readonly List<Folk> People=new();
        static readonly string[] FolkNames={"Aggie","Bram","Cosmo","Delphine","Elio","Fenna","Gus","Hattie","Ignazio","Juno","Kip","Lottie","Marco","Nonna Rosa","Orla","Pietro","Queenie","Rocco","Sunny","Tilda",
            "Umberto","Vivi","Walt","Xena","Yusuf","Zola","Bea","Carlo","Dotty","Enzo","Flo","Gino","Hugo","Ines","Jojo","Kiki","Lulu","Mimi","Nico","Olga","Paolo","Ruby","Salvo","Tess","Ugo","Vera","Wendell","Yara","Zeno","Alba"};
        public static readonly string[] Gossip={
            "Duke Rolo pays 300 credits to anyone who climbs to the castle in under 85 seconds. My record is... let's not talk about it.",
            "Gio's Valley V6 is the talk of the town. Pricey, but my cousin swears by it.",
            "Someone hid a Hillcrest stamp near the balloon field. I saw a glint when the wind blew!",
            "The train comes out of the headland, over the falls viaduct and round the station loop. Stella knows the timetable.",
            "Mind the hairpin after the tunnel. Everyone overcooks it their first time. I did. Twice.",
            "I grow sunflowers out east. Taller than my roof rack!",
            "The Grand Hotel does a lovely breakfast. Mostly petrol and toast.",
            "Market day is every day in Hillcrest. That's the rule.",
            "Forty years on Via Crest and I still get dizzy going round.",
            "The river runs all the way to the sea through the gorge. You can hear the falls from the castle on a still day.",
            "Those wind turbines up on the ridge power the whole valley. And my hairdryer.",
            "Try the gelato van in the square. Pistachio is the only correct answer.",
            "Vito's raceway is flat as a pancake, but the corners by the lake bite!",
            "Bayview? Tiny little island. Lovely lighthouse, mind.",
            "There's a stamp tucked up in the vineyards south of town. Don't tell Stella I told you.",
            "Wren has been at the viewpoint since sunrise. Says she's 'taking it all in'.",
            "My tyres are Gio's hill grips. Stick like jam on toast.",
            "The castle keep has a flag you can see from the other side of the valley.",
            "Moss catches the biggest fish right below the falls. Or so he says.",
            "They say one day the line will run further, to towns over the mountains. Wouldn't that be something?",
            "The balloon pilots go up every morning. I get queasy on a kerb.",
            "Lost? Just follow the river. It always knows the way home.",
            "I painted my shutters to match my bonnet. Very important.",
            "Watch out on the spiral, the tourists stop anywhere for a photo!"};
        static readonly string[] QuarryNames={"Dafydd","Sian","Gethin","Nia","Rhys","Ffion","Iolo","Megan","Owain","Cerys","Huw","Eleri","Bryn","Angharad","Emyr","Lowri","Gwilym","Mared","Ianto","Bethan","Tomos","Seren","Aled","Elin","Elfed","Morfudd","Cai","Heledd"};
        public static readonly string[] QuarryGossip={
            "Bore da! That's good morning. You'll pick it up.",
            "My taid split slate in the galleries for forty years. Twelve benches, and he knew every one by name.",
            "The lake in the pit is that colour on its own. Nobody painted it, honest.",
            "Don't go near the edge of the galleries in the wet. Or the dry. Just don't.",
            "Slate Quay used to send roofs all over the world. Now it sends me fishing.",
            "There's talk of a rally school down on the quarry floor. Loose stuff, mind. Not like your tarmac.",
            "The haul road's gravel. Ease off before the bends or you'll be on the tips.",
            "Capel Bethel choir practises on Thursdays. You can hear it from the crest.",
            "The Caban does a cawl that'll fix anything. Even a bent wheel.",
            "Those grey hills? Tips. Waste slate. Two tonnes of waste for every tonne of roof.",
            "Kettle Falls is up past the high street, along the gravel. Lovely when it's been raining. Which is always.",
            "Rain? This? This is just damp. Proper rain is sideways.",
            "Hillcrest folk think they've got hills. Bless them.",
            "We had a Blondin up on the rim once, carrying wagons across the pit on a cable. Imagine racing that!"};
        static readonly string[] FolkBump={"Oi! That's my front yard!","Mind the geraniums!","Honk! I'm parked, you know!","Careful, I just polished that!","Ooh, a visitor!","Beep! Hello to you too!"};
        void Townsfolk(Transform root)
        {
            People.Clear();var rng=new System.Random(512);
            string[] bodies={"bubble","beetle","taxi","camper","ritz","postvan","gelato","minibus","pebble","tallboy","marlow","dover","heron","bramble","picnic","kipper","doodle","otter"};
            var homes=new List<(Vector3 yard,float yaw,string house)>(HillcrestTown.Homes);int placed=0,name=0;
            for(int i=0;i<homes.Count&&placed<72;i++) {
                if(rng.NextDouble()>.42)continue;var (yard,yaw,_)=homes[i];
                string body=bodies[rng.Next(bodies.Length)];if(body=="gelato"||body=="minibus")body="beetle";
                Spawn(root,body,PaintCatalog.All[rng.Next(PaintCatalog.All.Length)].color,yard,yaw,FolkNames[name++%FolkNames.Length],"Hillcrest",new[]{Gossip[rng.Next(Gossip.Length)],Gossip[rng.Next(Gossip.Length)]});placed++;
            }
            // Quarry Hills' residents, outside their terraces (--quarry-hills).
            int quarryPlaced=0;
            for(int i=0;i<QuarryHillsTown.Homes.Count&&quarryPlaced<110;i++) {
                if(rng.NextDouble()>.36)continue;var (yard,yaw,_)=QuarryHillsTown.Homes[i];
                string body=bodies[rng.Next(bodies.Length)];if(body=="gelato"||body=="minibus")body="beetle";
                Spawn(root,body,PaintCatalog.All[rng.Next(PaintCatalog.All.Length)].color,yard,yaw,QuarryNames[quarryPlaced%QuarryNames.Length],"Quarry Hills",new[]{QuarryGossip[rng.Next(QuarryGossip.Length)],QuarryGossip[rng.Next(QuarryGossip.Length)]});quarryPlaced++;
            }
            void Special(string spot,string body,Color paint,string who,string title,params string[] lines){if(HillcrestTown.Spots.TryGetValue(spot,out var s))Spawn(root,body,paint,s.at,s.yaw,who,title,lines);}
            // (Gwen at the rally school is a resident: AdventureGame.)
            Special("gelato","gelato",Toy.Hex(0xffd6e8),"Gelato Gina","Gelato van","Gelato! Twelve flavours, all of them pistachio. Well, nearly.","Best seat in town: right by the fountain, watching the market.");
            Special("postvan","postvan",Toy.Hex(0xd92b3a),"Postie Percival","Hillcrest Post","Letters for the castle, parcels for the hotel, postcards for everyone!","Pip on Bayview and I swap postcards every week. Island news travels slow!");
            if(HillcrestTown.Spots.TryGetValue("taxirank",out var rank))for(int k=0;k<2;k++)Spawn(root,"taxi",Toy.Hex(0xffd23f),rank.at+new Vector3(0,0,k*5.5f),rank.yaw,k==0?"Cabbie Carmine":"Cabbie Lou","Station taxis",new[]{k==0?"Taxi? Where to? Anywhere in the valley, as long as it's downhill.":"The train brings the tourists, I bring them up the hill. Teamwork!"});
            Debug.Log("LITTLE_ROAD_TOWNSFOLK placed="+People.Count);
        }
        void Spawn(Transform root,string body,Color paint,Vector3 at,float yaw,string who,string title,string[] lines)
        {
            var p=at;p.y=Mathf.Max(at.y,ValleyField.Height(at.x,at.z))+.03f;
            var go=TrialWorld.Car(body,paint);go.name="Resident "+who;go.transform.SetParent(root);go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            var box=go.AddComponent<BoxCollider>();box.center=new Vector3(0,.9f,0);box.size=new Vector3(1.9f,1.8f,body=="minibus"?4.4f:body=="camper"||body=="ritz"?4.1f:3.7f);go.tag="Respawn";
            if(StationarySpot.Settle(go)<0){Destroy(go);return;} // front yards sat against the house fronts; no clear spot: leave them out
            var chatter=go.AddComponent<NpcChatter>();chatter.speaker=who;chatter.colour=Toy.Party[People.Count%Toy.Party.Length];chatter.lines=FolkBump;chatter.talkable=false;
            go.AddComponent<ResidentIdle>();
            People.Add(new Folk{car=go.transform,name=who,title=title,lines=lines});
        }
        void Balloons(Transform root)
        {
            int[][] tints={new[]{0xff4d5e,0x3a7bd5},new[]{0xffb400,0x2e9e5b},new[]{0xff7ac8,0x1f8a70},new[]{0x3a7bd5,0xffd23f},new[]{0x2e9e5b,0xff4d5e},new[]{0x9b6bd9,0xffb400}};
            var field=HillcrestRoute.BalloonField;
            for(int i=0;i<6;i++) {
                var tint=new BayviewKit.Tint(Color.white,Toy.Hex(tints[i][1]),Toy.Hex(tints[i][0]),Color.white);
                var b=BayviewKit.Spawn("balloon",Vector3.zero,i*60,root,tint,solid:false,scale:i<2?1:1.2f);var drift=b.AddComponent<BalloonDrift>();
                if(i<2){var p=new Vector3(field.x+(i==0?-18:20),0,field.y+(i==0?6:-10));p.y=ValleyField.Height(p.x,p.z)+(i==0?.1f:12);drift.Tethered(p,i);}
                else drift.Wander(new Vector3(150,0,-1080),380+i*90,95+i*18,i);
            }
        }
        void Birds(Transform root)
        {
            var flocks=new List<(Vector3,float,Color)>{(new Vector3(200,72,-1110),26f,Toy.Hex(0x3d3a4a)),(new Vector3(150,24,-870),40f,Color.white),(new Vector3(-392,26,-1250),55f,Color.white),(HillcrestRoute.FallsHead+Vector3.up*28,30f,Color.white),(HillcrestRoute.Square+Vector3.up*22,20f,Toy.Hex(0x3d3a4a))};
            // Quarry Hills: choughs over the pit, gulls over Slate Quay, jackdaws round the chapel.
            if(QuarryHills.Enabled){flocks.Add((new Vector3(QuarryHills.Pit.x,QuarryHills.FloorY+70,QuarryHills.Pit.y),60f,Toy.Hex(0x2b2b33)));flocks.Add((QuarryHills.QuayPoint(0,-20,QuarryHills.QuayY+22),35f,Color.white));
                flocks.Add((new Vector3(QuarryHills.Square.x,QuarryHills.SquareY+26,QuarryHills.Square.y),22f,Toy.Hex(0x3d3a4a)));}
            var rng=new System.Random(5);
            foreach(var (centre,radius,colour) in flocks) {
                var material=TrialWorld.Material("Bird",colour,0,.2f);
                for(int i=0;i<7;i++){var bird=new GameObject("Bird").AddComponent<Bird>();bird.transform.SetParent(root);bird.Setup(centre+new Vector3((float)rng.NextDouble()*6,(float)rng.NextDouble()*5,(float)rng.NextDouble()*6),radius*(.8f+.4f*(float)rng.NextDouble()),(float)rng.NextDouble()*6.28f,material);}
            }
        }
    }

    // Kinematic car that potters along one road, keeping to the right, slowing behind the
    // player or another cruiser, turning round at each end of its beat.
    public sealed class RoadCruiser:MonoBehaviour
    {
        static readonly List<RoadCruiser> all=new();
        // Others the cruisers give way to as they do to you (the Duke's Regale out for a drive).
        public static readonly List<Transform> GiveWayTo=new();
        public static IReadOnlyList<RoadCruiser> All=>all;
        // Castle Climb: pulled over (stands still where it was put) until released.
        bool holding;public void Hold(){holding=true;speed=0;}public void Release(){holding=false;blocked=0;}
        public bool OnRoad(HillcrestRoute.Road r)=>road==r;
        HillcrestRoute.Road road;float from,to,s,speed,cruise,lane,laneNow,blocked;int dir;RoadCar player;Rigidbody body;
        public float Speed=>speed;
        public void Setup(HillcrestRoute.Road r,float a,float b,float start,int direction,RoadCar playerCar)
        {
            road=r;from=a;to=b;s=start;dir=direction;player=playerCar;cruise=Random.Range(7.5f,10.5f);lane=road.width*.5f-1.5f;laneNow=dir*lane;
            body=gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;
            var renderers=GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var x in renderers)bounds.Encapsulate(x.bounds);
            var box=gameObject.AddComponent<BoxCollider>();box.center=bounds.center-transform.position;box.size=bounds.size;gameObject.tag="Respawn";
            all.Add(this);Place(0);horn=Horn.Attach(gameObject,Random.Range(.88f,1.18f));
        }
        Horn horn;bool tooted;
        public void TootBack(){if(horn!=null&&!horn.Busy)horn.Toot(.1f,.09f,.14f,0);}
        void OnDestroy()=>all.Remove(this);
        Vector3 Heading=>(road.At(s+dir*2)-road.At(s-dir*2)).normalized;
        void Place(float dt)
        {
            var p=road.At(s)+road.Right(s)*laneNow+Vector3.up*.03f;var f=Heading;
            var rot=Quaternion.LookRotation(f);if(dt>0)rot=Quaternion.Slerp(body.rotation,rot,1-Mathf.Exp(-dt*4));
            if(dt>0){body.MovePosition(p);body.MoveRotation(rot);}else transform.SetPositionAndRotation(p,rot);
        }
        void FixedUpdate()
        {
            if(holding)return;
            float dt=Time.fixedDeltaTime,target=cruise;Vector3 me=body.position,fwd=Heading;
            bool Blocking(Vector3 other){var d=other-me;float along=Vector3.Dot(d,fwd),side=Vector3.Dot(d,Vector3.Cross(Vector3.up,fwd));return along>0&&along<13&&Mathf.Abs(side)<3.2f;}
            // Held up by the player: wait a moment, then turn round rather than jam the road.
            // (A second in, a polite beep-beep: docs/51.)
            bool held=player!=null&&Blocking(player.body.position);foreach(var t in GiveWayTo)if(t!=null&&Blocking(t.position))held=true;
            if(held){target=0;blocked+=dt;if(blocked>1&&!tooted){tooted=true;TootBack();}if(blocked>3){blocked=0;dir=-dir;}}else{blocked=0;tooted=false;}
            foreach(var c in all)if(c!=this&&c.dir==dir&&Blocking(c.body.position)){target=Mathf.Min(target,c.speed*.8f);}
            if((dir>0&&s>to-14)||(dir<0&&s<from+14))target=Mathf.Min(target,3.5f);
            speed=Mathf.MoveTowards(speed,target,(target<speed?6:2.2f)*dt);
            s+=dir*speed*dt;
            if(dir>0&&s>=to){s=to;dir=-1;}else if(dir<0&&s<=from){s=from;dir=1;}
            laneNow=Mathf.MoveTowards(laneNow,dir*lane,1.4f*dt);
            Place(dt);
        }
    }

    // Steam train on the Hillcrest Line: round the Hillcrest station loop, out over the valley and
    // the falls gorge, through the headland (hidden), along the sea viaduct and round the loop at
    // Bayview Halt, then all the way back. It stops at both stations.
    public sealed class ValleyTrain:MonoBehaviour
    {
        readonly List<Vector3> path=new();readonly List<float> run=new();readonly List<bool> hidden=new();readonly List<float> stops=new();
        readonly List<(Transform car,float offset,Renderer[] renderers)> cars=new();
        float head=20,speed,wait;int nextStop;Transform chimney;readonly List<(Transform puff,float age)> puffs=new();float puffTimer;Material puffMaterial;
        public static ValleyTrain Instance;public string NextStation=>nextStop%2==0?"Hillcrest":"Bayview Halt";
        void Add(IEnumerable<Vector3> points,bool isHidden){foreach(var p in points){if(path.Count>0&&Vector3.Distance(path[^1],p)<.05f)continue;path.Add(p);hidden.Add(isHidden);}}
        static IEnumerable<Vector3> Tunnel(Vector3 a,Vector3 b){int n=Mathf.CeilToInt(Vector3.Distance(a,b)/4);for(int i=1;i<n;i++)yield return Vector3.Lerp(a,b,i/(float)n);}
        static int Junction(Vector3[] line,Vector2 loop,float radius,float angle)
        {
            var j=new Vector2(loop.x+Mathf.Sin(angle*Mathf.Deg2Rad)*radius,loop.y+Mathf.Cos(angle*Mathf.Deg2Rad)*radius);
            for(int i=0;i<line.Length;i++)if(Vector2.Distance(new(line[i].x,line[i].z),j)<2.5f)return i;return line.Length-1;
        }
        public void Setup(Transform parent)
        {
            transform.SetParent(parent);Instance=this;
            var valley=HillcrestRoute.Rail.line;int jv=Junction(valley,HillcrestRoute.StationLoop,HillcrestRoute.StationLoopRadius,90);
            var sea=HillcrestRoute.SeaRail.line;int js=HillcrestRoute.Index(sea,new Vector3(HillcrestRoute.BayEntry.x,0,HillcrestRoute.BayEntry.y));
            // Round the Hillcrest loop, back out to the headland, through it, out to Bayview, round, and home.
            Add(valley[jv..],false);var outward=new List<Vector3>(valley[..(jv+1)]);outward.Reverse();Add(outward,false);
            Add(Tunnel(valley[0],sea[0]),true);Add(sea,false);var home=new List<Vector3>(sea[..(js+1)]);home.Reverse();Add(home,false);
            Add(Tunnel(sea[0],valley[0]),true);Add(valley[..(jv+1)],false);
            run.Add(0);for(int i=1;i<path.Count;i++)run.Add(run[i-1]+Mathf.Max(.01f,Vector3.Distance(path[i],path[i-1])));
            float At(Vector2 target,int from,int to){float best=float.MaxValue,where=0;for(int i=from;i<to;i++){float d=Vector2.Distance(new(path[i].x,path[i].z),target);if(d<best){best=d;where=run[i];}}return where;}
            int loopEnd=valley.Length-jv;
            stops.Add(At(new(HillcrestRoute.StationLoop.x-HillcrestRoute.StationLoopRadius,HillcrestRoute.StationLoop.y),0,loopEnd));
            int bayStart=path.Count-1;for(int i=0;i<path.Count;i++)if(Vector2.Distance(new(path[i].x,path[i].z),new(sea[js].x,sea[js].z))<2.5f){bayStart=i;break;}
            stops.Add(At(HillcrestRoute.BayPlatform,bayStart,path.Count));
            var livery=new BayviewKit.Tint(Toy.Hex(0x1d6fa8),Toy.Hex(0xffd23f),Toy.Hex(0xff4d5e),Color.white);
            Car("train_loco",-5.3f,livery,"Driver Bert");
            int[] coaches={0xff4d5e,0x2e9e5b,0xffb400};
            for(int k=0;k<3;k++)Car("train_coach",-19.6f-k*12,new BayviewKit.Tint(Toy.Hex(coaches[k]),Toy.Hex(0xfff0c8),Toy.Hex(0xff4d5e),Color.white),"Passengers");
            chimney=new GameObject("Chimney").transform;chimney.SetParent(cars[0].car,false);chimney.localPosition=new Vector3(0,4.8f,4.3f);
            puffMaterial=TrialWorld.Material("Steam puff",new Color(.97f,.97f,.97f),0,.1f);
            head=stops[0]+.5f;wait=6;nextStop=1;whistle=Horn.Attach(cars[0].car.gameObject,1,true);
            Debug.Log($"LITTLE_ROAD_TRAIN path={run[^1]:0} stops={string.Join(",",stops.ConvertAll(x=>x.ToString("0")))}");
        }
        // Beat the Train (docs/51): stop 0 is Hillcrest, stop 1 Bayview Halt.
        public float MaxSpeed=13;public System.Action<int> Arrived;
        public const int HillcrestStop=0,BayviewStop=1;
        public bool WaitingAt(int stop)=>wait>0&&(nextStop+stops.Count-1)%stops.Count==stop;
        // Brought to a stop's platform (from wherever it was), leaving in `seconds`.
        public void Hold(int stop,float seconds){head=stops[stop]+.5f;wait=seconds;nextStop=stop+1;speed=0;FixedUpdate();}
        // How long it takes from standing at one stop to standing at the next, at a given top speed.
        public float TimeToNext(int stop,float top)
        {
            float total=run[^1],from=stops[stop],to=stops[(stop+1)%stops.Count],dist=to-from;if(dist<0)dist+=total;
            float s=0,v=0,t=0,dt=.05f;
            while(s<dist-.6f&&t<3000){float left=dist-s;float target=Mathf.Min(top,Mathf.Sqrt(2*.7f*left)+.4f);v=Mathf.MoveTowards(v,target,1.1f*dt);s+=v*dt;t+=dt;}
            return t;
        }
        // The whistle: long-short as it pulls out of a station, and back at anyone who honks at it (docs/51).
        Horn whistle;
        public void Whistle(){if(whistle!=null&&!whistle.Busy&&Visible)whistle.Toot(.9f,.22f,.45f,0);}
        public bool Visible=>cars.Count>0&&cars[0].renderers.Length>0&&cars[0].renderers[0].enabled;
        public Transform Loco=>cars.Count>0?cars[0].car:null;
        void Car(string id,float offset,BayviewKit.Tint tint,string speaker)
        {
            var go=BayviewKit.Spawn(id,Vector3.down*200,0,transform,tint,solid:false);
            var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var box=go.AddComponent<BoxCollider>();box.center=new Vector3(0,2.2f,id=="train_loco"?-1.4f:0);box.size=new Vector3(2.6f,4.2f,id=="train_loco"?13.4f:11.2f);go.tag="Respawn";
            var chatter=go.AddComponent<NpcChatter>();chatter.speaker=speaker;chatter.colour=Toy.Sun;chatter.talkable=false;
            chatter.lines=id=="train_loco"?new[]{"TOOT TOOT! Stand clear of the line!","Next stop: Hillcrest!","Oi! Trains always have right of way!"}:new[]{"Ooh, a racer!","Wave to the train!","We're off to the seaside!"};
            cars.Add((go.transform,offset,go.GetComponentsInChildren<Renderer>()));
        }
        int Index(float d){d=Mathf.Clamp(d,0,run[^1]);int lo=0,hi=run.Count-1;while(hi-lo>1){int m=(lo+hi)/2;if(run[m]<=d)lo=m;else hi=m;}return lo;}
        Vector3 At(float d){int lo=Index(d),hi=Mathf.Min(lo+1,run.Count-1);return Vector3.Lerp(path[lo],path[hi],(Mathf.Clamp(d,0,run[^1])-run[lo])/Mathf.Max(run[hi]-run[lo],1e-4f));}
        void FixedUpdate()
        {
            float dt=Time.fixedDeltaTime,total=run[^1];
            if(wait>0){wait-=dt;speed=0;if(wait<=0)Whistle();}
            else {
                float stop=stops[nextStop%stops.Count],left=stop-head;if(left<0)left+=total;
                float target=Mathf.Min(MaxSpeed,Mathf.Sqrt(2*.7f*left)+.4f);
                if(left<.6f){wait=16;int arrived=nextStop%stops.Count;nextStop++;speed=0;Arrived?.Invoke(arrived);}
                speed=Mathf.MoveTowards(speed,target,1.1f*dt);head=Mathf.Repeat(head+speed*dt,total);
            }
            foreach(var (car,offset,renderers) in cars) {
                float d=Mathf.Repeat(head+offset,total);bool visible=!hidden[Index(d)]&&!hidden[Index(Mathf.Repeat(d+6,total))]&&!hidden[Index(Mathf.Repeat(d-6,total))];
                foreach(var r in renderers)r.enabled=visible;
                Vector3 front=At(Mathf.Repeat(d+4,total)),rear=At(Mathf.Repeat(d-4,total)),p=(front+rear)*.5f+Vector3.up*.33f;var rb=car.GetComponent<Rigidbody>();
                rb.MovePosition(p);if((front-rear).sqrMagnitude>.01f)rb.MoveRotation(Quaternion.LookRotation(front-rear));
            }
        }
        void Update()
        {
            bool running=speed>.5f&&cars.Count>0&&cars[0].renderers.Length>0&&cars[0].renderers[0].enabled;puffTimer-=Time.deltaTime;
            if(running&&puffTimer<=0){puffTimer=.35f;Puff();}
            for(int i=puffs.Count-1;i>=0;i--) {
                var (puff,age)=puffs[i];age+=Time.deltaTime;puffs[i]=(puff,age);
                puff.position+=Vector3.up*Time.deltaTime*2.2f;puff.localScale=Vector3.one*Mathf.Lerp(.6f,2.6f,age/2.4f)*(age>1.8f?Mathf.Max(0,(2.4f-age)/.6f):1);
                if(age>2.4f){Destroy(puff.gameObject);puffs.RemoveAt(i);}
            }
        }
        void Puff()
        {
            var p=GameObject.CreatePrimitive(PrimitiveType.Sphere);DestroyImmediate(p.GetComponent<Collider>());p.name="Steam puff";p.transform.SetParent(transform);
            p.transform.position=chimney.position;p.GetComponent<Renderer>().sharedMaterial=puffMaterial;p.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;puffs.Add((p.transform,0));
        }
    }

    // Stations on the line. Tickets move the player's car straight to another station's
    // forecourt: a fast-travel network new areas join by adding a station here.
    public static class TrainNetwork
    {
        public sealed class Station{public string id,name,resident;public Vector3 arrival;public float yaw;public System.Func<AdventureProgress,bool> open;}
        public const int Fare=20;
        public static List<Station> All()
        {
            var list=new List<Station>{new Station{id="bayview",name="Bayview Halt",resident="bayhalt",arrival=CoastalLandscape.HaltArrival,yaw=180,open=_=>true}};
            if(HillcrestTown.Spots.TryGetValue("station",out var st))list.Add(new Station{id="hillcrest",name="Hillcrest",resident="station",arrival=HillcrestRoute.StationYard+new Vector3(-2,0,4),yaw=0,open=p=>p.collected.Contains(Collectibles.VisitHillcrest)});
            return list;
        }
    }
    public sealed class BalloonDrift:MonoBehaviour
    {
        Vector3 home,centre;float radius,height,phase;bool wander;
        public void Tethered(Vector3 at,float p){home=at;phase=p;transform.position=at;}
        public void Wander(Vector3 c,float r,float h,float p){wander=true;centre=c;radius=r;height=h;phase=p;Update();}
        void Update()
        {
            float t=Time.time;
            if(!wander){transform.position=home+Vector3.up*(Mathf.Sin(t*.4f+phase)*.6f+Mathf.Max(0,Mathf.Sin(t*.05f+phase))*6);return;}
            float a=t*(3.2f/radius)+phase;var p=centre+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius*.7f);
            p.y=Mathf.Max(ValleyField.Height(p.x,p.z)+60,height)+Mathf.Sin(t*.3f+phase)*4;transform.position=p;transform.rotation=Quaternion.Euler(0,a*20*Mathf.Rad2Deg*.02f,0);
        }
    }

    // Toy bird: a body and two flapping wings circling a point.
    public sealed class Bird:MonoBehaviour
    {
        Vector3 centre;float radius,phase,speed;Transform left,right;
        public void Setup(Vector3 c,float r,float p,Material material)
        {
            centre=c;radius=r;phase=p;speed=Random.Range(.35f,.55f);
            var bodyPart=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(bodyPart.GetComponent<Collider>());bodyPart.transform.SetParent(transform,false);bodyPart.transform.localScale=new Vector3(.22f,.2f,.5f);bodyPart.GetComponent<Renderer>().sharedMaterial=material;
            Transform Wing(int side)
            {
                var pivot=new GameObject("Wing").transform;pivot.SetParent(transform,false);
                var w=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(w.GetComponent<Collider>());w.transform.SetParent(pivot,false);w.transform.localPosition=new Vector3(side*.42f,0,0);w.transform.localScale=new Vector3(.8f,.04f,.3f);
                w.GetComponent<Renderer>().sharedMaterial=material;w.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return pivot;
            }
            left=Wing(-1);right=Wing(1);
        }
        void Update()
        {
            float t=Time.time*speed+phase;var p=centre+new Vector3(Mathf.Sin(t)*radius,Mathf.Sin(t*2.3f)*2.5f,Mathf.Cos(t)*radius);
            var tangent=new Vector3(Mathf.Cos(t),0,-Mathf.Sin(t));transform.SetPositionAndRotation(p,Quaternion.LookRotation(tangent)*Quaternion.Euler(0,0,-12));
            float flap=Mathf.Sin(Time.time*9+phase*3)*35;left.localRotation=Quaternion.Euler(0,0,-flap);right.localRotation=Quaternion.Euler(0,0,flap);
        }
    }

    // Places: the first time you enter one, a "discovered" card (AdventureUI.Discovery via `discovered`);
    // after that its name just sits under the minimap (Minimap). No banner every time you cross a line.
    public sealed class AreaBanner:MonoBehaviour
    {
        Transform car;string current;float since=99;
        public System.Func<string,bool> known;public System.Action<string,string> discovered;public System.Action<string> entered;
        public string Current=>current;
        public static string Area(Vector3 p)
        {
            var main=HillcrestRoute.Mainline;float along=main.Nearest(p,out float lateral);
            if(HillcrestRoute.OnHaltQuay(p,0))return "BAYVIEW HALT";
            if(p.z>-300)return "BAYVIEW";
            if(along>HillcrestRoute.PortalNorthRun-2&&along<HillcrestRoute.PortalSouthRun+2&&Mathf.Abs(lateral)<8)return "HEADLAND TUNNEL";
            if(p.z>-452)return "THE CAUSEWAY";
            var hill=new Vector2(p.x-HillcrestRoute.HillCentre.x,p.z-HillcrestRoute.HillCentre.y).magnitude;
            if(hill<55)return "HILLCREST CASTLE";
            if(hill<200||(p.z<-880&&p.z>-960&&p.x>-100&&p.x<470))return "HILLCREST";
            if(Vector3.Distance(p,HillcrestRoute.Lookout)<70)return "SILVER FALLS";
            if(HillcrestRoute.Distance(HillcrestRoute.Circuit,p.x,p.z,out _)<60||Vector3.Distance(p,HillcrestRoute.Paddock)<80)return "HILLCREST RACEWAY";
            if(Vector3.Distance(p,HillcrestRoute.Vista)<40)return "VALLEY VIEWPOINT";
            if(Vector2.Distance(new(p.x,p.z),HillcrestRoute.FarmCentre)<150)return "GOLDEN ACRE FARM";
            return "HILLCREST VALLEY";
        }
        public static string Subtitle(string area)=>area switch{
            "BAYVIEW"=>"Home island · Harbour town","HEADLAND TUNNEL"=>"Mind the echo!","THE CAUSEWAY"=>"To the mainland","HILLCREST"=>"Market town on the hill",
            "HILLCREST CASTLE"=>"Views for miles","SILVER FALLS"=>"Hold on to your hat","HILLCREST RACEWAY"=>"Home of the Valley Cup","VALLEY VIEWPOINT"=>"Say cheese!",_=>"The mainland · Beyond the headland"};
        public void Setup(Transform playerCar){car=playerCar;current=Area(car.position);}
        void Update()
        {
            if(car==null)return;since+=Time.unscaledDeltaTime;
            string area=Area(car.position);if(area==current||since<3)return;
            current=area;since=0;entered?.Invoke(area);
            if(known!=null&&!known(area))discovered?.Invoke(area,Subtitle(area));
        }
    }
}

namespace LittleRoad
{
    // Toy puffs: spheres that swell, drift and shrink away (no particle shaders in the build).
    public abstract class PuffPool:MonoBehaviour
    {
        protected sealed class Puff{public Transform t;public Renderer r;public float age,life,size,alpha,flat,spin;public Vector3 velocity;public Color colour;}
        protected readonly List<Puff> live=new();readonly Stack<Transform> spare=new();protected Material material;
        // Soft puffs: transparent, fading out over their life, optionally flattened (dust hugs the ground).
        protected bool soft;static MaterialPropertyBlock block;
        protected static Material SoftMaterial(string name,Color colour)
        {
            var m=TrialWorld.Material(name,colour,0,0);m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_ZWrite",0);
            m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha",(float)UnityEngine.Rendering.BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType","Transparent");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster",false);m.SetShaderPassEnabled("DepthOnly",false);m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent;return m;
        }
        protected void Emit(Vector3 at,Vector3 velocity,float size,float life,float alpha=1,float flat=1,Color? colour=null)
        {
            var t=spare.Count>0?spare.Pop():Make();t.gameObject.SetActive(true);t.position=at;t.localScale=Vector3.one*size*.3f;t.rotation=Quaternion.Euler(0,Random.value*360,0);
            live.Add(new Puff{t=t,r=t.GetComponent<Renderer>(),velocity=velocity,size=size,life=life,alpha=alpha,flat=flat,spin=Random.Range(-40f,40f),colour=colour??material.color});
        }
        Transform Make()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);DestroyImmediate(go.GetComponent<Collider>());go.name="Puff";go.transform.SetParent(transform);   // (collider removed immediately: puffs spawn at the wheels)
            var r=go.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go.transform;
        }
        protected void Step(float dt)
        {
            for(int i=live.Count-1;i>=0;i--) {
                var p=live[i];p.age+=dt;float u=p.age/p.life;
                if(u>=1){p.t.gameObject.SetActive(false);spare.Push(p.t);live.RemoveAt(i);continue;}
                p.t.position+=p.velocity*dt;p.velocity*=1-dt*(soft?2.2f:.8f);
                if(!soft){p.t.localScale=Vector3.one*p.size*Mathf.Lerp(.3f,1,Mathf.Sqrt(u))*(u>.7f?(1-u)/.3f:1);continue;}
                // Soft: grows and spreads, fades out (fast in, long tail), a slow turn.
                float grow=p.size*Mathf.Lerp(.35f,1,1-(1-u)*(1-u));p.t.localScale=new Vector3(grow*1.15f,grow*p.flat,grow);p.t.Rotate(0,p.spin*dt,0);
                block??=new MaterialPropertyBlock();var c=p.colour;c.a=p.alpha*Mathf.Clamp01(u*8)*(1-u)*(1-u);block.SetColor("_BaseColor",c);p.r.SetPropertyBlock(block);
            }
        }
    }
    // Dust kicked up behind the car on grass, dirt and runoff.
    public sealed class DustTrail:PuffPool
    {
        RoadCar car;float timer;readonly Vector3?[] lastWheel=new Vector3?[2];
        static readonly Color Dirt=new(.74f,.66f,.52f),Grass=new(.62f,.62f,.48f),Smoke=new(.86f,.86f,.86f);
        public void Setup(RoadCar c){car=c;soft=true;material=SoftMaterial("Kicked-up dust",Dirt);transform.SetParent(null);}
        void Update()
        {
            float dt=Time.deltaTime;Step(dt);if(car==null||car.body==null)return;
            float speed=Mathf.Abs(car.speed);var v=car.body.linearVelocity;var f=car.transform.forward;var r=car.transform.right;
            float slide=Mathf.Abs(Vector3.Dot(v,r));
            bool offRoad=car.Grip>0&&car.Grip<car.tuning.roadGrip-.01f&&speed>2.5f&&car.ContactBottom>-9;
            bool skid=!offRoad&&car.Grip>0&&slide>3.2f&&speed>4;
            // Tyre marks from each rear wheel: rubber while sliding, ruts off road.
            bool marking=(skid||offRoad&&speed>5)&&car.ContactBottom>-9;
            for(int w=0;w<2;w++) {
                if(!marking){lastWheel[w]=null;continue;}
                var at=car.transform.position-f*1.25f+r*(w==0?-.72f:.72f);at.y=car.transform.position.y+car.ContactBottom;
                if(lastWheel[w] is Vector3 from){if((at-from).sqrMagnitude>.09f){SkidMarks.Lay(from,at,car.transform.up,offRoad?.55f:Mathf.InverseLerp(3.2f,8,slide)*.8f+.2f,offRoad?SkidMarks.Rut:SkidMarks.Rubber);lastWheel[w]=at;}}
                else lastWheel[w]=at;
            }
            timer-=dt;if((!offRoad&&!skid)||timer>0)return;
            // Low, small puffs at each rear wheel: denser with speed (off road) or with slide (tyre smoke).
            float amount=offRoad?Mathf.InverseLerp(2.5f,20,speed):Mathf.InverseLerp(3.2f,9,slide);timer=Mathf.Lerp(.06f,.022f,amount);
            float y=car.transform.position.y+car.ContactBottom+.12f;
            foreach(int side in new[]{-1,1}) {
                var at=car.transform.position-f*1.25f+r*side*.72f;at.y=y;at+=Random.insideUnitSphere*.08f;
                if(offRoad) {
                    var colour=Color.Lerp(Grass,Dirt,Mathf.PerlinNoise(at.x*.3f,at.z*.3f));
                    Emit(at,-v*.18f+Vector3.up*Random.Range(.25f,.6f)+r*side*Random.Range(.1f,.5f),Random.Range(.35f,.6f)+amount*.35f,Random.Range(.6f,1.1f),Mathf.Lerp(.35f,.6f,amount),.55f,colour);
                } else Emit(at,-v*.1f+Vector3.up*Random.Range(.3f,.7f),Random.Range(.4f,.65f),Random.Range(.7f,1.2f),Mathf.Lerp(.2f,.45f,amount),.75f,Smoke);
            }
        }
    }
    // Lazy wood smoke from a handful of chimneys (only those near the player).
    public sealed class ChimneySmoke:PuffPool
    {
        List<Vector3> tops;Transform near;float[] timers;
        public void Setup(List<Vector3> chimneys,Transform player){tops=new List<Vector3>(chimneys);near=player;timers=new float[tops.Count];material=TrialWorld.Material("Chimney smoke",new Color(.9f,.9f,.92f),0,.05f);}
        void Update()
        {
            float dt=Time.deltaTime;Step(dt);var cam=Camera.main!=null?Camera.main.transform.position:Vector3.zero;
            for(int i=0;i<tops.Count;i++) {
                if((tops[i]-cam).sqrMagnitude>320*320)continue;timers[i]-=dt;if(timers[i]>0)continue;timers[i]=Random.Range(.7f,1.2f);
                Emit(tops[i],new Vector3(.35f,.9f,.2f)+Random.insideUnitSphere*.15f,Random.Range(.9f,1.4f),Random.Range(3.2f,4.4f));
            }
        }
    }
}

namespace LittleRoad
{
    // Full-screen fade for journeys (the train ride).
    public sealed class Fade:MonoBehaviour
    {
        UnityEngine.UI.Image image;
        public static Fade Get(Transform host)
        {
            var existing=host.GetComponentInChildren<Fade>();if(existing!=null)return existing;
            var canvas=CoastUI.Canvas(host,"Journey fade",90);canvas.gameObject.layer=Toy.MenuLayer;var f=canvas.gameObject.AddComponent<Fade>();
            var go=new GameObject("Fade",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            f.image=go.GetComponent<UnityEngine.UI.Image>();f.image.color=new Color(.11f,.17f,.39f,0);f.image.raycastTarget=false;return f;
        }
        public System.Collections.IEnumerator To(float alpha,float seconds)
        {
            float start=image.color.a;for(float t=0;t<seconds;t+=Time.unscaledDeltaTime){var c=image.color;c.a=Mathf.Lerp(start,alpha,t/seconds);image.color=c;yield return null;}
            var e=image.color;e.a=alpha;image.color=e;
        }
    }
}

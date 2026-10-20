using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Bramblewick Lanes on race day (docs/82): what makes the lanes read as a circuit and frame its corners.
    //   - The start: a chequered line across the village street, grid boxes for the 24 slots, and a gantry with its banner.
    //   - Kerbs on the inside of every corner tighter than 75 m: red and white, low enough to use.
    //   - Hay-bale walls on the outside of the four heavy stops (the Dairy T, Barn Corner, the Green Hairpin, Mill Bend).
    //   - Hedge banks along the lane, a stone-faced bank with the hedge on top, broken for field gateways, the brook,
    //     the bridge, the bale walls and the spectator banks.
    //   - Spectators: a grandstand and parked cars on the Green Hairpin's banks, more at the Dairy T and by the bridge.
    //   - A board at each corner with its name, and the brook's water running along its channel.
    public sealed partial class BramblewickVillage
    {
        public static int Banks,BankRefused,Kerbs,Bales,Boards;
        Material Mat(string name,Color c)=>TrialWorld.Material(name,c,0,.3f);
        GameObject Block(string name,Vector3 at,Vector3 size,Quaternion rotation,Material m,bool solid)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.SetPositionAndRotation(at,rotation);g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=m;if(!solid)Object.Destroy(g.GetComponent<Collider>());else g.tag="Respawn";return g;
        }
        void Words(string words,Vector3 at,Quaternion rotation,float size,Color? colour=null)
        {
            var go=new GameObject("Sign / "+words);go.transform.SetParent(root);go.transform.SetPositionAndRotation(at,rotation);
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=size;text.color=colour??Toy.Hex(0x1d2b3a);
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
        }
        // A spectator's car, parked on the bank facing the track (named as the valley's parked cars are, so the audit counts it;
        // placed here rather than through ValleyLife so it's there on race day too).
        static readonly string[] SpectatorBodies={"marlow","heron","picnic","doodle","otter","bramble","pebble","tallboy","dover","kipper","beetle","camper"};
        void SpectatorCar(Vector3 p,float yaw)
        {
            var go=TrialWorld.Car(SpectatorBodies[dressRng.Next(SpectatorBodies.Length)],PaintCatalog.All[dressRng.Next(PaintCatalog.Standard)].color);go.name="Parked car";go.transform.SetParent(root);
            go.transform.SetPositionAndRotation(p+Vector3.up*.02f,Quaternion.Euler(0,yaw,0));
        }
        void RaceDay()
        {
            if(Bramblewick.Lap==null)return;
            Banks=BankRefused=Kerbs=Bales=Boards=0;
            var lane=HillcrestRoute.Find(Bramblewick.LaneName);var street=HillcrestRoute.Find(Bramblewick.RoadName);
            var track=new Circuit(Bramblewick.Lap,Bramblewick.LaneWidth);float half=Bramblewick.LaneWidth*.5f;
            var junctions=new[]{street.At(Bramblewick.LaneW),street.At(Bramblewick.LaneE)};
            bool NearJunction(Vector3 p,float d){foreach(var j in junctions)if(Vector2.Distance(new(p.x,p.z),new(j.x,j.z))<d)return true;return false;}
            var white=Mat("Lanes paint",new Color(.96f,.96f,.92f));var red=Mat("Lanes kerb red",new Color(.84f,.2f,.17f));var dark=Mat("Lanes chequer",new Color(.1f,.1f,.12f));

            // ---- the start: chequers, grid boxes, gantry
            {
                var s0=track.At(0);var fw=track.Forward(0);fw.y=0;fw.Normalize();var r0=Vector3.Cross(Vector3.up,fw);
                for(int i=0;i<8;i++)for(int j=0;j<2;j++)Block("Lanes start line",s0+r0*(-3.5f+i)+fw*j*.5f+Vector3.up*.05f,new Vector3(1,.02f,.5f),Quaternion.LookRotation(fw),(i+j)%2==0?white:dark,false);
                for(int slot=0;slot<24;slot++) {
                    float d=RaceGrid.Distance(slot,false);var p=RaceGrid.Position(track,slot,false);var f=track.Forward(d);f.y=0;f.Normalize();
                    // (a painted slot: a front bar and two legs back along the car, as a grid's boxes are, LR1-06)
                    var rr=Vector3.Cross(Vector3.up,f);var front=p+f*2.3f+Vector3.down*.07f;
                    Block("Lanes grid box",front,new Vector3(2.3f,.02f,.16f),Quaternion.LookRotation(f),white,false);
                    foreach(int leg in new[]{-1,1})Block("Lanes grid box",front+rr*leg*1.07f-f*.6f,new Vector3(.16f,.02f,1.2f),Quaternion.LookRotation(f),white,false);
                }
                var blue=Mat("Lanes gantry green",Toy.Hex(0x2f5d46));var banner=Mat("Lanes banner",new Color(.97f,.95f,.88f));   // (cream, the words dark green: on mustard the review couldn't read them, LR1-06)
                foreach(int side in new[]{-1,1})Block("Lanes gantry post",s0+r0*side*6.2f+Vector3.up*3.2f,new Vector3(.45f,6.4f,.45f),Quaternion.identity,blue,true);
                Block("Lanes gantry",s0+Vector3.up*6.2f,new Vector3(12.8f,1.1f,.45f),Quaternion.LookRotation(fw),blue,false);
                Block("Lanes gantry banner",s0+Vector3.up*6.2f-fw*.26f,new Vector3(9,.8f,.06f),Quaternion.LookRotation(fw),banner,false);
                Block("Lanes gantry banner",s0+Vector3.up*6.2f+fw*.26f,new Vector3(9,.8f,.06f),Quaternion.LookRotation(fw),banner,false);
                foreach(int face in new[]{-1,1})Words("BRAMBLEWICK LANES",s0+Vector3.up*6.2f+fw*face*.31f,Quaternion.LookRotation(-fw*face),.085f,Toy.Hex(0x1f3d2c));   // (read from both ways)
                foreach(int side in new[]{-1,1}){var fp=s0+r0*side*8.4f-fw*3;fp.y=H(fp.x,fp.z);Put("flag_pole",fp,0,T(0xffffff,0xffd23f,0x2f5d46,0),null,true);}
                // (bunting strung across the street, zigzag, from 90 m before the line to 60 m after: the village's race day)
                for(float d=-90;d<=60;d+=15){var a=track.At(d);var r1=track.Right(d);r1.y=0;r1.Normalize();var b=track.At(d+15);var r2=track.Right(d+15);r2.y=0;r2.Normalize();
                    int z=Mathf.RoundToInt(d/15)%2==0?1:-1;var pa=a+r1*z*(half+1.2f);var pb=b-r2*z*(half+1.2f);
                    // (each end on a pole: the audit's loose bunting ends)
                    if(d==-90){var g0=pa;g0.y=H(g0.x,g0.z);Put("flag_pole",g0,0,T(0xffffff,0xffd23f,0x2f5d46,0),null,true);}
                    var g1=pb;g1.y=H(g1.x,g1.z);Put("flag_pole",g1,0,T(0xffffff,0xffd23f,0x2f5d46,0),null,true);
                    pa.y=H(pa.x,pa.z)+5.2f;pb.y=H(pb.x,pb.z)+5.2f;HillcrestTown.BuntingLine(root,pa,pb,1.1f);}
            }

            // ---- kerbs on the inside of every corner tighter than 75 m (not in the junctions, which have their own kerbs)
            {
                float L=track.length;int block=0;
                for(float s=0;s<L;s+=3.6f) {
                    var a=track.At(s);if(NearJunction(a,26))continue;
                    var f0=track.Forward(s-10);var f1=track.Forward(s+10);f0.y=f1.y=0;float turn=Vector3.SignedAngle(f0,f1,Vector3.up);   // (+ right)
                    float radius=20/Mathf.Max(.001f,Mathf.Abs(turn)*Mathf.Deg2Rad);if(radius>75)continue;
                    var f=track.Forward(s);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);int inside=turn>0?1:-1;   // (the inside of a right-hander is on the right)
                    // (painted kerbs, nearly flush and without a collider: as 10 cm boxes their square sides stopped cars dead at
                    //  20 m/s and piled the field up behind, study 235)
                    var k=a+right*inside*(half+.35f);k.y=Mathf.Max(a.y,H(k.x,k.z))+.035f;
                    Block("Lanes kerb",k,new Vector3(.7f,.04f,3.4f),Quaternion.LookRotation(f),block++%2==0?red:white,false);Kerbs++;
                }
            }

            // ---- hay-bale walls on the outside of the heavy stops, and the spectators' banks behind them
            var bales=new List<(Vector3 at,float radius)>();
            void BaleWall(string mark,float back,float ahead)
            {
                Vector3 c;if(mark=="Dairy T")c=junctions[1];else if(Bramblewick.Marks.TryGetValue(mark,out var m2))c=new Vector3(m2.x,0,m2.y);else return;
                float sc=track.Nearest(c,out _);
                for(float s=sc-back;s<sc+ahead;s+=1.5f) {
                    var f0=track.Forward(s-8);var f1=track.Forward(s+8);f0.y=f1.y=0;float turn=Vector3.SignedAngle(f0,f1,Vector3.up);if(Mathf.Abs(turn)<4)continue;
                    var f=track.Forward(s);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);int outside=turn>0?-1:1;
                    var p=track.At(s)+right*outside*(half+6.2f);if(NearJunction(p,12)||ValleyField.RoadDistance(p.x,p.z)<half+5)continue;p.y=H(p.x,p.z);
                    if(BayviewKit.Has("hay_bale")){Put("hay_bale",p,Quaternion.LookRotation(f).eulerAngles.y,null,null,true,1.1f);Bales++;}
                    bales.Add((p,9));
                }
            }
            BaleWall("Dairy T",40,25);BaleWall("Barn Corner",15,50);BaleWall("Green Hairpin",10,80);BaleWall("Mill Bend",30,70);BaleWall("Cider Press",10,60);

            // ---- spectators: a grandstand and parked cars on the Green Hairpin's outside bank, cars at the Dairy T and the bridge
            var stands=new List<(Vector3 at,float radius)>();
            void Spectators(string mark,int cars,bool grandstand)
            {
                Vector3 c;if(mark=="Dairy T")c=junctions[1];else if(Bramblewick.Marks.TryGetValue(mark,out var m2))c=new Vector3(m2.x,0,m2.y);else return;
                float sc=track.Nearest(c,out _);var f=track.Forward(sc);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);
                var f0=track.Forward(sc-8);var f1=track.Forward(sc+8);f0.y=f1.y=0;int outside=Vector3.SignedAngle(f0,f1,Vector3.up)>0?-1:1;
                var bank=track.At(sc)+right*outside*(half+18);bank.y=H(bank.x,bank.z);stands.Add((bank,22));
                if(grandstand&&BayviewKit.Has("grandstand"))Put("grandstand",bank,Yaw(bank,track.At(sc)),null,null,true);
                for(int k=0;k<cars;k++){var p=bank+right*outside*(grandstand?11:0)+f*((k-(cars-1)*.5f)*3.4f);if(ValleyField.RoadDistance(p.x,p.z)<half+6)continue;p.y=H(p.x,p.z);SpectatorCar(p,Yaw(p,track.At(sc)));}
                foreach(float x in new[]{-9f,9f}){var fp=bank+f*x;if(ValleyField.RoadDistance(fp.x,fp.z)<half+4)continue;fp.y=H(fp.x,fp.z);Put("flag_pole",fp,0,T(0xffffff,0xffd23f,0x2f5d46,0),null,true);}
            }
            Spectators("Green Apex",8,true);Spectators("Dairy T",5,false);Spectators("Humpback Bridge",4,false);Spectators("Beacon Apex",5,false);
            // (Ted the marshal stands on the Beacon's bank, by his flag)
            if(Bramblewick.Marks.TryGetValue("Beacon Apex",out var be)){var bp=new Vector3(be.x,0,be.y);float sb=track.Nearest(bp,out _);var fb=track.Forward(sb);fb.y=0;fb.Normalize();var rb=Vector3.Cross(Vector3.up,fb);
                var f0=track.Forward(sb-8);var f1=track.Forward(sb+8);f0.y=f1.y=0;int outside=Vector3.SignedAngle(f0,f1,Vector3.up)>0?-1:1;var ted=track.At(sb)+rb*outside*(half+9)-fb*14;ted.y=H(ted.x,ted.z);Spots["marshal"]=(ted,Yaw(ted,track.At(sb)));}

            // ---- hedge banks along the lane, both sides, 3.4 m beyond its edge
            {
                float L=lane.Length;var gates=new List<float>();for(float g=110;g<L-60;g+=150+(float)dressRng.NextDouble()*60)gates.Add(g);
                bool Gap(float s,Vector3 p)
                {
                    if(s<28||s>L-28||NearJunction(p,30))return true;
                    foreach(float g in gates)if(Mathf.Abs(s-g)<4)return true;
                    if(Vector3.Distance(p,Bramblewick.BridgeAt)<18)return true;
                    if(Bramblewick.BrookCut(p.x,p.z)>.3f)return true;
                    foreach(var (at,r) in bales)if(Vector2.Distance(new(p.x,p.z),new(at.x,at.z))<r)return true;
                    foreach(var (at,r) in stands)if(Vector2.Distance(new(p.x,p.z),new(at.x,at.z))<r)return true;
                    // (another lane alongside: where two parts of the circuit run close, no bank between them)
                    return ValleyField.RoadDistance(p.x,p.z)<half+2.2f;
                }
                // (the vista: along Top Lane and over the Beacon the vale's side has a low hedge, not a bank, so the view opens out)
                float Mark(string m)=>Bramblewick.Marks.TryGetValue(m,out var w)?lane.Nearest(new Vector3(w.x,0,w.y),out _):-1;
                float vista0=Mark("Rim Corner"),vista1=Mark("Gallows");var centre=new Vector3(Bramblewick.Centre.x,0,Bramblewick.Centre.y);
                foreach(int side in new[]{-1,1}) {
                    var run=new List<Vector3>();bool low=false;int gapRun=0;
                    void Lay()
                    {
                        if(run.Count>=4&&low){var R=Runs.Solve(new RunSpec{name=$"Lanes hedge {Banks}",kit="hedge",seed=$"lanes-hedge-{side}-{Banks}",along=run.ToArray(),solid=false});if(R.items.Count>0){Runs.Draw(R,root);Runs.Laid.Add(R);Banks++;}}
                        else if(run.Count>=4){var R=Runs.Solve(new RunSpec{name=$"Lanes hedge bank {Banks}",kit="hedge_bank",seed=$"lanes-bank-{side}-{Banks}",along=run.ToArray()});
                            if(R.items.Count>0&&R.failures.Count==0){Runs.Draw(R,root);Runs.Laid.Add(R);Banks++;}else BankRefused++;}
                        run.Clear();
                    }
                    for(float s=0;s<=L;s+=2) {
                        var right=lane.Right(s);right.y=0;right.Normalize();var p=lane.At(s)+right*side*(half+3.4f);
                        bool vista=s>vista0&&s<vista1&&Vector3.Dot(right*side,centre-lane.At(s))>0;
                        // (the inside of a corner tighter than 90 m has a low hedge, so the exit can be seen: LR1-05)
                        var g0=lane.Forward(s-12);var g1=lane.Forward(s+12);g0.y=g1.y=0;float bend=Vector3.SignedAngle(g0,g1,Vector3.up);
                        bool insideTight=24/Mathf.Max(.001f,Mathf.Abs(bend)*Mathf.Deg2Rad)<90&&(bend>0?1:-1)==side;
                        bool lowHere=vista||insideTight;if(lowHere!=low){Lay();low=lowHere;}
                        // (a gap shorter than three posts doesn't break the run: two runs a metre apart left doubled stubs, LR1-04)
                        if(Gap(s,p)){gapRun++;if(gapRun==3)Lay();continue;}gapRun=0;p.y=H(p.x,p.z);run.Add(p);
                        // (now and then an oak grows out of the bank)
                        if(!low&&dressRng.NextDouble()<.012){var oak=p+right*side*.4f;oak.y=H(oak.x,oak.z);Put("tree_round",oak,(float)dressRng.NextDouble()*360,null,null,true,1.1f+(float)dressRng.NextDouble()*.35f);}
                    }
                    Lay();
                }
            }

            // ---- a board at each corner, 25 m before it on the outside, with the corner's name
            foreach(var name in new[]{"Brook Esses","Barn Corner","Rookery","Hedge Gap","Rim Corner","Beacon","Gallows","Church Hill","Green Hairpin","Orchard","Cider Press","Long Meadow","Mill Bend","Humpback Bridge"}) {
                if(!Bramblewick.Marks.TryGetValue(name,out var m2))continue;float sc=track.Nearest(new Vector3(m2.x,0,m2.y),out _)-40;
                var f=track.Forward(sc);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);var f1=track.Forward(sc+35);f1.y=0;int outside=Vector3.SignedAngle(f,f1,Vector3.up)>0?-1:1;
                var p=track.At(sc)+right*outside*(half+2.4f);if(ValleyField.RoadDistance(p.x,p.z)<half+1.6f)continue;p.y=H(p.x,p.z);
                if(Put("map_board",p,Yaw(p,track.At(sc-40)),null,name.ToUpperInvariant())!=null)Boards++;   // (facing the driver coming up to it: the review saw one from behind, LR1-03)
            }
            // ---- 100 and 50 boards before the heavy stops (LR1-07): white boards on posts, black numbers, on the outside
            var postMat=Mat("Board post",new Color(.3f,.3f,.32f));
            foreach(var stop in new[]{"Dairy T","Barn Corner","Green Hairpin","Mill Bend","Cider Press"}) {
                Vector3 c;if(stop=="Dairy T")c=junctions[1];else if(Bramblewick.Marks.TryGetValue(stop,out var m3))c=new Vector3(m3.x,0,m3.y);else continue;
                float sc=track.Nearest(c,out _);var f1=track.Forward(sc+20);var f0=track.Forward(sc-5);f0.y=f1.y=0;int outside=Vector3.SignedAngle(f0,f1,Vector3.up)>0?-1:1;
                foreach(int dist in new[]{100,50}) {
                    float s=sc-dist;var f=track.Forward(s);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);var p=track.At(s)+right*outside*(half+4.2f);   // (beyond the run-off: at 1.8 m out their posts were in it)
                    if(ValleyField.RoadDistance(p.x,p.z)<half+3.4f)continue;p.y=H(p.x,p.z);var face=Quaternion.LookRotation(-f);
                    Block("Distance board post",p+Vector3.up*.6f,new Vector3(.12f,1.2f,.12f),Quaternion.identity,postMat,true);
                    Block("Distance board",p+Vector3.up*1.45f,new Vector3(.9f,.6f,.06f),face,white,false);
                    Words(dist.ToString(),p+Vector3.up*1.45f-f*.04f,face,.05f,new Color(.08f,.08f,.1f));Boards++;
                }
            }

            // ---- a landmark for each corner it's named after, on the outside, clear of the lane (one per section, so the lap
            //      is learnt by what you see, and the hedge corridor is broken up: LR1 "monotony")
            {
                Vector3? Outside(string mark,float ahead,float off)
                {
                    if(!Bramblewick.Marks.TryGetValue(mark,out var m4))return null;float sc=track.Nearest(new Vector3(m4.x,0,m4.y),out _)+ahead;
                    var f=track.Forward(sc);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);var f0=track.Forward(sc-10);var f1=track.Forward(sc+10);f0.y=f1.y=0;
                    int outside=Vector3.SignedAngle(f0,f1,Vector3.up)>0?-1:1;var p=track.At(sc)+right*outside*(half+off);
                    if(ValleyField.RoadDistance(p.x,p.z)<half+off*.7f)return null;p.y=H(p.x,p.z);return p;
                }
                var stone=Mat("Beacon stone",new Color(.62f,.6f,.54f));var iron=Mat("Beacon iron",new Color(.16f,.15f,.15f));var flame=Mat("Beacon flame",new Color(1f,.55f,.12f));
                if(Outside("Barn Corner",20,24) is Vector3 barnAt&&BayviewKit.Has("barn"))Put("barn",barnAt,Yaw(barnAt,track.At(track.Nearest(barnAt,out _))),T(0x8a3b2e,0x6b4a2f,0x6b4a2f,0x2f5d46),null,true);
                if(Outside("Rookery",30,22) is Vector3 rk)for(int k=0;k<7;k++){var q=rk+new Vector3(((float)dressRng.NextDouble()-.5f)*16,0,((float)dressRng.NextDouble()-.5f)*16);if(ValleyField.RoadDistance(q.x,q.z)<half+8)continue;q.y=H(q.x,q.z)-.05f;Put(k%3==0?"tree_pine":"tree_round",q,(float)dressRng.NextDouble()*360,null,null,true,1.5f+(float)dressRng.NextDouble()*.4f);}
                if(Outside("Beacon Apex",0,14) is Vector3 bk) {   // (the beacon: a stone tower with an iron fire basket, alight)
                    var t=GameObject.CreatePrimitive(PrimitiveType.Cylinder);t.name="Beacon tower";t.transform.SetParent(root);t.transform.position=bk+Vector3.up*2.4f;t.transform.localScale=new Vector3(2.2f,2.4f,2.2f);t.GetComponent<Renderer>().sharedMaterial=stone;t.tag="Respawn";
                    var basket=GameObject.CreatePrimitive(PrimitiveType.Cylinder);basket.name="Beacon basket";Object.Destroy(basket.GetComponent<Collider>());basket.transform.SetParent(root);basket.transform.position=bk+Vector3.up*5.1f;basket.transform.localScale=new Vector3(1.6f,.3f,1.6f);basket.GetComponent<Renderer>().sharedMaterial=iron;
                    var fire=GameObject.CreatePrimitive(PrimitiveType.Sphere);fire.name="Beacon fire";Object.Destroy(fire.GetComponent<Collider>());fire.transform.SetParent(root);fire.transform.position=bk+Vector3.up*5.7f;fire.transform.localScale=new Vector3(1.2f,1.1f,1.2f);fire.GetComponent<Renderer>().sharedMaterial=flame;
                    var light=new GameObject("Beacon light").AddComponent<Light>();light.transform.SetParent(root);light.transform.position=bk+Vector3.up*6.5f;light.type=LightType.Point;light.color=new Color(1,.6f,.25f);light.range=14;light.intensity=2;
                }
                if(Outside("Gallows",-10,6) is Vector3 gw) {   // (a fingerpost at the crossroads of the old drove roads)
                    Block("Fingerpost",gw+Vector3.up*1.4f,new Vector3(.16f,2.8f,.16f),Quaternion.identity,white,true);
                    var dir=track.Forward(track.Nearest(gw,out _));dir.y=0;dir.Normalize();
                    foreach(var (word,turn) in new[]{("BRAMBLEWICK",90f),("VINELANDS",-90f),("THE BEACON",180f)}){var arm=Quaternion.Euler(0,turn,0)*dir;Block("Fingerpost arm",gw+Vector3.up*(2.4f-.3f*(turn<0?1:0)-.6f*(turn>100?1:0))+arm*.75f,new Vector3(.12f,.22f,1.4f),Quaternion.LookRotation(arm),white,false);
                        Words(word,gw+Vector3.up*(2.4f-.3f*(turn<0?1:0)-.6f*(turn>100?1:0))+arm*.75f+Quaternion.Euler(0,90,0)*arm*.07f,Quaternion.LookRotation(Quaternion.Euler(0,-90,0)*arm),.018f);}
                }
                if(Outside("Orchard",10,16) is Vector3 oc)for(int a=-2;a<=2;a++)for(int b=0;b<3;b++){var f=track.Forward(track.Nearest(oc,out _));f.y=0;f.Normalize();var q=oc+f*a*6+Vector3.Cross(Vector3.up,f)*b*6*(1);if(ValleyField.RoadDistance(q.x,q.z)<half+9)continue;q.y=H(q.x,q.z)-.05f;Put("tree_blossom",q,(float)dressRng.NextDouble()*360,null,null,true,.65f);}
                if(Outside("Cider Press",-15,20) is Vector3 cp) {   // (the cider press: a small barn with its barrels)
                    if(BayviewKit.Has("barn"))Put("barn",cp,Yaw(cp,track.At(track.Nearest(cp,out _))),T(0xd9c9a3,0x8c2f2a,0x8c2f2a,0x5b4a3a),"CIDER",true,.75f);
                    var barrel=Mat("Cider barrel",new Color(.48f,.32f,.18f));var f=track.Forward(track.Nearest(cp,out _));f.y=0;f.Normalize();
                    for(int k=0;k<5;k++){var b=GameObject.CreatePrimitive(PrimitiveType.Cylinder);b.name="Cider barrel";b.transform.SetParent(root);var q=cp-f*7+Vector3.Cross(Vector3.up,f)*(k-2)*1.1f;q.y=H(q.x,q.z)+.5f;b.transform.position=q;b.transform.localScale=new Vector3(.9f,.5f,.9f);b.GetComponent<Renderer>().sharedMaterial=barrel;b.tag="Respawn";}
                }
                if(Outside("Long Meadow Roll",0,22) is Vector3 lm&&BayviewKit.Has("hay_bale"))for(int k=0;k<9;k++){var q=lm+new Vector3(((float)dressRng.NextDouble()-.5f)*40,0,((float)dressRng.NextDouble()-.5f)*40);if(ValleyField.RoadDistance(q.x,q.z)<half+10)continue;q.y=H(q.x,q.z);Put("hay_bale",q,(float)dressRng.NextDouble()*360,null,null,true,1.2f);}
                if(Outside("Mill Bend",10,18) is Vector3 ml) {   // (the mill on the brook: a cottage and its water wheel)
                    var f=track.Forward(track.Nearest(ml,out _));f.y=0;f.Normalize();
                    if(BayviewKit.Has("cottage_thatch_c"))Put("cottage_thatch_c",ml,Yaw(ml,track.At(track.Nearest(ml,out _))),T(0xf3e6c8,0x2f5d46,0x2f5d46,0x2f5d46),null,true);
                    var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Mill wheel";wheel.transform.SetParent(root);var wq=ml-f*6;wq.y=H(wq.x,wq.z)+2.2f;wheel.transform.SetPositionAndRotation(wq,Quaternion.LookRotation(Vector3.Cross(Vector3.up,f))*Quaternion.Euler(90,0,0));
                    wheel.transform.localScale=new Vector3(4.4f,.4f,4.4f);wheel.GetComponent<Renderer>().sharedMaterial=Mat("Mill wheel wood",new Color(.36f,.24f,.14f));wheel.tag="Respawn";
                }
                if(Bramblewick.Marks.TryGetValue("Hedge Brow",out var hb)){var q=new Vector3(hb.x,0,hb.y);float sc=track.Nearest(q,out _);var right=track.Right(sc);right.y=0;right.Normalize();var g=track.At(sc)+right*(half+3.4f);g.y=H(g.x,g.z);Put("field_gate",g,Yaw(g,g+track.Forward(sc)));}
            }
            // ---- lamps where the lane leaves the village street, either end (the village's own lanes, lit)
            foreach(float s in new[]{14f,42f,lane.Length-14,lane.Length-42}){var right=lane.Right(s);right.y=0;right.Normalize();var lp=lane.At(s)+right*(s<100?1:-1)*(half+1.3f);lp.y=H(lp.x,lp.z);Put("lamp_post",lp,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}

            // ---- the brook's water along its channel (not under the roads: the ford and the bridge have their own)
            if(Bramblewick.Brook!=null) {
                var water=TrialWorld.Material("Brook water",new Color(.34f,.5f,.54f),.1f,.95f);var v=new List<Vector3>();var t=new List<int>();
                void Flush(){if(v.Count>=4){var mesh=new Mesh{name="Brook"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();var go=new GameObject("Brook water",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=water;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}v.Clear();t.Clear();}
                var b=Bramblewick.Brook;float total=0;for(int i=1;i<b.Length;i++)total+=Vector2.Distance(b[i-1],b[i]);float run2=0;
                for(int i=1;i<b.Length;i++) {
                    var a=b[i-1];var ab=b[i]-a;float seg=ab.magnitude;
                    for(float u=0;u<seg;u+=3) {
                        var p=a+ab*(u/seg);float w=Mathf.Lerp(2.4f,1.4f,(run2+u)/total);var dir=ab/seg;var across=new Vector2(-dir.y,dir.x)*w*.5f;
                        if(ValleyField.RoadDistance(p.x,p.y)<half+3){Flush();continue;}
                        float y=H(p.x,p.y)+.08f;int k=v.Count;v.Add(new Vector3(p.x-across.x,y,p.y-across.y));v.Add(new Vector3(p.x+across.x,y,p.y+across.y));
                        if(k>=2)t.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});
                    }
                    run2+=seg;
                }
                Flush();
            }
            Debug.Log($"LITTLE_ROAD_LANES_RACEDAY kerbs={Kerbs} bales={Bales} banks={Banks} banks_refused={BankRefused} boards={Boards} parking={Parking.Count}");
        }
    }
    // Spray off the wheels of anything driving through the ford faster than walking pace: white-blue puffs that rise and fall
    // back. Watches the cars near it (found every half second), not every car on the map.
    public sealed class FordSplash:PuffPool
    {
        Vector3 centre,along;float halfLength,halfWidth,find,timer;readonly List<RoadCar> cars=new();
        public void Setup(Vector3 c,Vector3 forward,float length,float width){centre=c;along=forward;along.y=0;along.Normalize();halfLength=length;halfWidth=width;soft=true;material=SoftMaterial("Ford spray",new Color(.86f,.93f,.96f));transform.SetParent(null);}
        void Update()
        {
            float dt=Time.deltaTime;Step(dt);find-=dt;
            if(find<=0){find=.5f;cars.Clear();foreach(var car in Object.FindObjectsByType<RoadCar>(FindObjectsSortMode.None))if(car!=null&&(car.transform.position-centre).sqrMagnitude<80*80)cars.Add(car);}
            timer-=dt;if(timer>0)return;timer=.04f;var across=Vector3.Cross(Vector3.up,along);
            foreach(var car in cars) {
                if(car==null)continue;var d=car.transform.position-centre;float u=Vector3.Dot(d,along),w=Vector3.Dot(d,across);float speed=Mathf.Abs(car.speed);
                if(Mathf.Abs(u)>halfLength||Mathf.Abs(w)>halfWidth||speed<3)continue;var f=car.transform.forward;var r=car.transform.right;
                foreach(int side in new[]{-1,1})foreach(float axle in new[]{1.2f,-1.2f}) {
                    var at=car.transform.position+f*axle+r*side*.95f;at.y=centre.y+.12f;
                    Emit(at,r*side*Random.Range(.8f,1.6f)*Mathf.Min(1,speed/12)+Vector3.up*Random.Range(1.2f,2.6f)*Mathf.Min(1,speed/10)-car.body.linearVelocity*.15f,Random.Range(.25f,.45f),Random.Range(.5f,.8f),.7f,.2f,new Color(.86f,.93f,.96f));
                }
            }
        }
    }
}

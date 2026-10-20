using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The quarry floor (docs/59): the old pit floor between the town and the galleries, worked flat. The rally school
    // (in the old quarry hospital), the Quarry Sprint rallycross loop with tyre walls, boards, a start gate and a
    // grandstand, and the Blondin: a cableway from the floor up to the top of the big tip. The floor itself is laid
    // over with slate rubble as one smooth ellipse (the 2.5 m terrain grid's cells left a saw-tooth edge).
    public static class QuarryFloor
    {
        // The Quarry Hills Rallycross circuit (Richard, 3 Oct: a class C dirt race here): about 1.5 km of loose slate round the
        // floor, 16 m wide like every race circuit (Circuit.Width), clockwise from the start line. A long start straight, a
        // sweeper east, a hairpin, the chicane past the grandstand, the dive south and the long west loop back. The Quarry
        // Sprint time trial and Gwen's mini-stage run it too.
        static readonly Vector2[] LoopControl={new(-2470,-2935),new(-2300,-2935),new(-2160,-2938),new(-2085,-2965),new(-2058,-3025),new(-2088,-3082),new(-2155,-3118),
            new(-2228,-3098),new(-2262,-3040),new(-2318,-3012),new(-2378,-3048),new(-2420,-3110),new(-2500,-3132),new(-2570,-3095),new(-2600,-3040),new(-2595,-2985),new(-2550,-2952)};
        public const float LoopWidth=Circuit.Width;
        public static Vector3[] Loop=>loop??=MakeLoop();static Vector3[] loop;
        static Vector3[] MakeLoop()
        {
            var c=new Vector3[LoopControl.Length];for(int i=0;i<c.Length;i++)c[i]=new Vector3(LoopControl[i].x,QuarryHills.FloorY,LoopControl[i].y);
            var line=HillcrestRoute.DenseClosed(c,2);for(int i=0;i<line.Length;i++)line[i].y=QuarryHills.FloorY;return line;
        }
        public static readonly Vector3 School=new(-2290,QuarryHills.FloorY,-2878);
        public static readonly Vector2 BlondinFloor=new(-2650,-2985),BlondinTip=new(-2925,-2958);
        // Cars parked outside the school (read by ValleyLife).
        public static readonly List<(Vector3 at,float yaw)> Parking=new();
        // The Blondin's two loading bays (top: the tip; bottom: the floor).
        public static (Vector3 top,Vector3 bottom) Bays;

        public static void Build(Transform parent)
        {
            if(!QuarryHills.Enabled)return;
            var root=new GameObject("Quarry floor").transform;root.SetParent(parent);Parking.Clear();
            var rng=new System.Random(7711);
            Overlay(root);
            // The loop: loose slate gravel (no tarmac grip: the floor's own dirt grip under it).
            // (4 Oct: a flat cream strip before.) Loose slate gravel: fine chips, two polished wheel ruts, stones on the berms
            // (Kit shader, slate flats with _Wear 3.2; the strip's second UV carries the across-coordinate).
            var gravel=WorldLook.Kit("Rallycross gravel",new Color(.39f,.39f,.41f),WorldLook.Pattern.Slate,0,.1f,true,3.2f);
            Strip("Quarry Sprint",Loop,LoopWidth,.07f,gravel,root,true);
            RallySchool.BuildCrest(root,gravel);
            int n=Loop.Length;
            // Tyre walls on the outside of the bends, boards along the straights.
            string[] brands={"GRIT & GRAVEL","CABAN","SLATE QUAY","PINE & PEAK","BARA BRITH","DAI'S DUMPERS","GLAS WATER","YSGOL RALI"};int brand=0;float since=99;
            // (--prop-forge: a bend's outside as one tyre barrier run, docs/80 round 7: TyreBarriers)
            var corner=new List<Vector3>();var cornerYaw=new List<float>();int cornerSide=0;
            void FlushCorner(){if(corner.Count>0&&!TyreBarriers.Lay(corner,root,"Quarry Sprint tyres"))for(int k=0;k<corner.Count;k++)Put("tyre_wall",corner[k],cornerYaw[k],null,null,root);corner.Clear();cornerYaw.Clear();}
            for(int i=0;i<n;i+=3) {
                Vector3 p=Loop[i],back=Loop[(i+n-8)%n],ahead=Loop[(i+8)%n];Vector3 tan=ahead-back;tan.y=0;tan.Normalize();var r=Vector3.Cross(Vector3.up,tan);
                float turn=Vector3.SignedAngle(new Vector3(p.x-back.x,0,p.z-back.z),new Vector3(ahead.x-p.x,0,ahead.z-p.z),Vector3.up);since+=6;if(PropForge.Enabled&&Mathf.Abs(turn)<=9)FlushCorner();
                if(Mathf.Abs(turn)>9){int outside=turn>0?-1:1;var q=p+r*outside*(LoopWidth*.5f+3.4f);if(Clear(q,LoopWidth*.5f+2.8f)){q.y=QuarryHills.FloorY-.02f;if(PropForge.Enabled){if(outside!=cornerSide)FlushCorner();cornerSide=outside;corner.Add(q);cornerYaw.Add(Yaw(q,q-r*outside));}else Put("tyre_wall",q,Yaw(q,q-r*outside),null,null,root);}}
                else if(since>48){since=0;int side=brand%2==0?1:-1;var q=p+r*side*(LoopWidth*.5f+4.5f);if(Clear(q,LoopWidth*.5f+4)){q.y=QuarryHills.FloorY-.02f;
                    Put("ad_board",q,Yaw(q,q-r*side),T(new[]{0xf4f1ea,0xf1c40f,0xc9d3e8,0xe5bfa8}[brand%4],new[]{0xc0392b,0x229954,0x1f3a93,0x2e86c1}[brand%4],0,0),brands[brand%brands.Length],root);brand++;}}
            }
            FlushCorner();
            // Start gate on the north straight, the grandstand facing it from the infield, flags along the start.
            int s0=HillcrestRoute.Index(Loop,new Vector3(-2335,0,-2935));var f=Loop[(s0+2)%n]-Loop[(s0+n-2)%n];f.y=0;f.Normalize();
            // (No gate over the circuit: a 16 m race track is wider than the gate, and its piers stopped the field.) Painted lines
            // across the track instead, chequered at the race start (index 0) and white at the Sprint's line.
            var paint=TrialWorld.Material("Start line paint",new Color(.95f,.95f,.93f),0,.3f);var dark=TrialWorld.Material("Start line check",new Color(.08f,.08f,.09f),0,.3f);
            void Across(int idx,bool chequer){var c=Loop[idx];var d=Loop[(idx+1)%Loop.Length]-Loop[(idx+Loop.Length-1)%Loop.Length];d.y=0;var side=Vector3.Cross(Vector3.up,d.normalized);
                for(int k=0;k<16;k++){var p=c+side*(k-7.5f);p.y=ValleyField.Height(p.x,p.z)+.09f;var g=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(g.GetComponent<Collider>());g.name="Start line";g.transform.SetParent(root);
                    g.transform.SetPositionAndRotation(p,Quaternion.LookRotation(d));g.transform.localScale=new Vector3(1,.02f,chequer?1.2f:.6f);g.GetComponent<Renderer>().sharedMaterial=chequer&&k%2==1?dark:paint;}}
            Across(0,true);Across(s0,false);
            if(BayviewKit.Has("ad_board")){var side0=Vector3.Cross(Vector3.up,(Loop[1]-Loop[0]).normalized);var b=Loop[0]+side0*(LoopWidth*.5f+5);b.y=QuarryHills.FloorY-.02f;Put("ad_board",b,Quaternion.LookRotation(-side0).eulerAngles.y,T(0xffffff,0xc0392b,0,0),"START · FINISH",root);}
            var stand=new Vector3(-2405,QuarryHills.FloorY-.05f,-2990);Put("grandstand",stand,Yaw(stand,Loop[s0]),T(0x8c9095,0xc0392b,0xf1c40f,0x229954),"YSGOL RALI",root);
            foreach(float d in new[]{-24f,-12f,12f,24f}){var q=Loop[s0]+f*d+Vector3.Cross(Vector3.up,f)*(LoopWidth*.5f+2.2f);q.y=QuarryHills.FloorY;Put("flag_pole",q,0,T(0xffffff,0x229954,0xc0392b,0),null,root);}
            // The rally school: the old quarry hospital, long and low, its front to the loop; cars outside.
            var school=T(0x969089,0xf4f1ea,0xc0392b,0x229954);school.roof=Toy.Hex(0x4b5158);Put("station",School+Vector3.down*.04f,180,school,"RALLY SCHOOL",root);
            QuarryHillsTown.Spots["rallyschool"]=(School+new Vector3(-6,0,-11),90);
            for(int k=0;k<5;k++)Parking.Add((School+new Vector3(14+k*4.2f,0,-12),0));
            foreach(float x in new[]{-16f,16f})Put("lamp_post",School+new Vector3(x,-.02f,-7),0,T(0xffffff,0x1f3a93,0xffffff,0xffffff),null,root);
            Put("map_board",School+new Vector3(-14,-.02f,-12),180,null,null,root);
            Blondin(root);
            QuarryWorks.Build(root);
        }
        static bool Clear(Vector3 q,float fromLoop)
        {
            if(HillcrestRoute.Distance(Loop,q.x,q.z,out _)<fromLoop)return false;
            return ValleyField.RoadDistance(q.x,q.z)>8;
        }
        static float Yaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return Quaternion.LookRotation(d).eulerAngles.y;}
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door){return new BayviewKit.Tint(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));}
        static GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint,string sign,Transform root)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,root,tint,sign,true,1):null;
        // The floor's slate: a smooth elliptical sheet over the floor (to 1.08 of its radii) on the ground.
        static void Overlay(Transform root)
        {
            const int rings=64,around=256;var v=new List<Vector3>();var nrm=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            var c=QuarryHills.FloorCentre;
            for(int k=0;k<=rings;k++)for(int j=0;j<around;j++) {
                float a=j*Mathf.PI*2/around,rr=1.08f*k/rings;
                // A ragged outer edge, in bays and spurs at two scales (4 Oct: a smooth line read as paint on the grass).
                if(k==rings)rr+=(Mathf.PerlinNoise(j*.21f,4.2f)-.5f)*.09f+(Mathf.PerlinNoise(j*.9f,1.3f)-.5f)*.04f;
                else if(k==rings-1)rr+=(Mathf.PerlinNoise(j*.21f,4.2f)-.5f)*.04f;
                float x=c.x+Mathf.Cos(a)*QuarryHills.FloorRX*rr,z=c.y+Mathf.Sin(a)*QuarryHills.FloorRZ*rr;
                v.Add(new Vector3(x,ValleyField.Height(x,z)+.035f,z));nrm.Add(ValleyField.Normal(x,z));uv.Add(new Vector2(x,z)*.2f);
            }
            for(int k=0;k<rings;k++)for(int j=0;j<around;j++){int a=k*around+j,b=k*around+(j+1)%around;t.AddRange(new[]{a,a+around,b,b,a+around,b+around});}
            var mesh=new Mesh{name="Quarry floor slate",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetNormals(nrm);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            // Winding: check it faces up (flip if not), so it's never culled.
            int q0=around*6*2;var n0=Vector3.Cross(v[t[q0+1]]-v[t[q0]],v[t[q0+2]]-v[t[q0]]);if(n0.y<0){for(int i=0;i<t.Count;i+=3)(t[i+1],t[i+2])=(t[i+2],t[i+1]);mesh.SetTriangles(t,0);}
            var go=new GameObject("Quarry floor slate",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=CoastalLandscape.SlateMaterial;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Strip(string name,Vector3[] line,float width,float lift,Material material,Transform root,bool closed)
        {
            int n=line.Length,count=closed?n+1:n;var v=new Vector3[count*2];var uv=new Vector2[count*2];var across=new Vector2[count*2];var t=new List<int>();float run=0;
            for(int k=0;k<count;k++) {
                int i=k%n;Vector3 f=line[(i+1)%n]-line[(i+n-1)%n];f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);if(k>0)run+=Vector3.Distance(line[i],line[(k-1)%n]);
                Vector3 a=line[i]-right*width*.5f,b=line[i]+right*width*.5f;a.y=ValleyField.Height(a.x,a.z)+lift;b.y=ValleyField.Height(b.x,b.z)+lift;
                v[k*2]=a;v[k*2+1]=b;uv[k*2]=new(0,run/5);uv[k*2+1]=new(1,run/5);across[k*2]=new(0,run);across[k*2+1]=new(1,run);if(k<count-1){int q=k*2;t.AddRange(new[]{q,q+2,q+1,q+1,q+2,q+3});}
            }
            var mesh=new Mesh{name=name,vertices=v,uv=uv};mesh.SetUVs(1,across);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        // ---------------------------------------------------------------- the Blondin
        // Two riveted lattice towers (rust-red iron), the main and hauling ropes between them in a catenary, and the
        // carriage hanging part-way across with a wagon of slate on its hook.
        static void Blondin(Transform root)
        {
            var iron=TrialWorld.Material("Blondin iron",Toy.Hex(0x8c3b2e),.2f,.35f);var rope=TrialWorld.Material("Blondin rope",new Color(.16f,.16f,.17f),.6f,.5f);
            var beams=new List<Matrix4x4>();var ropes=new List<Matrix4x4>();
            void Beam(List<Matrix4x4> list,Vector3 a,Vector3 b,float thick){var d=b-a;if(d.sqrMagnitude<1e-4f)return;list.Add(Matrix4x4.TRS((a+b)*.5f,Quaternion.LookRotation(d),new Vector3(thick,thick,d.magnitude)));}
            Vector3 Tower(Vector2 at,float height,Vector3 towards)
            {
                var b=new Vector3(at.x,ValleyField.Height(at.x,at.y)-.3f,at.y);var fwd=towards-b;fwd.y=0;fwd.Normalize();var side=Vector3.Cross(Vector3.up,fwd);
                Vector3 Corner(int i,float h){float s=Mathf.Lerp(3.2f,1.1f,h/height);return b+Vector3.up*h+fwd*(i<2?s:-s)+side*(i%2==0?s:-s);}
                for(int i=0;i<4;i++)Beam(beams,Corner(i,0),Corner(i,height),.45f);
                int[] ring={0,1,3,2};
                for(float h=0;h<height-.1f;h+=5) {
                    float h1=Mathf.Min(height,h+5);
                    for(int k=0;k<4;k++){int i=ring[k],j=ring[(k+1)%4];Beam(beams,Corner(i,h),Corner(j,h1),.22f);Beam(beams,Corner(j,h),Corner(i,h1),.22f);Beam(beams,Corner(i,h1),Corner(j,h1),.26f);}
                }
                // The head: a cross-arm along the ropes with the sheaves at its end.
                var head=b+Vector3.up*(height+.6f);Beam(beams,head-fwd*2.5f,head+fwd*2.5f,.6f);Beam(beams,head-side*1.6f,head+side*1.6f,.5f);
                return head+fwd*2.5f;
            }
            var floorAt=new Vector3(BlondinFloor.x,0,BlondinFloor.y);var tipAt=new Vector3(BlondinTip.x,0,BlondinTip.y);
            var lo=Tower(BlondinFloor,34,tipAt);var hi=Tower(BlondinTip,28,floorAt);
            // Ropes: 40 segments of a catenary-ish sag (2.2% of the span at mid-span), main and hauling ropes.
            float span=Vector2.Distance(BlondinFloor,BlondinTip),sag=span*.022f;var across=Vector3.Cross(Vector3.up,(hi-lo).normalized).normalized;
            Vector3 On(float u,float offset)=>Vector3.Lerp(lo,hi,u)+Vector3.down*sag*4*u*(1-u)+across*offset;
            foreach(float offset in new[]{-.5f,.5f})for(int i=0;i<40;i++)Beam(ropes,On(i/40f,offset),On((i+1)/40f,offset),.09f);
            // The ride (docs/59; Richard, 3 Oct: the Blondin is the car lift): the carriage runs on the main rope between bays on
            // the ground under the rope, 14 m in from each tower: the tip's top and the quarry floor.
            var ropePts=new List<Vector3>();const int segs=60;for(int i=0;i<=segs;i++)ropePts.Add(On(1-i/(float)segs,0));   // tip end first: "top"
            float ropeLen=0;for(int i=1;i<ropePts.Count;i++)ropeLen+=Vector3.Distance(ropePts[i],ropePts[i-1]);
            List<Vector3> Cut(float from,float to){var pts=new List<Vector3>();float run=0;for(int i=0;i<ropePts.Count;i++){if(i>0)run+=Vector3.Distance(ropePts[i],ropePts[i-1]);if(run>=from&&run<=to)pts.Add(ropePts[i]);}return pts;}
            var ride=Cut(14,ropeLen-14);
            Vector3 Bay(Vector3 above){var g=above;g.y=ValleyField.Height(g.x,g.z)+.02f;return g;}
            var dirDown=(lo-hi);dirDown.y=0;float yawDown=Quaternion.LookRotation(dirDown).eulerAngles.y;
            var topStation=new ZipLine.Station{bay=Bay(ride[0]),cable=hi,facing=yawDown,name="Blondin tip bay",ride="Ride the Blondin down to the quarry floor",arrive="Down on the floor! Diolch, Blondin."};
            var bottomStation=new ZipLine.Station{bay=Bay(ride[^1]),cable=lo,facing=yawDown+180,name="Blondin floor bay",ride="Ride the Blondin up to the big tip",arrive="Top of the tip! Mind the edge."};
            Bays=(topStation.bay,bottomStation.bay);
            if(BayviewKit.Has("zip_trolley")){var carriage=BayviewKit.Spawn("zip_trolley",ride[0],yawDown,BayviewKit.AnimatedRoot,null,null,false,1);
                var line=new ZipLine("Quarry Hills Blondin",topStation,bottomStation,ride,carriage.transform);
                Debug.Log($"LITTLE_ROAD_BLONDIN ride={ropeLen:0} clearance={ZipLine.Clearance(ride[0],ride[^1],.022f,6,6,c=>c.isTrigger||c.transform.IsChildOf(root)):0.0} bays={topStation.bay}/{bottomStation.bay}");}
            // Loading bays: slate-flag pads under each end of the ride.
            foreach(var bayAt in new[]{topStation.bay,bottomStation.bay}){var pad=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Object.Destroy(pad.GetComponent<Collider>());pad.name="Blondin bay";pad.transform.SetParent(root);
                pad.transform.SetPositionAndRotation(bayAt+Vector3.up*.02f,Quaternion.identity);pad.transform.localScale=new Vector3(9,.02f,9);pad.GetComponent<Renderer>().sharedMaterial=WorldLook.Kit("Blondin bay flags",Toy.Hex(0x8f9296),WorldLook.Pattern.Stone,0,.15f,true);}
            Combine("Blondin towers",beams,iron,root,true);Combine("Blondin ropes",ropes,rope,root,false);
            // A winding house at the foot of the floor tower.
            var wh=floorAt+(floorAt-tipAt).normalized*12;wh.y=ValleyField.Height(wh.x,wh.z)-.04f;
            var slate=T(0x8a8478,0x3b4047,0x3b4047,0xc0392b);slate.roof=Toy.Hex(0x4b5158);Put("house_d",wh,Yaw(wh,tipAt),slate,null,root);
        }
        public static void Combine(string name,List<Matrix4x4> boxes,Material material,Transform root,bool solid)
        {
            if(boxes.Count==0)return;
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.Destroy(temp);
            var combine=new CombineInstance[boxes.Count];for(int i=0;i<boxes.Count;i++)combine[i]=new CombineInstance{mesh=cube,transform=boxes[i]};
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            if(solid){go.AddComponent<MeshCollider>().sharedMesh=mesh;go.tag="Respawn";}
        }
    }
}

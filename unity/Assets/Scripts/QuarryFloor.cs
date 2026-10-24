using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The quarry floor (docs/59): the old pit floor between the town and the galleries, worked flat. The rally school
    // (in the old quarry hospital) and the rallycross loop with tyre walls, boards, a start gate and a grandstand. The floor
    // itself is laid over with slate rubble as one smooth ellipse (the 2.5 m terrain grid's cells left a saw-tooth edge).
    public static class QuarryFloor
    {
        // The Quarry Hills Rallycross circuit (Richard, 3 Oct: a class C dirt race here): about 1.5 km of loose slate round the
        // floor, 16 m wide like every race circuit (Circuit.Width), clockwise from the start line. A long start straight, a
        // sweeper east, a hairpin, the chicane past the grandstand, the dive south and the long west loop back.
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
        // Cars parked outside the school (read by ValleyLife).
        public static readonly List<(Vector3 at,float yaw)> Parking=new();

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
            // across the track instead, chequered at the race start (index 0) and white at the old Quarry Sprint's line.
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

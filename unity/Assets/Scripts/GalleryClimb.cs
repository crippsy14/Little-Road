using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The Gallery Climb (docs/59; reworked after Richard, 3 Oct: "routes complex, scenic, and challenging not to fall off the
    // mountain, with checkpoints"). Twelve ramps climb the galleries bench by bench, from the bottom shelf at the west end to
    // the rim. Each ramp is built against the face above its shelf: its outer edge is buried in the face, so nothing clips or
    // gaps, and a rubble bund guards the drop on the other side. A flat landing at the top lets you turn onto the next shelf.
    // Between ramps you drive the shelves themselves: hairpins low down, then "the Long Bench", a fast run of about 500 m
    // along bench 6 with only marker posts between you and the drop, then tighter switchbacks to the rim and a viewpoint over
    // Glas. Checkpoints (flag gates) split the time, and if you go over the edge you're put back at the last one (+5 s).
    public static class GalleryClimb
    {
        public const string Id="gallery_climb";public const float Par=210;public const int Prize=300;
        public static bool Known(string id)=>id==Id;
        const float RampLength=125,Width=8.5f,LandingLength=16;
        public static Vector3 Start,StartDir,Finish;public static bool Built;
        // The route's middle line bottom to top (for --climb-test and the seam audit) and its checkpoints (where, facing, the
        // lowest height you can be at before you've gone over the edge).
        public static readonly List<Vector3> Path=new();
        public static readonly List<(Vector3 at,Vector3 dir,float floor)> Checkpoints=new();
        static Vector2 Dir(float degrees){float r=degrees*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(r),Mathf.Sin(r));}
        // The galleries mesh's lips in direction `dir` (as QuarryHillsBuild.Galleries lays them).
        public static float[] Lips(Vector2 dir)
        {
            int B=QuarryHills.Benches;float W=QuarryHills.BenchWidth;var lips=new float[B];float prev=QuarryHills.RimLedge+.6f;
            for(int k=0;k<B;k++){var at=QuarryHills.PitPoint(dir,k*W);float lip=Mathf.Max(prev+(k==0?1.5f:.5f),k*W-QuarryHills.Wobble(at.x,at.z));lips[k]=lip;prev=lip+1.1f;}
            return lips;
        }
        static Vector3 At(float a,float e,float y){var p=QuarryHills.PitPoint(Dir(a),e);p.y=y;return p;}
        // The angle `length` metres on along the ramp's middle line up face k.
        static float Walk(float from,int sign,int k,float length)
        {
            float run=0,a=from;Vector3 last=At(a,Lips(Dir(a))[k]-Width*.5f,0);
            while(run<length){a+=sign*.05f;var p=At(a,Lips(Dir(a))[k]-Width*.5f,0);run+=Vector3.Distance(p,last);last=p;}
            return a;
        }
        // The route: (direction, the shelf traverse in degrees before this ramp starts). Hairpins on the lower benches in the west
        // end; the Long Bench on shelf 6 (above the west incline's head) east to the middle; switchbacks to the rim.
        static readonly (int dir,float traverse)[] Plan={(1,0),(-1,2),(1,2),(-1,2),(1,2),(-1,2),(1,68),(-1,2),(1,2),(-1,2),(1,2),(-1,2)};
        const float StartAngle=-137;   // (the west hairpins stay well inside the benches)
        // The angles the ramps and landings cover, by bench (scree and birch keep out).
        static readonly List<(float a0,float a1,int k)> covered=new();
        public static bool Covers(float degrees,int bench){float a=Mathf.DeltaAngle(0,degrees);foreach(var c in covered)if(c.k==bench&&a>=Mathf.Min(c.a0,c.a1)-2&&a<=Mathf.Max(c.a0,c.a1)+2)return true;return false;}

        public static void Build(Transform parent)
        {
            Built=false;Path.Clear();Checkpoints.Clear();covered.Clear();if(!QuarryHills.Enabled)return;
            int B=QuarryHills.Benches;float F=QuarryHills.FloorY,H=QuarryHills.BenchHeight;
            var gravel=WorldLook.Kit("Gallery Climb gravel",new Color(.50f,.49f,.47f),WorldLook.Pattern.Rock,0,.06f,true);
            var slate=WorldLook.Kit("Gallery Climb walls",new Color(.33f,.34f,.38f),WorldLook.Pattern.Slate,0,.05f,true);
            var v=new List<Vector3>();var t=new List<int>();var tw=new List<int>();var bunds=new List<Matrix4x4>();var posts=new List<Matrix4x4>();var postsRed=new List<Matrix4x4>();var stripe=new List<Vector3>();var ts=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool wall=false){foreach(bool back in new[]{false,true}){int s=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);(wall?tw:t).AddRange(back?new[]{s,s+2,s+1,s,s+3,s+2}:new[]{s,s+1,s+2,s,s+2,s+3});}}
            void Bund(Vector3 a,Vector3 b){var d=b-a;d.y=0;if(d.sqrMagnitude<.01f)return;var m=(a+b)*.5f;bunds.Add(Matrix4x4.TRS(m+Vector3.up*.35f,Quaternion.LookRotation(d),new Vector3(.9f,1.1f,d.magnitude+.25f)));}
            float angle=StartAngle;
            for(int k=0;k<B;k++) {
                var (sign,traverse)=Plan[k];float y0=F+k*H,y1=F+(k+1)*H;
                // The traverse along shelf k (from the last landing to this ramp's foot): a gravel track on the shelf and marker
                // posts along its drop edge (red and white, every 1.6 m).
                if(k>0) {
                    float from=angle,to=angle+sign*traverse;const int n=24;var prevLeft=Vector3.zero;var prevRight=Vector3.zero;
                    for(int i=0;i<=n;i++) {
                        float u=i/(float)n,a=Mathf.Lerp(from,to,u);var l=Lips(Dir(a));float eMid=Mathf.Lerp(l[k-1]-Width*.5f+3,l[k]-Width*.5f,Mathf.SmoothStep(0,1,Mathf.Clamp01(u*2)));
                        // (On the shelf itself: past the top of the face below, clear of its edge.)
                        eMid=Mathf.Max(eMid,l[k-1]+1.1f+4.5f);var p=At(a,eMid,y0+.03f);Path.Add(p);
                        var left=At(a,eMid-3.5f,y0+.035f);var right=At(a,eMid+3.5f,y0+.035f);
                        if(i>0){int s=stripe.Count;stripe.Add(prevLeft);stripe.Add(prevRight);stripe.Add(right);stripe.Add(left);ts.AddRange(new[]{s,s+3,s+1,s+1,s+3,s+2});}
                        prevLeft=left;prevRight=right;
                    }
                    float span=Mathf.Abs(to-from)*8+12;int count=0;
                    for(float d=-6;d<=span;d+=1.6f){float a=from+sign*d/8f;var p=At(a,Lips(Dir(a))[k-1]+1.6f,y0);(count++%2==0?posts:postsRed).Add(Matrix4x4.TRS(p+Vector3.up*.6f,Quaternion.identity,new Vector3(.18f,1.2f,.18f)));}
                    if(traverse>20)Checkpoints.Add((At(to,Lips(Dir(to))[k]-Width*.5f,y0+.1f),Tangent(to,sign,k),y0-6));
                    angle=to;
                }
                // The ramp: from shelf k (y0) up to shelf k+1 (y1) against face k.
                float end=Walk(angle,sign,k,RampLength);const int m=70;
                Vector3 pIn=default,pOut=default,pFoot=default;
                for(int i=0;i<=m;i++) {
                    float u=i/(float)m,a=Mathf.Lerp(angle,end,u);var l=Lips(Dir(a));float lip=l[k];
                    float y=y0+.03f+u*H;float inner=Mathf.Max(lip-Width,k==0?QuarryHills.RimLedge+1.2f:l[k-1]+2);
                    // The outer edge past where the face stands at this height: buried in the face, no gap.
                    float outer=lip+1.1f*(y-y0)/H+.5f;
                    // Its drop side is a spoil bank (about 60 degrees) down to the shelf, as the inclines are built, not a sheer wall.
                    float foot=Mathf.Max(inner-(y-y0)*.55f,k==0?QuarryHills.RimLedge+.8f:l[k-1]+1.4f);
                    var ni=At(a,inner,y);var no=At(a,outer,y);var nf=At(a,foot,y0-.2f);
                    if(i>0){Quad(pIn,ni,no,pOut);if(y-y0>.5f){Quad(pFoot,nf,ni,pIn,true);Bund(pIn,ni);}}
                    pIn=ni;pOut=no;pFoot=nf;if(i%2==0)Path.Add(At(a,(inner+lip)*.5f,y));
                    if(k==0&&i==0){Start=At(a,(inner+lip)*.5f,y0);StartDir=Tangent(a,sign,0);}
                }
                // The landing: level at y1, from the ramp's inner edge out over the top of face k (onto shelf k+1).
                float landEnd=Walk(end,sign,k,LandingLength);const int ln=12;
                for(int i=0;i<=ln;i++) {
                    float u=i/(float)ln,a=Mathf.Lerp(end,landEnd,u);var l=Lips(Dir(a));float inner=Mathf.Max(l[k]-Width,k==0?QuarryHills.RimLedge+1.2f:l[k-1]+2);
                    var ni=At(a,inner,y1+.03f);var no=At(a,l[k]+1.6f,y1+.03f);var nf=At(a,Mathf.Max(inner-H*.55f,k==0?QuarryHills.RimLedge+.8f:l[k-1]+1.4f),y0-.2f);
                    Quad(pIn,ni,no,pOut);Quad(pFoot,nf,ni,pIn,true);Bund(pIn,ni);pIn=ni;pOut=no;pFoot=nf;if(i%3==0)Path.Add(At(a,(inner+l[k])*.5f,y1+.03f));
                }
                // The landing's far end: a wall down to the shelf below with the bund across it (you turn onto shelf k+1 before it).
                { var l=Lips(Dir(landEnd));var top=At(landEnd,l[k]+1.1f,y1+.03f);Quad(pIn,top,new Vector3(top.x,y0-.2f,top.z),pFoot,true);Bund(pIn,top); }
                covered.Add((angle,landEnd,k));covered.Add((angle,landEnd,k+1));
                if(k%2==1&&k<B-1&&Plan[k+1].traverse<20)Checkpoints.Add((At(end,Lips(Dir(end))[k]-Width*.5f,y1+.1f),Tangent(end,sign,k),y1-6));
                if(k==B-1){var dir=Dir(landEnd);Finish=QuarryHills.PitPoint(dir,Lips(dir)[k]+12);Finish.y=y1;}
                angle=landEnd;
            }
            Mesh Make(string name,List<Vector3> verts,List<int> a,List<int> b){var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=b!=null?2:1};mesh.SetVertices(verts);mesh.SetTriangles(a,0);if(b!=null)mesh.SetTriangles(b,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
            var go=new GameObject("Gallery Climb",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
            var climb=Make("Gallery Climb",v,t,tw);go.GetComponent<MeshFilter>().sharedMesh=climb;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{gravel,slate};go.GetComponent<MeshCollider>().sharedMesh=climb;
            var sg=new GameObject("Gallery Climb shelf track",typeof(MeshFilter),typeof(MeshRenderer));sg.transform.SetParent(parent);sg.GetComponent<MeshFilter>().sharedMesh=Make("Shelf track",stripe,ts,null);
            var sr=sg.GetComponent<MeshRenderer>();sr.sharedMaterial=gravel;sr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            QuarryFloor.Combine("Gallery Climb bund",bunds,slate,parent,true);
            QuarryFloor.Combine("Gallery Climb posts",posts,TrialWorld.Material("Marker white",new Color(.96f,.96f,.94f),0,.3f),parent,false);
            QuarryFloor.Combine("Gallery Climb posts red",postsRed,TrialWorld.Material("Marker red",Toy.Red,0,.3f),parent,false);
            Checkpoints.Add((Finish,StartDir,Finish.y-6));
            Physics.SyncTransforms();
            // Flag gates: at the start, each checkpoint and the finish, stood on whatever is solid under them.
            // (The highest solid hit: RaycastAll's order is arbitrary, so taking the first one found made the flags' heights
            // depend on how the physics engine happened to store its colliders.)
            Vector3 Down(Vector3 p){float top=float.MinValue;foreach(var h in Physics.RaycastAll(p+Vector3.up*6,Vector3.down,14))if(!h.collider.isTrigger&&h.point.y>p.y-8)top=Mathf.Max(top,h.point.y);if(top>float.MinValue)p.y=Mathf.Max(top,p.y-8);return p;}
            var flags=new BayviewKit.Tint(Color.white,Toy.Hex(0x229954),Toy.Hex(0xc0392b),Color.white);
            void Gate(Vector3 at,Vector3 dir,float half){var across=Vector3.Cross(Vector3.up,dir);foreach(int s in new[]{-1,1})if(BayviewKit.Has("flag_pole"))BayviewKit.Spawn("flag_pole",Down(at+across*s*half),0,parent,flags,null,false,1);}
            Gate(Start,StartDir,3.6f);for(int i=0;i<Checkpoints.Count;i++)Gate(Checkpoints[i].at,Checkpoints[i].dir,i==Checkpoints.Count-1?7:3.4f);
            var board=Start-StartDir*14;board.y=Start.y;if(BayviewKit.Has("map_board"))BayviewKit.Spawn("map_board",Down(board),Quaternion.LookRotation(StartDir).eulerAngles.y,parent,null,null,false,1);
            // The finish: a viewpoint on the rim over Glas (a cairn, benches, a telescope).
            var toPit=new Vector3(QuarryHills.Pit.x-Finish.x,0,QuarryHills.Pit.y-Finish.z).normalized;var side=Vector3.Cross(Vector3.up,toPit);
            var cairn=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Object.Destroy(cairn.GetComponent<Collider>());cairn.name="Rim cairn";cairn.transform.SetParent(parent);
            cairn.transform.SetPositionAndRotation(Down(Finish+side*10)+Vector3.up*1,Quaternion.identity);cairn.transform.localScale=new Vector3(1.6f,1,1.6f);cairn.GetComponent<Renderer>().sharedMaterial=slate;
            if(BayviewKit.Has("telescope"))BayviewKit.Spawn("telescope",Down(Finish-side*9),Quaternion.LookRotation(toPit).eulerAngles.y,parent,null,null,false,1);
            foreach(int s in new[]{-1,1})if(BayviewKit.Has("bench"))BayviewKit.Spawn("bench",Down(Finish+side*s*5-toPit*3),Quaternion.LookRotation(toPit).eulerAngles.y,parent,null,null,false,1);
            Built=true;
            Debug.Log($"LITTLE_ROAD_GALLERY_CLIMB start={Start} finish={Finish} rise={Finish.y-Start.y:0} checkpoints={Checkpoints.Count} path={Path.Count}");
        }
        static Vector3 Tangent(float a,int sign,int k){var p=At(a,Lips(Dir(a))[k]-Width*.5f,0);var q=At(a+sign*.5f,Lips(Dir(a+sign*.5f))[k]-Width*.5f,0);var d=q-p;d.y=0;return d.normalized;}
    }
}

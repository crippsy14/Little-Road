using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The Gallery Climb (docs/59): a hill climb up the galleries' western end. Twelve gravel ramps, one per bench, each about
    // 150 m at 10% hugging the face above it, zig-zag up from the pit's ledge to the rim; each ends on a landing that spills
    // onto the next shelf, where you hairpin onto the next ramp. A rubble bund lines every drop. You reach it from the quarry
    // floor along the 5 m ledge round Glas. The run starts itself as you pass the bottom flags heading up and ends at the
    // top flags (best time kept; a prize for beating par once).
    public static class GalleryClimb
    {
        public const string Id="gallery_climb";public const float Par=180;public const int Prize=300;
        public static bool Known(string id)=>id==Id;
        const float StartAngle=-146,RampLength=150,Width=8.5f;
        public static Vector3 Start,StartDir,Finish;public static bool Built;
        // The climb's middle line, bottom to top (for --climb-test).
        public static readonly List<Vector3> Path=new();
        static Vector2 Dir(float degrees){float r=degrees*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(r),Mathf.Sin(r));}
        // The galleries mesh's lips in direction `dir` (as QuarryHillsBuild.Galleries lays them).
        public static float[] Lips(Vector2 dir)
        {
            int B=QuarryHills.Benches;float W=QuarryHills.BenchWidth;var lips=new float[B];float prev=QuarryHills.RimLedge+.6f;
            for(int k=0;k<B;k++){var at=QuarryHills.PitPoint(dir,k*W);float lip=Mathf.Max(prev+(k==0?1.5f:.5f),k*W-QuarryHills.Wobble(at.x,at.z));lips[k]=lip;prev=lip+1.1f;}
            return lips;
        }
        // The angular span (degrees) each ramp takes: walk from `from` in direction `sign` until RampLength along its middle.
        static float Walk(float from,int sign,int k)
        {
            float run=0,a=from;Vector3 last=Mid(a,k);
            while(run<RampLength){a+=sign*.1f;var p=Mid(a,k);run+=Vector3.Distance(new(p.x,0,p.z),new(last.x,0,last.z));last=p;}
            return a;
        }
        static Vector3 Mid(float a,int k){var d=Dir(a);return QuarryHills.PitPoint(d,Lips(d)[k]-4);}
        // The angles the climb covers (scree and trees keep out of them).
        public static bool Covers(float degrees){float a=Mathf.DeltaAngle(0,degrees);return a>-152&&a<-108;}

        public static void Build(Transform parent)
        {
            Built=false;Path.Clear();if(!QuarryHills.Enabled)return;
            int B=QuarryHills.Benches;float F=QuarryHills.FloorY,H=QuarryHills.BenchHeight;
            var gravel=WorldLook.Kit("Gallery Climb gravel",new Color(.62f,.60f,.56f),WorldLook.Pattern.Rock,0,.1f,true);
            var bund=WorldLook.Kit("Gallery Climb bund",new Color(.45f,.46f,.50f),WorldLook.Pattern.Rock,0,.1f,true);
            var v=new List<Vector3>();var t=new List<int>();var tw=new List<int>();var bunds=new List<Matrix4x4>();
            // Road surface (gravel) and the walls under it (slate rubble), each two-sided with its own vertices.
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool wall=false){foreach(bool back in new[]{false,true}){int s=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);(wall?tw:t).AddRange(back?new[]{s,s+2,s+1,s,s+3,s+2}:new[]{s,s+1,s+2,s,s+2,s+3});}}
            void Bund(Vector3 a,Vector3 b){var d=b-a;d.y=0;if(d.sqrMagnitude<.01f)return;var m=(a+b)*.5f;bunds.Add(Matrix4x4.TRS(m+Vector3.up*.35f,Quaternion.LookRotation(d),new Vector3(.9f,1.1f,d.magnitude+.2f)));}
            float angle=StartAngle;Vector2 finishDir=default;float finishLip=0;
            for(int k=0;k<B;k++) {
                int sign=k%2==0?1:-1;float end=Walk(angle,sign,k);const int n=60;float y0=F+k*H,y1=F+(k+1)*H;
                Vector3 prevIn=default,prevOut=default,prevFoot=default;
                for(int i=0;i<=n;i++) {
                    float u=i/(float)n,a=Mathf.Lerp(angle,end,u);var dir=Dir(a);var lips=Lips(dir);float lip=lips[k];
                    float inner=Mathf.Max(lip-Width,k==0?.8f:lips[k-1]+1.8f);
                    // The last 12% is the landing: level with the shelf above, spilling out over the face onto it.
                    float landing=Mathf.InverseLerp(.88f,1,u);float outer=lip+.2f+landing*7;float y=Mathf.Lerp(y0,y1,Mathf.Min(1,u/.88f))+.06f;
                    var pin=QuarryHills.PitPoint(dir,inner);pin.y=y;var pout=QuarryHills.PitPoint(dir,outer);pout.y=y;var foot=pin;foot.y=y0-.3f;
                    if(i>0){Quad(prevIn,pin,pout,prevOut);Quad(prevFoot,foot,pin,prevIn,true);if(y-y0>.6f)Bund(prevIn,pin);}
                    if(i%3==0){var mid=QuarryHills.PitPoint(dir,(inner+lip)*.5f);mid.y=y;Path.Add(mid);}
                    prevIn=pin;prevOut=pout;prevFoot=foot;
                    if(k==0&&i==0){Start=QuarryHills.PitPoint(dir,(inner+lip)*.5f);Start.y=y0;var nd=Dir(a+sign*1);var ahead=QuarryHills.PitPoint(nd,(inner+lip)*.5f);StartDir=(ahead-Start);StartDir.y=0;StartDir.Normalize();}
                    if(k==B-1&&i==n){Finish=QuarryHills.PitPoint(dir,lip+4);Finish.y=y1;finishDir=dir;finishLip=lip;}
                }
                // The top end: a cliff down to the shelf below on the inner part, closed by the bund.
                { var dir=Dir(end);var lips=Lips(dir);float lip=lips[k],inner=Mathf.Max(lip-Width,k==0?.8f:lips[k-1]+1.8f);
                  var a0=QuarryHills.PitPoint(dir,inner);a0.y=y1+.06f;var a1=QuarryHills.PitPoint(dir,lip);a1.y=y1+.06f;
                  Quad(a0,a1,new Vector3(a1.x,y0-.3f,a1.z),new Vector3(a0.x,y0-.3f,a0.z),true);Bund(a0,a1); }
                angle=end;
            }
            var mesh=new Mesh{name="Gallery Climb",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=2};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetTriangles(tw,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var uv=new List<Vector2>();foreach(var p in v)uv.Add(new Vector2(p.x+p.y,p.z)*.2f);mesh.SetUVs(0,uv);
            var go=new GameObject("Gallery Climb",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{gravel,bund};go.GetComponent<MeshCollider>().sharedMesh=mesh;
            QuarryFloor.Combine("Gallery Climb bund",bunds,bund,parent,true);
            // Flags at the start (on ramp 1's two edges) and the finish (along the top shelf), and a map board on the ledge
            // before the start: each stood on whatever is solid under it (the ramp, the galleries' ledge and shelves).
            Physics.SyncTransforms();
            Vector3 Down(Vector3 p){foreach(var h in Physics.RaycastAll(p+Vector3.up*6,Vector3.down,14))if(!h.collider.isTrigger&&h.point.y>p.y-8){p.y=Mathf.Max(h.point.y,p.y-8);return p;}return p;}
            var white=new BayviewKit.Tint(Color.white,Toy.Hex(0x229954),Toy.Hex(0xc0392b),Color.white);
            var d0=Dir(StartAngle);var l0=Lips(d0);float in0=Mathf.Max(l0[0]-Width,.8f);
            var flags=new List<Vector3>{QuarryHills.PitPoint(d0,in0+.6f),QuarryHills.PitPoint(d0,l0[0]-.6f),QuarryHills.PitPoint(finishDir,finishLip+2),QuarryHills.PitPoint(finishDir,finishLip+11)};
            for(int i=0;i<flags.Count;i++){var f=flags[i];f.y=i<2?Start.y+.1f:Finish.y+.1f;if(BayviewKit.Has("flag_pole"))BayviewKit.Spawn("flag_pole",Down(f),0,parent,white,null,false,1);}
            var board=Start-StartDir*14;board.y=Start.y;if(BayviewKit.Has("map_board"))BayviewKit.Spawn("map_board",Down(board),Quaternion.LookRotation(StartDir).eulerAngles.y,parent,null,null,false,1);
            Built=true;
            Debug.Log($"LITTLE_ROAD_GALLERY_CLIMB start={Start} finish={Finish} rise={Finish.y-Start.y:0}");
        }
    }
}

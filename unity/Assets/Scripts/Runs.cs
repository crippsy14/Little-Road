using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge's run layer (docs/77, step 2; the architecture review 1.2, 8.1): a run (a fence, later a wall) is a spec,
    // solved once into a layout as data (pieces and matrices, contact samples, measures), then drawn as merged chunks.
    // The solve knows its cutters before it lays anything: every road that crosses the run's line makes an opening framed
    // by gate posts, so nothing is laid and then trimmed. Rigid (modular) kits for now: posts plumb at facet points along
    // the offset line, rails straight between them, raked to the line between their feet.
    public sealed class RunSpec
    {
        public string name,kit,seed;
        public Vector3[] along;            // (the road or line the run follows)
        public float offset;                // (metres to the right of the line's direction; negative: the left)
        public float from=0,to=-1;          // (arc range along the offset line; to < 0: to the end)
        public Func<Vector3,bool> cut;      // (true where something cuts the run: a crossing road, a drive)
    }
    public struct RunItem{public string piece;public Matrix4x4 m;public float arc;}
    public sealed class RunLayout
    {
        public RunSpec spec;public PropKit kit;
        public readonly List<RunItem> items=new();
        public readonly List<Vector3> feet=new();          // (every post's foot: on the ground)
        public readonly List<string> failures=new(),notes=new();
        public Vector3[] line;public float length,minSpan=float.MaxValue,maxSpan,worstRake,worstClearLow=float.MaxValue,worstClearHigh;
        public int posts,spans,openings,gatePosts,split;public bool tooShort;public double solveMs,drawMs;
        public string Hash()
        {
            var sb=new StringBuilder();foreach(var it in items){sb.Append(it.piece);for(int i=0;i<16;i++)sb.Append(Mathf.Round(it.m[i]*1000)).Append(',');}
            return Hash128.Compute(sb.ToString()).ToString();
        }
    }

    public static class Runs
    {
        public static readonly List<RunLayout> Laid=new();
        const float Step=.5f;
        // (a span may be split down to this to keep its rails at the right height over a dip or a crest: by a drive cut
        //  into the bank on the range, a 2.3 m span had to become two, 18 Oct)
        public const float MinSplit=.9f;

        // A polyline offset sideways in plan (heights from the ground after), its folds on the inside of tight bends
        // removed: an offset point whose step runs backwards against the line it came from is dropped.
        public static Vector3[] Offset(Vector3[] line,float d)
        {
            var dense=Resample(line,Step);var o=new List<Vector3>();
            for(int i=0;i<dense.Count;i++) {
                var f=dense[Mathf.Min(i+1,dense.Count-1)]-dense[Mathf.Max(i-1,0)];f.y=0;f.Normalize();
                var p=dense[i]+Vector3.Cross(Vector3.up,f)*d;p.y=0;
                if(o.Count>0) {
                    var step=p-o[^1];var g=dense[i]-dense[i-1];g.y=0;
                    if(Vector3.Dot(step,g)<=0||step.magnitude<1e-3f)continue;
                }
                o.Add(p);
            }
            return o.ToArray();
        }
        static List<Vector3> Resample(Vector3[] line,float step)
        {
            var o=new List<Vector3>{line[0]};float carry=0;
            for(int i=0;i+1<line.Length;i++){var a=line[i];var b=line[i+1];float L=Vector3.Distance(a,b);float s=step-carry;
                while(s<=L){o.Add(Vector3.Lerp(a,b,s/L));s+=step;}carry=L-(s-step);}
            if((o[^1]-line[^1]).sqrMagnitude>1e-4f)o.Add(line[^1]);return o;
        }
        static float[] Arc(Vector3[] p){var a=new float[p.Length];for(int i=1;i<p.Length;i++)a[i]=a[i-1]+Vector3.Distance(p[i-1],p[i]);return a;}
        static Vector3 At(Vector3[] p,float[] arc,float s)
        {
            int i=Array.BinarySearch(arc,s);if(i>=0)return p[i];i=~i;if(i<=0)return p[0];if(i>=p.Length)return p[^1];
            float t=(s-arc[i-1])/Mathf.Max(arc[i]-arc[i-1],1e-6f);return Vector3.Lerp(p[i-1],p[i],t);
        }
        static Vector3 Foot(Vector3 p)=>new(p.x,Ground.Surface(p.x,p.z),p.z);

        public static RunLayout Solve(RunSpec spec)
        {
            var sw=Stopwatch.StartNew();var kit=PropKit.Load(spec.kit);var L=new RunLayout{spec=spec,kit=kit};var run=kit.file.run;
            var line=Offset(spec.along,spec.offset);var arc=Arc(line);float total=arc[^1];
            float a0=Mathf.Clamp(spec.from,0,total),a1=spec.to<0?total:Mathf.Clamp(spec.to,0,total);
            L.line=line;L.length=a1-a0;
            float nominal=run.spacing,maxSpan=nominal*kit.Piece("rail").stretch[1],minSpan=nominal*kit.Piece("rail").stretch[0];
            // the open stretches: walk the line every half metre, cutting where anything cuts it
            var stretches=new List<(float a,float b)>();float? start=null;
            for(float s=a0;s<=a1+1e-4f;s+=Step) {
                bool blocked=spec.cut!=null&&spec.cut(At(line,arc,Mathf.Min(s,a1)));
                if(!blocked&&start==null)start=s;
                if((blocked||s+Step>a1+1e-4f)&&start!=null){float e=blocked?s-Step:a1;if(e>start.Value)stretches.Add((start.Value,e));start=null;}
            }
            L.openings=Mathf.Max(0,stretches.Count-1)+(stretches.Count>0&&stretches[0].a>a0+1e-3f?1:0)+(stretches.Count>0&&stretches[^1].b<a1-1e-3f?1:0);
            float[] heights=run.railHeights;
            foreach(var (sa,sb) in stretches) {
                float len=sb-sa;
                if(len<minSpan){L.tooShort=true;L.notes.Add($"a stretch of {len:0.0} m at {sa:0} m is shorter than one span ({minSpan:0.0} m): nothing laid");continue;}
                // (as near the kit's own spacing as whole spans allow, never past its stretch either way)
                int n=Mathf.Clamp(Mathf.RoundToInt(len/nominal),Mathf.Max(1,Mathf.CeilToInt(len/maxSpan)),Mathf.Max(1,Mathf.FloorToInt(len/minSpan)));var posts=new List<float>();for(int k=0;k<=n;k++)posts.Add(sa+len*k/n);
                // (rails clear of a crest and not stranded over a hollow: a span that fails gets a post at its middle)
                for(int k=0;k+1<posts.Count;k++) {
                    var (low,high)=Clearance(line,arc,posts[k],posts[k+1],heights[0]-.055f);
                    float span=posts[k+1]-posts[k];
                    if((low<run.clearance[0]||high>run.clearance[1])&&span/2>=MinSplit){posts.Insert(k+1,(posts[k]+posts[k+1])/2);L.split++;k--;}
                }
                bool openStart=sa>a0+1e-3f,openEnd=sb<a1-1e-3f;
                for(int k=0;k<posts.Count;k++) {
                    var f=Foot(At(line,arc,posts[k]));L.feet.Add(f);
                    bool first=k==0,last=k==posts.Count-1;
                    string piece=(first&&openStart)||(last&&openEnd)?"gate_post":first||last?"end_post":"post";if(piece=="gate_post")L.gatePosts++;
                    var dir=At(line,arc,Mathf.Min(posts[k]+.5f,a1))-At(line,arc,Mathf.Max(posts[k]-.5f,a0));dir.y=0;
                    L.items.Add(new RunItem{piece=piece,arc=posts[k],m=Matrix4x4.TRS(f,Quaternion.LookRotation(dir.normalized,Vector3.up),Vector3.one)});L.posts++;
                    if(k>0) {
                        var a=L.feet[^2];var b=f;float span=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(b.x,0,b.z));
                        L.minSpan=Mathf.Min(L.minSpan,span);L.maxSpan=Mathf.Max(L.maxSpan,span);L.spans++;
                        var (low,high)=Clearance(line,arc,posts[k-1],posts[k],heights[0]-.055f);
                        L.worstClearLow=Mathf.Min(L.worstClearLow,low);L.worstClearHigh=Mathf.Max(L.worstClearHigh,high);
                        if(low<run.clearance[0]-1e-3f)L.failures.Add($"the bottom rail is {low:0.00} m off the ground at {posts[k-1]:0} m (at least {run.clearance[0]:0.00})");
                        if(high>run.clearance[1]+1e-3f)L.failures.Add($"the bottom rail is {high:0.00} m off the ground at {posts[k-1]:0} m (at most {run.clearance[1]:0.00})");
                        foreach(float h in heights) {
                            var ra=a+Vector3.up*h;var rb=b+Vector3.up*h;var d=rb-ra;
                            L.items.Add(new RunItem{piece="rail",arc=posts[k-1],m=Matrix4x4.TRS(ra,Quaternion.LookRotation(d.normalized,Vector3.up),new Vector3(1,1,d.magnitude/kit.Piece("rail").length))});
                        }
                    }
                }
            }
            L.solveMs=sw.Elapsed.TotalMilliseconds;return L;
        }
        // The bottom rail's lowest and highest gap to the ground between two posts, sampled every quarter metre along the
        // chord (where the rail is, not the curve).
        static (float low,float high) Clearance(Vector3[] line,float[] arc,float sa,float sb,float bottom)
        {
            var a=Foot(At(line,arc,sa));var b=Foot(At(line,arc,sb));float len=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(b.x,0,b.z));
            float low=float.MaxValue,high=0;int n=Mathf.Max(2,Mathf.CeilToInt(len/.25f));
            for(int i=1;i<n;i++){var p=Vector3.Lerp(a,b,i/(float)n);float gap=p.y+bottom-Ground.Surface(p.x,p.z);low=Mathf.Min(low,gap);high=Mathf.Max(high,gap);}
            return (low==float.MaxValue?bottom:low,high==0?bottom:high);
        }

        // ---- drawing: chunks of about 40 m, one merged mesh per LOD with a submesh per material class, a LOD group, and
        // box colliders merged along straight stretches (never the visual mesh)
        const float Chunk=40;
        public static GameObject Draw(RunLayout L,Transform parent)
        {
            var sw=Stopwatch.StartNew();var root=new GameObject("Run "+L.spec.name);root.transform.SetParent(parent,false);
            foreach(var group in L.items.GroupBy(i=>Mathf.FloorToInt(i.arc/Chunk))) {
                var chunk=new GameObject($"Run {L.spec.name} chunk {group.Key}");chunk.transform.SetParent(root.transform,false);
                var lods=new List<LOD>();
                for(int lod=0;lod<2;lod++) {
                    var byMat=new Dictionary<string,List<CombineInstance>>();
                    foreach(var it in group) {
                        var mesh=L.kit.Mesh(it.piece,lod);var subs=L.kit.Piece(it.piece).lods[lod].submeshes;
                        for(int s=0;s<subs.Length;s++){if(!byMat.TryGetValue(subs[s].material,out var list))byMat[subs[s].material]=list=new();list.Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=it.m});}
                    }
                    var parts=new List<CombineInstance>();var mats=new List<Material>();
                    foreach(var kv in byMat){var m=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.CombineMeshes(kv.Value.ToArray(),true,true);parts.Add(new CombineInstance{mesh=m});mats.Add(PropKit.MaterialFor(kv.Key));}
                    var merged=new Mesh{name=chunk.name+" lod"+lod,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};merged.CombineMeshes(parts.ToArray(),false,false);
                    var go=new GameObject(chunk.name+" lod"+lod,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(chunk.transform,false);
                    go.GetComponent<MeshFilter>().sharedMesh=merged;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=mats.ToArray();
                    lods.Add(new LOD(lod==0?.06f:.012f,new Renderer[]{r}));
                }
                chunk.AddComponent<LODGroup>().SetLODs(lods.ToArray());
            }
            Colliders(L,root.transform);
            L.drawMs=sw.Elapsed.TotalMilliseconds;return root;
        }
        static void Colliders(RunLayout L,Transform root)
        {
            var col=new GameObject($"Run {L.spec.name} barrier");col.transform.SetParent(root,false);col.tag="Respawn";
            var feet=L.items.Where(i=>i.piece!="rail").Select(i=>(Vector3)i.m.GetColumn(3)).ToList();
            float h=1.15f;int startIdx=0;
            // (one box per straight stretch, on its own child so it can turn with the run)
            void Box(int a,int b){var p=feet[a];var q=feet[b];var d=q-p;d.y=0;if(d.magnitude<.05f)return;
                var g=new GameObject("span");g.transform.SetParent(col.transform,false);g.tag="Respawn";g.transform.SetPositionAndRotation((p+q)/2+Vector3.up*h/2,Quaternion.LookRotation(d.normalized,Vector3.up));
                var box=g.AddComponent<BoxCollider>();box.size=new Vector3(.14f,h,d.magnitude+.1f);}
            for(int i=1;i<feet.Count;i++) {
                bool gap=Vector3.Distance(new Vector3(feet[i].x,0,feet[i].z),new Vector3(feet[i-1].x,0,feet[i-1].z))>L.kit.file.run.spacing*1.3f;
                bool turn=false;if(i-startIdx>=1&&i+1<feet.Count){var d0=feet[i]-feet[startIdx];var d1=feet[i+1]-feet[i];d0.y=d1.y=0;turn=Vector3.Angle(d0,d1)>3;}
                if(gap){if(i-1>startIdx)Box(startIdx,i-1);startIdx=i;continue;}
                if(turn||i==feet.Count-1){Box(startIdx,i);startIdx=i;}
            }
        }

        public static string Report(RunLayout L)=>
            $"{L.spec.name,-24} {L.kit.file.kit}@{L.kit.file.version}  {L.length,6:0.0} m  posts {L.posts,3}  spans {L.spans,3} ({(L.spans>0?L.minSpan:0):0.00}-{L.maxSpan:0.00} m)  " +
            $"openings {L.openings} (gate posts {L.gatePosts})  split {L.split}  clearance {(L.spans>0?L.worstClearLow:0):0.00}-{L.worstClearHigh:0.00} m  " +
            $"solve {L.solveMs:0.0} ms  draw {L.drawMs:0.0} ms  {(L.failures.Count==0?"ok":L.failures.Count+" failure(s)")}" +
            (L.notes.Count>0?"  ("+string.Join("; ",L.notes)+")":"");
        public static void WriteReport(string path)
        {
            var lines=Laid.Select(Report).ToList();foreach(var L in Laid)foreach(var f in L.failures)lines.Add($"  {L.spec.name}: {f}");
            File.WriteAllLines(path,lines);foreach(var l in lines)UnityEngine.Debug.Log("LITTLE_ROAD_RUN "+l);
        }
    }
}

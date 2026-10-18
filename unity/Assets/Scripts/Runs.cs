using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge's run layer (docs/77, steps 2-3; the architecture review 1.2, 8.1): a run (a fence, a wall) is a spec,
    // solved once into a layout as data (pieces and matrices, deform values, contact samples, measures), then drawn as
    // merged chunks. The solve knows its cutters before it lays anything: every road that crosses the run's line makes an
    // opening framed by gate posts or piers, so nothing is laid and then trimmed. Pieces are found by role (node, end,
    // gate_post, middle), so one solver lays any kit:
    //   modular (a fence): posts plumb at facet points along the offset line, rails straight between them, raked to the
    //     line between their feet;
    //   deform (a dry-stone wall): piers plumb at the ends, openings and corners; between them, wall pieces bent along a
    //     smoothed path on upright frames (no twist) and sheared to the ground, ending where the next begins (no cracks).
    public sealed class RunSpec
    {
        public string name,kit,seed;
        public Vector3[] along;            // (the road or line the run follows)
        public float offset;                // (metres to the right of the line's direction; negative: the left)
        public float from=0,to=-1;          // (arc range along the offset line; to < 0: to the end)
        public Func<Vector3,bool> cut;      // (true where something cuts the run: a crossing road, a drive)
    }
    // One laid piece: rigid (its matrix places it) or bent (deform: along path `path` from arc s0 to s1; its matrix is
    // only where it starts).
    public struct RunItem{public string piece;public Matrix4x4 m;public float arc;public bool deform;public int path;public float s0,s1;}
    // A bent piece's path between two piers: plan points every deform step, their arc length, and the wall's base height
    // at each (the lowest ground across its foot, less the kit's sink); u0 is the run's arc at its start (the stone
    // pattern's coordinate, so courses run on across pieces).
    public sealed class RunPath
    {
        public Vector3[] p;public float[] arc,baseY;public float u0;
        public float Length=>arc[^1];
        int Seg(float s){int i=Array.BinarySearch(arc,s);if(i>=0)return Mathf.Clamp(i,0,arc.Length-2);return Mathf.Clamp(~i-1,0,arc.Length-2);}
        public Vector3 At(float s){int i=Seg(s);return Vector3.LerpUnclamped(p[i],p[i+1],(s-arc[i])/Mathf.Max(arc[i+1]-arc[i],1e-6f));}
        public float Base(float s){int i=Seg(s);return Mathf.Lerp(baseY[i],baseY[i+1],(s-arc[i])/Mathf.Max(arc[i+1]-arc[i],1e-6f));}
        public Vector3 Tangent(float s,float h=.25f){var d=At(Mathf.Min(s+h,Length))-At(Mathf.Max(s-h,0));d.y=0;return d.normalized;}
    }
    public sealed class RunLayout
    {
        public RunSpec spec;public PropKit kit;
        public readonly List<RunItem> items=new();
        public readonly List<RunPath> paths=new();
        public readonly List<Vector3> feet=new();          // (every post's or pier's foot)
        public readonly List<string> failures=new(),notes=new();
        public Vector3[] line;public float length,minSpan=float.MaxValue,maxSpan,worstRake,worstClearLow=float.MaxValue,worstClearHigh;
        public int posts,spans,openings,gatePosts,split,corners;public bool tooShort;public double solveMs,drawMs;
        public bool Deforming=>kit.file.kind=="deform";
        public string Hash()
        {
            var sb=new StringBuilder();
            foreach(var it in items){sb.Append(it.piece);for(int i=0;i<16;i++)sb.Append(Mathf.Round(it.m[i]*1000)).Append(',');if(it.deform)sb.Append('|').Append(it.path).Append(',').Append(Mathf.Round(it.s0*1000)).Append(',').Append(Mathf.Round(it.s1*1000));}
            foreach(var p in paths){sb.Append('#');for(int i=0;i<p.p.Length;i++)sb.Append(Mathf.Round(p.p[i].x*1000)).Append(',').Append(Mathf.Round(p.p[i].z*1000)).Append(',').Append(Mathf.Round(p.baseY[i]*1000)).Append(';');}
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
            if((o[^1]-line[^1]).sqrMagnitude>1e-4f)o.Add(line[^1]);else o[^1]=line[^1];return o;
        }
        static float[] Arc(Vector3[] p){var a=new float[p.Length];for(int i=1;i<p.Length;i++)a[i]=a[i-1]+Vector3.Distance(p[i-1],p[i]);return a;}
        static Vector3 At(Vector3[] p,float[] arc,float s)
        {
            int i=Array.BinarySearch(arc,s);if(i>=0)return p[i];i=~i;if(i<=0)return p[0];if(i>=p.Length)return p[^1];
            float t=(s-arc[i-1])/Mathf.Max(arc[i]-arc[i-1],1e-6f);return Vector3.Lerp(p[i-1],p[i],t);
        }
        static Vector3 Foot(Vector3 p)=>new(p.x,Ground.Surface(p.x,p.z),p.z);
        public static Vector3 Right(Vector3 t)=>new(t.z,0,-t.x);
        public static string Role(PropKit kit,string role)
        {
            foreach(var p in kit.file.pieces)if(p.role==role)return p.name;
            throw new KeyNotFoundException($"Prop Forge kit '{kit.file.kit}' has no {role} piece");
        }

        public static RunLayout Solve(RunSpec spec)
        {
            var sw=Stopwatch.StartNew();var kit=PropKit.Load(spec.kit);var L=new RunLayout{spec=spec,kit=kit};var run=kit.file.run;
            var line=Offset(spec.along,spec.offset);var arc=Arc(line);float total=arc[^1];
            float a0=Mathf.Clamp(spec.from,0,total),a1=spec.to<0?total:Mathf.Clamp(spec.to,0,total);
            L.line=line;L.length=a1-a0;
            // the open stretches: walk the line every half metre, cutting where anything cuts it
            var stretches=new List<(float a,float b)>();float? start=null;
            for(float s=a0;s<=a1+1e-4f;s+=Step) {
                bool blocked=spec.cut!=null&&spec.cut(At(line,arc,Mathf.Min(s,a1)));
                if(!blocked&&start==null)start=s;
                if((blocked||s+Step>a1+1e-4f)&&start!=null){float e=blocked?s-Step:a1;if(e>start.Value)stretches.Add((start.Value,e));start=null;}
            }
            L.openings=Mathf.Max(0,stretches.Count-1)+(stretches.Count>0&&stretches[0].a>a0+1e-3f?1:0)+(stretches.Count>0&&stretches[^1].b<a1-1e-3f?1:0);
            if(L.Deforming){SolveDeform(L,line,arc,stretches,a0,a1);L.solveMs=sw.Elapsed.TotalMilliseconds;return L;}
            string rail=Role(kit,"middle"),post=Role(kit,"node"),endPost=Role(kit,"end"),gatePost=Role(kit,"gate_post");
            float nominal=run.spacing,maxSpan=nominal*kit.Piece(rail).stretch[1],minSpan=nominal*kit.Piece(rail).stretch[0];
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
                    string piece=(first&&openStart)||(last&&openEnd)?gatePost:first||last?endPost:post;if(piece==gatePost)L.gatePosts++;
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
                            L.items.Add(new RunItem{piece=rail,arc=posts[k-1],m=Matrix4x4.TRS(ra,Quaternion.LookRotation(d.normalized,Vector3.up),new Vector3(1,1,d.magnitude/kit.Piece(rail).length))});
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

        // ---- deforming kits (a dry-stone wall)
        static void SolveDeform(RunLayout L,Vector3[] line,float[] arc,List<(float a,float b)> stretches,float a0,float a1)
        {
            var kit=L.kit;var run=kit.file.run;string wall=Role(kit,"middle"),node=Role(kit,"node"),end=Role(kit,"end"),gate=Role(kit,"gate_post");
            var wp=kit.Piece(wall);float nominal=run.spacing,minLen=nominal*wp.stretch[0],maxLen=nominal*wp.stretch[1];
            // corners: where the line turns by more than the kit's maxTurn over a metre either side (the sharpest sample of
            // each); a gentle bend (an 8 m road bend turns about 16 deg a metre on its inside) bends the wall instead
            var turn=new float[line.Length];
            for(int i=2;i+2<line.Length;i++){var d0=line[i]-line[i-2];var d1=line[i+2]-line[i];d0.y=d1.y=0;turn[i]=Vector3.Angle(d0,d1);}
            foreach(var (sa,sb) in stretches) {
                if(sb-sa<minLen){L.tooShort=true;L.notes.Add($"a stretch of {sb-sa:0.0} m at {sa:0} m is shorter than one piece ({minLen:0.0} m): nothing laid");continue;}
                var cuts=new List<float>{sa};
                for(int i=2;i+2<line.Length;i++) {
                    if(turn[i]<=run.maxTurn||turn[i]<=turn[i-1]||turn[i]<turn[i+1])continue;
                    if(arc[i]-cuts[^1]<minLen||sb-arc[i]<minLen)continue;
                    cuts.Add(arc[i]);L.corners++;
                }
                cuts.Add(sb);
                bool openStart=sa>a0+1e-3f,openEnd=sb<a1-1e-3f;
                for(int c=0;c<cuts.Count;c++) {
                    bool first=c==0,last=c==cuts.Count-1;
                    string piece=(first&&openStart)||(last&&openEnd)?gate:first||last?end:node;if(piece==gate)L.gatePosts++;
                    // (a pier is square: at a corner it turns to the bisector, so a wall entering at any angle stays inside it)
                    var at=At(line,arc,cuts[c]);var din=at-At(line,arc,Mathf.Max(cuts[c]-1,a0));var dout=At(line,arc,Mathf.Min(cuts[c]+1,a1))-at;din.y=dout.y=0;
                    var dir=first?dout.normalized:last?din.normalized:din.normalized+dout.normalized;if(dir.sqrMagnitude<1e-6f)dir=dout.sqrMagnitude>1e-6f?dout:Vector3.forward;
                    var rot=Quaternion.LookRotation(dir.normalized,Vector3.up);var pp=kit.Piece(piece);
                    float low=Ground.Surface(at.x,at.z),high=low;
                    foreach(var cp in pp.contact){var w=at+rot*cp.V;float g=Ground.Surface(w.x,w.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);}
                    float y=low-pp.sink[0];
                    if(high-y>pp.skirt)L.failures.Add($"the {piece} at {cuts[c]:0} m stands on ground {high-low:0.00} m out of level, more than its skirt");
                    var f=new Vector3(at.x,y,at.z);L.feet.Add(f);L.posts++;
                    L.items.Add(new RunItem{piece=piece,arc=cuts[c],m=Matrix4x4.TRS(f,rot,Vector3.one)});
                }
                for(int c=0;c+1<cuts.Count;c++) {
                    var path=Path(L,line,arc,cuts[c],cuts[c+1]);int pi=L.paths.Count;L.paths.Add(path);float len=path.Length;
                    int n=Mathf.Clamp(Mathf.RoundToInt(len/nominal),Mathf.Max(1,Mathf.CeilToInt(len/maxLen)),Mathf.Max(1,Mathf.FloorToInt(len/minLen)));
                    for(int k=0;k<n;k++) {
                        float s0=len*k/n,s1=len*(k+1)/n;var p0=path.At(s0);p0.y=path.Base(s0);
                        L.items.Add(new RunItem{piece=wall,deform=true,path=pi,s0=s0,s1=s1,arc=cuts[c]+s0,m=Matrix4x4.TRS(p0,Quaternion.LookRotation(path.Tangent(s0),Vector3.up),Vector3.one)});
                        L.minSpan=Mathf.Min(L.minSpan,s1-s0);L.maxSpan=Mathf.Max(L.maxSpan,s1-s0);L.spans++;
                    }
                }
            }
        }
        // A wall's own path between two piers: the offset line between them, smoothed (a road's polyline kinks would show
        // as creases in a bent wall), resampled every deform step, with the wall's base at each point: the lowest ground
        // across its foot, less the kit's sink, so no foot is in the air on a cross-slope (the skirt covers the high side).
        static RunPath Path(RunLayout L,Vector3[] line,float[] arc,float sa,float sb)
        {
            var run=L.kit.file.run;var pts=new List<Vector3>();
            int n=Mathf.Max(2,Mathf.CeilToInt((sb-sa)/Step));for(int i=0;i<=n;i++){var q=At(line,arc,Mathf.Lerp(sa,sb,i/(float)n));q.y=0;pts.Add(q);}
            for(int pass=0;pass<4;pass++){var o=pts.ToArray();for(int i=1;i+1<o.Length;i++)pts[i]=o[i]*.5f+(o[i-1]+o[i+1])*.25f;}
            var p=Resample(pts.ToArray(),Mathf.Max(run.deform,.05f)).ToArray();var a=Arc(p);
            var path=new RunPath{p=p,arc=a,baseY=new float[p.Length],u0=sa};float hb=run.halfBase+.05f;
            // (the base follows the ground at the centre, less a drop: what the low side of the foot needs, the most within
            //  3 m either way, averaged over the same 6 m, so it never leaves a foot in the air yet changes gently; taken
            //  point by point, the drop swung with the low side round the inside of the 8 m bend on the bank and put the
            //  top 2.8 deg off the ground's slope, and a 2 m window still 2.2, 18 Oct)
            int n0=p.Length,W=Mathf.Max(1,Mathf.RoundToInt(3f/Mathf.Max(run.deform,.05f)));var centre=new float[n0];var drop=new float[n0];var most=new float[n0];
            for(int i=0;i<n0;i++) {
                var r=Right(path.Tangent(a[i]));float c=Ground.Surface(p[i].x,p[i].z);
                float low=Mathf.Min(c,Mathf.Min(Ground.Surface(p[i].x+r.x*hb,p[i].z+r.z*hb),Ground.Surface(p[i].x-r.x*hb,p[i].z-r.z*hb)));
                centre[i]=c;drop[i]=c-low+run.sink;
            }
            for(int i=0;i<n0;i++){float m=0;for(int j=Mathf.Max(0,i-W);j<=Mathf.Min(n0-1,i+W);j++)m=Mathf.Max(m,drop[j]);most[i]=m;}
            for(int i=0;i<n0;i++){float sum=0;int k=0;for(int j=Mathf.Max(0,i-W);j<=Mathf.Min(n0-1,i+W);j++){sum+=most[j];k++;}path.baseY[i]=centre[i]-sum/k;}
            return path;
        }
        // A bent piece's mesh at a LOD, in world space: each vertex (x across, y up, z along) goes to the path at its arc,
        // out along the upright frame's right by x, and up from the wall's base by y (a vertical shear, so the wall stays
        // plumb on a slope); the cope's top is roughened by a seeded wobble along the run's arc. Normals go through the
        // map's Jacobian, so they agree across joins. uv1 is the stone pattern's coordinate: (the run's arc, up the
        // section), and uv2.x the height above the base.
        public static Mesh Deform(RunLayout L,RunItem it,int lod)
        {
            var src=L.kit.Mesh(it.piece,lod);var v=src.vertices;var nr=src.normals;var uv=src.uv;var path=L.paths[it.path];var run=L.kit.file.run;
            float k=(it.s1-it.s0)/L.kit.Piece(it.piece).length,cope=Mathf.Max(run.height-run.copeFrom,1e-3f);const float h=.25f;
            var ov=new Vector3[v.Length];var on=new Vector3[v.Length];var u1=new Vector2[v.Length];var u2=new Vector2[v.Length];
            for(int i=0;i<v.Length;i++) {
                float s=it.s0+v[i].z*k;var P=path.At(s);var T=path.Tangent(s);var R=Right(T);float b=path.Base(s);
                float y=v[i].y;if(y>run.copeFrom)y+=run.copeJitter*Jitter(path.u0+s)*(y-run.copeFrom)/cope;
                ov[i]=new Vector3(P.x+R.x*v[i].x,b+y,P.z+R.z*v[i].x);
                var dR=Right((path.Tangent(s+h)-path.Tangent(s-h))/(2*h));float db=(path.Base(s+h)-path.Base(s-h))/(2*h);
                var c=(T+dR*v[i].x+Vector3.up*db)*k;
                on[i]=(nr[i].x*Vector3.Cross(Vector3.up,c)+nr[i].y*Vector3.Cross(c,R)+nr[i].z*Vector3.Cross(R,Vector3.up)).normalized;
                u1[i]=new Vector2(path.u0+s,uv[i].y);u2[i]=new Vector2(v[i].y,0);
            }
            var m=new Mesh{name=src.name+" bent"};m.vertices=ov;m.normals=on;m.uv=uv;m.SetUVs(1,u1);m.SetUVs(2,u2);m.subMeshCount=src.subMeshCount;
            for(int sm=0;sm<src.subMeshCount;sm++)m.SetTriangles(src.GetTriangles(sm),sm,false);
            m.RecalculateBounds();return m;
        }
        // (smooth value noise in -1..1 along a run, about three stones a metre)
        static float Jitter(float u){float x=u/.31f;int i=Mathf.FloorToInt(x);float t=x-i;t=t*t*(3-2*t);return Mathf.Lerp(Hash01(i),Hash01(i+1),t)*2-1;}
        static float Hash01(int i){uint x=(uint)i*747796405u+2891336453u;x=((x>>(int)((x>>28)+4u))^x)*277803737u;x=(x>>22)^x;return x/4294967295f;}

        // ---- drawing: chunks of about 40 m, one merged mesh per LOD with a submesh per material class, a LOD group, and
        // colliders merged along each stretch (never the visual mesh)
        const float Chunk=40;
        public static GameObject Draw(RunLayout L,Transform parent)
        {
            var sw=Stopwatch.StartNew();var root=new GameObject("Run "+L.spec.name);root.transform.SetParent(parent,false);
            var temp=new List<Mesh>();
            foreach(var group in L.items.GroupBy(i=>Mathf.FloorToInt(i.arc/Chunk))) {
                var chunk=new GameObject($"Run {L.spec.name} chunk {group.Key}");chunk.transform.SetParent(root.transform,false);
                var lods=new List<LOD>();
                for(int lod=0;lod<2;lod++) {
                    var byMat=new Dictionary<string,List<CombineInstance>>();
                    foreach(var it in group) {
                        Mesh mesh;if(it.deform){mesh=Deform(L,it,lod);temp.Add(mesh);}else mesh=L.kit.Mesh(it.piece,lod);
                        var subs=L.kit.Piece(it.piece).lods[lod].submeshes;
                        for(int s=0;s<subs.Length;s++){if(!byMat.TryGetValue(subs[s].material,out var list))byMat[subs[s].material]=list=new();list.Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=it.deform?Matrix4x4.identity:it.m});}
                    }
                    var parts=new List<CombineInstance>();var mats=new List<Material>();
                    foreach(var kv in byMat){var m=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.CombineMeshes(kv.Value.ToArray(),true,true);parts.Add(new CombineInstance{mesh=m});temp.Add(m);mats.Add(PropKit.MaterialFor(kv.Key));}
                    var merged=new Mesh{name=chunk.name+" lod"+lod,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};merged.CombineMeshes(parts.ToArray(),false,false);
                    var go=new GameObject(chunk.name+" lod"+lod,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(chunk.transform,false);
                    go.GetComponent<MeshFilter>().sharedMesh=merged;var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=mats.ToArray();
                    lods.Add(new LOD(lod==0?.06f:.012f,new Renderer[]{r}));
                }
                chunk.AddComponent<LODGroup>().SetLODs(lods.ToArray());
            }
            foreach(var m in temp)UnityEngine.Object.Destroy(m);
            Colliders(L,root.transform);
            L.drawMs=sw.Elapsed.TotalMilliseconds;return root;
        }
        public static GameObject Colliders(RunLayout L,Transform root)
        {
            var col=new GameObject($"Run {L.spec.name} barrier");col.transform.SetParent(root,false);col.tag="Respawn";
            if(L.Deforming){WallColliders(L,col);return col;}
            var feet=L.items.Where(i=>i.piece!=Role(L.kit,"middle")).Select(i=>(Vector3)i.m.GetColumn(3)).ToList();
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
            return col;
        }
        // One mesh collider per wall between piers (the battered section swept every half metre: no seams between pieces
        // for a car to catch on), and each pier's box.
        static void WallColliders(RunLayout L,GameObject col)
        {
            var run=L.kit.file.run;float hb=run.halfBase,ht=run.halfBase*.62f,top=run.height;
            foreach(var path in L.paths) {
                var v=new List<Vector3>();var t=new List<int>();int n=Mathf.Max(1,Mathf.CeilToInt(path.Length/.5f));
                for(int i=0;i<=n;i++) {
                    float s=path.Length*i/n;var P=path.At(s);var R=Right(path.Tangent(s));float b=path.Base(s);
                    v.Add(new Vector3(P.x-R.x*hb,b-.3f,P.z-R.z*hb));v.Add(new Vector3(P.x-R.x*ht,b+top,P.z-R.z*ht));
                    v.Add(new Vector3(P.x+R.x*ht,b+top,P.z+R.z*ht));v.Add(new Vector3(P.x+R.x*hb,b-.3f,P.z+R.z*hb));
                    if(i>0){int a=v.Count-8;for(int f=0;f<3;f++){int p0=a+f,p1=a+f+1,q0=p0+4,q1=p1+4;t.AddRange(new[]{p0,q1,p1,p0,q0,q1});}}
                }
                var m=new Mesh{name=$"Run {L.spec.name} wall collider"};m.SetVertices(v);m.SetTriangles(t,0);
                var g=new GameObject("wall");g.transform.SetParent(col.transform,false);g.tag="Respawn";g.AddComponent<MeshCollider>().sharedMesh=m;
            }
            foreach(var it in L.items) {
                if(it.deform)continue;var pc=L.kit.Piece(it.piece).collision;if(pc==null||pc.size==null||pc.size.V==Vector3.zero)continue;
                var g=new GameObject(it.piece);g.transform.SetParent(col.transform,false);g.tag="Respawn";g.transform.SetPositionAndRotation(it.m.GetColumn(3),it.m.rotation);
                var box=g.AddComponent<BoxCollider>();box.center=pc.centre.V;box.size=pc.size.V;
            }
        }

        public static string Report(RunLayout L)=>L.Deforming
            ? $"{L.spec.name,-24} {L.kit.file.kit}@{L.kit.file.version}  {L.length,6:0.0} m  piers {L.posts,3}  pieces {L.spans,3} ({(L.spans>0?L.minSpan:0):0.00}-{L.maxSpan:0.00} m)  " +
              $"corners {L.corners}  openings {L.openings} (gate piers {L.gatePosts})  solve {L.solveMs:0.0} ms  draw {L.drawMs:0.0} ms  {(L.failures.Count==0?"ok":L.failures.Count+" failure(s)")}" +
              (L.notes.Count>0?"  ("+string.Join("; ",L.notes)+")":"")
            : $"{L.spec.name,-24} {L.kit.file.kit}@{L.kit.file.version}  {L.length,6:0.0} m  posts {L.posts,3}  spans {L.spans,3} ({(L.spans>0?L.minSpan:0):0.00}-{L.maxSpan:0.00} m)  " +
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

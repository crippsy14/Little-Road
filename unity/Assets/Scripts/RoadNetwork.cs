using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Road network (docs/52; Richard, 1 Oct: "a road snapping system... T junctions and splits, SimCity vibes"). Roads are
    // polylines between nodes; a junction is a node where road ends ("arms") meet. Solve() works out, for each pair of
    // neighbouring arms, the rounded corner (a fillet tangent to both kerbs), trims each road back to where its corners
    // begin, and gives the node a junction surface: the polygon through the arm mouths and the corners, at the roads'
    // own heights. Kerb lines follow the corners, so markings meet. Roads the network doesn't own (the Bayview loop, the
    // race circuit) join as external arms: they shape the corners but only the corner pieces and the other roads' mouths
    // are built over them. Pure geometry: the builder (HillcrestValley) makes meshes, the editor checks test it.
    public sealed class RoadNetwork
    {
        public float Fillet=6;
        public sealed class Node
        {
            public Vector3 at;public readonly List<Arm> arms=new();
            public bool HasExternal{get{foreach(var a in arms)if(a.external)return true;return false;}}
            // After Solve: the junction surface (clockwise, around `at`) and its kerbs (for edge lines), and the
            // corner pieces (fans from the kerb corner) for nodes with an external road.
            public (bool straight,float s,float t,Vector3 centre)[] solved;
            public readonly List<Vector3> polygon=new();public readonly List<List<Vector3>> kerbs=new();public readonly List<List<Vector3>> corners=new();
            public float Radius;
            // This junction's own kerb radius (0: the network's Fillet). A circuit's T whose race line turns through it takes the
            // line's radius less half the road, so the kerb follows the race corridor's inside edge (Vinelands' Eira Velha).
            public float fillet;
            public float F(float d)=>fillet>0?fillet:d;
        }
        public sealed class Road{public string name;public Vector3[] line;public float width,lift;public Arm start,end;public bool noCentre;public Vector3[] trimmed;}
        public sealed class Arm
        {
            public Node node;public Road road;public bool atStart,minor,external;public Vector3 dir;public float half,trim;
            public float lift,externalY;
            // The ribbon's own end vertices (after Solve's cut): the junction's mouth uses exactly these, so they meet.
            public Vector3 mouthLeft,mouthRight,endDir;public bool hasMouth;
            public Vector3 MouthLeft=>hasMouth?mouthLeft:Left(trim);public Vector3 MouthRight=>hasMouth?mouthRight:Right(trim);
            // Height of this arm's road surface `s` metres out from the node along it.
            public float Height(float s)=>external?externalY:road.trimmed!=null||road.line!=null?RoadNetwork.HeightAlong(road.line,atStart,s)+lift:node.at.y;
            public Vector3 Left(float s){var r=Vector3.Cross(Vector3.up,dir);var p=node.at+dir*s-r*half;p.y=Height(s);return p;}
            public Vector3 Right(float s){var r=Vector3.Cross(Vector3.up,dir);var p=node.at+dir*s+r*half;p.y=Height(s);return p;}
        }
        public readonly List<Node> nodes=new();public readonly List<Road> roads=new();
        public Node Add(Vector3 at){var n=new Node{at=at};nodes.Add(n);return n;}
        // A road between two nodes (either may be null: a free end). `minorStart`/`minorEnd`: it gives way there.
        public Road Connect(string name,Vector3[] line,float width,float lift,Node from,Node to,bool minorStart=false,bool minorEnd=false,bool noCentre=false)
        {
            var r=new Road{name=name,line=line,width=width,lift=lift,noCentre=noCentre};roads.Add(r);
            if(from!=null){r.start=new Arm{node=from,road=r,atStart=true,minor=minorStart,half=width*.5f,lift=lift};from.arms.Add(r.start);}
            if(to!=null){r.end=new Arm{node=to,road=r,atStart=false,minor=minorEnd,half=width*.5f,lift=lift};to.arms.Add(r.end);}
            return r;
        }
        // A road the network doesn't build, joining at a node (both directions of a through road are two arms).
        public void External(Node node,Vector3 dir,float half,float y){dir.y=0;node.arms.Add(new Arm{node=node,dir=dir.normalized,half=half,external=true,externalY=y});}
        // One long road split at junction nodes along it (snapped to its nearest points), connected in order.
        public List<Road> Through(string name,Vector3[] line,float width,float lift,params Node[] stops)
        {
            var cuts=new List<(int index,Node node)>();
            foreach(var n in stops){int best=0;float bd=float.MaxValue;for(int i=0;i<line.Length;i++){float d=Flat(line[i]-n.at).sqrMagnitude;if(d<bd){bd=d;best=i;}}cuts.Add((best,n));n.at=line[best];}
            cuts.Sort((a,b)=>a.index.CompareTo(b.index));
            var pieces=new List<Road>();int from=0;Node last=null;
            foreach(var (index,node) in cuts){if(index>from)pieces.Add(Connect(name,Slice(line,from,index),width,lift,last,node));from=index;last=node;}
            if(from<line.Length-1)pieces.Add(Connect(name,Slice(line,from,line.Length-1),width,lift,last,null));
            return pieces;
        }
        static Vector3[] Slice(Vector3[] line,int a,int b){var s=new Vector3[b-a+1];System.Array.Copy(line,a,s,0,s.Length);return s;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}

        public void Solve()
        {
            foreach(var n in nodes) {
                foreach(var a in n.arms)if(!a.external){var line=a.road.line;int i=a.atStart?Mathf.Min(line.Length-1,2):Mathf.Max(0,line.Length-3);a.dir=Flat(line[i]-n.at).normalized;if(a.dir==Vector3.zero)a.dir=Flat(a.atStart?line[1]-line[0]:line[^2]-line[^1]).normalized;}
                n.arms.Sort((x,y)=>Bearing(x.dir).CompareTo(Bearing(y.dir)));
                foreach(var a in n.arms)a.trim=0;
                int count=n.arms.Count;if(count<2)continue;
                var corners=new (bool straight,float s,float t,Vector3 centre)[count];
                for(int i=0;i<count;i++) {
                    var A=n.arms[i];var B=n.arms[(i+1)%count];
                    float sweep=Mathf.Repeat(Bearing(B.dir)-Bearing(A.dir),360);
                    if(sweep>170){corners[i]=(true,0,0,default);continue;}   // straight on (or the reflex side): kerbs simply continue
                    Vector3 ra=Vector3.Cross(Vector3.up,A.dir),lb=-Vector3.Cross(Vector3.up,B.dir);
                    // Kerbs offset by the corner radius into the corner; where they cross is the fillet's centre.
                    float F=n.F(Fillet);if(!Cross(A.dir,ra*(A.half+F),B.dir,lb*(B.half+F),out float s,out float t)){corners[i]=(true,0,0,default);continue;}
                    s=Mathf.Max(s,0);t=Mathf.Max(t,0);
                    corners[i]=(false,s,t,n.at+A.dir*s+ra*(A.half+F));
                    A.trim=Mathf.Max(A.trim,s);B.trim=Mathf.Max(B.trim,t);
                }
                // External roads aren't trimmed (the network doesn't build them); a road meeting one starts exactly at its
                // kerb (the corner pieces fill beside its mouth).
                if(n.HasExternal) {
                    foreach(var a in n.arms)a.trim=0;
                    foreach(var a in n.arms)if(!a.external)foreach(var e in n.arms)if(e.external) {
                        float sinAngle=Mathf.Abs(Vector3.Cross(a.dir,e.dir).y);if(sinAngle>.2f)a.trim=Mathf.Max(a.trim,e.half/sinAngle+a.half*Mathf.Abs(Vector3.Dot(a.dir,e.dir))/sinAngle-.4f);   // (0.4 m under its edge: no hairline)
                    }
                }
                n.solved=corners;
            }
            // Each road trimmed back to its junction mouths (measured from the node: a road's own end can sit a metre or
            // three from it, which left the Bayview mainline 3 m short of the loop).
            foreach(var r in roads) {
                float total=Length(r.line);float a=r.start!=null?Mathf.Max(0,r.start.trim-Along(r.start)):0,b=r.end!=null?Mathf.Max(0,r.end.trim-Along(r.end)):0;
                r.trimmed=Cut(r.line,a,Mathf.Max(a+.5f,total-b));
                foreach(var arm in new[]{r.start,r.end}) {
                    if(arm==null)continue;var tr=r.trimmed;bool s=arm.atStart;Vector3 p=s?tr[0]:tr[^1],f=Vector3.zero;
                    for(int k=1;k<tr.Length&&Flat(f).sqrMagnitude<1e-6f;k++)f=s?tr[k]-tr[0]:tr[^1]-tr[^(k+1)];
                    f.y=0;f.Normalize();var rr=Vector3.Cross(Vector3.up,f);
                    Vector3 lv=p-rr*r.width*.5f+Vector3.up*r.lift,rv=p+rr*r.width*.5f+Vector3.up*r.lift;
                    arm.mouthLeft=s?lv:rv;arm.mouthRight=s?rv:lv;arm.endDir=s?f:-f;arm.hasMouth=true;
                }
            }
            foreach(var n in nodes) {
                int count=n.arms.Count;if(count<2)continue;var corners=n.solved;
                // The surface, clockwise: each arm's mouth, then its right kerb into the corner and round to the next arm.
                n.polygon.Clear();n.kerbs.Clear();n.corners.Clear();n.Radius=0;
                for(int i=0;i<count;i++) {
                    var A=n.arms[i];var B=n.arms[(i+1)%count];var c=corners[i];
                    n.polygon.Add(A.MouthLeft);n.polygon.Add(A.MouthRight);
                    var kerb=new List<Vector3>{A.MouthRight};
                    if(c.straight)kerb.Add(B.MouthLeft);
                    else {
                        // The corner on the kerbs as built: each through its ribbon's end vertex along its end direction
                        // (an average approach direction put notches where a curving road met its corner).
                        Vector3 da=A.hasMouth?A.endDir:A.dir,db=B.hasMouth?B.endDir:B.dir;
                        Vector3 pa=A.hasMouth?A.MouthRight:A.Right(0),pb=B.hasMouth?B.MouthLeft:B.Left(0);
                        Vector3 ra=Vector3.Cross(Vector3.up,da),lb=-Vector3.Cross(Vector3.up,db);float F=n.F(Fillet);
                        if(!Cross2(pa+ra*F,da,pb+lb*F,db,out float s,out float u)){kerb.Add(B.MouthLeft);}
                        else {
                            var centre=pa+ra*F+da*s;var ta=pa+da*s;var tb=pb+db*u;ta.y=pa.y;tb.y=pb.y;
                            if(s<-.01f)kerb.Add(ta);
                            var from=Flat(ta-centre).normalized;var to=Flat(tb-centre).normalized;float sweep=Vector3.SignedAngle(from,to,Vector3.up);
                            for(int k=1;k<12;k++){float w=k/12f;var p=centre+Quaternion.Euler(0,sweep*w,0)*from*F;p.y=Mathf.Lerp(ta.y,tb.y,w);kerb.Add(p);}
                            if(u<-.01f)kerb.Add(tb);
                            kerb.Add(B.MouthLeft);
                            // The corner piece: a fan from the kerbs' own corner over the fillet (nodes with an external road).
                            if(Cross2(pa,da,pb,db,out float s0,out _)){var q=pa+da*s0;q.y=(ta.y+tb.y)*.5f;var piece=new List<Vector3>{q};for(int k=1;k<kerb.Count-1;k++)piece.Add(kerb[k]);n.corners.Add(piece);}
                        }
                    }
                    for(int k=1;k<kerb.Count-1;k++)n.polygon.Add(kerb[k]);
                    n.kerbs.Add(kerb);
                }
                foreach(var a in n.arms)n.Radius=Mathf.Max(n.Radius,a.trim+a.half);
                n.Radius+=1;
            }
        }
        // How far along its arm a road's own end already is from the node.
        static float Along(Arm a){var e=a.atStart?a.road.line[0]:a.road.line[^1];return Mathf.Max(0,Vector3.Dot(Flat(e-a.node.at),a.dir));}
        static float Bearing(Vector3 d)=>Mathf.Repeat(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,360);
        // Lines pa+da*s and pb+db*t (2D): where they cross.
        static bool Cross2(Vector3 pa,Vector3 da,Vector3 pb,Vector3 db,out float s,out float t)
        {
            float den=da.x*db.z-da.z*db.x;s=t=0;if(Mathf.Abs(den)<1e-4f)return false;
            float dx=pb.x-pa.x,dz=pb.z-pa.z;s=(dx*db.z-dz*db.x)/den;t=(dx*da.z-dz*da.x)/den;return true;
        }
        // Lines p+da*s+oa and p+db*t+ob (2D): where they cross.
        static bool Cross(Vector3 da,Vector3 oa,Vector3 db,Vector3 ob,out float s,out float t)
        {
            float den=da.x*db.z-da.z*db.x;s=t=0;if(Mathf.Abs(den)<1e-4f)return false;
            float dx=ob.x-oa.x,dz=ob.z-oa.z;s=(dx*db.z-dz*db.x)/den;t=(dx*da.z-dz*da.x)/den;return true;
        }
        public static float Length(Vector3[] line){float d=0;for(int i=1;i<line.Length;i++)d+=Vector3.Distance(line[i],line[i-1]);return d;}
        // The part of a polyline from distance a to distance b, ends interpolated.
        public static Vector3[] Cut(Vector3[] line,float a,float b)
        {
            var o=new List<Vector3>();float run=0;
            for(int i=1;i<line.Length;i++) {
                float seg=Vector3.Distance(line[i-1],line[i]),r0=run,r1=run+seg;run=r1;
                if(r1<a||r0>b)continue;
                if(o.Count==0)o.Add(Vector3.Lerp(line[i-1],line[i],Mathf.Clamp01((a-r0)/Mathf.Max(seg,1e-4f))));
                // (A cut landing exactly on a vertex added it twice: a zero-length first segment had no direction.)
                var add=r1<=b?line[i]:Vector3.Lerp(line[i-1],line[i],Mathf.Clamp01((b-r0)/Mathf.Max(seg,1e-4f)));
                if((add-o[^1]).sqrMagnitude>1e-6f)o.Add(add);
                if(r1>b)break;
            }
            if(o.Count<2)o=new List<Vector3>{line[0],line[^1]};
            return o.ToArray();
        }
        // The polyline's height `s` metres in from one end.
        public static float HeightAlong(Vector3[] line,bool fromStart,float s)
        {
            float run=0;int n=line.Length;
            for(int k=1;k<n;k++){int i=fromStart?k-1:n-k,j=fromStart?k:n-k-1;float seg=Vector3.Distance(line[i],line[j]);if(run+seg>=s)return Mathf.Lerp(line[i].y,line[j].y,(s-run)/Mathf.Max(seg,1e-4f));run+=seg;}
            return fromStart?line[^1].y:line[0].y;
        }
        // Inside a junction (with a margin): shoulders, guardrails and paint keep out.
        public bool NearJunction(Vector3 p,float pad){foreach(var n in nodes)if(n.arms.Count>1&&Flat(p-n.at).magnitude<n.Radius+pad)return true;return false;}
        // On (or within `pad` of) a junction's own surface: its polygon after Solve, or its radius where it has none.
        public bool OnJunction(Vector3 p,float pad)
        {
            foreach(var n in nodes) {
                if(n.arms.Count<2)continue;
                if(n.polygon.Count<3){if(Flat(p-n.at).magnitude<n.Radius+pad)return true;continue;}
                if(Flat(p-n.at).magnitude>n.Radius+pad+4)continue;
                if(Inside(n.polygon,p)||EdgeDistance(n.polygon,p)<pad)return true;
            }
            return false;
        }
        public static bool Inside(List<Vector3> poly,Vector3 p)
        {
            bool inside=false;
            for(int i=0,j=poly.Count-1;i<poly.Count;j=i++){Vector3 a=poly[i],b=poly[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}
            return inside;
        }
        static float EdgeDistance(List<Vector3> poly,Vector3 p)
        {
            float best=float.MaxValue;var q=new Vector2(p.x,p.z);
            for(int i=0,j=poly.Count-1;i<poly.Count;j=i++){Vector2 a=new(poly[j].x,poly[j].z),b=new(poly[i].x,poly[i].z),ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(q-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-6f));best=Mathf.Min(best,(a+ab*t-q).magnitude);}
            return best;
        }
    }
}

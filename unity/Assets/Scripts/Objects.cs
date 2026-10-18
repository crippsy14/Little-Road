using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge's objects (docs/77, step 4): a family of seeded variants (hay bales) placed as data. The spots come from
    // the caller (a field's jittered grid, a row along a hedge); here each spot gets a variant under the family's
    // repetition rules, is turned (a round bale on a slope turns its axis downhill, as a farmer leaves one so it can't
    // roll), tilted to the ground's plane under its contact patch and settled into it; then all are drawn as merged
    // chunks with a box each to hit.
    public struct PlacedObject{public string piece;public int variant;public float yaw,tilt,sink,air;public Vector3 at;public Quaternion rot;}
    public sealed class ObjectLayout
    {
        public string name,seed;public PropKit kit;public bool row;
        public readonly List<RunItem> items=new();
        public readonly List<PlacedObject> placed=new();
        public readonly List<string> failures=new(),notes=new();
        public int forced;public double solveMs,drawMs;
        public string Hash()
        {
            var sb=new StringBuilder();foreach(var it in items){sb.Append(it.piece);for(int i=0;i<16;i++)sb.Append(Mathf.Round(it.m[i]*1000)).Append(',');}
            return Hash128.Compute(sb.ToString()).ToString();
        }
    }

    public static class Objects
    {
        public static readonly List<ObjectLayout> Laid=new();

        // Lay a family at the given spots (x, z and a yaw in degrees). `only` keeps to pieces whose names start with one of
        // its prefixes; `row` adds the along-a-row rules (no variant twice running, no repeat of a two- or three-long run).
        public static ObjectLayout Place(string name,string kit,IList<(float x,float z,float yaw)> spots,string seed,bool row=false,string[] only=null)
        {
            var sw=Stopwatch.StartNew();var k=PropKit.Load(kit);var L=new ObjectLayout{name=name,seed=seed,kit=k,row=row};var run=k.file.run;
            var family=k.file.pieces.Where(p=>p.role=="object"&&(only==null||only.Any(o=>p.name.StartsWith(o)))).ToList();
            var seq=new List<int>();
            for(int i=0;i<spots.Count;i++) {
                var (x,z,yaw0)=spots[i];
                // (the order variants are tried in: this spot's own shuffle)
                var order=family.OrderBy(p=>Hash01($"{seed}/{i}/{p.name}")).ToList();PropPiece pick=null;float pickYaw=0;int best=int.MaxValue;
                foreach(var p in order) {
                    float yaw=Turn(k,p,x,z,yaw0,$"{seed}/{i}");int clash=Clashes(L,p,x,z,yaw,run)+(row?Repeats(seq,p.variant):0);
                    if(clash<best){best=clash;pick=p;pickYaw=yaw;}if(clash==0)break;
                }
                if(best>0){L.forced++;L.notes.Add($"spot {i}: every variant breaks a repetition rule; {pick.name} breaks the fewest ({best})");}
                var placed=Settle(pick,new Vector3(x,0,z),pickYaw);seq.Add(pick.variant);L.placed.Add(placed);
                if(placed.tilt>4)L.failures.Add($"{pick.name} at {x:0},{z:0} sits {placed.tilt:0.0} deg off the ground's plane (at most 4)");
                if(placed.sink<pick.sink[0]-1e-3f||placed.sink>pick.sink[1]+1e-3f)L.failures.Add($"{pick.name} at {x:0},{z:0} sunk {placed.sink*100:0.0} cm (its kit: {pick.sink[0]*100:0}-{pick.sink[1]*100:0})");
                if(placed.air>.02f)L.failures.Add($"{pick.name} at {x:0},{z:0} has a contact {placed.air*100:0.0} cm in the air (at most 2)");
            }
            Items(L);L.solveMs=sw.Elapsed.TotalMilliseconds;return L;
        }
        // A round bale (a piece the kit says rolls) on a slope steeper than the kit's rollSlope turns its axis (local x)
        // downhill, give or take 10 deg; anything else keeps the spot's yaw.
        static float Turn(PropKit k,PropPiece p,float x,float z,float yaw,string seed)
        {
            var run=k.file.run;if(run.rolls==null||!run.rolls.Contains(p.name))return yaw;
            var n=Ground.Normal(x,z,.5f);var down=new Vector2(n.x,n.z);if(down.magnitude/Mathf.Max(n.y,1e-3f)<run.rollSlope)return yaw;
            return Mathf.Atan2(-down.y,down.x)*Mathf.Rad2Deg+(Hash01(seed+"/roll")-.5f)*20;
        }
        // How many of the family's rules a variant would break here: the same variant within sameGap metres at a similar
        // yaw (a bale is the same turned half round, so yaws compare modulo 180).
        static int Clashes(ObjectLayout L,PropPiece p,float x,float z,float yaw,PropRun run)
        {
            int n=0;
            foreach(var o in L.placed) {
                if(o.variant!=p.variant)continue;
                if(new Vector2(o.at.x-x,o.at.z-z).magnitude>=run.sameGap)continue;
                if(YawGap(o.yaw,yaw)<run.sameYaw)n++;
            }
            return n;
        }
        public static float YawGap(float a,float b){float d=Mathf.Abs(Mathf.DeltaAngle(a,b));return Mathf.Min(d,180-d);}
        // Along a row: the variant twice running, or closing a repeat of the last two or three.
        public static int Repeats(List<int> seq,int v)
        {
            int n=seq.Count,r=0;
            if(n>=1&&seq[n-1]==v)r++;
            if(n>=3&&seq[n-2]==v&&seq[n-1]==seq[n-3])r++;
            if(n>=5&&seq[n-3]==v&&seq[n-1]==seq[n-4]&&seq[n-2]==seq[n-5])r++;
            return r;
        }
        // Tilted to the least-squares plane of the ground under the contact samples and lowered until they're sunk by the
        // middle of the kit's sink range on average (twice: the samples move as it tilts).
        public static PlacedObject Settle(PropPiece p,Vector3 at,float yaw)
        {
            var turn=Quaternion.Euler(0,yaw,0);var rot=turn;Vector3 n=Vector3.up;float y=0;var cs=p.contact.Select(c=>c.V).ToArray();
            for(int pass=0;pass<2;pass++) {
                var w=cs.Select(c=>{var q=at+rot*c;return new Vector3(q.x,Ground.Surface(q.x,q.z),q.z);}).ToArray();
                n=Plane(w,at);rot=Quaternion.FromToRotation(Vector3.up,n)*turn;
                float target=(p.sink[0]+p.sink[1])/2,sum=0;foreach(var c in cs){var q=rot*c;sum+=Ground.Surface(at.x+q.x,at.z+q.z)-q.y;}
                y=sum/cs.Length-target;
            }
            float sink=0,air=0;
            foreach(var c in cs){var q=at+rot*c;q.y+=y;float d=Ground.Surface(q.x,q.z)-q.y;sink+=d;air=Mathf.Max(air,-d);}
            return new PlacedObject{piece=p.name,variant=p.variant,yaw=yaw,at=new Vector3(at.x,y,at.z),rot=rot,tilt=Vector3.Angle(rot*Vector3.up,Ground.Normal(at.x,at.z,.5f)),sink=sink/cs.Length,air=air};
        }
        // (the plane y = a x + b z + c through samples, by least squares; with fewer than three or a degenerate set, the
        //  ground's normal at the middle)
        static Vector3 Plane(Vector3[] w,Vector3 at)
        {
            if(w.Length<3)return Ground.Normal(at.x,at.z);
            double sxx=0,sxz=0,szz=0,sx=0,sz=0,sy=0,sxy=0,szy=0;int n=w.Length;
            foreach(var p in w){double x=p.x-at.x,z=p.z-at.z;sxx+=x*x;sxz+=x*z;szz+=z*z;sx+=x;sz+=z;sy+=p.y;sxy+=x*p.y;szy+=z*p.y;}
            // (centred: solve [sxx sxz sx; sxz szz sz; sx sz n] [a b c] = [sxy szy sy])
            double[,] A={{sxx,sxz,sx},{sxz,szz,sz},{sx,sz,n}};double[] B={sxy,szy,sy};
            double det=Det(A);if(Math.Abs(det)<1e-9)return Ground.Normal(at.x,at.z);
            double a=Det(Col(A,0,B))/det,b=Det(Col(A,1,B))/det;
            return new Vector3((float)-a,1,(float)-b).normalized;
        }
        static double Det(double[,] m)=>m[0,0]*(m[1,1]*m[2,2]-m[1,2]*m[2,1])-m[0,1]*(m[1,0]*m[2,2]-m[1,2]*m[2,0])+m[0,2]*(m[1,0]*m[2,1]-m[1,1]*m[2,0]);
        static double[,] Col(double[,] m,int c,double[] v){var o=(double[,])m.Clone();for(int r=0;r<3;r++)o[r,c]=v[r];return o;}
        static float Hash01(string s){var h=Hash128.Compute(s);return (h.GetHashCode()&0x7fffffff)/(float)int.MaxValue;}

        // The layout's matrices, one per placed object (drawn in 40 m squares, with a box each).
        static void Items(ObjectLayout L)
        {
            L.items.Clear();foreach(var o in L.placed)L.items.Add(new RunItem{piece=o.piece,m=Matrix4x4.TRS(o.at,o.rot,Vector3.one)});
        }
        public static GameObject Draw(ObjectLayout L,Transform parent)
        {
            var sw=Stopwatch.StartNew();
            var root=new GameObject("Objects "+L.name);root.transform.SetParent(parent,false);
            Runs.DrawChunks(root.transform,"Objects "+L.name,L.kit,L.items.GroupBy(i=>{var p=i.m.GetColumn(3);return Mathf.FloorToInt(p.x/40)*1000+Mathf.FloorToInt(p.z/40);}),null);
            foreach(var it in L.items) {
                var pc=L.kit.Piece(it.piece).collision;if(pc==null||pc.size==null)continue;
                var g=new GameObject(it.piece);g.transform.SetParent(root.transform,false);g.transform.SetPositionAndRotation(it.m.GetColumn(3),it.m.rotation);
                var box=g.AddComponent<BoxCollider>();box.center=pc.centre.V;box.size=pc.size.V;
            }
            L.drawMs=sw.Elapsed.TotalMilliseconds;return root;
        }

        public static string Report(ObjectLayout L)
        {
            var near=new List<float>();
            for(int i=0;i<L.placed.Count;i++){float d=float.MaxValue;for(int j=0;j<L.placed.Count;j++)if(j!=i&&L.placed[j].variant==L.placed[i].variant)d=Mathf.Min(d,new Vector2(L.placed[j].at.x-L.placed[i].at.x,L.placed[j].at.z-L.placed[i].at.z).magnitude);if(d<float.MaxValue)near.Add(d);}
            near.Sort();
            return $"{L.name,-24} {L.kit.file.kit}@{L.kit.file.version}  {L.placed.Count,3} placed  variants {L.placed.Select(o=>o.variant).Distinct().Count()}  " +
                   $"same variant nearest {(near.Count>0?near[0]:0):0.0} m (median {(near.Count>0?near[near.Count/2]:0):0.0})  tilt {(L.placed.Count>0?L.placed.Max(o=>o.tilt):0):0.0} deg  " +
                   $"sink {(L.placed.Count>0?L.placed.Min(o=>o.sink)*100:0):0.0}-{(L.placed.Count>0?L.placed.Max(o=>o.sink)*100:0):0.0} cm  air {(L.placed.Count>0?L.placed.Max(o=>o.air)*100:0):0.0} cm  forced {L.forced}  " +
                   $"solve {L.solveMs:0.0} ms  draw {L.drawMs:0.0} ms  {(L.failures.Count==0?"ok":L.failures.Count+" failure(s)")}";
        }
    }
}

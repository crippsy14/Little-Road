using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Bramblewick (docs/82): the West Country farm village 1.7 km east of Hillcrest's Golden Acre Farm, the first place on
    // docs/81's list. Behind --bramblewick (off by default) until it's judged.
    //   - The land: a farmland vale at about 34 m, gently rolling, framed by low hills, in place of the valley's east ring of
    //     hills there; a pass through that ring from the farm, where Valley Road South climbs out of the valley.
    //   - Valley Road South: off the farm lane at a T before Golden Acre's gate, over the pass and down into the village, on
    //     to a gate at its far side (closed until Vinelands, as the Kettle Falls road's).
    // The valley's height grid reaches east to x 3500 and south to z -2500 with it (ValleyField), so the vale has its mesh,
    // collider and roads as the valley does.
    public static class Bramblewick
    {
        public static bool Enabled=>enabled??=RaceTrial.Has("--bramblewick");static bool? enabled;
        public static readonly Vector2 Centre=new(2400,-1780);
        public const float GreenY=34,East=3500,South=-2500,FarmFloor=12;   // (FarmFloor: Golden Acre's level, about 11.7 m; not HillcrestRoute.FarmY, which asks this)
        public const string RoadName="Valley Road South";
        // Church Lane (docs/82 step 5): the lanes race's loop out of the village and back. It leaves Valley Road South at a
        // T east of the village, climbs north past the orchard, swings west along the vale's north side behind the church,
        // and comes back down to a T west of the village. The Bramblewick Lanes race runs the village street west to east,
        // then the lane. My call (Richard, 20 Oct: per-circuit widths, decide it yourself): 7.5 m, the width of Valley Road
        // South, so the street and the lane race the same: two abreast, a third only where there's room.
        public const string LaneName="Church Lane";public const float LaneWidth=7.5f;
        public static float LaneW,LaneE;   // (where the lane leaves Valley Road South, metres along it)
        public static Vector3[] Lap;   // (the race's closed line: point 0 is the start line on the village street)
        public static float L1,L2;public static readonly Dictionary<string,Vector2> Marks=new();public static Vector2[] Brook;
        public static Vector3 BridgeAt,BridgeAlong;   // (the humpback's crown, and the lane's direction over it)
        static float LapLength(){float s=0;for(int i=0;i<Lap.Length;i++)s+=Vector3.Distance(Lap[i],Lap[(i+1)%Lap.Length]);return s;}
        // A turtle in the street's frame: straights and arcs (degrees, + left), 2 m steps, with named marks.
        sealed class Turtle
        {
            public readonly List<Vector2> pts=new();public Vector2 at;float h;public readonly Dictionary<string,Vector2> marks=new();
            public Turtle(Vector2 start,float headingDeg){at=start;h=headingDeg*Mathf.Deg2Rad;pts.Add(at);}
            public void S(float len){int n=Mathf.Max(1,Mathf.CeilToInt(len/2));for(int i=0;i<n;i++){at+=new Vector2(Mathf.Cos(h),Mathf.Sin(h))*len/n;pts.Add(at);}}
            public void A(float deg,float radius){float ang=deg*Mathf.Deg2Rad,L=Mathf.Abs(ang)*radius;int n=Mathf.Max(1,Mathf.CeilToInt(L/2));for(int i=0;i<n;i++){h+=ang/n;at+=new Vector2(Mathf.Cos(h-ang/n/2),Mathf.Sin(h-ang/n/2))*L/n;pts.Add(at);}}
            public void M(string name)=>marks[name]=at;
        }
        // The humpback bridge: the deck rises 1.4 m over the brook in 26 m, a smooth crown, its ends on the lane's own grade.
        static void Humpback(Vector3[] line,Vector2 at)
        {
            int c=0;float best=float.MaxValue;for(int i=0;i<line.Length;i++){float d=Vector2.Distance(new(line[i].x,line[i].z),at);if(d<best){best=d;c=i;}}
            int a=c,b=c;float run=0;while(a>0&&run<13){run+=Vector3.Distance(line[a],line[a-1]);a--;}run=0;while(b<line.Length-1&&run<13){run+=Vector3.Distance(line[b],line[b+1]);b++;}
            float ya=line[a].y,yb=line[b].y;
            for(int i=a;i<=b;i++){float t=(i-a)/(float)Mathf.Max(1,b-a);line[i].y=Mathf.Lerp(ya,yb,t)+1.4f*Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*Mathf.PI);}
            BridgeAt=line[c];BridgeAlong=(line[Mathf.Min(b,c+1)]-line[Mathf.Max(a,c-1)]).normalized;
        }
        // The brook's carve: a channel `depth` deep at its line, its banks easing out over `half` metres: a broad combe in the
        // east, where the lane dips through it, narrowing to a stream by the bridge.
        static float BrookCut(float x,float z)
        {
            if(Brook==null)return 0;var p=new Vector2(x,z);float best=float.MaxValue,u=0,run=0,total=0;
            for(int i=1;i<Brook.Length;i++)total+=Vector2.Distance(Brook[i-1],Brook[i]);
            for(int i=1;i<Brook.Length;i++){var a=Brook[i-1];var ab=Brook[i]-a;float L=ab.magnitude;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/(L*L));float d=Vector2.Distance(p,a+ab*t);if(d<best){best=d;u=(run+t*L)/total;}run+=L;}
            float half=Mathf.Lerp(48,9,Mathf.SmoothStep(0,1,u*1.6f)),depth=Mathf.Lerp(4.5f,1.8f,Mathf.SmoothStep(0,1,u*1.6f));
            return best>=half?0:depth*Mathf.SmoothStep(0,1,1-best/half);
        }
        // The crest before Crest Corner: a broad rise of 2.5 m the climb goes over, so the car is light as it turns.
        static float CrestRise(float x,float z)=>Marks.TryGetValue("Crest",out var c)?2.5f*Mathf.SmoothStep(0,1,1-Mathf.Clamp01(Vector2.Distance(new(x,z),c)/70)):0;
        // The pass: from the farm lane, round the north of Golden Acre's pad (150 m about (720, -1262): through it the road ran
        // under the farm lane's end, 2 m below it), east through the ring of hills, down to the green.
        static readonly Vector2[] Pass={new(500,-1100),new(580,-1092),new(690,-1074),new(820,-1084),new(945,-1135),new(1065,-1225),new(1200,-1345),new(1330,-1455),new(1560,-1560),new(1820,-1660),new(2100,-1740),new(2320,-1775)};
        public static Vector3 Tee,Gate,Green;   // (Green: the village green's middle, beside the road through the vale's middle)
        // (the vale, and the pass east of Golden Acre: the village's approach, which it dresses)
        public static Vector3 PubSite;
        // The level ground the village stands on: the green and its church plot, the pub's (ValleyField stamps them).
        public static IEnumerable<(Vector2 at,float inner,float outer,float y)> Pads(){yield return (new Vector2(Green.x,Green.z),48,72,Green.y);yield return (new Vector2(PubSite.x,PubSite.z),9,18,PubSite.y);}
        public static bool Core(float x,float z)=>Vector2.Distance(new(x,z),Centre)<380;   // (the village itself; the rest is the vale)
        public static bool Contains(float x,float z)=>Vector2.Distance(new(x,z),Centre)<950||x>760&&PassDistance(x,z,out _)<200;
        static float Blend(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float PassDistance(float x,float z,out float t)
        {
            float best=float.MaxValue,run=0,bestRun=0,total=0;for(int i=1;i<Pass.Length;i++)total+=Vector2.Distance(Pass[i-1],Pass[i]);
            var p=new Vector2(x,z);
            for(int i=1;i<Pass.Length;i++){var a=Pass[i-1];var ab=Pass[i]-a;float L=ab.magnitude;float u=Mathf.Clamp01(Vector2.Dot(p-a,ab)/(L*L));float d=Vector2.Distance(p,a+ab*u);if(d<best){best=d;bestRun=run+u*L;}run+=L;}
            t=bestRun/total;return best;
        }
        // The race's line: the village street west to east, Church Lane back round. Each T is turned on a curve through the
        // junction (a quadratic from 24 m before the corner to 24 m after it, the corner as its control point), so the line
        // stays on the junction's tarmac; point 0 is 140 m along the street, with the grid's 83 m behind it on the straight.
        static Vector3[] BuildLap(HillcrestRoute.Road road,HillcrestRoute.Road lane,float sW,float sE)
        {
            const float R=24,Step=2;var pts=new List<Vector3>();float L=lane.Length;   // (24 m: at 11 m the corners were too tight to turn at the lanes' speed, study 227)
            void Add(Vector3 p){if(pts.Count==0||Vector3.Distance(pts[^1],p)>.4f)pts.Add(p);}
            void Corner(Vector3 a,Vector3 j,Vector3 b){for(int k=0;k<10;k++){float t=k/10f;Add((1-t)*(1-t)*a+2*(1-t)*t*j+t*t*b);}}
            Corner(lane.At(L-R),road.At(sW),road.At(sW+R));
            for(float s=sW+R;s<sE-R;s+=Step)Add(road.At(s));
            Corner(road.At(sE-R),road.At(sE),lane.At(R));
            for(float s=R;s<L-R;s+=Step)Add(lane.At(s));
            if(Vector3.Distance(pts[^1],pts[0])<.4f)pts.RemoveAt(pts.Count-1);
            int start=0;float best=float.MaxValue;var line=road.At(sW+140);for(int i=0;i<pts.Count;i++){float d=Vector3.Distance(pts[i],line);if(d<best){best=d;start=i;}}
            var lap=new Vector3[pts.Count];for(int i=0;i<pts.Count;i++)lap[i]=pts[(start+i)%pts.Count];return lap;
        }
        // The vale over the valley's land `valley` (ValleyNatural, or Quarry Hills' gap through it).
        public static float Natural(float x,float z,float valley)
        {
            float d=Vector2.Distance(new(x,z),Centre);
            float n1=Mathf.PerlinNoise((x+5100)*.004f,(z+700)*.004f),n2=Mathf.PerlinNoise(x*.017f+31,z*.017f+11),n3=Mathf.PerlinNoise(x*.0025f+9,z*.0025f+3);
            // Farmland: the green at 34 m, the fields rolling ±6 m, rising gently to the vale's rim; then hills round it.
            float farm=GreenY+(n1-.5f)*12+(n2-.5f)*3+10*Blend(350,850,d)-BrookCut(x,z)+CrestRise(x,z);   // (the brook and the crest: the lanes circuit)
            float hills=farm+70*Blend(850,1300,d)*(.6f+.8f*n3);
            float vale=Mathf.Lerp(valley,hills,1-Blend(1250,1500,d));
            // The pass: a broad saddle from the valley floor up to the vale, the hills either side left standing.
            float dp=PassDistance(x,z,out float t);if(dp>260)return vale;
            float floor=Mathf.Lerp(FarmFloor,GreenY,Mathf.SmoothStep(0,1,t))+(n2-.5f)*2;
            return vale<=floor?vale:Mathf.Lerp(floor,vale,Blend(60,250,dp));
        }
        // Valley Road South: a T on the farm lane, the pass, the green, and on to the gate.
        public static List<HillcrestRoute.Road> Roads(Vector3[] farmLane)
        {
            int tee=0;float best=float.MaxValue;for(int i=0;i<farmLane.Length;i++){float d=Vector2.Distance(new(farmLane[i].x,farmLane[i].z),new(480,-1122));if(d<best){best=d;tee=i;}}
            Tee=farmLane[tee];Gate=new Vector3(2780,float.NaN,-2040);
            var via=new List<Vector2>{new(Tee.x+10,Tee.z+17)};via.AddRange(Pass[1..]);   // (square off the lane, north-east, then round the farm)via.Add(new(2520,-1830));via.Add(new(2660,-1930));
            var line=QuarryHills.Profile(Tee,via.ToArray(),Gate,.08f,30,QuarryHills.BackStreetLanding);   // (level off the T for 20 m: a 1 m drop in its first 12 m tilted the junction)
            var road=HillcrestRoute.Make(RoadName,line,7.5f,6,40);
            float sC=road.Nearest(new Vector3(Centre.x,0,Centre.y),out _);var r=road.Right(sC);r.y=0;r.Normalize();if(r.z<0)r=-r;
            Green=road.At(sC)+r*(road.width*.5f+2.2f+26);Green.y=road.At(sC).y+.05f;PubSite=road.At(sC)-r*(road.width*.5f+2.4f+3.6f+3.9f);PubSite.y=road.At(sC).y+.05f;
            // Church Lane: its ends on Valley Road South's own vertices (so the junctions' nodes are exact), 260 m west and
            // 300 m east of the village's middle, past the last cottages either side.
            int Vertex(float s){int best=0;for(int i=0;i<road.line.Length;i++)if(Mathf.Abs(road.run[i]-s)<Mathf.Abs(road.run[best]-s))best=i;return best;}
            int iW=Vertex(sC-260),iE=Vertex(sC+300);LaneW=road.run[iW];LaneE=road.run[iE];
            var fwd=road.At(sC+30)-road.At(sC-30);fwd.y=0;fwd.Normalize();
            Vector3 North(float s){var q=road.Right(s);q.y=0;q.Normalize();return q.z>0?q:-q;}
            // The lane, laid out as a race circuit (my design, Richard 20 Oct: "a track that is fun to drive"), in the street's
            // frame: `a` metres along the street (east), `n` north. Exact straights and arcs, so every corner's radius is
            // designed; the two straights marked L1 and L2 are solved so the lane lands on the west T.
            var O=road.At(sC);Vector2 World(Vector2 f){var p=O+fwd*f.x+r*f.y;return new(p.x,p.z);}
            Vector2 Frame(Vector3 p){var d=p-O;return new(Vector3.Dot(d,fwd),Vector3.Dot(d,r));}
            var start=Frame(road.line[iE]);var up=North(LaneE);float heading=Mathf.Atan2(Vector3.Dot(up,r),Vector3.Dot(up,fwd))*Mathf.Rad2Deg;
            Turtle Lay(float l1,float l2)
            {
                var t=new Turtle(start,heading);
                t.S(40);t.M("Brook Dip");t.A(28,70);t.M("Brook");t.A(-56,60);t.A(28,75);   // (a fast S through the brook's dip)
                t.M("Long Acre");t.S(170);t.M("Crest");t.A(-35,110);t.S(60);   // (the climb, then a fast right taken light over a crest)
                t.M("Rookery Bends");t.A(65,55);t.A(-70,48);t.A(80,45);t.A(-40,60);t.S(40);   // (rhythm: each exit sets up the next)
                t.M("Rim Corner");t.A(90,70);t.M("Top Lane");t.S(l2);   // (the long rim straight, the vale opening out on the left)
                t.M("Zigzag");t.A(180,20);t.S(90);t.A(-180,22);t.S(60);   // (a hairpin pair down the hillside: heavy braking, the overtakes)
                t.M("Orchard Corner");t.A(50,110);t.A(70,62);   // (tightening: an early entry costs the exit)
                t.M("Long Meadow");t.S(l1);t.A(-18,380);t.S(40);   // (a fast downhill sweep across the meadows)
                t.M("Brook Bend");t.A(-25,90);t.M("Mill Lane");t.S(140);t.M("Humpback Bridge");t.S(50);t.M("Green Corner");t.S(25);
                return t;
            }
            {   // (L1, Long Meadow, and L2, Top Lane, run at nearly right angles: two straights solve the closing exactly)
                var target=Frame(road.line[iW]);var e0=Lay(100,100).at;var d1=(Lay(200,100).at-e0)/100;var d2=(Lay(100,200).at-e0)/100;var rr=target-e0;float det=d1.x*d2.y-d1.y*d2.x;
                L1=100+(rr.x*d2.y-rr.y*d2.x)/det;L2=100+(d1.x*rr.y-d1.y*rr.x)/det;
            }
            var laid=Lay(L1,L2);Marks.Clear();foreach(var kv in laid.marks)Marks[kv.Key]=World(kv.Value);
            // The brook: from the hills in the north-east, under the lane at the Brook Dip, west behind the church, under the
            // humpback bridge, to the mill pond. It's carved into the vale (Natural) before the lane takes its heights.
            Vector2 B(float a,float n)=>World(new(a,n));
            Brook=new[]{B(640,170),B(470,130),Marks["Brook"],B(150,140),B(-20,150),B(-170,130),Marks["Humpback Bridge"],B(Frame(new Vector3(Marks["Humpback Bridge"].x,0,Marks["Humpback Bridge"].y)).x-140,60),B(-560,40)};
            var laneVia=new List<Vector2>();for(int i=1;i<laid.pts.Count-1;i++)laneVia.Add(World(laid.pts[i]));
            var laneLine=QuarryHills.Profile(road.line[iE],laneVia.ToArray(),road.line[iW],.11f,20,QuarryHills.BackStreetLanding);
            Humpback(laneLine,Marks["Humpback Bridge"]);
            var lane=HillcrestRoute.Make(LaneName,laneLine,LaneWidth,5,30);
            Lap=BuildLap(road,lane,LaneW,LaneE);
            Debug.Log($"LITTLE_ROAD_BRAMBLEWICK_LANES lane={lane.Length:0} m lap={LapLength():0} m top lane={L2:0} m long meadow={L1:0} m");
            return new List<HillcrestRoute.Road>{road,lane};
        }
    }
}

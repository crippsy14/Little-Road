using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Marks colliders whose surface counts as tarmac for free-roam grip.
    public sealed class RoadSurface:MonoBehaviour{}

    // Every drivable road in the adventure world (Bayview loop, causeway, mainland roads):
    // free-roam recovery returns the car to the nearest of them.
    public static class FreeRoamRoads
    {
        static readonly List<Vector3[]> roads=new();
        public static int Count=>roads.Count;
        public static void Clear()=>roads.Clear();
        public static void Register(Vector3[] centreline){if(centreline!=null&&centreline.Length>1)roads.Add(centreline);}
        public static bool Nearest(Vector3 p,out Vector3 point,out Vector3 forward)
        {
            point=forward=default;float best=float.MaxValue;
            foreach(var road in roads)for(int i=0;i<road.Length-1;i++) {
                Vector3 a=road[i],b=road[i+1],ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));
                Vector3 q=a+ab*t;float d=(q-p).sqrMagnitude;if(d<best){best=d;point=q;forward=ab.normalized;}
            }
            return best<float.MaxValue;
        }
    }

    // Hillcrest Valley road network, river, railway and landmarks. The mainline branches off
    // the Bayview road, crosses the Bayview Halt quay, climbs along the causeway viaduct into
    // the Headland Tunnel and comes out high above the valley; everything else hangs off it.
    public static class HillcrestRoute
    {
        public sealed class Road
        {
            public string name;public Vector3[] line;public float width,inner,outer;
            public float[] run;public float Length=>run[^1];
            public Vector3 At(float d)
            {
                d=Mathf.Clamp(d,0,Length);int lo=0,hi=run.Length-1;
                while(hi-lo>1){int mid=(lo+hi)/2;if(run[mid]<=d)lo=mid;else hi=mid;}
                return Vector3.Lerp(line[lo],line[hi],(d-run[lo])/Mathf.Max(run[hi]-run[lo],1e-4f));
            }
            public Vector3 Forward(float d){var f=At(d+1)-At(d-1);f.y=0;return f.normalized;}
            public Vector3 Right(float d)=>Vector3.Cross(Vector3.up,Forward(d));
            // Run distance of the closest point; lateral is signed (+ right of travel).
            public float Nearest(Vector3 p,out float lateral)
            {
                float best=float.MaxValue,where=0;lateral=0;var q=new Vector2(p.x,p.z);
                for(int i=0;i<line.Length-1;i++) {
                    Vector2 a=new(line[i].x,line[i].z),b=new(line[i+1].x,line[i+1].z),ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(q-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));
                    float d=(a+ab*t-q).sqrMagnitude;if(d<best){best=d;where=Mathf.Lerp(run[i],run[i+1],t);lateral=(ab.x*(q.y-a.y)-ab.y*(q.x-a.x))<0?Mathf.Sqrt(d):-Mathf.Sqrt(d);}
                }
                return where;
            }
        }
        public static Road Make(string name,Vector3[] line,float width,float inner,float outer)
        {
            var r=new Road{name=name,line=line,width=width,inner=inner,outer=outer,run=new float[line.Length]};
            for(int i=1;i<line.Length;i++)r.run[i]=r.run[i-1]+Vector3.Distance(line[i],line[i-1]);return r;
        }

        // ---- Mainline: junction on the Bayview road, the Halt quay, causeway, Headland Tunnel,
        // the descent into the valley, the river bridge.
        public static readonly Vector3 PortalNorth=new(103,20,-450),PortalSouth=new(106,27,-602);
        // (--new-bayview: the mainline begins on the new loop's start straight, 28 m north of the old junction: docs/79)
        static Vector3[] MainlineControl=>NewBayview.On?System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Prepend(System.Linq.Enumerable.Prepend(MainlineBase,NewBayview.LinkHold),NewBayview.LinkTop)):MainlineBase;
        static readonly Vector3[] MainlineBase={
            new(68.3f,0,-83.4f),new(70,0,-94),new(71.2f,0,-114),new(72.4f,0,-138),new(73.4f,0,-160),new(76.5f,2.4f,-198),new(84,7,-250),new(93,11.5f,-315),new(99,15.5f,-375),new(102,18.5f,-422),
            PortalNorth,new(104.5f,23.5f,-526),PortalSouth,
            new(105.5f,27,-624),new(96,26.4f,-644),new(66,24.6f,-660),new(26,21.6f,-672),new(-6,19,-686),new(-22,17.4f,-706),new(-12,15.8f,-728),new(18,13.8f,-742),
            new(58,11.2f,-764),new(86,9,-792),new(99,7.9f,-826),new(103,7.5f,-856),new(106,7.3f,-884),new(110,6.9f,-905)};
        // ---- Quay: the lower town's riverside street.
        static readonly Vector3[] QuayControl={new(-96,6,-893),new(-40,6,-901),new(30,6.4f,-905),new(110,6.9f,-905),new(185,7,-910),new(262,7,-920),new(330,7.2f,-929),new(395,7.4f,-936),new(452,7.6f,-934)};
        // ---- Via Crest: spirals up the town hill to the market square, then on to the castle.
        public static readonly Vector2 HillCentre=new(200,-1110);
        public const float SquareY=26,CastleY=52,SquareTheta=300;
        public static Vector3 Spiral(float theta,float r,float y){float a=theta*Mathf.Deg2Rad;return new Vector3(HillCentre.x+Mathf.Sin(a)*r,y,HillCentre.y+Mathf.Cos(a)*r);}
        public static readonly Vector3 Square=Spiral(SquareTheta,124,SquareY);
        public static readonly Vector3 CastleCourt=new(HillCentre.x,CastleY,HillCentre.y);
        static Vector3[] CrestControl()
        {
            var c=new List<Vector3>{new(262,7,-920),new(263,7.1f,-940)};
            for(float t=30;t<=SquareTheta;t+=15){float f=(t-30)/(SquareTheta-30);c.Add(Spiral(t,175-67*f,7.2f+(SquareY-7.2f)*Mathf.SmoothStep(0,1,Mathf.Min(1,f*1.06f))));}
            for(float t=SquareTheta+15;t<=530;t+=15){float f=(t-SquareTheta)/(530-SquareTheta);c.Add(Spiral(t,108-58*f,SquareY+(CastleY-.6f-SquareY)*Mathf.SmoothStep(0,1,f)));}
            c.Add(Spiral(532,36,CastleY));c.Add(new Vector3(HillCentre.x+2,CastleY,HillCentre.y-14));
            return c.ToArray();
        }
        // ---- To the raceway paddock, the falls lookout, the station.
        public static readonly Vector3 Paddock=new(-330,9,-1050);
        static readonly Vector3[] RacewayControl={new(-96,6,-893),new(-150,6.2f,-925),new(-215,6.6f,-972),new(-262,7.6f,-1018),new(-300,8.5f,-1042),Paddock};
        public static readonly Vector3 Lookout=new(641,7.5f,-683);
        static readonly Vector3[] FallsControl={new(452,7.6f,-934),new(502,7.4f,-908),new(542,7.1f,-866),new(575,7,-812),new(603,7.2f,-756),new(626,7.4f,-716),Lookout};
        public static readonly Vector3 StationYard=new(428,8.8f,-1010);
        static readonly Vector3[] StationControl={new(395,7.4f,-936),new(396,7.6f,-956),new(408,8.0f,-980),StationYard};
        public static readonly Vector3 Vista=new(122,26.9f,-644);
        public static readonly Vector2 BalloonField=new(340,-1310);
        // ---- Golden Acre Farm: the valley's big working farm, south-east of the station, on
        // levelled ground. Layout is in farm-local metres (x along the yaw, z across it).
        public static readonly Vector2 FarmCentre=new(720,-1262);public const float FarmYaw=8,FarmInner=150,FarmOuter=205;
        static float farmY=float.NaN;
        public static float FarmY{get{if(float.IsNaN(farmY)){float sum=0;int n=0;for(int i=-2;i<=2;i++)for(int j=-2;j<=2;j++){sum+=ValleyField.Natural(FarmCentre.x+i*40,FarmCentre.y+j*40);n++;}farmY=sum/n;}return farmY;}}
        public static Vector3 Farm(float x,float z){var p=Quaternion.Euler(0,FarmYaw,0)*new Vector3(x,0,z);return new Vector3(FarmCentre.x+p.x,FarmY,FarmCentre.y+p.z);}
        static Vector3[] FarmLaneControl()
        {
            var gate=Farm(-128,52);var c=new List<Vector3>{StationYard,new(415,0,-1030),new(418,0,-1068),new(452,0,-1106),new(512,0,-1140),new(588,0,-1166),gate,Farm(-100,56)};
            for(int i=1;i<c.Count;i++){var p=c[i];float t=Mathf.InverseLerp(StationYard.x,gate.x,p.x);p.y=i>=c.Count-2?FarmY:Mathf.Lerp(StationYard.y,FarmY,t);c[i]=p;}
            return c.ToArray();
        }

        // ---- River: from the western hills to the falls, then down the gorge to the sea.
        public static readonly Vector3 FallsHead=new(612,1,-652);
        static readonly Vector3[] RiverControl={
            new(-1180,3.4f,-1330),new(-930,3,-1235),new(-710,2.6f,-1128),new(-490,2.2f,-992),new(-290,1.9f,-906),new(-120,1.6f,-864),new(40,1.5f,-852),
            new(150,1.4f,-855),new(262,1.3f,-868),new(360,1.2f,-858),new(452,1.1f,-814),new(522,1,-748),new(574,1,-694),new(600,1,-665),FallsHead,
            new(618,-10.2f,-645),new(632,-10.8f,-626),new(650,-11.2f,-590),new(676,-11.7f,-540),new(700,-12.2f,-488),new(722,-12.4f,-430),new(740,-12.6f,-360)};
        // ---- Railway: a balloon loop at Hillcrest station, then out across the valley on a
        // viaduct, over the gorge below the falls and into the ridge (the hidden return).
        public static readonly Vector2 StationLoop=new(505,-1035);public const float StationLoopRadius=48,RailY=9;
        static Vector3[] RailControl()
        {
            var c=new List<Vector3>{new(560,24,-590),new(592,24,-594),new(626,24,-600),new(662,24,-606),new(705,23.4f,-624),new(748,22,-662),new(784,20.5f,-722),new(800,18.5f,-800),new(792,16,-880),new(752,13.5f,-948),new(680,11,-996),new(610,9.6f,-1026)};
            for(float t=90;t>=-270;t-=18){float a=t*Mathf.Deg2Rad;c.Add(new Vector3(StationLoop.x+Mathf.Sin(a)*StationLoopRadius,RailY,StationLoop.y+Mathf.Cos(a)*StationLoopRadius));}
            return c.ToArray();
        }
        // ---- Hillcrest Raceway v2 (1.69 km, 16 m wide, counter-clockwise round the lake). Built to
        // the usual circuit-design brief: two long straights each ending in a heavy braking zone
        // (the overtaking spots), a mix of slow, medium and fast corners that alternate, an esses
        // sequence, a long corner that tightens, a hairpin switchback, and a fast final sweeper
        // onto the main straight.
        //   Main straight W · T1 90° left (r40) · esses R-L-R (r50) · long tightening left (r106) ·
        //   back straight E · T5 90° left (r41) · kink R · T8 right hairpin (r24) · T9 left
        //   hairpin (r23) · run N · fast final left.
        // Elevation (7.8 m): level pits and straight at 9 m; the esses climb ~3% over Vine Hill's
        // flank to a crest at 14.8 m; the back straight falls gently to T5; the switchback climbs ~4%
        // to the hairpins at 16.8 m, drops ~3% to the left hairpin, and the run north descends to the
        // final sweeper. Context drivers race it with the grade model (see RaceTrial / GradeModel).
        public const float RacewayY=9;
        public static readonly Vector3[] RacewayCircuitControl={
            new(-420,9f,-1084),new(-505,9f,-1085),new(-545,9.2f,-1086),new(-562,9.4f,-1087),new(-574,9.6f,-1092),new(-581,9.9f,-1103),
            new(-584,10.4f,-1124),new(-588,11.5f,-1158),new(-602,12.6f,-1188),new(-600,13.6f,-1222),new(-618,14.4f,-1252),new(-646,14.8f,-1292),
            new(-658,14.4f,-1340),new(-652,13.6f,-1386),new(-634,12.9f,-1418),new(-604,12.3f,-1436),new(-570,11.9f,-1443),new(-490,11.3f,-1445),
            new(-410,10.8f,-1443),new(-335,10.4f,-1441),new(-312,10.3f,-1440),new(-299,10.3f,-1435),new(-291,10.4f,-1424),new(-288,10.9f,-1402),
            new(-280,11.8f,-1372),new(-258,13f,-1350),new(-238,14.3f,-1320),new(-229,15.4f,-1292),new(-222,16.3f,-1263),new(-205,16.8f,-1250),
            new(-188,16.5f,-1262),new(-186,15.8f,-1292),new(-186,14.6f,-1322),new(-180,13.7f,-1350),new(-162,13.2f,-1360),new(-146,13f,-1344),
            new(-146,12.8f,-1312),new(-150,12f,-1260),new(-160,11.1f,-1206),new(-176,10.4f,-1156),new(-206,9.8f,-1113),new(-250,9.3f,-1091),
            new(-310,9.05f,-1084),new(-365,9f,-1084)};
        public static readonly Vector2 Lake=new(-392,-1250);public const float LakeRadius=78,LakeY=5.2f;

        static Road[] roads;static Road rail,river;static Vector3[] circuit,causeway;
        // Hillcrest's Pit Stop: the south side of the quay road just west of the junction, the first
        // thing a visitor from the gatehouse passes. Centre 15 m back from the kerb (forecourt under
        // the wing, pylon at the kerb), frontage to the road. The field levels a pad for it.
        static (Vector3 at,float yaw)? pitStop;
        public static (Vector3 at,float yaw) PitStopSite=>pitStop??=FindPitStopSite();
        static (Vector3,float) FindPitStopSite()
        {
            var quay=Quay;float best=0,bd=float.MaxValue;for(float s=0;s<quay.Length;s+=1){float d=Mathf.Abs(quay.At(s).x-40);if(d<bd){bd=d;best=s;}}
            var at=quay.At(best);var right=quay.Right(best);var south=right.z<0?right:-right;
            var centre=at+south*(quay.width*.5f+15);centre.y=at.y;
            var face=-south;face.y=0;return (centre,Quaternion.LookRotation(face).eulerAngles.y);
        }
        public static Road Mainline=>All()[0];public static Road Quay=>All()[1];public static Road Crest=>All()[2];
        public static Road RacewayAccess=>All()[3];public static Road Falls=>All()[4];public static Road StationSpur=>All()[5];public static Road FarmLane=>All()[6];
        public static Road Rail=>rail??=Make("Hillcrest railway",Dense(RailControl(),2),4,3,9);
        // The line's Bayview end: out of the headland's sea cliff beside the road tunnel, down a
        // sea viaduct east of the causeway and round a stadium-shaped balloon loop. The loop's
        // west straight runs along the Bayview Halt quay: that is the platform.
        public static readonly Vector2 BayLoop=new(124,-142);public const float BayLoopRadius=20,BayLoopStraight=16,BayRailY=0;
        public static Vector2 BayEntry=>new(BayLoop.x+BayLoopRadius,BayLoop.y-BayLoopStraight);
        public static Vector2 BayPlatform=>new(BayLoop.x-BayLoopRadius,BayLoop.y-BayLoopStraight);
        public static readonly Vector3 SeaPortal=new(141,24,-448.5f);
        static Vector3[] SeaRailControl()
        {
            var c=new List<Vector3>{SeaPortal,new(141.5f,23.4f,-430),new(142,21,-380),new(143,16.5f,-320),new(144,11,-262),new(144,5.4f,-216),new(144,1.3f,-184)};
            // Up the east straight, round the north end, down the platform straight, round the south end.
            float r=BayLoopRadius,s=BayLoopStraight,cx=BayLoop.x,cz=BayLoop.y;
            for(float z=-s;z<s;z+=8)c.Add(new Vector3(cx+r,BayRailY,cz+z));
            for(float a=0;a<180;a+=18){float t=a*Mathf.Deg2Rad;c.Add(new Vector3(cx+Mathf.Cos(t)*r,BayRailY,cz+s+Mathf.Sin(t)*r));}
            for(float z=s;z>-s;z-=8)c.Add(new Vector3(cx-r,BayRailY,cz+z));
            for(float a=180;a<=360;a+=18){float t=a*Mathf.Deg2Rad;c.Add(new Vector3(cx+Mathf.Cos(t)*r,BayRailY,cz-s+Mathf.Sin(t)*r));}
            return c.ToArray();
        }
        // Bayview Halt quay: the island pad the mainline crosses, walled on its three sea sides
        // (x 56.3 to 108.7, z -162.7 up to the island at -86). Forecourt, station and platform.
        public const float QuayWest=56.3f,QuayEast=108.7f,QuaySouth=-162.7f,QuayNorth=-86;
        public static bool OnHaltQuay(Vector3 p,float margin)=>p.x>QuayWest-margin&&p.x<QuayEast+margin&&p.z>QuaySouth-margin&&p.z<QuayNorth+margin;
        static Road seaRail;
        public static Road SeaRail=>seaRail??=Make("Bayview sea railway",Dense(SeaRailControl(),2),4,3,9);
        public static Vector3 ValleyPortal=>Rail.line[0];
        // Banks blend over 34 m (was 26): the steep cut read as grey rock walls, a canal, not a river.
        // With Quarry Hills (docs/59) the river rises in a tarn below Quarry Road's crest instead of at the grid's edge.
        public static Road River=>river??=Make("Silver river",Dense(QuarryHills.Enabled?Concat(QuarryHills.RiverHead,RiverControl):RiverControl,4),22,9,34);
        static Vector3[] Concat(Vector3[] a,Vector3[] b){var c=new Vector3[a.Length+b.Length];a.CopyTo(c,0);b.CopyTo(c,a.Length);return c;}
        public static Vector3[] Circuit=>circuit??=DenseClosed(RacewayCircuitControl,1.5f);
        public static Road[] All()=>roads??=BuildRoads();
        static Road[] BuildRoads()
        {
            var list=new List<Road>{
            Make("Hillcrest mainline",Dense(MainlineControl,2),8.5f,6,22),Make("Quay road",Dense(QuayControl,2),8,6,20),Make("Via Crest",Dense(CrestControl(),2),7.5f,6,20),
            Make("Raceway road",Dense(RacewayControl,2),7.5f,6,18),Make("Falls road",Dense(FallsControl,2),7,5,16),Make("Station road",Dense(StationControl,2),7,5,16),
            Make("Farm lane",Dense(FarmLaneControl(),2),6.5f,5,16)};
            // Quarry Hills' roads follow Hillcrest's seven (docs/59): stamped, filled, planted round and bridged the same way.
            if(QuarryHills.Enabled)list.AddRange(QuarryHills.Roads(list[3].line));
            if(Bramblewick.Enabled)list.AddRange(Bramblewick.Roads(list[6].line));   // (Valley Road South, off the farm lane: docs/82)
            return list.ToArray();
        }
        public static Road Find(string name){foreach(var r in All())if(r.name==name)return r;return null;}
        // The island end of the mainline (planting exclusions and the island terrain cut use it).
        public static Vector3[] Causeway=>causeway??=System.Array.FindAll(Mainline.line,p=>p.z>-452);
        public static float PortalNorthRun=>Mainline.Nearest(PortalNorth,out _);
        public static float PortalSouthRun=>Mainline.Nearest(PortalSouth,out _);

        // Catmull-Rom through the control points (x, y and z), resampled every `step` metres.
        public static Vector3[] Dense(Vector3[] c,float step)
        {
            var raw=new List<Vector3>();
            for(int s=0;s<c.Length-1;s++) {
                Vector3 p0=c[Mathf.Max(0,s-1)],p1=c[s],p2=c[s+1],p3=c[Mathf.Min(c.Length-1,s+2)];
                for(int k=0;k<24;k++){float t=k/24f;raw.Add(.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t));}
            }
            raw.Add(c[^1]);return Resample(raw,step,false);
        }
        public static Vector3[] DenseClosed(Vector3[] c,float step)
        {
            var raw=new List<Vector3>();int n=c.Length;
            for(int s=0;s<n;s++) {
                Vector3 p0=c[(s+n-1)%n],p1=c[s],p2=c[(s+1)%n],p3=c[(s+2)%n];
                for(int k=0;k<24;k++){float t=k/24f;raw.Add(.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t));}
            }
            raw.Add(c[0]);return Resample(raw,step,true);
        }
        static Vector3[] Resample(List<Vector3> raw,float step,bool closed)
        {
            var result=new List<Vector3>{raw[0]};float travelled=0,next=step;
            for(int i=1;i<raw.Count;i++) {
                float seg=Vector2.Distance(new(raw[i].x,raw[i].z),new(raw[i-1].x,raw[i-1].z));
                while(travelled+seg>=next){float f=(next-travelled)/Mathf.Max(seg,1e-4f);result.Add(Vector3.Lerp(raw[i-1],raw[i],f));next+=step;}
                travelled+=seg;
            }
            if(closed){if(Vector3.Distance(result[^1],raw[0])<step*.5f)result.RemoveAt(result.Count-1);}
            else if(Vector3.Distance(result[^1],raw[^1])>.05f)result.Add(raw[^1]);
            return result.ToArray();
        }
        public static int Index(Vector3[] line,Vector3 p){int best=0;float bd=float.MaxValue;for(int i=0;i<line.Length;i++){float d=Vector2.Distance(new(line[i].x,line[i].z),new(p.x,p.z));if(d<bd){bd=d;best=i;}}return best;}
        // Bayview road junction to Hillcrest's market square: mainline, quay, Via Crest.
        public static List<Vector3> DriveToSquare()
        {
            var route=new List<Vector3>(Mainline.line);var quay=Quay.line;var crest=Crest.line;
            for(int i=Index(quay,Mainline.line[^1])+1;i<=Index(quay,crest[0]);i++)route.Add(quay[i]);
            for(int i=1;i<=Index(crest,Square);i++)route.Add(crest[i]);
            return route;
        }
        // ...and on up Via Crest to the castle courtyard.
        public static List<Vector3> DriveToCastle()
        {
            var route=DriveToSquare();var crest=Crest.line;for(int i=Index(crest,Square)+1;i<crest.Length;i++)route.Add(crest[i]);return route;
        }
        // Horizontal distance to a centreline, with the road height at the closest point.
        public static float Distance(Vector3[] road,float x,float z,out float roadHeight)
        {
            if(road.Length>65)return SegmentGrid.Of(road).Distance(x,z,out roadHeight);
            float best=float.MaxValue;roadHeight=0;var p=new Vector2(x,z);
            for(int i=0;i<road.Length-1;i++) {
                Vector2 a=new(road[i].x,road[i].z),b=new(road[i+1].x,road[i+1].z),ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));
                float d=(a+ab*t-p).sqrMagnitude;if(d<best){best=d;roadHeight=Mathf.Lerp(road[i].y,road[i+1].y,t);}
            }
            return Mathf.Sqrt(best);
        }
        // A long line's segments in a bounding-box tree (docs/72): the woods, towns and shoulders ask "how far is the river /
        // the railway / this road?" tens of thousands of times at load, and each answer scanned the whole line. The tree finds
        // the nearest segment exactly, near or far, pruning only boxes strictly farther than the best so far; ties go to the
        // lowest segment, as the full scan's did, so every answer is identical. Built once per line, safe from parallel builders.
        sealed class SegmentGrid
        {
            static readonly System.Collections.Concurrent.ConcurrentDictionary<Vector3[],SegmentGrid> trees=new(new Same());
            sealed class Same:IEqualityComparer<Vector3[]>{public bool Equals(Vector3[] a,Vector3[] b)=>ReferenceEquals(a,b);public int GetHashCode(Vector3[] a)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a);}
            public static SegmentGrid Of(Vector3[] line)=>trees.GetOrAdd(line,l=>new SegmentGrid(l));
            struct Node{public Vector2 min,max;public int left,right,start,count;}
            readonly Vector3[] line;readonly int[] order;readonly List<Node> nodes=new();
            SegmentGrid(Vector3[] line)
            {
                this.line=line;order=new int[line.Length-1];for(int i=0;i<order.Length;i++)order[i]=i;Build(0,order.Length);
            }
            Vector2 Lo(int i)=>Vector2.Min(new Vector2(line[i].x,line[i].z),new Vector2(line[i+1].x,line[i+1].z));
            Vector2 Hi(int i)=>Vector2.Max(new Vector2(line[i].x,line[i].z),new Vector2(line[i+1].x,line[i+1].z));
            int Build(int start,int count)
            {
                var node=new Node{min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue),left=-1,right=-1,start=start,count=count};
                for(int k=start;k<start+count;k++){node.min=Vector2.Min(node.min,Lo(order[k]));node.max=Vector2.Max(node.max,Hi(order[k]));}
                int index=nodes.Count;nodes.Add(node);
                if(count>8) {
                    var size=node.max-node.min;bool alongX=size.x>=size.y;
                    System.Array.Sort(order,start,count,Comparer<int>.Create((a,b)=>{float ca=alongX?line[a].x+line[a+1].x:line[a].z+line[a+1].z,cb=alongX?line[b].x+line[b+1].x:line[b].z+line[b+1].z;int c=ca.CompareTo(cb);return c!=0?c:a.CompareTo(b);}));
                    int half=count/2;int l=Build(start,half),r=Build(start+half,count-half);node.left=l;node.right=r;node.count=0;nodes[index]=node;
                }
                return index;
            }
            static float Bound(Vector2 p,in Node n){var d=Vector2.Max(Vector2.zero,Vector2.Max(n.min-p,p-n.max));return d.sqrMagnitude;}
            public float Distance(float x,float z,out float roadHeight)
            {
                float best=float.MaxValue,height=0;int bestI=int.MaxValue;var p=new Vector2(x,z);
                Search(0,p,ref best,ref bestI,ref height);roadHeight=height;return Mathf.Sqrt(best);
            }
            void Search(int index,Vector2 p,ref float best,ref int bestI,ref float height)
            {
                var node=nodes[index];if(Bound(p,in node)>best)return;
                if(node.left<0) {
                    for(int k=node.start;k<node.start+node.count;k++) {
                        int i=order[k];Vector2 a=new(line[i].x,line[i].z),b=new(line[i+1].x,line[i+1].z),ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));
                        float d=(a+ab*t-p).sqrMagnitude;if(d<best||d==best&&i<bestI){best=d;bestI=i;height=Mathf.Lerp(line[i].y,line[i+1].y,t);}
                    }
                    return;
                }
                int first=node.left,second=node.right;if(Bound(p,nodes[second])<Bound(p,nodes[first])){first=node.right;second=node.left;}
                Search(first,p,ref best,ref bestI,ref height);Search(second,p,ref best,ref bestI,ref height);
            }
        }
    }

    // The mainland as one height grid (2.5 m): natural landforms first, then the town hill,
    // lake, pads, roads, railway and river are stamped in. Terrain chunks, placement and the
    // road/rail builders all sample this same field, so nothing floats or sinks.
    public static class ValleyField
    {
        // Hillcrest's grid (x -1100..1300, z -2000..-380); with Quarry Hills (docs/59) it reaches west to -4300 and south to
        // -4000. Cells south of the valley and east of the seam aren't part of either region and stay sea floor.
        public const float Cell=2.5f;
        public static readonly float X0=QuarryHills.Enabled?QuarryHills.West:-1100,Z0=QuarryHills.Enabled?QuarryHills.South:Bramblewick.Enabled?Bramblewick.South:-2000;
        // (--bramblewick: east to x 3500 for Bramblewick's vale, docs/82)
        public static readonly int NX=Mathf.RoundToInt(((Bramblewick.Enabled?Bramblewick.East:1300)-X0)/Cell)+1,NZ=Mathf.RoundToInt((-380-Z0)/Cell)+1;
        // Outside both regions: east of the seam and south of the valley (with a 160 m margin for the chunk row that straddles it).
        public static bool Outside(float x,float z)=>x>=QuarryHills.SeamX&&z<-2160&&!(Bramblewick.Enabled&&x>1000&&z>Bramblewick.South);
        static float[] h,roadDistance;
        static float Blend(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        public static float Raw(int i,int j){if(h==null)Build();return h[Mathf.Clamp(j,0,NZ-1)*NX+Mathf.Clamp(i,0,NX-1)];}
        public static float Height(float x,float z)
        {
            if(h==null)Build();
            float fx=(x-X0)/Cell,fz=(z-Z0)/Cell;
            if(fx<0||fz<0||fx>NX-1||fz>NZ-1)return Natural(x,z);
            int i=Mathf.Min((int)fx,NX-2),j=Mathf.Min((int)fz,NZ-2);float u=fx-i,v=fz-j;
            // Same split as the mesh triangles (a, a+NX, a+1 / a+1, a+NX, a+NX+1).
            float a=h[j*NX+i],b=h[j*NX+i+1],c=h[(j+1)*NX+i],d=h[(j+1)*NX+i+1];
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        public static Vector3 Normal(float x,float z)=>new Vector3(Height(x-1.25f,z)-Height(x+1.25f,z),2.5f,Height(x,z-1.25f)-Height(x,z+1.25f)).normalized;
        // Distance to the nearest stamped road or raceway centreline (1e6 when none is near).
        public static float RoadDistance(float x,float z)
        {
            if(h==null)Build();
            int i=Mathf.Clamp(Mathf.RoundToInt((x-X0)/Cell),0,NX-1),j=Mathf.Clamp(Mathf.RoundToInt((z-Z0)/Cell),0,NZ-1);return roadDistance[j*NX+i];
        }
        // Ridge crest line and the gorge the river cuts through it.
        public static float RidgeZ(float x)=>-525+18*Mathf.Sin(x*.005f+1)+(Mathf.PerlinNoise(x*.006f+300,11)-.5f)*16;
        // Headland bluff: where both tunnels pierce the sea cliff (road x 103, railway x 141).
        static float Bluff(float x)=>Mathf.Exp(-(x-122)*(x-122)/(2*48f*48f));
        const float BluffFaceZ=-452.5f;
        static float GorgeDistance(float x,float z)
        {
            Vector2 a=new(600,-668),b=new(750,-340),p=new(x,z),ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
            return Vector2.Distance(p,a+ab*t)+Mathf.Max(0,-Vector2.Dot(p-a,ab)/ab.magnitude)*2;
        }
        public static float Natural(float x,float z)
        {
            float v=!QuarryHills.Enabled?ValleyNatural(x,z):x<QuarryHills.SeamX?QuarryHills.Natural(x,z):QuarryHills.Gap(x,z,ValleyNatural(x,z));
            return Bramblewick.Enabled&&x>500?Bramblewick.Natural(x,z,v):v;   // (--bramblewick: its vale and the pass to it, docs/82)
        }
        public static float ValleyNatural(float x,float z)
        {
            float n1=Mathf.PerlinNoise((x+900)*.0045f,(z+300)*.0045f),n2=Mathf.PerlinNoise((x+40)*.021f,(z+77)*.021f);
            // Valley floor rising gently south, with soft rolling.
            float valley=4+.011f*Mathf.Max(0,-z-700)+(n2-.5f)*5+(n1-.5f)*6;
            // Ring of hills: south (wind-turbine ridge), west and east.
            float south=150*Blend(1420,1880,-z)*(.65f+.7f*n1);
            float west=120*Blend(720,1060,-x)*(.7f+.6f*Mathf.PerlinNoise(z*.004f,7));
            float east=120*Blend(880,1260,x)*(.7f+.6f*Mathf.PerlinNoise(z*.004f,19));
            // Vine Hill inside the raceway and a few knolls across the valley.
            float vine=9*Mathf.Exp(-((x+565)*(x+565)+(z+1330)*(z+1330))/5000f);
            float knolls=Mathf.Max(0,Mathf.PerlinNoise(x*.0065f+50,z*.0065f+90)-.55f)*60;
            float land=valley+Mathf.Max(south,Mathf.Max(west,east))+vine+knolls;
            // Headland ridge: a broken skyline of downs and saddles, a gentle landward fall and a
            // grassy seaward shoulder down to low sea cliffs. Only the bluff the tunnels pierce is
            // tall and sheer, and there the cliff face stands right behind the portals.
            float zr=RidgeZ(x),d=z-zr,bluff=Bluff(x);
            float ridgeTop=50+(Mathf.PerlinNoise(x*.0055f,40)-.5f)*40+(Mathf.PerlinNoise(x*.021f,12)-.5f)*14;
            ridgeTop=Mathf.Max(ridgeTop,Mathf.Lerp(ridgeTop,60,bluff),58*Mathf.Exp(-(x-560)*(x-560)/(2*50f*50f)));
            float edge=Mathf.Lerp(62+(Mathf.PerlinNoise(x*.013f,77)-.5f)*22,BluffFaceZ-zr,bluff);
            float cliffTop=Mathf.Lerp(3+Mathf.PerlinNoise(x*.017f,5)*13,48,bluff);
            float ridge;
            if(d>0){float s=Mathf.Clamp01(d/edge);ridge=Mathf.Lerp(ridgeTop,cliffTop,s*s*(3-2*s))*(1-Blend(edge,edge+Mathf.Lerp(9,4,bluff),d));}
            else ridge=ridgeTop*(1-Blend(25,150,-d));
            // Knobbly downs: rounded bumps and gullies, stronger on the open slopes.
            ridge+=((Mathf.PerlinNoise(x*.03f+7,z*.03f+3)-.5f)*7*(1-.75f*bluff)+(Mathf.PerlinNoise(x*.011f+2,z*.011f+9)-.5f)*9*(1-bluff))*Mathf.Clamp01(ridge/8);
            ridge*=Blend(26,70,GorgeDistance(x,z));
            float height=Mathf.Max(land,ridge);
            // Sea beyond the cliff foot.
            float foot=edge+Mathf.Lerp(8,3,bluff);
            return Mathf.Lerp(NewBayview.On?NewBayview.SeaFloor:-26,height,1-Blend(foot,foot+12,d));   // (--new-bayview: one deep sea floor all round, docs/80 R1-1)
        }
        public static void Build()
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            h=new float[NX*NZ];roadDistance=new float[NX*NZ];
            // Cooked once per build and world (WorldCache, docs/72): the same field every launch, read back in ~30 ms.
            if(WorldCache.TryRead("valley-field",h,roadDistance)){Debug.Log($"LITTLE_ROAD_VALLEY_FIELD cached cells={h.Length} ms={clock.ElapsedMilliseconds} checksum={Checksum():0.000}");return;}
            System.Threading.Tasks.Parallel.For(0,NZ,j=>{for(int i=0;i<NX;i++){float x=X0+i*Cell,z=Z0+j*Cell;h[j*NX+i]=Outside(x,z)?-40:Natural(x,z);roadDistance[j*NX+i]=1e6f;}});
            // Town hill: a cone matched to the spiral road, flat on top for the castle.
            Stamp(HillcrestRoute.HillCentre,230,(r,old)=>{float cone=r<48?HillcrestRoute.CastleY-.2f:r<108?Mathf.Lerp(HillcrestRoute.CastleY-2,HillcrestRoute.SquareY,(r-48)/60f):r<176?Mathf.Lerp(HillcrestRoute.SquareY,7,(r-108)/68f):Mathf.Lerp(7,old,Blend(176,228,r));return Mathf.Max(old,cone+(Mathf.PerlinNoise(r*.1f,3)-.5f)*1.5f);});
            // Lake in the raceway infield.
            Stamp(HillcrestRoute.Lake,HillcrestRoute.LakeRadius+40,(r,old)=>Mathf.Min(old,Mathf.Lerp(HillcrestRoute.LakeY-2.4f,old,Blend(HillcrestRoute.LakeRadius-6,HillcrestRoute.LakeRadius+34,r))));
            // Flat pads: market square, castle court, paddock, lookout, station, vista, balloon field.
            var sq=HillcrestRoute.Square;Pad(new(sq.x,sq.z),34,50,HillcrestRoute.SquareY-.12f);
            Pad(HillcrestRoute.HillCentre,42,58,HillcrestRoute.CastleY-.12f);
            var pd=HillcrestRoute.Paddock;Pad(new(pd.x-10,pd.z+4),34,48,pd.y-.12f);
            var lk=HillcrestRoute.Lookout;Pad(new(lk.x,lk.z),12,22,lk.y-.12f);
            var st=HillcrestRoute.StationYard;Pad(new(st.x+4,st.z-22),30,44,st.y-.12f);
            Pad(HillcrestRoute.StationLoop,HillcrestRoute.StationLoopRadius+6,HillcrestRoute.StationLoopRadius+34,HillcrestRoute.RailY-.4f);
            var vs=HillcrestRoute.Vista;Pad(new(vs.x,vs.z),13,24,vs.y-.12f);
            var bf=HillcrestRoute.BalloonField;Pad(bf,60,110,Natural(bf.x,bf.y));
            var ps=HillcrestRoute.PitStopSite.at;Pad(new(ps.x,ps.z),22,34,ps.y-.12f);
            Pad(HillcrestRoute.FarmCentre,HillcrestRoute.FarmInner,HillcrestRoute.FarmOuter,HillcrestRoute.FarmY-.1f);
            if(QuarryHills.Enabled)foreach(var (padAt,padIn,padOut,padY) in QuarryHills.Pads())Pad(padAt,padIn,padOut,padY-.12f);
            // (--bramblewick: level ground under Valley Road South's T, so its corners meet the grass: the audit's two lips)
            if(Bramblewick.Enabled){HillcrestRoute.All();var t=Bramblewick.Tee;Pad(new(t.x,t.z),16,30,t.y-.08f);}
            var before=(float[])h.Clone();
            // Roads and the raceway flatten their corridors; the railway only cuts through high ground.
            var mainline=HillcrestRoute.Mainline;int portalN=Index(mainline,HillcrestRoute.PortalNorth),portalS=Index(mainline,HillcrestRoute.PortalSouth);
            var nearest=new float[NX*NZ];var nearestY=new float[NX*NZ];var inner=new float[NX*NZ];var outer=new float[NX*NZ];
            for(int k=0;k<nearest.Length;k++)nearest[k]=1e6f;
            // (Load time, docs/60: the segments are gathered, then stamped row by row on every core; see Rows.)
            var corridors=new List<(Vector3 p,Vector3 q,float a,float b)>();
            void Corridor(Vector3[] line,int from,int to,float a,float b,bool closed)
            {
                int n=line.Length;
                for(int s=from;s<to;s++){if(!closed&&s+1>=n)break;corridors.Add((line[s],line[(s+1)%n],a,b));}
            }
            Corridor(mainline.line,portalS,mainline.line.Length-1,mainline.inner,mainline.outer,false);
            foreach(var road in HillcrestRoute.All())if(road!=mainline)Corridor(road.line,0,road.line.Length-1,road.inner,road.outer,false);
            Corridor(HillcrestRoute.Circuit,0,HillcrestRoute.Circuit.Length,11,32,true);
            Rows(corridors.Count,s=>(corridors[s].p,corridors[s].q,corridors[s].b),(k,d,t,s)=>{var c=corridors[s];if(d<nearest[k]){nearest[k]=d;nearestY[k]=Mathf.Lerp(c.p.y,c.q.y,t);inner[k]=c.a;outer[k]=c.b;}if(d<roadDistance[k])roadDistance[k]=d;});
            for(int k=0;k<h.Length;k++)if(nearest[k]<outer[k])h[k]=Mathf.Lerp(h[k],nearestY[k]-.14f,1-Blend(inner[k],outer[k],nearest[k]));
            // Embankments (Richard, 30 Sep: roads you can't get back onto after leaving them). The corridors above blend
            // to the ground over a fixed width however high the road runs, so the mainline's descent from the tunnel sat
            // on a ridge with 55-66° banks up to 16 m deep. Where a road runs above the ground its fill now spreads until
            // the bank is about 1 in 4 (under ~21° at the steepest), like a real road on an embankment: only ever
            // raising the ground, measured from the natural ground. Not the raceway (its run-off is designed), the town
            // hill, the lake or the gorge.
            var fillNearest=new float[NX*NZ];var fillY=new float[NX*NZ];var fillInner=new float[NX*NZ];
            for(int k=0;k<fillNearest.Length;k++)fillNearest[k]=1e6f;
            var fills=new List<(Vector3 p,Vector3 q,float a)>();
            void Fill(Vector3[] line,int from,int to,float a){for(int s=from;s<to&&s+1<line.Length;s++)fills.Add((line[s],line[s+1],a));}
            Fill(mainline.line,portalS,mainline.line.Length-1,mainline.inner);
            foreach(var road in HillcrestRoute.All())if(road!=mainline)Fill(road.line,0,road.line.Length-1,road.inner);
            Rows(fills.Count,s=>(fills[s].p,fills[s].q,FillReach),(k,d,t,s)=>{var f=fills[s];if(d<fillNearest[k]){fillNearest[k]=d;fillY[k]=Mathf.Lerp(f.p.y,f.q.y,t);fillInner[k]=f.a;}});
            for(int j=0;j<NZ;j++)for(int i=0;i<NX;i++) {
                int k=j*NX+i;if(fillNearest[k]>=FillReach)continue;
                float x=X0+i*Cell,z=Z0+j*Cell;
                if(Vector2.Distance(new(x,z),HillcrestRoute.HillCentre)<230||Vector2.Distance(new(x,z),HillcrestRoute.Lake)<HillcrestRoute.LakeRadius+30||GorgeDistance(x,z)<60)continue;
                float road=fillY[k]-.14f,rise=road-before[k];if(rise<=.5f)continue;
                float reach=Mathf.Min(FillReach,fillInner[k]+rise/FillSlope);if(fillNearest[k]>=reach)continue;
                h[k]=Mathf.Max(h[k],Mathf.Lerp(before[k],road,1-Blend(fillInner[k],reach,fillNearest[k])));
            }
            // Quarry Hills' junctions: within 24 m of each, the ground stays 0.2 m under the lowest road near it (the corridors
            // follow whichever road is nearest, so a junction's surface, which blends its roads' heights, could be buried).
            if(QuarryHills.Enabled)foreach(var node in QuarryHills.JunctionPoints()) {
                int i0=Mathf.Max(0,(int)((node.x-24-X0)/Cell)),i1=Mathf.Min(NX-1,(int)((node.x+24-X0)/Cell)+1),j0=Mathf.Max(0,(int)((node.z-24-Z0)/Cell)),j1=Mathf.Min(NZ-1,(int)((node.z+24-Z0)/Cell)+1);
                var near=new List<HillcrestRoute.Road>();foreach(var road in HillcrestRoute.All())if(HillcrestRoute.Distance(road.line,node.x,node.z,out _)<40)near.Add(road);
                for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++) {
                    float x=X0+i*Cell,z=Z0+j*Cell;float r=Mathf.Sqrt((x-node.x)*(x-node.x)+(z-node.z)*(z-node.z));if(r>24)continue;
                    float low=float.MaxValue;foreach(var road in near){float d=HillcrestRoute.Distance(road.line,x,z,out float ry);if(d<road.width*.5f+4)low=Mathf.Min(low,ry-.2f);}
                    if(low<float.MaxValue){int k=j*NX+i;h[k]=Mathf.Lerp(Mathf.Min(h[k],low),h[k],Blend(18,24,r));}
                }
            }
            // Causeway approach: keep the cliff foot below the deck; railway cuttings.
            Cut(mainline.line,0,portalN,7,14,4);
            Cut(HillcrestRoute.Rail.line,0,HillcrestRoute.Rail.line.Length-1,3.5f,11,.35f);
            Cut(HillcrestRoute.SeaRail.line,0,HillcrestRoute.SeaRail.line.Length-1,3.5f,11,.35f);
            // River bed (after roads: bridges span it).
            var river=HillcrestRoute.River.line;
            for(int s=0;s<river.Length-1;s++){Vector3 p=river[s],q=river[s+1];Segment(p,q,36,(k,d,t)=>{float bed=Mathf.Lerp(p.y,q.y,t)-2.6f;h[k]=Mathf.Min(h[k],Mathf.Lerp(bed,h[k],Blend(8,30,d)));});}
            // Gorge below the falls: two light blur passes take the 2.5 m grid's zig-zag off the
            // carved walls' top edges without changing the gorge's shape.
            int gi0=(int)((540-X0)/Cell),gi1=(int)((790-X0)/Cell),gj0=(int)((-720-Z0)/Cell),gj1=NZ-2;
            for(int pass=0;pass<2;pass++) {
                var copy=(float[])h.Clone();
                for(int j=gj0;j<=gj1;j++)for(int i=gi0;i<=gi1;i++) {
                    float x=X0+i*Cell,z=Z0+j*Cell;float g=GorgeDistance(x,z);if(g>80)continue;int k=j*NX+i;
                    float sum=0;for(int dj=-1;dj<=1;dj++)for(int di=-1;di<=1;di++)sum+=copy[k+dj*NX+di]*(di==0&&dj==0?4:di==0||dj==0?2:1);
                    h[k]=Mathf.Lerp(copy[k],sum/16,.7f*(1-Blend(55,80,g)));
                }
            }
            // Headland Tunnel: solid rock over the bore. Each mouth is a shelf just above the bore
            // (the portal kit's opening) set back behind the kit, then the full face rises behind.
            Vector3 n0=HillcrestRoute.PortalNorth,s0=HillcrestRoute.PortalSouth;Vector2 axis=new(s0.x-n0.x,s0.z-n0.z);float length=axis.magnitude;axis/=length;
            for(int j=0;j<NZ;j++)for(int i=0;i<NX;i++) {
                float x=X0+i*Cell,z=Z0+j*Cell;Vector2 rel=new(x-n0.x,z-n0.z);float along=Vector2.Dot(rel,axis),lateral=Mathf.Abs(axis.x*rel.y-axis.y*rel.x);
                if(along<-2||along>length+2||lateral>48)continue;
                float inside=Mathf.Min(along,length-along);
                float road=Mathf.Lerp(n0.y,s0.y,Mathf.Clamp01(along/length));
                float mask=(1-Blend(22,44,lateral))*Mathf.Clamp01(inside/2.5f),roof=road+(inside<5?PortalShelf:19);
                int k=j*NX+i;float target=inside<5&&lateral<9?roof:Mathf.Max(roof,before[k]);
                h[k]=Mathf.Max(h[k],Mathf.Lerp(h[k],target,mask));
            }
            // Railway mouths: the cuttings stop at a clean rock face behind each portal.
            Face(before,HillcrestRoute.SeaRail.line[0],-HillcrestRoute.SeaRail.Forward(0),14);
            Face(before,HillcrestRoute.Rail.line[0],-HillcrestRoute.Rail.Forward(0),14);
            // The Bayview gatehouse bastions (x 77 to 155) stand in deep water: no rock foot in front.
            for(int j=0;j<NZ;j++)for(int i=0;i<NX;i++) {
                float x=X0+i*Cell,z=Z0+j*Cell;if(z<-451||x<66||x>166)continue;
                float m=Blend(-451,-446.5f,z)*(1-Blend(0,8,Mathf.Max(74-x,x-158,0)));
                int k=j*NX+i;h[k]=Mathf.Min(h[k],Mathf.Lerp(h[k],-24,m));
            }
            WorldCache.Write("valley-field",h,roadDistance);
            double check=Checksum();Debug.Log($"LITTLE_ROAD_VALLEY_FIELD cells={h.Length} ms={clock.ElapsedMilliseconds} checksum={check:0.000}");
        }
        static double Checksum(){double check=0;for(int k=0;k<h.Length;k+=7)check+=h[k]+(roadDistance[k]<1e5f?roadDistance[k]:0);return check;}
        const float FillSlope=.25f,FillReach=80;
        // Portal shelf height above the road: just over the bore (the terrain hole ends below it).
        public const float PortalShelf=10;
        static void Face(float[] before,Vector3 mouth,Vector3 inward,float halfWidth)
        {
            Vector2 axis=new Vector2(inward.x,inward.z).normalized;
            for(int j=0;j<NZ;j++)for(int i=0;i<NX;i++) {
                float x=X0+i*Cell,z=Z0+j*Cell;if(Mathf.Abs(x-mouth.x)>70||Mathf.Abs(z-mouth.z)>70)continue;
                Vector2 rel=new(x-mouth.x,z-mouth.z);float along=Vector2.Dot(rel,axis),lateral=Mathf.Abs(axis.x*rel.y-axis.y*rel.x);
                if(along<0||along>40||lateral>halfWidth+12)continue;
                int k=j*NX+i;float mask=Mathf.Clamp01(along/2.5f)*(1-Blend(halfWidth,halfWidth+12,lateral));
                h[k]=Mathf.Max(h[k],Mathf.Lerp(h[k],before[k],mask));
            }
        }
        static int Index(HillcrestRoute.Road road,Vector3 p){int best=0;float bd=float.MaxValue;for(int i=0;i<road.line.Length;i++){float d=Vector2.Distance(new(road.line[i].x,road.line[i].z),new(p.x,p.z));if(d<bd){bd=d;best=i;}}return best;}
        // Visits the grid cells within `reach` of segment p-q: (cell index, distance, t along).
        static void Segment(Vector3 p,Vector3 q,float reach,System.Action<int,float,float> visit)
        {
            int i0=Mathf.Max(0,(int)((Mathf.Min(p.x,q.x)-reach-X0)/Cell)),i1=Mathf.Min(NX-1,(int)((Mathf.Max(p.x,q.x)+reach-X0)/Cell)+1);
            int j0=Mathf.Max(0,(int)((Mathf.Min(p.z,q.z)-reach-Z0)/Cell)),j1=Mathf.Min(NZ-1,(int)((Mathf.Max(p.z,q.z)+reach-Z0)/Cell)+1);
            Vector2 A=new(p.x,p.z),AB=new(q.x-p.x,q.z-p.z);float len2=Mathf.Max(AB.sqrMagnitude,1e-4f);
            for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++) {
                Vector2 P=new(X0+i*Cell,Z0+j*Cell);float t=Mathf.Clamp01(Vector2.Dot(P-A,AB)/len2);float d=Vector2.Distance(P,A+AB*t);
                if(d<=reach)visit(j*NX+i,d,t);
            }
        }
        // Segment for many segments at once, on every core: each grid row is one thread's, and within a row the segments are
        // visited in their order, so every cell sees the same sequence as one Segment call after another (same result).
        static void Rows(int count,System.Func<int,(Vector3 p,Vector3 q,float reach)> segment,System.Action<int,float,float,int> visit)
        {
            var rows=new List<int>[NZ];var spans=new (int i0,int i1,Vector2 A,Vector2 AB,float len2,float reach)[count];
            for(int s=0;s<count;s++) {
                var (p,q,reach)=segment(s);
                int i0=Mathf.Max(0,(int)((Mathf.Min(p.x,q.x)-reach-X0)/Cell)),i1=Mathf.Min(NX-1,(int)((Mathf.Max(p.x,q.x)+reach-X0)/Cell)+1);
                int j0=Mathf.Max(0,(int)((Mathf.Min(p.z,q.z)-reach-Z0)/Cell)),j1=Mathf.Min(NZ-1,(int)((Mathf.Max(p.z,q.z)+reach-Z0)/Cell)+1);
                var AB=new Vector2(q.x-p.x,q.z-p.z);spans[s]=(i0,i1,new Vector2(p.x,p.z),AB,Mathf.Max(AB.sqrMagnitude,1e-4f),reach);
                for(int j=j0;j<=j1;j++)(rows[j]??=new List<int>()).Add(s);
            }
            System.Threading.Tasks.Parallel.For(0,NZ,j=>{
                var list=rows[j];if(list==null)return;
                foreach(int s in list) {
                    var (i0,i1,A,AB,len2,reach)=spans[s];
                    for(int i=i0;i<=i1;i++){Vector2 P=new(X0+i*Cell,Z0+j*Cell);float t=Mathf.Clamp01(Vector2.Dot(P-A,AB)/len2);float d=Vector2.Distance(P,A+AB*t);if(d<=reach)visit(j*NX+i,d,t,s);}
                }
            });
        }
        static void Stamp(Vector2 centre,float radius,System.Func<float,float,float> shape)
        {
            int i0=Mathf.Max(0,(int)((centre.x-radius-X0)/Cell)),i1=Mathf.Min(NX-1,(int)((centre.x+radius-X0)/Cell)+1);
            int j0=Mathf.Max(0,(int)((centre.y-radius-Z0)/Cell)),j1=Mathf.Min(NZ-1,(int)((centre.y+radius-Z0)/Cell)+1);
            for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++){float r=Vector2.Distance(new(X0+i*Cell,Z0+j*Cell),centre);if(r<=radius)h[j*NX+i]=shape(r,h[j*NX+i]);}
        }
        static void Pad(Vector2 centre,float inner,float outer,float y)=>Stamp(centre,outer,(r,old)=>Mathf.Lerp(y,old,Blend(inner,outer,r)));
        static void Cut(Vector3[] line,int from,int to,float a,float b,float depth)
        {
            for(int s=from;s<to&&s+1<line.Length;s++){Vector3 p=line[s],q=line[s+1];Segment(p,q,b,(k,d,t)=>h[k]=Mathf.Min(h[k],Mathf.Lerp(Mathf.Lerp(p.y,q.y,t)-depth,h[k],Blend(a,b,d))));}
        }
    }
}

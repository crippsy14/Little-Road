using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Quarry Hills (docs/59): slate country west of Hillcrest, over the valley's west hills. "The mountain the town
    // dug": a terraced town on the slope above the sea, the quarry floor, twelve 15 m galleries cut round the Glas pit
    // lake, slate tips, moor and forest. Behind --quarry-hills while it's built (CLAUDE.md: new behaviour behind a flag).
    //
    // It extends the mainland's one height grid (ValleyField) west and south, so the terrain, roads, embankments,
    // planting and audits all work on it exactly as on Hillcrest. Natural() here is the landform west of x -1100; it
    // blends into the valley's own west hills over 320 m, and Hillcrest's ground (x >= -1100) is unchanged.
    public static class QuarryHills
    {
        // Part of the game since 4 October (Richard: one build with everything); --no-quarry-hills leaves it out (A/B, old studies).
        public static bool Enabled=>enabled??=!RaceTrial.Has("--no-quarry-hills");static bool? enabled;
        public const float SeamX=-1100,West=-4300,South=-4000;
        // The region's map outline (world x/z).
        public static bool Contains(float x,float z)=>Enabled&&x<SeamX&&x>=West&&z>=South;

        // ---------------------------------------------------------------- landmarks (world x/z, heights in world y)
        public static readonly Vector2 Square=new(-2600,-2350);           // town square (Institute, chapel)
        public static readonly Vector2 Quay=new(-3080,-1745);public const float QuayY=-8.8f; // Slate Quay
        // Slate Quay's apron: a rectangle of made ground along the coast, 190 m long, from 48 m out into the sea to 26 m
        // inland of the waterline (which lies ~38 m inland of the coast polyline), walled on every side. Local axes: u along
        // the coast, v inland.
        public const float QuayHalf=95,QuaySea=48,QuayLand=26;
        public static Vector2 QuayInland=>quayInland??=new Vector2(CoastDistance(Quay.x+5,Quay.y)-CoastDistance(Quay.x-5,Quay.y),CoastDistance(Quay.x,Quay.y+5)-CoastDistance(Quay.x,Quay.y-5)).normalized;static Vector2? quayInland;
        public static Vector2 QuayAlong=>new(QuayInland.y,-QuayInland.x);
        public static Vector2 QuayCentre=>quayCentre??=Quay-QuayInland*(CoastDistance(Quay.x,Quay.y)-38);static Vector2? quayCentre;
        public static void QuayLocal(float x,float z,out float u,out float v){var d=new Vector2(x,z)-QuayCentre;u=Vector2.Dot(d,QuayAlong);v=Vector2.Dot(d,QuayInland);}
        public static Vector3 QuayPoint(float u,float v,float y){var p=QuayCentre+QuayAlong*u+QuayInland*v;return new Vector3(p.x,y,p.y);}
        // Inside the apron (on the land side 3 m past the walls, so the grid's step lies behind them, under the coping).
        public static bool OnQuay(float u,float v,float margin=0){float side=v>=0?3:0;return Mathf.Abs(u)<QuayHalf+side+margin&&v>-QuaySea-margin&&v<QuayLand+3+margin;}
        public static readonly Vector2 FloorCentre=new(-2330,-2980);public const float FloorY=45,FloorRX=330,FloorRZ=210;
        public static readonly Vector2 Pit=new(-1900,-3380);public const float PitRX=380,PitRZ=230,LakeY=FloorY-14,LakeBed=6; // the bed above sea level (the sea plane showed through the clear water)
        public const int Benches=12;public const float BenchHeight=15,BenchWidth=24;
        public static float RimY=>FloorY+Benches*BenchHeight;
        public static readonly Vector2 Source=new(-1600,-1680);public const float SourceY=10.5f,SourceRadius=46; // the river's tarn
        public static readonly Vector2 Crest=new(-1785,-1945);            // Quarry Road's crest: the reveal
        // The slate tips: centre, flat-top height, top radius, base radius.
        // Slate tips: long flat-topped fingers of waste, tipped from rail lines run out from the quarry floor, with straight
        // spoil slopes (about 36 degrees) and scalloped edges where each wagon load ran down. Segment, top height, half width.
        public static readonly (Vector2 a,Vector2 b,float top,float half)[] Tips={
            (new(-2640,-2990),new(-3260,-2850),96,40),(new(-2610,-3160),new(-3010,-3410),88,32),(new(-2470,-3240),new(-2690,-3450),78,24)};
        public const float TipSlope=.73f;
        // The tips' own surface (very low far from any tip); the ground is the higher of it and the land.
        public static float TipSurface(float x,float z)
        {
            float best=-1e4f,scallop=(Mathf.PerlinNoise(x*.035f+11,z*.035f+3)-.5f)*16;
            foreach(var t in Tips){float d=SegDist(x,z,t.a,t.b,out _)+scallop;best=Mathf.Max(best,t.top-Mathf.Max(0,d-t.half)*TipSlope);}
            return best;
        }
        // Whether the ground at (x,z) is a tip's (within `margin` metres of one's foot).
        public static bool OnTip(float x,float z,float margin=0)=>TipSurface(x,z)+margin*TipSlope>=ValleyField.Height(x,z)-.6f;
        // Bare worked ground where no grass or verge tuft grows: the quarry floor, the pit and its galleries, the tips and
        // Slate Quay's apron.
        public static bool Bare(float x,float z)
        {
            if(!Enabled||!Contains(x,z))return false;
            if(FloorQ(x,z)<1.1f)return true;
            PitMetric(x,z,out float q,out float e,out float mask);if(q<1.03f||mask>.4f&&e<Benches*BenchWidth+8)return true;
            QuayLocal(x,z,out float u,out float v);if(OnQuay(u,v,4))return true;
            return OnTip(x,z,3);
        }
        // The twelve slate carvings (docs/59): carved slate plaques hidden round the district, each where a car can reach it
        // (on the gallery faces themselves once the Gallery Climb is built). Ground positions; the keepsake floats above.
        public static Vector3[] CarvingSpots()
        {
            Vector3 G(Vector2 p){return new Vector3(p.x,ValleyField.Height(p.x,p.y),p.y);}
            Vector3 Off(HillcrestRoute.Road r,float s,float side){var p=r.At(s)+r.Right(s)*side;return G(new Vector2(p.x,p.z));}
            var quarry=HillcrestRoute.Find(QuarryRoadName);var haul=HillcrestRoute.Find(HaulRoadName);var kettle=HillcrestRoute.Find(KettleRoadName);var high=HillcrestRoute.Find(HighStreetName);
            var toFloor=(FloorCentre-Pit).normalized;var ledge=PitPoint(toFloor,9);var tip=Tips[0];
            return new[]{
                Off(quarry,quarry.Nearest(new Vector3(Crest.x,0,Crest.y),out _),-14),
                G(Source+new Vector2(0,-(SourceRadius+12))),
                G(Square+new Vector2(0,8)),
                QuayPoint(76,-24,QuayY),
                G(new Vector2(QuarryFloor.School.x,QuarryFloor.School.z+16)),
                G(tip.b+(tip.a-tip.b).normalized*90),
                Off(haul,haul.Length*.55f,9),
                new Vector3(ledge.x,FloorY,ledge.z),
                Off(kettle,kettle.Length-22,0),
                G(QuayCentre+QuayAlong*(-QuayHalf-30)+QuayInland*20),
                G(Tips[2].a+new Vector2(30,25)),
                Off(high,high.Length-8,-7)};
        }
        // The town's road junctions (ValleyField keeps the ground below the roads round them).
        public static IEnumerable<Vector3> JunctionPoints()
        {
            yield return TeeOnRaceway;yield return new Vector3(Square.x,SquareY,Square.y);yield return HillcrestRoute.Find(HighStreetName).line[^1];
            yield return ChapelFrom;yield return ChapelTo;yield return TerraceFrom;yield return TerraceTo;
        }
        // How far into the quarry floor's ellipse (1 at its edge).
        public static float FloorQ(float x,float z)=>Mathf.Sqrt(Mathf.Pow((x-FloorCentre.x)/FloorRX,2)+Mathf.Pow((z-FloorCentre.y)/FloorRZ,2));
        public static Rect TipBox(int i){var t=Tips[i];float r=t.half+t.top/TipSlope*.8f;return Rect.MinMaxRect(Mathf.Min(t.a.x,t.b.x)-r,Mathf.Min(t.a.y,t.b.y)-r,Mathf.Max(t.a.x,t.b.x)+r,Mathf.Max(t.a.y,t.b.y)+r);}

        static float Blend(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float SegDist(float x,float z,Vector2 a,Vector2 b,out float t)
        {
            var p=new Vector2(x,z);var ab=b-a;t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));return Vector2.Distance(p,a+ab*t);
        }
        // The coast, east to west; land lies south of it.
        static readonly Vector2[] Coast={new(-1060,-430),new(-1250,-640),new(-1500,-880),new(-1900,-1150),new(-2450,-1480),new(-3050,-1660),new(-3650,-1700),new(-4450,-1620)};
        // Signed distance to the coast: positive inland.
        public static float CoastDistance(float x,float z)
        {
            float best=float.MaxValue,sign=1;var p=new Vector2(x,z);
            for(int i=0;i+1<Coast.Length;i++) {
                float d=SegDist(x,z,Coast[i],Coast[i+1],out _);
                if(d<best){best=d;var a=Coast[i];var b=Coast[i+1];sign=(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x)>=0?1:-1;}
            }
            return best*sign;
        }
        // The pit: q is the elliptical radius (1 at the rim), e the metres beyond the rim along the ray, mask 1 where
        // the galleries are cut (the hillside to the south-east; the pit opens to the quarry floor on the north-west).
        public static void PitMetric(float x,float z,out float q,out float e,out float mask)
        {
            float dx=(x-Pit.x)/PitRX,dz=(z-Pit.y)/PitRZ;q=Mathf.Sqrt(dx*dx+dz*dz);
            float len=new Vector2(x-Pit.x,z-Pit.y).magnitude,radius=q>1e-4f?len/q:PitRX;e=(q-1)*radius;
            var dir=len>1e-3f?new Vector2(x-Pit.x,z-Pit.y)/len:Vector2.zero;mask=Blend(-.45f,.12f,Vector2.Dot(dir,new Vector2(.6f,-.8f)));
        }
        // Height of the stepped gallery face at `e` metres beyond the rim: twelve 15 m benches 24 m deep, each face
        // rising over its first 3.6 m (near-vertical on the 2.5 m grid: the terrain shader shows rock).
        public static float GalleryStep(float e)
        {
            float s=Mathf.Max(0,e)/BenchWidth;int k=Mathf.FloorToInt(s);if(k>=Benches)return RimY;
            return FloorY+BenchHeight*(k+Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.15f,s-k)));
        }
        // The worked edge isn't a contour line: each bench's lip wanders a few metres with the slate's grain.
        public static float Wobble(float x,float z)=>(Mathf.PerlinNoise(x*.011f+13,z*.011f+29)-.5f)*16+(Mathf.PerlinNoise(x*.047f+3,z*.047f+5)-.5f)*5;
        // The authored galleries mesh (QuarryHillsBuild.Galleries) covers the directions where the cut is at least this deep;
        // beyond them (the horseshoe's ends) the ground's own partial cut carries the benches into the hillside.
        public const float MeshMask=.55f,RimLedge=4.5f;
        public static readonly Vector2 GalleryAxis=new Vector2(.6f,-.8f);
        public static float MaskOf(Vector2 dir)=>Blend(-.45f,.12f,Vector2.Dot(dir,GalleryAxis));
        // A point `e` metres beyond the pit rim, in direction `dir` from the pit's centre (y left 0).
        public static Vector3 PitPoint(Vector2 dir,float e)
        {
            float rx=dir.x/PitRX,rz=dir.y/PitRZ,radius=1/Mathf.Sqrt(rx*rx+rz*rz);var p=Pit+dir*(radius+e);return new Vector3(p.x,0,p.y);
        }
        // Which bench (0 = floor level, 12 = the rim) a point stands on, or -1 outside the galleries.
        public static int Bench(float x,float z){PitMetric(x,z,out _,out float e,out float mask);if(mask<.5f||e<0||e>Benches*BenchWidth)return -1;return Mathf.Min(Benches,Mathf.FloorToInt(e/BenchWidth)+1);}

        // The gateway west: with Quarry Hills built, the river's gorge through the valley's west hills opens into a broad
        // valley that Quarry Road shares (the gorge was 60 m wide between 100 m walls: no room for a road). Applied to the
        // valley's ground too (ValleyField.Natural), only near the river's line through the hills.
        static readonly Vector2 GapA=new(-840,-1150),GapB=new(-1500,-1530);
        public static float Gap(float x,float z,float h)
        {
            float d=SegDist(x,z,GapA,GapB,out float t);if(d>240)return h;
            float floor=Mathf.Lerp(7.5f,13,t);return h<=floor?h:Mathf.Lerp(floor,h,Blend(70,230,d));
        }
        public static float Natural(float x,float z)
        {
            float dc=CoastDistance(x,z);
            float n1=Mathf.PerlinNoise((x+3000)*.0042f,(z+500)*.0042f),n2=Mathf.PerlinNoise(x*.019f+5,z*.019f+9),n3=Mathf.PerlinNoise(x*.0016f+2,z*.0016f+7);
            // Land rises off the beach (low cliffs and turf), then gently to about 28 m a kilometre inland, softly rolling;
            // south of the town it climbs another 30 m: the hillside the town's terraces step up.
            float land=-7+15*Blend(0,150,dc)+20*Blend(120,1000,dc)+(n2-.5f)*5*Blend(20,200,dc)+(n1-.5f)*10*Blend(100,500,dc);
            // The town's hillside: up 48 m from the lower town to the top streets (steepest ~18%), then a shelf above the
            // sunken quarry floor.
            land+=48*Blend(-2140,-2580,z)*Blend(40,500,dc);
            // Hills: the crest ridge between the river's basin and the town (Quarry Road crosses it at the reveal), the
            // hill the galleries are cut into, the moor to the south, forest hills to the south-west and west.
            float rd=SegDist(x,z,new(-1640,-1420),new(-1980,-2480),out _);
            float hills=30*Mathf.Exp(-(rd/250)*(rd/250))*(.75f+.5f*n1);
            PitMetric(x,z,out float q,out float e,out float mask);
            hills=Mathf.Max(hills,(RimY-24)*Blend(-80,240,e)*mask);
            hills=Mathf.Max(hills,(196+(n3-.5f)*50+(n1-.5f)*20)*Blend(-3250,-3820,z));
            hills=Mathf.Max(hills,(150+(n1-.5f)*40)*Blend(-3700,-4250,x));
            hills=Mathf.Max(hills,(122+(n3-.5f)*40)*Blend(-3000,-3550,z)*Blend(-2900,-3450,x));
            // High ground along the grid's west and south edges: the district's horizon (the world doesn't go on there yet).
            hills=Mathf.Max(hills,(270+(n1-.5f)*80)*Mathf.Max(Blend(West+900,West+80,x),Blend(South+800,South+80,z)));
            float h=land+hills*Blend(60,420,dc);
            // Slate tips.
            h=Mathf.Max(h,TipSurface(x,z)+(n2-.5f)*.8f);
            // The quarry floor: one level pad between the town and the pit.
            float qf=Mathf.Sqrt(Mathf.Pow((x-FloorCentre.x)/FloorRX,2)+Mathf.Pow((z-FloorCentre.y)/FloorRZ,2));
            h=Mathf.Lerp(FloorY,h,Blend(1,1.55f,qf));
            // The galleries cut the hill; the pit falls to the lake bed.
            // Where the authored galleries mesh stands, the ground steps back 3.5 m and sits a metre lower, out of sight under
            // its shelves and behind its faces; beyond, the ground's own cut runs the benches into the hillside.
            float ew=e+Wobble(x,z);
            if(mask>0&&e>-40) {
                float carve=Mathf.Min(h,GalleryStep(Mathf.Max(0,ew)));
                float under=Mathf.Min(h,GalleryStep(Mathf.Max(0,ew-3.5f))-1);
                h=Mathf.Lerp(h,Mathf.Lerp(carve,under,Blend(MeshMask-.04f,MeshMask,mask)),mask);
            }
            // The pit: the mesh's wall stands at the rim (e 0) from the lake bed to the floor, with a 5 m ledge at floor level;
            // the ground drops behind the wall and under the ledge, so no grid stair-step shows at the rim and the water is deep
            // right up to the wall.
            if(e<RimLedge){h=e<RimLedge-2?LakeBed:Mathf.Lerp(LakeBed,Mathf.Min(h,FloorY-.4f),Mathf.InverseLerp(RimLedge-2,RimLedge,e));}
            else if(e<14)h=Mathf.Min(h,FloorY-.5f); // under the mesh's rim ledge (the far terrain levels mustn't poke through)
            // Round the rest of the rim (no galleries) the ground climbs from the ledge as a quarried bank at 31 degrees, so
            // the mesh's skirt meets smooth ground instead of a grid cliff where the floor ends.
            if(e>=14&&e<120)h=Mathf.Lerp(h,Mathf.Min(h,FloorY-.5f+(e-14)*.6f),1-Blend(MeshMask-.12f,MeshMask,mask));
            // The river's source: a tarn below the crest ridge.
            float rs=Vector2.Distance(new(x,z),Source);if(rs<SourceRadius+44)h=Mathf.Min(h,Mathf.Lerp(SourceY-3,h,Blend(SourceRadius-6,SourceRadius+40,rs)));
            // Sea beyond the beach.
            h=Mathf.Lerp(-26,h,Blend(-30,80,dc));
            // Slate Quay's apron, and deep water right up to its sea walls.
            QuayLocal(x,z,out float qu,out float qv);
            if(OnQuay(qu,qv))h=QuayY;else if(qv<0&&OnQuay(qu,qv,7))h=Mathf.Min(h,CoastalLandscape.SeaLevel-6);
            // Seam: into the valley's own west hills.
            float w=Blend(SeamX-320,SeamX,x);if(w>0)h=Mathf.Lerp(h,ValleyField.ValleyNatural(x,z),w);
            return Gap(x,z,h);
        }

        // Flat pads stamped into the grid (centre, inner, outer, height): the town square and Slate Quay.
        public static IEnumerable<(Vector2 centre,float inner,float outer,float y)> Pads()
        {
            yield return (Square,30,48,SquareY);
            // The rugby ground (QuarryHillsCountry): a level pitch with room for its in-goals and touchlines.
            yield return (QuarryHillsCountry.Pitch,72,92,PitchY);
            var ps=PitStopSite.at;yield return (new Vector2(ps.x,ps.z),22,34,ps.y);
        }
        // The Pit Stop: on Quarry Road's south side 210 m before the square, the first thing a visitor passes (as Hillcrest's).
        public static (Vector3 at,float yaw) PitStopSite=>pitStop??=FindPitStop();static (Vector3,float)? pitStop;
        static (Vector3,float) FindPitStop()
        {
            var r=HillcrestRoute.Find(QuarryRoadName);float s=r.Length-210;var at=r.At(s);var right=r.Right(s);var south=right.z<0?right:-right;
            var centre=at+south*(r.width*.5f+15);centre.y=at.y;var face=-south;face.y=0;return (centre,Quaternion.LookRotation(face).eulerAngles.y);
        }
        public static float PitchY=>pitchY??=ValleyField.Natural(QuarryHillsCountry.Pitch.x,QuarryHillsCountry.Pitch.y);static float? pitchY;
        public static float SquareY=>squareY??=Mathf.Max(ValleyField.Natural(Square.x,Square.y),8);static float? squareY;

        // ---------------------------------------------------------------- roads
        // Built by HillcrestRoute.All() after its own seven (so they're stamped, filled, planted round and bridged like
        // Hillcrest's): Quarry Road from a T on the raceway road, over the river and up its north bank through the gap in
        // the west hills, past the source tarn to the crest, and down into the town square; the High Street west; Quay Lane
        // down to Slate Quay; the haul road (gravel) to the quarry floor; and the gravel road towards Kettle Falls.
        public const string QuarryRoadName="Quarry Road",HighStreetName="High Street",QuayLaneName="Quay Lane",HaulRoadName="Haul road",KettleRoadName="Kettle Falls road",
            ChapelStreetName="Chapel Street",TerraceRowName="Terrace Row";
        // Where the back streets meet the town's through roads (set by Roads()).
        public static Vector3 ChapelFrom,ChapelTo,TerraceFrom,TerraceTo;
        public static bool Gravel(string name)=>name==HaulRoadName||name==KettleRoadName;
        public static bool Owns(string name)=>name==QuarryRoadName||name==HighStreetName||name==QuayLaneName||name==ChapelStreetName||name==TerraceRowName||Gravel(name);
        public static Vector3 TeeOnRaceway;
        public static List<HillcrestRoute.Road> Roads(Vector3[] racewayRoad)
        {
            // The T: on the raceway road 70 m before the paddock.
            int tee=0;float best=float.MaxValue;for(int i=0;i<racewayRoad.Length;i++){float d=Vector2.Distance(new(racewayRoad[i].x,racewayRoad[i].z),new(-262,-1018));if(d<best){best=d;tee=i;}}
            TeeOnRaceway=racewayRoad[tee];
            var list=new List<HillcrestRoute.Road>();
            var quarry=Profile(TeeOnRaceway,new Vector2[]{new(-300,-990),new(-352,-948),new(-420,-912),new(-520,-918),new(-640,-975),new(-780,-1050),new(-960,-1140),new(-1120,-1222),
                new(-1270,-1318),new(-1380,-1400),new(-1480,-1480),new(-1570,-1570),new(-1680,-1710),Crest,new(-1900,-2085),new(-2050,-2210),new(-2220,-2290),new(-2400,-2335)},
                new Vector3(Square.x,SquareY,Square.y),.085f,30);
            // Wide corridor: the crest cutting's banks lie back as grass instead of standing as dark rock.
            list.Add(HillcrestRoute.Make(QuarryRoadName,quarry,8,6,40));
            var sq=new Vector3(Square.x,SquareY,Square.y);
            list.Add(HillcrestRoute.Make(HighStreetName,Profile(sq,new Vector2[]{new(-2700,-2368),new(-2820,-2392),new(-2940,-2420)},new Vector3(-3050,float.NaN,-2450),.09f,20),7.5f,5,18));
            list.Add(HillcrestRoute.Make(QuayLaneName,Profile(sq,new Vector2[]{new(-2640,-2240),new(-2720,-2110),new(-2830,-1980),new(-2950,-1860)},QuayPoint(-30,QuayLand-9,QuayY),.08f,20),7,5,16));
            // (It ends at the floor's north edge, by the rally school, clear of the Quarry Sprint loop.)
            list.Add(HillcrestRoute.Make(HaulRoadName,Profile(sq,new Vector2[]{new(-2580,-2470),new(-2520,-2600),new(-2450,-2740)},new Vector3(-2395,FloorY,-2856),.1f,16),9,6,18));
            // Back streets: Chapel Street (uphill, off the haul road, to the High Street's far end) and Terrace Row (downhill, off
            // Quay Lane, to the High Street), so the town is blocks of terraces rather than a cross of two roads.
            Vector3 OnRoad(HillcrestRoute.Road r,float s)=>r.At(Mathf.Clamp(s,0,r.Length));
            var highRoad=list.Find(r=>r.name==HighStreetName);var haulRoad=list.Find(r=>r.name==HaulRoadName);var quayRoad=list.Find(r=>r.name==QuayLaneName);
            ChapelFrom=OnRoad(haulRoad,150);ChapelTo=OnRoad(highRoad,highRoad.Length-150);TerraceFrom=OnRoad(quayRoad,160);TerraceTo=OnRoad(highRoad,190);
            // Each back street leaves and meets its through road square-on (a point 22 m out along the through road's normal,
            // towards where the street goes): shallow, steep approaches made long, tilted junctions with steps at their edges.
            Vector2 SquareOn(HillcrestRoute.Road through,Vector3 at,Vector2 towards,float out_=22)
            {
                float s=through.Nearest(at,out _);var n=through.Right(s);var dir=new Vector2(n.x,n.z);if(Vector2.Dot(dir,towards-new Vector2(at.x,at.z))<0)dir=-dir;
                return new Vector2(at.x,at.z)+dir*out_;
            }
            var chapelVia=new[]{new Vector2(ChapelFrom.x-110,ChapelFrom.z-30),new Vector2(ChapelFrom.x-240,ChapelFrom.z-40),new Vector2(ChapelTo.x+70,ChapelTo.z-80)};
            list.Add(HillcrestRoute.Make(ChapelStreetName,Profile(ChapelFrom,new[]{SquareOn(haulRoad,ChapelFrom,chapelVia[0],10),SquareOn(haulRoad,ChapelFrom,chapelVia[0],28),chapelVia[0],chapelVia[1],chapelVia[2],SquareOn(highRoad,ChapelTo,chapelVia[2],28),SquareOn(highRoad,ChapelTo,chapelVia[2],10)},ChapelTo,.1f,14,BackStreetLanding),6.5f,6,16));
            var terraceVia=new[]{new Vector2(TerraceFrom.x-90,TerraceFrom.z-10),new Vector2(TerraceTo.x+40,TerraceTo.z+90)};
            list.Add(HillcrestRoute.Make(TerraceRowName,Profile(TerraceFrom,new[]{SquareOn(quayRoad,TerraceFrom,terraceVia[0],10),SquareOn(quayRoad,TerraceFrom,terraceVia[0],28),terraceVia[0],terraceVia[1],SquareOn(highRoad,TerraceTo,terraceVia[1],28),SquareOn(highRoad,TerraceTo,terraceVia[1],10)},TerraceTo,.1f,14,BackStreetLanding),6.5f,6,16));
            // (Kettle Falls isn't built yet: the gravel road stops at a gate short of the district's edge hills.)
            list.Add(HillcrestRoute.Make(KettleRoadName,Profile(new Vector3(-3050,float.NaN,-2450),new Vector2[]{new(-3250,-2520),new(-3400,-2556)},new Vector3(-3520,float.NaN,-2590),.11f,24),7,5,16));
            return list;
        }
        // A road line from `start` through `via` to `end` (a NaN height takes the ground's): densified every 2 m, its height
        // the natural ground smoothed over `smooth` metres, then held inside a grade limit between its two ends.
        // The back streets come off their through roads level (1%) for their first and last 20 m, then climb: at a full 10%
        // right up to the tee they met it 1.1-1.3 m above or below the through road, which tilted the junctions (the area
        // audit's lips, the bank and the seams at Chapel Street's and Terrace Row's four junctions).
        public const float BackStreetLanding=20,LandingGrade=.01f;
        static Vector3[] Profile(Vector3 start,Vector2[] via,Vector3 end,float grade,float smooth,float landing=0)
        {
            var c=new List<Vector3>{new(start.x,0,start.z)};foreach(var v in via)c.Add(new Vector3(v.x,0,v.y));c.Add(new Vector3(end.x,0,end.z));
            var line=HillcrestRoute.Dense(c.ToArray(),2);int n=line.Length;var y=new float[n];
            for(int i=0;i<n;i++)y[i]=Mathf.Max(ValleyField.Natural(line[i].x,line[i].z),-9.2f);
            float y0=float.IsNaN(start.y)?y[0]:start.y,y1=float.IsNaN(end.y)?y[n-1]:end.y;
            // Smooth (moving average).
            int half=Mathf.Max(1,Mathf.RoundToInt(smooth/2));var sum=new double[n+1];for(int i=0;i<n;i++)sum[i+1]=sum[i]+y[i];
            var s=new float[n];for(int i=0;i<n;i++){int a=Mathf.Max(0,i-half),b=Mathf.Min(n-1,i+half);s[i]=(float)((sum[b+1]-sum[a])/(b-a+1));}
            // Inside the corridor the grade allows between the ends, then a forward and back pass of the grade limit.
            float[] run=new float[n];for(int i=1;i<n;i++)run[i]=run[i-1]+Vector2.Distance(new(line[i].x,line[i].z),new(line[i-1].x,line[i-1].z));float L=run[n-1];
            // (How far the road can rise or fall in `d` metres from an end: the landing's gentle grade first, then the full grade.)
            float Reach(float d)=>LandingGrade*Mathf.Min(d,landing)+grade*Mathf.Max(0,d-landing);
            float Grade(float r)=>landing>0&&(r<landing||L-r<landing)?LandingGrade:grade;
            for(int pass=0;pass<3;pass++) {
                for(int i=0;i<n;i++){float lo=Mathf.Max(y0-Reach(run[i]),y1-Reach(L-run[i])),hi=Mathf.Min(y0+Reach(run[i]),y1+Reach(L-run[i]));if(lo>hi)lo=hi=(lo+hi)*.5f;s[i]=Mathf.Clamp(s[i],lo,hi);}
                for(int i=1;i<n;i++){float g=Grade((run[i]+run[i-1])*.5f)*(run[i]-run[i-1]);s[i]=Mathf.Clamp(s[i],s[i-1]-g,s[i-1]+g);}
                for(int i=n-2;i>=0;i--){float g=Grade((run[i]+run[i+1])*.5f)*(run[i+1]-run[i]);s[i]=Mathf.Clamp(s[i],s[i+1]-g,s[i+1]+g);}
            }
            s[0]=y0;s[n-1]=y1;
            for(int i=0;i<n;i++)line[i].y=s[i];
            return line;
        }
        // The Silver river's upper reach: from the source tarn down to where the valley's river begins (x -1180).
        public static readonly Vector3[] RiverHead={new(-1572,SourceY,-1648),new(-1490,9.4f,-1572),new(-1400,7.6f,-1488),new(-1320,5.8f,-1420),new(-1250,4.5f,-1368)};
    }
}

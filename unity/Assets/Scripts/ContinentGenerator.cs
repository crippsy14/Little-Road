using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LittleRoad
{
    // The continent's land, climate and roads (docs/58 §2, §11). A port of the world blueprint's generator, so the game
    // and the blueprint draw the same map:
    //  * Climate grid (50 m): land and lakes, distance to the sea, elevation, and moisture marched west to east on the
    //    westerlies (rising ground rains it out; the lee of the Spine is dry).
    //  * Height field (cellMetres, 10 m in the game): analytic elevation x HeightScale, lake beds below each lake's
    //    level (the lowest point of its shore), and the sea floor shelving away from the coast.
    //  * Biomes from height, moisture and temperature (colder south and higher), exactly as the blueprint.
    //  * Roads: the blueprint's polylines splined and given a gentle wander; node heights relaxed until every road can
    //    keep to its type's grade limit; each profile smoothed, kept above water (causeways), held inside the grade
    //    corridor between its ends; then the ground is cut and filled to the roads and to each town's flat pad.
    // World y is metres with the sea at CoastalLandscape.SeaLevel, as in today's game.
    public sealed class ContinentGenerator
    {
        public const float HeightScale=650;public static float Sea=>CoastalLandscape.SeaLevel;
        public enum Biome:byte{Deep,Shallow,Lake,Frozen,Beach,Farmland,Forest,Pine,Steppe,Scrub,Desert,Redrock,Tundra,Rock,Snow}
        public static readonly Color32[] BiomeColours={
            new(47,111,159,255),new(92,164,201,255),new(86,160,206,255),new(214,236,246,255),new(236,220,174,255),new(156,196,98,255),new(64,128,72,255),
            new(46,98,80,255),new(214,194,120,255),new(190,170,108,255),new(232,182,112,255),new(204,110,70,255),new(176,186,158,255),new(150,146,140,255),new(244,247,250,255)};
        public static readonly string[] BiomeNames={"Deep sea","Shallow sea","Lake","Frozen lake","Beach","Farmland & meadow","Rainforest","Pine forest","Dry grassland","Scrub","Sand desert","Red canyons","Tundra","Alpine rock","Snow & glacier"};

        public readonly float cellMetres,cellKm;public readonly int N; // vertices per side
        public float[] height,natural,roadGap;public byte[] biome;
        public readonly float[] lakeLevel=new float[ContinentPlan.Lakes.Length];
        public readonly List<BuiltRoad> roads=new();
        public readonly Dictionary<string,float> nodeHeight=new(),nodeNatural=new();
        public float LandKm2{get;private set;}
        public double Seconds{get;private set;}

        public sealed class BuiltRoad
        {
            public ContinentPlan.Road plan;public ContinentPlan.RoadType type;public Vector3[] points;public float length,maxGrade,climb,maxCut,maxFill,straightLength,minRadius;public bool hairpins;
            public string Label=>string.IsNullOrEmpty(plan.name)?$"{plan.a}–{plan.b}":$"{plan.name} ({plan.a}–{plan.b})";
        }

        public ContinentGenerator(float cellMetres=10)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            this.cellMetres=cellMetres;cellKm=cellMetres/1000;N=Mathf.RoundToInt(ContinentPlan.SizeKm/cellKm)+1;
            Climate();Ground();Roads();Conform();
            Seconds=clock.Elapsed.TotalSeconds;
        }

        // ---------------------------------------------------------------- noise (the blueprint's, bit for bit)
        static float Hash(int x,int y,int s)
        {
            unchecked{uint h=(uint)(x*374761393+y*668265263+s*1442695041);h=(h^(h>>13))*1274126177u;h^=h>>16;return h/4294967295f;}
        }
        static float VNoise(float x,float y,int s)
        {
            int xi=Mathf.FloorToInt(x),yi=Mathf.FloorToInt(y);float xf=x-xi,yf=y-yi,u=xf*xf*(3-2*xf),v=yf*yf*(3-2*yf);
            float a=Hash(xi,yi,s),b=Hash(xi+1,yi,s),c=Hash(xi,yi+1,s),d=Hash(xi+1,yi+1,s);return a+(b-a)*u+(c-a)*v+(a-b-c+d)*u*v;
        }
        public static float Fbm(float x,float y,int s,int o=5)
        {
            float sum=0,amp=.5f,f=1,n=0;for(int i=0;i<o;i++){sum+=amp*VNoise(x*f,y*f,s+i*17);n+=amp;amp*=.5f;f*=2;}return sum/n;
        }
        static float InEll(float x,float y,float[] b){float dx=(x-b[0])/b[2],dy=(y-b[1])/b[3];return dx*dx+dy*dy;}
        static float SegDist(float px,float py,Vector2 a,Vector2 b)
        {
            float dx=b.x-a.x,dy=b.y-a.y,l=dx*dx+dy*dy,t=l>0?((px-a.x)*dx+(py-a.y)*dy)/l:0;t=Mathf.Clamp01(t);
            float x=a.x+dx*t-px,y=a.y+dy*t-py;return Mathf.Sqrt(x*x+y*y);
        }
        static float PolyDist(float px,float py,Vector2[] pts){float d=1e9f;for(int i=0;i+1<pts.Length;i++)d=Mathf.Min(d,SegDist(px,py,pts[i],pts[i+1]));return d;}

        // ---------------------------------------------------------------- the land, analytically (km in)
        public static float LandValue(float x,float y)
        {
            float L=-1;foreach(var b in ContinentPlan.Blobs)L=Mathf.Max(L,(1-InEll(x,y,b))*b[4]);
            foreach(var c in ContinentPlan.Cuts)L=Mathf.Min(L,InEll(x,y,c)-1);
            L+=(Fbm(x*.32f,y*.32f,7)-.5f)*.62f+(Fbm(x*1.3f,y*1.3f,9)-.5f)*.2f;
            var v=ContinentPlan.TodayValley;var bay=ContinentPlan.BayKm;
            // Today's Hillcrest mainland is always land: a rounded, ragged outline (a rectangle read as a slab in 3D).
            if(BoxSd(x,y,v.xMin+.2f,v.yMin+.2f,v.xMax-.2f,v.yMax-.08f,.55f)+(Fbm(x*1.6f,y*1.6f,121)-.5f)*.35f<-.12f)L=Mathf.Max(L,.3f);
            if(y>v.yMax-.06f&&y<ContinentPlan.TodayIsland.yMin+.02f&&x>bay.x-.9f&&x<bay.x+.9f)L=Mathf.Min(L,-.2f);
            if(InEll(x,y,new[]{bay.x+.01f,bay.y-.16f,.2f,.31f})<1)L=Mathf.Max(L,.2f);
            foreach(var isl in ContinentPlan.Islands){float r=InEll(x,y,isl);if(r<2.5f&&r+(Fbm(x*2.2f,y*2.2f,91)-.5f)*.6f<1)L=Mathf.Max(L,.2f);}
            return L;
        }
        // Signed distance to a rounded box (negative inside).
        static float BoxSd(float x,float y,float x0,float y0,float x1,float y1,float r)
        {
            float cx=(x0+x1)*.5f,cy=(y0+y1)*.5f,qx=Mathf.Abs(x-cx)-(x1-x0)*.5f+r,qy=Mathf.Abs(y-cy)-(y1-y0)*.5f+r;
            return new Vector2(Mathf.Max(qx,0),Mathf.Max(qy,0)).magnitude+Mathf.Min(Mathf.Max(qx,qy),0)-r;
        }
        // Which lake (or -1), and how far in (r: 0 at the centre, 1 at the shore).
        public static int LakeAt(float x,float y,out float r)
        {
            r=9;for(int i=0;i<ContinentPlan.Lakes.Length;i++){var lk=ContinentPlan.Lakes[i];float q=InEll(x,y,lk);if(q>2.2f)continue;q+=(Fbm(x*.9f,y*.9f,31)-.5f)*.5f;if(q<1){r=q;return i;}}
            return -1;
        }
        static float Elevation(float x,float y,float dSea,bool lake)
        {
            float h=Mathf.Min(1,dSea/6)*.16f+Mathf.Max(0,Fbm(x*.45f,y*.45f,3)-.42f)*.36f;
            float ridgeNoise=-1;
            foreach(var r in ContinentPlan.Ridges){float d=PolyDist(x,y,r.pts);if(d<r.w*3){if(ridgeNoise<0)ridgeNoise=Fbm(x*.7f,y*.7f,41);float q=d/r.w;h+=r.h*Mathf.Exp(-q*q)*(.7f+.6f*ridgeNoise);}}
            foreach(var b in ContinentPlan.Basins){float dx=x-b[0],dy=y-b[1],q=Mathf.Sqrt(dx*dx+dy*dy)/b[2];h*=1-.75f*Mathf.Exp(-q*q);}
            foreach(var rv in ContinentPlan.Rivers){float q=PolyDist(x,y,rv)/.75f;h*=1-.5f*Mathf.Exp(-q*q);}
            if(lake)h=Mathf.Min(h,.06f);
            return Mathf.Clamp01(h*Mathf.Min(1,dSea/.25f));
        }

        // ---------------------------------------------------------------- climate grid (50 m, as the blueprint)
        const int NC=400;const float CC=ContinentPlan.SizeKm/NC;
        bool[] cLand;bool[] cLake;float[] cE,cWet,cDSea,cDLand;
        void Climate()
        {
            int n=NC*NC;cLand=new bool[n];cLake=new bool[n];cE=new float[n];cWet=new float[n];cDSea=new float[n];cDLand=new float[n];
            Parallel.For(0,NC,j=>{for(int i=0;i<NC;i++){float x=(i+.5f)*CC,y=(j+.5f)*CC;int k=j*NC+i;cLand[k]=LandValue(x,y)>0;cLake[k]=cLand[k]&&LakeAt(x,y,out _)>=0;}});
            Chamfer(cDSea,k=>!cLand[k]);Chamfer(cDLand,k=>cLand[k]);
            Parallel.For(0,NC,j=>{for(int i=0;i<NC;i++){int k=j*NC+i;cE[k]=cLand[k]?Elevation((i+.5f)*CC,(j+.5f)*CC,cDSea[k],cLake[k]):0;}});
            // Moisture: the westerlies march across each row; the sea refills them, rising ground rains them out.
            Parallel.For(0,NC,j=>{
                float m=1;
                for(int i=0;i<NC;i++) {
                    int k=j*NC+i;float x=(i+.5f)*CC,y=(j+.5f)*CC;
                    if(!cLand[k]||cLake[k]){m=Mathf.Min(1,m+(cLake[k]?.003f:.08f));cWet[k]=1;continue;}
                    float step=i>=1?cE[k]-cE[k-1]:0,slope=i>=3?cE[k]-cE[k-3]:0;
                    m=Mathf.Max(.04f,m-Mathf.Max(0,step)*.62f-.00124f);float local=m+Mathf.Min(.34f,Mathf.Max(0,slope)*9);
                    float bonus=.2f*Mathf.Exp(-cDSea[k]/.9f);
                    foreach(var rv in ContinentPlan.Rivers){float d=PolyDist(x,y,rv);if(d<3.6f){float q=d/1.35f;bonus+=.4f*Mathf.Exp(-q*q);}}
                    var tv=ContinentPlan.TodayValley;
                    float own=Mathf.Min(1,local+bonus+(Fbm(x*.6f,y*.6f,51)-.5f)*.12f),meadow=.5f+(Fbm(x*1.4f,y*1.4f,53)-.5f)*.3f;
                    float outside=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.5f,.4f,BoxSd(x,y,tv.xMin-.6f,tv.yMin-.6f,tv.xMax+.6f,tv.yMax+.3f,.8f)+(Fbm(x*1.1f,y*1.1f,57)-.5f)*.5f));
                    cWet[k]=Mathf.Lerp(meadow,own,outside);
                }
            });
            int landCells=0;foreach(bool l in cLand)if(l)landCells++;LandKm2=landCells*CC*CC;
        }
        static void Chamfer(float[] o,Func<int,bool> src)
        {
            const float INF=1e9f;float a=CC,b=CC*1.4142f;for(int k=0;k<o.Length;k++)o[k]=src(k)?0:INF;
            for(int j=0;j<NC;j++)for(int i=0;i<NC;i++){int k=j*NC+i;float d=o[k];if(i>0)d=Mathf.Min(d,o[k-1]+a);if(j>0){d=Mathf.Min(d,o[k-NC]+a);if(i>0)d=Mathf.Min(d,o[k-NC-1]+b);if(i<NC-1)d=Mathf.Min(d,o[k-NC+1]+b);}o[k]=d;}
            for(int j=NC-1;j>=0;j--)for(int i=NC-1;i>=0;i--){int k=j*NC+i;float d=o[k];if(i<NC-1)d=Mathf.Min(d,o[k+1]+a);if(j<NC-1){d=Mathf.Min(d,o[k+NC]+a);if(i<NC-1)d=Mathf.Min(d,o[k+NC+1]+b);if(i>0)d=Mathf.Min(d,o[k+NC-1]+b);}o[k]=d;}
        }
        float ClimateAt(float[] f,float x,float y)
        {
            float gx=Mathf.Clamp(x/CC-.5f,0,NC-1.001f),gy=Mathf.Clamp(y/CC-.5f,0,NC-1.001f);int i=(int)gx,j=(int)gy;float tx=gx-i,ty=gy-j;int k=j*NC+i;
            return Mathf.Lerp(Mathf.Lerp(f[k],f[k+1],tx),Mathf.Lerp(f[k+NC],f[k+NC+1],tx),ty);
        }

        // ---------------------------------------------------------------- the height field and biomes
        void Ground()
        {
            // Each lake's level: the lowest point of its shore, a metre down.
            for(int l=0;l<lakeLevel.Length;l++) {
                var lk=ContinentPlan.Lakes[l];float low=float.MaxValue;
                for(int a=0;a<160;a++){float t=a/160f*Mathf.PI*2;float x=lk[0]+Mathf.Cos(t)*lk[2]*1.14f,y=lk[1]+Mathf.Sin(t)*lk[3]*1.14f;if(LandValue(x,y)<=0)continue;low=Mathf.Min(low,LandHeight(x,y,false));}
                lakeLevel[l]=(low<float.MaxValue?low:Sea+5)-1;
            }
            int n=N*N;natural=new float[n];biome=new byte[n];
            Parallel.For(0,N,j=>{for(int i=0;i<N;i++){float x=i*cellKm,y=j*cellKm;natural[j*N+i]=Natural(x,y,out var b);biome[j*N+i]=(byte)b;}});
            height=(float[])natural.Clone();
        }
        float LandHeight(float x,float y,bool lake)
        {
            float dSea=ClimateAt(cDSea,x,y),e=Elevation(x,y,dSea,lake);
            float detail=(Fbm(x*7,y*7,101)-.5f)*7*Mathf.Min(1,dSea/.3f);
            return Sea+e*HeightScale+detail;
        }
        float Natural(float x,float y,out Biome b)
        {
            if(LandValue(x,y)<=0) {
                float dLand=ClimateAt(cDLand,x,y);b=dLand<.55f?Biome.Shallow:Biome.Deep;
                return Sea-(2+Mathf.Min(42,dLand*35))+(Fbm(x*3,y*3,111)-.5f)*3;
            }
            int lake=LakeAt(x,y,out float r);float dSea=ClimateAt(cDSea,x,y),e=Elevation(x,y,dSea,lake>=0);
            float temp=.2f+.8f*(y/ContinentPlan.SizeKm)-.6f*e+(Fbm(x*.5f,y*.5f,61)-.5f)*.08f;
            if(lake>=0){b=temp<.36f?Biome.Frozen:Biome.Lake;return lakeLevel[lake]-1.5f-Mathf.Min(1,(1-r)*3)*24;}
            float w=ClimateAt(cWet,x,y),h=e;
            if(temp<.1f)b=Biome.Snow;else if(h>.55f)b=Biome.Rock;else if(temp<.22f)b=Biome.Tundra;
            else if(dSea<.13f&&h<.1f&&temp>.45f)b=Biome.Beach;
            else if(temp<.42f&&w>.36f)b=Biome.Pine;else if(w>.68f)b=Biome.Forest;else if(w>.4f)b=Biome.Farmland;else if(w>.27f)b=Biome.Steppe;else if(w>.18f)b=Biome.Scrub;
            else b=h>.18f||Fbm(x*.8f,y*.8f,71)>.56f?Biome.Redrock:Biome.Desert;
            float detail=(Fbm(x*7,y*7,101)-.5f)*7*Mathf.Min(1,dSea/.3f);
            return Sea+e*HeightScale+detail;
        }

        // ---------------------------------------------------------------- sampling
        public bool InMap(Vector3 world){var km=ContinentPlan.ToKm(world);return km.x>=0&&km.y>=0&&km.x<=ContinentPlan.SizeKm&&km.y<=ContinentPlan.SizeKm;}
        public float Sample(float[] f,Vector3 world)
        {
            var km=ContinentPlan.ToKm(world);float gx=Mathf.Clamp(km.x/cellKm,0,N-1.001f),gy=Mathf.Clamp(km.y/cellKm,0,N-1.001f);int i=(int)gx,j=(int)gy;float tx=gx-i,ty=gy-j;int k=j*N+i;
            return Mathf.Lerp(Mathf.Lerp(f[k],f[k+1],tx),Mathf.Lerp(f[k+N],f[k+N+1],tx),ty);
        }
        public float HeightAt(Vector3 world)=>Sample(height,world);
        public Biome BiomeAt(Vector3 world){var km=ContinentPlan.ToKm(world);int i=Mathf.Clamp(Mathf.RoundToInt(km.x/cellKm),0,N-1),j=Mathf.Clamp(Mathf.RoundToInt(km.y/cellKm),0,N-1);return (Biome)biome[j*N+i];}
        // The water surface over a point, if there is water there (sea or a lake).
        public bool WaterAt(Vector3 world,out float level,out int lake)
        {
            var km=ContinentPlan.ToKm(world);lake=-1;level=Sea;
            if(LandValue(km.x,km.y)<=0)return true;
            lake=LakeAt(km.x,km.y,out _);if(lake>=0){level=lakeLevel[lake];return true;}
            return false;
        }
        public Vector3 WorldOf(int i,int j)=>ContinentPlan.ToWorld(i*cellKm,j*cellKm,height[j*N+i]);

        // ---------------------------------------------------------------- roads
        void Roads()
        {
            foreach(var p in ContinentPlan.Places){var w=ContinentPlan.ToWorld(p.Km);float h=Sample(natural,w);if(p.kind!="secret")h=Mathf.Max(h,Sea+2.5f);nodeNatural[p.id]=h;nodeHeight[p.id]=h;}
            foreach(var j in ContinentPlan.Junctions){var w=ContinentPlan.ToWorld(j.Value);float h=Mathf.Max(Sample(natural,w),Sea+3);
                if(WaterAt(w,out float level,out _))h=Mathf.Max(h,level+3);nodeNatural[j.Key]=h;nodeHeight[j.Key]=h;}
            // Relax the node heights until every road can keep to its grade (a mountain town is cut down, a valley
            // junction raised): each road leaves itself 15% of slack for the wander and the smoothing.
            var spans=new List<(string a,string b,float allowed)>();
            foreach(var r in ContinentPlan.Roads){var t=ContinentPlan.Types[r.type];float len=0;var pts=ControlKm(r);for(int i=1;i<pts.Count;i++)len+=(pts[i]-pts[i-1]).magnitude*1000;spans.Add((r.a,r.b,t.grade*len*.85f*(CanWind(r.type)?2.8f:1)));}
            for(int it=0;it<400;it++) {
                bool moved=false;
                foreach(var (a,b,allowed) in spans){float d=nodeHeight[a]-nodeHeight[b];if(Mathf.Abs(d)<=allowed)continue;float fix=(Mathf.Abs(d)-allowed)*.5f*Mathf.Sign(d);nodeHeight[a]-=fix;nodeHeight[b]+=fix;moved=true;}
                if(!moved)break;
            }
            int seed=0;foreach(var r in ContinentPlan.Roads)roads.Add(Build(r,seed++));
            Merge();
        }
        // Where two roads overlap (junctions, crossings, a road sharing a corridor), the lesser one takes the greater
        // one's surface height and eases back to its own profile over 60 m, so there is no lip between the two
        // surfaces (a car snagged on one where the Lake Causeway runs beside the My City road; scale test 9).
        static int Rank(string type)=>type switch{"motorway"=>5,"causeway"=>4,"road"=>3,"mountain"=>2,"gravel"=>1,"ice"=>1,_=>0};
        public int MergedRoads{get;private set;}
        void Merge()
        {
            var cells=new Dictionary<long,List<(int road,int index)>>();
            for(int r=0;r<roads.Count;r++)for(int i=0;i<roads[r].points.Length;i++){var p=roads[r].points[i];long k=Key(Mathf.FloorToInt(p.x/40),Mathf.FloorToInt(p.z/40));if(!cells.TryGetValue(k,out var l))cells[k]=l=new();l.Add((r,i));}
            var order=Enumerable.Range(0,roads.Count).OrderByDescending(r=>Rank(roads[r].plan.type)).ToList();var position=new int[roads.Count];for(int n=0;n<order.Count;n++)position[order[n]]=n;
            foreach(int r in order) {
                var road=roads[r];var pts=road.points;var delta=new float[pts.Length];var has=new bool[pts.Length];bool any=false;
                for(int i=0;i<pts.Length;i++) {
                    var p=pts[i];int cx=Mathf.FloorToInt(p.x/40),cz=Mathf.FloorToInt(p.z/40);float best=float.MaxValue,y=0;
                    for(int x=cx-1;x<=cx+1;x++)for(int z=cz-1;z<=cz+1;z++)if(cells.TryGetValue(Key(x,z),out var list))foreach(var (o,j) in list) {
                        if(o==r||position[o]>position[r])continue; // only roads already settled (greater, or equal and earlier)
                        var q=roads[o].points[j];float d=new Vector2(q.x-p.x,q.z-p.z).magnitude;
                        if(d<(road.type.width+roads[o].type.width)*.5f+1&&d<best){best=d;y=q.y;}
                    }
                    if(best<float.MaxValue){delta[i]=y-p.y;has[i]=true;any=true;}
                }
                if(!any)continue;
                // Ease each adopted height back to the road's own profile over 60 m either side.
                var shift=new float[pts.Length];
                for(int pass=0;pass<2;pass++){int last=-1;for(int n=0;n<pts.Length;n++){int i=pass==0?n:pts.Length-1-n;if(has[i]){last=i;shift[i]=delta[i];continue;}if(last<0)continue;float fall=1-Mathf.Abs(i-last)*4f/60f;if(fall<=0)continue;float v=delta[last]*fall;if(Mathf.Abs(v)>Mathf.Abs(shift[i]))shift[i]=v;}}
                for(int i=0;i<pts.Length;i++)pts[i].y+=shift[i];
                float maxGrade=0;for(int i=5;i<pts.Length;i+=5)maxGrade=Mathf.Max(maxGrade,Mathf.Abs(pts[i].y-pts[i-5].y)/20f);road.maxGrade=maxGrade;MergedRoads++;
            }
        }
        static bool CanWind(string type)=>type=="road"||type=="mountain"||type=="gravel"||type=="ice";
        static List<Vector2> ControlKm(ContinentPlan.Road r){var pts=new List<Vector2>{ContinentPlan.NodeKm(r.a)};pts.AddRange(r.via);pts.Add(ContinentPlan.NodeKm(r.b));return pts;}
        BuiltRoad Build(ContinentPlan.Road plan,int seed)
        {
            var type=ContinentPlan.Types[plan.type];var control=ControlKm(plan).ConvertAll(k=>{var w=ContinentPlan.ToWorld(k);return new Vector2(w.x,w.z);});
            // Catmull-Rom through the control points, then a gentle wander either side (none at the ends).
            var line=Resample(Spline(control),4);float total=line.Count>1?(line.Count-1)*4f:0;
            var wandered=new List<Vector2>(line.Count);
            for(int i=0;i<line.Count;i++) {
                float s=i*4f;var tangent=(line[Mathf.Min(i+1,line.Count-1)]-line[Mathf.Max(i-1,0)]).normalized;var side=new Vector2(-tangent.y,tangent.x);
                float taper=Smooth(0,350,s)*Smooth(0,350,total-s);float n=(Fbm(s/type.wavelength,seed*7.31f,1000+seed,3)-.5f)*2;
                wandered.Add(line[i]+side*(n*type.wander*taper));
            }
            var flat=Resample(wandered,4);float straight=(flat.Count-1)*4f;bool hairpins=false;
            float climbNeeded=Mathf.Abs(nodeHeight[plan.b]-nodeHeight[plan.a]),needLength=climbNeeded/(type.grade*.88f);
            if(CanWind(plan.type)) {
                // Switchbacks where the ground is steep: straight legs across the slope, alternately left and right of the
                // line every 65 m, joined by circular hairpins of 20 m radius (a spline here pinched the tips to ~2 m and
                // a car fell off one; scale test 10). Each leg swings out just enough that its length brings the ground's
                // grade under the road's limit there (a leg of a triangle wave: factor = sqrt(1 + (4A/lambda)^2)), and the
                // whole road is scaled so it also covers the height between its two ends.
                const float lambda=130;var ground=new float[flat.Count];
                for(int i=0;i<flat.Count;i++)ground[i]=Sample(natural,new Vector3(flat[i].x,0,flat[i].y));
                ground=MovingAverage(ground,25);
                float Factor(int i){int a=Mathf.Max(0,i-25),b=Mathf.Min(flat.Count-1,i+25);float run=Mathf.Max(4,(b-a)*4f);return Mathf.Max(1,Mathf.Abs(ground[b]-ground[a])/run/(type.grade*.85f));}
                float overall=Mathf.Max(1,needLength/straight);float local=0;for(int i=0;i<flat.Count;i++)local=Mathf.Max(local,Factor(i));
                if(local>1.05f||overall>1.05f) {
                    List<Vector2> Zig(float boost)
                    {
                        var ctrl=new List<Vector2>{flat[0]};int sign=1;
                        for(float s=lambda*.5f;s<straight-lambda*.25f;s+=lambda*.5f) {
                            int i=Mathf.Clamp(Mathf.RoundToInt(s/4),0,flat.Count-1);var tan=(flat[Mathf.Min(i+1,flat.Count-1)]-flat[Mathf.Max(i-1,0)]).normalized;
                            float f=Mathf.Min(4,Mathf.Max(Factor(i),1)*boost),amp=Mathf.Min(260,lambda*.25f*Mathf.Sqrt(Mathf.Max(0,f*f-1))*1.12f);
                            ctrl.Add(flat[i]+new Vector2(-tan.y,tan.x)*(sign*amp*Smooth(0,160,s)*Smooth(0,160,straight-s)));sign=-sign;
                        }
                        ctrl.Add(flat[^1]);return Resample(Fillet(ctrl,HairpinRadius),4);
                    }
                    // A little extra everywhere if the steep parts alone don't cover the climb end to end.
                    float boost=1;var z=Zig(1);
                    for(int it=0;it<10&&(z.Count-1)*4f<needLength*1.02f;it++){boost*=1.12f;z=Zig(boost);}
                    flat=z;hairpins=true;
                }
            }
            int count=flat.Count;var y=new float[count];
            float start=nodeHeight[plan.a],end=nodeHeight[plan.b];float L=(count-1)*4f;
            var floor=new float[count];
            for(int i=0;i<count;i++) {
                var w=new Vector3(flat[i].x,0,flat[i].y);y[i]=Sample(natural,w);floor[i]=float.MinValue;
                if(type.overWater&&WaterAt(w,out float level,out int lake))floor[i]=level+(plan.type=="ice"&&lake>=0?.3f:2.5f);
            }
            for(int i=0;i<count;i++)y[i]=Mathf.Max(y[i],floor[i]);
            y=MovingAverage(y,Mathf.Max(1,Mathf.RoundToInt(type.smooth/4)));
            float g=type.grade*4;
            for(int pass=0;pass<4;pass++) {
                for(int i=0;i<count;i++) {
                    float s=i*4f;float lo=Mathf.Max(start-type.grade*s,end-type.grade*(L-s)),hi=Mathf.Min(start+type.grade*s,end+type.grade*(L-s));
                    if(lo>hi){float mid=(lo+hi)*.5f;lo=hi=mid;}
                    y[i]=Mathf.Clamp(Mathf.Max(y[i],floor[i]),lo,hi);
                }
                for(int i=1;i<count;i++)y[i]=Mathf.Clamp(y[i],y[i-1]-g,y[i-1]+g);
                for(int i=count-2;i>=0;i--)y[i]=Mathf.Clamp(y[i],y[i+1]-g,y[i+1]+g);
            }
            y=MovingAverage(y,5);int ease=Mathf.Min(30,count/2);float d0=start-y[0],d1=end-y[count-1];
            for(int i=0;i<ease;i++){float w=1-i/(float)ease;y[i]+=d0*w;y[count-1-i]+=d1*w;}
            var points=new Vector3[count];float maxGrade=0,climb=0;
            for(int i=0;i<count;i++)points[i]=new Vector3(flat[i].x,y[i],flat[i].y);
            for(int i=5;i<count;i+=5){float dy=points[i].y-points[i-5].y;maxGrade=Mathf.Max(maxGrade,Mathf.Abs(dy)/20f);if(dy>0)climb+=dy;}
            // Cuttings and embankments: how far the road sits below or above the natural ground.
            float maxCut=0,maxFill=0;for(int i=0;i<count;i++){float n=Sample(natural,points[i]);maxCut=Mathf.Max(maxCut,n-points[i].y);if(!WaterAt(points[i],out _,out _))maxFill=Mathf.Max(maxFill,points[i].y-n);}
            // The tightest turn: the circle through points 12 m apart, along the whole road.
            float minRadius=float.MaxValue;for(int i=3;i+3<count;i++){Vector2 a=new(points[i-3].x,points[i-3].z),b=new(points[i].x,points[i].z),c=new(points[i+3].x,points[i+3].z);float cross=Mathf.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x));if(cross<1e-3f)continue;minRadius=Mathf.Min(minRadius,(a-b).magnitude*(b-c).magnitude*(c-a).magnitude/(2*cross));}
            return new BuiltRoad{plan=plan,type=type,points=points,length=L,maxGrade=maxGrade,climb=climb,maxCut=maxCut,maxFill=maxFill,straightLength=straight,hairpins=hairpins,minRadius=minRadius};
        }
        static float Smooth(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float[] MovingAverage(float[] v,int half)
        {
            var sum=new double[v.Length+1];for(int i=0;i<v.Length;i++)sum[i+1]=sum[i]+v[i];var o=new float[v.Length];
            for(int i=0;i<v.Length;i++){int a=Mathf.Max(0,i-half),b=Mathf.Min(v.Length-1,i+half);o[i]=(float)((sum[b+1]-sum[a])/(b-a+1));}
            return o;
        }
        public const float HairpinRadius=20;
        // A polyline with every corner rounded by a circular arc of radius r (smaller where the legs are too short).
        static List<Vector2> Fillet(List<Vector2> p,float r)
        {
            var o=new List<Vector2>{p[0]};
            for(int k=1;k<p.Count-1;k++) {
                Vector2 v=p[k],a=p[k-1]-v,b=p[k+1]-v;float la=a.magnitude,lb=b.magnitude;if(la<.01f||lb<.01f)continue;a/=la;b/=lb;
                float theta=Mathf.Acos(Mathf.Clamp(Vector2.Dot(a,b),-1,1));if(theta>Mathf.PI-.01f){o.Add(v);continue;}
                float t=r/Mathf.Tan(theta*.5f),limit=.48f*Mathf.Min(la,lb),rr=r;if(t>limit){t=limit;rr=t*Mathf.Tan(theta*.5f);}
                Vector2 a1=v+a*t,a2=v+b*t,centre=v+(a+b).normalized*(rr/Mathf.Sin(theta*.5f));
                float s0=Mathf.Atan2(a1.y-centre.y,a1.x-centre.x),s1=Mathf.Atan2(a2.y-centre.y,a2.x-centre.x),sweep=Mathf.DeltaAngle(s0*Mathf.Rad2Deg,s1*Mathf.Rad2Deg)*Mathf.Deg2Rad;
                int n=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(sweep)*rr/2));
                for(int i=0;i<=n;i++){float ang=s0+sweep*i/n;o.Add(centre+new Vector2(Mathf.Cos(ang),Mathf.Sin(ang))*rr);}
            }
            o.Add(p[^1]);return o;
        }
        static List<Vector2> Spline(List<Vector2> p)
        {
            var o=new List<Vector2>();int n=p.Count;
            for(int s=0;s<n-1;s++) {
                Vector2 p0=s>0?p[s-1]:2*p[0]-p[1],p1=p[s],p2=p[s+1],p3=s+2<n?p[s+2]:2*p[n-1]-p[n-2];
                int steps=Mathf.Max(2,Mathf.CeilToInt((p2-p1).magnitude/8));
                for(int k=0;k<steps;k++){float t=k/(float)steps,t2=t*t,t3=t2*t;o.Add(.5f*(2*p1+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t2+(-p0+3*p1-3*p2+p3)*t3));}
            }
            o.Add(p[n-1]);return o;
        }
        static List<Vector2> Resample(List<Vector2> p,float step)
        {
            var o=new List<Vector2>{p[0]};float carry=0;
            for(int i=1;i<p.Count;i++) {
                var a=p[i-1];var b=p[i];float seg=(b-a).magnitude;float at=step-carry;
                while(at<=seg){o.Add(Vector2.Lerp(a,b,at/seg));at+=step;}
                carry=seg-(at-step);
            }
            if((o[^1]-p[^1]).magnitude>.5f)o.Add(p[^1]);return o;
        }

        // ---------------------------------------------------------------- cut and fill: town pads, then roads
        void Conform()
        {
            foreach(var p in ContinentPlan.Places) {
                var c=ContinentPlan.ToWorld(p.Km);float pad=nodeHeight[p.id],r=p.Radius,reach=r+240;
                bool any=p.kind=="secret";
                Around(c,reach,(k,d)=>{if(!any&&natural[k]<=Sea+.5f)return;float t=Smooth(r,reach,d);height[k]=Mathf.Lerp(pad,height[k],t);});
            }
            int n=N*N;roadGap=new float[n];var target=new float[n];var blend=new float[n];for(int k=0;k<n;k++)roadGap[k]=float.MaxValue;
            foreach(var road in roads) {
                float hw=road.type.width*.5f,reach=hw+6+road.type.blend;
                foreach(var p in road.points)Around(p,reach,(k,d)=>{float gap=d-hw;if(gap<roadGap[k]){roadGap[k]=gap;target[k]=p.y;blend[k]=road.type.blend;}});
            }
            for(int k=0;k<n;k++) {
                float gap=roadGap[k];if(gap==float.MaxValue)continue;
                if(gap<=0)height[k]=target[k]-.35f;
                else if(gap<=6)height[k]=target[k]-.35f-.3f*gap/6;
                else height[k]=Mathf.Lerp(target[k]-.7f,height[k],Smooth(6,6+blend[k],gap));
            }
        }
        void Around(Vector3 centre,float radius,Action<int,float> visit)
        {
            var km=ContinentPlan.ToKm(centre);int ci=Mathf.RoundToInt(km.x/cellKm),cj=Mathf.RoundToInt(km.y/cellKm),r=Mathf.CeilToInt(radius/cellMetres);
            for(int j=Mathf.Max(0,cj-r);j<=Mathf.Min(N-1,cj+r);j++)for(int i=Mathf.Max(0,ci-r);i<=Mathf.Min(N-1,ci+r);i++) {
                float dx=(i*cellKm-km.x)*1000,dz=(j*cellKm-km.y)*1000,d=Mathf.Sqrt(dx*dx+dz*dz);if(d<=radius)visit(j*N+i,d);
            }
        }

        // ---------------------------------------------------------------- road queries
        Dictionary<long,List<(int road,int index)>> roadIndex;
        static long Key(int x,int z)=>((long)x<<32)^(uint)z;
        void EnsureIndex()
        {
            if(roadIndex!=null)return;roadIndex=new();
            for(int r=0;r<roads.Count;r++)for(int i=0;i<roads[r].points.Length;i+=2){var p=roads[r].points[i];long k=Key(Mathf.FloorToInt(p.x/100),Mathf.FloorToInt(p.z/100));if(!roadIndex.TryGetValue(k,out var l))roadIndex[k]=l=new();l.Add((r,i));}
        }
        // The nearest road point within `range` metres (flat distance).
        public bool NearestRoad(Vector3 at,float range,out BuiltRoad road,out int index,out float distance)
        {
            EnsureIndex();road=null;index=-1;distance=float.MaxValue;int reach=Mathf.CeilToInt(range/100);int cx=Mathf.FloorToInt(at.x/100),cz=Mathf.FloorToInt(at.z/100);
            for(int x=cx-reach;x<=cx+reach;x++)for(int z=cz-reach;z<=cz+reach;z++)if(roadIndex.TryGetValue(Key(x,z),out var list))foreach(var (r,i) in list) {
                var p=roads[r].points[i];float d=new Vector2(p.x-at.x,p.z-at.z).magnitude;if(d<distance){distance=d;road=roads[r];index=i;}
            }
            return road!=null&&distance<=range;
        }
    }
}

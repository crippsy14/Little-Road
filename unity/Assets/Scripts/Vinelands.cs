using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands (docs/85, plan docs/68): the class C main town, a Douro-style river town of terraced vineyards 1.4 km on
    // from Bramblewick's gate. Behind --vinelands (off) until it's judged; it needs Bramblewick (its road comes from there).
    //   - The land, in Vinelands' frame (a metres east, n north of the vila at (3400, -3280)): a ridge carrying the vila
    //     (about 120 m) and the Belvedere's knoll (200 m); the amphitheatre, the ridge's east face falling about 12% to the
    //     river (6 m) at a = +1000; the far bank rising beyond; the Vale, the back slope falling west towards Bramblewick;
    //     a rim of hills, then the sea, south and east of the district (the map ends there for now).
    //   - The roads: Vinelands Road from Bramblewick's gate over the ridge's north shoulder into the Largo; the Subida, one
    //     road from the river's north end past the Cais, along the river, up the switchback stack to the Belvedere and down
    //     the Rua da Torre into the Largo (the hill climb's road, docs/85); Adega Lane from the Largo to the Vineyard Loop.
    // The valley's height grid reaches east to x 5250 and south to z -5130 with it (ValleyField).
    public static class Vinelands
    {
        public static bool Enabled=>enabled??=RaceTrial.Has("--vinelands")&&Bramblewick.Enabled;static bool? enabled;
        public static readonly Vector2 Centre=new(3400,-3280);
        public const float East=5250,South=-5130,Radius=1800,Water=6;
        public const string RoadName="Vinelands Road",SubidaName="Subida",AdegaLaneName="Adega Lane",LoopName="Vineyard Loop",VinesTrackName="Adega Vines";
        // Where the loop is split for the road network (its point at Press Dip, a T with the Adega Vines track): a closed road
        // with both ends on one node left a gap in the junction under the start line (town round 3's 0.65 m steps).
        public static int LoopSplit;
        public const float LoopWidth=9,SubidaWidth=7.5f;
        public static Vector3[] Lap;   // (the Vineyard Loop's closed line, point 0 at the Adega junction; round 2 sets the start line)
        public static readonly Dictionary<string,Vector2> Marks=new();
        public static Vector3 Largo,Adega,Cais,Belvedere,PitStopAt;public static float PitStopYaw;
        static Vector2 W(float a,float n)=>Centre+new Vector2(a,n);
        static Vector2 F(float x,float z)=>new Vector2(x,z)-Centre;
        public static bool Contains(float x,float z)=>Enabled&&(new Vector2(x,z)-Centre).sqrMagnitude<Radius*Radius;
        public static bool Core(float x,float z)=>Enabled&&(new Vector2(x,z)-Centre).sqrMagnitude<460*460;   // (the vila: the Largo, its three streets and the Pit Stop at the town gate)
        static float S(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));

        // The river (frame), from the north edge to the south, past the Cais at a = +1040.
        public static readonly Vector2[] River={new(1330,1800),new(1160,800),new(1040,150),new(1070,-400),new(1200,-1050),new(1150,-1400),new(1360,-1800)};   // (a bend in the south gorge: no line of sight out to the map's edge)
        // The ridge's crest (frame a, n) and its height: the north shoulder Vinelands Road crosses, the vila, the Belvedere.
        // (Round 1's views: at 120 m the face was too gentle to read and the shoulder's cutting too deep. The vila is at 150 m,
        //  the Belvedere 230 m (the 230 m climb the design asked for), the shoulder 108 m.)
        static readonly Vector3[] Crest={new(-380,1350,80),new(-160,700,108),new(0,0,150),new(-300,-330,190),new(-590,-614,230),new(-700,-1300,205),new(-760,-1800,170)};
        // `a` along a north-south polyline at `n` (held at its ends).
        static float AtN(Vector2[] line,float n){if(n>=line[0].y)return line[0].x;for(int i=1;i<line.Length;i++)if(n>=line[i].y){float t=Mathf.InverseLerp(line[i-1].y,line[i].y,n);return Mathf.Lerp(line[i-1].x,line[i].x,t);}return line[^1].x;}
        static (float a,float h) CrestAt(float n){if(n>=Crest[0].y)return (Crest[0].x,Crest[0].z);for(int i=1;i<Crest.Length;i++)if(n>=Crest[i].y){float t=Mathf.InverseLerp(Crest[i-1].y,Crest[i].y,n);return (Mathf.Lerp(Crest[i-1].x,Crest[i].x,t),Mathf.Lerp(Crest[i-1].z,Crest[i].z,t));}return (Crest[^1].x,Crest[^1].z);}
        // The town's bairros (the plan rev. 2, section 1), in the frame: the built-up land, where the slope isn't terraced.
        public static readonly Vector2[][] Bairros={
            new Vector2[]{new(-75,-100),new(-75,100),new(30,140),new(180,110),new(240,10),new(220,-110),new(100,-130),new(-10,-120)},   // (Vila Velha)
            new Vector2[]{new(-90,130),new(50,130),new(5,250),new(-35,500),new(-45,520),new(-150,520),new(-175,500),new(-130,250)},   // (Rua Direita)
            new Vector2[]{new(-95,-120),new(-25,-120),new(-110,-200),new(-260,-340),new(-330,-380),new(-380,-330),new(-300,-260),new(-190,-160)},   // (Mirante)
            new Vector2[]{new(-75,-60),new(-75,80),new(-180,90),new(-300,110),new(-300,10),new(-240,-40),new(-160,-70)},   // (Eiras)
            new Vector2[]{new(-330,600),new(-180,520),new(-60,540),new(60,610),new(130,740),new(-40,830),new(-200,800),new(-330,720)},   // (Alto da Portela)
            new Vector2[]{new(870,-130),new(985,-130),new(985,110),new(870,110)},   // (Ribeira do Cais)
            new Vector2[]{new(470,110),new(540,-10),new(640,-30),new(660,40),new(560,110)},   // (Casal do Meio)
            new Vector2[]{new(820,900),new(1000,930),new(1080,1040),new(900,1060),new(800,1000)}};   // (Entroncamento)
        static bool Inside(Vector2[] poly,Vector2 p){bool c=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)if(((poly[i].y>p.y)!=(poly[j].y>p.y))&&(p.x<(poly[j].x-poly[i].x)*(p.y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x))c=!c;return c;}
        static float EdgeDistance(Vector2[] poly,Vector2 p){float best=float.MaxValue;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){var a=poly[j];var ab=poly[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,a+ab*t));}return best;}
        // 1 inside a bairro, fading to 0 over 25 m outside it (frame point)
        public static float InTown(Vector2 p){float m=0;foreach(var b in Bairros){if(Inside(b,p))return 1;m=Mathf.Max(m,1-S(0,25,EdgeDistance(b,p)));}return m;}
        public static (float a,float h) CrestAtN(float n)=>CrestAt(n);public static float RiverAtN(float n)=>AtN(River,n);
        static float RiverDistance(Vector2 p){float best=float.MaxValue;for(int i=1;i<River.Length;i++){var a=River[i-1];var ab=River[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,a+ab*t));}return best;}

        // The land over `valley` (Bramblewick's vale and the valley's own, which this replaces within the district).
        public static float Natural(float x,float z,float valley)
        {
            var p=F(x,z);float d=p.magnitude;
            if(d>Radius+50){float dB0=Vector2.Distance(new(x,z),Bramblewick.Centre);return x<2300?valley:Mathf.Lerp(-40,valley,1-S(1350,1550,dB0));}
            float n1=Mathf.PerlinNoise((x+900)*.004f,(z+4100)*.004f),n2=Mathf.PerlinNoise(x*.019f+7,z*.019f+19),n3=Mathf.PerlinNoise(x*.0028f+3,z*.0028f+41);
            float ra=AtN(River,p.y);var (ca,hc)=CrestAt(p.y);float h;
            if(p.x>=ra)h=Water+140*S(0,700,p.x-ra)*(.75f+.5f*n3)*(1-.75f*S(1100,1600,d))+(n2-.5f)*3;   // (the far bank, lower towards the coast: round 1's 200 m cliff into the sea)
            else if(p.x>=ca) {   // (the amphitheatre, stepped into terraces)
                float s=(p.x-ca)/Mathf.Max(1,ra-ca);s=1-s;h=Water+(hc-Water)*(s*(.55f+.45f*s))+(n2-.5f)*2.5f;
                float mask=S(110,170,RiverDistance(p))*S(30,80,p.x-ca)*(1-S(1250,1450,d))*(1-InTown(p));   // (not under the town: its houses stand on the natural slope)
                if(mask>0)h=Mathf.Lerp(h,Terrace(h),mask);
            }
            else{   // (the Vale: a steep upper flank, then a broad shelf falling about 4.5% west, the Vineyard Loop's ground)
                float w=ca-p.x,shelf=hc-55,vale=48+(n1-.5f)*10;   // (a 400 m flank: at 250 m it was 22%, and Adega Lane stood on an embankment off the Largo)
                h=w<400?Mathf.Lerp(hc,shelf,S(0,400,w)):Mathf.Lerp(shelf,vale,S(400,1400,w));h+=(n2-.5f)*3;
            }
            // The river: about 100 m bank to bank and 5 m deep (round 1's 40 m ditch hid its water), its banks a metre or two
            // over the water before the slopes rise.
            float dr=RiverDistance(p);bool channel=dr<100;if(channel)h=Mathf.Lerp(Water-5,Mathf.Max(h,Water+1.5f),S(45,100,dr));
            // Rim hills round the district (none towards Bramblewick, whose own hills frame that side); the river leaves
            // through gaps in them.
            float dB=Vector2.Distance(new(x,z),Bramblewick.Centre);
            if(!channel)h+=55*S(1250,1500,d)*(1-S(1500,1750,d))*S(1000,1600,dB)*(.7f+.6f*n3);
            // Towards Bramblewick the district blends into its vale.
            h=Mathf.Lerp(h,valley,S(1100,1500,d));
            // The coast: land only within the district or Bramblewick's hills (east of Bramblewick, the valley's own land
            // beyond them would stand on the sea floor, round 1's floating slab); everything falls to the sea bed by the edge.
            float land=Mathf.Max(1-S(1450,Radius,d),x<2300?1:1-S(1350,1550,dB));
            return Mathf.Lerp(-40,h,land);   // (to the sea bed outside the district, -40: at -16 it stood as a shelf with a 24 m underwater cliff, the "slab")
        }

        // The terraces (docs/85, the signature): every 5 m of height a bench rising gently (1.1 m over four fifths of the step),
        // then a steep riser (the rest over the last fifth). At the 2.5 m grid the risers stand at 40-60 degrees and take the
        // slope's stone; from across the water the face reads as stripes along the contours.
        public const float TerraceStep=5;
        static float Terrace(float h){float k=Mathf.Floor(h/TerraceStep),f=h/TerraceStep-k;float shaped=f<.78f?f/.78f*1.1f:1.1f+(f-.78f)/.22f*(TerraceStep-1.1f);return k*TerraceStep+shaped;}
        public static readonly List<(Vector2 at,float y)> Quay=new();
        // Level ground: the Largo, the Cais's quay, the Belvedere's terrace, the Pit Stop and the Adega yard.
        public static IEnumerable<(Vector2 at,float inner,float outer,float y)> Pads()
        {
            yield return (new Vector2(Largo.x,Largo.z),42,80,Largo.y);   // (the Largo, 70 x 45 m: level on the crest)
            yield return (new Vector2(Cais.x,Cais.z),24,40,Cais.y);   // (the Praça do Cais)
        }

        // A track laid from pieces (the track-design skill's format): "S 40" straight, "A 28 70" arc (degrees, + left;
        // radius m), "M Name" marker, "S L1"/"S L2" the solved straights. 2 m steps, in a local frame.
        sealed class Turtle
        {
            public readonly List<Vector2> pts=new();public Vector2 at;float h;public readonly Dictionary<string,Vector2> marks=new();
            public Turtle(Vector2 start,float headingDeg){at=start;h=headingDeg*Mathf.Deg2Rad;pts.Add(at);}
            public void S(float len){int n=Mathf.Max(1,Mathf.CeilToInt(len/2));for(int i=0;i<n;i++){at+=new Vector2(Mathf.Cos(h),Mathf.Sin(h))*len/n;pts.Add(at);}}
            public void A(float deg,float radius){float ang=deg*Mathf.Deg2Rad,L=Mathf.Abs(ang)*radius;int n=Mathf.Max(1,Mathf.CeilToInt(L/2));for(int i=0;i<n;i++){h+=ang/n;at+=new Vector2(Mathf.Cos(h-ang/n/2),Mathf.Sin(h-ang/n/2))*L/n;pts.Add(at);}}
            public static Turtle Run(string spec,Vector2 start,float heading,float l1,float l2)
            {
                var t=new Turtle(start,heading);
                foreach(var raw in spec.Split(';',System.StringSplitOptions.RemoveEmptyEntries)) {
                    var f=raw.Trim().Split(' ',2);if(f.Length<2)continue;
                    if(f[0]=="M")t.marks[f[1]]=t.at;
                    else if(f[0]=="S")t.S(f[1]=="L1"?l1:f[1]=="L2"?l2:float.Parse(f[1],System.Globalization.CultureInfo.InvariantCulture));
                    else if(f[0]=="A"){var g=f[1].Split(' ');t.A(float.Parse(g[0],System.Globalization.CultureInfo.InvariantCulture),float.Parse(g[1],System.Globalization.CultureInfo.InvariantCulture));}
                }
                return t;
            }
            // L1 and L2 solved so the pieces end at `target` (two non-parallel straights: a linear solve).
            public static (float l1,float l2) Solve(string spec,Vector2 start,float heading,Vector2 target)
            {
                var e0=Run(spec,start,heading,200,200).at;var d1=(Run(spec,start,heading,300,200).at-e0)/100;var d2=(Run(spec,start,heading,200,300).at-e0)/100;var r=target-e0;float det=d1.x*d2.y-d1.y*d2.x;
                return (200+(r.x*d2.y-r.y*d2.x)/det,200+(d1.x*r.y-d1.y*r.x)/det);
            }
        }
        // The Vineyard Loop (docs/85; the track-design skill's loop, review/tmp/vlr5.txt): anticlockwise from the Adega
        // junction, 1.68 km. Laid in its design frame (start at the origin heading -75), then turned -85 degrees so the main
        // straight runs downhill west-south-west across the Vale, its start at the Adega.
        public const string LoopSpec="M Adega Straight; S L2; M Lagar; A 60 38; A 60 60; S 15; M Lagar Kink; A 15 200; M Long Bench; S 60; A 30 160; S 20;"+
            "M Wall Esses; A -40 40; A 75 30; A -45 35; S 20; M Press Dip; S 30; M Cork Run; A -40 80; S 25; A 40 60; S 25; A -50 70; S 30;"+
            "M Cooperage Hairpin; A 150 22; M Quinta Straight; S L1; M Barrel Kink; A -14 150; S 25; A 14 150; S 90; M Quinta Gate; A 105 65; S 20;"+
            "M Olive Grove; A -55 70; S 70; A 55 70";
        public const float LoopTurn=-85;public static readonly Vector2 AdegaAt=new(-620,230);
        // The hill climb's road (docs/85; review/tmp/hc1.txt): from the Cais south along the river, up the stack of six
        // hairpins to the Belvedere. Laid from the Cais heading south.
        public const string ClimbSpec="M Cais Start; S 150; M Rabelo Reach; A 20 250; S 120; A -25 220; S 150; A 15 300; S 200; A -20 250; S 100;"+
            "M Station Kink; A 12 120; S 60; M Lodge Corner; A -92 35; S 60; A -60 120; S 180; M Letter Hairpin; A 120 32; S 200;"+
            "M Cellar Cut; A -120 16; S 180; M Press Hairpin; A 60 25; S 20; A 60 25; S 200; M Monorail Hairpin; A -120 24; S 180;"+
            "M Outer Hairpin; A 120 30; S 160; M Last Hairpin; A -60 50; A -60 28; M Long Bench Climb; S 400; M The Lip; S 30; A 40 120;"+
            "M Belvedere Avenue; S 300";
        // The Rua da Torre: a right-hand hairpin round the tower, then down the ridge to the Largo (L1, L2 solved to land there).
        public const string TorreSpec="M Belvedere; A -115 28; S 30; M Rua da Torre; A -36 100; S L1; A 70 90; S L2; A -10 100; S 20";   // (it ends heading 70: into the Largo from the south-south-west, its arm well apart from the others)
        public static readonly Vector2 CaisAt=new(950,0);

        // The two designs as points in the frame (for VinelandsChecks: pure geometry, no land).
        public static List<Vector2> LoopDesign(out float l1,out float l2,out float endHeadingError)
        {
            (l1,l2)=Turtle.Solve(LoopSpec,Vector2.zero,-75,Vector2.zero);var t=Turtle.Run(LoopSpec,Vector2.zero,-75,l1,l2);
            var a=t.pts[^1]-t.pts[^2];endHeadingError=Mathf.DeltaAngle(Mathf.Atan2(a.y,a.x)*Mathf.Rad2Deg,-75);
            float c=Mathf.Cos(LoopTurn*Mathf.Deg2Rad),s=Mathf.Sin(LoopTurn*Mathf.Deg2Rad);var placed=new List<Vector2>();foreach(var q in t.pts)placed.Add(AdegaAt+new Vector2(q.x*c-q.y*s,q.x*s+q.y*c));return placed;
        }
        public static List<Vector2> ClimbDesign(out float t1,out float t2){(t1,t2)=Turtle.Solve(ClimbSpec+";"+TorreSpec,CaisAt,270,Vector2.zero);return Turtle.Run(ClimbSpec+";"+TorreSpec,CaisAt,270,t1,t2).pts;}
        static Vector3 Ground(Vector2 frame){var w=W(frame.x,frame.y);return new Vector3(w.x,float.NaN,w.y);}
        static Vector3 OnGround(Vector2 frame){var w=W(frame.x,frame.y);return new Vector3(w.x,ValleyField.Natural(w.x,w.y),w.y);}
        // The roads, after Valley Road South (whose far end, Bramblewick's gate, is `gateEnd`).
        // ---- Zone 0, the skeleton (docs/85 restart, the town plan rev. 2 section 2): the primary roads.
        //   - Estrada de Vinelands: from Bramblewick's gate over the ridge's saddle (108 m) and down past the Portela and the
        //     co-op to the river road. It never touches the Largo.
        //   - Estrada Ribeirinha: along the river's west bank, 75 m off the centreline.
        //   - Rua Direita: the main street, a spur from the saddle south along the ridge to the Largo (leaving the saddle
        //     heading south, square to the entry road: a shallow arm overlapped the junction, docs/86 round 11).
        //   - Estrada do Cais: the Calçada, from the Largo down the amphitheatre in Z bends to the quay.
        //   - Estrada do Mirante: the crest road from the Largo south-west to the Belvedere's knoll.
        //   - Caminho Velho do Vale: from the Largo west down to the Vale (the club circuit, zone 7).
        // Each road ends on another's own vertex, so the junctions (the saddle, the river's north T, the Cais, the Largo)
        // are exact.
        public const string EntryName="Estrada de Vinelands",RiverRoadName="Estrada Ribeirinha",RuaDireitaName="Rua Direita",
            CaisRoadName="Estrada do Cais",MiranteName="Estrada do Mirante",ValeRoadName="Caminho Velho do Vale";
        public static int EntrySaddle,RiverNorthT,RiverCaisT;
        static Vector2[] Wv(params (float a,float n)[] f){var o=new Vector2[f.Length];for(int i=0;i<f.Length;i++)o[i]=W(f[i].a,f[i].n);return o;}
        static int VertexNear(Vector3[] line,Vector2 frame){var w=W(frame.x,frame.y);int best=0;float bd=float.MaxValue;for(int i=0;i<line.Length;i++){float d=(new Vector2(line[i].x,line[i].z)-w).sqrMagnitude;if(d<bd){bd=d;best=i;}}return best;}
        public static List<HillcrestRoute.Road> Roads(Vector3 gateEnd)
        {
            var list=new List<HillcrestRoute.Road>();Marks.Clear();Lap=null;
            Largo=OnGround(Vector2.zero);Cais=OnGround(new Vector2(960,0));
            // the river road, north to south
            var rline=QuarryHills.Profile(Ground(new Vector2(1173,1320)),Wv((1131,1070),(1085,800),(1011,400),(965,150),(973,0),(995,-400),(1075,-800),(1115,-1000)),Ground(new Vector2(1089,-1320)),.06f,30,0);
            var river=HillcrestRoute.Make(RiverRoadName,rline,7.5f,5,30);
            RiverNorthT=VertexNear(rline,new Vector2(1131,1070));RiverCaisT=VertexNear(rline,new Vector2(973,0));Cais=rline[RiverCaisT];
            // the entry road: the gate, the saddle, the Portela, the co-op, the river's north T
            var eline=QuarryHills.Profile(gateEnd,Wv((-450,1030),(-300,830),(-160,700),(-20,715),(150,750),(330,800),(520,860),(720,930),(900,985),(1080,1050)),rline[RiverNorthT],.09f,30,30);
            var entry=HillcrestRoute.Make(EntryName,eline,8,6,40);EntrySaddle=VertexNear(eline,new Vector2(-160,700));
            // Rua Direita: the saddle to the Largo
            var dline=QuarryHills.Profile(eline[EntrySaddle],Wv((-152,655),(-100,500),(-40,250)),Largo,.08f,25,30);
            var rua=HillcrestRoute.Make(RuaDireitaName,dline,8,5,30);
            // the Calçada: the Largo to the Cais, in Z bends
            var cline=QuarryHills.Profile(Largo,Wv((60,-25),(150,-45),(250,-80),(330,-130),(480,100),(700,-60),(850,40)),Cais,.14f,20,30);
            var calcada=HillcrestRoute.Make(CaisRoadName,cline,6.5f,5,25);
            // the Mirante road: the Largo to the Belvedere's knoll
            var mline=QuarryHills.Profile(Largo,Wv((-60,-90),(-150,-180),(-300,-330),(-445,-472)),Ground(new Vector2(-590,-614)),.11f,20,30);
            var mirante=HillcrestRoute.Make(MiranteName,mline,6.5f,5,25);Belvedere=mline[^1];
            // the old Vale road: the Largo west down to the Vale
            var vline=QuarryHills.Profile(Largo,Wv((-160,20),(-300,50),(-400,10)),Ground(new Vector2(-470,-20)),.12f,20,30);
            var vale=HillcrestRoute.Make(ValeRoadName,vline,6,5,25);
            list.AddRange(new[]{river,entry,rua,calcada,mirante,vale});
            Debug.Log($"LITTLE_ROAD_VINELANDS_ROADS entry={entry.Length:0} m river={river.Length:0} m rua direita={rua.Length:0} m calcada={calcada.Length:0} m mirante={mirante.Length:0} m vale={vale.Length:0} m largo y={Largo.y:0.0} saddle y={eline[EntrySaddle].y:0.0} cais y={Cais.y:0.0} belvedere y={Belvedere.y:0.0}");
            return list;
        }
    }
}

using System.Linq;
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
        public static Vector3 Largo,Adega,Cais,Belvedere,PitStopAt,Terreiro;public static float PitStopYaw;
        static Vector2 W(float a,float n)=>Centre+new Vector2(a,n);
        static Vector2 F(float x,float z)=>new Vector2(x,z)-Centre;
        public static bool Contains(float x,float z)=>Enabled&&(new Vector2(x,z)-Centre).sqrMagnitude<Radius*Radius;
        public static bool Core(float x,float z)=>Enabled&&(new Vector2(x,z)-Centre).sqrMagnitude<460*460;   // (the vila: the Largo, its three streets and the Pit Stop at the town gate)
        static float S(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        // The river's surface at a frame point: Water through the district, falling to the sea over its last 180 m before the
        // coast (rapids into the bay; at Water to the edge it hung 18 m over the beach, zone 8 round 3).
        public static float WaterAt(Vector2 f)=>Mathf.Lerp(Water,CoastalLandscape.SeaLevel+.3f,S(1380,1560,f.magnitude));

        // ---- the working slope's parcels (zone 4, docs/89; the town plan section 5): who owns each piece of the amphitheatre,
        // and so what grows there. First match wins (the abandoned corners inside the quintas' parcels come first).
        public enum Kind{Velha,Nova,Courela,Mortorio,Laranjal,Olival}
        public static readonly (string name,Kind kind,Vector2[] poly)[] Parcels={
            ("Mortorio da Vinha Alta",Kind.Mortorio,new Vector2[]{new(230,520),new(380,540),new(370,650),new(210,630)}),
            ("Mortorio do Alto",Kind.Mortorio,new Vector2[]{new(180,-430),new(430,-400),new(440,-210),new(200,-190)}),
            ("Mortorio do Fojo",Kind.Mortorio,new Vector2[]{new(100,-540),new(380,-540),new(390,-860),new(120,-860)}),
            ("Laranjal do Pombal",Kind.Laranjal,new Vector2[]{new(440,330),new(520,330),new(520,430),new(440,430)}),
            ("Vinha Nova",Kind.Nova,new Vector2[]{new(620,250),new(980,260),new(1000,420),new(640,480)}),
            ("Socalcos Velhos",Kind.Velha,new Vector2[]{new(300,250),new(620,250),new(640,480),new(500,560),new(300,520)}),
            ("Vinha Alta",Kind.Velha,new Vector2[]{new(250,520),new(500,560),new(480,680),new(280,700)}),
            ("Lajes de Baixo",Kind.Velha,new Vector2[]{new(700,-420),new(980,-400),new(990,-120),new(720,-100)}),
            ("Lajes do Meio",Kind.Velha,new Vector2[]{new(430,-420),new(700,-420),new(720,-160),new(440,-150)}),
            ("Fojo Fundo",Kind.Velha,new Vector2[]{new(600,-1000),new(1000,-960),new(1020,-560),new(620,-540)}),
            ("Courelas de Cima",Kind.Courela,new Vector2[]{new(60,-330),new(290,-330),new(300,650),new(110,650)}),
            ("Vinhas do Casal",Kind.Velha,new Vector2[]{new(290,-160),new(720,-160),new(720,250),new(290,250)}),
            ("Mortorio das Voltas",Kind.Mortorio,new Vector2[]{new(-100,-1250),new(280,-1250),new(280,-900),new(-100,-900)}),
            ("Vinhas das Voltas",Kind.Velha,new Vector2[]{new(280,-1250),new(1000,-1250),new(1000,-1000),new(600,-1000),new(390,-860),new(280,-900)}),
            ("Olival do Cais",Kind.Olival,new Vector2[]{new(720,-100),new(990,-120),new(990,260),new(720,250)})};
        static bool InPoly(Vector2[] poly,Vector2 p){bool c=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)if((poly[i].y>p.y)!=(poly[j].y>p.y)&&p.x<(poly[j].x-poly[i].x)*(p.y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)c=!c;return c;}
        public static (string name,Kind kind)? ParcelAt(Vector2 frame){foreach(var (name,kind,poly) in Parcels)if(InPoly(poly,frame))return (name,kind);return null;}
        // (Vinha Nova is planted up and down the slope, the modern way: no terraces; they fade out over its edge's 20 m)
        static float NovaMask(Vector2 p){if(p.x<600||p.x>1010||p.y<240||p.y>490)return 0;var poly=Parcels[4].poly;return InPoly(poly,p)?S(0,20,EdgeDistance(poly,p)):0;}

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
            float ra=AtN(River,p.y);var (ca,hc)=CrestAt(p.y);float h;float bowl=S(-650,-850,p.y)*S(-450,-250,p.x);float entr=S(760,900,p.y)*S(560,720,p.x);float farb=S(-100,-40,p.x-ra)*S(-650,-450,p.y)*(1-S(450,650,p.y));   // (the far bank facing the town, zone 8: no rim wall over it)   // (the south bowl: the Seis Voltas, zone 5; the Entroncamento, zone 6)
            if(p.x>=ra)h=Water+140*S(0,700,p.x-ra)*(.75f+.5f*n3)*(1-.75f*S(1100,1600,d))+(n2-.5f)*3;   // (the far bank, lower towards the coast: round 1's 200 m cliff into the sea)
            else if(p.x>=ca) {   // (the amphitheatre, stepped into terraces)
                float s=(p.x-ca)/Mathf.Max(1,ra-ca);s=1-s;h=Water+(hc-Water)*(s*(.55f+.45f*s))+(n2-.5f)*2.5f;
                float mask=S(100,130,RiverDistance(p))*S(30,80,p.x-ca)*(1-S(1250+220*bowl,1450+220*bowl,d))*(1-InTown(p))*(1-NovaMask(p));   // (not under the town: its houses stand on the natural slope)
                if(mask>0)h=Mathf.Lerp(h,Terrace(h),mask);
            }
            else{   // (the Vale: a steep upper flank, then a broad shelf falling about 4.5% west, the Vineyard Loop's ground)
                float w=ca-p.x,shelf=hc-55,vale=48+(n1-.5f)*10;   // (a 400 m flank: at 250 m it was 22%, and Adega Lane stood on an embankment off the Largo)
                h=w<400?Mathf.Lerp(hc,shelf,S(0,400,w)):Mathf.Lerp(shelf,vale,S(400,1400,w));h+=(n2-.5f)*3;
            }
            // The river: about 100 m bank to bank and 5 m deep (round 1's 40 m ditch hid its water), its banks a metre or two
            // over the water before the slopes rise.
            float dr=RiverDistance(p),wl=WaterAt(p);bool channel=dr<100;if(channel)h=Mathf.Lerp(wl-5,Mathf.Max(h,wl+1.5f),S(45,100,dr));
            // Rim hills round the district (none towards Bramblewick, whose own hills frame that side); the river leaves
            // through gaps in them.
            float dB=Vector2.Distance(new(x,z),Bramblewick.Centre);
            // (none over the south bowl, the Seis Voltas' slope from the river to the knoll: zone 5 found its start on the rim, docs/90)
            // (nor over the Entroncamento, the co-op quarter by the river road: zone 6 found it in a 100 m cutting through the rim and the vale's hills)
            // (rising from the banks over 200 m: switched on at the channel's edge they stood as a cliff up to 55 m tall along
            // both banks, the dark scars across the far bank and the sea gap, zone 8 round 2)
            h+=55*S(100,300,dr)*S(1250,1500,d)*(1-S(1500,1750,d))*S(1000,1600,dB)*(.7f+.6f*n3)*(1-bowl)*(1-entr)*(1-farb);
            // Towards Bramblewick the district blends into its vale.
            // (not in the river's channel: blended up to the vale's height it buried the river under a hill north of the Aldeia,
            // its water ending in a straight edge, zone 8 round 3)
            h=Mathf.Lerp(h,valley,S(1100,1500,d)*(1-bowl)*(1-entr)*(1-farb)*S(70,170,dr));
            // The coast: land only within the district or Bramblewick's hills (east of Bramblewick, the valley's own land
            // beyond them would stand on the sea floor, round 1's floating slab); everything falls to the sea bed by the edge.
            float land=Mathf.Max(1-S(1450+220*bowl,Radius,d),   // (the bowl's land reaches the river at the Seis Voltas' start)
                x<2300?1:1-S(1350,1550,dB));
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
            {var pc=W(946,2);yield return (pc,26,42,Cais.y);}   // (the Praça do Cais, level: docs/88)
            yield return (new Vector2(Terreiro.x,Terreiro.z),46,66,Terreiro.y);
            yield return (new Vector2(PitStopAt.x,PitStopAt.z),27,38,PitStopAt.y);   // (the Pit Stop, level with Rua Direita: docs/88)
            foreach(var q in QuayPads)yield return (new Vector2(q.x,q.z),15,20,q.y);
            foreach(var j in JunctionPads)yield return (new Vector2(j.x,j.z),11,22,j.y);
            yield return (new Vector2(Belvedere.x,Belvedere.z),26,44,Belvedere.y);
            if(Lap!=null){var pk=OnGround(new Vector2(-625,-62));yield return (new Vector2(pk.x,pk.z),46,70,pk.y);}   // (the circuit's paddock at the infield's own height, blended wide: at the main straight's height it stood as a table with cliffs, round 2)
            {var g=OnGround(new Vector2(0,840));yield return (new Vector2(g.x,g.z),66,90,g.y);}   // (the football ground and its courts, beyond the entry road's fill: docs/91)
            foreach(var (c,_,inner,outer) in Sites.Values)yield return (new Vector2(c.x,c.z),inner,outer,c.y-.05f);
            foreach(var (c,_,_) in AmendoeirasPlots)yield return (new Vector2(c.x,c.z),9,16,c.y-.05f);   // (the knoll's car park and terrace: docs/90)
            {var v=W(1050,-935);yield return (v,30,48,Veiga.y);}   // (the Veiga do Fojo, the climb's paddock, north of its start: level with the river road)
            foreach(var (a,n,r) in new[]{(475f,398f,38f),(565f,-288f,38f),(432f,-705f,38f),(575f,58f,34f)}){var g=OnGround(new Vector2(a,n));yield return (new Vector2(g.x,g.z),r,r+16,g.y);}   // (the quintas' yards and Casal do Meio's threshing floor, docs/89)
            if(Ponte!=null){var e=W(1110,-71);yield return (e,7,14,PonteDeck);var t=Ponte[^1];yield return (new Vector2(t.x,t.z),11,20,t.y);}
            if(CorvoLine!=null){var t=CorvoLine[^1];var d=CorvoLine[^1]-CorvoLine[^6];d.y=0;d.Normalize();var ru=t+d*20;yield return (new Vector2(t.x,t.z),12,22,t.y);yield return (new Vector2(ru.x,ru.z),16,28,t.y);   // (the Corvo's yard and the ruin's terrace, at the lane's end)
                var o=OutraLine[^1];var g=W(AldeiaAt.x,AldeiaAt.y);yield return (g,34,52,o.y);}   // (the Aldeia's spur, levelled at the lane's height: zone 8)   // (the Corvo's yard at the lane's end; the Aldeia's spur, levelled: zone 8)   // (the far abutment and the far-bank lane's turning space: their seams, docs/88)   // (the quay, level along the river: docs/88)   // (the Terreiro da Feira, 80 x 40 m: a terrace cut into the slope, docs/87)
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
            CaisRoadName="Estrada do Cais",MiranteName="Estrada do Mirante",ValeRoadName="Caminho Velho do Vale",FeiraName="Rua da Feira",PonteName="Ponte Nova";
        public static int EntrySaddle,RiverNorthT,RiverCaisT,MiranteFeira,PonteRiverT,RiverClimbT;
        // the Subida das Seis Voltas (zone 5, docs/90; track-design skill, review/tmp/sv3.txt): from the river road at the Veiga do
        // Fojo up the south bowl in six voltas to the Belvedere's knoll, 2.2 km, about 10 %. Piece list in the frame, heading 180.
        public const string ClimbName="Subida das Seis Voltas";
        // the Circuito da Cumeada (zone 7, docs/92; track-design skill, review/tmp/cc4.txt): the club circuit on the Vale shelf,
        // farm lanes closed on race days, counter-clockwise from the start line at (-470,-260); 1.85 km. And its long version
        // for the faster classes (Richard's short-and-long idea, here): off the west leg before the Gancho, south through the
        // montado and back to the lap after the chicane, about 2.4 km.
        public const string CircuitName="Circuito da Cumeada",ExtName="Circuito da Cumeada - Montado";
        public const string CircuitSpec="M Recta da Eira; S 369; M Curva da Eira Velha; A 40 90; S 40; M Lomba do Alto; A -18 140; S 40; A 68 80; S 134; M Curva do Carvalhal; A 85 75; S 130; M Esses do Amendoal; A 28 100; S 30; A -28 100; S 110; A -20 160; S 70; M Gancho do Sobreiro; A 110 22; S 190; M Chicane da Ribeira; A -40 40; A 40 40; S 100; M Curva da Fonte; A 95 45";
        public const string ExtSpec="A -55 50; S 110; A 55 110; S 150; A 30 120; M Curva do Montado; A 70 40; S 350; M Curva dos Sobreiros; A 85 45; S 20";   // (off the west leg after the esses, out round the Gancho's outside: from 30 m before the Gancho its junction overlapped the hairpin, round 1)
        public static Vector3[] LapLong;public static int LapA,LapB,LapVale;public static readonly List<(string name,Vector2 at)> CircuitMarks=new();
        static float Length(Vector3[] l){float t=0;for(int i=1;i<l.Length;i++)t+=Vector3.Distance(l[i],l[i-1]);return t;}
        static List<Vector2> Pieces(string spec,float x,float y,float h,List<(string,Vector2)> marks)
        {
            var o=new List<Vector2>{new(x,y)};
            foreach(var part in spec.Split(';')) {
                var t=part.Trim().Split(' ');if(t.Length<2)continue;
                if(t[0]=="M"){marks?.Add((string.Join(" ",t,1,t.Length-1),new(x,y)));continue;}
                if(t[0]=="S"){float L=float.Parse(t[1],System.Globalization.CultureInfo.InvariantCulture);int n=Mathf.Max(1,Mathf.CeilToInt(L/5));for(int i=0;i<n;i++){x+=L/n*Mathf.Cos(h*Mathf.Deg2Rad);y+=L/n*Mathf.Sin(h*Mathf.Deg2Rad);o.Add(new(x,y));}}
                if(t[0]=="A"){float d=float.Parse(t[1],System.Globalization.CultureInfo.InvariantCulture),R=float.Parse(t[2],System.Globalization.CultureInfo.InvariantCulture);int n=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(d)*Mathf.Deg2Rad*R/3));
                    for(int i=0;i<n;i++){float s=d>0?1:-1;float h0=h;h+=d/n;float cx=x-s*R*Mathf.Sin(h0*Mathf.Deg2Rad),cy=y+s*R*Mathf.Cos(h0*Mathf.Deg2Rad);x=cx+s*R*Mathf.Sin(h*Mathf.Deg2Rad);y=cy-s*R*Mathf.Cos(h*Mathf.Deg2Rad);o.Add(new(x,y));}}
            }
            return o;
        }
        public const string VoltasSpec="M Veiga Start; S 60; A 32 180; S 120; M Volta do Rio; A -82 30; S 210; M Volta das Lajes; A 88 26; S 180; A -10 220; M Volta do Fojo; A -86 22; S 200; A 12 160; M Volta da Oliveira; A 92 20; S 180; M Volta da Capela; A -95 22; S 140; M Volta do Moinho; A 90 22; S 120; M Gancho do Alto; A -80 40; S 200; A 16 160; S 160; M Belvedere";
        public static readonly List<(string name,Vector2 at)> ClimbMarks=new();public static Vector3 Veiga;
        static List<Vector2> ClimbFrame()
        {
            var o=new List<Vector2>();ClimbMarks.Clear();float x=1115,y=-1000,h=180;o.Add(new(x,y));
            foreach(var part in VoltasSpec.Split(';')) {
                var t=part.Trim().Split(' ');if(t.Length<2)continue;
                if(t[0]=="M"){ClimbMarks.Add((string.Join(" ",t,1,t.Length-1),new(x,y)));continue;}
                if(t[0]=="S"){float L=float.Parse(t[1],System.Globalization.CultureInfo.InvariantCulture);int n=Mathf.Max(1,Mathf.CeilToInt(L/6));for(int i=0;i<n;i++){x+=L/n*Mathf.Cos(h*Mathf.Deg2Rad);y+=L/n*Mathf.Sin(h*Mathf.Deg2Rad);o.Add(new(x,y));}}
                if(t[0]=="A"){float d=float.Parse(t[1],System.Globalization.CultureInfo.InvariantCulture),R=float.Parse(t[2],System.Globalization.CultureInfo.InvariantCulture);int n=Mathf.Max(2,Mathf.CeilToInt(Mathf.Abs(d)*Mathf.Deg2Rad*R/4));
                    for(int i=0;i<n;i++){float s=d>0?1:-1;float h0=h;h+=d/n;float cx=x-s*R*Mathf.Sin(h0*Mathf.Deg2Rad),cy=y+s*R*Mathf.Cos(h0*Mathf.Deg2Rad);x=cx+s*R*Mathf.Sin(h*Mathf.Deg2Rad);y=cy-s*R*Mathf.Cos(h*Mathf.Deg2Rad);o.Add(new(x,y));}}
            }
            return o;
        }
        public const string AmendoeirasName="Rua das Amendoeiras";public static int RuaAmendoeirasT;
        // building sites on their own level platforms at their road's height (zone 6: the saddle and the flank are steep, and
        // the buildings rose 6-9 m into them, round 1): the ground (Pads) and the buildings (VinelandsPortela) use the same list
        public static readonly Dictionary<string,(Vector3 c,float yaw,float inner,float outer)> Sites=new();public static readonly List<(Vector3 c,float yaw,int side)> AmendoeirasPlots=new();
        static void Site(string name,HillcrestRoute.Road road,Vector2 near,Vector2 toward,float off,float inner,float outer)
        {
            var w=W(near.x,near.y);float best=0,bd=float.MaxValue;for(float s=0;s<road.Length;s+=1){var q=road.At(s);float d=(new Vector2(q.x,q.z)-w).sqrMagnitude;if(d<bd){bd=d;best=s;}}
            var at=road.At(best);var n=road.Right(best);n.y=0;n.Normalize();var t=W(toward.x,toward.y);if(Vector2.Dot(new Vector2(n.x,n.z),t-new Vector2(at.x,at.z))<0)n=-n;
            var c=at+n*(road.width*.5f+off);c.y=at.y;var back=at-c;back.y=0;Sites[name]=(c,Quaternion.LookRotation(back).eulerAngles.y,inner,outer);
        }
        public const string MeioName="Estrada do Meio",PombalTrack="Caminho da Quinta do Pombal",LajesTrack="Caminho da Quinta das Lajes",FojoTrack="Caminho da Quinta do Fojo";
        public static bool Track(string name)=>Enabled&&(name==PombalTrack||name==LajesTrack||name==FojoTrack);
        public static int CaisMeioT,EntryMeioT,MeioCasalT;public static readonly List<int> TrackStarts=new();public static readonly List<Vector3> JunctionPads=new();public static Vector3 CasalDoMeio;
        public const float PitN=402;public static Vector3[] Ponte,QuayPads,CorvoLine,OutraLine;public const string CorvoName="Caminho do Corvo",OutraName="Caminho da Outra Banda";public static readonly Vector2 AldeiaAt=new(1270,180);public static float PonteDeck;
        static Vector2[] Wv(params (float a,float n)[] f){var o=new Vector2[f.Length];for(int i=0;i<f.Length;i++)o[i]=W(f[i].a,f[i].n);return o;}
        static int VertexNear(Vector3[] line,Vector2 frame){var w=W(frame.x,frame.y);int best=0;float bd=float.MaxValue;for(int i=0;i<line.Length;i++){float d=(new Vector2(line[i].x,line[i].z)-w).sqrMagnitude;if(d<bd){bd=d;best=i;}}return best;}
        public static List<HillcrestRoute.Road> Roads(Vector3 gateEnd)
        {
            var list=new List<HillcrestRoute.Road>();Marks.Clear();Lap=null;
            Largo=OnGround(Vector2.zero);Cais=OnGround(new Vector2(960,0));
            // the river road, north to south, from the beach at the river's mouth: 60 m past the entry road's T (on to a 1173 n 1320
            // it ran out on a sand spit into the sea; ending at the T, the two-armed corner stood 2.4 m off the beach, zone 8 rounds 4-5)
            var rline=QuarryHills.Profile(Ground(new Vector2(1143,1128)),Wv((1131,1070),(1085,800),(1011,400),(965,150),(973,0),(995,-400),(1075,-800),(1115,-1000)),Ground(new Vector2(1089,-1320)),.06f,30,0);
            var river=HillcrestRoute.Make(RiverRoadName,rline,7.5f,11,30);   // (11 m level each side: the Passeio and the quay stand on it, docs/88)
            RiverNorthT=VertexNear(rline,new Vector2(1131,1070));RiverCaisT=VertexNear(rline,new Vector2(973,0));Cais=rline[RiverCaisT];
            QuayPads=new Vector3[7];for(int k=0;k<7;k++){float qn=-60+20*k;var w=W(986,qn);QuayPads[k]=new Vector3(w.x,rline[VertexNear(rline,new Vector2(973,qn))].y,w.y);}
            // Ponte Nova (zone 3, docs/88): off the river road at n -70, up 2.6 m to its deck over the abutments (a 998-1108), a
            // 0.8 m rise to the middle (the cais_ponte kit's own curve), down to the far bank's foot at a 1135
            PonteRiverT=VertexNear(rline,new Vector2(977,-70));Vector3[] pline;
            {var j=rline[PonteRiverT];float ja=j.x-Centre.x;PonteDeck=j.y+2.6f;var pts=new List<Vector3>{j};
                for(float a=ja+2;a<=1108.01f;a+=2f){float y;if(a<998)y=Mathf.Lerp(j.y,PonteDeck,Mathf.SmoothStep(0,1,(a-ja)/(998-ja)));else if(a<=1108){float u=(a-1053)/55f;y=PonteDeck+.8f*(1-u*u);}else y=PonteDeck;
                    var wp=W(a,-70);pts.Add(new Vector3(wp.x,y,wp.y));}
                // (the bridge ends level at the far bank's T, a 1122: zone 8's two lanes leave it, south to the Quinta do Corvo and
                // north up to the Aldeia; straight on, a road cut into the hill like a tunnel mouth, zone 3 round 3)
                for(float a=1110;a<=1122.01f;a+=2){var wp=W(a,-70);pts.Add(new Vector3(wp.x,PonteDeck,wp.y));}
                Ponte=pline=pts.ToArray();}
            {var t0=Ponte[^1];
                // (each lane's end no higher than its grade can reach from the bridge: the far bank rises 60 m in 150 m, and lanes
                // aimed at the natural ground broke into steps, round 1)
                Vector3 Reach(Vector3 from,Vector2[] via,Vector2 end,float grade){float L=0;var prev=new Vector2(from.x,from.z);foreach(var v in via){L+=Vector2.Distance(prev,v);prev=v;}var e=W(end.x,end.y);L+=Vector2.Distance(prev,e);
                    return new Vector3(e.x,Mathf.Min(ValleyField.Natural(e.x,e.y),from.y+grade*(L-45)),e.y);}
                var corvoV=Wv((1128,-90),(1136,-130),(1142,-190),(1146,-250));var corvoL=QuarryHills.Profile(t0,corvoV,Reach(t0,corvoV,new Vector2(1152,-300),.1f),.1f,20,20);
                CorvoLine=corvoL;
                // (to the Aldeia by a switchback: north along the bank's foot, a hairpin, back up south-east to the spur)
                var ov=Wv((1128,-50),(1136,0),(1146,100),(1158,190),(1172,262),(1196,282),(1222,262),(1240,226));var oline=QuarryHills.Profile(t0,ov,Reach(t0,ov,new Vector2(1252,196),.13f),.13f,20,20);
                OutraLine=oline;
                foreach(var c in new[]{new Vector2(1250,200),new Vector2(1330,250),new Vector2(1420,280),new Vector2(1500,300),new Vector2(1200,-280),new Vector2(1300,-250),new Vector2(1450,-250)})Debug.Log($"LITTLE_ROAD_VINELANDS_FARBANK at {c} y={OnGround(c).y:0.0}");}
            // the entry road: the gate, the saddle, the Portela, the co-op, the river's north T
            // (in two profiles meeting level at the Adega's junction (900, 985), where the Estrada do Meio arrives: on the 9 %
            // grade the junction stood 1.9 m off its arms, zone 4 round 1)
            // (at the height the road had there in one piece: the natural ground there is 112 m, and the road fell 11 m in a
            // step to reach the river from it, round 2)
            var whole=QuarryHills.Profile(gateEnd,Wv((-450,1030),(-300,830),(-160,700),(-20,715),(150,750),(330,800),(520,860),(720,930),(900,985),(1080,1050)),rline[RiverNorthT],.09f,30,30);
            var adega=whole[VertexNear(whole,new Vector2(900,985))];
            var ea=QuarryHills.Profile(gateEnd,Wv((-450,1030),(-300,830),(-160,700),(-20,715),(150,750),(330,800),(520,860),(720,930)),adega,.09f,30,30);
            var eb=QuarryHills.Profile(ea[^1],Wv((1080,1050)),rline[RiverNorthT],.09f,30,30);
            var el=new List<Vector3>(ea);for(int i=1;i<eb.Length;i++)el.Add(eb[i]);var eline=el.ToArray();EntryMeioT=ea.Length-1;
            var entry=HillcrestRoute.Make(EntryName,eline,8,6,40);EntrySaddle=VertexNear(eline,new Vector2(-160,700));
            Sites.Clear();Site("bombeiros",entry,new Vector2(-15,708),new Vector2(-15,680),15.3f,20,32);Site("escola",entry,new Vector2(110,735),new Vector2(110,690),18.8f,26,38);
            // Rua Direita: the saddle to the Largo
            var dline=QuarryHills.Profile(eline[EntrySaddle],Wv((-152,655),(-125,575),(-80,480),(-98,390),(-70,300),(-25,205),(-32,110),(-12,50)),Largo,.08f,25,30);   // (gentle bends, the old carriage road's: docs/87 round 2)
            var rua=HillcrestRoute.Make(RuaDireitaName,dline,8,5,30);
            // Rua das Amendoeiras (zone 6, docs/91): the Portela's contour street off Rua Direita, west along the flank
            RuaAmendoeirasT=VertexNear(dline,new Vector2(-112,560));
            var amLine=QuarryHills.Profile(dline[RuaAmendoeirasT],Wv((-180,572),(-240,586),(-300,622)),Ground(new Vector2(-335,690)),.1f,20,20);
            var amendoeiras=HillcrestRoute.Make(AmendoeirasName,amLine,6f,5,20);
            AmendoeirasPlots.Clear();
            Site("saude",rua,new Vector2(-110,600),new Vector2(-60,600),13.3f,16,26);
            foreach(int side in new[]{1,-1})for(float s=26;s<amendoeiras.Length-14;s+=21) {
                var at=amendoeiras.At(s);var n=amendoeiras.Right(s);n.y=0;n.Normalize();n*=side;var c=at+n*(3+4.5f+6.5f);c.y=at.y;var back=at-c;back.y=0;
                AmendoeirasPlots.Add((c,Quaternion.LookRotation(back).eulerAngles.y,side));
            }
            // the Pit Stop's site (zone 2): off the street's west side at n 402, set back 23 m for its forecourt, on its own level pad
            // at the street's height (on the falling slope it had the hill in its bays: docs/88)
            {int pi=0;float bd=float.MaxValue;for(int i=0;i<dline.Length;i++){float d=Mathf.Abs(dline[i].z-(Centre.y+PitN));if(d<bd){bd=d;pi=i;}}
                var p=dline[pi];var dir=dline[Mathf.Min(pi+3,dline.Length-1)]-dline[Mathf.Max(pi-3,0)];dir.y=0;dir.Normalize();var wv=new Vector3(-dir.z,0,dir.x);if(wv.x>0)wv=-wv;
                PitStopAt=p+wv*23f;PitStopAt.y=p.y;var back=p-PitStopAt;back.y=0;PitStopYaw=Quaternion.LookRotation(back).eulerAngles.y;}
            // the Calçada: the Largo to the Cais, in Z bends
            // (in two profiles meeting level at Casal do Meio, where the Estrada do Meio crosses it: zone 4, docs/89)
            var c1=QuarryHills.Profile(Largo,Wv((60,-25),(150,-45),(250,-80),(330,-130),(480,100)),Ground(new Vector2(590,20)),.14f,20,30);
            var c2=QuarryHills.Profile(c1[^1],Wv((700,-60),(850,40)),Cais,.14f,20,30);
            var cl=new List<Vector3>(c1);for(int i=1;i<c2.Length;i++)cl.Add(c2[i]);var cline=cl.ToArray();CaisMeioT=c1.Length-1;CasalDoMeio=c1[^1];
            var calcada=HillcrestRoute.Make(CaisRoadName,cline,6.5f,5,25);
            // the Estrada do Meio (zone 4): the tractor road along the mid-slope, from the Fojo's gate north through Casal do Meio
            // to the entry road by the Adega; and the three quintas' zig-zag tracks up from it
            var e1=QuarryHills.Profile(Ground(new Vector2(700,-890)),Wv((640,-790),(595,-735),(580,-700),(615,-500),(620,-300),(632,-140)),CasalDoMeio,.1f,20,30);   // (on past the Fojo's gate as the plan's Ligacao do Fojo: a track meeting the road's very end left a curb step, round 5)
            var e2=QuarryHills.Profile(CasalDoMeio,Wv((660,140),(680,300),(700,500),(760,720),(850,860),(895,925)),eline[EntryMeioT],.1f,20,30);   // (arriving from the south, square to the entry road: at 29 degrees the junction ballooned, round 3)
            var meio=new List<Vector3>(e1);for(int i=1;i<e2.Length;i++)meio.Add(e2[i]);var meioLine=meio.ToArray();MeioCasalT=e1.Length-1;
            var estradaMeio=HillcrestRoute.Make(MeioName,meioLine,5.5f,5,20);
            var tracks=new List<HillcrestRoute.Road>();TrackStarts.Clear();JunctionPads.Clear();
            foreach(var (name,from,via,end) in new[]{(PombalTrack,new Vector2(690,400),Wv((560,370),(470,380),(400,450),(330,400)),new Vector2(270,440)),
                (LajesTrack,new Vector2(622,-300),Wv((560,-300),(480,-250),(400,-310),(320,-260)),new Vector2(260,-300)),
                (FojoTrack,new Vector2(580,-700),Wv((430,-720),(340,-760),(280,-700)),new Vector2(210,-740))}) {
                int mi=VertexNear(meioLine,from);var tl=QuarryHills.Profile(meioLine[mi],via,Ground(end),.16f,15,12);
                tracks.Add(HillcrestRoute.Make(name,tl,4f,3,12));TrackStarts.Add(mi);JunctionPads.Add(meioLine[mi]);
            }
            JunctionPads.Add(CasalDoMeio);JunctionPads.Add(eline[EntryMeioT]);JunctionPads.Add(rline[RiverNorthT]);   // (level ground round the junctions at their roads' height: zone 4's lips)
            // the Mirante road: the Largo to the Belvedere's knoll
            // (in two profiles meeting level at the bus terminus (-95,-125), on the same line: Rua da Feira leaves there, and a
            // junction on the 11 % grade sank under the carved ground, docs/87)
            var m1=QuarryHills.Profile(Largo,Wv((-60,-90)),Ground(new Vector2(-95,-125)),.11f,20,20);
            var m2=QuarryHills.Profile(m1[^1],Wv((-150,-180),(-300,-330),(-445,-472)),Ground(new Vector2(-590,-614)),.11f,20,20);
            var ml=new List<Vector3>(m1);for(int i=1;i<m2.Length;i++)ml.Add(m2[i]);var mline=ml.ToArray();
            var mirante=HillcrestRoute.Make(MiranteName,mline,6.5f,5,25);Belvedere=mline[^1];
            // the Seis Voltas: from the river road's vertex at the Veiga up the turtle's line to the Belvedere (the Mirante's end)
            RiverClimbT=VertexNear(rline,new Vector2(1115,-1000));var cf=ClimbFrame();var cvl=new List<Vector2>();for(int i=1;i<cf.Count;i++)cvl.Add(W(cf[i].x,cf[i].y));
            {var a=cvl[^1];var b=new Vector2(Belvedere.x,Belvedere.z);int k=Mathf.CeilToInt(Vector2.Distance(a,b)/6);for(int i=1;i<k;i++)cvl.Add(Vector2.Lerp(a,b,i/(float)k));}   // (evenly spaced to the knoll: one long last segment made the spline loop back, round 3)
            var cv=cvl.ToArray();
            var climbLine=QuarryHills.Profile(rline[RiverClimbT],cv,Belvedere,.15f,14,20);var climb=HillcrestRoute.Make(ClimbName,climbLine,7.5f,5,20);
            JunctionPads.Add(rline[RiverClimbT]);Veiga=rline[RiverClimbT];JunctionPads.Add(dline[RuaAmendoeirasT]);
            {float worst=0;int wi=0;for(int i=1;i<climbLine.Length;i++){float g=Mathf.Abs(climbLine[i].y-climbLine[i-1].y)/Mathf.Max(.01f,Vector2.Distance(new(climbLine[i].x,climbLine[i].z),new(climbLine[i-1].x,climbLine[i-1].z)));if(g>worst){worst=g;wi=i;}}
                float minSeg=float.MaxValue;for(int i=1;i<climbLine.Length;i++)minSeg=Mathf.Min(minSeg,Vector2.Distance(new(climbLine[i].x,climbLine[i].z),new(climbLine[i-1].x,climbLine[i-1].z)));
                foreach(var q in climbLine)if(Vector2.Distance(new(q.x,q.z),new(2944,-4024))<8)Debug.Log($"LITTLE_ROAD_VINELANDS_CLIMB_PT {q}");
                Debug.Log($"LITTLE_ROAD_VINELANDS_CLIMB length={climb.Length:0} m rise={Belvedere.y-rline[RiverClimbT].y:0.0} m steepest={worst*100:0.0}% at {climbLine[wi]} shortest segment={minSeg:0.00} m points={climbLine.Length}");}
            // Rua da Feira (zone 2, docs/87): off the Mirante road at the bus terminus, past the market hall to the Terreiro
            Terreiro=OnGround(new Vector2(50,-200));MiranteFeira=m1.Length-1;
            // (the Terreiro is a terrace cut and filled into the slope no lower than 15 m under the terminus, and the street winds
            // round the slope's shoulder to arrive level at its south-west corner: lower, its profile had no solution and fell 9 m
            // in its first two metres (round 2); ending in the middle, it ran on down across the square in a hump (round 4))
            Terreiro.y=Mathf.Max(Terreiro.y,mline[MiranteFeira].y-15f);var tend=W(13,-212);
            var fline=QuarryHills.Profile(mline[MiranteFeira],Wv((-60,-152),(-25,-180),(-8,-214)),new Vector3(tend.x,Terreiro.y,tend.y),.13f,20,20);
            var feira=HillcrestRoute.Make(FeiraName,fline,6.5f,5,25);
            Debug.Log($"LITTLE_ROAD_VINELANDS_FEIRA terminus y={mline[MiranteFeira].y:0.0} terreiro y={Terreiro.y:0.0} feira {fline[0].y:0.0}..{fline[^1].y:0.0}");
            // the old Vale road: the Largo west down to the Vale
            // the circuit: the lap from the start line round to it again (its last point dropped: closed), then the extension
            CircuitMarks.Clear();var cfp=Pieces(CircuitSpec,-470,-260,90,CircuitMarks);var cw=new Vector2[cfp.Count-2];for(int i=1;i<cfp.Count-1;i++)cw[i-1]=W(cfp[i].x,cfp[i].y);
            // (profiled in four pieces meeting level at its three junctions, each on a 20 m landing: on the lap's grade the
            // extension's junction stood 1.3 m off its arms, round 2)
            int Near(Vector2 f){int best=0;float bd=float.MaxValue;for(int i=0;i<cfp.Count;i++){float d=(cfp[i]-f).sqrMagnitude;if(d<bd){bd=d;best=i;}}return best;}
            var cuts=new[]{0,Near(new(-470,-20)),Near(new(-829,-30)),Near(new(-575,-301)),cfp.Count-1};var lapPts=new List<Vector3>();
            // (each junction at the height the whole lap's profile gives it there: pinned to the natural ground, a piece could
            // need 12 % and broke into 6 m steps, round 3)
            var lapWhole=QuarryHills.Profile(Ground(new Vector2(-470,-260)),cw,Ground(new Vector2(-470,-260)),.08f,30,0);
            Vector3 At(int ci){var f=cfp[ci];var w=W(f.x,f.y);var q=lapWhole[VertexNear(lapWhole,f)];return new Vector3(w.x,q.y,w.y);}
            // (and each within what its piece can climb from the last: 8 % less its two 1 % landings. On the whole lap's grade the
            // main straight needed 19 m in 240 m, its profile ended in a 2 m step at both ends and the whole grid stuck, zone 8's race)
            var hj=new float[4];var lc=new float[4];for(int c=0;c<4;c++){hj[c]=At(cuts[c]).y;for(int i=cuts[c]+1;i<=cuts[c+1];i++)lc[c]+=Vector2.Distance(cfp[i],cfp[i-1]);}
            // (the long lap leaves at a narrow angle and its fillet reaches 45 m down the lap: both arms level for 50 m there, as the
            // lap fell 2 m under the junction's level patch, round 3)
            const float WideLanding=50;float Land(int c)=>c==2?WideLanding:20;
            float Climb(int c)=>.85f*(QuarryHills.LandingGrade*2*Land(c)+.08f*Mathf.Max(0,lc[c]-2*Land(c)));
            for(int it=0;it<12;it++){for(int c=0;c<3;c++)hj[c+1]=Mathf.Clamp(hj[c+1],hj[c]-Climb(c),hj[c]+Climb(c));for(int c=3;c>=1;c--){float nx=c==3?hj[0]:hj[c+1];hj[c]=Mathf.Clamp(hj[c],nx-Climb(c),nx+Climb(c));}}
            for(int c=0;c<4;c++) {
                var via=new List<Vector2>();for(int i=cuts[c]+1;i<cuts[c+1];i++)via.Add(W(cfp[i].x,cfp[i].y));
                var from=c==0?At(0):lapPts[^1];var to=c==3?lapPts[0]:At(cuts[c+1]);to.y=c==3?to.y:hj[c+1];
                var seg=QuarryHills.Profile(from,via.ToArray(),to,.08f,30,Land(c));for(int i=c==0?0:1;i<seg.Length;i++)lapPts.Add(seg[i]);
            }
            var lapLine=lapPts.ToArray();
            var lapL=new List<Vector3>(lapLine);lapL.RemoveAt(lapL.Count-1);Lap=lapL.ToArray();
            LapVale=VertexNear(Lap,new Vector2(-470,-20));LapA=VertexNear(Lap,new Vector2(-829,-30));LapB=VertexNear(Lap,new Vector2(-575,-301));
            var efp=Pieces(ExtSpec,-829,-30,265,CircuitMarks);var ew=new List<Vector2>();for(int i=1;i<efp.Count;i++)ew.Add(W(efp[i].x,efp[i].y));
            {var a=ew[^1];var b=new Vector2(Lap[LapB].x,Lap[LapB].z);int k=Mathf.CeilToInt(Vector2.Distance(a,b)/5);for(int i=1;i<k;i++)ew.Add(Vector2.Lerp(a,b,i/(float)k));}
            var extLine=QuarryHills.Profile(Lap[LapA],ew.ToArray(),Lap[LapB],.08f,25,WideLanding);
            // (both arms dead level for their first 40 m, then into their 1 % landings over 20 m: the junction's level patch reaches
            // 45 m down the narrow fork, and on the landing its edge stood 0.3 m proud of the arm, round 5)
            void Level(Vector3[] line,int from){float y0=line[from].y,run=0;for(int i=from+1;i<line.Length&&run<60;i++){run+=Vector2.Distance(new(line[i].x,line[i].z),new(line[i-1].x,line[i-1].z));line[i].y=Mathf.Lerp(y0,line[i].y,S(40,60,run));}}
            Level(lapLine,LapA);Level(Lap,LapA);Level(extLine,0);
            var ll=new List<Vector3>();for(int i=0;i<=LapA;i++)ll.Add(Lap[i]);for(int i=1;i<extLine.Length-1;i++)ll.Add(extLine[i]);for(int i=LapB;i<Lap.Length;i++)ll.Add(Lap[i]);LapLong=ll.ToArray();
            var circuit=HillcrestRoute.Make(CircuitName,lapLine,LoopWidth,5,25);var ext=HillcrestRoute.Make(ExtName,extLine,LoopWidth,5,25);
            JunctionPads.Add(Lap[LapA]);JunctionPads.Add(Lap[LapB]);JunctionPads.Add(Lap[LapVale]);JunctionPads.Add(Lap[0]);   // (and the start line, where the ground stood 18 cm over the road)
            // (and the wedge between the long lap's two arms as they part, level with them: lips and a steep bank 25-57 m out, round 4)
            for(int k=10;k<=24;k+=7)JunctionPads.Add((Lap[Mathf.Min(LapA+k,Lap.Length-1)]+extLine[Mathf.Min(k,extLine.Length-1)])*.5f);
            Debug.Log($"LITTLE_ROAD_VINELANDS_CIRCUIT lap={circuit.Length:0} m long={Length(LapLong):0} m fall={Lap.Max(p=>p.y)-Lap.Min(p=>p.y):0.0} m ext={ext.Length:0} m");
            var vline=QuarryHills.Profile(Largo,Wv((-160,20),(-300,50),(-400,10)),Lap[LapVale],.12f,20,30);   // (to the circuit's main straight, where the paddock is: zone 7)
            var vale=HillcrestRoute.Make(ValeRoadName,vline,6,5,25);
            list.AddRange(new[]{river,entry,rua,calcada,mirante,vale,feira,HillcrestRoute.Make(PonteName,pline,7f,5,20),estradaMeio,climb,amendoeiras,circuit,ext,HillcrestRoute.Make(CorvoName,CorvoLine,6f,5,20),HillcrestRoute.Make(OutraName,OutraLine,6f,5,20)});list.AddRange(tracks);
            Debug.Log($"LITTLE_ROAD_VINELANDS_ROADS entry={entry.Length:0} m river={river.Length:0} m rua direita={rua.Length:0} m calcada={calcada.Length:0} m mirante={mirante.Length:0} m feira={feira.Length:0} m vale={vale.Length:0} m largo y={Largo.y:0.0} saddle y={eline[EntrySaddle].y:0.0} cais y={Cais.y:0.0} belvedere y={Belvedere.y:0.0}");
            return list;
        }
    }
}

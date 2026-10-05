using UnityEngine;

namespace LittleRoad
{
    // Quarry Hills' ground colour (docs/59), baked once like the valley's macro tint (~3.1 m a texel): RGB multiplies the
    // toy grass, so each surface is written as a target colour divided by the mid grass; alpha is the distance to the
    // nearest field edge (/8 m) where there are walled fields, which the terrain shader draws as dry-stone walls.
    public static class QuarryHillsLook
    {
        public const float X=QuarryHills.West,Z=QuarryHills.South,W=QuarryHills.SeamX+60-QuarryHills.West,D=-380-QuarryHills.South;
        public static Vector4 MacroRect=>new(X,Z,W,D);
        static float Blend(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float Hash(int a,int b,int c){unchecked{uint h=(uint)(a*73856093)^(uint)(b*19349663)^(uint)(c*83492791);h^=h>>13;h*=0x5bd1e995;h^=h>>15;return (h&0xffffff)/16777216f;}}
        public static Texture2D MacroTint()
        {
            const int nx=1024;int nz=Mathf.RoundToInt(nx*D/W);
            var tex=new Texture2D(nx,nz,TextureFormat.RGBAHalf,true,true){name="Quarry Hills macro tint",wrapMode=TextureWrapMode.Clamp};var px=new Color[nx*nz];
            var mid=(WorldLook.ToyGrassLight+WorldLook.ToyGrassDeep)*.5f;
            Color C(float r,float g,float b)=>new(r/mid.r,g/mid.g,b/mid.b,1);
            Color slate=C(.36f,.39f,.42f),slateDark=C(.27f,.30f,.33f),floor=C(.46f,.45f,.42f),rubble=C(.41f,.43f,.46f),moor=C(.55f,.50f,.33f),heather=C(.45f,.35f,.42f),
                woods=C(.10f,.21f,.11f),pasture=C(.22f,.40f,.17f),bracken=C(.47f,.36f,.21f),coastTurf=C(.28f,.46f,.20f),garden=C(.17f,.33f,.13f),yardGrey=C(.40f,.41f,.42f);
            var square=QuarryHills.Square;
            // Rows in parallel (every input is a pure read of the built height field): ~1.2M texels.
            var clock=System.Diagnostics.Stopwatch.StartNew();
            System.Threading.Tasks.Parallel.For(0,nz,j=>{for(int i=0;i<nx;i++) {
                float x=X+(i+.5f)/nx*W,z=Z+(j+.5f)/nz*D;
                float h=ValleyField.Height(x,z),up=ValleyField.Normal(x,z).y,n=Mathf.PerlinNoise(x*.013f+4,z*.013f+8),fine=Mathf.PerlinNoise(x*.06f,z*.06f);
                var c=Color.white;float alpha=1;
                // Small walled fields on the gentle ground right up to the backs of the town's terraces and down to the coast;
                // in town, the ground between the rows is gardens and slate yards.
                float sq=Vector2.Distance(new(x,z),square),rd=ValleyField.RoadDistance(x,z);bool town=sq<720;
                // (Not on the tips, the quarry floor or the pit's banks.)
                QuarryHills.PitMetric(x,z,out float pq,out _,out _);float onTip=Blend(-4,-.5f,QuarryHills.TipSurface(x,z)-h);
                float fields=Blend(.86f,.94f,up)*(town?Blend(52,64,rd):Blend(7,12,rd))*(1-Blend(1100,1350,sq))*Blend(-6,2,h)*(1-Blend(80,100,h))
                    *(1-onTip)*Blend(1.25f,1.45f,QuarryHills.FloorQ(x,z))*Blend(1.2f,1.4f,pq);
                if(town){float yards=(1-Blend(52,62,rd))*Blend(5,9,rd);c=Color.Lerp(c,Color.Lerp(garden,yardGrey,Blend(.5f,.68f,fine)),yards*.85f);}
                if(fields>0) {
                    float ang=.35f+(Hash(Mathf.FloorToInt(x/400),Mathf.FloorToInt(z/400),1)-.5f)*.5f,ca=Mathf.Cos(ang),sa=Mathf.Sin(ang);
                    float u=x*ca+z*sa,v=-x*sa+z*ca;float rowH=38+Hash(Mathf.FloorToInt(u/400),0,4)*30;int row=Mathf.FloorToInt(v/rowH);
                    float colW=44+Hash(row,7,5)*50,shift=Hash(row,3,6)*colW;int col=Mathf.FloorToInt((u+shift)/colW);
                    float fu=(u+shift)/colW-col,fv=v/rowH-row;float edge=Mathf.Min(Mathf.Min(fu,1-fu)*colW,Mathf.Min(fv,1-fv)*rowH);
                    float roll=Hash(col,row,9);var crop=roll<.5f?pasture:roll<.75f?C(.27f,.45f,.19f):roll<.92f?C(.34f,.49f,.23f):C(.42f,.47f,.26f);
                    // (Street review, 5 Oct: the shader's painted field edges broke into pale see-through blobs across the fields; the
                    // fields' walls are real ones along the roads.) The crops keep their patchwork; no painted edges.
                    c=Color.Lerp(c,crop,fields);
                }
                // Coastal turf near the sea.
                float dc=QuarryHills.CoastDistance(x,z);c=Color.Lerp(c,coastTurf,(1-Blend(60,260,dc))*Blend(-2,3,h)*.6f);
                // Moor on the high ground: tawny grass with heather patches and bracken on the slopes.
                float high=Blend(110,170,h);
                if(high>0){var m=Color.Lerp(moor,heather,Blend(.55f,.68f,n));m=Color.Lerp(m,bracken,Blend(.9f,.8f,up)*.6f);c=Color.Lerp(c,m,high*.85f);}
                // Wet woods: the west and south-west hills and the steep gullies.
                float woodsNoise=Mathf.PerlinNoise(x*.007f+31,z*.007f+57);
                float wood=Blend(.44f,.56f,woodsNoise)*Blend(25,45,h)*(1-Blend(150,185,h))*(1-Blend(-2450,-2150,x)*Blend(-3150,-2850,z));
                wood=Mathf.Max(wood,Blend(-3600,-3900,x)*Blend(30,60,h)*(1-Blend(170,200,h)));
                c=Color.Lerp(c,woods,wood*.9f*(1-fields));
                // The quarry: tips, floor, gallery shelves and the pit rim are slate.
                c=Color.Lerp(c,Color.Lerp(slate,slateDark,fine),Blend(-2.5f,.2f,QuarryHills.TipSurface(x,z)-h));
                float qf=Mathf.Sqrt(Mathf.Pow((x-QuarryHills.FloorCentre.x)/QuarryHills.FloorRX,2)+Mathf.Pow((z-QuarryHills.FloorCentre.y)/QuarryHills.FloorRZ,2));
                c=Color.Lerp(c,Color.Lerp(floor,rubble,fine*.6f),1-Blend(1.05f,1.3f,qf));
                QuarryHills.PitMetric(x,z,out float q,out float e,out float mask);
                float gallery=mask*Blend(-6,0,e)*(1-Blend(QuarryHills.Benches*QuarryHills.BenchWidth,QuarryHills.Benches*QuarryHills.BenchWidth+12,e));
                c=Color.Lerp(c,Color.Lerp(rubble,slate,fine),Mathf.Max(gallery,1-Blend(1.0f,1.12f,q)));
                c*=Mathf.Lerp(.92f,1.06f,Mathf.PerlinNoise(x*.035f+1,z*.035f+2));
                // Fade to plain at the seam so the valley's own tint takes over without a line.
                float seam=1-Blend(QuarryHills.SeamX-140,QuarryHills.SeamX,x);c=Color.Lerp(Color.white,c,seam);c.a=Mathf.Lerp(1,alpha,seam);
                px[j*nx+i]=c;
            }});
            Debug.Log($"LITTLE_ROAD_QUARRY_TINT ms={clock.ElapsedMilliseconds}");
            tex.SetPixels(px);tex.Apply(true);return tex;
        }
    }
}

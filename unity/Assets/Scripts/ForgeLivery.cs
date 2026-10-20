using UnityEngine;

namespace LittleRoad
{
    // Car Forge livery space (car-forge: forge/paint.py, docs/00 §9): every Forge body's body-colour faces carry a
    // `Livery` UV map into one shared atlas, laid out from a nominal car (4.3 m long, 2.4 m wide, 2 m tall) at one
    // scale: a column of the right side, the left side and the top, and beside it the front over the rear. Along a side
    // or the top, u runs through the car's landmarks (tail 0, rear axle .2, front axle .8, nose 1), so a door roundel
    // between the axles sits at .5 on any car; heights and widths are metres at the atlas's scale.
    // The livery (on in the game since 20 Oct; --no-livery): Forge bodies get a composed texture over their paint, in the style their body file names
    // (Car Forge's paint.livery.style, 17 Oct): stripes (twin racing stripes over the top), centre (one broad stripe),
    // side (a stripe along each flank), chequer (a chequered band along each flank), roundel (the number alone) or plain
    // (the paint as it is); each but plain with a number roundel on each door. No shader change: the paint is baked into
    // the texture and the material's colour set to white (URP Lit multiplies the two).
    public static class ForgeLivery
    {
        // (the nominal car and the gap between the columns: forge/paint.py _L, _W, _H, _G; K metres to atlas units)
        public const float L=4.3f,W=2.4f,H=2f,G=.05f,K=1f/(L+W+G);
        public static readonly Rect Right=Rect.MinMaxRect(0,0,L*K,H*K),Left=Rect.MinMaxRect(0,H*K,L*K,2*H*K),
            Top=Rect.MinMaxRect(0,2*H*K,L*K,(2*H+W)*K),Front=Rect.MinMaxRect((L+G)*K,0,1,H*K),Rear=Rect.MinMaxRect((L+G)*K,H*K,1,2*H*K);
        public const int Size=512;
        public static bool Enabled=>Application.isEditor?RaceTrial.Has("--livery"):!RaceTrial.Has("--no-livery");   // (on in the game since 20 Oct, Richard: "assume they pass"; the editor's checks keep it off unless asked)
        // (the roundel: centred between the axles, 0.55 m above the sill, 0.3 m across; the stripes 6-21 cm either side
        //  of the centre line)
        public const float RoundelS=.5f,RoundelUp=.55f,RoundelR=.3f,StripeIn=.06f,StripeOut=.21f;
        // (centre: a stripe 36 cm wide; side: a stripe 30-40 cm above the sill, tail to nose; chequer: 6 cm checks 14-26 cm up)
        public const float CentreHalf=.18f,SideLo=.30f,SideHi=.40f,CheckLo=.14f,CheckHi=.26f,Check=.06f;
        public static readonly string[] Styles={"stripes","centre","side","chequer","roundel","plain"};

        // A stable racing number for a body (1-99).
        public static int Number(string id){unchecked{int h=17;foreach(char c in id)h=h*31+c;return 1+Mathf.Abs(h%99);}}

        // Pixel centre of a point in metres on a side row: s along the car (0..1), up metres above the sill.
        public static Vector2 SidePixel(Rect row,float s,float up)=>new((row.xMin+row.width*s)*Size,(row.yMin+up*K)*Size);

        public static Texture2D Compose(Color paint,Color stripe,int number,string style="stripes")
        {
            var px=new Color32[Size*Size];Color32 p=paint,st=stripe,ink=new Color(.06f,.06f,.07f);
            for(int i=0;i<px.Length;i++)px[i]=p;
            // bands along the whole top row (bonnet, roof, boot): across b = .5 + .5 x / (W/2)
            float b0=Top.yMin*Size,bh=Top.height*Size;
            void TopBand(float xa,float xb){float lo=.5f+.5f*xa/(W/2),hi=.5f+.5f*xb/(W/2);if(lo>hi)(lo,hi)=(hi,lo);
                for(int y=Mathf.FloorToInt(b0+lo*bh);y<Mathf.CeilToInt(b0+hi*bh);y++)for(int x=0;x<Mathf.CeilToInt(Top.xMax*Size);x++)px[y*Size+x]=st;}
            // bands along each side, tail to nose, between heights above the sill (metres); chequered in checks of `check`
            void SideBand(float lo,float hi,float check){foreach(var row in new[]{Right,Left})
                for(int y=Mathf.FloorToInt((row.yMin+lo*K)*Size);y<Mathf.CeilToInt((row.yMin+hi*K)*Size);y++)
                    for(int x=Mathf.FloorToInt(row.xMin*Size);x<Mathf.CeilToInt(row.xMax*Size);x++){
                        if(check>0&&((Mathf.FloorToInt((x-row.xMin*Size)/(check*K*Size))+Mathf.FloorToInt((y-(row.yMin+lo*K)*Size)/(check*K*Size)))&1)==1)continue;
                        px[y*Size+x]=st;}}
            if(style=="stripes")foreach(float sign in new[]{-1f,1f})TopBand(sign*StripeIn,sign*StripeOut);
            else if(style=="centre")TopBand(-CentreHalf,CentreHalf);
            else if(style=="side")SideBand(SideLo,SideHi,0);
            else if(style=="chequer")SideBand(CheckLo,CheckHi,Check);
            // a roundel on each side, its number in the middle
            foreach(var row in new[]{Right,Left}) {
                var c=SidePixel(row,RoundelS,RoundelUp);float r=RoundelR*K*Size;
                for(int y=Mathf.FloorToInt(c.y-r);y<=Mathf.CeilToInt(c.y+r);y++)for(int x=Mathf.FloorToInt(c.x-r);x<=Mathf.CeilToInt(c.x+r);x++)
                    if(x>=0&&y>=0&&x<Size&&y<Size&&(new Vector2(x,y)-c).sqrMagnitude<=r*r)px[y*Size+x]=st;
                Digits(px,number.ToString(),c,r*1.1f,ink);
            }
            var tex=new Texture2D(Size,Size,TextureFormat.RGBA32,true){name="ForgeLivery",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
            tex.SetPixels32(px);tex.Apply(true);return tex;
        }

        // A 5x7 bitmap font for the numbers, centred on c, `height` pixels tall.
        static readonly string[] Glyphs={"01110100011001110101110011000101110","00100011000010000100001000010001110","01110100010000100010001000100011111",
            "11110000010000101110000010000111110","00010001100101010010111110001000010","11111100001111000001000011000101110","00110010001000011110100011000101110",
            "11111000010001000100010000100001000","01110100011000101110100011000101110","01110100011000101111000010001001100"};
        static void Digits(Color32[] px,string text,Vector2 c,float height,Color32 ink)
        {
            float cell=height/7f,gap=cell;float w=text.Length*5*cell+(text.Length-1)*gap;float x0=c.x-w/2,y0=c.y-height/2;
            for(int k=0;k<text.Length;k++){string g=Glyphs[text[k]-'0'];
                for(int row=0;row<7;row++)for(int col=0;col<5;col++){if(g[row*5+col]!='1')continue;
                    float gx=x0+k*(5*cell+gap)+col*cell,gy=y0+(6-row)*cell;
                    for(int y=Mathf.FloorToInt(gy);y<Mathf.CeilToInt(gy+cell);y++)for(int x=Mathf.FloorToInt(gx);x<Mathf.CeilToInt(gx+cell);x++)
                        if(x>=0&&y>=0&&x<Size&&y<Size)px[y*Size+x]=ink;}}
        }

        // Puts a composed livery on every body-paint material of a car (not its second colour). Returns how many.
        // (plain: the paint as it is, no texture)
        public static int Apply(GameObject car,Color paint,Color stripe,int number,string style="stripes")
        {
            if(style=="plain")return 0;
            Texture2D tex=null;int n=0;var done=new System.Collections.Generic.HashSet<Material>();
            foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials){
                if(m==null||!m.name.Contains("BodyPaint")||!done.Add(m))continue;
                tex??=Compose(paint,stripe,number,style);m.SetTexture("_BaseMap",tex);m.color=Color.white;n++;}
            return n;
        }
    }
}

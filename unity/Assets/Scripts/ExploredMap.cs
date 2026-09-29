using System;
using UnityEngine;

namespace LittleRoad
{
    // Fog of war for the maps (docs/ui-design.md, "Fog of war"): one bit per ~10 m cell over the baked world
    // map, saved with the adventure. Driving reveals a circle the size of the minimap's standstill view
    // (Minimap.NearRadius), so the corner map is never blotchy when you stop but shows fog ahead at speed;
    // viewpoints reveal the country you can see from them, and arriving in a town reveals the whole town.
    // The map shaders read the mask as the global _ExploredFog.
    public static class ExploredMap
    {
        public const int Cells=256;
        public const float DriveRadius=Minimap.NearRadius+25,ViewpointRadius=520,CastleRadius=620;
        static byte[] bits,pixels;static Texture2D texture;static bool textureDirty;
        public static bool Dirty{get;private set;}
        public static bool Loaded=>bits!=null;
        public static Texture2D Texture=>texture;
        static float CellSize=>WorldMap.Size/Cells;

        public static void Load(string saved)
        {
            bits=new byte[Cells*Cells/8];
            // v2 masks only: earlier ones began with Bayview revealed for free, which is no longer the rule.
            if(!string.IsNullOrEmpty(saved)&&saved.StartsWith(Version)){try{var b=Convert.FromBase64String(saved.Substring(Version.Length));if(b.Length==bits.Length)bits=b;}catch(FormatException){}}
            pixels=new byte[Cells*Cells];for(int i=0;i<pixels.Length;i++)pixels[i]=(byte)(Get(i)?255:0);
            if(texture==null)texture=new Texture2D(Cells,Cells,TextureFormat.R8,false,true){name="Explored map",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            textureDirty=true;Flush();Dirty=false;
            Shader.SetGlobalTexture("_ExploredFog",texture);Shader.SetGlobalFloat("_ExploredFogOn",1);
        }
        const string Version="v2:";
        public static string Save(){Dirty=false;return Version+Convert.ToBase64String(bits);}
        static bool Get(int i)=>(bits[i>>3]&(1<<(i&7)))!=0;
        static bool Set(int i){if(Get(i))return false;bits[i>>3]|=(byte)(1<<(i&7));pixels[i]=255;return true;}
        static (int x,int y) Cell(Vector3 p){var uv=WorldMap.Uv(p);return ((int)(uv.x*Cells),(int)(uv.y*Cells));}
        // Outside the baked area (open sea) counts as seen.
        public static bool Seen(Vector3 p)
        {
            if(bits==null)return true;var (x,y)=Cell(p);
            if(x<0||y<0||x>=Cells||y>=Cells)return true;return Get(y*Cells+x);
        }
        public static int Reveal(Vector3 centre,float radius)
        {
            if(bits==null)return 0;var (cx,cy)=Cell(centre);int r=Mathf.CeilToInt(radius/CellSize),n=0;float r2=(radius/CellSize)*(radius/CellSize);
            for(int y=Mathf.Max(0,cy-r);y<=Mathf.Min(Cells-1,cy+r);y++)
                for(int x=Mathf.Max(0,cx-r);x<=Mathf.Min(Cells-1,cx+r);x++)
                    if((x-cx)*(x-cx)+(y-cy)*(y-cy)<=r2&&Set(y*Cells+x))n++;
            if(n>0){Dirty=true;textureDirty=true;}return n;
        }
        public static int RevealRegion(Func<float,float,bool> contains)
        {
            if(bits==null)return 0;int n=0;
            for(int y=0;y<Cells;y++)for(int x=0;x<Cells;x++){
                float wx=WorldMap.X0+(x+.5f)*CellSize,wz=WorldMap.Z0+(y+.5f)*CellSize;
                if(contains(wx,wz)&&Set(y*Cells+x))n++;
            }
            if(n>0){Dirty=true;textureDirty=true;}return n;
        }
        public static float Fraction(Func<float,float,bool> within=null)
        {
            if(bits==null)return 1;int seen=0,total=0;
            for(int y=0;y<Cells;y++)for(int x=0;x<Cells;x++){
                if(within!=null&&!within(WorldMap.X0+(x+.5f)*CellSize,WorldMap.Z0+(y+.5f)*CellSize))continue;
                total++;if(Get(y*Cells+x))seen++;
            }
            return total==0?1:(float)seen/total;
        }
        public static void Flush()
        {
            if(!textureDirty||texture==null)return;textureDirty=false;
            texture.SetPixelData(pixels,0);texture.Apply(false);
        }
        // Tests and reviews: forget everything (no save involved).
        public static void Reset(){bits=null;pixels=null;Shader.SetGlobalFloat("_ExploredFogOn",0);}
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Trampled crops (bug inbox #20, Richard 20 Oct: "fields don't hold up to close scrutiny with the car clipping through
    // them"). The valley's crop fields are ridged rows (HillcrestTown's fields, Bramblewick's); each area of them has one
    // trample map, a value per metre, and the Crops shader lowers every ridge by its own height times the map, so where the
    // player's car drives through a field the crop lies down in its tracks and stays down. The car's four wheels press the
    // map each frame it moves over a field (the car's whole width, not just its wheels); the map uploads at most ten times a second.
    public sealed class CropTrample:MonoBehaviour
    {
        sealed class Area{public string name;public Rect rect;public float texel;public int w,h;public Texture2D tex;public byte[] px;public bool dirty;public Material material;}
        static readonly List<Area> areas=new();static CropTrample runner;
        public static int Pressed;   // (cells pressed so far: tests and the area audit)
        // The material for an area's fields: the Crops shader with the area's trample map (made once per name).
        public static Material Material(string name,Rect rect,float texel=1f)
        {
            foreach(var a in areas)if(a.name==name)return a.material;
            int w=Mathf.CeilToInt(rect.width/texel),h=Mathf.CeilToInt(rect.height/texel);
            var area=new Area{name=name,rect=rect,texel=texel,w=w,h=h,px=new byte[w*h]};
            area.tex=new Texture2D(w,h,TextureFormat.R8,false,true){name=name+" trample",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            area.tex.SetPixelData(area.px,0);area.tex.Apply(false,false);
            var shader=Shader.Find("LittleRoad/Crops");
            area.material=new Material(shader!=null?shader:Shader.Find("LittleRoad/Distant")){name=name+" crops"};
            area.material.SetFloat("_Haze",.00008f);area.material.SetColor("_HazeColor",new Color(.66f,.80f,.90f));
            area.material.SetTexture("_Trample",area.tex);area.material.SetVector("_TrampleRect",new Vector4(rect.xMin,rect.yMin,1/rect.width,1/rect.height));
            areas.Add(area);
            if(runner==null){var go=new GameObject("Crop trample");Object.DontDestroyOnLoad(go);runner=go.AddComponent<CropTrample>();}
            return area.material;
        }
        public static void Clear(){areas.Clear();Pressed=0;}
        float upload;
        void Update()
        {
            var car=WheatField.Player;if(car==null||areas.Count==0)return;
            var t=car.transform;var v=car.body!=null?car.body.velocity:Vector3.zero;
            if(v.sqrMagnitude>.25f) {
                var p=t.position;
                foreach(var a in areas) {
                    if(!a.rect.Contains(new Vector2(p.x,p.z)))continue;
                    // (the whole car's width: its bumper and floor flatten what its wheels don't)
                    foreach(var w in new[]{new Vector3(-.5f,0,1.6f),new Vector3(.5f,0,1.6f),new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(-.5f,0,-1.6f),new Vector3(.5f,0,-1.6f)})Press(a,t.TransformPoint(w),1.05f);
                }
            }
            upload-=Time.deltaTime;if(upload>0)return;upload=.1f;
            foreach(var a in areas)if(a.dirty){a.tex.SetPixelData(a.px,0);a.tex.Apply(false,false);a.dirty=false;}
        }
        static void Press(Area a,Vector3 at,float radius)
        {
            float fx=(at.x-a.rect.xMin)/a.texel,fz=(at.z-a.rect.yMin)/a.texel,r=radius/a.texel;
            for(int j=Mathf.FloorToInt(fz-r);j<=Mathf.CeilToInt(fz+r);j++)for(int i=Mathf.FloorToInt(fx-r);i<=Mathf.CeilToInt(fx+r);i++) {
                if(i<0||j<0||i>=a.w||j>=a.h)continue;float d=Mathf.Sqrt((i+.5f-fx)*(i+.5f-fx)+(j+.5f-fz)*(j+.5f-fz));if(d>r+.5f)continue;
                byte v=(byte)Mathf.Clamp(255*(1.3f-d/r),0,255);int k=j*a.w+i;if(v>a.px[k]){if(a.px[k]==0)Pressed++;a.px[k]=v;a.dirty=true;}
            }
        }
    }
}

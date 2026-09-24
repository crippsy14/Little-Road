using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LittleRoad
{
    // Shared "toy" menu kit: bright Road Trip Adventure style panels, stickered
    // titles, candy buttons and small unscaled-time animations (menus often
    // pause the race clock). Reference layout: review/ui-screens/mockup-v1.html.
    public static class Toy
    {
        public static readonly Color Sky=Hex(0x5ec8ff),SkyLight=Hex(0x8fdcff),Sun=Hex(0xffd23f),Gold=Hex(0xffb400),Red=Hex(0xff4d5e),
            Navy=Hex(0x1d2b64),Mint=Hex(0x3ee6a4),Cream=Hex(0xfff8e6),Pink=Hex(0xff7ac8),Orange=Hex(0xff8a3d),OrangeLight=Hex(0xffb366),
            Blue=Hex(0x4a7bff),White=Color.white,Silver=Hex(0xc0c7d8),Bronze=Hex(0xe0935a),Slower=Hex(0xc9f7d8),Faster=Hex(0xffd0d4),Soft=Hex(0x5a6aa8);
        public static readonly Color[] Party={Red,Sun,Mint,Blue,Pink,Orange};
        public static Color Hex(int rgb)=>new(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);
        static Font display,body,heavy;
        public static Font Display=>display!=null?display:display=Load("Fonts/LilitaOne-Regular");
        public static Font Body=>body!=null?body:body=Load("Fonts/Nunito-ExtraBold");
        public static Font Heavy=>heavy!=null?heavy:heavy=Load("Fonts/Nunito-Black");
        static Font Load(string path)=>Resources.Load<Font>(path)??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=parent.gameObject.layer;
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        // Chunky card: navy outline and a hard navy drop shadow, like a sticker.
        public static ToyShape Card(Transform parent,string name,Vector2 size,Vector2 position,Color fill,float radius=30,float border=6,float shadow=9)
        {
            var s=Rect(parent,name,size,position).gameObject.AddComponent<ToyShape>();
            s.color=fill;s.radius=radius;s.border=border;s.shadow=new Vector2(shadow,-shadow);s.raycastTarget=false;return s;
        }
        public static Text Label(Transform parent,string name,string value,int size,Vector2 box,Vector2 position,Color color,TextAnchor anchor=TextAnchor.MiddleLeft,bool black=false)
        {
            var t=Rect(parent,name,box,position).gameObject.AddComponent<Text>();t.font=black?Heavy:Body;t.fontSize=size;t.text=value;t.color=color;
            t.alignment=anchor;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.supportRichText=true;return t;
        }
        // Stickered title: display face, thick navy outline and offset shadow.
        public static Text Title(Transform parent,string name,string value,int size,Vector2 box,Vector2 position,Color fill,TextAnchor anchor=TextAnchor.MiddleLeft,float tilt=0)
        {
            var t=Rect(parent,name,box,position).gameObject.AddComponent<Text>();t.font=Display;t.fontSize=size;t.text=value;t.color=fill;t.alignment=anchor;
            t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;
            float o=Mathf.Max(2,size*.055f);
            var a=t.gameObject.AddComponent<Outline>();a.effectColor=Navy;a.effectDistance=new Vector2(o,o);
            var b=t.gameObject.AddComponent<Outline>();b.effectColor=Navy;b.effectDistance=new Vector2(o,-o)*.7f;
            var c=t.gameObject.AddComponent<Shadow>();c.effectColor=Navy;c.effectDistance=new Vector2(o*1.4f,-o*1.4f);
            t.rectTransform.localRotation=Quaternion.Euler(0,0,tilt);return t;
        }
        public static ToyShape Pill(Transform parent,string name,string value,int size,Vector2 box,Vector2 position,Color fill,Color text,float tilt=0)
        {
            var p=Card(parent,name,box,position,fill,box.y*.5f,4,4);
            Label(p.transform,"Pill text",value,size,box-new Vector2(18,0),Vector2.zero,text,TextAnchor.MiddleCenter,true);
            p.rectTransform.localRotation=Quaternion.Euler(0,0,tilt);return p;
        }
        // Big rounded candy button with a controller glyph. Keeps the CoastMenu
        // naming convention ("Choice ...") so existing review fixtures still work.
        public static Button Candy(Transform parent,string name,string caption,string glyph,Vector2 size,Vector2 position,Color fill,Action clicked,bool pulse=false)
        {
            var shape=Card(parent,name,size,position,fill,size.y*.5f,6,0);shape.raycastTarget=true;
            var button=shape.gameObject.AddComponent<Button>();button.targetGraphic=shape;button.transition=Selectable.Transition.None;
            var face=Rect(shape.transform,"Face",size,Vector2.zero);
            float glyphSize=size.y*.52f;
            var text=Title(face,"Caption",caption,Mathf.RoundToInt(size.y*.42f),new Vector2(size.x-glyphSize-40,size.y),new Vector2(glyphSize*.5f+8,2),White,TextAnchor.MiddleCenter);
            if(!string.IsNullOrEmpty(glyph)){
                var g=Card(face,"Glyph",new Vector2(glyphSize,glyphSize),new Vector2(-size.x*.5f+glyphSize*.5f+22,0),White,glyphSize*.5f,4,0);
                if(ToyPadGlyph.TryParse(glyph,out var symbol)){var mark=Rect(g.transform,"Glyph "+symbol,new Vector2(glyphSize,glyphSize)*.56f,Vector2.zero).gameObject.AddComponent<ToyPadGlyph>();mark.symbol=symbol;mark.raycastTarget=false;}
                else Label(g.transform,"Glyph text",glyph,Mathf.RoundToInt(glyphSize*.55f),new Vector2(glyphSize,glyphSize),Vector2.zero,Navy,TextAnchor.MiddleCenter,true);
            }
            var visual=shape.gameObject.AddComponent<ToyButton>();visual.Setup(button,shape,face,fill,pulse);
            button.onClick.AddListener(()=>clicked?.Invoke());return button;
        }
        public static ToySunburst Sunburst(Transform parent,Vector2 size,Vector2 position,Color a,Color b,int rays=30,float spin=4)
        {
            var s=Rect(parent,"Sunburst",size,position).gameObject.AddComponent<ToySunburst>();s.color=a;s.second=b;s.rays=rays;s.raycastTarget=false;
            if(spin!=0)s.gameObject.AddComponent<ToySpin>().degreesPerSecond=spin;return s;
        }
        public static void Bunting(Transform parent,float width,float y,int count)
        {
            var row=Rect(parent,"Bunting",new Vector2(width,70),new Vector2(0,y));
            for(int i=0;i<count;i++){
                var flag=Rect(row,"Flag",new Vector2(56,72),new Vector2(-width*.5f+(i+.5f)*width/count,0)).gameObject.AddComponent<ToyTriangle>();flag.color=Party[i%Party.Length];flag.raycastTarget=false;
                flag.rectTransform.pivot=new Vector2(.5f,1);flag.rectTransform.anchoredPosition+=new Vector2(0,36);
                var sway=flag.gameObject.AddComponent<ToySway>();sway.phase=i*.4f;
            }
        }
        public static void Checker(Transform parent,float width,float y,float cell=30)
        {
            var c=Rect(parent,"Chequered strip",new Vector2(width,cell),new Vector2(0,y)).gameObject.AddComponent<ToyChecker>();c.color=Navy;c.cell=cell;c.raycastTarget=false;
        }
        public static ToyCarIcon Car(Transform parent,string name,Vector2 size,Vector2 position,Color paint)
        {
            var c=Rect(parent,name,size,position).gameObject.AddComponent<ToyCarIcon>();c.color=paint;c.raycastTarget=false;return c;
        }
        // Menus render on their own overlay camera, stacked on the game camera with
        // post-processing off: the world's ACES tonemapping otherwise greys the
        // palette, and the menu always draws above the in-race HUD.
        public const int MenuLayer=5; // Unity's built-in "UI" layer
        static Camera menuCamera;static Camera menuBase;
        public static Camera MenuCamera()
        {
            var main=Camera.main;if(main==null)return null;
            if(menuCamera!=null&&menuBase==main)return menuCamera;
            if(menuCamera!=null)UnityEngine.Object.Destroy(menuCamera.gameObject);
            var go=new GameObject("Menu overlay camera");go.transform.SetParent(main.transform,false);
            menuCamera=go.AddComponent<Camera>();menuCamera.clearFlags=CameraClearFlags.Depth;menuCamera.cullingMask=1<<MenuLayer;menuCamera.nearClipPlane=.05f;menuCamera.farClipPlane=5;
            menuCamera.fieldOfView=main.fieldOfView;
            var data=menuCamera.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Overlay;data.renderPostProcessing=false;
            main.GetUniversalAdditionalCameraData().cameraStack.Add(menuCamera);main.cullingMask&=~(1<<MenuLayer);menuBase=main;return menuCamera;
        }
        public static void UseMenuCamera(Canvas canvas)
        {
            var camera=MenuCamera();if(camera==null)return;
            foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=MenuLayer;
            canvas.worldCamera=camera;
        }
        // Screenshot of the world plus menus. Camera.Render() into a texture skips the
        // overlay stack, so render the menu camera separately on top (as a base
        // camera that keeps the world image) and restore it afterwards.
        public static void Capture(string path,int width=1920,int height=1080)
        {
            var main=Camera.main;var menu=MenuCamera();Canvas.ForceUpdateCanvases();
            var rt=new RenderTexture(width,height,24);var active=RenderTexture.active;var oldMain=main.targetTexture;
            var stack=main.GetUniversalAdditionalCameraData().cameraStack;bool stacked=menu!=null&&stack.Remove(menu);
            main.targetTexture=rt;main.Render();main.targetTexture=oldMain;
            RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new UnityEngine.Rect(0,0,width,height),0,0);
            if(menu!=null){
                // Menus into their own transparent texture, then blended over the world image.
                var ui=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
                var data=menu.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Base;var flags=menu.clearFlags;var background=menu.backgroundColor;
                menu.clearFlags=CameraClearFlags.SolidColor;menu.backgroundColor=new Color(0,0,0,0);menu.targetTexture=ui;menu.Render();menu.targetTexture=null;
                menu.clearFlags=flags;menu.backgroundColor=background;data.renderType=CameraRenderType.Overlay;if(stacked)stack.Add(menu);
                RenderTexture.active=ui;var overlay=new Texture2D(width,height,TextureFormat.RGBA32,false);overlay.ReadPixels(new UnityEngine.Rect(0,0,width,height),0,0);
                var world=image.GetPixels32();var top=overlay.GetPixels32();
                for(int i=0;i<world.Length;i++){var t=top[i];if(t.a==0)continue;float a=t.a/255f;world[i]=new Color32((byte)Mathf.Lerp(world[i].r,t.r,a),(byte)Mathf.Lerp(world[i].g,t.g,a),(byte)Mathf.Lerp(world[i].b,t.b,a),255);}
                image.SetPixels32(world);ui.Release();UnityEngine.Object.Destroy(ui);UnityEngine.Object.Destroy(overlay);
            }
            image.Apply();
            System.IO.File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
        }
        public static void RefreshButtons(Transform root){foreach(var b in root.GetComponentsInChildren<ToyButton>())b.Refresh(true);}
        public static string Ordinal(int n)=>n+((n%100) is 11 or 12 or 13?"th":(n%10) switch{1=>"st",2=>"nd",3=>"rd",_=>"th"});
    }

    static class ToyMesh
    {
        public static void Rounded(VertexHelper vh,Rect r,float radius,Color c)
        {
            radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);int start=vh.currentVertCount;vh.AddVert(r.center,c,Vector2.zero);int n=0;
            for(int corner=0;corner<4;corner++){
                var centre=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(int j=0;j<=8;j++){float a=(corner*90+j*90f/8)*Mathf.Deg2Rad;vh.AddVert(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);n++;}
            }
            for(int i=1;i<=n;i++)vh.AddTriangle(start,start+i,start+(i==n?1:i+1));
        }
        public static void Circle(VertexHelper vh,Vector2 centre,float radius,Color c,int segments=28)
        {
            int start=vh.currentVertCount;vh.AddVert(centre,c,Vector2.zero);
            for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;vh.AddVert(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);}
            for(int i=1;i<=segments;i++)vh.AddTriangle(start,start+i,start+(i==segments?1:i+1));
        }
        public static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color col)
        {
            int s=vh.currentVertCount;vh.AddVert(a,col,Vector2.zero);vh.AddVert(b,col,Vector2.zero);vh.AddVert(c,col,Vector2.zero);vh.AddVert(d,col,Vector2.zero);
            vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);
        }
    }
    public sealed class ToyShape:MaskableGraphic
    {
        public float radius=24,border=6;public Vector2 shadow=new(8,-8);public Color borderColor=Toy.Navy,shadowColor=Toy.Navy;
        public void SetFill(Color c){if(color!=c)color=c;}
        public void SetBorder(Color c){if(borderColor!=c){borderColor=c;SetVerticesDirty();}}
        public void SetShadow(Vector2 offset){if(shadow!=offset){shadow=offset;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(shadow!=Vector2.zero)ToyMesh.Rounded(vh,new Rect(r.position+shadow,r.size),radius,shadowColor);
            if(border>0)ToyMesh.Rounded(vh,r,radius,borderColor);
            ToyMesh.Rounded(vh,new Rect(r.xMin+border,r.yMin+border,r.width-border*2,r.height-border*2),Mathf.Max(0,radius-border),color);
        }
    }
    public sealed class ToySunburst:MaskableGraphic
    {
        public Color second=Toy.SkyLight;public int rays=30;public bool circular;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // Circular bursts stay inside their rect (badges); others cover it while spinning.
            vh.Clear();var r=rectTransform.rect;float reach=circular?Mathf.Min(r.width,r.height)*.5f:r.size.magnitude;
            for(int i=0;i<rays*2;i++){
                float a0=i*Mathf.PI/rays,a1=(i+1)*Mathf.PI/rays;var col=i%2==0?color:second;int s=vh.currentVertCount;
                vh.AddVert(Vector2.zero,col,Vector2.zero);vh.AddVert(new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*reach,col,Vector2.zero);vh.AddVert(new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*reach,col,Vector2.zero);
                vh.AddTriangle(s,s+1,s+2);
            }
        }
    }
    // Controller face-button symbol drawn as geometry (the fonts have no PlayStation shapes).
    // Candy glyph strings: "X" cross, "O" circle, "[]" square, "^" triangle.
    public sealed class ToyPadGlyph:MaskableGraphic
    {
        public enum Symbol{Cross,Circle,Square,Triangle}
        public Symbol symbol;
        static readonly Color CrossBlue=new(.33f,.55f,.95f),CircleRed=new(.93f,.27f,.33f),SquarePink=new(.87f,.36f,.72f),TriangleGreen=new(.1f,.72f,.6f);
        public static bool TryParse(string glyph,out Symbol s)
        {
            s=glyph switch{"X"=>Symbol.Cross,"O"=>Symbol.Circle,"[]"=>Symbol.Square,"^"=>Symbol.Triangle,_=>(Symbol)(-1)};return (int)s>=0;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float h=Mathf.Min(r.width,r.height)*.5f,t=h*.3f;var c=r.center;
            switch(symbol){
                case Symbol.Cross:Bar(vh,c+new Vector2(-h,-h),c+new Vector2(h,h),t,CrossBlue);Bar(vh,c+new Vector2(-h,h),c+new Vector2(h,-h),t,CrossBlue);break;
                case Symbol.Circle:Outline(vh,c,h-t*.5f,32,90,t,CircleRed);break;
                case Symbol.Square:Outline(vh,c,(h-t*.5f)*1.41421f,4,45,t,SquarePink);break;
                case Symbol.Triangle:Outline(vh,c+new Vector2(0,-h*.12f),(h-t*.3f)*1.12f,3,90,t,TriangleGreen);break;
            }
        }
        static void Bar(VertexHelper vh,Vector2 a,Vector2 b,float width,Color col)
        {var side=Vector2.Perpendicular((b-a).normalized)*width*.5f;ToyMesh.Quad(vh,a-side,a+side,b+side,b-side,col);}
        // Regular polygon outline of the given thickness centred on radius (mitred corners).
        static void Outline(VertexHelper vh,Vector2 centre,float radius,int sides,float startDegrees,float width,Color col)
        {
            float mitre=width*.5f/Mathf.Cos(Mathf.PI/sides);
            for(int i=0;i<sides;i++){
                Vector2 D(int k){float a=(startDegrees+k*360f/sides)*Mathf.Deg2Rad;return new(Mathf.Cos(a),Mathf.Sin(a));}
                var d0=D(i);var d1=D(i+1);
                ToyMesh.Quad(vh,centre+d0*(radius-mitre),centre+d0*(radius+mitre),centre+d1*(radius+mitre),centre+d1*(radius-mitre),col);
            }
        }
    }
    public sealed class ToyTriangle:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;int s=0;
            vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.center.x,r.yMin),color,Vector2.zero);vh.AddTriangle(s,s+1,s+2);
        }
    }
    public sealed class ToyChecker:MaskableGraphic
    {
        public float cell=30;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;int columns=Mathf.CeilToInt(r.width/cell);float half=r.height*.5f;
            for(int i=0;i<columns;i++)for(int row=0;row<2;row++){
                var col=(i+row)%2==0?color:Color.white;float x=r.xMin+i*cell,y=r.yMin+row*half;
                ToyMesh.Quad(vh,new Vector2(x,y),new Vector2(x,y+half),new Vector2(x+cell,y+half),new Vector2(x+cell,y),col);
            }
        }
    }
    // Flat toy car silhouette in its paint colour (lists and chips; big views use 3D renders).
    public sealed class ToyCarIcon:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=r.width,h=r.height;Vector2 P(float x,float y)=>new(r.xMin+x*w,r.yMin+y*h);float line=Mathf.Max(1.5f,h*.07f);
            var navy=Toy.Navy;var glass=new Color(.87f,.96f,1f);
            ToyMesh.Rounded(vh,new Rect(P(.06f,.22f),new Vector2(w*.88f,h*.4f)),h*.14f,navy);
            ToyMesh.Rounded(vh,new Rect(P(.26f,.5f),new Vector2(w*.5f,h*.42f)),h*.14f,navy);
            ToyMesh.Rounded(vh,new Rect(P(.06f,.22f)+new Vector2(line,line),new Vector2(w*.88f-line*2,h*.4f-line*2)),h*.1f,color);
            ToyMesh.Rounded(vh,new Rect(P(.26f,.5f)+new Vector2(line,line),new Vector2(w*.5f-line*2,h*.42f-line*2)),h*.1f,color);
            ToyMesh.Rounded(vh,new Rect(P(.33f,.58f),new Vector2(w*.36f,h*.24f)),h*.06f,glass);
            foreach(float x in new[]{.28f,.72f}){ToyMesh.Circle(vh,P(x,.24f),h*.2f,navy);ToyMesh.Circle(vh,P(x,.24f),h*.08f,Color.white);}
        }
    }
    // Plots finishing position over race distance (x in laps, y = place; 1st at the top).
    public sealed class ToyLineChart:MaskableGraphic
    {
        public List<Vector2> points=new();public float maxX=3;public int maxPlace=24;public float thickness=7;public Color lineColor=Toy.Red,gridColor=new(0,0,0,.08f);
        public Vector2 Map(Vector2 p){var r=rectTransform.rect;return new Vector2(r.xMin+r.width*Mathf.Clamp01(p.x/maxX),r.yMax-r.height*(p.y-1)/Mathf.Max(1,maxPlace-1));}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(int lap=0;lap<=Mathf.RoundToInt(maxX);lap++){float x=r.xMin+r.width*lap/maxX;ToyMesh.Quad(vh,new Vector2(x-1.5f,r.yMin),new Vector2(x-1.5f,r.yMax),new Vector2(x+1.5f,r.yMax),new Vector2(x+1.5f,r.yMin),gridColor);}
            for(int place=1;place<=maxPlace;place+=Mathf.Max(1,(maxPlace-1)/4+0)){float y=Map(new Vector2(0,place)).y;ToyMesh.Quad(vh,new Vector2(r.xMin,y-1),new Vector2(r.xMin,y+1),new Vector2(r.xMax,y+1),new Vector2(r.xMax,y-1),gridColor);}
            for(int i=1;i<points.Count;i++){
                var a=Map(points[i-1]);var b=Map(points[i]);var n=Vector2.Perpendicular((b-a).normalized)*(thickness*.5f+3);var m=Vector2.Perpendicular((b-a).normalized)*thickness*.5f;
                ToyMesh.Quad(vh,a-n,a+n,b+n,b-n,Toy.Navy);
                ToyMesh.Circle(vh,a,thickness*.5f+3,Toy.Navy,14);
            }
            if(points.Count>0)ToyMesh.Circle(vh,Map(points[^1]),thickness*.5f+3,Toy.Navy,14);
            for(int i=1;i<points.Count;i++){var a=Map(points[i-1]);var b=Map(points[i]);var m=Vector2.Perpendicular((b-a).normalized)*thickness*.5f;ToyMesh.Quad(vh,a-m,a+m,b+m,b-m,lineColor);ToyMesh.Circle(vh,a,thickness*.5f,lineColor,14);}
            if(points.Count>0){ToyMesh.Circle(vh,Map(points[0]),thickness*1.3f,Toy.Navy);ToyMesh.Circle(vh,Map(points[0]),thickness*.9f,Toy.White);ToyMesh.Circle(vh,Map(points[^1]),thickness*1.6f,Toy.Navy);ToyMesh.Circle(vh,Map(points[^1]),thickness*1.2f,Toy.Sun);}
        }
    }

    public sealed class ToyButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        Button button;ToyShape shape;RectTransform face;Color fill;bool pulse,hover;float focusScale=1;
        public void Setup(Button b,ToyShape s,RectTransform f,Color c,bool beat){button=b;shape=s;face=f;fill=c;pulse=beat;Refresh(true);}
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        public bool Focused=>button!=null&&button.interactable&&(EventSystem.current?.currentSelectedGameObject==gameObject||hover);
        void LateUpdate()=>Refresh(false);
        public void Refresh(bool snap)
        {
            if(button==null)return;bool focus=Focused;
            shape.SetFill(!button.interactable?new Color(.78f,.8f,.86f):focus?Color.Lerp(fill,Color.white,.28f):fill);
            shape.SetShadow(new Vector2(0,focus?-10:-7));
            float target=focus?1.07f:1;focusScale=snap?target:Mathf.MoveTowards(focusScale,target,Time.unscaledDeltaTime*1.2f);
            float beat=pulse&&!focus?1+Mathf.Sin(Time.unscaledTime*6)*.025f:1;
            transform.localScale=Vector3.one*focusScale*beat;
            face.anchoredPosition=new Vector2(0,focus?3:0);
        }
    }
    // Queued pop-up stickers ("Postcard found!"): stamp in, hold, fade out.
    public sealed class ToyToaster:MonoBehaviour
    {
        readonly Queue<(string headline,string detail,Color colour)> queue=new();RectTransform current;CanvasGroup fade;float shownAt=-99;
        const float Hold=3.2f;
        public void Show(string headline,string detail,Color colour){queue.Enqueue((headline,detail,colour));}
        void Update()
        {
            float age=Time.unscaledTime-shownAt;
            if(current!=null){fade.alpha=age<Hold?1:Mathf.Clamp01(1-(age-Hold)/.4f);if(age>=Hold+.4f){Destroy(current.gameObject);current=null;}return;}
            if(queue.Count==0)return;var (headline,detail,colour)=queue.Dequeue();shownAt=Time.unscaledTime;
            current=Toy.Rect(transform,"Toast",new Vector2(900,190),new Vector2(-400,170));fade=current.gameObject.AddComponent<CanvasGroup>();fade.blocksRaycasts=false;
            Toy.Sunburst(current,new Vector2(300,300),new Vector2(0,10),colour,Color.Lerp(colour,Color.white,.35f),14,20).circular=true;
            Toy.Title(current,"Headline",headline,76,new Vector2(900,100),new Vector2(0,24),Color.white,TextAnchor.MiddleCenter);
            if(!string.IsNullOrEmpty(detail)){var pill=Toy.Pill(current,"Detail",detail,26,new Vector2(Mathf.Min(860,detail.Length*15+80),54),new Vector2(0,-56),Toy.White,Toy.Navy);}
            var stamp=current.gameObject.AddComponent<ToyStamp>();stamp.duration=.4f;stamp.tilt=-3;
        }
    }
    // Race start: big stamped 3 / 2 / 1 / GO! on the menu camera.
    public sealed class ToyCountdown:MonoBehaviour
    {
        Func<float> elapsed;Func<bool> hidden;Canvas canvas;Text label;ToyStamp stamp;string shown="";
        public void Setup(Func<float> raceClock,Func<bool> suppress)
        {
            elapsed=raceClock;hidden=suppress;
            canvas=CoastUI.Canvas(transform,"Race countdown",60);canvas.gameObject.layer=Toy.MenuLayer;Toy.UseMenuCamera(canvas);
            var holder=Toy.Rect(canvas.transform,"Countdown",new Vector2(600,300),new Vector2(0,120));
            label=Toy.Title(holder,"Number",string.Empty,220,new Vector2(600,300),Vector2.zero,Toy.Sun,TextAnchor.MiddleCenter);
            stamp=holder.gameObject.AddComponent<ToyStamp>();stamp.duration=.35f;
        }
        void LateUpdate()
        {
            if(elapsed==null)return;if(canvas.worldCamera==null)Toy.UseMenuCamera(canvas);
            float t=elapsed();string text=hidden()||t>.9f?"":t<0?Mathf.CeilToInt(-t).ToString():"GO!";
            canvas.enabled=text.Length>0;if(text==shown)return;shown=text;label.text=text;
            label.color=text=="GO!"?Toy.Mint:text=="1"?Toy.Red:text=="2"?Toy.Orange:Toy.Sun;if(text.Length>0)stamp.Replay();
        }
    }
    public sealed class ToySpin:MonoBehaviour{public float degreesPerSecond=4;void Update()=>transform.localRotation=Quaternion.Euler(0,0,Time.unscaledTime*degreesPerSecond);}
    public sealed class ToyBob:MonoBehaviour
    {
        public float height=8,speed=4,phase;Vector2 home;RectTransform rect;bool ready;
        void Update(){if(!ready){rect=(RectTransform)transform;home=rect.anchoredPosition;ready=true;}rect.anchoredPosition=home+new Vector2(0,Mathf.Abs(Mathf.Sin(Time.unscaledTime*speed+phase))*height);}
    }
    public sealed class ToySway:MonoBehaviour{public float phase,angle=6;void Update()=>transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*2.6f+phase)*angle);}
    // Scale-in "stamp" on open, with overshoot.
    public sealed class ToyStamp:MonoBehaviour
    {
        public float delay,duration=.45f,tilt;float start=-1;
        void OnEnable(){start=Time.unscaledTime;Apply(0);}
        void Update(){float t=(Time.unscaledTime-start-delay)/duration;Apply(Mathf.Clamp01(t));if(t>=1)enabled=false;}
        // Drops in from 2.2x, overshoots slightly below 1, and settles.
        void Apply(float t){float s=t<=0?.01f:Mathf.LerpUnclamped(2.2f,1,1-Mathf.Pow(1-t,3))-Mathf.Sin(t*Mathf.PI)*.08f;transform.localScale=Vector3.one*s;transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(tilt-14,tilt,t));}
        public void Replay(){enabled=true;OnEnable();}
    }
    public sealed class ToyConfetti:MonoBehaviour
    {
        struct Piece{public RectTransform rect;public float x,speed,spin,phase;}
        readonly List<Piece> pieces=new();Vector2 size;
        public void Setup(Vector2 area,int count)
        {
            size=area;var random=new System.Random(24);
            for(int i=0;i<count;i++){
                var r=Toy.Rect(transform,"Confetti",new Vector2(16,26),Vector2.zero);var s=r.gameObject.AddComponent<ToyShape>();s.color=Toy.Party[i%Toy.Party.Length];s.border=0;s.shadow=Vector2.zero;s.radius=4;s.raycastTarget=false;
                pieces.Add(new Piece{rect=r,x=(float)random.NextDouble()*area.x-area.x*.5f,speed=120+(float)random.NextDouble()*160,spin=(float)random.NextDouble()*400-200,phase=(float)random.NextDouble()*area.y});
            }
        }
        void Update()
        {
            float t=Time.unscaledTime;
            foreach(var p in pieces){float y=size.y*.5f+60-Mathf.Repeat(p.phase+t*p.speed,size.y+120);p.rect.anchoredPosition=new Vector2(p.x+Mathf.Sin(t*2+p.phase)*18,y);p.rect.localRotation=Quaternion.Euler(0,0,t*p.spin+p.phase);}
        }
    }
}

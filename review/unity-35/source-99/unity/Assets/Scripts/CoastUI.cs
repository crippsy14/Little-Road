using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // Native vector shapes: scale with the screen, no generated bitmap text.
    public sealed class CoastPanel:MaskableGraphic
    {
        public float radius=18;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;float rad=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
            vh.AddVert(r.center,color,Vector2.zero);int n=0;
            for(int corner=0;corner<4;corner++) {
                var centre=new Vector2(corner==0||corner==3?r.xMax-rad:r.xMin+rad,corner<2?r.yMax-rad:r.yMin+rad);
                for(int j=0;j<=8;j++){float a=(corner*90+j*90f/8)*Mathf.Deg2Rad;vh.AddVert(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad,color,Vector2.zero);n++;}
            }
            for(int i=1;i<=n;i++)vh.AddTriangle(0,i,i==n?1:i+1);
        }
    }
    public sealed class CoastArc:MaskableGraphic
    {
        public float value=1,thickness=7;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float outer=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f,inner=outer-thickness;
            for(int i=0;i<=64;i++){float a=(220-260*value*i/64)*Mathf.Deg2Rad;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vh.AddVert(d*outer,color,Vector2.zero);vh.AddVert(d*inner,color,Vector2.zero);if(i>0){int v=i*2;vh.AddTriangle(v-2,v-1,v);vh.AddTriangle(v-1,v+1,v);}}
        }
        public void Set(float v){v=Mathf.Clamp01(v);if(Mathf.Abs(value-v)>.0005f){value=v;SetVerticesDirty();}}
    }
    public static class CoastUI
    {
        public static readonly Color Ink=new(.075f,.22f,.24f),Paper=new(.98f,.94f,.81f),Sea=new(.10f,.48f,.49f),Orange=new(.92f,.36f,.17f),Sun=new(1,.75f,.25f),Muted=new(.33f,.43f,.41f);
        static Font font;
        public static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        public static RectTransform Panel(Transform parent,string name,Vector2 size,Vector2 pos,Color color,float radius=18,bool shadow=false)
        {
            var r=Rect(parent,name,size,pos);var p=r.gameObject.AddComponent<CoastPanel>();p.color=color;p.radius=radius;p.raycastTarget=false;
            if(shadow){var s=r.gameObject.AddComponent<Shadow>();s.effectColor=new Color(.03f,.12f,.13f,.28f);s.effectDistance=new Vector2(0,-5);}
            return r;
        }
        public static Text Label(Transform parent,string name,string value,int size,Vector2 box,Vector2 pos,Color color,bool bold=false,TextAnchor anchor=TextAnchor.MiddleLeft)
        {
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");var t=Rect(parent,name,box,pos).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.text=value;t.color=color;t.alignment=anchor;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
        }
        public static Canvas Canvas(Transform parent,string name,int order)
        {
            var r=Rect(parent,name,new Vector2(1920,1080),Vector2.zero);var c=r.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=Camera.main;c.planeDistance=.65f;c.sortingOrder=order;
            var s=r.gameObject.AddComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1920,1080);s.matchWidthOrHeight=1;return c;
        }
    }
}

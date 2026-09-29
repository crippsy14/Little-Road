using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleRoad
{
    // Driver radar (docs/45 F2): the racing abilities as a filled polygon over rings. Strategy joins at class A;
    // until then five axes are shown, because only abilities with a real effect are displayed.
    public sealed class RacerRadar:MaskableGraphic
    {
        public static readonly string[] Axes={"BRAKING","LINE","CONTROL","CONSISTENCY","RACECRAFT"};
        public float[] values=new float[5];
        public Color fill=new Color(.29f,.48f,1f,.45f),edge=Toy.Blue,ring=new Color(.11f,.17f,.39f,.25f);
        public void Set(in RacerAbilities a){values=new[]{a.braking,a.line,a.control,a.consistency,a.racecraft};SetVerticesDirty();}
        public static int DriverRating(in RacerAbilities a)=>RacerAbilities.Rating((a.braking+a.line+a.control+a.consistency+a.racecraft)/5);
        Vector2 Point(int axis,float value)
        {
            float r=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f*value,angle=Mathf.PI*.5f-axis*2*Mathf.PI/Axes.Length;
            return rectTransform.rect.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();int n=Axes.Length;
            for(int ringStep=1;ringStep<=4;ringStep++)for(int i=0;i<n;i++)Line(vh,Point(i,ringStep/4f),Point((i+1)%n,ringStep/4f),2,ring);
            for(int i=0;i<n;i++)Line(vh,Point(i,0),Point(i,1),2,ring);
            // Filled area as a fan from the centre, then its outline.
            int centre=vh.currentVertCount;vh.AddVert(Point(0,0),fill,Vector2.zero);
            for(int i=0;i<n;i++)vh.AddVert(Point(i,Mathf.Clamp(values[i],.04f,1)),fill,Vector2.zero);
            for(int i=0;i<n;i++)vh.AddTriangle(centre,centre+1+i,centre+1+(i+1)%n);
            for(int i=0;i<n;i++)Line(vh,Point(i,Mathf.Clamp(values[i],.04f,1)),Point((i+1)%n,Mathf.Clamp(values[(i+1)%n],.04f,1)),4,edge);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var d=(b-a).normalized;var side=new Vector2(-d.y,d.x)*width*.5f;int k=vh.currentVertCount;
            vh.AddVert(a-side,c,Vector2.zero);vh.AddVert(a+side,c,Vector2.zero);vh.AddVert(b+side,c,Vector2.zero);vh.AddVert(b-side,c,Vector2.zero);
            vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }
    // A radar card for the race preview: name, driver rating, radar with 1-20 values, personality chips.
    public sealed class RacerRadarCard
    {
        public readonly GameObject root;readonly RacerRadar radar;readonly Text title,rating,traits;readonly Text[] values=new Text[5];
        public RacerRadarCard(Transform parent,Vector2 position)
        {
            var card=Toy.Card(parent,"Driver radar",new Vector2(460,470),position,Toy.White,34,6,8);root=card.gameObject;
            title=Toy.Title(card.transform,"Radar name",string.Empty,34,new Vector2(300,48),new Vector2(-60,196),Toy.Sun);
            var badge=Toy.Card(card.transform,"Radar rating",new Vector2(92,56),new Vector2(165,196),Toy.Blue,28,4,4);
            rating=Toy.Label(badge.transform,"Radar rating text",string.Empty,24,new Vector2(92,56),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            radar=Toy.Rect(card.transform,"Radar",new Vector2(250,250),new Vector2(0,-5)).gameObject.AddComponent<RacerRadar>();radar.raycastTarget=false;
            for(int i=0;i<RacerRadar.Axes.Length;i++) {
                float angle=Mathf.PI*.5f-i*2*Mathf.PI/RacerRadar.Axes.Length;var at=new Vector2(Mathf.Cos(angle)*178,Mathf.Sin(angle)*152-5);
                Toy.Label(card.transform,"Axis "+i,RacerRadar.Axes[i],15,new Vector2(150,22),at+new Vector2(0,9),Toy.Soft,TextAnchor.MiddleCenter,true);
                values[i]=Toy.Label(card.transform,"Value "+i,string.Empty,19,new Vector2(150,24),at-new Vector2(0,12),Toy.Navy,TextAnchor.MiddleCenter,true);
            }
            traits=Toy.Label(card.transform,"Traits",string.Empty,18,new Vector2(420,30),new Vector2(0,-205),Toy.Navy,TextAnchor.MiddleCenter,true);
            root.SetActive(false);
        }
        public void Show(string name,in RacerAbilities a)
        {
            title.text=name;rating.text="DR "+RacerRadar.DriverRating(a);radar.Set(a);
            float[] v={a.braking,a.line,a.control,a.consistency,a.racecraft};
            for(int i=0;i<v.Length;i++)values[i].text=RacerAbilities.Rating(v[i])+"/20";
            traits.text=$"Aggression {RacerAbilities.Rating(a.aggression)}/20  ·  Patience {RacerAbilities.Rating(a.patience)}/20";
            root.SetActive(true);root.transform.SetAsLastSibling();
        }
        public void Hide()=>root.SetActive(false);
    }
    // Mouse hover in and out (the paddock hover only reports entering).
    public sealed class HoverArea:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public Action entered,exited;
        public void OnPointerEnter(PointerEventData e)=>entered?.Invoke();
        public void OnPointerExit(PointerEventData e)=>exited?.Invoke();
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // Lightweight vector markings, kept behind the genuinely rendered 3D parts.
    public sealed class WorkshopMat:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var grid=new Color(.5f,.82f,.76f,.12f);
            for(float x=r.xMin+10;x<r.xMax;x+=25)Line(vh,new Vector2(x,r.yMin+10),new Vector2(x,r.yMax-10),.7f,grid);
            for(float y=r.yMin+10;y<r.yMax;y+=25)Line(vh,new Vector2(r.xMin+10,y),new Vector2(r.xMax-10,y),.7f,grid);
            var ink=new Color(.6f,.87f,.78f,.32f);
            for(int i=0;i<90;i++)if(i%3!=0){float a=i*Mathf.PI*2/90,b=(i+1)*Mathf.PI*2/90;Line(vh,new Vector2(Mathf.Cos(a)*250,Mathf.Sin(a)*113),new Vector2(Mathf.Cos(b)*250,Mathf.Sin(b)*113),1.3f,ink);}
            Line(vh,new Vector2(-13,0),new Vector2(13,0),1,ink);Line(vh,new Vector2(0,-13),new Vector2(0,13),1,ink);
            for(int i=0;i<28;i++){float x=-335+i*25;Line(vh,new Vector2(x,-179),new Vector2(x,-179+(i%5==0?10:5)),1,ink);}
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {var d=b-a;var side=new Vector2(-d.y,d.x).normalized*width*.5f;int v=vh.currentVertCount;vh.AddVert(a-side,c,Vector2.zero);vh.AddVert(a+side,c,Vector2.zero);vh.AddVert(b+side,c,Vector2.zero);vh.AddVert(b-side,c,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
    }
}

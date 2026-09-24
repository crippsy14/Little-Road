using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleRoad
{
    // Shared menu treatment, kept separate from the transparent driving instruments.
    public static class CoastMenu
    {
        public static readonly Color Cream=new(.97f,.94f,.85f),Pale=new(.89f,.88f,.77f),Navy=new(.075f,.20f,.23f);
        public static Button Button(Transform parent,string name,string caption,Vector2 size,Vector2 position,Action clicked,bool primary=false)
        {
            var rect=CoastUI.Panel(parent,name,size,position,primary?CoastUI.Sun:Pale,14);
            var panel=rect.GetComponent<CoastPanel>();panel.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=panel;button.transition=Selectable.Transition.None;
            var label=CoastUI.Label(rect,"Caption",caption,22,size-new Vector2(66,8),new Vector2(9,0),Navy);
            var arrow=CoastUI.Label(rect,"Focus arrow",">",24,new Vector2(24,size.y),new Vector2(-size.x*.5f+23,0),Cream,true);
            var visual=rect.gameObject.AddComponent<CoastMenuButton>();visual.Setup(button,panel,label,arrow,primary);
            button.onClick.AddListener(()=>clicked?.Invoke());return button;
        }
        public static RectTransform Paper(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var paper=CoastUI.Panel(parent,name,size,position,Cream,26,true);
            for(int side=-1;side<=1;side+=2){
                var tape=CoastUI.Panel(paper,"Paper tape",new Vector2(93,24),new Vector2(side*(size.x*.5f-58),size.y*.5f-2),new Color(.94f,.77f,.42f,.9f),2);
                tape.localRotation=Quaternion.Euler(0,0,side*13);
            }
            return paper;
        }
        public static RawImage Illustration(Transform parent,Vector2 size,Vector2 position)
        {
            var image=CoastUI.Rect(parent,"Original coastal illustration",size,position).gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>("UI/coastal-race-postcard-v1");image.raycastTarget=false;return image;
        }
        public static void RefreshButtons(Transform root){foreach(var visual in root.GetComponentsInChildren<CoastMenuButton>())visual.Refresh();}
        public static RectTransform Backdrop(Transform parent)
        {
            var bg=CoastUI.Panel(parent,"Coastal race-day backdrop",new Vector2(2400,1600),Vector2.zero,Navy,0);
            Illustration(bg,new Vector2(2400,1350),Vector2.zero);
            CoastUI.Panel(bg,"Header paper wash",new Vector2(1010,168),new Vector2(-459,449),new Color(.97f,.94f,.85f,.93f),20);
            CoastUI.Panel(bg,"Control footer",new Vector2(1920,136),new Vector2(0,-481),new Color(.075f,.20f,.23f,.96f),0);
            return bg;
        }
    }
    public sealed class CoastMenuButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        Button button;CoastPanel panel;Text label,arrow;bool primary,hover;
        public void Setup(Button b,CoastPanel p,Text caption,Text pointer,bool action){button=b;panel=p;label=caption;arrow=pointer;primary=action;Refresh();}
        public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
        public void OnPointerExit(PointerEventData e){hover=false;Refresh();}
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(button==null)return;bool focus=button.interactable&&(EventSystem.current?.currentSelectedGameObject==gameObject||hover);
            panel.color=!button.interactable?new Color(.77f,.78f,.72f):focus?CoastUI.Sea:primary?CoastUI.Sun:CoastMenu.Pale;
            label.color=focus?Color.white:CoastMenu.Navy;label.fontStyle=focus?FontStyle.Bold:FontStyle.Normal;arrow.gameObject.SetActive(focus);
        }
    }
    // Original vector wave bands; no reference artwork is embedded in the product.
    public sealed class CoastWaves:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(int band=0;band<3;band++){
                Color ink=band==0?new Color(.12f,.39f,.40f):band==1?new Color(.18f,.49f,.47f):new Color(.81f,.69f,.43f);
                for(int i=0;i<48;i++){
                    float x0=r.xMin+r.width*i/48,x1=r.xMin+r.width*(i+1)/48;
                    float y0=r.yMin+165-band*55+Mathf.Sin(i*.19f+band)*65,y1=r.yMin+165-band*55+Mathf.Sin((i+1)*.19f+band)*65;
                    // Lift the shoreline into the lower edge of the 1080p reference canvas.
                    y0+=260;y1+=260;int v=vh.currentVertCount;
                    vh.AddVert(new Vector2(x0,r.yMin),ink,Vector2.zero);vh.AddVert(new Vector2(x0,y0),ink,Vector2.zero);vh.AddVert(new Vector2(x1,y1),ink,Vector2.zero);vh.AddVert(new Vector2(x1,r.yMin),ink,Vector2.zero);
                    vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
                }
            }
        }
    }
}

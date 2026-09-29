using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // World marker above someone you can talk to (the "discover" tier of interaction prompts): a small
    // round bubble in the character's colour, speech dots inside, or a "!" when they are your current
    // objective. No text: names and the button live in the bottom prompt card (AdventureUI.Interaction)
    // when you are in range. Keeps a steady on-screen size, fades with distance, dims behind walls,
    // and pulses while it is the prompt's target.
    public sealed class TalkBadge:MonoBehaviour
    {
        const float Show=38,FadeFrom=30,PixelsTall=58;
        public bool objective,targeted,onlyWhenTargeted;
        CanvasGroup group;RectTransform badge;ToyShape disc;GameObject dots,bang;Transform anchor;float height,occlusionTimer;bool occluded;
        public static TalkBadge Add(GameObject owner,Color colour)
        {
            var b=owner.AddComponent<TalkBadge>();b.Build(colour);return b;
        }
        void Build(Color colour)
        {
            var renderers=GetComponentsInChildren<Renderer>();float top=transform.position.y+1.7f;foreach(var r in renderers)top=Mathf.Max(top,r.bounds.max.y);
            height=top-transform.position.y+.75f;
            var go=new GameObject("Talk badge",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));anchor=go.transform;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=5;group=go.GetComponent<CanvasGroup>();group.blocksRaycasts=false;
            badge=(RectTransform)go.transform;badge.sizeDelta=new Vector2(120,120);
            disc=Toy.Card(badge,"Badge disc",new Vector2(100,100),Vector2.zero,colour,50,8,6);disc.borderColor=Toy.White;
            dots=new GameObject("Speech dots",typeof(RectTransform));dots.transform.SetParent(disc.transform,false);
            for(int i=-1;i<=1;i++)Toy.Card(dots.transform,"Dot",new Vector2(17,17),new Vector2(i*24,0),Toy.White,8.5f,0,0);
            bang=Toy.Title(disc.transform,"Objective bang","!",70,new Vector2(100,100),new Vector2(0,2),Toy.White,TextAnchor.MiddleCenter).gameObject;bang.SetActive(false);
            group.alpha=0;
        }
        void LateUpdate()
        {
            var view=Camera.main;if(view==null||anchor==null)return;
            Vector3 at=transform.position+Vector3.up*(height+Mathf.Sin(Time.time*2.4f+transform.position.x)*.06f);
            float distance=Vector3.Distance(view.transform.position,at);
            // Dim (not hide) behind buildings, checked a few times a second.
            occlusionTimer-=Time.deltaTime;
            if(occlusionTimer<=0){occlusionTimer=.25f;occluded=Physics.Linecast(view.transform.position,at,out var hit)&&!hit.transform.IsChildOf(transform);}
            float alpha=distance>Show||onlyWhenTargeted&&!targeted?0:1-Mathf.InverseLerp(FadeFrom,Show,distance);if(occluded&&!targeted)alpha*=.35f;
            group.alpha=Mathf.MoveTowards(group.alpha,alpha,Time.deltaTime*4);
            anchor.gameObject.SetActive(group.alpha>.01f);if(!anchor.gameObject.activeSelf)return;
            dots.SetActive(!objective);bang.SetActive(objective);
            // World size for a steady PixelsTall on screen (1080p), a bit larger while targeted.
            float world=PixelsTall*2*distance*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)/1080f;
            float pulse=targeted?1.2f+Mathf.Sin(Time.time*6)*.06f:objective?1.1f:1;
            anchor.SetPositionAndRotation(at,view.transform.rotation);anchor.localScale=Vector3.one*(world/100f)*pulse;
            disc.SetBorder(targeted?Toy.Sun:Toy.White);
        }
        void OnDestroy(){if(anchor!=null)Destroy(anchor.gameObject);}
    }
}

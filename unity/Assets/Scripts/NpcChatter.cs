using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // A car that answers back when the player bumps it (free roam): residents have their own
    // lines, everyone else uses the general set. Works for residents, parked, camp and traffic cars.
    public sealed class NpcChatter:MonoBehaviour
    {
        public string speaker="Driver";public Color colour=Toy.Sky;public string[] lines;
        float ready;int last=-1;public float LastSaid=-99;
        // Talk (E / Cross) to any standing car: title shows under the name; talk lines default to the
        // bump lines. Handled elsewhere (talkable=false): Hillcrest residents and Bayview characters
        // (their own dialogs) and the train.
        public string title;public string[] talk;public bool talkable=true;
        public static readonly List<NpcChatter> All=new();
        void OnEnable()=>All.Add(this);void OnDisable()=>All.Remove(this);
        Rigidbody body;
        public bool CanTalk
        {
            get{
                if(!talkable)return false;if(body==null)body=GetComponent<Rigidbody>();
                return body==null||(body.isKinematic?GetComponent<RoadCruiser>()==null||GetComponent<RoadCruiser>().Speed<.6f:body.linearVelocity.sqrMagnitude<1);
            }
        }
        public string[] TalkLines=>talk!=null&&talk.Length>0?talk:lines!=null&&lines.Length>0?lines:General;
        public static readonly string[] General={
            "Hey! Mind the paintwork!","Beep beep! Coming through?","Oof, my bumper!","Easy there, speedy!",
            "I only just washed this!","Nice little car you've got!","Are you racing today?","Have you tried the ice cream?",
            "Watch it, I'm parked here!","Honk honk! Lovely day for a drive!","You'll want Mara to look at that dent.","Heading down to the harbour?"};
        public bool TrySay(SpeechBubbles bubbles)
        {
            if(Time.unscaledTime<ready||bubbles==null)return false;
            var pool=lines!=null&&lines.Length>0?lines:General;
            int pick=Random.Range(0,pool.Length);if(pool.Length>1&&pick==last)pick=(pick+1)%pool.Length;last=pick;
            bubbles.Show(transform,speaker,pool[pick],colour);ready=Time.unscaledTime+4f;LastSaid=Time.time;return true;
        }
    }

    // Screen-space speech bubbles that follow a car: navy-outlined card, name pill, tail.
    public sealed class SpeechBubbles:MonoBehaviour
    {
        sealed class Bubble{public RectTransform rect;public Transform target;public float born;}
        Canvas canvas;RectTransform area;readonly List<Bubble> live=new();const float Life=3.2f;
        void Awake()
        {
            canvas=CoastUI.Canvas(transform,"Speech bubbles",7);canvas.gameObject.layer=Toy.MenuLayer;area=(RectTransform)canvas.transform;
        }
        public void Show(Transform target,string speaker,string words,Color colour)
        {
            Toy.UseMenuCamera(canvas);
            live.RemoveAll(b=>{if(b.target!=target)return false;Destroy(b.rect.gameObject);return true;});
            var holder=Toy.Rect(canvas.transform,"Speech",new Vector2(460,150),Vector2.zero);
            float width=Mathf.Clamp(words.Length*14+70,260,560);
            var card=Toy.Card(holder,"Bubble",new Vector2(width,96),new Vector2(0,20),Toy.White,40,6,6);
            Toy.Label(card.transform,"Words",words,26,new Vector2(width-44,90),new Vector2(0,-2),Toy.Navy,TextAnchor.MiddleCenter,true);
            var tail=Toy.Rect(holder,"Tail",new Vector2(34,30),new Vector2(-width*.25f,-40)).gameObject.AddComponent<ToyTriangle>();tail.color=Toy.Navy;tail.raycastTarget=false;
            var inner=Toy.Rect(holder,"Tail fill",new Vector2(22,19),new Vector2(-width*.25f,-35)).gameObject.AddComponent<ToyTriangle>();inner.color=Toy.White;inner.raycastTarget=false;
            Toy.Pill(holder,"Speaker",speaker.ToUpperInvariant(),20,new Vector2(Mathf.Max(120,speaker.Length*13+40),36),new Vector2(-width*.5f+80,72),colour,Toy.Navy,-4);
            var stamp=holder.gameObject.AddComponent<ToyStamp>();stamp.duration=.3f;stamp.tilt=2;
            foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Toy.MenuLayer;
            live.Add(new Bubble{rect=holder,target=target,born=Time.unscaledTime});Place(live[^1]);
        }
        void LateUpdate()
        {
            for(int i=live.Count-1;i>=0;i--) {
                var b=live[i];float age=Time.unscaledTime-b.born;
                if(b.target==null||age>Life){Destroy(b.rect.gameObject);live.RemoveAt(i);continue;}
                Place(b);
            }
        }
        void Place(Bubble b)
        {
            var view=Camera.main;if(view==null)return;
            Vector3 screen=view.WorldToScreenPoint(b.target.position+Vector3.up*2.6f);
            bool visible=screen.z>0;b.rect.gameObject.SetActive(visible);if(!visible)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(area,screen,canvas.worldCamera,out var local))b.rect.anchoredPosition=local+new Vector2(0,60);
        }
    }
}
